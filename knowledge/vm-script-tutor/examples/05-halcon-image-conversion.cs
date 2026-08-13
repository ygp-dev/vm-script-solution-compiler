/// <summary>
/// VisionMaster 图像处理脚本示例 - Halcon 图像与 ImageData 互转
/// 使用 HalconDotNet 库进行图像格式转换
/// 注意：本示例假定 VM 4.4 及以后，ImageData 高度字段名为 Height。
///      VM 4.3 及之前请把 ImageData 的 .Height 替换为 .Heigth（历史拼写错误）。
/// </summary>
using System;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Script.Methods;
using HalconDotNet;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;
    string errorStatus = string.Empty;

    /// <summary>
    /// 初始化函数
    /// </summary>
    public void Init()
    {
        processCount = 0;
        errorStatus = string.Empty;
    }

    /// <summary>
    /// 流程执行函数 - Halcon 图像转 ImageData 并写回 VM 输出
    /// </summary>
    public bool Process()
    {
        HObject halconImage = null;
        try
        {
            errorStatus = string.Empty;

            // 示例：构造一个 Halcon 灰度图像（实际使用时替换为算法生成的图像）
            HOperatorSet.GenImageConst(out halconImage, "byte", 640, 480);

            // Halcon → ImageData
            ImageData imgOut = HalconImageToImageData(halconImage);
            if (imgOut == null)
            {
                errorStatus = "Halcon 图像转换失败";
                return false;
            }

            // 直接赋值设置输出
            outputImage = imgOut;

            processCount++;
            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "Halcon 图像处理异常：" + ex.Message;
            return false;
        }
        finally
        {
            if (halconImage != null) halconImage.Dispose();
        }
    }

    /// <summary>
    /// Halcon HObject 图像 → VM ImageData
    /// </summary>
    /// <param name="hImageObj">Halcon 图像对象（调用方负责释放）</param>
    /// <returns>VM ImageData；失败返回 null 并设置 errorStatus</returns>
    private ImageData HalconImageToImageData(HObject hImageObj)
    {
        if (hImageObj == null)
        {
            errorStatus = "输入 HObject 为 null";
            return null;
        }

        try
        {
            ImageData imageData = new ImageData();

            // 获取对象类型
            HTuple objClass = new HTuple();
            HOperatorSet.GetObjClass(hImageObj, out objClass);
            if (!objClass.S.Equals("image"))
            {
                errorStatus = "HObject 非图像类型对象";
                return null;
            }

            // 获取图像类型（位深度）
            HTuple imageType = new HTuple();
            HOperatorSet.GetImageType(hImageObj, out imageType);
            if (!imageType.S.Equals("byte"))
            {
                errorStatus = "不支持 8bit 以外的位深度图像";
                return null;
            }

            // 获取通道数
            HTuple channels = new HTuple();
            HOperatorSet.CountChannels(hImageObj, out channels);

            HTuple imageWidth = new HTuple();
            HTuple imageHeight = new HTuple();

            if (channels.I == 1)
            {
                // 单通道灰度图
                HTuple imagePointer = new HTuple();
                HOperatorSet.GetImagePointer1(hImageObj, out imagePointer, out imageType, out imageWidth, out imageHeight);
                imageData.Width = imageWidth.I;
                imageData.Height = imageHeight.I;
                imageData.PixelFormat = ImagePixelFormate.MONO8;
                imageData.Buffer = new byte[imageWidth.I * imageHeight.I];
                Marshal.Copy(imagePointer.IP, imageData.Buffer, 0, imageWidth.I * imageHeight.I);
            }
            else if (channels.I == 3)
            {
                // 三通道彩色图
                HTuple redChannel = new HTuple();
                HTuple greenChannel = new HTuple();
                HTuple blueChannel = new HTuple();
                HOperatorSet.GetImagePointer3(hImageObj, out redChannel, out greenChannel, out blueChannel,
                    out imageType, out imageWidth, out imageHeight);

                imageData.Width = imageWidth.I;
                imageData.Height = imageHeight.I;
                imageData.PixelFormat = ImagePixelFormate.RGB24;

                int pixelCount = imageWidth.I * imageHeight.I;
                byte[] imageRedBuffer = new byte[pixelCount];
                byte[] imageGreenBuffer = new byte[pixelCount];
                byte[] imageBlueBuffer = new byte[pixelCount];

                Marshal.Copy(redChannel.IP, imageRedBuffer, 0, pixelCount);
                Marshal.Copy(greenChannel.IP, imageGreenBuffer, 0, pixelCount);
                Marshal.Copy(blueChannel.IP, imageBlueBuffer, 0, pixelCount);

                // 通道交错排列：RGBRGB...
                imageData.Buffer = new byte[pixelCount * 3];
                for (int row = 0; row < imageHeight.I; row++)
                {
                    for (int col = 0; col < imageWidth.I; col++)
                    {
                        imageData.Buffer[row * imageWidth.I * 3 + col * 3 + 0] = imageRedBuffer[row * imageWidth.I + col];
                        imageData.Buffer[row * imageWidth.I * 3 + col * 3 + 1] = imageGreenBuffer[row * imageWidth.I + col];
                        imageData.Buffer[row * imageWidth.I * 3 + col * 3 + 2] = imageBlueBuffer[row * imageWidth.I + col];
                    }
                }
            }
            else
            {
                errorStatus = "不支持单通道、三通道以外的图像";
                return null;
            }

            return imageData;
        }
        catch (Exception ex)
        {
            errorStatus = "HalconImageToImageData 异常：" + ex.Message;
            return null;
        }
    }

    /// <summary>
    /// VM ImageData → Halcon HObject 图像
    /// </summary>
    /// <param name="image">VM ImageData</param>
    /// <returns>Halcon 图像对象；调用方负责释放，失败返回 null 并设置 errorStatus</returns>
    private HObject ImageDataToHalconImage(ImageData image)
    {
        if (image == null || image.Buffer == null || image.Width <= 0 || image.Height <= 0)
        {
            errorStatus = "输入 ImageData 无效";
            return null;
        }

        IntPtr imagePointer = IntPtr.Zero;
        IntPtr redChannel = IntPtr.Zero;
        IntPtr greenChannel = IntPtr.Zero;
        IntPtr blueChannel = IntPtr.Zero;

        try
        {
            HObject imageObj = new HObject();
            HTuple width = image.Width;
            HTuple height = image.Height;

            if (image.PixelFormat == ImagePixelFormate.MONO8)
            {
                imagePointer = Marshal.AllocHGlobal(image.Buffer.Length);
                Marshal.Copy(image.Buffer, 0, imagePointer, image.Buffer.Length);
                HOperatorSet.GenImage1(out imageObj, "byte", width, height, imagePointer);
            }
            else if (image.PixelFormat == ImagePixelFormate.RGB24)
            {
                int pixelCount = image.Buffer.Length / 3;
                byte[] imageRedBuffer = new byte[pixelCount];
                byte[] imageGreenBuffer = new byte[pixelCount];
                byte[] imageBlueBuffer = new byte[pixelCount];

                int imgWidth = image.Width;
                int imgHeight = image.Height;
                for (int i = 0; i < imgHeight; i++)
                {
                    for (int j = 0; j < imgWidth; j++)
                    {
                        imageRedBuffer[i * imgWidth + j]   = image.Buffer[i * imgWidth * 3 + j * 3 + 0];
                        imageGreenBuffer[i * imgWidth + j] = image.Buffer[i * imgWidth * 3 + j * 3 + 1];
                        imageBlueBuffer[i * imgWidth + j]  = image.Buffer[i * imgWidth * 3 + j * 3 + 2];
                    }
                }

                redChannel   = Marshal.AllocHGlobal(imageRedBuffer.Length);
                greenChannel = Marshal.AllocHGlobal(imageGreenBuffer.Length);
                blueChannel  = Marshal.AllocHGlobal(imageBlueBuffer.Length);
                Marshal.Copy(imageRedBuffer,   0, redChannel,   imageRedBuffer.Length);
                Marshal.Copy(imageGreenBuffer, 0, greenChannel, imageGreenBuffer.Length);
                Marshal.Copy(imageBlueBuffer,  0, blueChannel,  imageBlueBuffer.Length);
                HOperatorSet.GenImage3(out imageObj, "byte", width, height, redChannel, greenChannel, blueChannel);
            }
            else
            {
                errorStatus = "不支持的像素格式";
                if (imageObj != null) imageObj.Dispose();
                return null;
            }

            return imageObj;
        }
        catch (Exception ex)
        {
            errorStatus = "ImageDataToHalconImage 异常：" + ex.Message;
            return null;
        }
        finally
        {
            // Halcon GenImage1/GenImage3 会拷贝数据，分配的非托管内存用完即释放
            if (imagePointer != IntPtr.Zero) Marshal.FreeHGlobal(imagePointer);
            if (redChannel   != IntPtr.Zero) Marshal.FreeHGlobal(redChannel);
            if (greenChannel != IntPtr.Zero) Marshal.FreeHGlobal(greenChannel);
            if (blueChannel  != IntPtr.Zero) Marshal.FreeHGlobal(blueChannel);
        }
    }
}
