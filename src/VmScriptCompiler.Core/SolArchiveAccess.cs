using System.IO.Compression;

namespace VmScriptCompiler.Core;

internal static class SolArchiveAccess
{
    public static ZipArchive OpenForUpdate(string path)
    {
        for (var attempt = 0; ; attempt++)
        {
            try { return ZipFile.Open(path, ZipArchiveMode.Update); }
            catch (IOException error) when ((error.HResult & 0xffff) is 32 or 33 && attempt < 20)
            {
                Thread.Sleep(100);
            }
        }
    }
}
