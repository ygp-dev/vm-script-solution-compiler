using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;
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
        IOBERROR ero = FAIO.iob_board_init(1, 0);// 初始化板卡
        return true;
    }
}

public enum FAIO_DELAYOFF
{
    FAIO_delayOff_0_1 = 0x12,   //0.1s  //iob_set_delayOff  secondevery
    FAIO_delayOff_0_5 = 0x13,   //0.5s  //iob_set_delayOff  secondevery
    FAIO_delayOff_1_0 = 0x14,   //1.0s  //iob_set_delayOff  secondevery
}


//定义回调函数  委托
public delegate void FA_IO_CALLBACK(IntPtr p, ushort comno, Byte inportstatus, Byte outportstatus, Byte framenum);
public delegate void FA_IO_CALLBACK2(IntPtr p, ushort comno, Byte inportstatus, Byte outportstatus, Byte inportstatus_last, Byte outportstatus_last, Byte framenum);

public enum IOBERROR
{
    FAIO_ERROR_SUCCESS = 0,  // 成功
    FAIO_ERROR_OVERMAXBORADNUM = -1, // comno 的值超出最大board数 0≤comno≤15
    FAIO_ERROR_INITFAILED = -2, // board打开失败 //串口不存在
    FAIO_ERROR_WRONGBOARD = -3, // board打开失败 //板卡异常 //次品卡或者非该软件专用卡
    FAIO_ERROR_BOARDNOTINIT = -4, // board没有初始化,即没有调用iob_board_init函数
    FAIO_ERROR_BOARDDISCONNECT = -5, // board没...