using System;
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
        
		//获取设置int
        int a = 0;
        GetIntValue("num",ref a);
        
		
		//获取并逆序X数组
		int count=0;
        float[] array = new float[a];
        GetFloatArrayValue("InX",ref array,out count);
        
        int start = 0;
        int end = array.Length - 1;

        while (start < end)
        {
            // 交换元素
            float temp = array[start];
            array[start] = array[end];
            array[end] = temp;

            start++;
            end--;
        }
        //输出X数组
        SetFloatArrayValue("OutX",array,0,array.Length);
        
        //获取并逆序Y数组
        GetFloatArrayValue("InY",ref array,out count);
        
        start = 0;
        end = array.Length - 1;

        while (start < end)
        {
            // 交换元素
            float temp = array[start];
            array[start] = array[end];
            array[end] = temp;

            start++;
            end--;
        }
        
        //输出Y数组
        SetFloatArrayValue("OutY",array,0,array.Length);
        
        
        
        
        return true;
    }
}
                            