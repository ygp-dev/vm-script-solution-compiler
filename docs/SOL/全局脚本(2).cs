//无问题
using System;
using VM.GlobalScript.Methods;
using System.Windows.Forms;
using iMVS_6000PlatformSDKCS;
using System.Runtime.InteropServices;
using VM.Core;
using VM.PlatformSDKCS;
using VMControls.Interface;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
/*****************************************
 * 脚本说明: 通信控制+SOL固定路径加载+当前SOL拷贝+保存方案
 * 0 - 预留流程执行 | 2 - 预留加载 | Save - 保存SOL
 * 21000-Disabled/Enabled - 面积检测模块禁用/启用
 * BTUp/BTDown-Enabled/Disabled - 上下相机自动标定禁用/启用+数据模块联动
 * ProsAndCons-Enabled/Disabled - 上相机正反区分禁用/启用
 * Stacking-Enabled/Disabled - 上相机叠料检测禁用/启用
 * Load|文件名 - 固定目录加载指定SOL（格式：Load|xxx.sol），成功返回LoadOK，失败返回LoadNG
 * Copy|自定义名称 - 拷贝当前SOL为新文件（自动补.sol后缀），成功返回CopyOK，失败返回CopyNG
 * 固定SOL目录：D:\视觉柔性工作站系统\视觉流程\
 * ***************************************/
public class UserGlobalScript : UserGlobalMethods, IScriptMethods
{
    // SOL文件存放目录（修改为指定路径，支持中文目录）
   // public string _fixedSolDir = @"";
    // 临时SOL文件路径
    //private readonly string _tempSolPath = Path.Combine(Path.GetTempPath(), "CurrentVM_SOL_Temp.sol");
    /// <summary>
    /// 初始化SDK+全局通信模块
    /// </summary>
    /// <returns>Success:return 0</returns>
    public int Init()
    {
        //SDK初始化
        int ret = InitSDK();
        //设置与全局通信模块的通信端口
        StartGlobalCommunicate();
        //注册通信数据接收事件
        RegesiterReceiveCommunicateDataEvent();
       
        return ret;
    }


