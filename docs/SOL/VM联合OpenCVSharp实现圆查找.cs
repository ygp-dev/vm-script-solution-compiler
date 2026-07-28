
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

    /// 
    /// Initialize the field's value when compiling
    /// 预编译时变量初始化
    /// 
    public void Init()
    {
        //You can add other global fields here
        //变量初始化，其余变量可在该函数中添加
        processCount = 0;
    }

    /// 
    /// Enter the process function when running code once
	/// 流程执行一次进入Process函数
    /// 
    /// 
    public bool Process()
    {
        //You can add your codes here, for realizing your desired function
        //每次执行将进入该函数，此处添加所需的逻辑流程处理
        //MessageBox.Show("Process Success");
        ImageData img = new ImageData();
        GetImageValue("in0", ref img);
        ImageData imgOut = new ImageData();
        Mat srcImage = Mat.Zeros(img.Heigth, img.Width, MatType.CV_8UC1);

        Rect rect = new Rect(0, 0, 300, 300);

        if (img.PixelFormat == ImagePixelFormate.MONO8)
        {
            IntPtr grayPtr = Marshal.AllocHGlobal(img.Width * img.Heigth);
            Marshal.Copy(img.Buffer, 0, grayPtr, img.Buffer.Length);

            //imagedata转Mat
            srcImage = new Mat(img.Heigth, img.Width, MatType.CV_8UC1, grayPtr);          
            Mat imageROI = new Mat(srcImage, rect);
            Mat dstImage = Mat.Zeros(imageROI.Height, imageROI.Width, MatType.CV_8UC1);
            Cv2.Threshold(imageROI,dstImage,10,120,ThresholdTypes.Otsu);
            Mat newImage = new Mat(srcImage, rect);
            dstImage.CopyTo(newImage);
            byte[] datab = new Byte[srcImage.Width * srcImage.Height];

            //mat转ImageData
            srcImage.GetArray(0, 0, datab);
            imgOut.Buffer = datab;
            imgOut.Width = srcImage.Width;
            imgOut.Heigth = srcImage.Height;
            imgOut.PixelFormat = ImagePixelFormate.MONO8;

            //用完记得释放指针
            Marshal.FreeHGlobal(grayPtr);
        }

        SetImageValue("imageOut", imgOut);
        return true;
    }
}
