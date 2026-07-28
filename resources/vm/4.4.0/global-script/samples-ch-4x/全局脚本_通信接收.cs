using System;
using VM.GlobalScript.Methods;
using System.Windows.Forms;
using iMVS_6000PlatformSDKCS;
using System.Runtime.InteropServices;
using VM.Core;
using VM.PlatformSDKCS;
using VMControls.Interface;
using System.Threading.Tasks;
/*****************************************
 * Example explanation:Example of multi process control operation
 * Logic Control:Single run, each flow execute once
 * Continuous run:continuous run, each flow execute continuous
 * 示例说明: 通信接收
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
        int ret = InitSDK();
        //设置与全局通信模块的通信端口
        StartGlobalCommunicate();
        //注册通信数据接收事件
        RegesiterReceiveCommunicateDataEvent();
        return ret;
    }


    /// <summary>
    /// 通信数据接收函数
    /// </summary>
    public override void UserGlobalMethods_OnReceiveCommunicateDataEvent(ReceiveDataInfo dataInfo)
    {
        if (dataInfo == null || dataInfo.DeviceData == null)
        { return; }
        //接收到的数据转成字符串
        string str = System.Text.Encoding.Default.GetString(dataInfo.DeviceData);
        //这里的deviceIndex和全局通信模块中的一致
        if (dataInfo.DeviceID == 1)
        {
            //解析收到的数据
            if (str == "0")
            {
                //异步执行流程1 一次
                VmProcedure pro1 = (VmProcedure)VmSolution.Instance["流程1"];
                if(pro1!=null)
                {pro1.Run("", false);}
            }
            else if(str == "1")
            {
				//保存方案
            	Task.Run(()=>{VmSolution.Save();});
            }
            else if(str == "2")
            {
				//加载方案
            	Task.Run(()=>{VmSolution.Load(@"D:\Program\VM_Temp\4.3\全局脚本\sdk.sol");});
            }
        }
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
        int nRet = DefaultExecuteProcess();
        return nRet;
    }

}