using System;
using System.Text;
using System.Windows.Forms;
using Script.Methods;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;
    string errorStatus = string.Empty;

    /// <summary>
    /// 预编译时变量初始化
    /// </summary>
    public void Init()
    {
        processCount = 0;
        errorStatus = string.Empty;
    }

    /// <summary>
    /// 流程执行一次进入 Process 函数
    /// </summary>
    /// <returns>执行结果</returns>
    public bool Process()
    {
        try
        {
            errorStatus = string.Empty;

            // ===== 标量类型：直接赋值 =====

            int intVal = in0;
            out0 = intVal;

            float floatVal = in1;
            out1 = floatVal;

            string strVal = in2;
            out2 = strVal;

            byte[] byteVal = in8;
            out8 = byteVal;

            // ===== 数组类型：直接赋值 =====

            int[] intArr = in3;
            out3 = intArr;

            float[] floatArr = in4;
            out4 = floatArr;

            string[] strArr = in5;
            out5 = strArr;

            // ===== 复合类型 =====

            // 图像数据（直接赋值为引用传递，如需独立副本需通过 Mat 转换）
            ImageData imageData = in6;
            out6 = imageData;

            // ROIBOX 数据
            RoiboxData[] roiData = in7;
            out7 = roiData;

            // ===== 全局变量 =====

            GlobalVariableModule.SetValue("var1", "323");
            object paramValue = GlobalVariableModule.GetValue("var1");

            // ===== 模块控制 =====
            // GetModule 传入模块名（嵌套在 group 中需带 group 名）
            object result = CurrentProcess.GetModule("图像源1").GetValue("Height");
            object result1 = CurrentProcess.GetModule("组合模块1.图像源1").GetValue("Height");

            CurrentProcess.GetModule("BLOB分析1").SetValue("FindNum", "4");
            CurrentProcess.GetModule("组合模块1.BLOB分析1").SetValue("FindNum", "4");

            // ===== 通信发送 =====

            GlobalCommunicateModule.GetDevice(1).SendData("msg");
            GlobalCommunicateModule.GetDevice(2).GetAddress(1).SendData("100", DataType.IntType);
            GlobalCommunicateModule.GetDevice(3).GetAddress(1).SendData("100", DataType.IntType);

            processCount++;
            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "Process 异常：" + ex.Message;
            return false;
        }
    }
}
