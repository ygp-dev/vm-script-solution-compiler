/// <summary>
/// VisionMaster 图像处理脚本示例 - Canny 边缘检测
/// 使用 OpenCvSharp 库进行图像处理
/// 注意：本示例假定 VM 4.4 及以后，ImageData 高度字段名为 Height。
///      VM 4.3 及之前请把 ImageData 的 .Height 替换为 .Heigth（历史拼写错误）。
/// </summary>
using System;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Script.Methods;
using OpenCvSharp;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    // 全局字段
    int processCount;
    string errorStatus = string.Empty;
    private Mat edgeMat;

    /// <summary>
    /// 初始化函数
    /// </summary>
    public void Init()
    {
        processCount = 0;
        errorStatus = string.Empty;
        edgeMat = new Mat();
    }

    /// <summary>
    /// 流程执行函数 - Canny 边缘检测
    /// </summary>
    public bool Process()
    {
        Mat srcImage = null;
        Mat grayMat = null;
        Mat resultImage = null;
        try
        {
            errorStatus = string.Empty;

            // ========== 1. 直接赋值获取输入参数 ==========

            int threshold1 = threshold1In;
            int threshold2 = threshold2In;

            // 直接赋值获取输入图像
            ImageData imgIn = inputImage;

            if (imgIn.Buffer == null || imgIn.Width <= 0 || imgIn.Height <= 0)
            {
                errorStatus = "输入图像无效";
                return false;
            }

            // ========== 2. 转换为 OpenCvSharp Mat ==========

            srcImage = ImageDataToMat(imgIn);

            // ========== 3. Canny 边缘检测 ==========

            grayMat = new Mat();
            Cv2.CvtColor(srcImage, grayMat, ColorConversionCodes.BGR2GRAY);

            Cv2.Canny(grayMat, edgeMat, threshold1, threshold2);

            // ========== 4. 查找并绘制轮廓 ==========

            Point[][] contours = new Point[0][];
            HierarchyIndex[] hierarchy = new HierarchyIndex[0];
            Cv2.FindContours(edgeMat, out contours, out hierarchy,
                RetrievalModes.External, ContourApproximationModes.APPROX_SIMPLE);

            resultImage = srcImage.Clone(); // 克隆输入图像，避免修改原始数据（ImageData 引用传递）
            Cv2.DrawContours(resultImage, contours, -1, new Scalar(0, 255, 0), 2);

            // ========== 5. 直接赋值设置输出 ==========

            // 输出边缘图像
            ImageData imgOutEdge = MatToImageData(edgeMat);
            edgeImage = imgOutEdge;

            // 输出绘制轮廓的结果图像
            ImageData imgOutResult = MatToImageData(resultImage);
            resultImageOut = imgOutResult;

            // 输出检测到的轮廓数量
            contourCount = contours.Length;

            processCount++;
            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "Canny 边缘检测失败：" + ex.Message;
            return false;
        }
        finally
        {
            if (srcImage != null) srcImage.Dispose();
            if (grayMat != null) grayMat.Dispose();
            if (resultImage != null) resultImage.Dispose();
        }
    }

    /// <summary>
    /// 资源释放函数
    /// </summary>
    public virtual void Dispose()
    {
        edgeMat.Dispose();
    }

    #region OpenCV 转换方法

    /// <summary>
    /// ImageData 转 Mat
    /// </summary>
    private Mat ImageDataToMat(ImageData img)
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
    /// Mat 转 ImageData
    /// </summary>
    private ImageData MatToImageData(Mat matImage)
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

    #endregion
}
