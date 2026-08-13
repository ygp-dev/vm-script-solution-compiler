// ============================================================
// 本文件为 Script.Methods.dll 反编译产物整理，仅供 *签名参考*，
// 不可直接复制编译（依赖外部类型定义）。语法已规范为 C# 5.0 兼容。
// 标量 Get/Set 等接口在反编译产物中定义于 VM3DScriptBase（3D 基类），
// 2D 的 ScriptMethods 基类未公开，两者同名同签名，可作 2D 脚本签名参考。
// 数组类 Get/Set 的 *实际* 调用（partial property 内部使用的 2 参重载）
// 定义在 Conceal.InternalMethods 中，见 ./InternalMethods.cs
// Image、ShapeConfig 等类型为 Script.Methods.dll 内部类型，本仓库未收录；
// 引用此类类型时以 SKILL.md 与 Script.DataStruct.cs 已收录的定义为准。
// ============================================================

using System;

namespace Script.Methods
{
    /// <summary>
    /// 脚本生命周期接口
    /// </summary>
    public interface IProcessMethods
    {
        /// <summary>
        /// 初始化脚本。可在此方法中实现初始化相关操作。该方法在加载方案或预编译全局脚本时执行。
        /// </summary>
        void Init();

        /// <summary>
        /// 定义脚本模块所在流程的执行逻辑
        /// </summary>
        /// <returns>执行结果</returns>
        bool Process();
    }

    #region 通信模块接口

