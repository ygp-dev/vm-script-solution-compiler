using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using Apps.Json;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Win32;
using VM.GlobalScript.Methods;
using VM.Utility;
using VMGlobalScript;

namespace VM.GlobalScript.Support
{
	// Token: 0x0200002B RID: 43
	public class UserGlobalScriptManger
	{
		// Token: 0x17000035 RID: 53
		// (get) Token: 0x06000123 RID: 291 RVA: 0x00008BB4 File Offset: 0x00006DB4
		public List<ShellRefrences> DefaultRefrences
		{
			get
			{
				bool flag = this._defaultRefrences == null;
				if (flag)
				{
					this._defaultRefrences = new List<ShellRefrences>();
					this._defaultRefrences.Add(new ShellRefrences
					{
						Name = "mscorlib.dll",
						refrencesType = 0
					});
					this._defaultRefrences.Add(new ShellRefrences
					{
						Name = "System.dll",
						refrencesType = 0
					});
					this._defaultRefrences.Add(new ShellRefrences
					{
						Name = "System.Core.dll",
						refrencesType = 0
					});
					this._defaultRefrences.Add(new ShellRefrences
					{
						Name = "System.Drawing.dll",
						refrencesType = 0
					});
					this._defaultRefrences.Add(new ShellRefrences
					{
						Name = "System.Windows.Forms.dll",
						refrencesType = 0
					});
					this._defaultRefrences.Add(new ShellRefrences
					{
						Name = "VM.GlobalScript.Methods.dll",
						refrencesType = 1
					});
					this._defaultRefrences.Add(new ShellRefrences
					{
						Name = "iMVS-6000PlatformSDKCS.dll",
						refrencesType = 1
					});
					this._defaultRefrences.Add(new ShellRefrences
					{
						Name = "Apps.Json.dll",
						refrencesType = 1
					});
					bool isSingleProcessMode = this.IsSingleProcessMode;
					if (isSingleProcessMode)
					{
						this._defaultRefrences.Add(new ShellRefrences
						{
							Name = "VM.Core.dll",
							refrencesType = 6
						});
						this._defaultRefrences.Add(new ShellRefrences
						{
							Name = "VM.PlatformSDKCS.dll",
							refrencesType = 7
						});
						this._defaultRefrences.Add(new ShellRefrences
						{
							Name = "VMControls.BaseInterface.dll",
							refrencesType = 6
						});
						this._defaultRefrences.Add(new ShellRefrences
						{
							Name = "VMControls.Interface.dll",
							refrencesType = 6
						});
						this._defaultRefrences.Add(new ShellRefrences
						{
							Name = "VMControls.RenderInterface.dll",
							refrencesType = 6
						});
						this._defaultRefrences.Add(new ShellRefrences
						{
							Name = "ImageSourceModuleCs.dll",
							refrencesType = 6
						});
						this._defaultRefrences.Add(new ShellRefrences
						{
							Name = "IMVSFastFeatureMatchModuCs.dll",
							refrencesType = 6
						});
					}
				}
				return this._defaultRefrences;
			}
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x06000124 RID: 292 RVA: 0x00008E24 File Offset: 0x00007024
		public string AppBaseDirectory
		{
			get
			{
				bool flag = string.IsNullOrEmpty(this.appBaseDirectory);
				if (flag)
				{
					bool isSingleProcessMode = this.IsSingleProcessMode;
					if (isSingleProcessMode)
					{
						string text = Assembly.GetExecutingAssembly().Location;
						text = text.Substring(0, text.LastIndexOf('\\') + 1);
						DirectoryInfo directoryInfo = new DirectoryInfo(text);
						this.VMBaseDllPath = directoryInfo.Parent.FullName;
						this.appBaseDirectory = text;
					}
					else
					{
						this.appBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
					}
				}
				return this.appBaseDirectory;
			}
		}

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x06000125 RID: 293 RVA: 0x00008EAC File Offset: 0x000070AC
		private string VMRegisterPath
		{
			get
			{
				bool flag = string.IsNullOrEmpty(this.vmregistrPath);
				if (flag)
				{
					this.vmregistrPath = this.GetRegistrykeyPath() + "\\";
				}
				return this.vmregistrPath;
			}
		}

		// Token: 0x06000126 RID: 294 RVA: 0x00008EEC File Offset: 0x000070EC
		public UserGlobalScriptManger(bool isSingleMode = false)
		{
			this.IsSingleProcessMode = isSingleMode;
			this.globalScriptDataContext = new GlobalScriptDataContext();
			this.strDefaultGlobalScript = this.AppBaseDirectory + "GlobalScript.txt";
			this.strBackGlobalScript = this.AppBaseDirectory + "GlobalScript.temp";
			this.compileMutex = new Mutex();
			this.objPlatFormSdkManager = new PlatFormSDKManager();
			this.objPlatFormSdkManager.SetRunMode(this.IsSingleProcessMode ? 1 : 0);
			UserGlobalScriptSupport.GetScriptInstance().IsUsePlatformSDK = this.IsSingleProcessMode;
			UserGlobalScriptSupport.GetScriptInstance().AppBaseDirectory = this.AppBaseDirectory;
			UserGlobalScriptSupport.GetScriptInstance().UpdateUIScriptEvent += this.SendDataToServer;
			UserGlobalScriptSupport.GetScriptInstance().VMRegisterPath = this.VMRegisterPath;
			bool flag = this.m_CheckScriptTime == null;
			if (flag)
			{
				this.m_CheckScriptTime = new System.Timers.Timer(2000.0);
				this.m_CheckScriptTime.AutoReset = true;
				this.m_CheckScriptTime.Elapsed += this.m_CheckScriptTime_Elapsed;
				this.m_CheckScriptTime.Enabled = false;
			}
			bool isSingleProcessMode = this.IsSingleProcessMode;
			if (isSingleProcessMode)
			{
				string text = ConfigurationHelper.INIGetStringValue("CompileWaitTime", this.AppBaseDirectory);
				bool flag2 = !string.IsNullOrEmpty(text);
				if (flag2)
				{
					int.TryParse(text, out this.nCompileWaitTime);
				}
			}
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00009194 File Offset: 0x00007394
		private string GetRegistrykeyPath()
		{
			string result = string.Empty;
			try
			{
				RegistryKey localMachine = Registry.LocalMachine;
				bool flag = localMachine != null;
				if (flag)
				{
					RegistryKey registryKey = localMachine.OpenSubKey(this._registrykeyPathName);
					bool flag2 = registryKey != null;
					if (flag2)
					{
						object value = registryKey.GetValue("");
						string text;
						bool flag3 = value != null && (text = (value as string)) != null;
						if (flag3)
						{
							bool flag4 = string.IsNullOrWhiteSpace(text);
							if (flag4)
							{
								return result;
							}
							bool flag5 = Directory.Exists(text);
							if (flag5)
							{
								result = text;
							}
						}
						registryKey.Close();
					}
					localMachine.Close();
				}
			}
			catch
			{
			}
			return result;
		}

		// Token: 0x06000128 RID: 296 RVA: 0x0000924C File Offset: 0x0000744C
		public void SetSdkHandel(IntPtr handle)
		{
			this.SdkBaseHandle = handle;
		}

		// Token: 0x06000129 RID: 297 RVA: 0x00009258 File Offset: 0x00007458
		public void InitZmqToServer(string ServerPairAddr, int receiveTime, int writeTime, bool bzmqType)
		{
			bool flag = string.IsNullOrEmpty(ServerPairAddr) || !ServerPairAddr.Contains(":");
			if (flag)
			{
				LogHelper.Error("ReportPairAddr is null");
			}
			else
			{
				ZmqDataContext zmqDataContext = new ZmqDataContext
				{
					ConnectionString = ServerPairAddr,
					RcvTimout = receiveTime,
					Encod = Encoding.UTF8,
					ServerOrClient = true,
					WriteTimeOut = writeTime,
					StartReceiveTask = false
				};
				if (bzmqType)
				{
					zmqDataContext.ZmqType = 1;
					this.objZmqToServer = new HkrMqCommunicate(zmqDataContext);
				}
				else
				{
					zmqDataContext.ZmqType = 0;
					this.objZmqToServer = new ZmqCommunicate(zmqDataContext);
				}
				bool flag2 = this.objZmqToServer.InitCommuncate();
				bool flag3 = flag2;
				if (flag3)
				{
					LogHelper.Info("Create Zmqserver Succeed:" + ServerPairAddr);
				}
				else
				{
					LogHelper.Error("Create Zmqserver Faild:" + ServerPairAddr);
				}
			}
		}

		// Token: 0x0600012A RID: 298 RVA: 0x00009340 File Offset: 0x00007540
		private void SendDataToServer(string commandType, int errorCode, string path = "")
		{
			try
			{
				object obj = this.lockObject;
				lock (obj)
				{
					object obj2 = new
					{
						command = CMDStatusWithUI.ReportData,
						type = commandType,
						msg = path,
						errorCode = errorCode
					};
					if (UserGlobalScriptManger.<>o__50.<>p__1 == null)
					{
						UserGlobalScriptManger.<>o__50.<>p__1 = CallSite<Func<CallSite, object, bool>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, bool> target = UserGlobalScriptManger.<>o__50.<>p__1.Target;
					CallSite <>p__ = UserGlobalScriptManger.<>o__50.<>p__1;
					if (UserGlobalScriptManger.<>o__50.<>p__0 == null)
					{
						UserGlobalScriptManger.<>o__50.<>p__0 = CallSite<Func<CallSite, object, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.NotEqual, typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
						}));
					}
					bool flag2 = target(<>p__, UserGlobalScriptManger.<>o__50.<>p__0.Target(UserGlobalScriptManger.<>o__50.<>p__0, obj2, null));
					if (flag2)
					{
						object arg = new
						{
							head = obj2
						};
						if (UserGlobalScriptManger.<>o__50.<>p__3 == null)
						{
							UserGlobalScriptManger.<>o__50.<>p__3 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(UserGlobalScriptManger)));
						}
						Func<CallSite, object, string> target2 = UserGlobalScriptManger.<>o__50.<>p__3.Target;
						CallSite <>p__2 = UserGlobalScriptManger.<>o__50.<>p__3;
						if (UserGlobalScriptManger.<>o__50.<>p__2 == null)
						{
							UserGlobalScriptManger.<>o__50.<>p__2 = CallSite<Func<CallSite, Type, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(CSharpBinderFlags.None, "SerializeObject", null, typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsStaticType, null),
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						string text = target2(<>p__2, UserGlobalScriptManger.<>o__50.<>p__2.Target(UserGlobalScriptManger.<>o__50.<>p__2, typeof(JsonConvert), arg));
						bool flag3 = this.objZmqToServer != null;
						if (flag3)
						{
							bool flag4 = !this.objZmqToServer.SendData(text);
							if (flag4)
							{
								LogHelper.Error("Global script report error failed");
							}
							else
							{
								LogHelper.Info("Global script report error msg " + text);
							}
						}
						else
						{
							this.objPlatFormSdkManager.ReportData(text);
						}
					}
					bool flag5 = commandType == "updateScript" && !string.IsNullOrEmpty(path) && File.Exists(path);
					if (flag5)
					{
						this.globalScriptDataContext.GlobalScriptContent = File.ReadAllText(path, Encoding.UTF8);
						this.updateBackFile(this.globalScriptDataContext.GlobalScriptPassword, this.globalScriptDataContext.GlobalScriptContent);
					}
					LogHelper.Error(string.Format("Global script report command:{0},errorcode:{1} ", commandType, errorCode));
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("Global script send report data error " + ex.Message);
			}
		}

