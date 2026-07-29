using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VmScriptCompiler.Core;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Clipboard = System.Windows.Clipboard;

namespace VmScriptCompiler.Desktop;

public partial class MainWindow : Window
{
    private readonly CompilerFacade _compiler;
    private readonly DesktopStateStore _stateStore;
    private DesktopSettings _settings;
    private AgentProcessClient? _agent;
    private CancellationTokenSource? _runCancellation;
    private IReadOnlyList<ArtifactRecord> _artifacts = [];
    private TextBlock? _streamingAssistant;
    private readonly Dictionary<string, ToolCardVisual> _activeTools = new(StringComparer.Ordinal);
    private string? _lastSolution;
    private string? _lastReport;
    private string? _lastDependencyDirectory;
    private string _conversationTitle = "新任务";
    private bool _selectingSession;

    public MainWindow()
    {
        InitializeComponent();
        _compiler = new CompilerFacade(RepositoryLocator.Find());
        _stateStore = new DesktopStateStore();
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var defaultOutput = Path.Combine(documents, "VM Script Compiler", "outputs");
        _settings = _stateStore.LoadSettings(defaultOutput);
        OutputPathText.Text = _settings.OutputDirectory;
        LoadSettingsForm();
        SetWorkspace(ComposerTab, "新任务");
    }

    internal void PrepareUiSnapshot(string? requestedTab, bool conversationPreview)
    {
        switch (requestedTab?.ToLowerInvariant())
        {
            case "artifacts":
                SetWorkspace(ArtifactsTab, "结果产物");
                ApplyArtifactFilter();
                break;
            case "settings":
                SetWorkspace(SettingsTab, "设置");
                break;
            default:
                _conversationTitle = conversationPreview ? "创建 C# 求和脚本并设置默认值" : "新任务";
                SetWorkspace(ComposerTab, _conversationTitle);
                break;
        }
        if (!conversationPreview || WorkspaceTabs.SelectedItem != ComposerTab) return;
        WelcomePanel.Visibility = Visibility.Collapsed;
        AppendUserMessage("创建一个 C# 脚本，输入 A 和 B，输出 Sum，并设置默认值。");
        AppendAssistantDelta("我会先确认 VM 4.4 环境和端口能力，再建立 Requirement 并交给确定性编译器生成方案。");
        _streamingAssistant = null;
        AppendToolStatus("vm_detect_environment", false, false);
        AppendToolStatus("vm_detect_environment", true, false);
        AppendToolStatus("vm_validate_requirement", false, false);
        WorkspacePhaseText.Text = "校验需求";
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshArtifactIndexAsync();
        await DetectEnvironmentAsync();
        await StartAgentAsync();
    }

    private async Task StartAgentAsync()
    {
        AgentStatusText.Text = "Agent 正在启动…";
        AgentStatusDot.Fill = new SolidColorBrush(Color.FromRgb(206, 155, 69));
        AiStatusText.Text = "正在初始化 Pi Runtime…";
        try
        {
            if (_agent is not null) await _agent.DisposeAsync();
            _agent = new AgentProcessClient();
            _agent.EventReceived += Agent_EventReceived;
            _agent.DiagnosticReceived += Agent_DiagnosticReceived;
            var result = await _agent.StartAsync(new AgentConnectionOptions(
                _settings.AiProvider,
                _settings.AiEndpoint,
                _settings.AiModel,
                _stateStore.UnprotectSecret(_settings.EncryptedApiKey),
                _settings.ThinkingLevel,
                _settings.OutputDirectory,
                Path.Combine(_stateStore.StateDirectory, "agent")));
            AgentStatusText.Text = "Agent 已连接";
            AgentStatusDot.Fill = new SolidColorBrush(Color.FromRgb(80, 182, 122));
            AiStatusText.Text = $"Pi · {_settings.AiProvider} · {_settings.AiModel}";
            RenderSnapshot(result);
            await RefreshHistoryAsync();
        }
        catch (Exception error)
        {
            AgentStatusText.Text = "Agent 未连接";
            AgentStatusDot.Fill = new SolidColorBrush(Color.FromRgb(227, 107, 107));
            AiStatusText.Text = "请在“配置”中填写 API 地址、模型和 Key";
            ShowFriendlyError(error);
        }
    }