    /// <summary>
    /// 全局变量模块
    /// </summary>
    public static class GlobalVariableModule
    {
        /// <summary>
        /// 设置全局变量
        /// </summary>
        /// <param name="paramName">全局变量名称</param>
        /// <param name="paramValue">全局变量的值</param>
        /// <returns>0-调用成功；非0-调用失败</returns>
        public static int SetValue(string paramName, string paramValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取全局变量的值
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <returns>调用成功返回全局变量的值（object类型）；调用异常返回null</returns>
        /// <remarks>返回值为object类型，如需转成其他类型，请将object转成string再转至其他类型</remarks>
        public static object GetValue(string paramName)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 全局通信模块
    /// </summary>
    public static class GlobalCommunicateModule
    {
        /// <summary>
        /// 获取指定设备
        /// </summary>
        /// <param name="deviceID">通信管理中设备的设备ID</param>
        /// <returns>设备对象</returns>
        public static Device GetDevice(int deviceID)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 通信设备对象
    /// </summary>
    public class Device
    {
        /// <summary>
        /// 获取指定地址（PLC/Modbus设备）
        /// </summary>
        /// <param name="addressID">通信管理中设备的地址ID</param>
        /// <returns>地址对象</returns>
        public Address GetAddress(int addressID)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 指定某个TCP、UDP或串口发送string类型的数据
        /// </summary>
        /// <param name="data">待发送的数据</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SendData(string data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 指定某个TCP、UDP或串口发送十六进制数据
        /// </summary>
        /// <param name="bytedata">待发送的十六进制数据</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SendData(byte[] bytedata)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 通信地址对象（PLC/Modbus设备）
    /// </summary>
    public class Address
    {
        /// <summary>
        /// 指定某个PLC/Modbus设备发送Int、float或string类型数据
        /// </summary>
        /// <param name="data">待发送的数据，多个数据用;隔开</param>
        /// <param name="dataType">待发送数据的类型</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SendData(string data, DataType dataType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 指定某个PLC/Modbus设备发送十六进制数据
        /// </summary>
        /// <param name="bytedata">待发送的十六进制数据</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SendData(byte[] bytedata, DataType dataType)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region 流程模块接口

    /// <summary>
    /// 当前流程模块
    /// </summary>
    public static class CurrentProcess
    {
        /// <summary>
        /// 获取指定模块
        /// </summary>
        /// <param name="paramModuleName">模块名称。如模块在Group中，需附带Group名称，例如："Group1.图像源1"</param>
        /// <returns>模块对象</returns>
        public static Module GetModule(string paramModuleName)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 流程中的模块对象
    /// </summary>
    public class Module
    {
        /// <summary>
        /// 获取模块某个结果参数的值
        /// </summary>
        /// <param name="paramValueName">模块结果中某个参数的名称</param>
        /// <returns>调用成功返回结果参数的值；调用异常返回null</returns>
        public object GetValue(string paramValueName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置模块运行参数的值
        /// </summary>
        /// <param name="paramValueName">参数名称</param>
        /// <param name="paramValue">参数值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetValue(string paramValueName, string paramValue)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region 脚本基类接口

    /// <summary>
    /// VM 3D算法开发平台脚本接口
    /// </summary>
    public abstract class VM3DScriptBase
    {
        #region 生命周期接口

        /// <summary>
        /// 初始化脚本。可在此方法中实现初始化相关操作。该方法在加载方案或预编译全局脚本时执行。
        /// </summary>
        public virtual void Init() { }

        /// <summary>
        /// 定义脚本模块所在流程的执行逻辑
        /// </summary>
        /// <returns>执行结果</returns>
        public abstract bool Process();

        #endregion

        #region 模块参数接口

        /// <summary>
        /// 获取模块运行参数
        /// </summary>
        /// <param name="nModuleID">模块ID</param>
        /// <param name="paramKey">模块运行参数</param>
        /// <param name="paramValue">模块运行参数的值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetModuleParam(uint nModuleID, string paramKey, ref string paramValue)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region 点集转换接口

        /// <summary>
        /// 将点集二进制数据转换为轮廓点数组数据
        /// </summary>
        /// <param name="inVariant">待转换的点集二进制数据</param>
        /// <param name="contourPointArray">轮廓点数组数据</param>
        /// <returns>0-转换成功；非0-转换失败</returns>
        public int BytesToPointset(byte[] inVariant, ref ContourPointData[] contourPointArray)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 将轮廓点数组数据转换为点集二进制数据
        /// </summary>
        /// <param name="contourPointArray">待转换的轮廓点数组数据</param>
        /// <returns>非null-转换成功，为点集二进制数据；null-转换失败</returns>
        public byte[] PointsetToBytes(ContourPointData[] contourPointArray)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region GetValue 系列接口

        /// <summary>
        /// 获取int类型变量的值
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="paramValue">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetIntValue(string paramName, ref int paramValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取float类型变量的值
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="paramValue">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetFloatValue(string paramName, ref float paramValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取string类型变量的值
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="paramValue">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetStringValue(string paramName, ref string paramValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取byte数组类型变量的值
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="paramValue">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetBytesValue(string paramName, ref byte[] paramValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取图像数据
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="paramValue">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetIMAGEValue(string paramName, ref Image paramValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取ROI的BOX数据（识别框等）
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="roiboxData">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetRoiboxValue(string paramName, ref RoiboxData roiboxData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取double型数据
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="paramValue">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetDoubleValue(string paramName, ref double paramValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取立体图像数据，包括图像源、深度图数据、图像偏移和缩放情况等
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="stereoImageData">变量值（StereoImageData结构体）</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetStereoImageValue(string key, ref StereoImageData stereoImageData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取3D点数据
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="point3d">变量值（Point3DData结构体）</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetPoint3DValue(string key, ref Point3DData point3d)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取位姿数据
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="poseInfoData">变量值（PoseInfoData结构体数组）</param>
        /// <param name="arrayCount">数组个数</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetPoseInfoArrayValue(string key, ref PoseInfoData[] poseInfoData, out int arrayCount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取3D点集数据
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="point3DData">变量值（Point3DData结构体数组）</param>
        /// <param name="arrayCount">数组个数</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetPoint3DArrayValue(string paramName, ref Point3DData[] point3DData, out int arrayCount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取double型数组数据
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="paramValue">变量值</param>
        /// <param name="arrayCount">数组个数</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetDoubleArrayValue(string paramName, ref double[] paramValue, out int arrayCount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取int数组变量
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="paramValue">变量值</param>
        /// <param name="arrayCount">数组个数</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetIntArrayValue(string paramName, ref int[] paramValue, out int arrayCount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取float型数组变量
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="paramValue">变量值</param>
        /// <param name="arrayCount">数组个数</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetFloatArrayValue(string paramName, ref float[] paramValue, out int arrayCount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取string类型数组变量的值
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="paramValue">变量值</param>
        /// <param name="arrayCount">数组个数</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int GetStringArrayValue(string paramName, ref string[] paramValue, out int arrayCount)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region SetValue 系列接口

        /// <summary>
        /// 设置int型变量的值
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="value">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetIntValue(string key, int value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置int数组变量的值
        /// </summary>
        /// <param name="key">key值</param>
        /// <param name="valueArray">数组</param>
        /// <param name="index">数组的索引</param>
        /// <param name="len">数组的长度</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetIntArrayValue(string key, int[] valueArray, int index, int len)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置float型变量值
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="value">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetFloatValue(string key, float value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置float数组变量的值
        /// </summary>
        /// <param name="key">key值</param>
        /// <param name="valueArray">数组</param>
        /// <param name="index">数组的索引</param>
        /// <param name="len">数组的长度</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetFloatArrayValue(string key, float[] valueArray, int index, int len)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置string型变量的值
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="value">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetStringValue(string key, string value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置string数组变量的值
        /// </summary>
        /// <param name="key">key值</param>
        /// <param name="valueArray">数组</param>
        /// <param name="index">数组的索引</param>
        /// <param name="len">数组的长度</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetStringArrayValue(string key, string[] valueArray, int index, int len)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置十六进制数据
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="value">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetBytesValue(string key, byte[] value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置图像数据
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="value">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetImageValue(string key, ImageData value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置double型数据
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="value">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetDoubleValue(string key, double value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置double数组变量的值
        /// </summary>
        /// <param name="key">key值</param>
        /// <param name="valueArray">数组</param>
        /// <param name="index">数组的索引</param>
        /// <param name="len">数组的长度</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetDoubleArrayValue(string key, double[] valueArray, int index, int len)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 按照索引设置string型数组内某个元素的值
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="value">变量值</param>
        /// <param name="index">数组的索引</param>
        /// <param name="total">数组元素个数</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetStringValueByIndex(string key, string value, int index, int total)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 按照索引设置int型数组内某个元素的值
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="value">变量值</param>
        /// <param name="index">数组的索引</param>
        /// <param name="total">数组元素个数</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetIntValueByIndex(string key, int value, int index, int total)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 按照索引设置float型数组内某个元素的值
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="value">变量值</param>
        /// <param name="index">数组的索引</param>
        /// <param name="total">数组元素个数</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetFloatValueByIndex(string key, float value, int index, int total)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 按照索引设置double型数组内某个元素的值
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="value">变量值</param>
        /// <param name="index">数组的索引</param>
        /// <param name="total">数组元素个数</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetDoubleValueByIndex(string key, double value, int index, int total)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置ROI的BOX数据（识别框等）
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="roiboxData">变量值</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetRoiboxValue(string paramName, RoiboxData roiboxData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置立体图像数据，包括图像源、深度图数据、图像偏移和缩放情况等
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="stereoImageData">变量值（StereoImageData结构体）</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetStereoImageValue(string key, ref StereoImageData stereoImageData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置3D点数据
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="point3d">变量值（Point3DData结构体）</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetPoint3DValue(string key, ref Point3DData point3d)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置3D点集数据
        /// </summary>
        /// <param name="paramName">变量名称</param>
        /// <param name="point3DData">变量值（Point3DData结构体数组）</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetPoint3DArrayValue(string paramName, ref Point3DData[] point3DData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置位姿数据
        /// </summary>
        /// <param name="key">变量名称</param>
        /// <param name="poseInfoData">变量值（PoseInfoData结构体数组）</param>
        /// <returns>0-调用成功；非0-调用异常</returns>
        public int SetPoseInfoArrayValue(string key, ref PoseInfoData[] poseInfoData)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region 显示与调试接口

        /// <summary>
        /// 在渲染控件上显示图像（仅型号中带6210或7120的加密狗支持使用）
        /// </summary>
        /// <param name="imageData">图像数据对象</param>
        public void ShowImage(ImageData imageData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 在渲染控件上绘制图形（仅型号中带6210或7120的加密狗支持使用）
        /// </summary>
        /// <param name="shapeData">图形数据对象</param>
        /// <param name="shapeConfig">图形属性对象（默认null）</param>
        public void DrawShape(object shapeData, ShapeConfig shapeConfig = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 将异常信息打印至DebugView中
        /// </summary>
        /// <param name="content">待打印的内容</param>
        public void ConsoleWrite(string content)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 脚本运行异常时，通过弹窗提示
        /// </summary>
        /// <param name="msg">弹窗内容</param>
        public void ShowMessageBox(string msg)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion
}
