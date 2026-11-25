using System.Text;
using VmScriptCompiler.Core;
using Xunit;

namespace VmScriptCompiler.Core.Tests;

public sealed class FrontendDocumentTranslationTests
{
    [Fact]
    public void TranslatesVisibleTextInEmbeddedSubwindowAndPreservesBindingAndTerminator()
    {
        var nested = "<SubRoot><Label Text=\"功能开关\" BindingPath=\"流程1.图像源1\" /></SubRoot>";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(nested));
        var original = Encoding.UTF8.GetBytes($"<FrontendWindow Header=\"相机调试\"><SubWindow Document=\"{encoded}\" /></FrontendWindow>\0");
        var path = "SolutionFile/UiParamData/_70000+100+solution";
        var items = new List<TranslationItem>();
        Assert.True(FrontendDocumentTranslation.Read(path, original, items));
        Assert.Equal(["相机调试", "功能开关"], items.Select(x => x.Source));

        var values = new Dictionary<string, string>
        {
            [items[0].Id] = "Camera Setup",
            [items[1].Id] = "Features"
        };
        var terms = new Dictionary<string, string> { ["流程1"] = "Process1", ["图像源1"] = "ImageSource1" };
        var rewritten = FrontendDocumentTranslation.Rewrite(path, original, values, terms);
        Assert.Equal(0, rewritten[^1]);
        var translated = new List<TranslationItem>();
        Assert.True(FrontendDocumentTranslation.Read(path, rewritten, translated));
        Assert.Equal(["Camera Setup", "Features"], translated.Select(x => x.Source));
        var xml = Encoding.UTF8.GetString(rewritten).TrimEnd('\0');
        var document = new System.Xml.XmlDocument();
        document.LoadXml(xml);
        var embedded = Encoding.UTF8.GetString(Convert.FromBase64String(document.DocumentElement!.FirstChild!.Attributes!["Document"]!.Value));
        Assert.Contains("BindingPath=\"Process1.ImageSource1\"", embedded);
    }
}
