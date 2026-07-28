using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Script.Methods;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    public struct HSLImg
    {
        public double h;
        public double s;
        public double l;
    }

    int processCount;

    public void Init()
    {
        processCount = 0;
    }

    public bool Process()
    {
        ImageData image = new ImageData();
        GetImageValue("Img", ref image);
        SetImageValue("outImg", Convert(image));
        return true;
    }

    public ImageData Convert(ImageData image)
    {
        int nImageHeight = image.Heigth;
        int nImageWidth = image.Width;

        byte[] hPtr = new byte[image.Buffer.Length / 3];
        byte[] sPtr = new byte[image.Buffer.Length / 3];
        byte[] lPtr = new byte[image.Buffer.Length / 3];

        if (image.PixelFormat == ImagePixelFormate.RGB24)
        {
            Parallel.For(0, nImageHeight, i =>
            {
                for (int j = 0; j < nImageWidth; j++)
                {
                    int index = (i * nImageWidth + j) * 3;

                    byte r = image.Buffer[index];
                    byte g = image.Buffer[index + 1];
                    byte b = image.Buffer[index + 2];

                    HSLImg hsl = RGBToHSL(Color.FromArgb(r, g, b));

                    int hslIndex = index / 3;
                    hPtr[hslIndex] = (byte)(hsl.h / 360 * 255);
                    sPtr[hslIndex] = (byte)(hsl.s * 255);
                    lPtr[hslIndex] = (byte)(hsl.l * 255);
                }
            });
        }

        ImageData hImage = new ImageData { Width = nImageWidth, Heigth = nImageHeight, PixelFormat = ImagePixelFormate.MONO8, Buffer = hPtr };
        ImageData sImage = new ImageData { Width = nImageWidth, Heigth = nImageHeight, PixelFormat = ImagePixelFormate.MONO8, Buffer = sPtr };
        ImageData lImage = new ImageData { Width = nImageWidth, Heigth = nImageHeight, PixelFormat = ImagePixelFormate.MONO8, Buffer = lPtr };

        return sImage;
    }

    static HSLImg RGBToHSL(Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(Math.Max(r, g), b);
        double min = Math.Min(Math.Min(r, g), b);
        double delta = max - min;

        double h = 0;
        if (delta == 0)
        {
            h = 0;
        }
        else if (max == r)
        {
            h = ((g - b) / delta) % 6;
        }
        else if (max == g)
        {
            h = (b - r) / delta + 2;
        }
        else if (max == b)
        {
            h = (r - g) / delta + 4;
        }
        h *= 60;
        if (h < 0)
        {
            h += 360;
        }

        double l = (max + min) / 2;

        double s = 0;
        if (delta != 0)
        {
            s = delta / (1 - Math.Abs(2 * l - 1));
        }

        return new HSLImg { h = h, s = s, l = l };
    }
}