		// Token: 0x0600012B RID: 299 RVA: 0x000095F8 File Offset: 0x000077F8
		public uint LoadSolution(string filePath, bool isCrash)
		{
			uint num = 0U;
			bool flag = string.IsNullOrEmpty(filePath);
			uint result;
			if (flag)
			{
				result = 3758096899U;
			}
			else
			{
				try
				{
					bool flag2 = !isCrash;
					if (flag2)
					{
						bool flag3 = filePath != "null";
						if (flag3)
						{
							num = this.readScriptDataFromMapFile(filePath);
						}
					}
				}
				catch (Exception ex)
				{
					LogHelper.Error("load solution falid,error:" + ex.Message);
					return 3758096900U;
				}
				result = num;
			}
			return result;
		}

		// Token: 0x0600012C RID: 300 RVA: 0x0000967C File Offset: 0x0000787C
		public uint SaveSolution(out string fileSavePath)
		{
			uint result = 0U;
			fileSavePath = this.AppBaseDirectory + "Global_0.txt";
			try
			{
				string value = JsonConvert.SerializeObject(new SaveInfo
				{
					ScriptPassword = this.globalScriptDataContext.GlobalScriptPassword,
					ScriptContent = this.globalScriptDataContext.GlobalScriptContent,
					ScriptRefences = this.globalScriptDataContext.GlobalScriptRefences
				});
				using (StreamWriter streamWriter = new StreamWriter(fileSavePath, false, Encoding.UTF8))
				{
					streamWriter.Write(value);
				}
			}
			catch (Exception ex)
			{
				result = 3758096898U;
				LogHelper.Error("save solution falid,error:" + ex.Message);
			}
			return result;
		}