    private async void ReconnectAgent_Click(object sender, RoutedEventArgs e) => await StartAgentAsync();

    private void PromptText_TextChanged(object sender, TextChangedEventArgs e)
        => PromptPlaceholder.Visibility = string.IsNullOrEmpty(PromptText.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

    private void PromptText_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) return;
        e.Handled = true;
        if (GenerateButton.IsEnabled) Generate_Click(GenerateButton, new RoutedEventArgs());
    }

    private void Suggestion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string prompt }) return;
        PromptText.Text = prompt;
        PromptText.Focus();
        PromptText.CaretIndex = PromptText.Text.Length;
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        var collapse = SidebarColumn.Width.Value > 0;
        SidebarColumn.Width = collapse ? new GridLength(0) : new GridLength(252);
        SidebarPanel.Visibility = collapse ? Visibility.Collapsed : Visibility.Visible;
        ExpandSidebarButton.Visibility = collapse ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        var text = PromptText.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            FriendlyStatus.Text = "请先描述要生成或修改的脚本。";
            FriendlyStatus.Visibility = Visibility.Visible;
            return;
        }
        if (_agent is null || !_agent.IsRunning)
        {
            await StartAgentAsync();
            if (_agent is null || !_agent.IsRunning) return;
        }

        var mode = PatchModeRadio.IsChecked == true ? "patch" : "create";
        string? baseSolution = null;
        if (mode == "patch")
        {
            if (string.IsNullOrWhiteSpace(BaseSolutionText.Text) || !File.Exists(BaseSolutionText.Text))
            {
                FriendlyStatus.Text = "Patch 模式需要先选择基底 SOL。";
                FriendlyStatus.Visibility = Visibility.Visible;
                return;
            }
            baseSolution = Path.GetFullPath(BaseSolutionText.Text);
        }

        AppendUserMessage(text);
        if (_conversationTitle == "新任务")
        {
            _conversationTitle = CompactTitle(text);
            WorkspaceTitleText.Text = _conversationTitle;
        }
        PromptText.Clear();
        WelcomePanel.Visibility = Visibility.Collapsed;
        FriendlyStatus.Text = "Agent 正在检查、规划和验证…";
        FriendlyStatus.Foreground = Brushes.DarkGray;
        FriendlyStatus.Visibility = Visibility.Visible;
        SuccessPanel.Visibility = Visibility.Collapsed;
        WorkspacePhaseText.Text = "处理中";
        GenerateButton.IsEnabled = false;
        StopButton.Visibility = Visibility.Visible;
        _runCancellation = new CancellationTokenSource();
        try
        {
            var completed = await _agent.PromptAsync(
                text,
                mode,
                baseSolution,
                RequireDirectory(OutputPathText.Text),
                _runCancellation.Token);
            var snapshot = completed.GetProperty("state");
            RenderSnapshot(snapshot);
            var domain = snapshot.GetProperty("state");
            if (domain.GetProperty("phase").GetString() == "offline-validated")
            {
                ReadArtifacts(domain);
                SuccessText.Text = _lastSolution is null
                    ? "已通过离线验证。"
                    : "已生成并通过离线验证：" + Path.GetFileName(_lastSolution);
                SuccessPanel.Visibility = Visibility.Visible;
                FriendlyStatus.Text = "离线验证已通过；请在 VisionMaster 中打开、编译和运行后再确认实机结果。";
                FriendlyStatus.Foreground = Brushes.SeaGreen;
            }
            await RefreshArtifactIndexAsync();
            await RefreshHistoryAsync();
        }
        catch (OperationCanceledException)
        {
            FriendlyStatus.Text = "已停止当前 Agent 任务。";
            FriendlyStatus.Foreground = Brushes.DarkOrange;
        }
        catch (Exception error) { ShowFriendlyError(error); }
        finally
        {
            _runCancellation?.Dispose();
            _runCancellation = null;
            GenerateButton.IsEnabled = true;
            StopButton.Visibility = Visibility.Collapsed;
            _streamingAssistant = null;
        }
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_agent is not null) await _agent.AbortAsync();
        }
        catch { }
        _runCancellation?.Cancel();
    }

    private void Agent_EventReceived(object? sender, JsonElement message)
    {
        var clone = message.Clone();
        Dispatcher.BeginInvoke(() => HandleAgentEvent(clone));
    }

    private void HandleAgentEvent(JsonElement message)
    {
        if (!message.TryGetProperty("event", out var envelope)) return;
        var envelopeType = String(envelope, "type");
        if (envelopeType == "domain" && envelope.TryGetProperty("state", out var state))
        {
            var phase = String(state, "phase") ?? "draft";
            WorkspacePhaseText.Text = FriendlyPhase(phase);
            SidebarStatusText.Text = $"阶段：{phase} · Requirement r{Int(state, "requirementRevision")}";
            return;
        }
        if (envelopeType != "pi" || !envelope.TryGetProperty("event", out var piEvent)) return;
        var type = String(piEvent, "type");
        if (type == "message_update" &&
            piEvent.TryGetProperty("assistantMessageEvent", out var update) &&
            String(update, "type") == "text_delta")
        {
            AppendAssistantDelta(String(update, "delta") ?? "");
        }
        else if (type == "message_end") _streamingAssistant = null;
        else if (type == "tool_execution_start")
        {
            var toolName = String(piEvent, "toolName") ?? "VM 工具";
            AppendToolStatus(
                toolName,
                false,
                false,
                String(piEvent, "toolCallId") ?? toolName);
        }
        else if (type == "tool_execution_end")
        {
            var toolName = String(piEvent, "toolName") ?? "VM 工具";
            AppendToolStatus(
                toolName,
                true,
                piEvent.TryGetProperty("isError", out var isError) && isError.GetBoolean(),
                String(piEvent, "toolCallId") ?? toolName);
        }
        else if (type == "auto_retry_start") AppendSystemStatus("Provider 暂时失败，Pi 正在重试…");
        ScrollTranscript();
    }

    private void Agent_DiagnosticReceived(object? sender, string diagnostic)
        => Dispatcher.BeginInvoke(() => ResultText.AppendText(diagnostic + Environment.NewLine));

    private void AppendUserMessage(string text)
    {
        var content = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            LineHeight = 22
        };
        var bubble = new Border
        {
            Child = content,
            Padding = new Thickness(14, 10, 14, 10),
            Background = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
            CornerRadius = new CornerRadius(11),
            MaxWidth = 650,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        var container = new StackPanel
        {
            Margin = new Thickness(90, 0, 6, 22),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        container.Children.Add(new TextBlock
        {
            Text = "你",
            Margin = new Thickness(0, 0, 2, 6),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Foreground = new SolidColorBrush(Color.FromRgb(137, 137, 137)),
            FontSize = 11
        });
        container.Children.Add(bubble);
        MessagesPanel.Children.Add(container);
        ScrollTranscript();
    }

    private void AppendAssistantDelta(string delta)
    {
        if (_streamingAssistant is null)
        {
            _streamingAssistant = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                LineHeight = 22
            };
            var container = new StackPanel { Margin = new Thickness(6, 0, 54, 22) };
            var header = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 7)
            };
            header.Children.Add(new Border
            {
                Width = 22,
                Height = 22,
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush(Color.FromRgb(239, 239, 239)),
                CornerRadius = new CornerRadius(6),
                Child = new TextBlock
                {
                    Text = "VM",
                    Foreground = new SolidColorBrush(Color.FromRgb(17, 17, 17)),
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
            header.Children.Add(new TextBlock
            {
                Text = "VM Script Agent",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold
            });
            container.Children.Add(header);
            container.Children.Add(_streamingAssistant);
            MessagesPanel.Children.Add(container);
        }
        _streamingAssistant.Text += delta;
        ScrollTranscript();
    }

    private void AppendToolStatus(string name, bool completed, bool error, string? toolCallId = null)
    {
        var key = toolCallId ?? name;
        if (!completed)
        {
            var status = new TextBlock
            {
                Text = "运行中",
                Foreground = new SolidColorBrush(Color.FromRgb(165, 165, 165)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            var icon = new TextBlock
            {
                Text = "◌",
                Width = 24,
                Foreground = new SolidColorBrush(Color.FromRgb(143, 175, 224)),
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Center
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Children.Add(icon);
            var title = new TextBlock
            {
                Text = FriendlyToolName(name),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(title, 1);
            Grid.SetColumn(status, 2);
            grid.Children.Add(title);
            grid.Children.Add(status);
            var card = new Border
            {
                Child = grid,
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(38, 0, 84, 8),
                Background = new SolidColorBrush(Color.FromRgb(23, 23, 23)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
            _activeTools[key] = new ToolCardVisual(card, icon, status);
            MessagesPanel.Children.Add(card);
            return;
        }

        if (!_activeTools.Remove(key, out var visual))
        {
            AppendToolStatus(name, false, false, key);
            _activeTools.Remove(key, out visual);
        }
        if (visual is null) return;
        visual.Icon.Text = error ? "×" : "✓";
        visual.Icon.Foreground = error
            ? new SolidColorBrush(Color.FromRgb(227, 107, 107))
            : new SolidColorBrush(Color.FromRgb(80, 182, 122));
        visual.Status.Text = error ? "失败" : "完成";
        visual.Status.Foreground = visual.Icon.Foreground;
        visual.Card.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
    }

    private void AppendSystemStatus(string text)
    {
        MessagesPanel.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(116, 116, 116)),
            FontSize = 11.5,
            Margin = new Thickness(40, 2, 40, 10),
            TextWrapping = TextWrapping.Wrap
        });
    }

    private static string FriendlyToolName(string name) => name switch
    {
        "vm_detect_environment" => "检测 VM 环境",
        "vm_inspect_solution" => "检查基底 SOL",
        "vm_query_capability" => "查询 VM 能力",
        "vm_update_requirement" => "更新 Requirement",
        "vm_compile_solution" => "生成并验证 SOL",
        "vm_validate_requirement" => "校验 Requirement",
        "vm_plan_solution" => "规划 SOL",
        "vm_build_solution" => "创建 SOL",
        "vm_patch_solution" => "补丁 SOL",
        "vm_validate_solution" => "验证 SOL",
        "vm_read_build_report" => "读取构建报告",
        _ => name
    };

    private void RenderSnapshot(JsonElement snapshot)
    {
        if (!snapshot.TryGetProperty("state", out var state)) return;
        var phase = String(state, "phase") ?? "draft";
        WorkspacePhaseText.Text = FriendlyPhase(phase);
        SidebarStatusText.Text = $"阶段：{phase} · Requirement r{Int(state, "requirementRevision")}";
        ReadArtifacts(state);
        if (!snapshot.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array) return;
        MessagesPanel.Children.Clear();
        _activeTools.Clear();
        WelcomePanel.Visibility = messages.GetArrayLength() == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var message in messages.EnumerateArray())
        {
            var role = String(message, "role");
            var text = MessageText(message);
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (role == "user") AppendUserMessage(StripTaskContext(text));
            else if (role == "assistant")
            {
                _streamingAssistant = null;
                AppendAssistantDelta(text);
                _streamingAssistant = null;
            }
        }
    }

    private void ReadArtifacts(JsonElement state)
    {
        if (!state.TryGetProperty("artifacts", out var artifacts) || artifacts.ValueKind != JsonValueKind.Array) return;
        foreach (var artifact in artifacts.EnumerateArray())
        {
            var kind = String(artifact, "kind");
            var path = String(artifact, "path");
            if (kind == "solution") _lastSolution = path;
            if (kind == "report") _lastReport = path;
            if (kind == "task-directory" && path is not null)
            {
                var dependency = Path.Combine(path, "dependencies");
                _lastDependencyDirectory = Directory.Exists(dependency) ? dependency : null;
            }
        }
        OpenDependenciesButton.Visibility = _lastDependencyDirectory is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        _conversationTitle = "新任务";
        SetWorkspace(ComposerTab, "新任务");
        if (_agent is null || !_agent.IsRunning) await StartAgentAsync();
        if (_agent is null || !_agent.IsRunning) return;
        try
        {
            var snapshot = await _agent.NewSessionAsync();
            MessagesPanel.Children.Clear();
            WelcomePanel.Visibility = Visibility.Visible;
            PromptText.Clear();
            BaseSolutionText.Clear();
            CreateModeRadio.IsChecked = true;
            SuccessPanel.Visibility = Visibility.Collapsed;
            FriendlyStatus.Visibility = Visibility.Collapsed;
            _lastSolution = _lastReport = _lastDependencyDirectory = null;
            WorkspacePhaseText.Text = "draft";
            RenderSnapshot(snapshot);
            await RefreshHistoryAsync();
            PromptText.Focus();
        }
        catch (Exception error) { ShowFriendlyError(error); }
    }

    private async void RefreshHistory_Click(object sender, RoutedEventArgs e) => await RefreshHistoryAsync();

    private async Task RefreshHistoryAsync()
    {
        if (_agent is null || !_agent.IsRunning) return;
        try
        {
            var sessions = await _agent.ListSessionsAsync();
            var items = sessions.ValueKind == JsonValueKind.Array
                ? sessions.EnumerateArray().Select(AgentSessionItem.FromJson).OrderByDescending(x => x.Modified).ToArray()
                : [];
            _selectingSession = true;
            RecentConversationList.ItemsSource = items;
            _selectingSession = false;
            SidebarStatusText.Text = $"共 {items.Length} 个可恢复会话";
        }
        catch (Exception error) { SidebarStatusText.Text = error.Message; }
    }

    private async void RecentConversation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectingSession || RecentConversationList.SelectedItem is not AgentSessionItem item ||
            _agent is null || !_agent.IsRunning) return;
        try
        {
            _conversationTitle = item.DisplayTitle;
            SetWorkspace(ComposerTab, item.DisplayTitle);
            var snapshot = await _agent.ResumeSessionAsync(item.Path);
            RenderSnapshot(snapshot);
        }
        catch (Exception error) { ShowFriendlyError(error); }
    }

    private void RecentConversation_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => PromptText.Focus();

    private async void ConfirmVm_Click(object sender, RoutedEventArgs e)
    {
        if (_agent is null || !_agent.IsRunning) return;
        try
        {
            await _agent.RecordUserValidationAsync("用户在桌面端明确确认：VM 内打开、编译、运行及保存重开结果正常。");
            ConfirmVmButton.IsEnabled = false;
            SuccessText.Text = "已记录用户 VM 实机验收。";
            AppendSystemStatus("已记录：用户确认 VM 实机验证通过。");
        }
        catch (Exception error) { ShowFriendlyError(error); }
    }

    private void Mode_Changed(object sender, RoutedEventArgs e)
    {
        if (BaseSolutionPanel is null || PatchModeRadio is null) return;
        BaseSolutionPanel.Visibility = PatchModeRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BrowseBase_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "VisionMaster solution (*.sol)|*.sol|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) == true) BaseSolutionText.Text = dialog.FileName;
    }

    private async Task DetectEnvironmentAsync()
    {
        try
        {
            var environment = await Task.Run(_compiler.DetectEnvironment);
            EnvironmentText.Text = environment.Found
                ? $"VM {environment.Version} · {environment.VmRoot}"
                : $"未检测到 VM：{environment.ErrorCode}";
        }
        catch (Exception error) { EnvironmentText.Text = error.Message; }
    }

    private async void Detect_Click(object sender, RoutedEventArgs e) => await DetectEnvironmentAsync();
    private void ShowComposer_Click(object sender, RoutedEventArgs e) => SetWorkspace(ComposerTab, _conversationTitle);
    private void ShowArtifacts_Click(object sender, RoutedEventArgs e)
    {
        SetWorkspace(ArtifactsTab, "结果产物");
        ApplyArtifactFilter();
    }
    private void ShowSettings_Click(object sender, RoutedEventArgs e)
    {
        LoadSettingsForm();
        SetWorkspace(SettingsTab, "设置");
    }

    private void ShowDeveloperTools_Click(object sender, RoutedEventArgs e)
    {
        AdvancedDeterministicTools.Visibility = Visibility.Visible;
        AdvancedDeterministicTools.IsExpanded = true;
        SetWorkspace(ComposerTab, _conversationTitle);
        AdvancedDeterministicTools.BringIntoView();
    }

    private void SetWorkspace(TabItem tab, string title)
    {
        if (WorkspaceTabs is null) return;
        WorkspaceTabs.SelectedItem = tab;
        WorkspaceTitleText.Text = string.IsNullOrWhiteSpace(title) ? "新任务" : title;
        ChatNavButton.Background = tab == ComposerTab
            ? new SolidColorBrush(Color.FromRgb(41, 41, 41))
            : Brushes.Transparent;
        ArtifactsNavButton.Background = tab == ArtifactsTab
            ? new SolidColorBrush(Color.FromRgb(41, 41, 41))
            : Brushes.Transparent;
        SettingsNavButton.Background = tab == SettingsTab
            ? new SolidColorBrush(Color.FromRgb(41, 41, 41))
            : Brushes.Transparent;
    }

    private void BrowseSpec_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Requirement JSON (*.json)|*.json|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) == true) SpecPathText.Text = dialog.FileName;
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = OutputPathText.Text, ShowNewFolderButton = true };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) OutputPathText.Text = dialog.SelectedPath;
    }

    private async void Plan_Click(object sender, RoutedEventArgs e)
        => await ShowCoreAsync(() => _compiler.Plan(RequireFile(SpecPathText.Text, "Requirement")));
    private async void Build_Click(object sender, RoutedEventArgs e)
        => await ShowCoreAsync(() => BuildView(_compiler.Build(RequireFile(SpecPathText.Text, "Requirement"), RequireDirectory(OutputPathText.Text))));
    private async void Patch_Click(object sender, RoutedEventArgs e)
        => await ShowCoreAsync(() => BuildView(_compiler.Patch(RequireFile(BaseSolutionText.Text, "基底 SOL"), RequireFile(SpecPathText.Text, "Requirement"), RequireDirectory(OutputPathText.Text))));
    private async void Validate_Click(object sender, RoutedEventArgs e)
    {
        var file = BaseSolutionText.Text;
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "VisionMaster solution (*.sol)|*.sol" };
            if (dialog.ShowDialog(this) != true) return;
            file = dialog.FileName;
        }
        await ShowCoreAsync(() => _compiler.ValidateSolution(file));
    }

    private async Task ShowCoreAsync(Func<object> operation)
    {
        ActionPanel.IsEnabled = false;
        try { ResultText.Text = JsonSerializer.Serialize(await Task.Run(operation), JsonDefaults.Options); }
        catch (Exception error) { ResultText.Text = JsonSerializer.Serialize(new { ok = false, message = error.Message }, JsonDefaults.Options); }
        finally { ActionPanel.IsEnabled = true; }
    }

    private async void RefreshArtifacts_Click(object sender, RoutedEventArgs e) => await RefreshArtifactIndexAsync();
    private async Task RefreshArtifactIndexAsync()
    {
        ArtifactSummaryText.Text = "正在建立索引…";
        _artifacts = await Task.Run(() => _stateStore.RefreshArtifactIndex(_settings.OutputDirectory));
        ApplyArtifactFilter();
    }

    private void ArtifactSearch_Changed(object sender, TextChangedEventArgs e) => ApplyArtifactFilter();
    private void ApplyArtifactFilter()
    {
        if (ArtifactList is null) return;
        var query = ArtifactSearchText.Text.Trim();
        var filtered = string.IsNullOrWhiteSpace(query) ? _artifacts : _artifacts.Where(x => x.SearchText.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        ArtifactList.ItemsSource = filtered;
        ArtifactEmptyPanel.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ArtifactSummaryText.Text = $"已索引 {_artifacts.Count} 个产物，当前显示 {filtered.Count} 个";
    }

    private void ArtifactList_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OpenSelectedArtifact();
    private void OpenArtifact_Click(object sender, RoutedEventArgs e) => OpenSelectedArtifact();
    private void OpenSelectedArtifact()
    {
        if (ArtifactList.SelectedItem is ArtifactRecord artifact && File.Exists(artifact.FilePath))
            Process.Start(new ProcessStartInfo(artifact.FilePath) { UseShellExecute = true });
    }
    private void OpenArtifactFolder_Click(object sender, RoutedEventArgs e)
    {
        if (ArtifactList.SelectedItem is ArtifactRecord artifact)
            Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + artifact.FilePath + "\"") { UseShellExecute = true });
    }

    private void LoadSettingsForm()
    {
        SettingsOutputPathText.Text = _settings.OutputDirectory;
        SettingsEndpointText.Text = _settings.AiEndpoint;
        SettingsModelText.Text = _settings.AiModel;
        SettingsApiKeyBox.Password = _stateStore.UnprotectSecret(_settings.EncryptedApiKey);
        SettingsFilePathText.Text = _stateStore.SettingsFile;
        SelectCombo(SettingsProviderChoice, _settings.AiProvider);
        SelectCombo(SettingsThinkingChoice, _settings.ThinkingLevel);
    }

    private void BrowseSettingsOutput_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = SettingsOutputPathText.Text, ShowNewFolderButton = true };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) SettingsOutputPathText.Text = dialog.SelectedPath;
    }

    private async void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SettingsOutputPathText.Text) ||
            string.IsNullOrWhiteSpace(SettingsEndpointText.Text) ||
            string.IsNullOrWhiteSpace(SettingsModelText.Text) ||
            string.IsNullOrWhiteSpace(SettingsApiKeyBox.Password))
        {
            SettingsStatusText.Text = "输出目录、API 地址、模型和 API Key 都必须填写。";
            SettingsStatusText.Foreground = Brushes.Firebrick;
            return;
        }
        _settings = new DesktopSettings
        {
            Version = 2,
            OutputDirectory = Path.GetFullPath(SettingsOutputPathText.Text.Trim()),
            AiProvider = SelectedCombo(SettingsProviderChoice, "openai-responses"),
            AiEndpoint = SettingsEndpointText.Text.Trim(),
            AiModel = SettingsModelText.Text.Trim(),
            ThinkingLevel = SelectedCombo(SettingsThinkingChoice, "high"),
            EncryptedApiKey = _stateStore.ProtectSecret(SettingsApiKeyBox.Password),
            MaxRecentConversations = _settings.MaxRecentConversations
        };
        _stateStore.SaveSettings(_settings);
        OutputPathText.Text = _settings.OutputDirectory;
        await RefreshArtifactIndexAsync();
        SettingsStatusText.Text = "配置已加密保存，正在重启 Agent…";
        SettingsStatusText.Foreground = Brushes.DarkGray;
        await StartAgentAsync();
        SettingsStatusText.Text = _agent is { IsRunning: true } ? "Agent 已使用新配置连接。" : "Agent 连接失败，请查看主界面提示。";
        SettingsStatusText.Foreground = _agent is { IsRunning: true } ? Brushes.SeaGreen : Brushes.Firebrick;
    }

    private void OpenSettingsFile_Click(object sender, RoutedEventArgs e)
    {
        _stateStore.SaveSettings(_settings);
        Process.Start(new ProcessStartInfo("notepad.exe", "\"" + _stateStore.SettingsFile + "\"") { UseShellExecute = true });
    }
    private void OpenStateDirectory_Click(object sender, RoutedEventArgs e) => OpenDirectory(_stateStore.StateDirectory);

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            _stateStore.SaveSettings(_settings);
            _runCancellation?.Cancel();
            var agent = Interlocked.Exchange(ref _agent, null);
            if (agent is not null)
            {
                agent.EventReceived -= Agent_EventReceived;
                agent.DiagnosticReceived -= Agent_DiagnosticReceived;
                agent.Terminate();
            }
        }
        catch { }
    }

    private void OpenConversationFolder_Click(object sender, RoutedEventArgs e)
        => OpenDirectory(_lastSolution is null ? null : Path.GetDirectoryName(_lastSolution));
    private void CopyResultPath_Click(object sender, RoutedEventArgs e) { if (_lastSolution is not null) Clipboard.SetText(_lastSolution); }
    private void OpenResultFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_lastSolution is not null && File.Exists(_lastSolution))
            Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + _lastSolution + "\"") { UseShellExecute = true });
    }
    private void OpenDependencies_Click(object sender, RoutedEventArgs e) => OpenDirectory(_lastDependencyDirectory);

    private void ShowFriendlyError(Exception error)
    {
        var code = error is AgentClientException agent ? agent.Code : "UNEXPECTED_ERROR";
        FriendlyStatus.Text = $"无法完成：{error.Message}（{code}）";
        FriendlyStatus.Foreground = Brushes.Firebrick;
        FriendlyStatus.Visibility = Visibility.Visible;
        ResultText.Text = JsonSerializer.Serialize(new { ok = false, error = code, message = error.Message }, JsonDefaults.Options);
    }

    private void ScrollTranscript() => Dispatcher.BeginInvoke(() => TranscriptScroll.ScrollToEnd());
    private static void OpenDirectory(string? directory)
    {
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + directory + "\"") { UseShellExecute = true });
    }

    private static string MessageText(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content)) return "";
        if (content.ValueKind == JsonValueKind.String) return content.GetString() ?? "";
        if (content.ValueKind != JsonValueKind.Array) return "";
        return string.Join("", content.EnumerateArray()
            .Where(item => String(item, "type") is "text" or "input_text" or "output_text")
            .Select(item => String(item, "text") ?? ""));
    }

    private static string StripTaskContext(string text)
    {
        var marker = "[/VM_TASK_CONTEXT]";
        var index = text.IndexOf(marker, StringComparison.Ordinal);
        return index >= 0 ? text[(index + marker.Length)..].Trim() : text;
    }

    private static string CompactTitle(string text)
    {
        var compact = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length > 34 ? compact[..34] + "…" : compact;
    }

    private static string FriendlyPhase(string phase) => phase switch
    {
        "draft" => "草稿",
        "requirement-validating" => "校验需求",
        "planned" => "已规划",
        "building" => "正在构建",
        "built" => "已构建",
        "offline-validated" => "离线验证通过",
        "user-validated" => "VM 验证通过",
        "blocked" => "需要处理",
        _ => phase
    };

    private static string? String(JsonElement value, string name)
        => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    private static int Int(JsonElement value, string name)
        => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var property) && property.TryGetInt32(out var number) ? number : 0;
    private static string SelectedCombo(System.Windows.Controls.ComboBox combo, string fallback)
        => (combo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? fallback;
    private static void SelectCombo(System.Windows.Controls.ComboBox combo, string value)
    {
        combo.SelectedItem = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(item =>
            string.Equals(item.Tag?.ToString(), value, StringComparison.OrdinalIgnoreCase)) ?? combo.Items[0];
    }
    private static string RequireFile(string path, string label)
        => !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? Path.GetFullPath(path) : throw new CompilerException("FILE_NOT_FOUND", label + " 文件不存在。");
    private static string RequireDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new CompilerException("OUTPUT_DIRECTORY_REQUIRED", "请选择输出目录。");
        Directory.CreateDirectory(path);
        return Path.GetFullPath(path);
    }
    private static BuildPresentation BuildView(BuildResult result)
    {
        var dependency = Path.Combine(result.TaskDirectory, "dependencies");
        return new BuildPresentation(result.TaskDirectory, result.SolutionFile, result.ReportFile, Directory.Exists(dependency) ? dependency : null);
    }

    private sealed record ToolCardVisual(Border Card, TextBlock Icon, TextBlock Status);
    private sealed record BuildPresentation(string TaskDirectory, string SolutionFile, string ReportFile, string? DependencyDirectory);
    private sealed record AgentSessionItem(string Path, string Id, DateTime Modified, string FirstMessage, string? Name)
    {
        public string DisplayTitle
        {
            get
            {
                var value = string.IsNullOrWhiteSpace(Name) ? StripTaskContext(FirstMessage) : Name!;
                value = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                return value.Length > 38 ? value[..38] + "…" : value;
            }
        }
        public string DisplayTime => Modified.ToLocalTime().ToString("MM-dd HH:mm");
        public static AgentSessionItem FromJson(JsonElement item) => new(
            String(item, "path") ?? "",
            String(item, "id") ?? "",
            item.TryGetProperty("modified", out var modified) && modified.TryGetDateTime(out var time) ? time : DateTime.MinValue,
            String(item, "firstMessage") ?? "新对话",
            String(item, "name"));
    }
}