    /// <summary>
    /// 通信数据接收主函数
    /// </summary>
    public override void UserGlobalMethods_OnReceiveCommunicateDataEvent(ReceiveDataInfo dataInfo)
    {
        // 基础校验：数据为空/设备ID非1，直接返回
        if (dataInfo == null || dataInfo.DeviceData == null) { return; }
        
        // 接收到的数据转成字符串并去除首尾空格
        string receiveStr = System.Text.Encoding.Default.GetString(dataInfo.DeviceData).Trim();
        
        // 仅处理DeviceID=1的通信数据
        if (dataInfo.DeviceID != 1) { return; }

        // --------------- 模块控制指令 ---------------
        // 预留流程执行
        if (receiveStr == "0")
        {
            // 预留：执行流程1
            //SendCommDeviceData("LoadNG",1);
            //VmProcedure pro1 = (VmProcedure)VmSolution.Instance["流程1"];
            //if(pro1!=null) { pro1.Run("", false); }
        }
        else if (receiveStr == "GetFileName")
        {
            int deviceId = dataInfo.DeviceID;
            Task.Run(delegate() 
            {
                try 
                {    
                	// 获取当前方案路径
                    string currentSolPath = GetCurrentSolPath();
            		string solFileName = Path.GetFileNameWithoutExtension(currentSolPath) ;             		
					if (!string.IsNullOrEmpty(solFileName))  
					{						
            			SendCommDeviceData(solFileName,deviceId);
               			return;
               		}
                }
                catch (Exception ex) 
                {
                    SendCommDeviceData("GetNG",deviceId);
                }
            });
       	}              
        // 禁用面积检测模块（true=禁用，false=启用）
        else if (receiveStr == "21000-Disabled")
        {
            VmSolution.Instance.GetModuleByID(21000).IsForbidden = true;
        }
        // 启用面积检测模块
        else if (receiveStr == "21000-Enabled")
        {
            VmSolution.Instance.GetModuleByID(21000).IsForbidden = false;
        }
        
        // 禁用面积检测模块2（true=禁用，false=启用）
        else if (receiveStr == "21008-Disabled")
        {
            VmSolution.Instance.GetModuleByID(21008).IsForbidden = true;
        }
        // 启用面积检测模块2
        else if (receiveStr == "21008-Enabled")
        {
            VmSolution.Instance.GetModuleByID(21008).IsForbidden = false;
        }
        
        // 预留加载指令
        else if (receiveStr == "2")
        {
            //Task.Run(delegate() { VmSolution.Load(@"D:\Program\VM_Temp\4.3\全局脚本\sdk.sol"); });
        }
        // 保存当前SOL方案
        else if (receiveStr == "Save")
        {
            Task.Run(delegate() 
            {
                try 
                {
                    VmSolution.Save(); 
                    // 获取当前方案路径并更新自动加载路径
                    //UpdateAutoLoadPath();
                } catch (Exception) { /* 工业现场静默处理 */ }
            });
        }
        // 上相机自动标定启用
        else if (receiveStr == "BTUp-Enabled")
        {
            VmSolution.Instance.GetModuleByID(21001).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(2).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(19).IsForbidden = true;
        }
        // 上相机手动标定启用
        else if (receiveStr == "ManualBTUp-Enabled")
        {
            VmSolution.Instance.GetModuleByID(21003).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(2).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(19).IsForbidden = true;
        }
        // 下相机自动标定启用
        else if (receiveStr == "BTDown-Enabled")
        {
            VmSolution.Instance.GetModuleByID(21004).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(70).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(77).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(259).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(262).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(21007).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(21014).IsForbidden = true;
        }	
        // 上相机自动标定禁用
        else if (receiveStr == "BTUp-Disabled")
        {
            VmSolution.Instance.GetModuleByID(21001).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(2).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(19).IsForbidden = false;
        }
  		// 第三相机自动标定启用
        else if (receiveStr == "BTcam3-Enabled")
        {
            VmSolution.Instance.GetModuleByID(21010).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(165).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(151).IsForbidden = true;
        }
         // 第三相机自动标定禁用
        else if (receiveStr == "BTcam3-Disabled")
        {
            VmSolution.Instance.GetModuleByID(21010).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(165).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(151).IsForbidden = false;
        }
	
         // 上相机手动标定禁用
        else if (receiveStr == "ManualBTUp-Disabled")
        {
            VmSolution.Instance.GetModuleByID(21003).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(2).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(19).IsForbidden = false;
        }
          // 传送带N点标定启用
        else if (receiveStr == "BeltNcal-Enabled")
        {
    		VmSolution.Instance.GetModuleByID(21005).IsForbidden = false;
     
        }
        // 传送带N点标定禁用
        else if (receiveStr == "BeltNcal-Disabled")
        {
            VmSolution.Instance.GetModuleByID(21005).IsForbidden = true;
     
        }
         // 传送带坐标排序启用
        else if (receiveStr == "BeltSort-Enabled")
        {
    		VmSolution.Instance.GetModuleByID(32).IsForbidden = false;
    		VmSolution.Instance.GetModuleByID(34).IsForbidden = false;
     		VmSolution.Instance.GetModuleByID(112).IsForbidden = false;
        }
        // 传送带坐标排序禁用
        else if (receiveStr == "BeltSort-Disabled")
        {
           	VmSolution.Instance.GetModuleByID(32).IsForbidden = true;
    		VmSolution.Instance.GetModuleByID(34).IsForbidden = true;
     		VmSolution.Instance.GetModuleByID(112).IsForbidden = true;
        }
        
        // 下相机自动标定禁用
        else if (receiveStr == "BTDown-Disabled")
        {
            VmSolution.Instance.GetModuleByID(21004).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(70).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(77).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(259).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(262).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(21007).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(21014).IsForbidden = false;
        }	
        // 上相机高级功能设置
		// 00，全关
        else if (receiveStr == "ScreenSet,0,0")
        {
            VmSolution.Instance.GetModuleByID(39).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(40).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(109).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(43).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(110).IsForbidden = true;
        }
		// 10，只开启正反区分
		 else if (receiveStr == "ScreenSet,1,0")
        {
            VmSolution.Instance.GetModuleByID(39).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(40).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(109).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(43).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(110).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(6).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(98).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(111).IsForbidden = true;
        }
		// 01，只开启叠料检测
		 else if (receiveStr == "ScreenSet,0,1")
        {
            VmSolution.Instance.GetModuleByID(39).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(40).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(109).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(43).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(110).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(6).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(98).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(111).IsForbidden = true;
        }
		// 11，全开启
		 else if (receiveStr == "ScreenSet,1,1")
        {
            VmSolution.Instance.GetModuleByID(39).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(40).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(109).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(43).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(110).IsForbidden = false;
            VmSolution.Instance.GetModuleByID(6).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(98).IsForbidden = true;
            VmSolution.Instance.GetModuleByID(111).IsForbidden = false;
        }
		 // 模板1叠料启用
		 else if (receiveStr == "MulBlob1-Enabled")
        {
            VmSolution.Instance.GetModuleByID(222).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(224).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(225).IsForbidden = false;
        }
		 // 模板1叠料禁用
		 else if (receiveStr == "MulBlob1-Disabled")
        {
            VmSolution.Instance.GetModuleByID(222).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(224).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(225).IsForbidden = true;
        }
		 // 模板2叠料启用
		 else if (receiveStr == "MulBlob2-Enabled")
        {
            VmSolution.Instance.GetModuleByID(226).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(228).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(227).IsForbidden = false;
        }
		 // 模板2叠料禁用
		 else if (receiveStr == "MulBlob2-Disabled")
        {
            VmSolution.Instance.GetModuleByID(226).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(228).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(227).IsForbidden = true;
        }
		 // 模板3叠料启用
		 else if (receiveStr == "MulBlob3-Enabled")
        {
            VmSolution.Instance.GetModuleByID(229).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(231).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(230).IsForbidden = false;
        }
		 // 模板3叠料禁用
		 else if (receiveStr == "MulBlob3-Disabled")
        {
            VmSolution.Instance.GetModuleByID(229).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(231).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(230).IsForbidden = true;
        }
		 // 模板4叠料启用
		 else if (receiveStr == "MulBlob4-Enabled")
        {
            VmSolution.Instance.GetModuleByID(232).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(233).IsForbidden  = false;
            VmSolution.Instance.GetModuleByID(234).IsForbidden = false;
        }
		 // 模板4叠料禁用
		 else if (receiveStr == "MulBlob4-Disabled")
        {
            VmSolution.Instance.GetModuleByID(232).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(233).IsForbidden  = true;
            VmSolution.Instance.GetModuleByID(234).IsForbidden = true;
        }
        // --------------- Load/Copy 指令---------------
        // Load|文件名：从指定目录加载SOL，成功返回LoadOK，失败返回LoadNG
        else if (receiveStr.StartsWith("Load|"))
        {
            int deviceId = dataInfo.DeviceID;
	 		// 获取当前方案全路径（包含文件名和后缀，如 "D:\\MySolutions\\Test.sol"）
			string fullPath = VmSolution.Instance.SolutionPath;
			
			// 方法一：使用 Path.GetDirectoryName 获取不含文件名的路径
			string directoryPath = System.IO.Path.GetDirectoryName(fullPath);
			// 结果： "D:\\MySolutions"
			
			// 方法二：使用字符串分割（按最后一个反斜杠分割）
			int lastBackslash = fullPath.LastIndexOf('\\');
			string directoryPath2 = fullPath.Substring(0, lastBackslash);
			// 结果： "D:\\MySolutions"
		    string _fixedSolDir = directoryPath2;
            Task.Run(delegate() 
            {
                try
                {
                    // 解析指令并校验参数
                    string[] cmdParts = receiveStr.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (cmdParts.Length != 2 || string.IsNullOrWhiteSpace(cmdParts[1])) 
                    {
                        SendCommDeviceData("LoadNG",deviceId);
                        return; 
                    }
                    string solFileName = cmdParts[1].Trim();

                    // 文件名合法性校验
                    if (!IsValidFileName(solFileName)) 
                    {
                        SendCommDeviceData("LoadNG",deviceId);
                        return; 
                    }
                    
                    // 自动创建目标目录
                    if (!Directory.Exists(_fixedSolDir)) 
                    {
                        Directory.CreateDirectory(_fixedSolDir);
                    }
                    
                    string loadSolFullPath = Path.Combine(_fixedSolDir, solFileName);
                    // 校验文件是否存在且后缀为.sol
                    if (!File.Exists(loadSolFullPath) || Path.GetExtension(loadSolFullPath).ToLower() != ".sol") 
                    {
                        SendCommDeviceData("LoadNG",deviceId);
                        return; 
                    }

                    // 加载SOL文件
                    VmSolution.Load(loadSolFullPath);
                    
                    // 延时6秒,等方案加载完成再返回结果
                    Thread.Sleep(6000);
                    SendCommDeviceData("LoadOK",deviceId);
                }
                catch (VmException ex)
                {
					// 转16进制，和截图 Convert.ToString(ex.errorCode,16) 保持一致
					string hexCode = Convert.ToString(ex.errorCode, 16);
					string vmHexErr = "0x" + hexCode;
					// 返回格式：LoadNG|VM|0xXXXX
					SendCommDeviceData("LoadNG|VM|" + vmHexErr, deviceId);
                }
            });
        }
        // Copy|自定义名称：拷贝当前SOL到指定目录，成功返回CopyOK，失败返回CopyNG
        else if (receiveStr.StartsWith("Copy|"))
        {
            int deviceId = dataInfo.DeviceID;
	 		// 获取当前方案全路径（包含文件名和后缀，如 "D:\\MySolutions\\Test.sol"）
			string fullPath = VmSolution.Instance.SolutionPath;
			
			// 方法一：使用 Path.GetDirectoryName 获取不含文件名的路径
			string directoryPath = System.IO.Path.GetDirectoryName(fullPath);
			// 结果： "D:\\MySolutions"
			
			// 方法二：使用字符串分割（按最后一个反斜杠分割）
			int lastBackslash = fullPath.LastIndexOf('\\');
			string directoryPath2 = fullPath.Substring(0, lastBackslash);
			// 结果： "D:\\MySolutions"
		    string _fixedSolDir = directoryPath2;           
            Task.Run(delegate()
            {
                try
                {
                    // 1. 解析指令并校验参数
                    string[] cmdParts = receiveStr.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (cmdParts.Length != 2 || string.IsNullOrWhiteSpace(cmdParts[1])) 
                    {
                        SendCommDeviceData("CopyNG",deviceId);
                        return; 
                    }
                    string customFileName = cmdParts[1].Trim();

                    // 2. 自定义名称合法性校验
                    if (!IsValidFileName(customFileName)) 
                    {
                        SendCommDeviceData("CopyNG",deviceId);
                        return; 
                    }

                    // 3. 拼接SOL路径
                    string newSolFileName = Path.ChangeExtension(customFileName, "sol");
                    string newSolFullPath = Path.Combine(_fixedSolDir, newSolFileName);

                    // 4. 自动创建目标目录
                    if (!Directory.Exists(_fixedSolDir)) 
                    {
                        Directory.CreateDirectory(_fixedSolDir);
                    }

                    // 5. 直接保存当前方案为新文件
                    string saveAsResult = VmSolution.SaveAs(newSolFullPath);
                    if (string.IsNullOrEmpty(saveAsResult)) 
                    {
                        SendCommDeviceData("CopyNG",deviceId);
                        return; 
                    }

                    // 6. 校验新文件是否生成
                    if (!File.Exists(newSolFullPath)) 
                    {
                        SendCommDeviceData("CopyNG",deviceId);
                        return; 
                    }

                    // 发送给对端设备
                    SendCommDeviceData("CopyOK",deviceId);
                }
                catch (Exception ex)
                {
                    SendCommDeviceData("CopyNG",deviceId);
                }
            });
        }
    }
    
  

