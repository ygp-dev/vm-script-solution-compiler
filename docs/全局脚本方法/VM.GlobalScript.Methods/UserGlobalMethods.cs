using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using iMVS_6000PlatformSDKCS;

namespace VM.GlobalScript.Methods
{
	// Token: 0x0200000E RID: 14
	public class UserGlobalMethods : IDisposable
	{
		// Token: 0x06000073 RID: 115 RVA: 0x00003E00 File Offset: 0x00002000
		public int InitSDK()
		{
			if (this.dictProcessExecuteResetEvent == null)
			{
				this.dictProcessExecuteResetEvent = new Dictionary<uint, AutoResetEvent>();
			}
			PlatformSdkFunction.GetInstance().ResultCallBack = null;
			PlatformSdkFunction instance = PlatformSdkFunction.GetInstance();
			instance.ResultCallBack = (Action<IntPtr, IntPtr>)Delegate.Combine(instance.ResultCallBack, new Action<IntPtr, IntPtr>(this.ResultDataCallBack));
			PlatformSdkFunction.GetInstance().ExprotResultCallBack = null;
			PlatformSdkFunction instance2 = PlatformSdkFunction.GetInstance();
			instance2.ExprotResultCallBack = (Action<IntPtr, IntPtr>)Delegate.Combine(instance2.ExprotResultCallBack, new Action<IntPtr, IntPtr>(this.ExportResultCallBack));
			if (PlatformSdkFunction.GetInstance().m_operateHandle != IntPtr.Zero)
			{
				this.m_operateHandle = PlatformSdkFunction.GetInstance().m_operateHandle;
				this.GetProcessList();
				UDPTransTool.GetInstance().InitDequeue();
				this.UnRegesiterReceiveCommunicateDataEvent();
				this.RegesiterReceiveCommunicateDataEvent();
				return 0;
			}
			return -536870902;
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00002473 File Offset: 0x00000673
		public virtual int InitAfterLoadSol()
		{
			return 0;
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00003ED0 File Offset: 0x000020D0
		public int DefaultExecuteProcess()
		{
			LogHelper.objLog.Info("DefaultExecuteProcess start");
			int num;
			if (!this.bExecuteOnceOrContinues)
			{
				num = ImvsPlatformSDK_API.IMVS_PF_ExecuteOnce_CS(this.m_operateHandle, null);
				if (num != 0)
				{
					LogHelper.objLog.Error("IMVS_PF_ExecuteOnce_CS errorcode :" + ImvsPlatformSDK_API.IMVS_PF_GetErrorMsg_CS(num));
				}
				else
				{
					LogHelper.objLog.Info("IMVS_PF_ExecuteOnce_CS executed");
				}
			}
			else if (!this.bExecuteContinues)
			{
				num = ImvsPlatformSDK_API.IMVS_PF_ContinousExecute_CS(this.m_operateHandle);
				if (num != 0)
				{
					LogHelper.objLog.Error("IMVS_PF_ContinousExecute_CS errorcode :" + ImvsPlatformSDK_API.IMVS_PF_GetErrorMsg_CS(num));
				}
				else
				{
					LogHelper.objLog.Info("IMVS_PF_ContinousExecute_CS executed");
				}
				if (this.bCrash)
				{
					num = 0;
					this.bCrash = false;
				}
				this.bExecuteContinues = true;
			}
			else
			{
				num = 0;
			}
			LogHelper.objLog.Info("DefaultExecuteProcess end");
			return num;
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00003FA1 File Offset: 0x000021A1
		public void DefaultInitProcess(bool isExecute, bool isCrash)
		{
			this.bExecuteOnceOrContinues = isExecute;
			this.bExecuteContinues = false;
			this.bCrash = isCrash;
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00003FB8 File Offset: 0x000021B8
		public int ExecuteProcessOnce(string processName, string strCommand = "")
		{
			if (!this.dictProcessID.ContainsKey(processName))
			{
				return -1;
			}
			return ImvsPlatformSDK_API.IMVS_PF_ExecuteOnce_V30_CS(this.m_operateHandle, this.dictProcessID[processName], strCommand);
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00003FE2 File Offset: 0x000021E2
		public int ContinuousExecuteProcess(string processName)
		{
			if (!this.dictProcessID.ContainsKey(processName))
			{
				return -1;
			}
			return ImvsPlatformSDK_API.IMVS_PF_ContinousExecute_V30_CS(this.m_operateHandle, this.dictProcessID[processName]);
		}

		// Token: 0x06000079 RID: 121 RVA: 0x0000400B File Offset: 0x0000220B
		public int StopProcessExecute(string processName, uint nwaitime = 500U)
		{
			if (!this.dictProcessID.ContainsKey(processName))
			{
				return -1;
			}
			return ImvsPlatformSDK_API.IMVS_PF_StopExecute_V30_CS(this.m_operateHandle, this.dictProcessID[processName], nwaitime);
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00004035 File Offset: 0x00002235
		public void SetScriptContinuousExecuteInterval(uint nMilliSecond)
		{
			PlatformSdkFunction.GetInstance().ScriptContinusExecuteInterval = nMilliSecond;
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00004042 File Offset: 0x00002242
		public uint GetScriptContinuousExecuteInterval()
		{
			return PlatformSdkFunction.GetInstance().ScriptContinusExecuteInterval;
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00004050 File Offset: 0x00002250
		public void StartTryGlobalCommunicate()
		{
			if (!PlatformSdkFunction.GetInstance().GetRunMode())
			{
				return;
			}
			object comLock = this._comLock;
			lock (comLock)
			{
				if (!UDPTransTool.GetInstance().GetLocalUdpStatus())
				{
					LogHelper.objLog.Info("StartTryGlobalCommunicate start");
					if (PlatformSdkFunction.GetInstance().GetGlobalCommunicatePort(ref this.iGlobalScriptPort, ref this.iGloablCommPort) == 0 && UDPTransTool.GetInstance().StartUDPCommunicate(this.iGlobalScriptPort, this.iGloablCommPort) == 0)
					{
						this.UnRegesiterReceiveCommunicateDataEvent();
						this.RegesiterReceiveCommunicateDataEvent();
					}
				}
			}
		}

		// Token: 0x0600007D RID: 125 RVA: 0x000040F0 File Offset: 0x000022F0
		public int StartGlobalCommunicate()
		{
			int num = 0;
			if (!PlatformSdkFunction.GetInstance().GetRunMode())
			{
				return num;
			}
			object comLock = this._comLock;
			lock (comLock)
			{
				if (!UDPTransTool.GetInstance().GetLocalUdpStatus())
				{
					num = PlatformSdkFunction.GetInstance().GetGlobalCommunicatePort(ref this.iGlobalScriptPort, ref this.iGloablCommPort);
					if (num != 0)
					{
						if (this.bGetUdpPortFlag)
						{
							return num;
						}
						if (this.objTokenSource != null)
						{
							this.objTokenSource.Dispose();
							this.objTokenSource = null;
						}
						this.objTokenSource = new CancellationTokenSource();
						this.bGetUdpPortFlag = true;
						Task.Run(() => this.StartUDPCommunicte());
					}
					else
					{
						num = UDPTransTool.GetInstance().StartUDPCommunicate(this.iGlobalScriptPort, this.iGloablCommPort);
					}
				}
			}
			return num;
		}

		// Token: 0x0600007E RID: 126 RVA: 0x000041C8 File Offset: 0x000023C8
		public int SetGetPortCount(int nCount)
		{
			this.nGetUdpPortTryCount = nCount;
			return 0;
		}

		// Token: 0x0600007F RID: 127 RVA: 0x000041D2 File Offset: 0x000023D2
		public int SetGetPortDelayTime(int nTime)
		{
			this.nGetUdpPortDelayTime = nTime;
			return 0;
		}

		// Token: 0x06000080 RID: 128 RVA: 0x000041DC File Offset: 0x000023DC
		private async Task StartUDPCommunicte()
		{
			try
			{
				int count = 0;
				int num = 0;
				while (count < this.nGetUdpPortTryCount)
				{
					object comLock = this._comLock;
					lock (comLock)
					{
						num = PlatformSdkFunction.GetInstance().GetGlobalCommunicatePort(ref this.iGlobalScriptPort, ref this.iGloablCommPort);
						if (num == 0)
						{
							num = UDPTransTool.GetInstance().StartUDPCommunicate(this.iGlobalScriptPort, this.iGloablCommPort);
							if (num == 0)
							{
								LogHelper.objLog.Info(string.Format("Get communicate port success,iGlobalScriptPort:{0},iGloablCommPort:{1}", this.iGlobalScriptPort, this.iGloablCommPort));
								break;
							}
						}
					}
					if (num != 0)
					{
						int num2 = count;
						count = num2 + 1;
						await Task.Delay(this.nGetUdpPortDelayTime, this.objTokenSource.Token);
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.objLog.Error("StartUDPCommunicte task error," + ex.Message);
			}
			finally
			{
				this.bGetUdpPortFlag = false;
			}
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00004221 File Offset: 0x00002421
		public void RegesiterReceiveCommunicateDataEvent()
		{
			if (!this.bRegesiterReceiveEvent)
			{
				UDPTransTool.GetInstance().OnReceiveEvent += this.UserGlobalMethods_OnReceiveCommunicateDataEvent;
				this.bRegesiterReceiveEvent = true;
			}
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00004249 File Offset: 0x00002449
		public void UnRegesiterReceiveCommunicateDataEvent()
		{
			UDPTransTool.GetInstance().OnReceiveEvent -= this.UserGlobalMethods_OnReceiveCommunicateDataEvent;
			this.bRegesiterReceiveEvent = false;
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00002281 File Offset: 0x00000481
		public virtual void UserGlobalMethods_OnReceiveCommunicateDataEvent(ReceiveDataInfo dataInfo)
		{
		}

		// Token: 0x06000084 RID: 132 RVA: 0x0000426C File Offset: 0x0000246C
		public int SendCommDeviceData(string sendString, int deceiveID, int addressID = -1, DataType dataType = DataType.StringType)
		{
			if (string.IsNullOrEmpty(sendString))
			{
				return -1;
			}
			byte[] bytes = null;
			switch (dataType)
			{
			case DataType.StringType:
				bytes = PlatformSdkFunction.UTF8GetBytesPadZero(sendString);
				break;
			case DataType.IntType:
			{
				int[] array = Array.ConvertAll<string, int>(sendString.Split(new char[]
				{
					';'
				}), (string x) => int.Parse(x));
				List<byte> tmpbytes = new List<byte>();
				Array.ForEach<int>(array, delegate(int p)
				{
					tmpbytes.AddRange(BitConverter.GetBytes(p));
				});
				bytes = tmpbytes.ToArray();
				break;
			}
			case DataType.FloatType:
			{
				float[] array2 = Array.ConvertAll<string, float>(sendString.Split(new char[]
				{
					';'
				}), (string x) => float.Parse(x));
				List<byte> tmpFbytes = new List<byte>();
				Array.ForEach<float>(array2, delegate(float p)
				{
					tmpFbytes.AddRange(BitConverter.GetBytes(p));
				});
				bytes = tmpFbytes.ToArray();
				break;
			}
			case DataType.ByteType:
				bytes = Encoding.UTF8.GetBytes(sendString);
				break;
			}
			return PlatformSdkFunction.GetInstance().SendNormalData(bytes, deceiveID, addressID, (int)dataType, 1U);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x0000438F File Offset: 0x0000258F
		public int SendCommDeviceData(byte[] sendBytes, int deceiveID, int addressID = -1, DataType dataType = DataType.ByteType)
		{
			return PlatformSdkFunction.GetInstance().SendNormalData(sendBytes, deceiveID, addressID, (int)dataType, 1U);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x000043A4 File Offset: 0x000025A4
		public int GetGlobalVariableIntValue(string paramName, ref int paramValue)
		{
			string s = "";
			int globalVariable = PlatformSdkFunction.GetInstance().GetGlobalVariable(paramName, ref s);
			if (globalVariable != 0)
			{
				return globalVariable;
			}
			if (!int.TryParse(s, out paramValue))
			{
				return -536870911;
			}
			return globalVariable;
		}

		// Token: 0x06000087 RID: 135 RVA: 0x000043DC File Offset: 0x000025DC
		public int GetGlobalVariableFloatValue(string paramName, ref float paramValue)
		{
			string s = "";
			int globalVariable = PlatformSdkFunction.GetInstance().GetGlobalVariable(paramName, ref s);
			if (globalVariable != 0)
			{
				return globalVariable;
			}
			if (!float.TryParse(s, out paramValue))
			{
				return -536870911;
			}
			return globalVariable;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00004412 File Offset: 0x00002612
		public int GetGlobalVariableStringValue(string paramName, ref string paramValue)
		{
			return PlatformSdkFunction.GetInstance().GetGlobalVariable(paramName, ref paramValue);
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00004420 File Offset: 0x00002620
		public int SetGlobalVariableIntValue(string paramName, int paramValue)
		{
			return PlatformSdkFunction.GetInstance().SetGlobalVariable(paramName, paramValue.ToString());
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00004434 File Offset: 0x00002634
		public int SetGlobalVariableFloatValue(string paramName, float paramValue)
		{
			return PlatformSdkFunction.GetInstance().SetGlobalVariable(paramName, paramValue.ToString());
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00004448 File Offset: 0x00002648
		public int SetGlobalVariableStringValue(string paramName, string paramValue)
		{
			return PlatformSdkFunction.GetInstance().SetGlobalVariable(paramName, paramValue);
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00004456 File Offset: 0x00002656
		public int SetAllModuleResultRepor(bool bEnable)
		{
			return PlatformSdkFunction.GetInstance().SetModuleResultReport(0U, true, bEnable ? 1 : 0);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x0000446B File Offset: 0x0000266B
		public int SetModuleResultReport(uint nModuleID, bool bEnbale)
		{
			return PlatformSdkFunction.GetInstance().SetModuleResultReport(nModuleID, bEnbale, -1);
		}

		// Token: 0x0600008E RID: 142 RVA: 0x0000447C File Offset: 0x0000267C
		public void GetProcessList()
		{
			try
			{
				ImvsSdkPFDefine.IMVS_PF_PROCESS_INFO_LIST imvs_PF_PROCESS_INFO_LIST = default(ImvsSdkPFDefine.IMVS_PF_PROCESS_INFO_LIST);
				if (PlatformSdkFunction.GetInstance().GetAllProcess(ref imvs_PF_PROCESS_INFO_LIST))
				{
					this.dictProcessID.Clear();
					int num = 0;
					while ((long)num < (long)((ulong)imvs_PF_PROCESS_INFO_LIST.nNum))
					{
						string strProcessName = imvs_PF_PROCESS_INFO_LIST.astProcessInfo[num].strProcessName;
						if (this.dictProcessID.ContainsKey(strProcessName))
						{
							this.dictProcessID[strProcessName] = imvs_PF_PROCESS_INFO_LIST.astProcessInfo[num].nProcessID;
						}
						else
						{
							this.dictProcessID.Add(strProcessName, imvs_PF_PROCESS_INFO_LIST.astProcessInfo[num].nProcessID);
						}
						num++;
					}
				}
			}
			catch
			{
			}
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00004530 File Offset: 0x00002730
		public void ConsoleWrite(string content)
		{
			Debugger.Log(0, null, content);
		}

		// Token: 0x06000090 RID: 144 RVA: 0x0000453A File Offset: 0x0000273A
		public void Sleep(int millisecondsTimeout)
		{
			Thread.Sleep(millisecondsTimeout);
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00002281 File Offset: 0x00000481
		public virtual void ResultDataCallBack(IntPtr outputPlatformInfo, IntPtr puser)
		{
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00002281 File Offset: 0x00000481
		public virtual void ExportResultCallBack(IntPtr exportPlatformInfo, IntPtr puser)
		{
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00004544 File Offset: 0x00002744
		~UserGlobalMethods()
		{
			this.Dispose(false);
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00004574 File Offset: 0x00002774
		private void Dispose(bool dispose)
		{
			PlatformSdkFunction.GetInstance().ResultCallBack = null;
			this.m_operateHandle = IntPtr.Zero;
			UDPTransTool.GetInstance().OnReceiveEvent -= this.UserGlobalMethods_OnReceiveCommunicateDataEvent;
			UDPTransTool.GetInstance().Dispose();
			this.bRegesiterReceiveEvent = false;
			this.bGetUdpPortFlag = false;
			if (this.objTokenSource != null)
			{
				this.objTokenSource.Cancel();
				this.objTokenSource.Dispose();
				this.objTokenSource = null;
			}
			if (this.dictProcessExecuteResetEvent != null)
			{
				this.dictProcessExecuteResetEvent.Clear();
			}
			if (dispose)
			{
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00004607 File Offset: 0x00002807
		public virtual void Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x04000033 RID: 51
		public IntPtr m_operateHandle = IntPtr.Zero;

		// Token: 0x04000034 RID: 52
		private bool bRegesiterReceiveEvent;

		// Token: 0x04000035 RID: 53
		private int iGlobalScriptPort = -1;

		// Token: 0x04000036 RID: 54
		private int iGloablCommPort = -1;

		// Token: 0x04000037 RID: 55
		public Dictionary<uint, AutoResetEvent> dictProcessExecuteResetEvent;

		// Token: 0x04000038 RID: 56
		public const string strGlobalCommunicateIP = "127.0.0.1";

		// Token: 0x04000039 RID: 57
		private bool bExecuteOnceOrContinues;

		// Token: 0x0400003A RID: 58
		private bool bExecuteContinues;

		// Token: 0x0400003B RID: 59
		private bool bCrash;

		// Token: 0x0400003C RID: 60
		private int nGetUdpPortTryCount = 20;

		// Token: 0x0400003D RID: 61
		private CancellationTokenSource objTokenSource;

		// Token: 0x0400003E RID: 62
		private bool bGetUdpPortFlag;

		// Token: 0x0400003F RID: 63
		private int nGetUdpPortDelayTime = 1000;

		// Token: 0x04000040 RID: 64
		public Dictionary<string, uint> dictProcessID = new Dictionary<string, uint>();

		// Token: 0x04000041 RID: 65
		private object _comLock = new object();
	}
}
