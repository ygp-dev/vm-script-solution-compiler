using System.Windows;

namespace VmScriptCompiler.Desktop;

/// <summary>One row in the global search list: either a past conversation or an indexed artifact.</summary>
internal sealed record SearchResult(
    string Kind,
    string Title,
    string Detail,
    DateTime TimestampUtc,
    string SearchText,
    string? FilePath,
    string? DirectoryPath)
{
    public string DisplayTime => TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

/// <summary>
/// Pure query logic behind the artifact list and the global search box. Kept out of the window so the
/// matching and ordering rules can be exercised without a WPF dispatcher.
/// </summary>
internal static class ArtifactSearch
{
    /// <summary>Upper bound on rendered global-search hits; a broad query must not stall the list.</summary>
    public const int GlobalResultLimit = 200;

    public static IReadOnlyList<ArtifactRecord> FilterArtifacts(IReadOnlyList<ArtifactRecord> artifacts, string? query)
    {
        var trimmed = query?.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            ? artifacts
            : artifacts.Where(x => Matches(x.SearchText, trimmed)).ToArray();
    }

    public static string DescribeArtifactFilter(int total, int shown) => $"已索引 {total} 个产物，当前显示 {shown} 个";

    /// <summary>Merges sessions and artifacts into one newest-first result set, then applies the query.</summary>
    public static (IReadOnlyList<SearchResult> Filtered, int Total) SearchEverything(
        IEnumerable<ConversationRecord> sessions,
        IEnumerable<ArtifactRecord> artifacts,
        string? query)
    {
        var all = sessions
            .Select(session => new SearchResult(
                "Session",
                session.DisplayTitle,
                session.Prompt,
                session.TimestampUtc,
                string.Join(" ", session.DisplayTitle, session.Prompt, session.TaskDirectory ?? ""),
                null,
                session.TaskDirectory))
            .Concat(artifacts.Select(artifact => new SearchResult(
                artifact.Kind,
                artifact.DisplayTitle,
                artifact.DisplayDetail,
                artifact.ModifiedUtc,
                artifact.SearchText,
                artifact.FilePath,
                null)))
            .OrderByDescending(result => result.TimestampUtc)
            .ToArray();

        var trimmed = query?.Trim();
        var filtered = string.IsNullOrWhiteSpace(trimmed)
            ? all
            : all.Where(result => Matches(result.SearchText, trimmed)).Take(GlobalResultLimit).ToArray();
        return (filtered, all.Length);
    }

    public static string DescribeGlobalSearch(string? query, int total, int shown) =>
        string.IsNullOrWhiteSpace(query?.Trim())
            ? $"搜索 Sessions、提示词、SOL、报告和生成脚本；共 {total} 条"
            : $"搜索“{query!.Trim()}”：找到 {shown} 条结果";

    public static Visibility EmptyPanelVisibility(int shown) => shown == 0 ? Visibility.Visible : Visibility.Collapsed;

    private static bool Matches(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