		// Token: 0x0600012D RID: 301 RVA: 0x00009750 File Offset: 0x00007950
		public uint SaveSolutionByMap(out string fileSavePath)
		{
			uint result = 0U;
			string text = JsonConvert.SerializeObject(new SaveInfo
			{
				ScriptPassword = this.globalScriptDataContext.GlobalScriptPassword,
				ScriptContent = this.globalScriptDataContext.GlobalScriptContent,
				ScriptRefences = this.globalScriptDataContext.GlobalScriptRefences
			});
			bool isSingleProcessMode = this.IsSingleProcessMode;
			if (isSingleProcessMode)
			{
				fileSavePath = text;
				LogHelper.Info("save solution byte len:" + Encoding.UTF8.GetByteCount(fileSavePath));
			}
			else
			{
				bool flag = this.bSmGlobalProfix;
				if (flag)
				{
					fileSavePath = string.Format("Global\\GlobalScriptSaveSol_{0}", Process.GetCurrentProcess().Id);
				}
				else
				{
					fileSavePath = string.Format("GlobalScriptSaveSol_{0}", Process.GetCurrentProcess().Id);
				}
				try
				{
					result = this.WriteToMemory(Encoding.UTF8.GetBytes(text), fileSavePath);
				}
				catch (Exception ex)
				{
					result = 3758096898U;
					LogHelper.Error("save solution failed,error:" + ex.Message);
				}
			}
			return result;
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00009878 File Offset: 0x00007A78
		public uint ReleaseShaleMap(string shareMemory)
		{
			try
			{
				this.ReleaseMapFileHandle();
			}
			catch (Exception ex)
			{
				LogHelper.Error("release map memory falid,error:" + ex.Message);
				return 3758096639U;
			}
			return 0U;
		}

		// Token: 0x0600012F RID: 303 RVA: 0x000098C8 File Offset: 0x00007AC8
		private void ReleaseMapFileHandle()
		{
			bool flag = this.hBufferView != IntPtr.Zero;
			if (flag)
			{
				MemoryHelper.UnmapViewOfFile(this.hBufferView);
				this.hBufferView = IntPtr.Zero;
			}
			bool flag2 = this.hShareMemoryHandle != IntPtr.Zero;
			if (flag2)
			{
				MemoryHelper.CloseHandle(this.hShareMemoryHandle);
				this.hShareMemoryHandle = IntPtr.Zero;
			}
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00009930 File Offset: 0x00007B30
		private uint WriteToMemory(byte[] byteData, string fileName)
		{
			IntPtr hFile = new IntPtr(-1);
			int num = byteData.Length + 16 + 64;
			try
			{
				this.ReleaseMapFileHandle();
				this.hShareMemoryHandle = MemoryHelper.CreateFileMapping(hFile, IntPtr.Zero, 4, 0, num, fileName);
				bool flag = this.hShareMemoryHandle == IntPtr.Zero;
				if (flag)
				{
					LogHelper.Error("create filemap error");
					return 3758096405U;
				}
				this.hBufferView = MemoryHelper.MapViewOfFile(this.hShareMemoryHandle, 2, 0, 0, new IntPtr(num));
				bool flag2 = this.hBufferView == IntPtr.Zero;
				if (flag2)
				{
					MemoryHelper.CloseHandle(this.hShareMemoryHandle);
					LogHelper.Error("create MapViewOfFile error");
					return 3758096405U;
				}
				byte[] bytes = BitConverter.GetBytes(num);
				Marshal.Copy(bytes, 0, this.hBufferView, bytes.Length);
				Marshal.Copy(byteData, 0, this.hBufferView + 16, byteData.Length);
			}
			catch (Exception ex)
			{
				LogHelper.Error("create MapViewOfFile error," + ex.Message);
				return 3758096639U;
			}
			return 0U;
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00009A78 File Offset: 0x00007C78
		private bool WaiteLoadCompileEnd()
		{
			bool flag = this.isLoadSol;
			if (flag)
			{
				object obj = this.loadlock;
				lock (obj)
				{
					bool flag3 = !this.isLoadSol;
					if (flag3)
					{
						return true;
					}
					bool flag4 = !this.lodResetEvent.WaitOne(this.nCompileWaitTime * 1000);
					if (flag4)
					{
						return false;
					}
					this.isLoadSol = false;
				}
			}
			return true;
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00009B0C File Offset: 0x00007D0C
		public uint StartOnce()
		{
			bool flag = this.bContinueRunProcess || this.bSeliceExecuteProcess;
			uint result;
			if (flag)
			{
				this.objPlatFormSdkManager.ReportData("execute error:" + -536870113.ToString());
				result = 0U;
			}
			else
			{
				Task.Run(delegate()
				{
					this.bRunOnceProcess = true;
					try
					{
						bool flag2 = !this.WaiteLoadCompileEnd();
						if (flag2)
						{
							LogHelper.Error("StartOnce is time out, waite for compile end");
						}
						this.initSourceBeforeRunScript();
						bool flag3 = !this.globalScriptDataContext.IsComplieOK;
						if (flag3)
						{
							this.SendDataToServer("report", -536870112, "");
							this.bRunOnceProcess = false;
							return;
						}
						bool flag4 = UserGlobalScriptSupport.GetScriptInstance().CodeInitFunction(false, false);
						if (flag4)
						{
							bool flag5 = !UserGlobalScriptSupport.GetScriptInstance().LoadExternAssembly(false, false);
							if (flag5)
							{
								this.SendDataToServer("report", -536870112, "");
								this.bRunOnceProcess = false;
								return;
							}
							int num = 0;
							LogHelper.Info("GlobalScript execute once start");
							bool flag6 = !UserGlobalScriptSupport.GetScriptInstance().CodeRun(ref num);
							if (flag6)
							{
								LogHelper.Error("GlobalScript execute faild " + num);
								bool flag7 = (long)num != 0L;
								if (flag7)
								{
									this.objPlatFormSdkManager.ReportData("execute error:" + num.ToString());
								}
							}
							else
							{
								LogHelper.Info("GlobalScript execute once end");
							}
						}
					}
					catch (Exception ex)
					{
						LogHelper.Error("GlobalScript execute once error " + ex.Message);
					}
					this.bRunOnceProcess = false;
				});
				result = 0U;
			}
			return result;
		}

		// Token: 0x06000133 RID: 307 RVA: 0x00009B70 File Offset: 0x00007D70
		public uint ExecuteLoadInit()
		{
			uint result = 0U;
			this.nLoadInitCount++;
			bool flag = this.nLoadInitCount > 65535;
			if (flag)
			{
				this.nLoadInitCount = 1;
			}
			Task.Run(delegate()
			{
				this.loadInitAction(this.nLoadInitCount);
			});
			return result;
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00009BC0 File Offset: 0x00007DC0
		private void loadInitAction(int nLoadCount)
		{
			bool flag = nLoadCount > this.nLoadSolCount + 1;
			if (!flag)
			{
				bool flag2 = this.WaiteLoadCompileEnd();
				if (flag2)
				{
					LogHelper.Info("ExecuteLoadInit start");
					UserGlobalScriptSupport.GetScriptInstance().LoadSolutionInit();
				}
				else
				{
					LogHelper.Error("ExecuteLoadInit is time out");
				}
			}
		}

		// Token: 0x06000135 RID: 309 RVA: 0x00009C14 File Offset: 0x00007E14
		private string GetConfig(string key)
		{
			string text = string.Empty;
			Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			AppSettingsSection appSettings = configuration.AppSettings;
			bool flag = appSettings == null || appSettings.Settings == null || appSettings.Settings.Count == 0 || appSettings.Settings[key] == null;
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				text = appSettings.Settings[key].Value;
				result = text;
			}
			return result;
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00009C88 File Offset: 0x00007E88
		public uint SilentlyExecuteOnce(string executeMode = "")
		{
			uint ret = 0U;
			int num;
			bool flag = int.TryParse(executeMode, out num);
			if (flag)
			{
				this.nModuSlientExecuteMode = num;
				LogHelper.Info(string.Format("SilentlyExecuteOnce {0}", executeMode));
			}
			Task.Run(delegate()
			{
				LogHelper.Info("SilentlyExecuteOnce start");
				try
				{
					this.bSeliceExecuteProcess = true;
					this.initSourceAndStartScript(false, true, false);
					this.SendDataToServer("SilentExecuteStart", 0, "");
					ret = (uint)this.objPlatFormSdkManager.SilentlyExecuteOnce(this.nModuSlientExecuteMode);
				}
				catch (Exception ex)
				{
					ret = 3758096408U;
					LogHelper.Error("SilentlyExecuteOnce is exception:" + ex.ToString());
				}
				finally
				{
					this.bSeliceExecuteProcess = false;
				}
				this.SendDataToServer("SilentExecuteEnd", 0, "");
				LogHelper.Info("SilentlyExecuteOnce end");
			});
			return ret;
		}

		// Token: 0x06000137 RID: 311 RVA: 0x00009CEC File Offset: 0x00007EEC
		public uint StopExcute()
		{
			bool flag = this.bContinueRunProcess;
			if (flag)
			{
				this.bContinueRunWhile = false;
				this.runResetEvent.Set();
			}
			this.StopAllProcess();
			return 0U;
		}

		// Token: 0x06000138 RID: 312 RVA: 0x00009D25 File Offset: 0x00007F25
		public void StopAllProcess()
		{
			Task.Run(delegate()
			{
				int num = this.objPlatFormSdkManager.StopRunAllProcess();
				LogHelper.Info("StopRunAllProcess return " + num);
			});
		}

		// Token: 0x06000139 RID: 313 RVA: 0x00009D3C File Offset: 0x00007F3C
		public uint StartContinueRun()
		{
			bool flag = this.bContinueRunProcess || this.bRunOnceProcess || this.bSeliceExecuteProcess;
			uint result;
			if (flag)
			{
				LogHelper.Info("Continue Task is Run");
				this.objPlatFormSdkManager.ReportData("execute error:" + -536870113.ToString());
				result = 0U;
			}
			else
			{
				this.bContinueRunProcess = true;
				this.runResetEvent.Reset();
				Task.Run(() => this.ContinueRun());
				result = 0U;
			}
			return result;
		}

		// Token: 0x0600013A RID: 314 RVA: 0x00009DC4 File Offset: 0x00007FC4
		private async Task ContinueRun()
		{
			try
			{
				bool bError = false;
				bool flag = !this.WaiteLoadCompileEnd();
				if (flag)
				{
					LogHelper.Error("ContinueRun is time out, waite for compile end");
				}
				this.initSourceBeforeRunScript();
				bool flag2 = !this.globalScriptDataContext.IsComplieOK;
				if (flag2)
				{
					this.SendDataToServer("report", -536870112, "");
					this.bContinueRunProcess = false;
				}
				else
				{
					bool flag3 = !UserGlobalScriptSupport.GetScriptInstance().CodeInitFunction(true, this.bCrashFlag);
					if (!flag3)
					{
						bool flag4 = this.bCrashFlag;
						if (flag4)
						{
							this.bCrashFlag = false;
						}
						LogHelper.Info("GlobalScript continueExecute begin");
						int errorInfo = 0;
						this.bContinueRunWhile = true;
						uint executeInterval = UserGlobalScriptSupport.GetScriptInstance().GetScriptContinusExecuteInterval();
						bool flag5 = !UserGlobalScriptSupport.GetScriptInstance().LoadExternAssembly(false, false);
						if (flag5)
						{
							this.SendDataToServer("report", -536870112, "");
						}
						else
						{
							int nRunCount = 0;
							while (this.bContinueRunWhile)
							{
								bool flag6 = !UserGlobalScriptSupport.GetScriptInstance().CodeRun(ref errorInfo);
								if (flag6)
								{
									bool flag7 = !bError;
									if (flag7)
									{
										bError = true;
										LogHelper.Error("Script continueExecute faild " + errorInfo);
										bool flag8 = (long)errorInfo != 0L;
										if (flag8)
										{
											this.objPlatFormSdkManager.ReportData("execute error:" + errorInfo.ToString());
											break;
										}
									}
								}
								bool flag9 = nRunCount == 0;
								if (flag9)
								{
									int num = nRunCount;
									nRunCount = num + 1;
								}
								this.runResetEvent.WaitOne((int)executeInterval);
							}
						}
					}
				}
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				bool flag10 = ex is TaskCanceledException;
				if (flag10)
				{
					LogHelper.Info("Global script cancel continue task");
				}
				else
				{
					LogHelper.Error("Global script continueRun faild " + ex.Message);
				}
			}
			finally
			{
				this.bContinueRunProcess = false;
			}
		}

		// Token: 0x0600013B RID: 315 RVA: 0x00009E0C File Offset: 0x0000800C
		private void DisposeScriptObject()
		{
			try
			{
				UserGlobalScriptSupport.GetScriptInstance().DisposeAndUnload();
			}
			catch (Exception ex)
			{
				LogHelper.Error("DisposeScriptObject is error:" + ex.ToString());
			}
			finally
			{
			}
		}

		// Token: 0x0600013C RID: 316 RVA: 0x00009E68 File Offset: 0x00008068
		public uint CloseScript(bool bUnsdk = false)
		{
			this.bContinueRunWhile = false;
			bool flag = this.bContinueRunProcess;
			if (flag)
			{
				this.bContinueRunProcess = false;
				this.runResetEvent.Set();
			}
			this.bRunOnceProcess = false;
			this.bSeliceExecuteProcess = false;
			this.globalScriptDataContext.GlobalScriptContent = string.Empty;
			this.globalScriptDataContext.GlobalScriptPassword = string.Empty;
			this.globalScriptDataContext.GlobalScriptComplieResult = string.Empty;
			this.globalScriptDataContext.IsComplieFinish = false;
			this.globalScriptDataContext.GlobalScriptRefences.Clear();
			this.DisposeScriptObject();
			this.objPlatFormSdkManager.Dispose();
			this.globalScriptDataContext.IsComplieOK = false;
			this.bCrashFlag = false;
			bool flag2 = this.m_CheckScriptTime != null;
			if (flag2)
			{
				this.m_CheckScriptTime.Enabled = false;
			}
			if (bUnsdk)
			{
				bool flag3 = !this.IsSingleProcessMode;
				if (flag3)
				{
					this.objPlatFormSdkManager.UinitSDK();
					LogHelper.Info("Global script cancel UinitSDK end");
					bool flag4 = this.objZmqToServer != null;
					if (flag4)
					{
						this.objZmqToServer.Dispose();
					}
				}
				bool flag5 = this.m_CheckScriptTime != null;
				if (flag5)
				{
					this.bStartCheck = false;
					this.m_CheckScriptTime.Enabled = false;
					this.m_CheckScriptTime.Dispose();
					this.m_CheckScriptTime = null;
				}
			}
			this.ReleaseMapFileHandle();
			return 0U;
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00009FD0 File Offset: 0x000081D0
		public uint SetVMZmqPair(string pairAddress)
		{
			LogHelper.Info("SetVMZmqPair pairAddress:" + pairAddress);
			bool flag = string.IsNullOrEmpty(pairAddress);
			if (flag)
			{
				pairAddress = "null";
			}
			return 0U;
		}

		// Token: 0x0600013E RID: 318 RVA: 0x0000A007 File Offset: 0x00008207
		public void SetCommunicateData(IntPtr ptrData)
		{
			this.objPlatFormSdkManager.Enqueue(ptrData);
		}

		// Token: 0x0600013F RID: 319 RVA: 0x0000A017 File Offset: 0x00008217
		public void LoadDefaultSolution()
		{
			this.initSourceAndStartScript(true, false, false);
		}

		// Token: 0x06000140 RID: 320 RVA: 0x0000A024 File Offset: 0x00008224
		public void LoadRecoverSolution()
		{
			this.globalScriptDataContext.IsComplieFinish = false;
			Task.Run(delegate()
			{
				LogHelper.Info("Get data from back file");
				bool flag = this.readScriptDataFromFile(this.strBackGlobalScript);
				bool flag2 = !flag;
				if (flag2)
				{
					this.globalScriptDataContext.GlobalScriptContent = string.Empty;
					this.globalScriptDataContext.GlobalScriptPassword = string.Empty;
				}
				this.initSourceAndStartScript(false, false, false);
			});
		}

		// Token: 0x06000141 RID: 321 RVA: 0x0000A048 File Offset: 0x00008248
		private void compileGlobalScript(string scriptContent, bool bCompile = false)
		{
			bool flag = string.IsNullOrEmpty(scriptContent);
			if (!flag)
			{
				bool flag2 = !this.globalScriptDataContext.IsComplieOK || bCompile;
				if (flag2)
				{
					LogHelper.Info("Compile begin");
					bool isComplieOK = false;
					this.globalScriptDataContext.GlobalScriptComplieResult = UserGlobalScriptSupport.GetScriptInstance().CompileCode(scriptContent, out isComplieOK);
					this.globalScriptDataContext.IsComplieOK = isComplieOK;
					bool isComplieOK2 = this.globalScriptDataContext.IsComplieOK;
					if (isComplieOK2)
					{
						this.updateBackFile(this.globalScriptDataContext.GlobalScriptPassword, scriptContent);
					}
					LogHelper.Info("Compile end");
				}
			}
		}

		// Token: 0x06000142 RID: 322 RVA: 0x0000A0DC File Offset: 0x000082DC
		private void initSourceAndStartScript(bool bCompile = false, bool bSclienceRunonce = false, bool bLoadSol = false)
		{
			this.compileMutex.WaitOne();
			try
			{
				this.objPlatFormSdkManager.InitPlatformSDKEx(this.ClientCommAddr, this.ServerRepAddr, this.ServerPid, this.SdkBaseHandle);
				bool flag = !bSclienceRunonce;
				if (flag)
				{
					bool flag2 = this.globalScriptDataContext.IsComplieFinish && !bLoadSol;
					if (flag2)
					{
						LogHelper.Info("InitSourceEndStartScript already init");
					}
					else
					{
						bool flag3 = this.readDefaultScript();
						if (flag3)
						{
							this.compileGlobalScript(this.globalScriptDataContext.GlobalScriptContent, bCompile);
							if (bLoadSol)
							{
								this.lodResetEvent.Set();
							}
							this.globalScriptDataContext.IsComplieFinish = true;
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("InitSourceEndStartScript Error," + ex.Message);
			}
			finally
			{
				this.compileMutex.ReleaseMutex();
			}
		}

		// Token: 0x06000143 RID: 323 RVA: 0x0000A1D8 File Offset: 0x000083D8
		private void initSourceBeforeRunScript()
		{
			this.initSourceAndStartScript(false, false, false);
		}

		// Token: 0x06000144 RID: 324 RVA: 0x0000A1E8 File Offset: 0x000083E8
		private bool readDefaultScript()
		{
			bool flag = string.IsNullOrEmpty(this.globalScriptDataContext.GlobalScriptContent);
			if (flag)
			{
				bool flag2 = !string.IsNullOrEmpty(this.globalScriptDataContext.GlobalScriptDefault);
				if (flag2)
				{
					this.globalScriptDataContext.GlobalScriptContent = this.globalScriptDataContext.GlobalScriptDefault;
					this.globalScriptDataContext.GlobalScriptRefences = this.DefaultRefrences;
					this.setScriptCodeAndRefrences(this.globalScriptDataContext.GlobalScriptContent, this.DefaultRefrences);
					return true;
				}
				bool flag3 = !File.Exists(this.strDefaultGlobalScript);
				if (flag3)
				{
					LogHelper.Error("Default Script File Is Not Exit");
					return false;
				}
				try
				{
					this.globalScriptDataContext.GlobalScriptContent = File.ReadAllText(this.strDefaultGlobalScript, Encoding.UTF8);
					this.globalScriptDataContext.GlobalScriptDefault = this.globalScriptDataContext.GlobalScriptContent;
					this.globalScriptDataContext.GlobalScriptRefences = this.DefaultRefrences;
					this.setScriptCodeAndRefrences(this.globalScriptDataContext.GlobalScriptContent, this.DefaultRefrences);
					return true;
				}
				catch (Exception ex)
				{
					LogHelper.Error("Read Script File Error," + ex.Message);
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000145 RID: 325 RVA: 0x0000A32C File Offset: 0x0000852C
		public uint SetMsgFromUI(string strCommandMsg)
		{
			bool flag = string.IsNullOrEmpty(strCommandMsg);
			uint result;
			if (flag)
			{
				LogHelper.Error("SetMsgFromUI Error,strCommandMsg is null");
				result = 3758096385U;
			}
			else
			{
				Task.Run(delegate()
				{
					this.compileMutex.WaitOne();
					string text = string.Empty;
					try
					{
						LogHelper.Error(strCommandMsg);
						object arg = JsonConvert.DeserializeObject(strCommandMsg);
						if (UserGlobalScriptManger.<>o__86.<>p__1 == null)
						{
							UserGlobalScriptManger.<>o__86.<>p__1 = CallSite<Func<CallSite, object, bool>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, bool> target = UserGlobalScriptManger.<>o__86.<>p__1.Target;
						CallSite <>p__ = UserGlobalScriptManger.<>o__86.<>p__1;
						if (UserGlobalScriptManger.<>o__86.<>p__0 == null)
						{
							UserGlobalScriptManger.<>o__86.<>p__0 = CallSite<Func<CallSite, object, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.NotEqual, typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
							}));
						}
						bool flag2 = target(<>p__, UserGlobalScriptManger.<>o__86.<>p__0.Target(UserGlobalScriptManger.<>o__86.<>p__0, arg, null));
						if (flag2)
						{
							if (UserGlobalScriptManger.<>o__86.<>p__4 == null)
							{
								UserGlobalScriptManger.<>o__86.<>p__4 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(UserGlobalScriptManger)));
							}
							Func<CallSite, object, string> target2 = UserGlobalScriptManger.<>o__86.<>p__4.Target;
							CallSite <>p__2 = UserGlobalScriptManger.<>o__86.<>p__4;
							if (UserGlobalScriptManger.<>o__86.<>p__3 == null)
							{
								UserGlobalScriptManger.<>o__86.<>p__3 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "command", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							Func<CallSite, object, object> target3 = UserGlobalScriptManger.<>o__86.<>p__3.Target;
							CallSite <>p__3 = UserGlobalScriptManger.<>o__86.<>p__3;
							if (UserGlobalScriptManger.<>o__86.<>p__2 == null)
							{
								UserGlobalScriptManger.<>o__86.<>p__2 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "head", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							string value = target2(<>p__2, target3(<>p__3, UserGlobalScriptManger.<>o__86.<>p__2.Target(UserGlobalScriptManger.<>o__86.<>p__2, arg)));
							CMDStatusWithUI cmdstatusWithUI = (CMDStatusWithUI)Convert.ToInt32(value);
							bool flag3 = cmdstatusWithUI == CMDStatusWithUI.SetRefenceAssembly;
							if (flag3)
							{
								if (UserGlobalScriptManger.<>o__86.<>p__8 == null)
								{
									UserGlobalScriptManger.<>o__86.<>p__8 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(UserGlobalScriptManger)));
								}
								Func<CallSite, object, string> target4 = UserGlobalScriptManger.<>o__86.<>p__8.Target;
								CallSite <>p__4 = UserGlobalScriptManger.<>o__86.<>p__8;
								if (UserGlobalScriptManger.<>o__86.<>p__7 == null)
								{
									UserGlobalScriptManger.<>o__86.<>p__7 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(CSharpBinderFlags.None, "ToString", null, typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
									}));
								}
								Func<CallSite, object, object> target5 = UserGlobalScriptManger.<>o__86.<>p__7.Target;
								CallSite <>p__5 = UserGlobalScriptManger.<>o__86.<>p__7;
								if (UserGlobalScriptManger.<>o__86.<>p__6 == null)
								{
									UserGlobalScriptManger.<>o__86.<>p__6 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "refrences", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
									}));
								}
								Func<CallSite, object, object> target6 = UserGlobalScriptManger.<>o__86.<>p__6.Target;
								CallSite <>p__6 = UserGlobalScriptManger.<>o__86.<>p__6;
								if (UserGlobalScriptManger.<>o__86.<>p__5 == null)
								{
									UserGlobalScriptManger.<>o__86.<>p__5 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "body", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
									}));
								}
								string value2 = target4(<>p__4, target5(<>p__5, target6(<>p__6, UserGlobalScriptManger.<>o__86.<>p__5.Target(UserGlobalScriptManger.<>o__86.<>p__5, arg))));
								this.globalScriptDataContext.GlobalScriptRefences = JsonConvert.DeserializeObject<List<ShellRefrences>>(value2);
								bool flag4 = this.globalScriptDataContext.GlobalScriptRefences != null;
								if (flag4)
								{
									UserGlobalScriptSupport.GetScriptInstance().SetRefrences(this.GetRefrences());
								}
								this.setScriptToSolutionRefrence(this.globalScriptDataContext.GlobalScriptRefences);
							}
							else
							{
								bool flag5 = cmdstatusWithUI == CMDStatusWithUI.SetGlobalScript;
								if (flag5)
								{
									GlobalScriptDataContext globalScriptDataContext = this.globalScriptDataContext;
									if (UserGlobalScriptManger.<>o__86.<>p__11 == null)
									{
										UserGlobalScriptManger.<>o__86.<>p__11 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(UserGlobalScriptManger)));
									}
									Func<CallSite, object, string> target7 = UserGlobalScriptManger.<>o__86.<>p__11.Target;
									CallSite <>p__7 = UserGlobalScriptManger.<>o__86.<>p__11;
									if (UserGlobalScriptManger.<>o__86.<>p__10 == null)
									{
										UserGlobalScriptManger.<>o__86.<>p__10 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "password", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
										{
											CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
										}));
									}
									Func<CallSite, object, object> target8 = UserGlobalScriptManger.<>o__86.<>p__10.Target;
									CallSite <>p__8 = UserGlobalScriptManger.<>o__86.<>p__10;
									if (UserGlobalScriptManger.<>o__86.<>p__9 == null)
									{
										UserGlobalScriptManger.<>o__86.<>p__9 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "body", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
										{
											CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
										}));
									}
									globalScriptDataContext.GlobalScriptPassword = target7(<>p__7, target8(<>p__8, UserGlobalScriptManger.<>o__86.<>p__9.Target(UserGlobalScriptManger.<>o__86.<>p__9, arg)));
									if (UserGlobalScriptManger.<>o__86.<>p__14 == null)
									{
										UserGlobalScriptManger.<>o__86.<>p__14 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(UserGlobalScriptManger)));
									}
									Func<CallSite, object, string> target9 = UserGlobalScriptManger.<>o__86.<>p__14.Target;
									CallSite <>p__9 = UserGlobalScriptManger.<>o__86.<>p__14;
									if (UserGlobalScriptManger.<>o__86.<>p__13 == null)
									{
										UserGlobalScriptManger.<>o__86.<>p__13 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "filePath", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
										{
											CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
										}));
									}
									Func<CallSite, object, object> target10 = UserGlobalScriptManger.<>o__86.<>p__13.Target;
									CallSite <>p__10 = UserGlobalScriptManger.<>o__86.<>p__13;
									if (UserGlobalScriptManger.<>o__86.<>p__12 == null)
									{
										UserGlobalScriptManger.<>o__86.<>p__12 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "body", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
										{
											CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
										}));
									}
									text = target9(<>p__9, target10(<>p__10, UserGlobalScriptManger.<>o__86.<>p__12.Target(UserGlobalScriptManger.<>o__86.<>p__12, arg)));
									bool flag6 = string.IsNullOrEmpty(text) || !File.Exists(text);
									if (!flag6)
									{
										this.globalScriptDataContext.GlobalScriptComplieResult = "";
										this.globalScriptDataContext.GlobalScriptContent = File.ReadAllText(text, Encoding.UTF8);
										this.setScriptToSolutionFile(this.globalScriptDataContext.GlobalScriptContent);
										bool isComplieOK = false;
										this.globalScriptDataContext.GlobalScriptComplieResult = UserGlobalScriptSupport.GetScriptInstance().CompileCode(this.globalScriptDataContext.GlobalScriptContent, out isComplieOK);
										this.globalScriptDataContext.IsComplieOK = isComplieOK;
										bool isComplieOK2 = this.globalScriptDataContext.IsComplieOK;
										if (isComplieOK2)
										{
											this.updateBackFile(this.globalScriptDataContext.GlobalScriptPassword, this.globalScriptDataContext.GlobalScriptContent);
										}
									}
								}
								else
								{
									bool flag7 = cmdstatusWithUI == CMDStatusWithUI.SetGlobalScriptForSave;
									if (flag7)
									{
										GlobalScriptDataContext globalScriptDataContext2 = this.globalScriptDataContext;
										if (UserGlobalScriptManger.<>o__86.<>p__17 == null)
										{
											UserGlobalScriptManger.<>o__86.<>p__17 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(UserGlobalScriptManger)));
										}
										Func<CallSite, object, string> target11 = UserGlobalScriptManger.<>o__86.<>p__17.Target;
										CallSite <>p__11 = UserGlobalScriptManger.<>o__86.<>p__17;
										if (UserGlobalScriptManger.<>o__86.<>p__16 == null)
										{
											UserGlobalScriptManger.<>o__86.<>p__16 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "password", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
											{
												CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
											}));
										}
										Func<CallSite, object, object> target12 = UserGlobalScriptManger.<>o__86.<>p__16.Target;
										CallSite <>p__12 = UserGlobalScriptManger.<>o__86.<>p__16;
										if (UserGlobalScriptManger.<>o__86.<>p__15 == null)
										{
											UserGlobalScriptManger.<>o__86.<>p__15 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "body", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
											{
												CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
											}));
										}
										globalScriptDataContext2.GlobalScriptPassword = target11(<>p__11, target12(<>p__12, UserGlobalScriptManger.<>o__86.<>p__15.Target(UserGlobalScriptManger.<>o__86.<>p__15, arg)));
										if (UserGlobalScriptManger.<>o__86.<>p__20 == null)
										{
											UserGlobalScriptManger.<>o__86.<>p__20 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(UserGlobalScriptManger)));
										}
										Func<CallSite, object, string> target13 = UserGlobalScriptManger.<>o__86.<>p__20.Target;
										CallSite <>p__13 = UserGlobalScriptManger.<>o__86.<>p__20;
										if (UserGlobalScriptManger.<>o__86.<>p__19 == null)
										{
											UserGlobalScriptManger.<>o__86.<>p__19 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "filePath", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
											{
												CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
											}));
										}
										Func<CallSite, object, object> target14 = UserGlobalScriptManger.<>o__86.<>p__19.Target;
										CallSite <>p__14 = UserGlobalScriptManger.<>o__86.<>p__19;
										if (UserGlobalScriptManger.<>o__86.<>p__18 == null)
										{
											UserGlobalScriptManger.<>o__86.<>p__18 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "body", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
											{
												CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
											}));
										}
										text = target13(<>p__13, target14(<>p__14, UserGlobalScriptManger.<>o__86.<>p__18.Target(UserGlobalScriptManger.<>o__86.<>p__18, arg)));
										bool flag8 = string.IsNullOrEmpty(text) || !File.Exists(text);
										if (!flag8)
										{
											this.globalScriptDataContext.GlobalScriptComplieResult = "";
											this.globalScriptDataContext.GlobalScriptContent = File.ReadAllText(text, Encoding.UTF8);
											this.setScriptToSolutionFile(this.globalScriptDataContext.GlobalScriptContent);
										}
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						LogHelper.Error("SetMsgFromUI Error," + ex.Message);
						this.globalScriptDataContext.IsComplieOK = false;
					}
					finally
					{
						bool flag9 = File.Exists(text);
						if (flag9)
						{
							File.Delete(text);
						}
						this.compileMutex.ReleaseMutex();
					}
				});
				result = 0U;
			}
			return result;
		}

		// Token: 0x06000146 RID: 326 RVA: 0x0000A388 File Offset: 0x00008588
		private string[] GetRefrences()
		{
			List<string> arrayRefrences = new List<string>();
			this.globalScriptDataContext.GlobalScriptRefences.ForEach(delegate(ShellRefrences x)
			{
				bool flag = x.refrencesType == 1;
				if (flag)
				{
					string text = this.AppBaseDirectory + x.Name;
					bool flag2 = !File.Exists(text);
					if (flag2)
					{
						DLLNameMap dllnameMap = this.mapNameInfo.FirstOrDefault((DLLNameMap p) => p.StdDLLName == x.Name);
						bool flag3 = dllnameMap != null;
						if (flag3)
						{
							string text2 = this.AppBaseDirectory + dllnameMap.NeuDLLName;
							bool flag4 = File.Exists(text2);
							if (flag4)
							{
								text = text2;
								x.Name = dllnameMap.NeuDLLName;
							}
						}
					}
					arrayRefrences.Add(text);
				}
				else
				{
					bool flag5 = x.refrencesType == 2;
					if (flag5)
					{
						arrayRefrences.Add(this.AppBaseDirectory + "DLL\\" + x.Name);
					}
					else
					{
						bool flag6 = x.refrencesType == 6;
						if (flag6)
						{
							string text3 = this.VMRegisterPath + x.Name;
							bool flag7 = !File.Exists(text3);
							if (flag7)
							{
								DLLNameMap dllnameMap2 = this.mapNameInfo.FirstOrDefault((DLLNameMap p) => p.StdDLLName == x.Name);
								bool flag8 = dllnameMap2 != null;
								if (flag8)
								{
									string text4 = this.VMRegisterPath + dllnameMap2.NeuDLLName;
									bool flag9 = File.Exists(text4);
									if (flag9)
									{
										text3 = text4;
										x.Name = dllnameMap2.NeuDLLName;
									}
								}
							}
							arrayRefrences.Add(text3);
						}
						else
						{
							bool flag10 = x.refrencesType == 7;
							if (flag10)
							{
								string text5 = this.VMBaseDllPath + "\\PublicFile\\x64\\VM.PlatformSDKCS.dll";
								bool flag11 = !File.Exists(text5);
								if (flag11)
								{
									DLLNameMap dllnameMap3 = this.mapNameInfo.FirstOrDefault((DLLNameMap p) => p.StdDLLName == x.Name);
									bool flag12 = dllnameMap3 != null;
									if (flag12)
									{
										string text6 = string.Format("{0}\\PublicFile\\x64\\{1}", this.VMBaseDllPath, dllnameMap3.NeuDLLName);
										bool flag13 = File.Exists(text6);
										if (flag13)
										{
											text5 = text6;
											x.Name = dllnameMap3.NeuDLLName;
										}
									}
								}
								arrayRefrences.Add(text5);
							}
							else
							{
								arrayRefrences.Add(x.Name);
							}
						}
					}
				}
			});
			return arrayRefrences.ToArray();
		}

		// Token: 0x06000147 RID: 327 RVA: 0x0000A3DC File Offset: 0x000085DC
		public uint GetMsgToUI(string strCommandMsg, ref string returnStr)
		{
			uint num = 0U;
			object obj = null;
			object arg = JsonConvert.DeserializeObject(strCommandMsg);
			if (UserGlobalScriptManger.<>o__88.<>p__1 == null)
			{
				UserGlobalScriptManger.<>o__88.<>p__1 = CallSite<Func<CallSite, object, bool>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			Func<CallSite, object, bool> target = UserGlobalScriptManger.<>o__88.<>p__1.Target;
			CallSite <>p__ = UserGlobalScriptManger.<>o__88.<>p__1;
			if (UserGlobalScriptManger.<>o__88.<>p__0 == null)
			{
				UserGlobalScriptManger.<>o__88.<>p__0 = CallSite<Func<CallSite, object, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Equal, typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
				}));
			}
			bool flag = target(<>p__, UserGlobalScriptManger.<>o__88.<>p__0.Target(UserGlobalScriptManger.<>o__88.<>p__0, arg, null));
			if (flag)
			{
				num = 3758096649U;
			}
			else
			{
				try
				{
					if (UserGlobalScriptManger.<>o__88.<>p__4 == null)
					{
						UserGlobalScriptManger.<>o__88.<>p__4 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(UserGlobalScriptManger)));
					}
					Func<CallSite, object, string> target2 = UserGlobalScriptManger.<>o__88.<>p__4.Target;
					CallSite <>p__2 = UserGlobalScriptManger.<>o__88.<>p__4;
					if (UserGlobalScriptManger.<>o__88.<>p__3 == null)
					{
						UserGlobalScriptManger.<>o__88.<>p__3 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "command", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, object> target3 = UserGlobalScriptManger.<>o__88.<>p__3.Target;
					CallSite <>p__3 = UserGlobalScriptManger.<>o__88.<>p__3;
					if (UserGlobalScriptManger.<>o__88.<>p__2 == null)
					{
						UserGlobalScriptManger.<>o__88.<>p__2 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "head", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					string value = target2(<>p__2, target3(<>p__3, UserGlobalScriptManger.<>o__88.<>p__2.Target(UserGlobalScriptManger.<>o__88.<>p__2, arg)));
					CMDStatusWithUI cmdstatusWithUI = (CMDStatusWithUI)Convert.ToInt32(value);
					bool flag2 = cmdstatusWithUI == CMDStatusWithUI.GetGlobalScript;
					if (flag2)
					{
						num = this.getScriptInfo(ref obj);
					}
					else
					{
						bool flag3 = cmdstatusWithUI == CMDStatusWithUI.Getcomplie;
						if (flag3)
						{
							num = this.getScriptCompileInfo(ref obj);
						}
					}
				}
				catch (Exception ex)
				{
					LogHelper.Error("GetMsgToUI Error," + ex.Message);
					num = 3758096639U;
				}
			}
			if (UserGlobalScriptManger.<>o__88.<>p__6 == null)
			{
				UserGlobalScriptManger.<>o__88.<>p__6 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "command", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			Func<CallSite, object, object> target4 = UserGlobalScriptManger.<>o__88.<>p__6.Target;
			CallSite <>p__4 = UserGlobalScriptManger.<>o__88.<>p__6;
			if (UserGlobalScriptManger.<>o__88.<>p__5 == null)
			{
				UserGlobalScriptManger.<>o__88.<>p__5 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "head", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			object command = target4(<>p__4, UserGlobalScriptManger.<>o__88.<>p__5.Target(UserGlobalScriptManger.<>o__88.<>p__5, arg));
			string type = "response";
			if (UserGlobalScriptManger.<>o__88.<>p__8 == null)
			{
				UserGlobalScriptManger.<>o__88.<>p__8 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "seqId", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			Func<CallSite, object, object> target5 = UserGlobalScriptManger.<>o__88.<>p__8.Target;
			CallSite <>p__5 = UserGlobalScriptManger.<>o__88.<>p__8;
			if (UserGlobalScriptManger.<>o__88.<>p__7 == null)
			{
				UserGlobalScriptManger.<>o__88.<>p__7 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "head", typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			object head = new
			{
				command = command,
				type = type,
				seqId = target5(<>p__5, UserGlobalScriptManger.<>o__88.<>p__7.Target(UserGlobalScriptManger.<>o__88.<>p__7, arg)),
				errorCode = num,
				errorDesc = ErrorCode.GetErrorInfo(num)
			};
			if (UserGlobalScriptManger.<>o__88.<>p__10 == null)
			{
				UserGlobalScriptManger.<>o__88.<>p__10 = CallSite<Func<CallSite, object, bool>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			Func<CallSite, object, bool> target6 = UserGlobalScriptManger.<>o__88.<>p__10.Target;
			CallSite <>p__6 = UserGlobalScriptManger.<>o__88.<>p__10;
			if (UserGlobalScriptManger.<>o__88.<>p__9 == null)
			{
				UserGlobalScriptManger.<>o__88.<>p__9 = CallSite<Func<CallSite, object, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.NotEqual, typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
				}));
			}
			bool flag4 = target6(<>p__6, UserGlobalScriptManger.<>o__88.<>p__9.Target(UserGlobalScriptManger.<>o__88.<>p__9, obj, null));
			object arg2;
			if (flag4)
			{
				arg2 = new
				{
					head = head,
					body = obj
				};
			}
			else
			{
				arg2 = new
				{
					head
				};
			}
			if (UserGlobalScriptManger.<>o__88.<>p__12 == null)
			{
				UserGlobalScriptManger.<>o__88.<>p__12 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(UserGlobalScriptManger)));
			}
			Func<CallSite, object, string> target7 = UserGlobalScriptManger.<>o__88.<>p__12.Target;
			CallSite <>p__7 = UserGlobalScriptManger.<>o__88.<>p__12;
			if (UserGlobalScriptManger.<>o__88.<>p__11 == null)
			{
				UserGlobalScriptManger.<>o__88.<>p__11 = CallSite<Func<CallSite, Type, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(CSharpBinderFlags.None, "SerializeObject", null, typeof(UserGlobalScriptManger), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsStaticType, null),
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			returnStr = target7(<>p__7, UserGlobalScriptManger.<>o__88.<>p__11.Target(UserGlobalScriptManger.<>o__88.<>p__11, typeof(JsonConvert), arg2));
			return num;
		}

		// Token: 0x06000148 RID: 328 RVA: 0x0000A8B4 File Offset: 0x00008AB4
		private bool setScriptCodeAndRefrences(string content, List<ShellRefrences> shellRefrences)
		{
			return this.setScriptToSolutionFile(content) && this.setScriptToSolutionRefrence(shellRefrences);
		}

		// Token: 0x06000149 RID: 329 RVA: 0x0000A8DC File Offset: 0x00008ADC
		private bool setScriptToSolutionFile(string content)
		{
			string text = this.AppBaseDirectory + "\\GlobalUserScript\\UserGlobalScript.cs";
			bool flag = !Directory.Exists(this.AppBaseDirectory + "\\GlobalUserScript");
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool flag2 = this.WriteStringToFile(text, content);
				FileInfo fileInfo = new FileInfo(text);
				this.objLastWriteTime = fileInfo.LastWriteTime;
				bool flag3 = this.m_CheckScriptTime != null && !this.m_CheckScriptTime.Enabled;
				if (flag3)
				{
					this.m_CheckScriptTime.Enabled = true;
				}
				this.bStartCheck = true;
				result = flag2;
			}
			return result;
		}

		// Token: 0x0600014A RID: 330 RVA: 0x0000A978 File Offset: 0x00008B78
		private void setDefaultRefrence()
		{
			try
			{
				this.setScriptToSolutionRefrence(new List<ShellRefrences>
				{
					new ShellRefrences
					{
						Name = "mscorlib.dll",
						refrencesType = 0
					},
					new ShellRefrences
					{
						Name = "System.dll",
						refrencesType = 0
					},
					new ShellRefrences
					{
						Name = "System.Core.dll",
						refrencesType = 0
					},
					new ShellRefrences
					{
						Name = "System.Windows.Forms.dll",
						refrencesType = 0
					},
					new ShellRefrences
					{
						Name = "iMVS-6000PlatformSDKCS.dll",
						refrencesType = 1
					},
					new ShellRefrences
					{
						Name = "VM.GlobalScript.Methods.dll",
						refrencesType = 1
					},
					new ShellRefrences
					{
						Name = "Apps.Json.dll",
						refrencesType = 1
					}
				});
			}
			catch (Exception ex)
			{
				LogHelper.Error("setDefaultRefrence exception:" + ex.ToString());
			}
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000AAA4 File Offset: 0x00008CA4
		private bool setScriptToSolutionRefrence(List<ShellRefrences> shellRefrences)
		{
			try
			{
				string filename = this.AppBaseDirectory + "\\GlobalUserScript\\GlobalUserScript.csproj";
				bool flag = !Directory.Exists(this.AppBaseDirectory + "\\GlobalUserScript");
				if (flag)
				{
					return false;
				}
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(filename);
				XmlElement documentElement = xmlDocument.DocumentElement;
				XmlElement xmlElement = null;
				foreach (object obj in documentElement.ChildNodes)
				{
					XmlNode xmlNode = (XmlNode)obj;
					bool flag2 = xmlNode.Name == "ItemGroup";
					if (flag2)
					{
						xmlElement = (XmlElement)xmlNode;
						break;
					}
				}
				bool flag3 = xmlElement == null;
				if (flag3)
				{
					return false;
				}
				xmlElement.RemoveAll();
				foreach (ShellRefrences shellRefrences2 in shellRefrences)
				{
					bool flag4 = shellRefrences2.refrencesType == 0;
					if (flag4)
					{
						XmlElement xmlElement2 = xmlDocument.CreateElement("Reference", documentElement.NamespaceURI);
						XmlAttribute xmlAttribute = xmlDocument.CreateAttribute("Include");
						xmlAttribute.InnerText = shellRefrences2.Name.Substring(0, shellRefrences2.Name.LastIndexOf('.'));
						xmlElement2.SetAttributeNode(xmlAttribute);
						xmlElement.AppendChild(xmlElement2);
					}
					else
					{
						string innerText = "";
						bool flag5 = shellRefrences2.refrencesType == 1;
						if (flag5)
						{
							innerText = "..\\" + shellRefrences2.Name;
						}
						else
						{
							bool flag6 = shellRefrences2.refrencesType == 2;
							if (flag6)
							{
								innerText = "..\\DLL\\" + shellRefrences2.Name;
							}
						}
						XmlElement xmlElement3 = xmlDocument.CreateElement("Reference", documentElement.NamespaceURI);
						XmlAttribute xmlAttribute2 = xmlDocument.CreateAttribute("Include");
						xmlAttribute2.InnerText = shellRefrences2.Name.Substring(0, shellRefrences2.Name.LastIndexOf('.'));
						xmlElement3.SetAttributeNode(xmlAttribute2);
						XmlElement xmlElement4 = xmlDocument.CreateElement("Reference", documentElement.NamespaceURI);
						xmlElement3.AppendChild(this.CreateNewLelment(xmlDocument, "HintPath", innerText, documentElement.NamespaceURI));
						xmlElement3.AppendChild(this.CreateNewLelment(xmlDocument, "Private", "False", documentElement.NamespaceURI));
						xmlElement.AppendChild(xmlElement3);
					}
				}
				xmlDocument.Save(filename);
			}
			catch (Exception ex)
			{
				LogHelper.Error("setScriptToSolutionRefrence exception:" + ex.ToString());
			}
			return true;
		}

		// Token: 0x0600014C RID: 332 RVA: 0x0000AD98 File Offset: 0x00008F98
		private XmlElement CreateNewLelment(XmlDocument doc, string nodeName, string innerText, string namespaceUrl)
		{
			XmlElement xmlElement = doc.CreateElement(nodeName, namespaceUrl);
			xmlElement.InnerText = innerText;
			return xmlElement;
		}

		// Token: 0x0600014D RID: 333 RVA: 0x0000ADC0 File Offset: 0x00008FC0
		private string GetOutDllRefrenceNode(string filepath)
		{
			string result;
			try
			{
				bool flag = !File.Exists(filepath);
				if (flag)
				{
					result = null;
				}
				else
				{
					AssemblyName assemblyName = AssemblyName.GetAssemblyName(filepath);
					string text = string.Format("{0},processorArchitecture={1}", assemblyName.FullName, assemblyName.ProcessorArchitecture.ToString());
					result = text;
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("GetOutDllRefrenceNode Error," + ex.Message);
				result = null;
			}
			return result;
		}

		// Token: 0x0600014E RID: 334 RVA: 0x0000AE44 File Offset: 0x00009044
		private void m_CheckScriptTime_Elapsed(object sender, ElapsedEventArgs e)
		{
			bool flag = !this.bStartCheck;
			if (!flag)
			{
				string text = this.AppBaseDirectory + "\\GlobalUserScript\\UserGlobalScript.cs";
				bool flag2 = File.Exists(text);
				if (flag2)
				{
					FileInfo fileInfo = new FileInfo(text);
					DateTime lastWriteTime = fileInfo.LastWriteTime;
					bool flag3 = lastWriteTime.Subtract(this.objLastWriteTime).TotalSeconds > 2.0;
					if (flag3)
					{
						this.objLastWriteTime = lastWriteTime;
						string text2 = File.ReadAllText(text, Encoding.UTF8);
						bool flag4 = text2 != this.globalScriptDataContext.GlobalScriptContent;
						if (flag4)
						{
							this.globalScriptDataContext.GlobalScriptContent = text2;
							this.SendDataToServer("updateScript", 0, text);
						}
					}
				}
			}
		}

		// Token: 0x0600014F RID: 335 RVA: 0x0000AF08 File Offset: 0x00009108
		private uint getScriptInfo(ref dynamic returnBody)
		{
			uint result = 0U;
			string text = "";
			bool flag = string.IsNullOrEmpty(this.globalScriptDataContext.GlobalScriptContent);
			if (flag)
			{
				bool flag2 = !string.IsNullOrEmpty(this.globalScriptDataContext.GlobalScriptDefault);
				if (flag2)
				{
					this.globalScriptDataContext.GlobalScriptContent = this.globalScriptDataContext.GlobalScriptDefault;
					text = this.AppBaseDirectory + Guid.NewGuid().ToString("N") + "GlobalScript.txt";
					bool flag3 = this.WriteStringToFile(text, this.globalScriptDataContext.GlobalScriptContent);
					bool flag4 = !flag3;
					if (flag4)
					{
						result = 3758096896U;
						text = "";
					}
				}
				else
				{
					bool flag5 = File.Exists(this.strDefaultGlobalScript);
					if (flag5)
					{
						this.globalScriptDataContext.GlobalScriptContent = File.ReadAllText(this.strDefaultGlobalScript, Encoding.UTF8);
						text = this.AppBaseDirectory + Guid.NewGuid().ToString("N") + "GlobalScript.txt";
						File.Copy(this.strDefaultGlobalScript, text);
					}
					else
					{
						result = 3758096896U;
					}
				}
			}
			else
			{
				text = this.AppBaseDirectory + Guid.NewGuid().ToString("N") + "GlobalScript.txt";
				bool flag6 = this.WriteStringToFile(text, this.globalScriptDataContext.GlobalScriptContent);
				bool flag7 = !flag6;
				if (flag7)
				{
					result = 3758096896U;
					text = "";
				}
			}
			returnBody = new
			{
				password = this.globalScriptDataContext.GlobalScriptPassword,
				filePath = text,
				refrences = JsonConvert.SerializeObject(this.globalScriptDataContext.GlobalScriptRefences)
			};
			return result;
		}

		// Token: 0x06000150 RID: 336 RVA: 0x0000B0A4 File Offset: 0x000092A4
		private uint getScriptCompileInfo(ref dynamic returnBody)
		{
			uint result = 0U;
			string text = "";
			bool flag = !string.IsNullOrEmpty(this.globalScriptDataContext.GlobalScriptComplieResult);
			if (flag)
			{
				text = this.AppBaseDirectory + Guid.NewGuid().ToString("N") + "GlobalComplie.txt";
				bool flag2 = this.WriteStringToFile(text, this.globalScriptDataContext.GlobalScriptComplieResult);
				bool flag3 = !flag2;
				if (flag3)
				{
					result = 3758096896U;
					text = "";
				}
			}
			returnBody = new
			{
				filePath = text
			};
			return result;
		}

		// Token: 0x06000151 RID: 337 RVA: 0x0000B134 File Offset: 0x00009334
		private bool WriteStringToFile(string path, string info)
		{
			bool result;
			try
			{
				using (StreamWriter streamWriter = new StreamWriter(path, false, Encoding.UTF8))
				{
					streamWriter.Write(info);
				}
				result = true;
			}
			catch (Exception ex)
			{
				LogHelper.Error("StreamWriter Write File Error," + ex.Message);
				result = false;
			}
			return result;
		}

		// Token: 0x06000152 RID: 338 RVA: 0x0000B1A4 File Offset: 0x000093A4
		private void updateBackFile(string password, string content)
		{
			try
			{
				string value = JsonConvert.SerializeObject(new SaveInfo
				{
					ScriptPassword = password,
					ScriptContent = content
				});
				using (StreamWriter streamWriter = new StreamWriter(this.strBackGlobalScript, false, Encoding.UTF8))
				{
					streamWriter.Write(value);
				}
				LogHelper.Info("Update BackFile end");
			}
			catch (Exception ex)
			{
				LogHelper.Error("Update BackFile is error," + ex.Message);
			}
		}

		// Token: 0x06000153 RID: 339 RVA: 0x0000B240 File Offset: 0x00009440
		private bool readScriptDataFromFile(string filepath)
		{
			bool flag = !File.Exists(filepath);
			bool result;
			if (flag)
			{
				LogHelper.Error("GlobalScript File is not exit,file name:" + filepath);
				result = false;
			}
			else
			{
				try
				{
					string value = File.ReadAllText(filepath, Encoding.UTF8);
					SaveInfo saveInfo = JsonConvert.DeserializeObject<SaveInfo>(value);
					this.globalScriptDataContext.GlobalScriptContent = saveInfo.ScriptContent;
					this.globalScriptDataContext.GlobalScriptPassword = saveInfo.ScriptPassword;
					this.globalScriptDataContext.GlobalScriptRefences = saveInfo.ScriptRefences;
					result = true;
				}
				catch (Exception ex)
				{
					LogHelper.Error("Read script file is error," + ex.Message);
					result = false;
				}
			}
			return result;
		}

		// Token: 0x06000154 RID: 340 RVA: 0x0000B2F0 File Offset: 0x000094F0
		private uint readScriptDataFromMapFile(string filepath)
		{
			bool flag = string.IsNullOrEmpty(filepath);
			uint result;
			if (flag)
			{
				LogHelper.Error("GlobalScript map file is error:" + filepath);
				result = 3758096899U;
			}
			else
			{
				try
				{
					bool isSingleProcessMode = this.IsSingleProcessMode;
					string text;
					if (isSingleProcessMode)
					{
						text = filepath;
					}
					else
					{
						byte[] array = MemoryHelper.VMReadFromMemory(filepath, 16);
						bool flag2 = array == null;
						if (flag2)
						{
							LogHelper.Error("VMReadFromMemory return bytes is null");
							return 3758096899U;
						}
						text = Encoding.UTF8.GetString(array);
					}
					int num = text.IndexOf("{");
					int num2 = text.LastIndexOf("}");
					bool flag3 = num == num2;
					if (flag3)
					{
						LogHelper.Error("startIndex is error");
						result = 3758096899U;
					}
					else
					{
						string value = text.Substring(num, num2 - num + 1);
						SaveInfo saveInfo = JsonConvert.DeserializeObject<SaveInfo>(value);
						this.globalScriptDataContext.GlobalScriptContent = saveInfo.ScriptContent;
						this.globalScriptDataContext.GlobalScriptPassword = saveInfo.ScriptPassword;
						this.globalScriptDataContext.GlobalScriptRefences = saveInfo.ScriptRefences;
						this.globalScriptDataContext.IsComplieFinish = false;
						this.isLoadSol = true;
						Task.Run(delegate()
						{
							bool flag4 = this.globalScriptDataContext.GlobalScriptRefences != null && this.globalScriptDataContext.GlobalScriptRefences.Count > 0;
							if (flag4)
							{
								bool flag5 = !this.IsLoadNeuFile;
								if (flag5)
								{
									this.LoadNeufileName();
									this.IsLoadNeuFile = true;
								}
								UserGlobalScriptSupport.GetScriptInstance().SetRefrences(this.GetRefrences());
								this.setScriptToSolutionRefrence(this.globalScriptDataContext.GlobalScriptRefences);
							}
							else
							{
								this.setScriptToSolutionRefrence(this.DefaultRefrences);
							}
							this.setScriptToSolutionFile(this.globalScriptDataContext.GlobalScriptContent);
							this.initSourceAndStartScript(true, false, true);
							this.nLoadSolCount++;
							bool flag6 = this.nLoadSolCount >= 65535;
							if (flag6)
							{
								this.nLoadSolCount = 0;
							}
						});
						result = 0U;
					}
				}
				catch (Exception ex)
				{
					LogHelper.Error("Read script file is error," + ex.ToString());
					result = 3758096899U;
				}
			}
			return result;
		}

		// Token: 0x06000155 RID: 341 RVA: 0x0000B470 File Offset: 0x00009670
		public void Dispose()
		{
			try
			{
				bool isSingleProcessMode = this.IsSingleProcessMode;
				if (isSingleProcessMode)
				{
					this.CloseScript(true);
					AssemblyManager.ClearAeembly();
				}
				else
				{
					this.CloseScript(true);
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("UserGlobalScriptManger Dispose Error" + ex.Message);
			}
		}

		// Token: 0x06000156 RID: 342 RVA: 0x0000B4D4 File Offset: 0x000096D4
		private void LoadNeufileName()
		{
			string text = this.VMBaseDllPath + "\\Tools\\NEUFILE\\NeuFileNameMap.xml";
			DirectoryInfo directoryInfo = new DirectoryInfo(this.VMRegisterPath);
			bool flag = directoryInfo == null;
			if (!flag)
			{
				bool flag2 = directoryInfo.Parent == null;
				if (!flag2)
				{
					string fullName = directoryInfo.Parent.FullName;
					string text2 = fullName + "\\Tool\\NeuFileNameMap.xml";
					string text3 = "";
					bool flag3 = File.Exists(text);
					if (flag3)
					{
						text3 = text;
					}
					else
					{
						bool flag4 = File.Exists(text2);
						if (flag4)
						{
							text3 = text2;
						}
					}
					bool flag5 = string.IsNullOrEmpty(text3);
					if (!flag5)
					{
						XmlDocument xmlDocument = new XmlDocument();
						xmlDocument.Load(text3);
						XmlElement documentElement = xmlDocument.DocumentElement;
						XmlElement xmlElement = null;
						foreach (object obj in documentElement.ChildNodes)
						{
							XmlNode xmlNode = (XmlNode)obj;
							bool flag6 = xmlNode.Name == "filenames";
							if (flag6)
							{
								xmlElement = (XmlElement)xmlNode;
								break;
							}
						}
						foreach (object obj2 in xmlElement.ChildNodes)
						{
							XmlNode xmlNode2 = (XmlNode)obj2;
							bool flag7 = xmlNode2.Name == "filename";
							if (flag7)
							{
								string stdDLLName = xmlNode2.Attributes["name"].Value + ".dll";
								string neuDLLName = xmlNode2.InnerText + ".dll";
								DLLNameMap item = new DLLNameMap
								{
									StdDLLName = stdDLLName,
									NeuDLLName = neuDLLName
								};
								this.mapNameInfo.Add(item);
							}
						}
					}
				}
			}
		}

		// Token: 0x0400011B RID: 283
		public GlobalScriptDataContext globalScriptDataContext = null;

		// Token: 0x0400011C RID: 284
		public string ClientCommAddr = string.Empty;

		// Token: 0x0400011D RID: 285
		public string ServerRepAddr = string.Empty;

		// Token: 0x0400011E RID: 286
		public int ServerPid = 0;

		// Token: 0x0400011F RID: 287
		public bool bCrashFlag = false;

		// Token: 0x04000120 RID: 288
		public bool bSmGlobalProfix = false;

		// Token: 0x04000121 RID: 289
		private bool bRunOnceProcess = false;

		// Token: 0x04000122 RID: 290
		private bool bContinueRunProcess = false;

		// Token: 0x04000123 RID: 291
		private bool bContinueRunWhile = false;

		// Token: 0x04000124 RID: 292
		private string strDefaultGlobalScript = string.Empty;

		// Token: 0x04000125 RID: 293
		private string strBackGlobalScript = string.Empty;

		// Token: 0x04000126 RID: 294
		private Mutex compileMutex = null;

		// Token: 0x04000127 RID: 295
		private PlatFormSDKManager objPlatFormSdkManager = null;

		// Token: 0x04000128 RID: 296
		private const string strSaveGlobalScriptFileName = "Global_0.txt";

		// Token: 0x04000129 RID: 297
		private const string strDefaultGlobalScriptFileName = "GlobalScript.txt";

		// Token: 0x0400012A RID: 298
		private const string strTempGlobalScriptCompileFileName = "GlobalComplie.txt";

		// Token: 0x0400012B RID: 299
		private const string strTempGlobalScriptFileName = "GlobalScript.temp";

		// Token: 0x0400012C RID: 300
		private BaseZmqCommunicate objZmqToServer = null;

		// Token: 0x0400012D RID: 301
		private object lockObject = new object();

		// Token: 0x0400012E RID: 302
		private bool bSeliceExecuteProcess = false;

		// Token: 0x0400012F RID: 303
		private System.Timers.Timer m_CheckScriptTime = null;

		// Token: 0x04000130 RID: 304
		private DateTime objLastWriteTime;

		// Token: 0x04000131 RID: 305
		private bool bStartCheck = false;

		// Token: 0x04000132 RID: 306
		private AutoResetEvent lodResetEvent = new AutoResetEvent(false);

		// Token: 0x04000133 RID: 307
		private AutoResetEvent runResetEvent = new AutoResetEvent(false);

		// Token: 0x04000134 RID: 308
		public GlobalScriptReportMsg ReportMsgAction = null;

		// Token: 0x04000135 RID: 309
		private int nModuSlientExecuteMode = 1;

		// Token: 0x04000136 RID: 310
		private int nCompileWaitTime = 60;

		// Token: 0x04000137 RID: 311
		private bool isLoadSol = false;

		// Token: 0x04000138 RID: 312
		private const string ReportError = "report";

		// Token: 0x04000139 RID: 313
		private const string ReportUpdateScript = "updateScript";

		// Token: 0x0400013A RID: 314
		private const string ReportSilentExecuteStart = "SilentExecuteStart";

		// Token: 0x0400013B RID: 315
		private const string ReportSilentExecuteEnd = "SilentExecuteEnd";

		// Token: 0x0400013C RID: 316
		private List<ShellRefrences> _defaultRefrences = null;

		// Token: 0x0400013D RID: 317
		private bool IsSingleProcessMode = false;

		// Token: 0x0400013E RID: 318
		private IntPtr SdkBaseHandle = IntPtr.Zero;

		// Token: 0x0400013F RID: 319
		private string VMBaseDllPath = "";

		// Token: 0x04000140 RID: 320
		private string appBaseDirectory = "";

		// Token: 0x04000141 RID: 321
		private string vmregistrPath = "";

		// Token: 0x04000142 RID: 322
		private readonly string _registrykeyPathName = "SOFTWARE\\WOW6432Node\\Microsoft\\.NETFramework\\v4.0.30319\\AssemblyFoldersEx\\VisionMaster";

		// Token: 0x04000143 RID: 323
		private const int INVALID_HANDLE_VALUE = -1;

		// Token: 0x04000144 RID: 324
		private IntPtr hShareMemoryHandle = IntPtr.Zero;

		// Token: 0x04000145 RID: 325
		private IntPtr hBufferView = IntPtr.Zero;

		// Token: 0x04000146 RID: 326
		private const int PAGE_READWRITE = 4;

		// Token: 0x04000147 RID: 327
		private const int FILE_MAP_ALL_ACCESS = 2;

		// Token: 0x04000148 RID: 328
		private object loadlock = new object();

		// Token: 0x04000149 RID: 329
		private int nLoadInitCount = 0;

		// Token: 0x0400014A RID: 330
		private int nLoadSolCount = 0;

		// Token: 0x0400014B RID: 331
		private const int LOADCOUNT = 65535;

		// Token: 0x0400014C RID: 332
		private List<DLLNameMap> mapNameInfo = new List<DLLNameMap>();

		// Token: 0x0400014D RID: 333
		private bool IsLoadNeuFile = false;
	}
}
