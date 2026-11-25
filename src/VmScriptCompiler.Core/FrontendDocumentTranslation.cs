using System.Text;
using System.Xml;

namespace VmScriptCompiler.Core;

internal static class FrontendDocumentTranslation
{
    private static readonly HashSet<string> CaptionAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Header", "Text", "Format", "CommandButtonContent", "ToolTip", "Title", "Content"
    };

    public static bool Read(string path, byte[] data, List<TranslationItem> items)
    {
        try
        {
            var text = Encoding.UTF8.GetString(data).TrimEnd('\0');
            var index = 0;
            Visit(text, path, items, null, null, ref index);
            return true;
        }
        catch (Exception error) when (error is XmlException or FormatException or DecoderFallbackException)
        {
            return false;
        }
    }

    public static byte[] Rewrite(string path, byte[] data, IReadOnlyDictionary<string, string> values,
        IReadOnlyDictionary<string, string> terms)
    {
        var text = Encoding.UTF8.GetString(data);
        var content = text.TrimEnd('\0');
        var terminators = text.Length - content.Length;
        var index = 0;
        var rewritten = Visit(content, path, null, values, terms, ref index);
        return Encoding.UTF8.GetBytes(rewritten + new string('\0', terminators));
    }

    private static string Visit(string xml, string path, List<TranslationItem>? items,
        IReadOnlyDictionary<string, string>? values, IReadOnlyDictionary<string, string>? terms, ref int index)
    {
        if (terms is not null) xml = ModuleFrameCompatibility.ReplaceTerms(xml, terms);
        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml(xml);
        foreach (XmlElement element in document.SelectNodes("//*")!)
        {
            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (CaptionAttributes.Contains(attribute.LocalName) && !string.IsNullOrWhiteSpace(attribute.Value))
                {
                    var id = $"frontend:{path}:{index++}";
                    items?.Add(new TranslationItem(id, "frontend", attribute.Value, path));
                    if (values is not null && values.TryGetValue(id, out var translated)) attribute.Value = translated;
                }
                if (!attribute.LocalName.Equals("Document", StringComparison.Ordinal)) continue;
                var nested = Encoding.UTF8.GetString(Convert.FromBase64String(attribute.Value));
                var rewritten = Visit(nested, path, items, values, terms, ref index);
                if (values is not null) attribute.Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(rewritten));
            }
        }
        return document.OuterXml;
    }
}
