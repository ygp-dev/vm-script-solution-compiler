using System;
using System.Text;
using System.Windows.Forms;
using Script.Methods;
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
        
        //获取16进制数据
        byte[] tempBytes = new byte[] { };
        GetBytesValue("in0", ref tempBytes);
           
        //数据编码
        Encoding gb2312 = Encoding.GetEncoding("GB2312");
        string decodedString = gb2312.GetString(tempBytes);
        
		//输出字符串
        SetStringValue("out0", decodedString);
        
        
        return true;
    }
}
                            