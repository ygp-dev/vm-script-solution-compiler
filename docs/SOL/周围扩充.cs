using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Script.Methods;
/************************************
Shell Module default code: using .NET Framwwork 4.6.1
*************************************/
public partial class UserScript:ScriptMethods,IProcessMethods
{
    //the count of process
	//执行次数计数
    int processCount ;  

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
        ImageData img = CreateBorderImg(inputImage, 2, 255);
        out0 = img;
        return true;
    }

    private ImageData CreateBorderImg(ImageData image, int padding = 1, byte borderColor = 0)
    {
        if (image.PixelFormat != ImagePixelFormate.MONO8)
            throw new NotSupportedException("仅支持 Gray8 图像格式");

        int w = image.Width;
        int h = image.Height;
        int newW = w + padding * 2;
        int newH = h + padding * 2;

        // 原图像数据：直接使用已有 buffer
        byte[] pBinaryData = image.Buffer;

        // 创建新数据，填充为 borderColor
        byte[] pData = Enumerable.Repeat(borderColor, newW * newH).ToArray();

        // 将原图数据复制到新图像中心区域
        for (int y = 0; y < h; y++)
        {
            int srcOffset = y * w;
            int destOffset = (y + padding) * newW + padding;
            Buffer.BlockCopy(pBinaryData, srcOffset, pData, destOffset, w);
        }

        // 构造新的 ImageData
        return new ImageData
        {
            Width = newW,
            Height = newH,
            PixelFormat = image.PixelFormat,
            Buffer = pData
        };
    }


}
