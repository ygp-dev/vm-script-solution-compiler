using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Script.Algorithm
{
	// Token: 0x02000012 RID: 18
	public class HkrMqCommunicate : BaseZmqCommunicate
	{
		// Token: 0x060000EF RID: 239 RVA: 0x00005A54 File Offset: 0x00003C54
		public HkrMqCommunicate(ZmqDataContext context) : base(context)
		{
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00005BB4 File Offset: 0x00003DB4
		public override bool InitCommuncate()
		{
			try
			{
				int num = 0;
				this.socket = Libzmq.HkrCreate(this.zmqDataContext.ZmqType, ref num);
				if (num != 0)
				{
					LogHelper.objLog.Error("global script start：create hkr mq faild！");
					return false;
				}
				int rcvTimout = this.zmqDataContext.RcvTimout;
				int writeTimeOut = this.zmqDataContext.WriteTimeOut;
				if (Libzmq.HkrSetSocketOpt(this.socket, 0, ref rcvTimout, 4) != 0)
				{
					LogHelper.objLog.Error("global script start：mq receive time set faild！");
					return false;
				}
				if (Libzmq.HkrSetSocketOpt(this.socket, 1, ref writeTimeOut, 4) != 0)
				{
					LogHelper.objLog.Error("global script start：mq write time set faild！");
					return false;
				}
				if (this.zmqDataContext.ServerOrClient)
				{
					int num2 = Libzmq.HkrBind(this.socket, this.zmqDataContext.ConnectionString);
					if (num2 != 0)
					{
						LogHelper.objLog.Error(string.Format("Global Script Start Zmq Listen Faild:{0},ReturnCode:{1}", this.zmqDataContext.ConnectionString, num2));
						return false;
					}
				}
				else
				{
					int num2 = Libzmq.HkrConnect(this.socket, this.zmqDataContext.ConnectionString);
					if (num2 != 0)
					{
						LogHelper.objLog.Error(string.Format("Global Script Start Zmq Connect Faild:{0},ReturnCode:{1}", this.zmqDataContext.ConnectionString, num2));
						return false;
					}
				}
				if (this.zmqDataContext.StartReceiveTask)
				{
					this.zmqTask = Task.Run(async delegate()
					{
						await this.CallReceived();
					});
				}
			}
			catch (Exception ex)
			{
				LogHelper.objLog.Error("Init ZMQ Error " + ex.ToString());
				return false;
			}
			return true;
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00005FF4 File Offset: 0x000041F4
		public async Task CallReceived()
		{
			while (!this._dispose)
			{
				try
				{
					int bytesReceived = 0;
					if (Libzmq.HkrReceive(this.socket, ref this.receiveBufferPtr, ref bytesReceived) == 0 && bytesReceived > 0)
					{
						byte[] array = new byte[bytesReceived];
						Marshal.Copy(this.receiveBufferPtr, array, 0, bytesReceived);
						string @string = this.zmqDataContext.Encod.GetString(array);
						if (this.GetReceiveData != null)
						{
							this.GetReceiveData(@string);
						}
					}
					await Task.Delay(TimeSpan.FromMilliseconds(30.0), this._tokenSource.Token);
				}
				catch (Exception ex)
				{
					LogHelper.objLog.Error("Receive data zmq error " + ex.Message);
				}
			}
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00006040 File Offset: 0x00004240
		public override bool ReceiveData(ref string msg)
		{
			try
			{
				int num = 0;
				if (Libzmq.HkrReceive(this.socket, ref this.receiveBufferPtr, ref num) == 0 && num > 0)
				{
					byte[] array = new byte[num];
					Marshal.Copy(this.receiveBufferPtr, array, 0, num);
					msg = this.zmqDataContext.Encod.GetString(array);
					return true;
				}
			}
			catch (Exception ex)
			{
				LogHelper.objLog.Error("Receive data zmq error " + ex.Message);
			}
			return false;
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x000060E4 File Offset: 0x000042E4
		public override bool SendData(string msg)
		{
			int num = Libzmq.HkrSend(this.socket, msg, 0);
			return num >= 0;
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x00006110 File Offset: 0x00004310
		~HkrMqCommunicate()
		{
			this.Dispose(false);
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x00006144 File Offset: 0x00004344
		private void Dispose(bool dispose)
		{
			if (!this._dispose)
			{
				if (dispose)
				{
					this._dispose = true;
					try
					{
						if (!this.zmqDataContext.ServerOrClient)
						{
							Libzmq.HkrDisConnect(this.socket, this.zmqDataContext.ConnectionString);
						}
						Libzmq.HkrClose(this.socket);
						LogHelper.objLog.Info("Dispose zmq success");
					}
					catch (Exception ex)
					{
						LogHelper.objLog.Error("Dispose zmq error " + ex.Message);
					}
				}
			}
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x000061F8 File Offset: 0x000043F8
		public override void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x04000053 RID: 83
		private const int receiveBufferSize = 10240;

		// Token: 0x04000054 RID: 84
		private const int readDataTimeOut = 50;

		// Token: 0x04000055 RID: 85
		private const int receiveTimeOut = 3000;

		// Token: 0x04000056 RID: 86
		private bool _dispose;

		// Token: 0x04000057 RID: 87
		private IntPtr socket;

		// Token: 0x04000058 RID: 88
		private IntPtr receiveBufferPtr = IntPtr.Zero;

		// Token: 0x04000059 RID: 89
		private Task zmqTask;

		// Token: 0x0400005A RID: 90
		private CancellationTokenSource _tokenSource = new CancellationTokenSource();

		// Token: 0x0400005B RID: 91
		private readonly object lockObj = new object();
	}
}
