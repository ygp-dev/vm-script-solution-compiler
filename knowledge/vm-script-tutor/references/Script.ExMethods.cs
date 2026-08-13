public ImageData MatToImageData(Mat matImage)
{
    ImageData imgOut = new ImageData();
    byte[] buffer = new Byte[matImage.Width * matImage.Height * matImage.Channels()];
    Marshal.Copy(matImage.Ptr(0), buffer, 0, buffer.Length);

    if (1 == matImage.Channels())
    {
        imgOut.Buffer = buffer;
        imgOut.Width = matImage.Width;
        imgOut.Height = matImage.Height;
        imgOut.PixelFormat = ImagePixelFormate.MONO8;
    }
    else if (3 == matImage.Channels())
    {
        //交换R与B通道
        for (int i = 0; i < buffer.Length - 2; i += 3)
        {
            byte temp = buffer[i];
            buffer[i] = buffer[i + 2];
            buffer[i + 2] = temp;
        }
        imgOut.Buffer = buffer;
        imgOut.Width = matImage.Width;
        imgOut.Height = matImage.Height;
        imgOut.PixelFormat = ImagePixelFormate.RGB24;
    }
    return imgOut;
}

public Mat ImageDataToMat(ImageData img)
{
    Mat matImage = null;
    if (ImagePixelFormate.MONO8 == img.PixelFormat)
    {
        matImage = Mat.Zeros(img.Height, img.Width, MatType.CV_8UC1);
        Marshal.Copy(img.Buffer, 0, matImage.Ptr(0), img.Buffer.Length);
    }
    else if (ImagePixelFormate.RGB24 == img.PixelFormat)
    {
        matImage = Mat.Zeros(img.Height, img.Width, MatType.CV_8UC3);
        Marshal.Copy(img.Buffer, 0, matImage.Ptr(0), img.Buffer.Length);
        Cv2.CvtColor(matImage, matImage, ColorConversionCodes.RGB2BGR);
    }
    else
    {
        // 不支持的像素格式：返回空 Mat（先分配后使用，避免泄漏）
        matImage = new Mat();
    }
    return matImage;
}

