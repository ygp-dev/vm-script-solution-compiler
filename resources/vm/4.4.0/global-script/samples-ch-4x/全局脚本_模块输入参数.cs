using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections;
using VM.GlobalScript.Methods;
using iMVS_6000PlatformSDKCS;
using VM.Core;
using VM.PlatformSDKCS;
using ImageSourceModuleCs;
using IMVSFastFeatureMatchModuCs;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;


/*****************************************
 * Example explanation:Example of multi process control operation
 * Logic Control:Single run, each flow execute once
 * Continuous run:continuous run, each flow execute continuous
 * 示例说明: 设置流程输入参数，执行流程
 * ***************************************/
public class UserGlobalScript : UserGlobalMethods, IScriptMethods
{
    /// <summary>
    /// Init
    /// </summary>
    /// <returns>Success:return 0</returns>
    public int Init()
    {
        //SDK init
        return InitSDK();
    }

    /// <summary>
    /// execute function
    /// Single run:the function execute once
    /// Continuous run:Repeat the function at regular intervals
    /// 运行函数
    /// 单次执行:该函数执行一次
    /// 连续执行:以一定时间间隔重复执行该函数
    /// </summary>
    /// <returns>Success:return 0</returns>
    public int Process()
    {
        //m_operateHandle SDK handle
        if (m_operateHandle == IntPtr.Zero)
        { return ImvsSdkPFDefine.IMVS_EC_NULL_PTR; }

        //All processes are executed by default
        //If execute in your own define logic,please remove the function :DefaultExecuteProcess, Create your own logic function.
        //默认执行全部流程，
        //如果自定义流程执行逻辑，请移除DefaultExecuteProcess方法，编写自定义流程执行逻辑代码
        int nRet = 0;

        VmProcedure pro1 = (VmProcedure)VmSolution.Instance["流程1"];
        if (pro1 != null)
        {
            ProcedureParam proParam = pro1.ModuParams;
            if (proParam != null)
            {
                //设置流程输入图像
                Bitmap bitmap = new Bitmap(@"D:\Program\VM_Temp\0257800-IMG_2.jpg");
                proParam.SetInputImage_V2("ImageData", new ImageBaseData(bitmap));
                //设置流程输入int
                proParam.SetInputInt("intX", new int[] { 10 });
                //设置流程输入float
                proParam.SetInputFloat("floatY", new float[] { 2.345f });
                //设置流程输入string
                proParam.SetInputString("stringZ", new InputStringData[] { new InputStringData() { strValue = "abc" } });
                //流程执行
                pro1.Run();
                //释放dispose
                bitmap.Dispose();
            }
        }
        return nRet;
    }

}