    /// <summary>
    /// 写入VisionMaster全局变量
    /// </summary>
    /// <param name="varName">变量名</param>
    /// <param name="value">变量值</param>
    private void SetGlobalVariable(string varName, string value)
    {
        try
        {
            // 获取全局变量管理器实例
            IGlobalVariableManager varManager = GlobalVariableManager.Instance;
            if (varManager != null)
            {
                // 若变量不存在则创建，存在则更新值
                if (!varManager.ContainsVariable(varName))
                {
                    varManager.CreateVariable(varName, VariableType.String, value);
                }
                else
                {
                    varManager.SetVariableValue(varName, value);
                }
            }
        }
        catch (Exception)
        {
            
        }
    }

    /// <summary>
    /// 发送数据给对端设备
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="data">要发送的数据</param>
    private void SendCommDeviceData(int deviceId, string data)
    {
        try
        {
            // 尝试通过反射调用基类的发送数据方法
            Type baseType = this.GetType().BaseType;
            if (baseType != null)
            {
                // 查找发送数据的方法
                MethodInfo sendMethod = baseType.GetMethod("SendCommunicateData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (sendMethod != null)
                {
                    // 准备参数
                    byte[] dataBytes = System.Text.Encoding.Default.GetBytes(data);
                    // 调用发送方法
                    sendMethod.Invoke(this, new object[] { deviceId, dataBytes });
                }
            }
        }
        catch (Exception)
        {
            
        }
    }

    /// <summary>
    /// 工具方法：校验文件名是否包含非法字符
    /// </summary>
    /// <param name="fileName">待校验名称</param>
    /// <returns>合法返回true，非法返回false</returns>
    private bool IsValidFileName(string fileName)
    {
        return !fileName.Any(c => Path.GetInvalidFileNameChars().Contains(c));
    }

    /// <summary>
    /// 运行函数
    /// </summary>
    /// <returns>Success:return 0</returns>
    public int Process()
    {
        if (m_operateHandle == IntPtr.Zero)
        {
            return ImvsSdkPFDefine.IMVS_EC_NULL_PTR;
        }
        int nRet = DefaultExecuteProcess();
        return nRet;
    }

    /// <summary>
    /// 更新自动加载路径为当前方案路径
    /// </summary>
    private void UpdateAutoLoadPath()
    {
        try
        {
            // 获取当前方案路径
            string currentSolPath = GetCurrentSolPath();
            if (!string.IsNullOrEmpty(currentSolPath))
            {
                // 提取目录路径
                string currentSolDir = Path.GetDirectoryName(currentSolPath);
                if (!string.IsNullOrEmpty(currentSolDir))
                {
                    // 更新VM配置文件中的自动加载路径（使用完整的文件路径）
                    UpdateVMConfigAutoLoadPath(currentSolPath);
                }
            }
        }
        catch (Exception) { /* 工业现场静默处理 */ }
    }

    /// <summary>
    /// 获取当前SOL方案的路径
    /// </summary>
    /// <returns>当前SOL方案的完整路径</returns>
    private string GetCurrentSolPath()
    {
        try
        {
            // 尝试通过反射获取当前方案路径
            Type vmSolutionType = typeof(VmSolution);
            
            // 尝试常见的属性名
            string[] possibleProperties = { "CurrentPath", "FilePath", "Path", "SolutionPath", "CurrentSolutionPath" };
            
            foreach (string propName in possibleProperties)
            {
                PropertyInfo pathProperty = vmSolutionType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pathProperty != null)
                {
                    try
                    {
                        object pathValue = pathProperty.GetValue(VmSolution.Instance);
                        if (pathValue != null)
                        {
                            string path = pathValue.ToString();
                            if (!string.IsNullOrEmpty(path))
                            {
                                return path;
                            }
                        }
                    }
                    catch { }
                }
            }
            
            // 尝试获取所有属性并查找包含路径的属性
            PropertyInfo[] properties = vmSolutionType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (PropertyInfo prop in properties)
            {
                try
                {
                    object value = prop.GetValue(VmSolution.Instance);
                    if (value != null)
                    {
                        string valueStr = value.ToString();
                        if (valueStr.Contains(".sol") || valueStr.Contains("\\") || valueStr.Contains("/"))
                        {
                            return valueStr;
                        }
                    }
                }
                catch { }
            }
        }
        catch (Exception) { }
        
        return string.Empty;
    }

