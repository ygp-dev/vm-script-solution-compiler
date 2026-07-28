using System.Text.RegularExpressions;

namespace VmScriptCompiler.Core;

internal static partial class Vm44SourceCompatibility
{
    private const string BitmapConverterName = "Vm44ImageDataFromBitmap";

    public static string Normalize(ScriptRequirement script, string source)
    {
        if (script.Carrier != "csharp-module"
            || !script.Dependencies.Any(x => x.Kind == "dotnet-assembly" && x.Name.Equals("System.Drawing.dll", StringComparison.OrdinalIgnoreCase)))
            return source;

        var normalized = ImageDataBitmapConstructor().Replace(source, match => BitmapConverterName + "(" + match.Groups["bitmap"].Value + ")");
        if (ReferenceEquals(normalized, source) || normalized == source || normalized.Contains("private static ImageData " + BitmapConverterName, StringComparison.Ordinal))
            return normalized;

        var classEnd = normalized.LastIndexOf('}');
        if (classEnd < 0) return normalized;
        return normalized.Insert(classEnd, BitmapConverterSource);
    }

    private const string BitmapConverterSource = """

    private static ImageData Vm44ImageDataFromBitmap(System.Drawing.Bitmap bitmap)
    {
        if (bitmap == null) throw new ArgumentNullException("bitmap");
        using (System.Drawing.Bitmap rgb = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
        {
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(rgb))
                graphics.DrawImageUnscaled(bitmap, 0, 0);

            System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(0, 0, rgb.Width, rgb.Height);
            System.Drawing.Imaging.BitmapData data = rgb.LockBits(rectangle, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            try
            {
                byte[] vmBuffer = new byte[rgb.Width * rgb.Height * 3];
                byte[] row = new byte[Math.Abs(data.Stride)];
                for (int y = 0; y < rgb.Height; y++)
                {
                    IntPtr rowAddress = IntPtr.Add(data.Scan0, y * data.Stride);
                    System.Runtime.InteropServices.Marshal.Copy(rowAddress, row, 0, row.Length);
                    for (int x = 0; x < rgb.Width; x++)
                    {
                        int sourceIndex = x * 3;
                        int targetIndex = (y * rgb.Width + x) * 3;
                        vmBuffer[targetIndex] = row[sourceIndex + 2];
                        vmBuffer[targetIndex + 1] = row[sourceIndex + 1];
                        vmBuffer[targetIndex + 2] = row[sourceIndex];
                    }
                }
                return new ImageData
                {
                    Buffer = vmBuffer,
                    Width = rgb.Width,
                    Height = rgb.Height,
                    PixelFormat = ImagePixelFormate.RGB24
                };
            }
            finally { rgb.UnlockBits(data); }
        }
    }
""";

    [GeneratedRegex(@"new\s+(?:Script\.Methods\.)?ImageData\s*\(\s*(?<bitmap>[A-Za-z_][A-Za-z0-9_]*)\s*\)")]
    private static partial Regex ImageDataBitmapConstructor();
}