/// <summary>
/// Bitmap 转 ImageData
/// </summary>
/// <param name="bmpInputImg">输入 Bitmap 图像</param>
/// <returns>VM ImageData 对象</returns>
public ImageData BitmapToImageData(Bitmap bmpInputImg)
{
    ImageData imageData = new ImageData();
    System.Drawing.Imaging.PixelFormat bitPixelFormat = bmpInputImg.PixelFormat;
    BitmapData bmData = bmpInputImg.LockBits(new Rectangle(0, 0, bmpInputImg.Width, bmpInputImg.Height), ImageLockMode.ReadOnly, bitPixelFormat);

    if (bitPixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
    {
        int bitmapDataSize = bmData.Stride * bmData.Height;
        int offset = bmData.Stride - bmData.Width;
        int imageBaseDataSize = bmData.Width * bmData.Height;
        byte[] bitImageBufferBytes = new byte[bitmapDataSize];
        byte[] imageBaseDataBufferBytes = new byte[imageBaseDataSize];
        Marshal.Copy(bmData.Scan0, bitImageBufferBytes, 0, bitmapDataSize);
        int bitmapIndex = 0;
        int imageBaseDataIndex = 0;
        for (int i = 0; i < bmData.Height; i++)
        {
            for (int j = 0; j < bmData.Width; j++)
            {
                imageBaseDataBufferBytes[imageBaseDataIndex++] = bitImageBufferBytes[bitmapIndex++];
            }
            bitmapIndex += offset;
        }
        imageData.Height = bmpInputImg.Height;
        imageData.Width = bmpInputImg.Width;
        imageData.PixelFormat = ImagePixelFormate.MONO8;
        imageData.Buffer = new byte[imageBaseDataSize];
        Array.Copy(imageBaseDataBufferBytes, imageData.Buffer, imageBaseDataSize);
    }
    else if (bitPixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
    {
        int bitmapDataSize = bmData.Stride * bmData.Height;
        int offset = bmData.Stride - bmData.Width * 3;
        int imageBaseDataSize = bmData.Width * bmData.Height * 3;
        byte[] bitImageBufferBytes = new byte[bitmapDataSize];
        byte[] imageBaseDataBufferBytes = new byte[imageBaseDataSize];
        Marshal.Copy(bmData.Scan0, bitImageBufferBytes, 0, bitmapDataSize);
        int bitmapIndex = 0;
        int imageBaseDataIndex = 0;
        for (int i = 0; i < bmData.Height; i++)
        {
            for (int j = 0; j < bmData.Width; j++)
            {
                imageBaseDataBufferBytes[imageBaseDataIndex++] = bitImageBufferBytes[bitmapIndex + 2];
                imageBaseDataBufferBytes[imageBaseDataIndex++] = bitImageBufferBytes[bitmapIndex + 1];
                imageBaseDataBufferBytes[imageBaseDataIndex++] = bitImageBufferBytes[bitmapIndex];
                bitmapIndex += 3;
            }
            bitmapIndex += offset;
        }
        imageData.Height = bmpInputImg.Height;
        imageData.Width = bmpInputImg.Width;
        imageData.PixelFormat = ImagePixelFormate.RGB24;
        imageData.Buffer = new byte[imageBaseDataSize];
        Array.Copy(imageBaseDataBufferBytes, imageData.Buffer, imageBaseDataSize);
    }
    bmpInputImg.UnlockBits(bmData);
    return imageData;
}

/// <summary>
/// ImageData 转 Bitmap
/// </summary>
/// <param name="imagedata">VM ImageData 对象</param>
/// <returns>Bitmap 图像</returns>
public Bitmap ImageDataToBitmap(ImageData imagedata)
{
    Bitmap bmpOutputImg = null;
    byte[] buffer = new byte[imagedata.Buffer.Length];
    Array.Copy(imagedata.Buffer, buffer, buffer.Length);

    if (ImagePixelFormate.MONO8 == imagedata.PixelFormat)
    {
        int imageWidth = Convert.ToInt32(imagedata.Width);
        int imageHeight = Convert.ToInt32(imagedata.Height);
        System.Drawing.Imaging.PixelFormat bitMaPixelFormat = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
        bmpOutputImg = new Bitmap(imageWidth, imageHeight, bitMaPixelFormat);
        int offset = imageWidth % 4 != 0 ? (4 - imageWidth % 4) : 0;
        int stride = imageWidth + offset;
        int bitmapBytesLength = stride * imageHeight;
        byte[] bitmapDataBytes = new byte[bitmapBytesLength];
        for (int i = 0; i < imageHeight; i++)
        {
            for (int j = 0; j < stride; j++)
            {
                int bitIndex = i * stride + j;
                int mvdIndex = i * imageWidth + j;
                if (j >= imageWidth)
                {
                    bitmapDataBytes[bitIndex] = 0;
                }
                else
                {
                    bitmapDataBytes[bitIndex] = buffer[mvdIndex];
                }
            }
        }
        BitmapData bitmapData = bmpOutputImg.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.WriteOnly, bitMaPixelFormat);
        IntPtr imageBufferPtr = bitmapData.Scan0;
        Marshal.Copy(bitmapDataBytes, 0, imageBufferPtr, bitmapBytesLength);
        bmpOutputImg.UnlockBits(bitmapData);

        var colorPalettes = bmpOutputImg.Palette;
        for (int j = 0; j < 256; j++)
        {
            colorPalettes.Entries[j] = Color.FromArgb(j, j, j);
        }
        bmpOutputImg.Palette = colorPalettes;
    }
    else if (ImagePixelFormate.RGB24 == imagedata.PixelFormat)
    {
        int imageWidth = Convert.ToInt32(imagedata.Width);
        int imageHeight = Convert.ToInt32(imagedata.Height);
        System.Drawing.Imaging.PixelFormat bitMaPixelFormat = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
        bmpOutputImg = new Bitmap(imageWidth, imageHeight, bitMaPixelFormat);
        int offset = imageWidth % 4 != 0 ? (4 - (imageWidth * 3) % 4) : 0;
        int stride = imageWidth * 3 + offset;
        int bitmapBytesLength = stride * imageHeight;
        byte[] bitmapDataBytes = new byte[bitmapBytesLength];
        for (int i = 0; i < imageHeight; i++)
        {
            for (int j = 0; j < imageWidth; j++)
            {
                int mvdIndex = i * imageWidth * 3 + j * 3;
                int bitIndex = i * stride + j * 3;
                bitmapDataBytes[bitIndex] = buffer[mvdIndex + 2];
                bitmapDataBytes[bitIndex + 1] = buffer[mvdIndex + 1];
                bitmapDataBytes[bitIndex + 2] = buffer[mvdIndex];
            }
            for (int k = 0; k < offset; k++)
            {
                bitmapDataBytes[i * stride + imageWidth * 3 + k] = 0;
            }
        }
        BitmapData bitmapData = bmpOutputImg.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.WriteOnly, bitMaPixelFormat);
        IntPtr imageBufferPtr = bitmapData.Scan0;
        Marshal.Copy(bitmapDataBytes, 0, imageBufferPtr, bitmapBytesLength);
        bmpOutputImg.UnlockBits(bitmapData);
    }
    return bmpOutputImg;
}