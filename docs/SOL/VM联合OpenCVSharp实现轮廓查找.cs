using System;
using System.Text;
using System.Windows.Forms;
using Script.Methods;
using OpenCvSharp;
using System.Runtime.InteropServices;

class UserScript : ScriptMethods, IProcessMethods
{
    //the count of process
    //执行次数计数
    int processCount;

    /// <summary>
    /// Initialize the field's value when compiling
    /// 预编译时变量初始化
    /// </summary>
    public void Init()
    {
        //You can add other global fields here
        //变量初始化，其余变量可在该函数中添加
        processCount = 0;

    }

    /// <summary>
    /// Enter the process function when running code once
	/// 流程执行一次进入Process函数
    /// </summary>
    /// <returns></returns>
    public bool Process()
    {
        //You can add your codes here, for realizing your desired function
        //每次执行将进入该函数，此处添加所需的逻辑流程处理
        //MessageBox.Show("Process Success");
        ImageData img = new ImageData();
        ImageData imgOut = new ImageData();
        GetImageValue("in0", ref img);
        Mat srcImage = Mat.Zeros(img.Heigth, img.Width, MatType.CV_8UC1);
        Mat dstImage = Mat.Zeros(img.Heigth, img.Width, MatType.CV_8UC1);
        Mat dstImageRGB = Mat.Zeros(img.Heigth, img.Width, MatType.CV_8UC3);

        if (img.PixelFormat == ImagePixelFormate.MONO8)
        {
            //开辟内存空间
            IntPtr grayPtr = Marshal.AllocHGlobal(img.Width * img.Heigth);
            //向内存空间中写入数据     
            Marshal.Copy(img.Buffer, 0, grayPtr, img.Buffer.Length);
            //imagedata转Mat     
            srcImage = new Mat(img.Heigth, img.Width, MatType.CV_8UC1, grayPtr);

            //调用OpenCV中函数接口进行图像处理
            Cv2.Canny(srcImage, dstImage, 90, 230);
            // 创建一个序列来存放所找到的轮廓
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(dstImage, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new Point(0, 0));
            
            Mat dst_Image = Mat.Zeros(dstImage.Size(), srcImage.Type());
            //如果灰度图要绘制彩框
            Cv2.CvtColor(dst_Image, dstImageRGB, ColorConversionCodes.GRAY2BGR);
            Random rnd = new Random();
            for (int i = 0; i < contours.Length; i++)
            {
                Scalar color = new Scalar(rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255));
                Cv2.DrawContours(dstImageRGB, contours, i, color, 2, LineTypes.Link8, hierarchy);
            }

            IntPtr intPtr = dstImageRGB.Data;
            byte[] data = new Byte[dstImageRGB.Width * dstImageRGB.Height * 3];
            Marshal.Copy(intPtr, data, 0, data.Length);

            imgOut.Buffer = data;
            imgOut.Width = dstImageRGB.Width;
            imgOut.Heigth = dstImageRGB.Height;
            imgOut.PixelFormat = ImagePixelFormate.RGB24;

            //用完记得释放指针 
            Marshal.FreeHGlobal(grayPtr);
        }

        SetImageValue("imageOut", imgOut);

        return true;
    }
}