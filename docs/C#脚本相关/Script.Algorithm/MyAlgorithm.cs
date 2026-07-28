using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Apps.Json;
using Microsoft.CSharp.RuntimeBinder;
using VM.Utility;

namespace Script.Algorithm
{
	// Token: 0x0200001D RID: 29
	public class MyAlgorithm : MarshalByRefObject, IAlgorithm
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x0600013B RID: 315 RVA: 0x00006724 File Offset: 0x00004924
		// (remove) Token: 0x0600013C RID: 316 RVA: 0x00006760 File Offset: 0x00004960
		public event ParamEventDelegate SetParamEventHandler = null;

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x0600013D RID: 317 RVA: 0x0000679C File Offset: 0x0000499C
		// (remove) Token: 0x0600013E RID: 318 RVA: 0x000067D8 File Offset: 0x000049D8
		public event ParamEventDelegate GetParamEventHandler = null;

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x0600013F RID: 319 RVA: 0x00006814 File Offset: 0x00004A14
		// (remove) Token: 0x06000140 RID: 320 RVA: 0x00006850 File Offset: 0x00004A50
		public event ProcessEventDelegate ProcessEventHandler = null;

		// Token: 0x14000004 RID: 4
		// (add) Token: 0x06000141 RID: 321 RVA: 0x0000688C File Offset: 0x00004A8C
		// (remove) Token: 0x06000142 RID: 322 RVA: 0x000068C8 File Offset: 0x00004AC8
		public event HeartEventDelegate HeartEventHandler = null;