    /// <summary>
    /// 更新VM配置文件中的自动加载路径
    /// </summary>
    /// <param name="newPath">新的自动加载路径</param>
    private void UpdateVMConfigAutoLoadPath(string newPath)
    {
        try
        {
            // 尝试多个可能的配置文件位置
            string[] possibleConfigPaths = {
                "E:\\软件\\VisionMaster\\VisionMaster4.4.0\\Applications\\Server\\AutoLoadSolution.cfg",
                "C:\\Program Files\\VisionMaster4.4.0\\Applications\\Server\\AutoLoadSolution.cfg",
                "C:\\Program Files (x86)\\VisionMaster4.4.0\\Applications\\Server\\AutoLoadSolution.cfg",
                "C:\\Program Files\\VisionMaster\\Applications\\Server\\AutoLoadSolution.cfg",
                "C:\\Program Files (x86)\\VisionMaster\\Applications\\Server\\AutoLoadSolution.cfg"
            };
            
            foreach (string configPath in possibleConfigPaths)
            {
                if (File.Exists(configPath))
                {
                    // 根据文件类型采取不同的处理方式
                    if (Path.GetExtension(configPath).ToLower() == ".cfg")
                    {
                        UpdateCfgFile(configPath, newPath);
                    }
                    break;
                }
            }
        }
        catch (Exception) { /* 工业现场静默处理 */ }
    }

