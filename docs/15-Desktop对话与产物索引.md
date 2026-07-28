# Desktop 对话、配置与产物索引

Desktop 采用与 Codex 对齐的任务工作台布局：

- 左侧固定显示“新任务”“当前任务”“结果产物”“设置”和最近任务，并可折叠；
- 顶部显示当前任务名、领域阶段、VM 环境和 Agent 重连入口；
- 中央是任务时间线，用户消息、Agent 回复、系统提示和确定性工具执行使用不同层级；
- 工具执行卡片在原位从“运行中”更新为“完成/失败”，不重复刷屏；
- 输入编辑器固定在底部，`Enter` 发送、`Shift+Enter` 换行；
- Create/Patch 模式、Pi Provider 状态和发送/停止操作都位于编辑器内；
- 空白任务提供 Create、Patch 和外部 DLL 三个可点击起始提示；
- 点击最近对话可恢复提示词、生成模式、执行状态及关联的 SOL/报告；
- “结果产物”可搜索 SOL、构建报告、脚本契约、Requirement、DLL 部署文件和生成源码；
- 搜索范围包括文件名、路径，以及较小的 JSON、Markdown、C#、Python 文件内容。

常规用户只面对任务对话、结果产物和设置。Requirement 路径、确定性 Core 命令及原始诊断默认隐藏，可从“设置 → 开发者工具”临时打开。

## 本地状态文件

首次启动后，程序会在 `%LOCALAPPDATA%\VM Script Compiler` 创建：

| 文件 | 用途 |
|---|---|
| `desktop-settings.json` | 默认输出目录、AI Provider、API 地址、模型、DPAPI 加密后的 API Key、历史保留数量 |
| `recent-conversations.json` | 最近提示词、成功/失败状态和关联产物 |
| `artifact-index.json` | 输出目录中相关产物的可检索索引 |

配置页会显示配置文件的绝对路径，并提供“打开配置文件”和“打开数据目录”。

API Key 使用 Windows DPAPI 的 `CurrentUser` 作用域加密后写入配置文件，启动时自动解密回填。配置文件中不出现明文 Key，并且其他 Windows 用户无法解密该密文。Requirement、最近对话、产物索引和构建报告均不记录 Key。

## 索引更新

程序启动、生成成功、修改默认输出目录或点击“重新建立索引”时，会重新扫描当前输出目录。索引只记录产物元数据和用于本地搜索的文本，不修改结果文件。

## 无窗口自检

`--smoke-test` 会验证：

1. 配置文件保存与读取；
2. 最近对话保存与读取；
3. SOL、报告、生成源码的索引和内容搜索；
4. WPF 主窗口能够完成 XAML 组合。

`--ui-snapshot` 会在不启动 Agent、不显示窗口的情况下渲染主界面 PNG，用于检查布局、主题和控件模板。输出路径由 `VM_SCRIPT_DESKTOP_SNAPSHOT` 指定；`VM_SCRIPT_DESKTOP_SNAPSHOT_TAB` 可设为 `composer`、`artifacts` 或 `settings`，`VM_SCRIPT_DESKTOP_SNAPSHOT_PREVIEW=conversation` 可渲染带消息和工具卡片的对话态。