		// Token: 0x06000143 RID: 323 RVA: 0x00006904 File Offset: 0x00004B04
		public MyAlgorithm()
		{
			this.myZmqContext = IntPtr.Zero;
			this.pHeartPairScoket = IntPtr.Zero;
			this.pSetRepScoket = IntPtr.Zero;
			this.pProcessRepScoket = IntPtr.Zero;
			this.pGetPairScoket = IntPtr.Zero;
			this.bSetRepTask = false;
			this.bProcessRepTask = false;
			this.m_listSetValueInfo = new List<SetValueInfo>();
			this.m_dictSubModuleResultInfo = new Dictionary<string, GetModuleResultInfo>();
			this.getParamPairMutex = new Mutex();
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00006A84 File Offset: 0x00004C84
		public override object InitializeLifetimeService()
		{
			return null;
		}

		// Token: 0x06000145 RID: 325 RVA: 0x00006A97 File Offset: 0x00004C97
		public void SetInOutputHandle(long input, long output)
		{
		}

		// Token: 0x06000146 RID: 326 RVA: 0x00006A9A File Offset: 0x00004C9A
		public void SetData(string key, object obj)
		{
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00006AA0 File Offset: 0x00004CA0
		public int GetModuleParamValue(int ModuleID, string paramName, ref string paramValue)
		{
			return 0;
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00006AB4 File Offset: 0x00004CB4
		public int GetLocalVarModuleID(ref int nVarID)
		{
			return 0;
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x06000149 RID: 329 RVA: 0x00006AC8 File Offset: 0x00004CC8
		public int ModuleID
		{
			get
			{
				return -1;
			}
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00006B10 File Offset: 0x00004D10
		public int Init(AddressInfo info)
		{
			int result;
			if (info == null)
			{
				result = -536870911;
			}
			else
			{
				this.myAddressInfo = info;
				Task.Run(delegate()
				{
					try
					{
						ProcessResult processResult = JsonConvert.DeserializeObject<ProcessResult>(this.strDefaultJson);
					}
					catch
					{
					}
				});
				ZmqDataContext zmqDataContext = new ZmqDataContext
				{
					ConnectionString = this.myAddressInfo.strSetParamRepAddress,
					RcvTimout = this.nRcvTimout,
					Encod = Encoding.UTF8,
					ZmqType = 1,
					ServerOrClient = true,
					WriteTimeOut = this.nWriteTimeout,
					StartReceiveTask = true
				};
				zmqDataContext.ConnectionString = this.myAddressInfo.strProcessRepAddress;
				zmqDataContext.StartReceiveTask = true;
				this.processReqMq = new HkrMqCommunicate(zmqDataContext);
				bool flag = this.processReqMq.InitCommuncate();
				if (!flag)
				{
					LogHelper.Error("开启运行socket失败,nRet:" + flag, 0);
					result = -536870911;
				}
				else
				{
					this.bProcessRepTask = true;
					this.processReqMq.GetReceiveData = new Action<string>(this.ReceiveProcessParamEvent);
					zmqDataContext.ConnectionString = this.myAddressInfo.strGetParamReqAddress;
					zmqDataContext.StartReceiveTask = false;
					this.getParamPairMq = new HkrMqCommunicate(zmqDataContext);
					flag = this.getParamPairMq.InitCommuncate();
					if (!flag)
					{
						LogHelper.Error("开启获取参数socket失败,nRet:" + flag, 0);
						result = -536870911;
					}
					else
					{
						this.m_heartTimer_Elapsed(null, null);
						this.InitHeartTime();
						result = 0;
					}
				}
			}
			return result;
		}

		// Token: 0x0600014B RID: 331 RVA: 0x00006C90 File Offset: 0x00004E90
		private int InitContext()
		{
			int result;
			try
			{
				if (this.myZmqContext == IntPtr.Zero)
				{
					this.myZmqContext = Libzmq.zmq_ctx_new();
					if (this.myZmqContext == IntPtr.Zero)
					{
						return -536870657;
					}
				}
				result = 0;
			}
			catch (Exception ex)
			{
				result = -536870657;
			}
			return result;
		}

		// Token: 0x0600014C RID: 332 RVA: 0x00006D04 File Offset: 0x00004F04
		private void InitHeartTime()
		{
			this.m_heartTimer = new System.Timers.Timer(10000.0);
			this.m_heartTimer.AutoReset = true;
			this.m_heartTimer.Elapsed += this.m_heartTimer_Elapsed;
			this.m_heartTimer.Enabled = true;
		}

		// Token: 0x0600014D RID: 333 RVA: 0x00006D58 File Offset: 0x00004F58
		public bool GetExitFlag()
		{
			return this.m_isExit;
		}

		// Token: 0x0600014E RID: 334 RVA: 0x00006D70 File Offset: 0x00004F70
		private void m_heartTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (!this._enterDispose)
			{
				this._enterHeartTime = true;
				bool flag = false;
				try
				{
					this.GetSeqId();
					int num = this.uMySeqId;
					string sendInfo = this.JoinRequestJsonData(4001, this.uMySeqId, "heart");
					object obj = null;
					flag = this.CommunToModule(sendInfo, this.uMySeqId, ref obj, "list");
				}
				catch (Exception ex)
				{
					LogHelper.Error("Heart time is catch. exception:" + ex.ToString(), 0);
				}
				this._enterHeartTime = false;
				if (!flag)
				{
					if (!this.isCheckServer("VisionMaster.exe", this.myAddressInfo.nProxyID))
					{
						LogHelper.Error("Heart error. kill process", 0);
						this.Dispose();
					}
				}
			}
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00006E4C File Offset: 0x0000504C
		public void Dispose()
		{
			this._enterDispose = true;
			int num = 0;
			while (this._enterHeartTime)
			{
				Thread.Sleep(1);
				num++;
				if (num > 30000)
				{
					break;
				}
			}
			lock (this.lockObj)
			{
				if (this._dispose)
				{
					return;
				}
				try
				{
					this.m_isExit = true;
					this.bSetRepTask = false;
					this.bProcessRepTask = false;
					if (this.m_heartTimer != null)
					{
						this.m_heartTimer.Enabled = false;
						this.m_heartTimer.Dispose();
						this.m_heartTimer = null;
					}
					if (this.processTask != null)
					{
						this.processTask.Wait(2000);
						this.processTask = null;
					}
					if (this.setParamTask != null)
					{
						this.setParamTask.Wait(2000);
						this.setParamTask = null;
					}
					if (this.heartPairMq != null)
					{
						this.heartPairMq.Dispose();
					}
					if (this.setParamRepMq != null)
					{
						this.setParamRepMq.GetReceiveData = null;
						this.setParamRepMq.Dispose();
					}
					if (this.getParamPairMq != null)
					{
						this.getParamPairMq.Dispose();
					}
					if (this.processReqMq != null)
					{
						this.processReqMq.GetReceiveData = null;
						this.processReqMq.Dispose();
					}
					this._dispose = true;
				}
				catch (Exception ex)
				{
					LogHelper.Error("Dispose error.ex:" + ex.ToString(), 0);
				}
			}
			if (this.HeartEventHandler != null)
			{
				this.HeartEventHandler();
			}
		}

		// Token: 0x06000150 RID: 336 RVA: 0x00007058 File Offset: 0x00005258
		private bool isCheckServer(string processName, int processID)
		{
			bool result;
			try
			{
				if (this.myAddressInfo.nProxyID <= 0)
				{
					result = true;
				}
				else
				{
					Process processById = Process.GetProcessById(processID);
					if (processName.Contains("."))
					{
						processName = processName.Substring(0, processName.IndexOf('.'));
					}
					if (processById.ProcessName == processName)
					{
						result = true;
					}
					else
					{
						LogHelper.Info(processName + " exe pid is not equal name", 0);
						result = false;
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.Info("isCheckServer is exception:" + ex.ToString(), 0);
				result = false;
			}
			return result;
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00007108 File Offset: 0x00005308
		private IntPtr InitZmqSocket(string address, Socket_Types types, out int nRet)
		{
			nRet = 0;
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = Libzmq.zmq_socket(this.myZmqContext, (int)types);
				if (intPtr == IntPtr.Zero)
				{
					nRet = -536870656;
					return intPtr;
				}
				if (Libzmq.zmq_setsockopt(intPtr, 27, ref this.nRcvTimout, 4) != 0)
				{
					nRet = -536870885;
					return intPtr;
				}
				if (Libzmq.zmq_setsockopt(intPtr, 28, ref this.nWriteTimeout, 4) != 0)
				{
					nRet = -536870885;
					return intPtr;
				}
				int num = Libzmq.zmq_bind(intPtr, address);
				if (num != 0)
				{
					LogHelper.Error(string.Format("Global Script Start Zmq Listen Faild:{0},ReturnCode:{1}", address, num), 0);
					nRet = -536870656;
					return intPtr;
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("InitZmqSocket error " + ex.Message, 0);
				nRet = -536870656;
				intPtr = IntPtr.Zero;
			}
			return intPtr;
		}

		// Token: 0x06000152 RID: 338 RVA: 0x000072F8 File Offset: 0x000054F8
		private void ReceiveProcessParamEvent(string strCommandMsg)
		{
			if (!string.IsNullOrEmpty(strCommandMsg))
			{
				try
				{
					ParamEventArgs paramEventArgs = new ParamEventArgs();
					paramEventArgs.Status = -536870888;
					string text = "";
					try
					{
						ProcessResult processResult = JsonConvert.DeserializeObject<ProcessResult>(strCommandMsg);
						if (processResult != null)
						{
							int command = processResult.head.command;
							int seqId = processResult.head.seqId;
							if (command == 4007)
							{
								Task.Run(delegate()
								{
									this.Dispose();
								});
								return;
							}
							switch (command)
							{
							case 4002:
							{
								if (this.SetParamEventHandler != null)
								{
									paramEventArgs.ParamName = "ShellContent";
									paramEventArgs.ParamValue = processResult.body.extrainfo;
									this.SetParamEventHandler(this, paramEventArgs);
								}
								object arg = new
								{
									filePath = paramEventArgs.ParamValue
								};
								if (MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site4 == null)
								{
									MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site4 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
								}
								Func<CallSite, object, string> target = MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site4.Target;
								CallSite <>p__Site = MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site4;
								if (MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site5 == null)
								{
									MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site5 = CallSite<Func<CallSite, MyAlgorithm, int, int, int, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.InvokeSimpleName, "JoinResponseJsonData", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
									}));
								}
								text = target(<>p__Site, MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site5.Target(MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site5, this, command, seqId, paramEventArgs.Status, arg));
								break;
							}
							case 4003:
							{
								paramEventArgs.ParamName = processResult.body.extrainfo;
								if (this.GetParamEventHandler != null)
								{
									this.GetParamEventHandler(this, paramEventArgs);
								}
								object arg = new
								{
									filePath = paramEventArgs.ParamValue
								};
								if (MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site6 == null)
								{
									MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site6 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
								}
								Func<CallSite, object, string> target2 = MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site6.Target;
								CallSite <>p__Site2 = MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site6;
								if (MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site7 == null)
								{
									MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site7 = CallSite<Func<CallSite, MyAlgorithm, int, int, int, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.InvokeSimpleName, "JoinResponseJsonData", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
									}));
								}
								text = target2(<>p__Site2, MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site7.Target(MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site7, this, command, seqId, paramEventArgs.Status, arg));
								break;
							}
							case 4005:
								if (processResult.body != null && processResult.body.resultinfo.Length > 0)
								{
									for (int i = 0; i < processResult.body.resultinfo.Length; i++)
									{
										this.m_dictSubModuleResultInfo.Add(processResult.body.resultinfo[i].id + "." + processResult.body.resultinfo[i].key, processResult.body.resultinfo[i]);
									}
								}
								if (this.ProcessEventHandler != null)
								{
									this.ClearSetObjectInfo();
									this.ProcessEventHandler(this, paramEventArgs);
								}
								this.m_dictSubModuleResultInfo.Clear();
								if (this.m_listSetValueInfo == null || this.m_listSetValueInfo.Count == 0)
								{
									text = this.JoinResponseJsonData(4005, seqId, paramEventArgs.Status, null);
								}
								else
								{
									text = this.JoinResponseJsonData(4005, seqId, paramEventArgs.Status, this.m_listSetValueInfo);
								}
								break;
							case 4008:
								paramEventArgs.Status = 0;
								text = this.JoinResponseJsonData(command, seqId, paramEventArgs.Status, null);
								break;
							case 4009:
							{
								if (this.SetParamEventHandler != null)
								{
									paramEventArgs.ParamName = "Refrences";
									paramEventArgs.ParamValue = processResult.body.extrainfo;
									this.SetParamEventHandler(this, paramEventArgs);
								}
								object arg = new
								{
									filePath = paramEventArgs.ParamValue
								};
								if (MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site8 == null)
								{
									MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site8 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
								}
								Func<CallSite, object, string> target3 = MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site8.Target;
								CallSite <>p__Site3 = MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site8;
								if (MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site9 == null)
								{
									MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site9 = CallSite<Func<CallSite, MyAlgorithm, int, int, int, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.InvokeSimpleName, "JoinResponseJsonData", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
									}));
								}
								text = target3(<>p__Site3, MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site9.Target(MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Site9, this, command, seqId, paramEventArgs.Status, arg));
								break;
							}
							case 4011:
							{
								if (this.SetParamEventHandler != null)
								{
									paramEventArgs.ParamName = "Exportsln";
									this.SetParamEventHandler(this, paramEventArgs);
								}
								object arg = new
								{
									filePath = paramEventArgs.ParamValue
								};
								if (MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Sitea == null)
								{
									MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Sitea = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
								}
								Func<CallSite, object, string> target4 = MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Sitea.Target;
								CallSite <>p__Sitea = MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Sitea;
								if (MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Siteb == null)
								{
									MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Siteb = CallSite<Func<CallSite, MyAlgorithm, int, int, int, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.InvokeSimpleName, "JoinResponseJsonData", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
									}));
								}
								text = target4(<>p__Sitea, MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Siteb.Target(MyAlgorithm.<ReceiveProcessParamEvent>o__SiteContainer3.<>p__Siteb, this, command, seqId, paramEventArgs.Status, arg));
								break;
							}
							}
						}
					}
					catch (Exception ex)
					{
						LogHelper.Error(string.Format("prase process params error:{0},exeption:{1}", strCommandMsg, ex.Message), 0);
					}
					if (!string.IsNullOrEmpty(text) && !this.processReqMq.SendData(text))
					{
						LogHelper.Error("process send data faild:" + text, 0);
					}
				}
				catch (Exception ex)
				{
					LogHelper.Error(string.Format("ReceiveProcessParamEvent error:{0},exeption:{1}", strCommandMsg, ex.Message), 0);
				}
			}
		}

		// Token: 0x06000153 RID: 339 RVA: 0x00007A28 File Offset: 0x00005C28
		private int PraseJsonData(string strCommandMsg, out uint seqID, out int cmdID)
		{
			int result = 0;
			seqID = 0U;
			cmdID = 0;
			try
			{
				object arg = JsonConvert.DeserializeObject(strCommandMsg);
				if (MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Sitef == null)
				{
					MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Sitef = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(MyAlgorithm), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
					}));
				}
				Func<CallSite, object, bool> target = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Sitef.Target;
				CallSite <>p__Sitef = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Sitef;
				if (MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site10 == null)
				{
					MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site10 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Equal, typeof(MyAlgorithm), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
					}));
				}
				if (target(<>p__Sitef, MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site10.Target(MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site10, arg, null)))
				{
					result = -536870911;
				}
				else
				{
					if (MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site11 == null)
					{
						MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site11 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
					}
					Func<CallSite, object, string> target2 = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site11.Target;
					CallSite <>p__Site = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site11;
					if (MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site12 == null)
					{
						MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site12 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "command", typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, object> target3 = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site12.Target;
					CallSite <>p__Site2 = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site12;
					if (MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site13 == null)
					{
						MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site13 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					string s = target2(<>p__Site, target3(<>p__Site2, MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site13.Target(MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site13, arg)));
					if (MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site14 == null)
					{
						MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site14 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
					}
					Func<CallSite, object, string> target4 = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site14.Target;
					CallSite <>p__Site3 = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site14;
					if (MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site15 == null)
					{
						MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site15 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "seqId", typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, object> target5 = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site15.Target;
					CallSite <>p__Site4 = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site15;
					if (MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site16 == null)
					{
						MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site16 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					string s2 = target4(<>p__Site3, target5(<>p__Site4, MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site16.Target(MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site16, arg)));
					if (MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site17 == null)
					{
						MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site17 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
					}
					Func<CallSite, object, string> target6 = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site17.Target;
					CallSite <>p__Site5 = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site17;
					if (MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site18 == null)
					{
						MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site18 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "errorCode", typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, object> target7 = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site18.Target;
					CallSite <>p__Site6 = MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site18;
					if (MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site19 == null)
					{
						MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site19 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					string text = target6(<>p__Site5, target7(<>p__Site6, MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site19.Target(MyAlgorithm.<PraseJsonData>o__SiteContainere.<>p__Site19, arg)));
					uint.TryParse(s2, out seqID);
					int.TryParse(s, out cmdID);
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("PraseJsonData Error,error = " + ex.Message, 0);
				result = -536870657;
			}
			return result;
		}

		// Token: 0x06000154 RID: 340 RVA: 0x0000803C File Offset: 0x0000623C
		public int GetObjectValueSend(string paramKey, int type, int index, ref string paramValue, ref int arrayCount)
		{
			paramValue = string.Empty;
			int num = 0;
			arrayCount = 0;
			int result;
			if (string.IsNullOrEmpty(paramKey))
			{
				result = -536870911;
			}
			else
			{
				try
				{
					object arg = new
					{
						index = index,
						type = type,
						paramKey = paramKey,
						array = 0
					};
					this.GetSeqId();
					if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1b == null)
					{
						MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1b = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
					}
					Func<CallSite, object, string> target = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1b.Target;
					CallSite <>p__Site1b = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1b;
					if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1c == null)
					{
						MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1c = CallSite<Func<CallSite, MyAlgorithm, int, int, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.InvokeSimpleName, "JoinRequestJsonData", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.Constant, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					string sendInfo = target(<>p__Site1b, MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1c.Target(MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1c, this, 4004, this.uMySeqId, arg));
					object arg2 = null;
					if (!this.CommunToModule(sendInfo, this.uMySeqId, ref arg2, paramKey))
					{
						num = -536870888;
					}
					else
					{
						if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1d == null)
						{
							MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1d = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
						}
						Func<CallSite, object, string> target2 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1d.Target;
						CallSite <>p__Site1d = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1d;
						if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1e == null)
						{
							MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1e = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "errorCode", typeof(MyAlgorithm), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target3 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1e.Target;
						CallSite <>p__Site1e = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1e;
						if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1f == null)
						{
							MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1f = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(MyAlgorithm), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						string s = target2(<>p__Site1d, target3(<>p__Site1e, MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1f.Target(MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site1f, arg2)));
						int.TryParse(s, out num);
						if (num == 0)
						{
							if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site20 == null)
							{
								MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site20 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "paramValue", typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							Func<CallSite, object, object> target4 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site20.Target;
							CallSite <>p__Site = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site20;
							if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site21 == null)
							{
								MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site21 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "body", typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							object arg3 = target4(<>p__Site, MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site21.Target(MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site21, arg2));
							if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site22 == null)
							{
								MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site22 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							Func<CallSite, object, bool> target5 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site22.Target;
							CallSite <>p__Site2 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site22;
							if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site23 == null)
							{
								MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site23 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.NotEqual, typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
								}));
							}
							object obj = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site23.Target(MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site23, arg3, null);
							if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site24 == null)
							{
								MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site24 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsFalse, typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							object arg5;
							if (!MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site24.Target(MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site24, obj))
							{
								if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site25 == null)
								{
									MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site25 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.BinaryOperationLogical, ExpressionType.And, typeof(MyAlgorithm), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
									}));
								}
								Func<CallSite, object, object, object> target6 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site25.Target;
								CallSite <>p__Site3 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site25;
								object arg4 = obj;
								if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site26 == null)
								{
									MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site26 = CallSite<Func<CallSite, object, int, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.GreaterThan, typeof(MyAlgorithm), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.Constant, null)
									}));
								}
								Func<CallSite, object, int, object> target7 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site26.Target;
								CallSite <>p__Site4 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site26;
								if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site27 == null)
								{
									MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site27 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "Count", typeof(MyAlgorithm), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
									}));
								}
								arg5 = target6(<>p__Site3, arg4, target7(<>p__Site4, MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site27.Target(MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site27, arg3), 0));
							}
							else
							{
								arg5 = obj;
							}
							if (target5(<>p__Site2, arg5))
							{
								if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site28 == null)
								{
									MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site28 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
								}
								Func<CallSite, object, string> target8 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site28.Target;
								CallSite <>p__Site5 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site28;
								if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site29 == null)
								{
									MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site29 = CallSite<Func<CallSite, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "ToString", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
									}));
								}
								Func<CallSite, object, object> target9 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site29.Target;
								CallSite <>p__Site6 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site29;
								if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2a == null)
								{
									MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2a = CallSite<Func<CallSite, object, int, object>>.Create(Binder.GetIndex(CSharpBinderFlags.None, typeof(MyAlgorithm), new CSharpArgumentInfo[]
									{
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
										CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.Constant, null)
									}));
								}
								paramValue = target8(<>p__Site5, target9(<>p__Site6, MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2a.Target(MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2a, arg3, 0)));
							}
							else
							{
								paramValue = "";
							}
							if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2b == null)
							{
								MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2b = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
							}
							Func<CallSite, object, string> target10 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2b.Target;
							CallSite <>p__Site2b = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2b;
							if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2c == null)
							{
								MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2c = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "count", typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							Func<CallSite, object, object> target11 = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2c.Target;
							CallSite <>p__Site2c = MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2c;
							if (MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2d == null)
							{
								MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2d = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "body", typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							string s2 = target10(<>p__Site2b, target11(<>p__Site2c, MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2d.Target(MyAlgorithm.<GetObjectValueSend>o__SiteContainer1a.<>p__Site2d, arg2)));
							int.TryParse(s2, out arrayCount);
						}
					}
				}
				catch (Exception ex)
				{
					LogHelper.Error("GetObjectValueSend error " + ex.ToString(), 0);
					num = -536870657;
				}
				result = num;
			}
			return result;
		}

		// Token: 0x06000155 RID: 341 RVA: 0x00008768 File Offset: 0x00006968
		public int GetObjectValue(string paramKey, int type, int index, ref object paramValue, ref int arrayCount, int moduleId = -1)
		{
			int result = 0;
			string text = "";
			string key = moduleId + "." + paramKey;
			if (this.m_dictSubModuleResultInfo.ContainsKey(key))
			{
				GetModuleResultInfo getModuleResultInfo = this.m_dictSubModuleResultInfo[key];
				if (getModuleResultInfo == null)
				{
					return -536870888;
				}
				if (getModuleResultInfo.ret != 0)
				{
					return getModuleResultInfo.ret;
				}
				arrayCount = getModuleResultInfo.count;
				if (index < getModuleResultInfo.count)
				{
					text = getModuleResultInfo.value[index];
				}
				else
				{
					result = -536870892;
				}
			}
			else
			{
				result = this.GetObjectValueSend(paramKey, type, index, ref text, ref arrayCount);
			}
			if (type == 3)
			{
				paramValue = Convert.FromBase64String(text);
			}
			else
			{
				paramValue = text;
			}
			return result;
		}

		// Token: 0x06000156 RID: 342 RVA: 0x0000886C File Offset: 0x00006A6C
		public int GetFloatArrayValue(string paramKey, ref float[] paramValue)
		{
			string[] array = new string[0];
			int objectArrayValue = this.GetObjectArrayValue(paramKey, 1, ref array, -1);
			paramValue = new float[array.Length];
			paramValue = Array.ConvertAll<string, float>(array, (string x) => float.Parse(x));
			return objectArrayValue;
		}

		// Token: 0x06000157 RID: 343 RVA: 0x000088DC File Offset: 0x00006ADC
		public int GetIntArrayValue(string paramKey, ref int[] paramValue)
		{
			string[] array = new string[0];
			int objectArrayValue = this.GetObjectArrayValue(paramKey, 0, ref array, -1);
			paramValue = new int[array.Length];
			paramValue = Array.ConvertAll<string, int>(array, (string x) => int.Parse(x));
			return objectArrayValue;
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00008934 File Offset: 0x00006B34
		public int GetObjectArrayValue(string paramKey, int type, ref string[] paramValue, int moduleId = -1)
		{
			int result = 0;
			string key = moduleId + "." + paramKey;
			if (this.m_dictSubModuleResultInfo.ContainsKey(key))
			{
				if (this.m_dictSubModuleResultInfo[key].count <= 0)
				{
					result = -536870888;
				}
				else
				{
					paramValue = this.m_dictSubModuleResultInfo[key].value;
				}
			}
			else
			{
				object param = new
				{
					index = 0,
					type = type,
					paramKey = paramKey,
					array = 1
				};
				int num = 0;
				if (MyAlgorithm.<GetObjectArrayValue>o__SiteContainer32.<>p__Site33 == null)
				{
					MyAlgorithm.<GetObjectArrayValue>o__SiteContainer32.<>p__Site33 = CallSite<Func<CallSite, object, int>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(int), typeof(MyAlgorithm)));
				}
				Func<CallSite, object, int> target = MyAlgorithm.<GetObjectArrayValue>o__SiteContainer32.<>p__Site33.Target;
				CallSite <>p__Site = MyAlgorithm.<GetObjectArrayValue>o__SiteContainer32.<>p__Site33;
				if (MyAlgorithm.<GetObjectArrayValue>o__SiteContainer32.<>p__Site35 == null)
				{
					MyAlgorithm.<GetObjectArrayValue>o__SiteContainer32.<>p__Site35 = CallSite<MyAlgorithm.<GetObjectArrayValue>o__SiteContainer32.<>q__SiteDelegate34>.Create(Binder.InvokeMember(CSharpBinderFlags.InvokeSimpleName, "GetObjectArrayValueSend", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsRef, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsRef, null)
					}));
				}
				result = target(<>p__Site, MyAlgorithm.<GetObjectArrayValue>o__SiteContainer32.<>p__Site35.Target(MyAlgorithm.<GetObjectArrayValue>o__SiteContainer32.<>p__Site35, this, paramKey, param, ref num, ref paramValue));
			}
			return result;
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00008D20 File Offset: 0x00006F20
		public int GetObjectArrayValueForModule(int moduleId, int index, string paramKey, ref int nType, ref Array paramValue)
		{
			int result = 0;
			string key = moduleId + "." + paramKey;
			string[] array = new string[0];
			if (this.m_dictSubModuleResultInfo.ContainsKey(key))
			{
				if (this.m_dictSubModuleResultInfo[key].count <= 0)
				{
					result = -536870888;
				}
				else
				{
					nType = this.m_dictSubModuleResultInfo[key].type;
					array = this.m_dictSubModuleResultInfo[key].value;
				}
			}
			else
			{
				int array2 = 1;
				if (index == 0)
				{
					array2 = 0;
				}
				object param = new
				{
					id = moduleId,
					paramKey = paramKey,
					index = index,
					array = array2,
					getType = 1
				};
				if (MyAlgorithm.<GetObjectArrayValueForModule>o__SiteContainer36.<>p__Site37 == null)
				{
					MyAlgorithm.<GetObjectArrayValueForModule>o__SiteContainer36.<>p__Site37 = CallSite<Func<CallSite, object, int>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(int), typeof(MyAlgorithm)));
				}
				Func<CallSite, object, int> target = MyAlgorithm.<GetObjectArrayValueForModule>o__SiteContainer36.<>p__Site37.Target;
				CallSite <>p__Site = MyAlgorithm.<GetObjectArrayValueForModule>o__SiteContainer36.<>p__Site37;
				if (MyAlgorithm.<GetObjectArrayValueForModule>o__SiteContainer36.<>p__Site39 == null)
				{
					MyAlgorithm.<GetObjectArrayValueForModule>o__SiteContainer36.<>p__Site39 = CallSite<MyAlgorithm.<GetObjectArrayValueForModule>o__SiteContainer36.<>q__SiteDelegate38>.Create(Binder.InvokeMember(CSharpBinderFlags.InvokeSimpleName, "GetObjectArrayValueSend", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsRef, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsRef, null)
					}));
				}
				result = target(<>p__Site, MyAlgorithm.<GetObjectArrayValueForModule>o__SiteContainer36.<>p__Site39.Target(MyAlgorithm.<GetObjectArrayValueForModule>o__SiteContainer36.<>p__Site39, this, paramKey, param, ref nType, ref array));
			}
			if (nType == 3)
			{
				paramValue = new byte[1][];
				paramValue.SetValue(this.ReadDataFromMemory(array[0]), 0);
			}
			else if (nType == 4)
			{
				paramValue = new byte[1][];
				paramValue.SetValue(Convert.FromBase64String(array[0]), 0);
			}
			else
			{
				paramValue = array;
			}
			return result;
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00008F08 File Offset: 0x00007108
		public int SetImageData(string paramKey, int type, byte[] imageBuffer, int nWidth, int nHeight, int nPxiFormat)
		{
			int result;
			if (string.IsNullOrEmpty(paramKey))
			{
				result = -1;
			}
			else if (imageBuffer == null || imageBuffer.Length <= 0)
			{
				result = -1;
			}
			else
			{
				MemoryInfo memoryInfo = new MemoryInfo();
				if (this.ImageMemoryInfo.ContainsKey(paramKey))
				{
					memoryInfo = this.ImageMemoryInfo[paramKey];
				}
				else
				{
					memoryInfo.index = this.ImageMemoryInfo.Count;
					this.ImageMemoryInfo.Add(paramKey, memoryInfo);
				}
				if (memoryInfo == null)
				{
					result = -1;
				}
				else
				{
					if (string.IsNullOrEmpty(memoryInfo.memoryFileName))
					{
						memoryInfo.memoryFileName = string.Format("Global\\ShellImage_{0}", Guid.NewGuid().ToString("N"));
					}
					int num = this.WriteToMemory(imageBuffer, ref memoryInfo);
					if (num == 0)
					{
						string paramValue = string.Format("{0}\r{1}\r{2}\r{3}\r{4}", new object[]
						{
							memoryInfo.memoryFileName,
							nWidth,
							nHeight,
							nPxiFormat,
							memoryInfo.index
						});
						result = this.SetObjectValueSend(0, type, paramKey, paramValue, -1, 0);
					}
					else
					{
						result = num;
					}
				}
			}
			return result;
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00009060 File Offset: 0x00007260
		public int GetImageData(string paramKey, int type, ref byte[] imageBuffer, ref int nWidth, ref int nHeight, ref int nPxiFormat)
		{
			try
			{
				object obj = "";
				int num = 0;
				int objectValue = this.GetObjectValue(paramKey, type, 0, ref obj, ref num, -1);
				if (objectValue != 0)
				{
					return objectValue;
				}
				string text = (string)obj;
				if (text.Contains("\r"))
				{
					string[] array = text.Split(new char[]
					{
						'\r'
					});
					if (array.Length < 4)
					{
						return -536870911;
					}
					imageBuffer = this.ReadDataFromMemory(array[0]);
					nWidth = Convert.ToInt32(array[1]);
					nHeight = Convert.ToInt32(array[2]);
					nPxiFormat = Convert.ToInt32(array[3]);
					return 0;
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("GetImageData is error," + ex.ToString(), 0);
			}
			return -536870657;
		}

		// Token: 0x0600015C RID: 348 RVA: 0x0000915C File Offset: 0x0000735C
		public int SetRoiBoxData(string paramKey, int type, int index, float fCenterX, float fCenterY, float fWidth, float fHeight, float fAngle)
		{
			int result;
			if (string.IsNullOrEmpty(paramKey))
			{
				result = -1;
			}
			else
			{
				string paramValue = string.Format("{0}\r{1}\r{2}\r{3}\r{4}", new object[]
				{
					fCenterX,
					fCenterY,
					fWidth,
					fHeight,
					fAngle
				});
				result = this.SetObjectValueEx(index, type, paramKey, paramValue);
			}
			return result;
		}

		// Token: 0x0600015D RID: 349 RVA: 0x000091D0 File Offset: 0x000073D0
		public int GetRoiBoxData(string paramKey, int type, ref float fCenterX, ref float fCenterY, ref float fWidth, ref float fHeight, ref float fAngle)
		{
			try
			{
				object obj = "";
				int num = 0;
				int objectValue = this.GetObjectValue(paramKey, type, 0, ref obj, ref num, -1);
				if (objectValue != 0)
				{
					return objectValue;
				}
				string text = (string)obj;
				if (text.Contains("\r"))
				{
					string[] array = text.Split(new char[]
					{
						'\r'
					});
					if (array.Length < 4)
					{
						return -536870911;
					}
					fCenterX = float.Parse(array[0]);
					fCenterY = float.Parse(array[1]);
					fWidth = float.Parse(array[2]);
					fHeight = float.Parse(array[3]);
					fAngle = float.Parse(array[4]);
					return 0;
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("GetImageData is error," + ex.ToString(), 0);
			}
			return -536870657;
		}

		// Token: 0x0600015E RID: 350 RVA: 0x000092D8 File Offset: 0x000074D8
		public int SetRoiBoxArrayData(string paramKey, RoiBoxArrayData roiBoxArray)
		{
			return 0;
		}

		// Token: 0x0600015F RID: 351 RVA: 0x000092EC File Offset: 0x000074EC
		public int GetRoiBoxArrayData(string paramKey, ref RoiBoxArrayData roiBoxArray)
		{
			return 0;
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00009300 File Offset: 0x00007500
		public int SetAnnulusArrayData(string paramKey, AnnulusArrayData annulusArray)
		{
			return 0;
		}

		// Token: 0x06000161 RID: 353 RVA: 0x00009314 File Offset: 0x00007514
		public int GetAnnulusArrayData(string paramKey, ref AnnulusArrayData annulusArray)
		{
			return 0;
		}

		// Token: 0x06000162 RID: 354 RVA: 0x00009328 File Offset: 0x00007528
		public int SetPolygonArrayData(string paramKey, PolygonArrayData polygonArray)
		{
			return 0;
		}

		// Token: 0x06000163 RID: 355 RVA: 0x0000933C File Offset: 0x0000753C
		public int GetPolygonArrayData(string paramKey, ref PolygonArrayData polygonArray)
		{
			return 0;
		}

		// Token: 0x06000164 RID: 356 RVA: 0x00009350 File Offset: 0x00007550
		public int SetPointArrayData(string paramKey, PointArrayData pointArray)
		{
			return 0;
		}

		// Token: 0x06000165 RID: 357 RVA: 0x00009364 File Offset: 0x00007564
		public int GetPointArrayData(string paramKey, ref PointArrayData pointArray)
		{
			return 0;
		}

		// Token: 0x06000166 RID: 358 RVA: 0x00009378 File Offset: 0x00007578
		public int SetLineArrayData(string paramKey, LineArrayData lineArray)
		{
			return 0;
		}

		// Token: 0x06000167 RID: 359 RVA: 0x0000938C File Offset: 0x0000758C
		public int GetLineArrayData(string paramKey, ref LineArrayData lineArray)
		{
			return 0;
		}

		// Token: 0x06000168 RID: 360 RVA: 0x000093A0 File Offset: 0x000075A0
		public int SetFixtureArrayData(string paramKey, FixtureArrayData fixtureArray)
		{
			return 0;
		}

		// Token: 0x06000169 RID: 361 RVA: 0x000093B4 File Offset: 0x000075B4
		public int GetFixtureArrayData(string paramKey, ref FixtureArrayData fixtureArray)
		{
			return 0;
		}

		// Token: 0x0600016A RID: 362 RVA: 0x000093C8 File Offset: 0x000075C8
		public int SetCircleArrayData(string paramKey, CircleArrayData circleArray)
		{
			return 0;
		}

		// Token: 0x0600016B RID: 363 RVA: 0x000093DC File Offset: 0x000075DC
		public int GetCircleArrayData(string paramKey, ref CircleArrayData circleArray)
		{
			return 0;
		}

		// Token: 0x0600016C RID: 364 RVA: 0x000093F0 File Offset: 0x000075F0
		public int SetRectArrayData(string paramKey, RectArrayData rectArray)
		{
			return 0;
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00009404 File Offset: 0x00007604
		public int GetRectArrayData(string paramKey, ref RectArrayData rectArray)
		{
			return 0;
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00009418 File Offset: 0x00007618
		public int SetEllipseArrayData(string paramKey, EllipseArrayData ellipseArray)
		{
			return 0;
		}

		// Token: 0x0600016F RID: 367 RVA: 0x0000942C File Offset: 0x0000762C
		public int GetEllipseArrayData(string paramKey, ref EllipseArrayData ellipseArray)
		{
			return 0;
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00009440 File Offset: 0x00007640
		public int SetPointsetData(string paramKey, byte[] pointset)
		{
			return 0;
		}

		// Token: 0x06000171 RID: 369 RVA: 0x00009454 File Offset: 0x00007654
		public int GetPointsetData(string paramKey, ref byte[] pointset)
		{
			return 0;
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00009468 File Offset: 0x00007668
		public byte[] ReadDataFromMemory(string address)
		{
			return MemoryHelper.VMReadFromMemory(address, 16);
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00009484 File Offset: 0x00007684
		public int SetObjectValueForModule(int moduleId, string paramKey, string paramValue, int valueType)
		{
			int result;
			if (moduleId == this.GlobalCommModuleId)
			{
				result = this.SetObjectValueSend(0, valueType, paramKey, paramValue, moduleId, 1);
			}
			else
			{
				SetValueInfo item = new SetValueInfo
				{
					Id = moduleId,
					ParamKey = paramKey,
					ParamValue = paramValue,
					SetType = 1
				};
				this.m_listSetValueInfo.Add(item);
				result = 0;
			}
			return result;
		}

		// Token: 0x06000174 RID: 372 RVA: 0x000094F0 File Offset: 0x000076F0
		public int GetObjectArrayValueSend(string paramKey, dynamic body, ref int nDataType, ref string[] paramValue)
		{
			paramValue = new string[0];
			int num = 0;
			int result;
			if (string.IsNullOrEmpty(paramKey))
			{
				result = -536870911;
			}
			else
			{
				try
				{
					this.GetSeqId();
					if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3c == null)
					{
						MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3c = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
					}
					Func<CallSite, object, string> target = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3c.Target;
					CallSite <>p__Site3c = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3c;
					if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3d == null)
					{
						MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3d = CallSite<Func<CallSite, MyAlgorithm, int, int, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.InvokeSimpleName, "JoinRequestJsonData", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.Constant, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					string sendInfo = target(<>p__Site3c, MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3d.Target(MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3d, this, 4004, this.uMySeqId, body));
					object arg = null;
					if (!this.CommunToModule(sendInfo, this.uMySeqId, ref arg, paramKey))
					{
						num = -536870888;
					}
					else
					{
						if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3e == null)
						{
							MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3e = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
						}
						Func<CallSite, object, string> target2 = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3e.Target;
						CallSite <>p__Site3e = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3e;
						if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3f == null)
						{
							MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3f = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "errorCode", typeof(MyAlgorithm), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target3 = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3f.Target;
						CallSite <>p__Site3f = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site3f;
						if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site40 == null)
						{
							MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site40 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(MyAlgorithm), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						string s = target2(<>p__Site3e, target3(<>p__Site3f, MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site40.Target(MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site40, arg)));
						int.TryParse(s, out num);
						if (num == 0)
						{
							if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site41 == null)
							{
								MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site41 = CallSite<Func<CallSite, object, int>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(int), typeof(MyAlgorithm)));
							}
							Func<CallSite, object, int> target4 = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site41.Target;
							CallSite <>p__Site = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site41;
							if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site42 == null)
							{
								MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site42 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "type", typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							Func<CallSite, object, object> target5 = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site42.Target;
							CallSite <>p__Site2 = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site42;
							if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site43 == null)
							{
								MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site43 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "body", typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							nDataType = target4(<>p__Site, target5(<>p__Site2, MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site43.Target(MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site43, arg)));
							if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site44 == null)
							{
								MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site44 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "paramValue", typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							Func<CallSite, object, object> target6 = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site44.Target;
							CallSite <>p__Site3 = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site44;
							if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site45 == null)
							{
								MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site45 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "body", typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							object arg2 = target6(<>p__Site3, MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site45.Target(MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site45, arg));
							if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site46 == null)
							{
								MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site46 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
								}));
							}
							Func<CallSite, object, bool> target7 = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site46.Target;
							CallSite <>p__Site4 = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site46;
							if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site47 == null)
							{
								MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site47 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.NotEqual, typeof(MyAlgorithm), new CSharpArgumentInfo[]
								{
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
									CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
								}));
							}
							if (target7(<>p__Site4, MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site47.Target(MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site47, arg2, null)))
							{
								List<string> list = new List<string>();
								if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site48 == null)
								{
									MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site48 = CallSite<Func<CallSite, object, IEnumerable>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(IEnumerable), typeof(MyAlgorithm)));
								}
								foreach (object arg3 in MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site48.Target(MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site48, arg2))
								{
									if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site49 == null)
									{
										MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site49 = CallSite<Action<CallSite, List<string>, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.ResultDiscarded, "Add", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
										{
											CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
											CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
										}));
									}
									Action<CallSite, List<string>, object> target8 = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site49.Target;
									CallSite <>p__Site5 = MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site49;
									List<string> arg4 = list;
									if (MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site4a == null)
									{
										MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site4a = CallSite<Func<CallSite, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "ToString", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
										{
											CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
										}));
									}
									target8(<>p__Site5, arg4, MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site4a.Target(MyAlgorithm.<GetObjectArrayValueSend>o__SiteContainer3b.<>p__Site4a, arg3));
								}
								paramValue = list.ToArray();
							}
						}
					}
				}
				catch (Exception ex)
				{
					LogHelper.Error("GetObjectArrayValue error " + ex.Message, 0);
					num = -536870657;
				}
				result = num;
			}
			return result;
		}

		// Token: 0x06000175 RID: 373 RVA: 0x00009B10 File Offset: 0x00007D10
		public int SetObjectValueEx(int index, int type, string paramKey, string paramValue)
		{
			int result;
			if (string.IsNullOrEmpty(paramKey))
			{
				result = -536870911;
			}
			else if (paramKey.Length >= 64)
			{
				result = -536870911;
			}
			else
			{
				SetValueInfo item = new SetValueInfo
				{
					Index = index,
					Type = type,
					ParamKey = paramKey,
					ParamValue = paramValue,
					SetType = 0
				};
				this.m_listSetValueInfo.Add(item);
				result = 0;
			}
			return result;
		}

		// Token: 0x06000176 RID: 374 RVA: 0x00009B90 File Offset: 0x00007D90
		public int SetObjectValue(int index, int type, string paramKey, object paramValue)
		{
			string text;
			if (type == 3 && paramValue is byte[])
			{
				text = Convert.ToBase64String((byte[])paramValue);
			}
			else
			{
				text = (paramValue as string);
			}
			int byteCount = Encoding.UTF8.GetByteCount(text);
			return this.SetObjectValueEx(index, type, paramKey, text);
		}

		// Token: 0x06000177 RID: 375 RVA: 0x00009BF8 File Offset: 0x00007DF8
		public int SetBasicArrayValue(int type, string paramKey, object paramValue)
		{
			int num = 0;
			int result;
			if (paramValue == null)
			{
				result = -536870911;
			}
			else
			{
				if (type == 0)
				{
					int[] array = (int[])paramValue;
					for (int i = 0; i < array.Length; i++)
					{
						num = this.SetObjectValue(i, 0, paramKey, array[i].ToString());
						if (num != 0)
						{
							break;
						}
					}
				}
				else if (type == 1)
				{
					float[] array2 = (float[])paramValue;
					for (int i = 0; i < array2.Length; i++)
					{
						num = this.SetObjectValue(i, 1, paramKey, array2[i].ToString());
						if (num != 0)
						{
							break;
						}
					}
				}
				result = num;
			}
			return result;
		}

		// Token: 0x06000178 RID: 376 RVA: 0x00009CCC File Offset: 0x00007ECC
		public void ClearSetObjectInfo()
		{
			this.nSetParamsLength = 0;
			if (this.m_listSetValueInfo != null)
			{
				this.m_listSetValueInfo.Clear();
			}
			else
			{
				this.m_listSetValueInfo = new List<SetValueInfo>();
			}
		}

		// Token: 0x06000179 RID: 377 RVA: 0x00009D0C File Offset: 0x00007F0C
		public int SendSetObjectInfo()
		{
			int result;
			if (this.m_listSetValueInfo == null || this.m_listSetValueInfo.Count == 0)
			{
				result = 0;
			}
			else
			{
				this.GetSeqId();
				string sendInfo = this.JoinRequestJsonData(4006, this.uMySeqId, this.m_listSetValueInfo);
				int num = 0;
				object arg = null;
				if (!this.CommunToModule(sendInfo, this.uMySeqId, ref arg, "list"))
				{
					num = -536870888;
				}
				else
				{
					if (MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4d == null)
					{
						MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4d = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
					}
					Func<CallSite, object, string> target = MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4d.Target;
					CallSite <>p__Site4d = MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4d;
					if (MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4e == null)
					{
						MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4e = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "errorCode", typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, object> target2 = MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4e.Target;
					CallSite <>p__Site4e = MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4e;
					if (MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4f == null)
					{
						MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4f = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					string text = target(<>p__Site4d, target2(<>p__Site4e, MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4f.Target(MyAlgorithm.<SendSetObjectInfo>o__SiteContainer4c.<>p__Site4f, arg)));
					if (!string.IsNullOrEmpty(text))
					{
						int.TryParse(text, out num);
					}
				}
				result = num;
			}
			return result;
		}

		// Token: 0x0600017A RID: 378 RVA: 0x00009E98 File Offset: 0x00008098
		public int SetObjectValueSend(int index, int type, string paramKey, string paramValue, int nModuleId = -1, int mode = 0)
		{
			int result;
			if (string.IsNullOrEmpty(paramKey))
			{
				result = -536870911;
			}
			else if (paramKey.Length >= 64)
			{
				result = -536870911;
			}
			else if (paramValue == null)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				List<SetValueInfo> respbody = new List<SetValueInfo>
				{
					new SetValueInfo
					{
						Id = nModuleId,
						Index = index,
						Type = type,
						ParamKey = paramKey,
						ParamValue = paramValue,
						SetType = mode
					}
				};
				try
				{
					this.GetSeqId();
					string sendInfo = this.JoinRequestJsonData(4006, this.uMySeqId, respbody);
					object arg = null;
					if (!this.CommunToModule(sendInfo, this.uMySeqId, ref arg, "list"))
					{
						num = -536870888;
					}
					else
					{
						if (MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site53 == null)
						{
							MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site53 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
						}
						Func<CallSite, object, string> target = MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site53.Target;
						CallSite <>p__Site = MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site53;
						if (MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site54 == null)
						{
							MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site54 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "errorCode", typeof(MyAlgorithm), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target2 = MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site54.Target;
						CallSite <>p__Site2 = MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site54;
						if (MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site55 == null)
						{
							MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site55 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(MyAlgorithm), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						string s = target(<>p__Site, target2(<>p__Site2, MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site55.Target(MyAlgorithm.<SetObjectValueSend>o__SiteContainer52.<>p__Site55, arg)));
						int.TryParse(s, out num);
					}
				}
				catch (Exception ex)
				{
					LogHelper.Error("SetObjectValue error " + ex.Message, 0);
					num = -536870657;
				}
				result = num;
			}
			return result;
		}

		// Token: 0x0600017B RID: 379 RVA: 0x0000A1B0 File Offset: 0x000083B0
		public int UpdateScriptCode()
		{
			int result = 0;
			try
			{
				this.GetSeqId();
				object arg = new
				{
					index = 0
				};
				if (MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site57 == null)
				{
					MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site57 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
				}
				Func<CallSite, object, string> target = MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site57.Target;
				CallSite <>p__Site = MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site57;
				if (MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site58 == null)
				{
					MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site58 = CallSite<Func<CallSite, MyAlgorithm, int, int, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.InvokeSimpleName, "JoinRequestJsonData", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.Constant, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
					}));
				}
				string sendInfo = target(<>p__Site, MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site58.Target(MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site58, this, 4010, this.uMySeqId, arg));
				object arg2 = null;
				if (!this.CommunToModule(sendInfo, this.uMySeqId, ref arg2, "updatecode"))
				{
					result = -536870888;
				}
				else
				{
					if (MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site59 == null)
					{
						MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site59 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
					}
					Func<CallSite, object, string> target2 = MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site59.Target;
					CallSite <>p__Site2 = MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site59;
					if (MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site5a == null)
					{
						MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site5a = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "errorCode", typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, object> target3 = MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site5a.Target;
					CallSite <>p__Site5a = MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site5a;
					if (MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site5b == null)
					{
						MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site5b = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(MyAlgorithm), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					string s = target2(<>p__Site2, target3(<>p__Site5a, MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site5b.Target(MyAlgorithm.<UpdateScriptCode>o__SiteContainer56.<>p__Site5b, arg2)));
					int.TryParse(s, out result);
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("SetObjectValue error " + ex.Message, 0);
				result = -536870657;
			}
			this.m_dictSubModuleResultInfo.Clear();
			return result;
		}

		// Token: 0x0600017C RID: 380 RVA: 0x0000A404 File Offset: 0x00008604
		private bool CommunToModule(string sendInfo, int nSeqid, ref dynamic receiveDyInfo, string paramkey)
		{
			bool flag = false;
			receiveDyInfo = null;
			string empty = string.Empty;
			int i = 0;
			bool result;
			if (!this.getParamPairMutex.WaitOne(5000))
			{
				this.getParamPairMutex.ReleaseMutex();
				result = false;
			}
			else
			{
				try
				{
					if (this.getParamPairMq != null)
					{
						if (this.getParamPairMq.SendData(sendInfo))
						{
							while (i <= 2000)
							{
								if (this.getParamPairMq.ReceiveData(ref empty))
								{
									receiveDyInfo = JsonConvert.DeserializeObject(empty);
									if (MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5d == null)
									{
										MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5d = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(MyAlgorithm), new CSharpArgumentInfo[]
										{
											CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
										}));
									}
									Func<CallSite, object, bool> target = MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5d.Target;
									CallSite <>p__Site5d = MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5d;
									if (MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5e == null)
									{
										MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5e = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.NotEqual, typeof(MyAlgorithm), new CSharpArgumentInfo[]
										{
											CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
											CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
										}));
									}
									if (target(<>p__Site5d, MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5e.Target(MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5e, receiveDyInfo, null)))
									{
										if (MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5f == null)
										{
											MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5f = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
										}
										Func<CallSite, object, string> target2 = MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5f.Target;
										CallSite <>p__Site5f = MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site5f;
										if (MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site60 == null)
										{
											MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site60 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "seqId", typeof(MyAlgorithm), new CSharpArgumentInfo[]
											{
												CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
											}));
										}
										Func<CallSite, object, object> target3 = MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site60.Target;
										CallSite <>p__Site = MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site60;
										if (MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site61 == null)
										{
											MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site61 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(MyAlgorithm), new CSharpArgumentInfo[]
											{
												CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
											}));
										}
										string a = target2(<>p__Site5f, target3(<>p__Site, MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site61.Target(MyAlgorithm.<CommunToModule>o__SiteContainer5c.<>p__Site61, receiveDyInfo)));
										if (a == nSeqid.ToString())
										{
											flag = true;
											break;
										}
									}
								}
								i += this.nRcvTimout;
							}
						}
					}
				}
				catch (Exception)
				{
					flag = false;
				}
				this.getParamPairMutex.ReleaseMutex();
				result = flag;
			}
			return result;
		}

		// Token: 0x0600017D RID: 381 RVA: 0x0000A6B4 File Offset: 0x000088B4
		private bool ReceiveMsg(IntPtr socket, int receiveSize, ref string retStr)
		{
			bool result = false;
			retStr = null;
			IntPtr intPtr = Marshal.AllocHGlobal(receiveSize);
			int num = Libzmq.zmq_recv(socket, intPtr, (uint)receiveSize, 0);
			if (num > 0)
			{
				byte[] array = new byte[num];
				Marshal.Copy(intPtr, array, 0, num);
				retStr = Encoding.UTF8.GetString(array);
				result = true;
			}
			Marshal.FreeCoTaskMem(intPtr);
			return result;
		}

		// Token: 0x0600017E RID: 382 RVA: 0x0000AB60 File Offset: 0x00008D60
		private string JoinResponseJsonData(int cmdID, int seqID, int errorCode, dynamic respbody)
		{
			object head = new
			{
				command = cmdID,
				type = "response",
				seqId = seqID,
				errorCode = errorCode
			};
			if (MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site63 == null)
			{
				MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site63 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(MyAlgorithm), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			Func<CallSite, object, bool> target = MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site63.Target;
			CallSite <>p__Site = MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site63;
			if (MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site64 == null)
			{
				MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site64 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Equal, typeof(MyAlgorithm), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
				}));
			}
			object arg;
			if (target(<>p__Site, MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site64.Target(MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site64, respbody, null)))
			{
				arg = new
				{
					head
				};
			}
			else
			{
				arg = new
				{
					head = head,
					body = respbody
				};
			}
			if (MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site65 == null)
			{
				MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site65 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
			}
			Func<CallSite, object, string> target2 = MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site65.Target;
			CallSite <>p__Site2 = MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site65;
			if (MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site66 == null)
			{
				MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site66 = CallSite<Func<CallSite, Type, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "SerializeObject", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsStaticType, null),
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			return target2(<>p__Site2, MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site66.Target(MyAlgorithm.<JoinResponseJsonData>o__SiteContainer62.<>p__Site66, typeof(JsonConvert), arg));
		}

		// Token: 0x0600017F RID: 383 RVA: 0x0000AEA0 File Offset: 0x000090A0
		private string JoinRequestJsonData(int cmdID, int seqID, dynamic respbody)
		{
			object head = new
			{
				command = cmdID,
				type = "request",
				seqId = seqID
			};
			if (MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site68 == null)
			{
				MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site68 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(MyAlgorithm), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			Func<CallSite, object, bool> target = MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site68.Target;
			CallSite <>p__Site = MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site68;
			if (MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site69 == null)
			{
				MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site69 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Equal, typeof(MyAlgorithm), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
				}));
			}
			object arg;
			if (target(<>p__Site, MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site69.Target(MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site69, respbody, null)))
			{
				arg = new
				{
					head
				};
			}
			else
			{
				arg = new
				{
					head = head,
					body = respbody
				};
			}
			if (MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site6a == null)
			{
				MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site6a = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(MyAlgorithm)));
			}
			Func<CallSite, object, string> target2 = MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site6a.Target;
			CallSite <>p__Site6a = MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site6a;
			if (MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site6b == null)
			{
				MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site6b = CallSite<Func<CallSite, Type, object, object>>.Create(Binder.InvokeMember(CSharpBinderFlags.None, "SerializeObject", null, typeof(MyAlgorithm), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsStaticType, null),
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			return target2(<>p__Site6a, MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site6b.Target(MyAlgorithm.<JoinRequestJsonData>o__SiteContainer67.<>p__Site6b, typeof(JsonConvert), arg));
		}

		// Token: 0x06000180 RID: 384 RVA: 0x0000B027 File Offset: 0x00009227
		private void GetSeqId()
		{
			Interlocked.Increment(ref this.uMySeqId);
		}

		// Token: 0x06000181 RID: 385 RVA: 0x0000B038 File Offset: 0x00009238
		public void ReleaseMapFileHandle()
		{
			if (this.ImageMemoryInfo != null)
			{
				foreach (KeyValuePair<string, MemoryInfo> keyValuePair in this.ImageMemoryInfo)
				{
					if (keyValuePair.Value.hBufferView != IntPtr.Zero)
					{
						MemoryHelper.UnmapViewOfFile(keyValuePair.Value.hBufferView);
						keyValuePair.Value.hBufferView = IntPtr.Zero;
					}
					if (keyValuePair.Value.hShareMemoryHandle != IntPtr.Zero)
					{
						MemoryHelper.CloseHandle(keyValuePair.Value.hShareMemoryHandle);
						keyValuePair.Value.hShareMemoryHandle = IntPtr.Zero;
					}
				}
				this.ImageMemoryInfo.Clear();
			}
		}

		// Token: 0x06000182 RID: 386 RVA: 0x0000B134 File Offset: 0x00009334
		private int WriteToMemory(byte[] byteData, ref MemoryInfo info)
		{
			IntPtr hFile = new IntPtr(-1);
			int num = byteData.Length + 16 + 64;
			try
			{
				if (info.dataLen < num)
				{
					if (info.hBufferView != IntPtr.Zero)
					{
						MemoryHelper.UnmapViewOfFile(info.hBufferView);
						info.hBufferView = IntPtr.Zero;
					}
					if (info.hShareMemoryHandle != IntPtr.Zero)
					{
						MemoryHelper.CloseHandle(info.hShareMemoryHandle);
						info.hShareMemoryHandle = IntPtr.Zero;
					}
					info.dataLen = num;
					info.hShareMemoryHandle = MemoryHelper.CreateFileMapping(hFile, IntPtr.Zero, 4, 0, num, info.memoryFileName);
					if (info.hShareMemoryHandle == IntPtr.Zero)
					{
						LogHelper.Error("create filemap error", 0);
						return -536870891;
					}
					info.hBufferView = MemoryHelper.MapViewOfFile(info.hShareMemoryHandle, 2, 0, 0, new IntPtr(num));
					if (info.hBufferView == IntPtr.Zero)
					{
						MemoryHelper.CloseHandle(info.hShareMemoryHandle);
						LogHelper.Error("create MapViewOfFile error", 0);
						return -536870891;
					}
				}
				if (info.hShareMemoryHandle == IntPtr.Zero || info.hBufferView == IntPtr.Zero)
				{
					LogHelper.Error("hShareMemoryHandle or hBufferView is error", 0);
					return -536870891;
				}
				byte[] bytes = BitConverter.GetBytes(byteData.Length);
				Marshal.Copy(bytes, 0, info.hBufferView, bytes.Length);
				Marshal.Copy(byteData, 0, info.hBufferView + 16, byteData.Length);
			}
			catch (Exception ex)
			{
				LogHelper.Error("create MapViewOfFile error," + ex.Message, 0);
				return -536870657;
			}
			return 0;
		}

		// Token: 0x040000AE RID: 174
		private const int RECEIVE_BUFFER_SIZE = 1024;

		// Token: 0x040000AF RID: 175
		private const int RECEIVE_MAX_BUFFER_SIZE = 102400;

		// Token: 0x040000B0 RID: 176
		private const int RECEIVE_MAX_RESULT_BUFFER_SIZE = 1024000;

		// Token: 0x040000B1 RID: 177
		private const int HEART_BUFFER_SIZE = 256;

		// Token: 0x040000B2 RID: 178
		private const int PROCESS_TIME_OUT = 2000;

		// Token: 0x040000B3 RID: 179
		private const int HEART_SEND_TIME = 10000;

		// Token: 0x040000B4 RID: 180
		private const int KEY_MAX_LEN = 64;

		// Token: 0x040000B5 RID: 181
		private const int VALUE_MAX_LEN = 4096;

		// Token: 0x040000B6 RID: 182
		private const string ProxyExeName = "VisionMaster.exe";

		// Token: 0x040000B7 RID: 183
		private const int INVALID_HANDLE_VALUE = -1;

		// Token: 0x040000B8 RID: 184
		private const int PAGE_READWRITE = 4;

		// Token: 0x040000B9 RID: 185
		private const int FILE_MAP_ALL_ACCESS = 2;

		// Token: 0x040000BE RID: 190
		private AddressInfo myAddressInfo = null;

		// Token: 0x040000BF RID: 191
		private IntPtr myZmqContext;

		// Token: 0x040000C0 RID: 192
		private int nRcvTimout = 500;

		// Token: 0x040000C1 RID: 193
		private int nWriteTimeout = 500;

		// Token: 0x040000C2 RID: 194
		private BaseZmqCommunicate heartPairMq = null;

		// Token: 0x040000C3 RID: 195
		private BaseZmqCommunicate setParamRepMq = null;

		// Token: 0x040000C4 RID: 196
		private BaseZmqCommunicate getParamPairMq = null;

		// Token: 0x040000C5 RID: 197
		private BaseZmqCommunicate processReqMq = null;

		// Token: 0x040000C6 RID: 198
		private IntPtr pHeartPairScoket = IntPtr.Zero;

		// Token: 0x040000C7 RID: 199
		private IntPtr pSetRepScoket = IntPtr.Zero;

		// Token: 0x040000C8 RID: 200
		private IntPtr pProcessRepScoket = IntPtr.Zero;

		// Token: 0x040000C9 RID: 201
		private IntPtr pGetPairScoket = IntPtr.Zero;

		// Token: 0x040000CA RID: 202
		private bool bSetRepTask;

		// Token: 0x040000CB RID: 203
		private bool bProcessRepTask;

		// Token: 0x040000CC RID: 204
		private int uMySeqId = 0;

		// Token: 0x040000CD RID: 205
		private System.Timers.Timer m_heartTimer = null;

		// Token: 0x040000CE RID: 206
		private List<SetValueInfo> m_listSetValueInfo = null;

		// Token: 0x040000CF RID: 207
		private Dictionary<string, GetModuleResultInfo> m_dictSubModuleResultInfo = null;

		// Token: 0x040000D0 RID: 208
		private int nSetParamsLength = 0;

		// Token: 0x040000D1 RID: 209
		private bool _dispose = false;

		// Token: 0x040000D2 RID: 210
		private object lockObj = new object();

		// Token: 0x040000D3 RID: 211
		private Task processTask = null;

		// Token: 0x040000D4 RID: 212
		private Task setParamTask = null;

		// Token: 0x040000D5 RID: 213
		private bool _enterDispose = false;

		// Token: 0x040000D6 RID: 214
		private bool _enterHeartTime = false;

		// Token: 0x040000D7 RID: 215
		private int GlobalCommModuleId = 11000;

		// Token: 0x040000D8 RID: 216
		private string strDefaultJson = "{\"head\": {\"command\": 4005,\"type\": \"request\",\"seqId\": 1},\"body\": []}";

		// Token: 0x040000D9 RID: 217
		private Mutex getParamPairMutex = null;

		// Token: 0x040000DA RID: 218
		private bool m_isExit = false;

		// Token: 0x040000DB RID: 219
		public Dictionary<string, MemoryInfo> ImageMemoryInfo = new Dictionary<string, MemoryInfo>();

		// Token: 0x02000044 RID: 68
		[CompilerGenerated]
		private static class <ReceiveProcessParamEvent>o__SiteContainer3
		{
			// Token: 0x040001E5 RID: 485
			public static CallSite<Func<CallSite, object, string>> <>p__Site4;

			// Token: 0x040001E6 RID: 486
			public static CallSite<Func<CallSite, MyAlgorithm, int, int, int, object, object>> <>p__Site5;

			// Token: 0x040001E7 RID: 487
			public static CallSite<Func<CallSite, object, string>> <>p__Site6;

			// Token: 0x040001E8 RID: 488
			public static CallSite<Func<CallSite, MyAlgorithm, int, int, int, object, object>> <>p__Site7;

			// Token: 0x040001E9 RID: 489
			public static CallSite<Func<CallSite, object, string>> <>p__Site8;

			// Token: 0x040001EA RID: 490
			public static CallSite<Func<CallSite, MyAlgorithm, int, int, int, object, object>> <>p__Site9;

			// Token: 0x040001EB RID: 491
			public static CallSite<Func<CallSite, object, string>> <>p__Sitea;

			// Token: 0x040001EC RID: 492
			public static CallSite<Func<CallSite, MyAlgorithm, int, int, int, object, object>> <>p__Siteb;
		}

		// Token: 0x02000046 RID: 70
		[CompilerGenerated]
		private static class <PraseJsonData>o__SiteContainere
		{
			// Token: 0x040001EE RID: 494
			public static CallSite<Func<CallSite, object, bool>> <>p__Sitef;

			// Token: 0x040001EF RID: 495
			public static CallSite<Func<CallSite, object, object, object>> <>p__Site10;

			// Token: 0x040001F0 RID: 496
			public static CallSite<Func<CallSite, object, string>> <>p__Site11;

			// Token: 0x040001F1 RID: 497
			public static CallSite<Func<CallSite, object, object>> <>p__Site12;

			// Token: 0x040001F2 RID: 498
			public static CallSite<Func<CallSite, object, object>> <>p__Site13;

			// Token: 0x040001F3 RID: 499
			public static CallSite<Func<CallSite, object, string>> <>p__Site14;

			// Token: 0x040001F4 RID: 500
			public static CallSite<Func<CallSite, object, object>> <>p__Site15;

			// Token: 0x040001F5 RID: 501
			public static CallSite<Func<CallSite, object, object>> <>p__Site16;

			// Token: 0x040001F6 RID: 502
			public static CallSite<Func<CallSite, object, string>> <>p__Site17;

			// Token: 0x040001F7 RID: 503
			public static CallSite<Func<CallSite, object, object>> <>p__Site18;

			// Token: 0x040001F8 RID: 504
			public static CallSite<Func<CallSite, object, object>> <>p__Site19;
		}

		// Token: 0x02000047 RID: 71
		[CompilerGenerated]
		private static class <GetObjectValueSend>o__SiteContainer1a
		{
			// Token: 0x040001F9 RID: 505
			public static CallSite<Func<CallSite, object, string>> <>p__Site1b;

			// Token: 0x040001FA RID: 506
			public static CallSite<Func<CallSite, MyAlgorithm, int, int, object, object>> <>p__Site1c;

			// Token: 0x040001FB RID: 507
			public static CallSite<Func<CallSite, object, string>> <>p__Site1d;

			// Token: 0x040001FC RID: 508
			public static CallSite<Func<CallSite, object, object>> <>p__Site1e;

			// Token: 0x040001FD RID: 509
			public static CallSite<Func<CallSite, object, object>> <>p__Site1f;

			// Token: 0x040001FE RID: 510
			public static CallSite<Func<CallSite, object, object>> <>p__Site20;

			// Token: 0x040001FF RID: 511
			public static CallSite<Func<CallSite, object, object>> <>p__Site21;

			// Token: 0x04000200 RID: 512
			public static CallSite<Func<CallSite, object, bool>> <>p__Site22;

			// Token: 0x04000201 RID: 513
			public static CallSite<Func<CallSite, object, object, object>> <>p__Site23;

			// Token: 0x04000202 RID: 514
			public static CallSite<Func<CallSite, object, bool>> <>p__Site24;

			// Token: 0x04000203 RID: 515
			public static CallSite<Func<CallSite, object, object, object>> <>p__Site25;

			// Token: 0x04000204 RID: 516
			public static CallSite<Func<CallSite, object, int, object>> <>p__Site26;

			// Token: 0x04000205 RID: 517
			public static CallSite<Func<CallSite, object, object>> <>p__Site27;

			// Token: 0x04000206 RID: 518
			public static CallSite<Func<CallSite, object, string>> <>p__Site28;

			// Token: 0x04000207 RID: 519
			public static CallSite<Func<CallSite, object, object>> <>p__Site29;

			// Token: 0x04000208 RID: 520
			public static CallSite<Func<CallSite, object, int, object>> <>p__Site2a;

			// Token: 0x04000209 RID: 521
			public static CallSite<Func<CallSite, object, string>> <>p__Site2b;

			// Token: 0x0400020A RID: 522
			public static CallSite<Func<CallSite, object, object>> <>p__Site2c;

			// Token: 0x0400020B RID: 523
			public static CallSite<Func<CallSite, object, object>> <>p__Site2d;
		}

		// Token: 0x02000049 RID: 73
		[CompilerGenerated]
		private static class <GetObjectArrayValue>o__SiteContainer32
		{
			// Token: 0x04000210 RID: 528
			public static CallSite<Func<CallSite, object, int>> <>p__Site33;

			// Token: 0x04000211 RID: 529
			public static CallSite<MyAlgorithm.<GetObjectArrayValue>o__SiteContainer32.<>q__SiteDelegate34> <>p__Site35;

			// Token: 0x0200004A RID: 74
			// (Invoke) Token: 0x06000288 RID: 648
			public delegate object <>q__SiteDelegate34(CallSite param0, MyAlgorithm param1, string param2, dynamic param3, ref int param4, ref string[] param5);
		}

		// Token: 0x0200004B RID: 75
		[CompilerGenerated]
		private static class <GetObjectArrayValueForModule>o__SiteContainer36
		{
			// Token: 0x04000212 RID: 530
			public static CallSite<Func<CallSite, object, int>> <>p__Site37;

			// Token: 0x04000213 RID: 531
			public static CallSite<MyAlgorithm.<GetObjectArrayValueForModule>o__SiteContainer36.<>q__SiteDelegate38> <>p__Site39;

			// Token: 0x0200004C RID: 76
			// (Invoke) Token: 0x0600028A RID: 650
			public delegate object <>q__SiteDelegate38(CallSite param0, MyAlgorithm param1, string param2, dynamic param3, ref int param4, ref string[] param5);
		}

		// Token: 0x0200004E RID: 78
		[CompilerGenerated]
		private static class <GetObjectArrayValueSend>o__SiteContainer3b
		{
			// Token: 0x04000219 RID: 537
			public static CallSite<Func<CallSite, object, string>> <>p__Site3c;

			// Token: 0x0400021A RID: 538
			public static CallSite<Func<CallSite, MyAlgorithm, int, int, object, object>> <>p__Site3d;

			// Token: 0x0400021B RID: 539
			public static CallSite<Func<CallSite, object, string>> <>p__Site3e;

			// Token: 0x0400021C RID: 540
			public static CallSite<Func<CallSite, object, object>> <>p__Site3f;

			// Token: 0x0400021D RID: 541
			public static CallSite<Func<CallSite, object, object>> <>p__Site40;

			// Token: 0x0400021E RID: 542
			public static CallSite<Func<CallSite, object, int>> <>p__Site41;

			// Token: 0x0400021F RID: 543
			public static CallSite<Func<CallSite, object, object>> <>p__Site42;

			// Token: 0x04000220 RID: 544
			public static CallSite<Func<CallSite, object, object>> <>p__Site43;

			// Token: 0x04000221 RID: 545
			public static CallSite<Func<CallSite, object, object>> <>p__Site44;

			// Token: 0x04000222 RID: 546
			public static CallSite<Func<CallSite, object, object>> <>p__Site45;

			// Token: 0x04000223 RID: 547
			public static CallSite<Func<CallSite, object, bool>> <>p__Site46;

			// Token: 0x04000224 RID: 548
			public static CallSite<Func<CallSite, object, object, object>> <>p__Site47;

			// Token: 0x04000225 RID: 549
			public static CallSite<Func<CallSite, object, IEnumerable>> <>p__Site48;

			// Token: 0x04000226 RID: 550
			public static CallSite<Action<CallSite, List<string>, object>> <>p__Site49;

			// Token: 0x04000227 RID: 551
			public static CallSite<Func<CallSite, object, object>> <>p__Site4a;
		}

		// Token: 0x0200004F RID: 79
		[CompilerGenerated]
		private static class <SendSetObjectInfo>o__SiteContainer4c
		{
			// Token: 0x04000228 RID: 552
			public static CallSite<Func<CallSite, object, string>> <>p__Site4d;

			// Token: 0x04000229 RID: 553
			public static CallSite<Func<CallSite, object, object>> <>p__Site4e;

			// Token: 0x0400022A RID: 554
			public static CallSite<Func<CallSite, object, object>> <>p__Site4f;
		}

		// Token: 0x02000050 RID: 80
		[CompilerGenerated]
		private static class <SetObjectValueSend>o__SiteContainer52
		{
			// Token: 0x0400022B RID: 555
			public static CallSite<Func<CallSite, object, string>> <>p__Site53;

			// Token: 0x0400022C RID: 556
			public static CallSite<Func<CallSite, object, object>> <>p__Site54;

			// Token: 0x0400022D RID: 557
			public static CallSite<Func<CallSite, object, object>> <>p__Site55;
		}

		// Token: 0x02000051 RID: 81
		[CompilerGenerated]
		private static class <UpdateScriptCode>o__SiteContainer56
		{
			// Token: 0x0400022E RID: 558
			public static CallSite<Func<CallSite, object, string>> <>p__Site57;

			// Token: 0x0400022F RID: 559
			public static CallSite<Func<CallSite, MyAlgorithm, int, int, object, object>> <>p__Site58;

			// Token: 0x04000230 RID: 560
			public static CallSite<Func<CallSite, object, string>> <>p__Site59;

			// Token: 0x04000231 RID: 561
			public static CallSite<Func<CallSite, object, object>> <>p__Site5a;

			// Token: 0x04000232 RID: 562
			public static CallSite<Func<CallSite, object, object>> <>p__Site5b;
		}

		// Token: 0x02000053 RID: 83
		[CompilerGenerated]
		private static class <CommunToModule>o__SiteContainer5c
		{
			// Token: 0x04000234 RID: 564
			public static CallSite<Func<CallSite, object, bool>> <>p__Site5d;

			// Token: 0x04000235 RID: 565
			public static CallSite<Func<CallSite, object, object, object>> <>p__Site5e;

			// Token: 0x04000236 RID: 566
			public static CallSite<Func<CallSite, object, string>> <>p__Site5f;

			// Token: 0x04000237 RID: 567
			public static CallSite<Func<CallSite, object, object>> <>p__Site60;

			// Token: 0x04000238 RID: 568
			public static CallSite<Func<CallSite, object, object>> <>p__Site61;
		}

		// Token: 0x02000054 RID: 84
		[CompilerGenerated]
		private static class <JoinResponseJsonData>o__SiteContainer62
		{
			// Token: 0x04000239 RID: 569
			public static CallSite<Func<CallSite, object, bool>> <>p__Site63;

			// Token: 0x0400023A RID: 570
			public static CallSite<Func<CallSite, object, object, object>> <>p__Site64;

			// Token: 0x0400023B RID: 571
			public static CallSite<Func<CallSite, object, string>> <>p__Site65;

			// Token: 0x0400023C RID: 572
			public static CallSite<Func<CallSite, Type, object, object>> <>p__Site66;
		}

		// Token: 0x02000058 RID: 88
		[CompilerGenerated]
		private static class <JoinRequestJsonData>o__SiteContainer67
		{
			// Token: 0x04000244 RID: 580
			public static CallSite<Func<CallSite, object, bool>> <>p__Site68;

			// Token: 0x04000245 RID: 581
			public static CallSite<Func<CallSite, object, object, object>> <>p__Site69;

			// Token: 0x04000246 RID: 582
			public static CallSite<Func<CallSite, object, string>> <>p__Site6a;

			// Token: 0x04000247 RID: 583
			public static CallSite<Func<CallSite, Type, object, object>> <>p__Site6b;
		}
	}
}
