using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Apps.Json;
using Microsoft.CSharp.RuntimeBinder;
using VMGlobalScript;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000027 RID: 39
	public class GlobalManager : IDisposable
	{
		// Token: 0x060000E8 RID: 232 RVA: 0x000060AC File Offset: 0x000042AC
		public static GlobalManager GetInstance()
		{
			bool flag = GlobalManager._instance == null;
			if (flag)
			{
				GlobalManager._instance = new GlobalManager();
			}
			return GlobalManager._instance;
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060000E9 RID: 233 RVA: 0x000060D9 File Offset: 0x000042D9
		// (set) Token: 0x060000EA RID: 234 RVA: 0x000060E1 File Offset: 0x000042E1
		public ServerInfo objServerInfo { get; private set; }

		// Token: 0x060000EB RID: 235 RVA: 0x000060EC File Offset: 0x000042EC
		public GlobalManager()
		{
			this.objServerInfo = new ServerInfo();
		}

		// Token: 0x060000EC RID: 236 RVA: 0x00006164 File Offset: 0x00004364
		public int StartByExe(string msg)
		{
			this.IsSingleProcess = false;
			this.objScriptManager = new UserGlobalScriptManger(false);
			return this.StartDealCommandLines(msg) ? 0 : -536870911;
		}

		// Token: 0x060000ED RID: 237 RVA: 0x0000619C File Offset: 0x0000439C
		public int StartByDll(IntPtr sdkHandle)
		{
			this.IsSingleProcess = true;
			this.objScriptManager = new UserGlobalScriptManger(true);
			this.objScriptManager.SetSdkHandel(sdkHandle);
			this.initLoadDefaultScriptTimer();
			return 0;
		}

		// Token: 0x060000EE RID: 238 RVA: 0x000061D8 File Offset: 0x000043D8
		public int HandleMsg(int cmd, IntPtr InMsg, int nMsgLen, ref string reMsg)
		{
			bool isSingleProcess = this.IsSingleProcess;
			int result;
			if (isSingleProcess)
			{
				result = this.HandleCmd(cmd, InMsg, nMsgLen, ref reMsg);
			}
			else
			{
				result = 0;
			}
			return result;
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00006204 File Offset: 0x00004404
		public void RegsiterRepotCallBack(GlobalScriptReportMsg reportMsg)
		{
			bool flag = this.objScriptManager != null;
			if (flag)
			{
				this.objScriptManager.ReportMsgAction = reportMsg;
			}
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00006230 File Offset: 0x00004430
		public bool StartDealCommandLines(string paramers)
		{
			try
			{
				LogHelper.Info(paramers);
				string[] array = paramers.Split(new char[]
				{
					';'
				});
				bool flag = array.Length < 4;
				if (flag)
				{
					return false;
				}
				for (int i = 0; i < array.Length; i++)
				{
					string[] array2 = array[i].Split(new char[]
					{
						':'
					});
					bool flag2 = array2.Length < 2;
					if (flag2)
					{
						break;
					}
					bool flag3 = array2[0] == "ServerPairAddr";
					if (flag3)
					{
						this.objServerInfo.ServerPairAddr = array[i].Substring(array2[0].Length + 1);
					}
					else
					{
						bool flag4 = array2[0] == "ReportPairAddr";
						if (flag4)
						{
							this.objServerInfo.ReportPairAddr = array[i].Substring(array2[0].Length + 1);
						}
						else
						{
							bool flag5 = array2[0] == "ServerRepAddr";
							if (flag5)
							{
								this.objServerInfo.ServerRepAddr = array[i].Substring(array2[0].Length + 1);
								bool flag6 = this.objServerInfo.ServerRepAddr.Contains("tcp");
								if (flag6)
								{
									this.objServerInfo.ServerRepAddr = this.objServerInfo.ServerRepAddr.Substring(this.objServerInfo.ServerRepAddr.LastIndexOf('/') + 1);
								}
								bool flag7 = this.objScriptManager != null;
								if (flag7)
								{
									this.objScriptManager.ServerRepAddr = this.objServerInfo.ServerRepAddr;
								}
							}
							else
							{
								bool flag8 = array2[0] == "ClientCommAddr";
								if (flag8)
								{
									this.objServerInfo.ClientCommAddr = array[i].Substring(array2[0].Length + 1);
									bool flag9 = this.objServerInfo.ClientCommAddr.Contains("tcp");
									if (flag9)
									{
										this.objServerInfo.ClientCommAddr = this.objServerInfo.ClientCommAddr.Substring(this.objServerInfo.ClientCommAddr.LastIndexOf('/') + 1);
									}
									bool flag10 = this.objScriptManager != null;
									if (flag10)
									{
										this.objScriptManager.ClientCommAddr = this.objServerInfo.ClientCommAddr;
									}
								}
								else
								{
									bool flag11 = array2[0] == "ServerName";
									if (flag11)
									{
										this.objServerInfo.ServerName = array2[1];
									}
									else
									{
										bool flag12 = array2[0] == "ServerPid";
										if (flag12)
										{
											int serverPID;
											bool flag13 = int.TryParse(array2[1], out serverPID);
											bool flag14 = flag13;
											if (flag14)
											{
												this.objServerInfo.ServerPID = serverPID;
												bool flag15 = this.objScriptManager != null;
												if (flag15)
												{
													this.objScriptManager.ServerPid = this.objServerInfo.ServerPID;
												}
											}
										}
										else
										{
											bool flag16 = array2[0] == "IsCrash";
											if (flag16)
											{
												this.objServerInfo.IsCrash = (array2[1] == "1");
											}
											else
											{
												bool flag17 = array2[0] == "SmGlobalProfix";
												if (flag17)
												{
													this.objServerInfo.IsSmGlobalProfix = (array2[1] == "1");
													bool flag18 = this.objScriptManager != null;
													if (flag18)
													{
														this.objScriptManager.bSmGlobalProfix = this.objServerInfo.IsSmGlobalProfix;
													}
												}
												else
												{
													bool flag19 = array2[0] == "MQType";
													if (flag19)
													{
														this.bZmqType = (array2[1] == "1");
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				bool flag20 = string.IsNullOrEmpty(this.objServerInfo.ServerPairAddr) || string.IsNullOrEmpty(this.objServerInfo.ServerName) || this.objServerInfo.ServerPID <= 0 || string.IsNullOrEmpty(this.objServerInfo.ClientCommAddr);
				if (flag20)
				{
					LogHelper.Error("Get the command from server is fault,paramers is " + paramers);
					return false;
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("Deal CommandLine Error:" + ex.Message);
				return false;
			}
			this.initZmq();
			this.initLoadDefaultScriptTimer();
			this.initListenTimer();
			bool flag21 = this.objScriptManager != null;
			if (flag21)
			{
				this.objScriptManager.InitZmqToServer(this.objServerInfo.ReportPairAddr, this._iReceiveTime, this._iWriteTime, this.bZmqType);
			}
			bool isCrash = this.objServerInfo.IsCrash;
			if (isCrash)
			{
				this.bNeedLoadDefaultScript = false;
				LogHelper.Info("GlobalScript last is crash,recover globalScript");
				bool flag22 = this.objScriptManager != null;
				if (flag22)
				{
					this.objScriptManager.bCrashFlag = true;
					this.objScriptManager.LoadRecoverSolution();
				}
				else
				{
					LogHelper.Error("objScriptManager is null");
				}
			}
			return true;
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00006738 File Offset: 0x00004938
		private void initLoadDefaultScriptTimer()
		{
			this.objLoadDefaultScriptTime = new Timer((double)this.iLoadDefaultScriptTimer);
			this.objLoadDefaultScriptTime.Elapsed += this.objLoadDefaultScriptTime_Elapsed;
			this.objLoadDefaultScriptTime.AutoReset = false;
			this.objLoadDefaultScriptTime.Enabled = true;
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x0000678C File Offset: 0x0000498C
		private void objLoadDefaultScriptTime_Elapsed(object sender, ElapsedEventArgs e)
		{
			bool flag = this.bNeedLoadDefaultScript;
			if (flag)
			{
				LogHelper.Info("Load default globalScript begin");
				this.objScriptManager.LoadDefaultSolution();
				LogHelper.Info("Load default globalScript end");
			}
			this.bNeedLoadDefaultScript = false;
			this.objLoadDefaultScriptTime.Enabled = false;
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x000067DC File Offset: 0x000049DC
		private void initZmq()
		{
			ZmqDataContext zmqDataContext = new ZmqDataContext
			{
				ConnectionString = this.objServerInfo.ServerPairAddr,
				RcvTimout = this._iReceiveTime,
				Encod = Encoding.UTF8,
				ServerOrClient = true,
				WriteTimeOut = this._iWriteTime,
				StartReceiveTask = true
			};
			bool flag = this.bZmqType;
			if (flag)
			{
				zmqDataContext.ZmqType = 1;
				this.objZmqCom = new HkrMqCommunicate(zmqDataContext);
			}
			else
			{
				zmqDataContext.ZmqType = 0;
				this.objZmqCom = new ZmqCommunicate(zmqDataContext);
			}
			BaseZmqCommunicate baseZmqCommunicate = this.objZmqCom;
			baseZmqCommunicate.GetReceiveData = (Func<string, string>)Delegate.Combine(baseZmqCommunicate.GetReceiveData, new Func<string, string>(this.ZmqGetReceiveData));
			bool flag2 = this.objZmqCom.InitCommuncate();
			bool flag3 = flag2;
			if (flag3)
			{
				LogHelper.Info(string.Format("Create {0} Zmqserver {1} Succeed:", this.bZmqType ? "hkrmq" : "zmq", this.objServerInfo.ServerPairAddr));
			}
			else
			{
				LogHelper.Info(string.Format("Create {0} Zmqserver {1} Faild:", this.bZmqType ? "hkrmq" : "zmq", this.objServerInfo.ServerPairAddr));
			}
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x00006910 File Offset: 0x00004B10
		private void initListenTimer()
		{
			this.objListenServerTime = new Timer((double)this.iListenServerTime);
			this.objListenServerTime.Elapsed += this.objListenServerTime_Elapsed;
			this.objListenServerTime.AutoReset = true;
			this.objListenServerTime.Enabled = true;
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x00006964 File Offset: 0x00004B64
		private void objListenServerTime_Elapsed(object sender, ElapsedEventArgs e)
		{
			bool flag = !this.isCheckServer(this.objServerInfo.ServerName, this.objServerInfo.ServerPID);
			if (flag)
			{
				LogHelper.Error("Check Server.exe is not exit,kill globalscript,serverPid:" + this.objServerInfo.ServerPID);
				this.Dispose();
				LogHelper.Info("Close Global Script over");
				try
				{
					Environment.Exit(0);
				}
				catch (Exception ex)
				{
					LogHelper.Error("Close Global Script error");
				}
			}
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x000069F4 File Offset: 0x00004BF4
		private bool isCheckServer(string processName, int processID)
		{
			bool result;
			try
			{
				Process processById = Process.GetProcessById(processID);
				bool flag = processName.Contains(".");
				if (flag)
				{
					processName = processName.Substring(0, processName.IndexOf('.'));
				}
				bool flag2 = processById.ProcessName == processName;
				if (flag2)
				{
					processById.Close();
					result = true;
				}
				else
				{
					processById.Close();
					LogHelper.Info("Server exe pid is not equal name");
					result = false;
				}
			}
			catch
			{
				result = false;
			}
			return result;
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x00006A78 File Offset: 0x00004C78
		private void initJson()
		{
			Task.Run(delegate()
			{
				try
				{
					string value = "{\"head\":{\"command\":4005,\"description\":\"reserved\",\"type\":\"request\",\"seqId\":2}}";
					object obj = JsonConvert.DeserializeObject(value);
					if (GlobalManager.<>o__30.<>p__2 == null)
					{
						GlobalManager.<>o__30.<>p__2 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(GlobalManager)));
					}
					Func<CallSite, object, string> target = GlobalManager.<>o__30.<>p__2.Target;
					CallSite <>p__ = GlobalManager.<>o__30.<>p__2;
					if (GlobalManager.<>o__30.<>p__1 == null)
					{
						GlobalManager.<>o__30.<>p__1 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "command", typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, object> target2 = GlobalManager.<>o__30.<>p__1.Target;
					CallSite <>p__2 = GlobalManager.<>o__30.<>p__1;
					if (GlobalManager.<>o__30.<>p__0 == null)
					{
						GlobalManager.<>o__30.<>p__0 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					string text = target(<>p__, target2(<>p__2, GlobalManager.<>o__30.<>p__0.Target(GlobalManager.<>o__30.<>p__0, obj)));
					if (GlobalManager.<>o__30.<>p__4 == null)
					{
						GlobalManager.<>o__30.<>p__4 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(GlobalManager)));
					}
					Func<CallSite, object, string> target3 = GlobalManager.<>o__30.<>p__4.Target;
					CallSite <>p__3 = GlobalManager.<>o__30.<>p__4;
					if (GlobalManager.<>o__30.<>p__3 == null)
					{
						GlobalManager.<>o__30.<>p__3 = CallSite<Func<CallSite, Type, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "SerializeObject", null, typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsStaticType, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					string text2 = target3(<>p__3, GlobalManager.<>o__30.<>p__3.Target(GlobalManager.<>o__30.<>p__3, typeof(JsonConvert), obj));
				}
				catch
				{
				}
			});
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x00006AA0 File Offset: 0x00004CA0
		public int HandleCmd(int cmd, IntPtr pInMsg, int nMsgLen, ref string reMsg)
		{
			reMsg = "";
			string text = "";
			CMDStatusWithServer cmdstatusWithServer = (CMDStatusWithServer)Convert.ToInt32(cmd);
			bool flag = cmdstatusWithServer == CMDStatusWithServer.SetCommunicateData;
			if (flag)
			{
				bool flag2 = pInMsg == IntPtr.Zero || nMsgLen <= 0;
				if (flag2)
				{
					return 0;
				}
				this.objScriptManager.SetCommunicateData(pInMsg);
			}
			else
			{
				bool flag3 = pInMsg != IntPtr.Zero && nMsgLen >= 0;
				if (flag3)
				{
					byte[] array = new byte[nMsgLen];
					Marshal.Copy(pInMsg, array, 0, nMsgLen);
					text = Encoding.UTF8.GetString(array);
				}
			}
			LogHelper.Info("Receive:" + cmdstatusWithServer);
			switch (cmdstatusWithServer)
			{
			case CMDStatusWithServer.ExcuteOnce:
			{
				uint num = this.objScriptManager.StartOnce();
				break;
			}
			case CMDStatusWithServer.ExcuteContinue:
			{
				uint num = this.objScriptManager.StartContinueRun();
				break;
			}
			case CMDStatusWithServer.StopExcute:
			{
				uint num = this.objScriptManager.StopExcute();
				break;
			}
			case CMDStatusWithServer.SaveSolution:
			{
				uint num = this.objScriptManager.SaveSolutionByMap(out reMsg);
				break;
			}
			case CMDStatusWithServer.LoadSolution:
			{
				this.bNeedLoadDefaultScript = false;
				uint num = this.objScriptManager.LoadSolution(text, this.objServerInfo.IsCrash);
				break;
			}
			case CMDStatusWithServer.CloseScript:
			{
				this.objServerInfo.IsCrash = false;
				uint num = this.objScriptManager.CloseScript(false);
				break;
			}
			case CMDStatusWithServer.SetMsgFromUI:
			{
				uint num = this.objScriptManager.SetMsgFromUI(text);
				break;
			}
			case CMDStatusWithServer.GetMsgToUI:
			{
				uint num = this.objScriptManager.GetMsgToUI(text, ref reMsg);
				break;
			}
			case CMDStatusWithServer.SetVMZmqPair:
			{
				uint num = this.objScriptManager.SetVMZmqPair(text);
				break;
			}
			case CMDStatusWithServer.ReleaseSharedMemory:
			{
				uint num = this.objScriptManager.ReleaseShaleMap(text);
				break;
			}
			case CMDStatusWithServer.SilentlyExecuteOnce:
			{
				uint num = this.objScriptManager.SilentlyExecuteOnce(text);
				break;
			}
			case CMDStatusWithServer.LoadSolutionEnd:
			{
				uint num = this.objScriptManager.ExecuteLoadInit();
				break;
			}
			}
			LogHelper.Info("Receive end:" + cmdstatusWithServer);
			return 0;
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x00006CB0 File Offset: 0x00004EB0
		public string ZmqGetReceiveData(string obj)
		{
			string text = string.Empty;
			bool flag = string.IsNullOrEmpty(obj);
			string result;
			if (flag)
			{
				result = text;
			}
			else
			{
				uint num = 0U;
				CMDStatusWithServer cmdstatusWithServer = CMDStatusWithServer.UnKnow;
				object arg = null;
				object obj2 = null;
				try
				{
					arg = JsonConvert.DeserializeObject(obj);
					if (GlobalManager.<>o__32.<>p__1 == null)
					{
						GlobalManager.<>o__32.<>p__1 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, bool> target = GlobalManager.<>o__32.<>p__1.Target;
					CallSite <>p__ = GlobalManager.<>o__32.<>p__1;
					if (GlobalManager.<>o__32.<>p__0 == null)
					{
						GlobalManager.<>o__32.<>p__0 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Equal, typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
						}));
					}
					bool flag2 = target(<>p__, GlobalManager.<>o__32.<>p__0.Target(GlobalManager.<>o__32.<>p__0, arg, null));
					if (flag2)
					{
						LogHelper.Error("Json Deserial Error,Info:" + obj);
						return text;
					}
					if (GlobalManager.<>o__32.<>p__4 == null)
					{
						GlobalManager.<>o__32.<>p__4 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(GlobalManager)));
					}
					Func<CallSite, object, string> target2 = GlobalManager.<>o__32.<>p__4.Target;
					CallSite <>p__2 = GlobalManager.<>o__32.<>p__4;
					if (GlobalManager.<>o__32.<>p__3 == null)
					{
						GlobalManager.<>o__32.<>p__3 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "command", typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, object> target3 = GlobalManager.<>o__32.<>p__3.Target;
					CallSite <>p__3 = GlobalManager.<>o__32.<>p__3;
					if (GlobalManager.<>o__32.<>p__2 == null)
					{
						GlobalManager.<>o__32.<>p__2 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					string value = target2(<>p__2, target3(<>p__3, GlobalManager.<>o__32.<>p__2.Target(GlobalManager.<>o__32.<>p__2, arg)));
					cmdstatusWithServer = (CMDStatusWithServer)Convert.ToInt32(value);
					LogHelper.Info("Receive:" + cmdstatusWithServer);
					switch (cmdstatusWithServer)
					{
					case CMDStatusWithServer.ShowWindow:
						num = 0U;
						break;
					case CMDStatusWithServer.ExcuteOnce:
						num = this.objScriptManager.StartOnce();
						break;
					case CMDStatusWithServer.ExcuteContinue:
						num = this.objScriptManager.StartContinueRun();
						break;
					case CMDStatusWithServer.StopExcute:
						num = this.objScriptManager.StopExcute();
						break;
					case CMDStatusWithServer.SaveSolution:
						num = this.objScriptManager.SaveSolutionByMap(out this._strSaveFilePath);
						obj2 = new
						{
							filePath = this._strSaveFilePath
						};
						break;
					case CMDStatusWithServer.LoadSolution:
					{
						this.bNeedLoadDefaultScript = false;
						if (GlobalManager.<>o__32.<>p__7 == null)
						{
							GlobalManager.<>o__32.<>p__7 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(GlobalManager)));
						}
						Func<CallSite, object, string> target4 = GlobalManager.<>o__32.<>p__7.Target;
						CallSite <>p__4 = GlobalManager.<>o__32.<>p__7;
						if (GlobalManager.<>o__32.<>p__6 == null)
						{
							GlobalManager.<>o__32.<>p__6 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "filePath", typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target5 = GlobalManager.<>o__32.<>p__6.Target;
						CallSite <>p__5 = GlobalManager.<>o__32.<>p__6;
						if (GlobalManager.<>o__32.<>p__5 == null)
						{
							GlobalManager.<>o__32.<>p__5 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "body", typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						string filePath = target4(<>p__4, target5(<>p__5, GlobalManager.<>o__32.<>p__5.Target(GlobalManager.<>o__32.<>p__5, arg)));
						num = this.objScriptManager.LoadSolution(filePath, this.objServerInfo.IsCrash);
						break;
					}
					case CMDStatusWithServer.CloseScript:
						this.objServerInfo.IsCrash = false;
						num = this.objScriptManager.CloseScript(false);
						break;
					case CMDStatusWithServer.SetMsgFromUI:
					{
						if (GlobalManager.<>o__32.<>p__17 == null)
						{
							GlobalManager.<>o__32.<>p__17 = CallSite<Func<CallSite, object, uint>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(uint), typeof(GlobalManager)));
						}
						Func<CallSite, object, uint> target6 = GlobalManager.<>o__32.<>p__17.Target;
						CallSite <>p__6 = GlobalManager.<>o__32.<>p__17;
						if (GlobalManager.<>o__32.<>p__16 == null)
						{
							GlobalManager.<>o__32.<>p__16 = CallSite<Func<CallSite, UserGlobalScriptManger, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "SetMsgFromUI", null, typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, UserGlobalScriptManger, object, object> target7 = GlobalManager.<>o__32.<>p__16.Target;
						CallSite <>p__7 = GlobalManager.<>o__32.<>p__16;
						UserGlobalScriptManger arg2 = this.objScriptManager;
						if (GlobalManager.<>o__32.<>p__15 == null)
						{
							GlobalManager.<>o__32.<>p__15 = CallSite<Func<CallSite, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "ToString", null, typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target8 = GlobalManager.<>o__32.<>p__15.Target;
						CallSite <>p__8 = GlobalManager.<>o__32.<>p__15;
						if (GlobalManager.<>o__32.<>p__14 == null)
						{
							GlobalManager.<>o__32.<>p__14 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "transparentData", typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target9 = GlobalManager.<>o__32.<>p__14.Target;
						CallSite <>p__9 = GlobalManager.<>o__32.<>p__14;
						if (GlobalManager.<>o__32.<>p__13 == null)
						{
							GlobalManager.<>o__32.<>p__13 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "body", typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						num = target6(<>p__6, target7(<>p__7, arg2, target8(<>p__8, target9(<>p__9, GlobalManager.<>o__32.<>p__13.Target(GlobalManager.<>o__32.<>p__13, arg)))));
						break;
					}
					case CMDStatusWithServer.GetMsgToUI:
					{
						string transparentData = null;
						if (GlobalManager.<>o__32.<>p__12 == null)
						{
							GlobalManager.<>o__32.<>p__12 = CallSite<Func<CallSite, object, uint>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(uint), typeof(GlobalManager)));
						}
						Func<CallSite, object, uint> target10 = GlobalManager.<>o__32.<>p__12.Target;
						CallSite <>p__10 = GlobalManager.<>o__32.<>p__12;
						if (GlobalManager.<>o__32.<>p__11 == null)
						{
							GlobalManager.<>o__32.<>p__11 = CallSite<<>F{00000008}<CallSite, UserGlobalScriptManger, object, string, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "GetMsgToUI", null, typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsRef, null)
							}));
						}
						<>F{00000008}<CallSite, UserGlobalScriptManger, object, string, object> target11 = GlobalManager.<>o__32.<>p__11.Target;
						CallSite <>p__11 = GlobalManager.<>o__32.<>p__11;
						UserGlobalScriptManger userGlobalScriptManger = this.objScriptManager;
						if (GlobalManager.<>o__32.<>p__10 == null)
						{
							GlobalManager.<>o__32.<>p__10 = CallSite<Func<CallSite, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "ToString", null, typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target12 = GlobalManager.<>o__32.<>p__10.Target;
						CallSite <>p__12 = GlobalManager.<>o__32.<>p__10;
						if (GlobalManager.<>o__32.<>p__9 == null)
						{
							GlobalManager.<>o__32.<>p__9 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "transparentData", typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target13 = GlobalManager.<>o__32.<>p__9.Target;
						CallSite <>p__13 = GlobalManager.<>o__32.<>p__9;
						if (GlobalManager.<>o__32.<>p__8 == null)
						{
							GlobalManager.<>o__32.<>p__8 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "body", typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						num = target10(<>p__10, target11(<>p__11, userGlobalScriptManger, target12(<>p__12, target13(<>p__13, GlobalManager.<>o__32.<>p__8.Target(GlobalManager.<>o__32.<>p__8, arg))), ref transparentData));
						obj2 = new
						{
							transparentData
						};
						break;
					}
					case CMDStatusWithServer.SetVMZmqPair:
					{
						if (GlobalManager.<>o__32.<>p__22 == null)
						{
							GlobalManager.<>o__32.<>p__22 = CallSite<Func<CallSite, object, uint>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(uint), typeof(GlobalManager)));
						}
						Func<CallSite, object, uint> target14 = GlobalManager.<>o__32.<>p__22.Target;
						CallSite <>p__14 = GlobalManager.<>o__32.<>p__22;
						if (GlobalManager.<>o__32.<>p__21 == null)
						{
							GlobalManager.<>o__32.<>p__21 = CallSite<Func<CallSite, UserGlobalScriptManger, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "SetVMZmqPair", null, typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, UserGlobalScriptManger, object, object> target15 = GlobalManager.<>o__32.<>p__21.Target;
						CallSite <>p__15 = GlobalManager.<>o__32.<>p__21;
						UserGlobalScriptManger arg3 = this.objScriptManager;
						if (GlobalManager.<>o__32.<>p__20 == null)
						{
							GlobalManager.<>o__32.<>p__20 = CallSite<Func<CallSite, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "ToString", null, typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target16 = GlobalManager.<>o__32.<>p__20.Target;
						CallSite <>p__16 = GlobalManager.<>o__32.<>p__20;
						if (GlobalManager.<>o__32.<>p__19 == null)
						{
							GlobalManager.<>o__32.<>p__19 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "clientCommAddr", typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target17 = GlobalManager.<>o__32.<>p__19.Target;
						CallSite <>p__17 = GlobalManager.<>o__32.<>p__19;
						if (GlobalManager.<>o__32.<>p__18 == null)
						{
							GlobalManager.<>o__32.<>p__18 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "body", typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						num = target14(<>p__14, target15(<>p__15, arg3, target16(<>p__16, target17(<>p__17, GlobalManager.<>o__32.<>p__18.Target(GlobalManager.<>o__32.<>p__18, arg)))));
						break;
					}
					case CMDStatusWithServer.ReleaseSharedMemory:
					{
						if (GlobalManager.<>o__32.<>p__27 == null)
						{
							GlobalManager.<>o__32.<>p__27 = CallSite<Func<CallSite, object, uint>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(uint), typeof(GlobalManager)));
						}
						Func<CallSite, object, uint> target18 = GlobalManager.<>o__32.<>p__27.Target;
						CallSite <>p__18 = GlobalManager.<>o__32.<>p__27;
						if (GlobalManager.<>o__32.<>p__26 == null)
						{
							GlobalManager.<>o__32.<>p__26 = CallSite<Func<CallSite, UserGlobalScriptManger, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "ReleaseShaleMap", null, typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, UserGlobalScriptManger, object, object> target19 = GlobalManager.<>o__32.<>p__26.Target;
						CallSite <>p__19 = GlobalManager.<>o__32.<>p__26;
						UserGlobalScriptManger arg4 = this.objScriptManager;
						if (GlobalManager.<>o__32.<>p__25 == null)
						{
							GlobalManager.<>o__32.<>p__25 = CallSite<Func<CallSite, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "ToString", null, typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target20 = GlobalManager.<>o__32.<>p__25.Target;
						CallSite <>p__20 = GlobalManager.<>o__32.<>p__25;
						if (GlobalManager.<>o__32.<>p__24 == null)
						{
							GlobalManager.<>o__32.<>p__24 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "sharedMemoryName", typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target21 = GlobalManager.<>o__32.<>p__24.Target;
						CallSite <>p__21 = GlobalManager.<>o__32.<>p__24;
						if (GlobalManager.<>o__32.<>p__23 == null)
						{
							GlobalManager.<>o__32.<>p__23 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "body", typeof(GlobalManager), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						num = target18(<>p__18, target19(<>p__19, arg4, target20(<>p__20, target21(<>p__21, GlobalManager.<>o__32.<>p__23.Target(GlobalManager.<>o__32.<>p__23, arg)))));
						break;
					}
					case CMDStatusWithServer.SilentlyExecuteOnce:
						num = this.objScriptManager.SilentlyExecuteOnce("");
						break;
					case CMDStatusWithServer.LoadSolutionEnd:
						num = this.objScriptManager.ExecuteLoadInit();
						break;
					}
				}
				catch (Exception ex)
				{
					LogHelper.Error("Deal Recive Json Command Error," + ex.Message + obj);
				}
				if (GlobalManager.<>o__32.<>p__29 == null)
				{
					GlobalManager.<>o__32.<>p__29 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(GlobalManager), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
					}));
				}
				Func<CallSite, object, bool> target22 = GlobalManager.<>o__32.<>p__29.Target;
				CallSite <>p__22 = GlobalManager.<>o__32.<>p__29;
				if (GlobalManager.<>o__32.<>p__28 == null)
				{
					GlobalManager.<>o__32.<>p__28 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Equal, typeof(GlobalManager), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
					}));
				}
				bool flag3 = target22(<>p__22, GlobalManager.<>o__32.<>p__28.Target(GlobalManager.<>o__32.<>p__28, arg, null));
				if (flag3)
				{
					LogHelper.Info("send:434{ 434:r34}");
					result = "434{434:r34}";
				}
				else
				{
					if (GlobalManager.<>o__32.<>p__31 == null)
					{
						GlobalManager.<>o__32.<>p__31 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "command", typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, object> target23 = GlobalManager.<>o__32.<>p__31.Target;
					CallSite <>p__23 = GlobalManager.<>o__32.<>p__31;
					if (GlobalManager.<>o__32.<>p__30 == null)
					{
						GlobalManager.<>o__32.<>p__30 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					object command = target23(<>p__23, GlobalManager.<>o__32.<>p__30.Target(GlobalManager.<>o__32.<>p__30, arg));
					string type = "response";
					if (GlobalManager.<>o__32.<>p__33 == null)
					{
						GlobalManager.<>o__32.<>p__33 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "seqId", typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, object> target24 = GlobalManager.<>o__32.<>p__33.Target;
					CallSite <>p__24 = GlobalManager.<>o__32.<>p__33;
					if (GlobalManager.<>o__32.<>p__32 == null)
					{
						GlobalManager.<>o__32.<>p__32 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					object head = new
					{
						command = command,
						type = type,
						seqId = target24(<>p__24, GlobalManager.<>o__32.<>p__32.Target(GlobalManager.<>o__32.<>p__32, arg)),
						errorCode = num,
						errorDesc = ErrorCode.GetErrorInfo(num)
					};
					if (GlobalManager.<>o__32.<>p__35 == null)
					{
						GlobalManager.<>o__32.<>p__35 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, bool> target25 = GlobalManager.<>o__32.<>p__35.Target;
					CallSite <>p__25 = GlobalManager.<>o__32.<>p__35;
					if (GlobalManager.<>o__32.<>p__34 == null)
					{
						GlobalManager.<>o__32.<>p__34 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.NotEqual, typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
						}));
					}
					bool flag4 = target25(<>p__25, GlobalManager.<>o__32.<>p__34.Target(GlobalManager.<>o__32.<>p__34, obj2, null));
					object arg5;
					if (flag4)
					{
						arg5 = new
						{
							head = head,
							body = obj2
						};
					}
					else
					{
						arg5 = new
						{
							head
						};
					}
					if (GlobalManager.<>o__32.<>p__37 == null)
					{
						GlobalManager.<>o__32.<>p__37 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(GlobalManager)));
					}
					Func<CallSite, object, string> target26 = GlobalManager.<>o__32.<>p__37.Target;
					CallSite <>p__26 = GlobalManager.<>o__32.<>p__37;
					if (GlobalManager.<>o__32.<>p__36 == null)
					{
						GlobalManager.<>o__32.<>p__36 = CallSite<Func<CallSite, Type, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "SerializeObject", null, typeof(GlobalManager), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsStaticType, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					text = target26(<>p__26, GlobalManager.<>o__32.<>p__36.Target(GlobalManager.<>o__32.<>p__36, typeof(JsonConvert), arg5));
					this.objZmqCom.SendData(text);
					LogHelper.Info("Send:" + cmdstatusWithServer);
					bool flag5 = num > 0U;
					if (flag5)
					{
						LogHelper.Info(text);
					}
					result = text;
				}
			}
			return result;
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00007AB8 File Offset: 0x00005CB8
		public void Dispose()
		{
			this.unEnableTimer();
			bool flag = this.objZmqCom != null;
			if (flag)
			{
				BaseZmqCommunicate baseZmqCommunicate = this.objZmqCom;
				baseZmqCommunicate.GetReceiveData = (Func<string, string>)Delegate.Remove(baseZmqCommunicate.GetReceiveData, new Func<string, string>(this.ZmqGetReceiveData));
				this.objZmqCom.Dispose();
			}
			bool flag2 = this.objScriptManager != null;
			if (flag2)
			{
				this.objScriptManager.Dispose();
			}
		}

		// Token: 0x060000FB RID: 251 RVA: 0x00007B2C File Offset: 0x00005D2C
		private void unEnableTimer()
		{
			bool flag = this.objListenServerTime != null;
			if (flag)
			{
				this.objListenServerTime.Enabled = false;
			}
			bool flag2 = this.objLoadDefaultScriptTime != null;
			if (flag2)
			{
				this.objLoadDefaultScriptTime.Enabled = false;
			}
		}

		// Token: 0x040000FA RID: 250
		private static GlobalManager _instance = null;

		// Token: 0x040000FC RID: 252
		private BaseZmqCommunicate objZmqCom = null;

		// Token: 0x040000FD RID: 253
		private int _iReceiveTime = 50;

		// Token: 0x040000FE RID: 254
		private int _iWriteTime = 100;

		// Token: 0x040000FF RID: 255
		private UserGlobalScriptManger objScriptManager = null;

		// Token: 0x04000100 RID: 256
		private string _strSaveFilePath;

		// Token: 0x04000101 RID: 257
		private Timer objListenServerTime = null;

		// Token: 0x04000102 RID: 258
		private int iListenServerTime = 15000;

		// Token: 0x04000103 RID: 259
		private Timer objLoadDefaultScriptTime = null;

		// Token: 0x04000104 RID: 260
		private int iLoadDefaultScriptTimer = 5000;

		// Token: 0x04000105 RID: 261
		private bool bNeedLoadDefaultScript = true;

		// Token: 0x04000106 RID: 262
		private bool bZmqType = false;

		// Token: 0x04000107 RID: 263
		private bool IsSingleProcess = false;
	}
}