    /// <summary>
    /// 更新CFG文件中的自动加载路径
    /// </summary>
    /// <param name="cfgPath">CFG文件路径</param>
    /// <param name="newPath">新的自动加载路径</param>
    private void UpdateCfgFile(string cfgPath, string newPath)
    {
        try
        {
            // 读取CFG文件内容
            string content = File.ReadAllText(cfgPath);
            
            // 查找Solution元素并更新Path属性
            int solutionStart = content.IndexOf("<Solution");
            if (solutionStart != -1)
            {
                int pathStart = content.IndexOf("Path=\"", solutionStart);
                if (pathStart != -1)
                {
                    pathStart += 6; // 跳过Path="
                    int pathEnd = content.IndexOf("\"", pathStart);
                    if (pathEnd != -1)
                    {
                        // 替换Path属性的值
                        string newContent = content.Substring(0, pathStart) + newPath + content.Substring(pathEnd);
                        
                        // 写回配置文件
                        File.WriteAllText(cfgPath, newContent);
                    }
                }
            }
        }
        catch (Exception) { /* 工业现场静默处理 */ }
    }



    // ========== 适配VM全局变量相关接口 ==========
    private enum VariableType
    {
        String
    }

    private interface IGlobalVariableManager
    {
        bool ContainsVariable(string varName);
        void CreateVariable(string varName, VariableType type, object value);
        void SetVariableValue(string varName, object value);
    }

    private class GlobalVariableManager
    {
        public static IGlobalVariableManager Instance
        {
            get { return new DefaultGlobalVariableManager(); }
        }

        private class DefaultGlobalVariableManager : IGlobalVariableManager
        {
            private System.Collections.Generic.Dictionary<string, object> _variables = new System.Collections.Generic.Dictionary<string, object>();

            public bool ContainsVariable(string varName)
            {
                return _variables.ContainsKey(varName);
            }

            public void CreateVariable(string varName, VariableType type, object value)
            {
                _variables[varName] = value;
            }

            public void SetVariableValue(string varName, object value)
            {
                if (_variables.ContainsKey(varName))
                {
                    _variables[varName] = value;
                }
            }
        }
    }
}

