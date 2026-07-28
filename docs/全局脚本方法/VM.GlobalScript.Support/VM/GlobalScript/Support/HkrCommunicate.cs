using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000019 RID: 25
	public class HkrCommunicate : BaseZmqCommunicate
	{
		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06000080 RID: 128 RVA: 0x000045BC File Offset: 0x000027BC
		// (remove) Token: 0x06000081 RID: 129 RVA: 0x000045F4 File Offset: 0x000027F4
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public event Action<byte[]> GetReceiveBytesEventHanlder = null;

		// Token: 0x06000082 RID: 130 RVA: 0x0000462C File Offset: 0x0000282C
		public HkrCommunicate(ZmqDataContext context) : base(context)
		{
			this._pOperateHandle = IntPtr.Zero;
			this._struInitInfo = default(HKR_COMM_INIT_INFO);
			this._struInitInfo.option_info_list = default(HKR_COMM_OPTION_INFO_LIST);
			this._struInitInfo.option_info_list.ast_option_info = new HKR_COMM_OPTION_INFO[64];
			this._struInitInfo.reserved = new int[4];
			this._struInitInfo.option_info_list.reserved = new int[4];
			for (int i = 0; i < this._struInitInfo.option_info_list.ast_option_info.Length; i++)
			{
				this._struInitInfo.option_info_list.ast_option_info[i].reserved = new int[4];
			}
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00004718 File Offset: 0x00002918
		public override bool InitCommuncate()
		{
			bool result;
			try
			{
				string str = "";
				bool flag = this.InitHandle(HKR_COMM_COMMUNICATION_TYPE.HKR_COMM_COMMUNICATION_TYPE_ZeromqPair, ref str);
				bool flag2 = !flag;
				if (flag2)
				{
					LogHelper.Error("libHkrCommunication InitHandle error " + str);
					result = false;
				}
				else
				{
					flag = this.InitZeroMQ(this.zmqDataContext.ServerOrClient, this.zmqDataContext.ConnectionString, this.zmqDataContext.RcvTimout, this.zmqDataContext.WriteTimeOut, 0, ref str);
					bool flag3 = !flag;
					if (flag3)
					{
						LogHelper.Error("libHkrCommunication InitZeroMQ error " + str);
						result = false;
					}
					else
					{
						result = true;
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("libHkrCommunication InitCommuncate error " + ex.Message);
				result = false;
			}
			return result;
		}

		// Token: 0x06000084 RID: 132 RVA: 0x000047E0 File Offset: 0x000029E0
		public override bool SendData(string msg)
		{
			string str = "";
			bool flag = this.SendStringData(msg, ref str);
			bool flag2 = !flag;
			bool result;
			if (flag2)
			{
				LogHelper.Error("libHkrCommunication SendData error " + str);
				result = false;
			}
			else
			{
				result = true;
			}
			return result;
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00004824 File Offset: 0x00002A24
		private void Dispose(bool dispose)
		{
			bool dispose2 = this._dispose;
			if (!dispose2)
			{
				if (dispose)
				{
					try
					{
						this.DestoryHandle();
					}
					catch (Exception ex)
					{
						LogHelper.Error("Dispose zmq error " + ex.Message);
					}
				}
				this._dispose = true;
			}
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00004884 File Offset: 0x00002A84
		public override void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00004898 File Offset: 0x00002A98
		private bool InitHandle(HKR_COMM_COMMUNICATION_TYPE communication_type, ref string errorInfo)
		{
			bool flag = this._pOperateHandle != IntPtr.Zero;
			if (flag)
			{
				libHkrCommunication.HKR_COMM_DestroyHandle(this._pOperateHandle);
			}
			int num = libHkrCommunication.HKR_COMM_CreateHandle(ref this._pOperateHandle, (int)communication_type);
			bool flag2 = num == 0;
			bool result;
			if (flag2)
			{
				result = true;
			}
			else
			{
				errorInfo = this.getLastError();
				result = false;
			}
			return result;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x000048F0 File Offset: 0x00002AF0
		private bool DestoryHandle()
		{
			bool flag = this._pOperateHandle == IntPtr.Zero;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				int num = libHkrCommunication.HKR_COMM_DestroyHandle(this._pOperateHandle);
				this._pOperateHandle = IntPtr.Zero;
				this._bInitSucceed = false;
				result = (num == 0);
			}
			return result;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00004940 File Offset: 0x00002B40
		private bool InitZeroMQ(bool bServer, string strConnect, int iReadTime, int iWriteTime, int iLingerTime, ref string error)
		{
			bool flag = this._pOperateHandle == IntPtr.Zero;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool bInitSucceed = this._bInitSucceed;
				if (bInitSucceed)
				{
					result = true;
				}
				else
				{
					bool flag2 = false;
					IntPtr intPtr = IntPtr.Zero;
					IntPtr intPtr2 = IntPtr.Zero;
					IntPtr intPtr3 = IntPtr.Zero;
					IntPtr intPtr4 = IntPtr.Zero;
					IntPtr intPtr5 = IntPtr.Zero;
					try
					{
						intPtr = Marshal.StringToBSTR(strConnect);
						if (bServer)
						{
							this._struInitInfo.option_info_list.ast_option_info[0].option_type = 1;
						}
						else
						{
							this._struInitInfo.option_info_list.ast_option_info[0].option_type = 3;
						}
						this._struInitInfo.option_info_list.ast_option_info[0].option_value = intPtr;
						this._struInitInfo.option_info_list.ast_option_info[0].option_value_length = strConnect.Length + 1;
						intPtr2 = Marshal.AllocHGlobal(4);
						byte[] bytes = BitConverter.GetBytes(iReadTime);
						Marshal.Copy(bytes, 0, intPtr2, bytes.Length);
						this._struInitInfo.option_info_list.ast_option_info[1].option_type = 5;
						this._struInitInfo.option_info_list.ast_option_info[1].option_value = intPtr2;
						this._struInitInfo.option_info_list.ast_option_info[1].option_value_length = 4;
						intPtr3 = Marshal.AllocHGlobal(4);
						byte[] bytes2 = BitConverter.GetBytes(iWriteTime);
						Marshal.Copy(bytes, 0, intPtr3, bytes2.Length);
						this._struInitInfo.option_info_list.ast_option_info[2].option_type = 6;
						this._struInitInfo.option_info_list.ast_option_info[2].option_value = intPtr3;
						this._struInitInfo.option_info_list.ast_option_info[2].option_value_length = 4;
						intPtr4 = Marshal.AllocHGlobal(4);
						byte[] bytes3 = BitConverter.GetBytes(iLingerTime);
						Marshal.Copy(bytes3, 0, intPtr4, bytes3.Length);
						this._struInitInfo.option_info_list.ast_option_info[3].option_type = 7;
						this._struInitInfo.option_info_list.ast_option_info[3].option_value = intPtr4;
						this._struInitInfo.option_info_list.ast_option_info[3].option_value_length = 4;
						this._struInitInfo.option_info_list.num = 4U;
						intPtr5 = Marshal.AllocHGlobal(Marshal.SizeOf<HKR_COMM_INIT_INFO>(this._struInitInfo));
						Marshal.StructureToPtr<HKR_COMM_INIT_INFO>(this._struInitInfo, intPtr5, true);
						int num = libHkrCommunication.HKR_COMM_Init(this._pOperateHandle, intPtr5);
						bool flag3 = num == 0;
						if (flag3)
						{
							this._bInitSucceed = true;
							flag2 = true;
							this.zmqTask = Task.Run(async delegate()
							{
								await this.CallReceived();
							});
						}
						else
						{
							error = this.getLastError();
							this._bInitSucceed = false;
							flag2 = false;
						}
					}
					catch
					{
						flag2 = false;
					}
					finally
					{
						this.ReleaseIntptr(intPtr);
						this.ReleaseIntptr(intPtr2);
						this.ReleaseIntptr(intPtr3);
						this.ReleaseIntptr(intPtr4);
						this.ReleaseIntptr(intPtr5);
					}
					result = flag2;
				}
			}
			return result;
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00004C80 File Offset: 0x00002E80
		private void ReleaseIntptr(IntPtr ptr)
		{
			bool flag = ptr != IntPtr.Zero;
			if (flag)
			{
				Marshal.FreeHGlobal(ptr);
				ptr = IntPtr.Zero;
			}
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00004CB0 File Offset: 0x00002EB0
		private string getLastError()
		{
			string result;
			try
			{
				IntPtr ptr = libHkrCommunication.HKR_COMM_GetLastErrorMsg(this._pOperateHandle, 3);
				string text = Marshal.PtrToStringAnsi(ptr);
				result = text;
			}
			catch (Exception ex)
			{
				result = "获取错误日志异常";
			}
			return result;
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00004CF4 File Offset: 0x00002EF4
		private bool SendStringData(string msg, ref string errorcode)
		{
			bool flag = this._pOperateHandle == IntPtr.Zero;
			bool result;
			if (flag)
			{
				errorcode = 3758096385U.ToString();
				result = false;
			}
			else
			{
				bool flag2 = !this._bInitSucceed;
				if (flag2)
				{
					errorcode = 3758096385U.ToString();
					result = false;
				}
				else
				{
					byte[] bytes = Encoding.UTF8.GetBytes(msg);
					HKR_COMM_WRITE_INFO hkr_COMM_WRITE_INFO = new HKR_COMM_WRITE_INFO
					{
						write_buffer = Marshal.AllocHGlobal(bytes.Length)
					};
					Marshal.Copy(bytes, 0, hkr_COMM_WRITE_INFO.write_buffer, bytes.Length);
					hkr_COMM_WRITE_INFO.write_buffer_length = bytes.Length + 1;
					int num = libHkrCommunication.HKR_COMM_Write(this._pOperateHandle, ref hkr_COMM_WRITE_INFO);
					Marshal.FreeHGlobal(hkr_COMM_WRITE_INFO.write_buffer);
					bool flag3 = num == 0;
					if (flag3)
					{
						result = true;
					}
					else
					{
						errorcode = this.getLastError();
						result = false;
					}
				}
			}
			return result;
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00004DD0 File Offset: 0x00002FD0
		private async Task CallReceived()
		{
			while (!this._dispose)
			{
				HKR_COMM_READ_INFO readInfo = default(HKR_COMM_READ_INFO);
				readInfo.read_buffer = Marshal.AllocHGlobal(this.receiveBufferSize);
				readInfo.read_buffer_size = this.receiveBufferSize;
				try
				{
					int bret = libHkrCommunication.HKR_COMM_Read(this._pOperateHandle, ref readInfo);
					bool flag = bret == 0;
					if (flag)
					{
						bool flag2 = readInfo.read_buffer_length > this.receiveBufferSize;
						if (flag2)
						{
							this.receiveBufferSize = readInfo.read_buffer_length;
							LogHelper.Info("Receive data is too long, message maybe cutoff");
							continue;
						}
						byte[] buffer = new byte[readInfo.read_buffer_length];
						Marshal.Copy(readInfo.read_buffer, buffer, 0, buffer.Length);
						string message = this.zmqDataContext.Encod.GetString(buffer);
						bool flag3 = this.GetReceiveData != null;
						if (flag3)
						{
							this.GetReceiveData(message);
						}
						buffer = null;
						message = null;
					}
					await Task.Delay(TimeSpan.FromMilliseconds(30.0), this._tokenSource.Token);
				}
				catch (Exception ex)
				{
					LogHelper.Error("Receive data zmq error " + ex.Message);
				}
				finally
				{
					Marshal.FreeCoTaskMem(readInfo.read_buffer);
				}
				readInfo = default(HKR_COMM_READ_INFO);
			}
		}

		// Token: 0x04000094 RID: 148
		private IntPtr _pOperateHandle;

		// Token: 0x04000095 RID: 149
		private HKR_COMM_INIT_INFO _struInitInfo;

		// Token: 0x04000096 RID: 150
		private bool _bInitSucceed = false;

		// Token: 0x04000097 RID: 151
		private Task zmqTask = null;

		// Token: 0x04000098 RID: 152
		private bool _dispose;

		// Token: 0x04000099 RID: 153
		private int receiveBufferSize = 10240;

		// Token: 0x0400009B RID: 155
		private CancellationTokenSource _tokenSource = new CancellationTokenSource();
	}
}
