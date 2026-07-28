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


/*****************************************
 * Example explanation:Example of multi process control operation
 * Logic Control:Single run, each flow execute once
 * Continuous run:continuous run, each flow execute continuous
 * 示例说明: 获取流程对象，模块对象，设置参数，运行，获取结果
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

        IMVSFastFeatureMatchModuTool fastFeatureTool = (IMVSFastFeatureMatchModuTool)VmSolution.Instance["流程1.快速匹配1"];
        VmProcedure pro1 = (VmProcedure)VmSolution.Instance["流程1"];
        if (fastFeatureTool != null)
        {
            FastFeatureMatchParam matchParam = fastFeatureTool.ModuParams;
            if (matchParam != null)
            {
                //设置特征匹配模块运行参数-最大匹配个数
                matchParam.MaxMatchNum = 10;
            }
        }
        if (pro1 != null)
        {
            pro1.Run();
            //获取特征匹配模块的运行结果
            FastFeatureMatchResult matchResult = fastFeatureTool.ModuResult;
            if (matchResult != null)
            {
                //获取匹配个数
                int matchnum = matchResult.MatchNum;
                //获取匹配点
                List<PointF> matchpoint = matchResult.MatchPoint;
            }
        }
        //获取/设置流程1的局部变量数据
       var localModule = pro1.LocalVariable;
       localModule.SetVarInt("var0",new int[]{1,2});
       int[] nArray = localModule.GetVarInt("var0").pIntVal;
       return nRet;
    }
}