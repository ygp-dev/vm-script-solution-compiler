using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace VmScriptCompiler.Core;

internal static class VmSolArchiveWriter
{
    // VM 4.4 writes SOL entries with its bundled SharpZipLib at Deflate level 0.
    // Keep the source entry order and use that same implementation for translated output.
    public static void Repack(string source, string translated)
    {
        var vm = new EnvironmentDetector().DetectInstalled();
        var library = vm.VmRoot is null ? null : Path.Combine(vm.VmRoot, "Applications", "myLibs", "VM.Compress.dll");
        if (library is null || !File.Exists(library))
            throw new CompilerException("VM_COMPRESS_NOT_FOUND", "未找到 VM 4.4 自带的 VM.Compress.dll，无法生成可导入的 SOL。");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var assembly = Assembly.LoadFrom(library);
        var outputType = assembly.GetType("ICSharpCode.SharpZipLib.Zip.ZipOutputStream", true)!;
        var entryType = assembly.GetType("ICSharpCode.SharpZipLib.Zip.ZipEntry", true)!;
        var setLevel = outputType.GetMethod("SetLevel", [typeof(int)])!;
        var putNext = outputType.GetMethod("PutNextEntry", [entryType])!;
        var closeEntry = outputType.GetMethod("CloseEntry", Type.EmptyTypes)!;
        var finish = outputType.GetMethod("Finish", Type.EmptyTypes)!;
        var temporary = translated + ".vmzip.tmp";
        try
        {
            using (var original = ZipFile.OpenRead(source))
            using (var updated = ZipFile.OpenRead(translated))
            using (var file = File.Create(temporary))
            using (var zip = (Stream)Activator.CreateInstance(outputType, file)!)
            {
                setLevel.Invoke(zip, [0]);
                foreach (var entry in original.Entries)
                {
                    var replacement = updated.GetEntry(entry.FullName)
                        ?? throw new CompilerException("TRANSLATION_ENTRY_MISSING", "翻译结果缺少原始条目: " + entry.FullName);
                    var vmEntry = Activator.CreateInstance(entryType, entry.FullName)!;
                    putNext.Invoke(zip, [vmEntry]);
                    using (var input = replacement.Open()) input.CopyTo(zip);
                    closeEntry.Invoke(zip, null);
                }
                if (original.Entries.Count != updated.Entries.Count)
                    throw new CompilerException("TRANSLATION_ENTRY_COUNT_CHANGED", "翻译结果的 SOL 条目数量发生变化。");
                finish.Invoke(zip, null);
            }
            File.Move(temporary, translated, true);
        }
        catch (TargetInvocationException error)
        {
            throw new CompilerException("VM_COMPRESS_FAILED", "VM.Compress.dll 压缩失败: " + (error.InnerException?.Message ?? error.Message));
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
