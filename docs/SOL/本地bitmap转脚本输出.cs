using Script.Methods;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;

    public void Init()
    {
        processCount = 0;
    }

    public bool Process()
    {
        try
        {
            string path = @"D:\User\Desktop\TestImage\RGB8\6.bmp";

            using (Bitmap bmp = new Bitmap(path))
            {
                ImageData img = BitmapToImageData(bmp);
                SetImageValue("outIMG", img);
            }

            return true;
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error: " + ex.Message);
            return false;
        }
    }

    public ImageData BitmapToImageData(Bitmap bmp)
    {
        PixelFormat fmt = bmp.PixelFormat;

        if (fmt != PixelFormat.Format8bppIndexed &&
            fmt != PixelFormat.Format24bppRgb)
        {
            throw new NotSupportedException("仅支持 8bit / 24bit Bitmap");
        }

        int bytesPerPixel = (fmt == PixelFormat.Format24bppRgb) ? 3 : 1;

        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        BitmapData bmData = bmp.LockBits(rect, ImageLockMode.ReadOnly, fmt);

        try
        {
            int width = bmData.Width;
            int height = bmData.Height;
            int stride = bmData.Stride;
            int dstSize = width * height * bytesPerPixel;

            byte[] dst = new byte[dstSize];

            unsafe
            {
                byte* srcPtr = (byte*)bmData.Scan0;
                int dstIndex = 0;

                for (int y = 0; y < height; y++)
                {
                    byte* row = srcPtr + y * stride;

                    if (bytesPerPixel == 1)
                    {
                        // 8bit：整行 copy（最快）
                        for (int x = 0; x < width; x++)
                        {
                            dst[dstIndex++] = row[x];
                        }
                    }
                    else
                    {
                        // 24bit：BGR → RGB
                        byte* pixel = row;

                        for (int x = 0; x < width; x++)
                        {
                            byte b = pixel[0];
                            byte g = pixel[1];
                            byte r = pixel[2];

                            dst[dstIndex++] = r;
                            dst[dstIndex++] = g;
                            dst[dstIndex++] = b;

                            pixel += 3;
                        }
                    }
                }
            }

            return new ImageData
            {
                Buffer = dst,
                Width = bmData.Width,
                Height = bmData.Height,
                PixelFormat = (bytesPerPixel == 1) ? ImagePixelFormate.MONO8 : ImagePixelFormate.RGB24
            };
        }
        finally
        {
            bmp.UnlockBits(bmData);
        }
    }

}
