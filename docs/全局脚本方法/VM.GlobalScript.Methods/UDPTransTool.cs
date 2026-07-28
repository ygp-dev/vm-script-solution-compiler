using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VM.GlobalScript.Methods
{
	// Token: 0x02000004 RID: 4
	public class UDPTransTool : IDisposable
	{
		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06000015 RID: 21 RVA: 0x00002480 File Offset: 0x00000680
		// (remove) Token: 0x06000016 RID: 22 RVA: 0x000024B8 File Offset: 0x000006B8
		public event Action<ReceiveDataInfo> OnReceiveEvent;

		// Token: 0x06000017 RID: 23 RVA: 0x000024ED File Offset: 0x000006ED
		public static UDPTransTool GetInstance()
		{
			if (UDPTransTool._instance == null)
			{
				UDPTransTool._instance = new UDPTransTool();
			}
			return UDPTransTool._instance;
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002508 File Offset: 0x00000708
		private int InitUDPClientSocket()
		{
			try
			{
				object obj = this.objLock;
				lock (obj)
				{
					if (this.iLocalPort <= 0 || string.IsNullOrEmpty("127.0.0.1"))
					{
						LogHelper.objLog.Error(string.Format("Start udp faild,ip:{0} port:{1}", "127.0.0.1", this.iLocalPort));
						return -1;
					}
					if (this.objUDPSocket != null && this.objUDPSocket.IsBound)
					{
						return 0;
					}
					this.objUDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					IPEndPoint localEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), this.iLocalPort);
					this.objUDPSocket.Bind(localEP);
					if (this.receiveThread == null)
					{
						this.receiveThread = new Thread(new ThreadStart(this.ReceiveData));
						this.receiveProcess = true;
						this.receiveThread.IsBackground = true;
						this.receiveThread.Start();
						this.queueTaskCancel = false;
						this.StartDataQueue();
					}
					LogHelper.objLog.Info("UDP InitUDPClientSocket Data Succeed,Bind Port:" + this.iLocalPort.ToString());
				}
			}
			catch (Exception ex)
			{
				LogHelper.objLog.Error("UDP InitUDPClientSocket Data Error," + ex.Message);
				return -1;
			}
			return 0;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002684 File Offset: 0x00000884
		public int StartUDPCommunicate(int localPort, int destPort)
		{
			if (this.destIPEndPoint == null)
			{
				this.destIPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), destPort);
			}
			this.iLocalPort = localPort;
			return this.InitUDPClientSocket();
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000026B1 File Offset: 0x000008B1
		public bool GetLocalUdpStatus()
		{
			return this.destIPEndPoint != null && this.receiveProcess;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000026C4 File Offset: 0x000008C4
		private void ReceiveData()
		{
			List<byte> list = new List<byte>();
			int num = 0;
			int num2 = 0;
			try
			{
				while (this.receiveProcess)
				{
					try
					{
						if (this.objUDPSocket.Poll(-1, SelectMode.SelectRead))
						{
							EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
							byte[] array = new byte[10240];
							int num3 = this.objUDPSocket.ReceiveFrom(array, ref endPoint);
							if (this.OnReceiveEvent != null)
							{
								int count = list.Count;
								list.AddRange(array);
								list.RemoveRange(count + num3, list.Count - count - num3);
								if (list.Count < 11)
								{
									list.Clear();
									string msg = "";
									list.ForEach(delegate(byte x)
									{
										msg += x.ToString("X2");
									});
									LogHelper.objLog.Error("udp receive data error " + msg);
								}
								else
								{
									list.Reverse(3, 8);
									int num4 = (int)BitConverter.ToInt64(list.ToArray(), 3);
									if (list.Count < 11 + num4)
									{
										num2++;
										list.Reverse(3, 8);
										string msg = "";
										list.ForEach(delegate(byte x)
										{
											msg += x.ToString("X2");
										});
										LogHelper.objLog.Info("udp receive data too long " + msg);
										if (num2 > 3)
										{
											num2 = 0;
											list.Clear();
										}
									}
									else
									{
										num2 = 0;
										ReceiveDataInfo receiveDataInfo = new ReceiveDataInfo();
										receiveDataInfo.CommunicateType = (CommunicateType)list[0];
										receiveDataInfo.DeviceID = (int)list[1];
										receiveDataInfo.DeviceAddressID = (int)list[2];
										list.RemoveRange(0, 11);
										receiveDataInfo.DeviceData = list.Take(num4).ToArray<byte>();
										this.Enqueue(receiveDataInfo);
										list.RemoveRange(0, num4);
										if (list.Count > 0)
										{
											if (num > 3)
											{
												num = 0;
												list.Clear();
											}
											else
											{
												num++;
											}
										}
										else
										{
											num = 0;
										}
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						list.Clear();
						num = 0;
						num2 = 0;
						LogHelper.objLog.Error("UDP Receive Data Error," + ex.Message);
					}
				}
			}
			catch (ThreadAbortException)
			{
				LogHelper.objLog.Error("UDP ReceiveThread Stop Exception");
			}
			catch (Exception ex2)
			{
				LogHelper.objLog.Error("UDP Receive Data Error," + ex2.Message);
			}
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002960 File Offset: 0x00000B60
		public void InitDequeue()
		{
			this.queueTaskCancel = false;
			this.StartDataQueue();
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002970 File Offset: 0x00000B70
		public object Dequeue()
		{
			object result = null;
			this.objDataQueue.TryDequeue(out result);
			return result;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002990 File Offset: 0x00000B90
		public void Enqueue(IntPtr ptrData)
		{
			if (ptrData == IntPtr.Zero)
			{
				return;
			}
			IMVS_COMMU_REPORT_DATA_INFO imvs_COMMU_REPORT_DATA_INFO = (IMVS_COMMU_REPORT_DATA_INFO)Marshal.PtrToStructure(ptrData, typeof(IMVS_COMMU_REPORT_DATA_INFO));
			if (imvs_COMMU_REPORT_DATA_INFO.pData == IntPtr.Zero || imvs_COMMU_REPORT_DATA_INFO.nLen <= 1)
			{
				return;
			}
			byte[] array = new byte[imvs_COMMU_REPORT_DATA_INFO.nLen];
			Marshal.Copy(imvs_COMMU_REPORT_DATA_INFO.pData, array, 0, imvs_COMMU_REPORT_DATA_INFO.nLen);
			ReceiveDataInfo receiveDataInfo = new ReceiveDataInfo();
			receiveDataInfo.CommunicateType = (CommunicateType)imvs_COMMU_REPORT_DATA_INFO.nType;
			receiveDataInfo.DeviceID = (int)array[0];
			receiveDataInfo.DeviceAddressID = (int)array[1];
			receiveDataInfo.DeviceAddressID = ((receiveDataInfo.DeviceAddressID == 255) ? -1 : receiveDataInfo.DeviceAddressID);
			receiveDataInfo.DeviceData = array.Skip(2).ToArray<byte>();
			this.Enqueue(receiveDataInfo);
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002A55 File Offset: 0x00000C55
		public void Enqueue(object data)
		{
			if (this.objDataQueue.Count > 500)
			{
				return;
			}
			this.objDataQueue.Enqueue(data);
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002A76 File Offset: 0x00000C76
		private void StartDataQueue()
		{
			if (this.queueTask == null)
			{
				this.queueTask = new Task(delegate()
				{
					while (!this.queueTaskCancel)
					{
						if (!this.objDataQueue.IsEmpty)
						{
							this.DoDataDequeue();
						}
						Thread.Sleep(1);
					}
				}, TaskCreationOptions.LongRunning);
				this.queueTask.Start();
			}
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002AA4 File Offset: 0x00000CA4
		public void StopDataQueue()
		{
			this.queueTaskCancel = true;
			if (this.queueTask != null)
			{
				this.queueTask.Wait(10000);
				this.queueTask = null;
			}
			if (!this.objDataQueue.IsEmpty)
			{
				object obj = null;
				while (this.objDataQueue.TryDequeue(out obj))
				{
				}
			}
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002AF8 File Offset: 0x00000CF8
		private void DoDataDequeue()
		{
			try
			{
				object obj = this.Dequeue();
				if (obj != null)
				{
					if (this.OnReceiveEvent != null && obj is ReceiveDataInfo)
					{
						this.OnReceiveEvent(obj as ReceiveDataInfo);
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.objLog.Error(ex.Message);
			}
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002B58 File Offset: 0x00000D58
		public bool SendUdpDataInfo(string msg, Encoding encod)
		{
			return this.objUDPSocket != null && this.destIPEndPoint != null && msg != null && this.objUDPSocket.SendTo(encod.GetBytes(msg), this.destIPEndPoint) >= 0;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002B8D File Offset: 0x00000D8D
		public bool SendUdpDataInfo(byte[] bytes)
		{
			return this.objUDPSocket != null && this.destIPEndPoint != null && bytes != null && this.objUDPSocket.SendTo(bytes, this.destIPEndPoint) >= 0;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002BBC File Offset: 0x00000DBC
		private int FindNextAvailableUDPPort(int startPort)
		{
			int num = startPort;
			bool flag = true;
			int i;
			try
			{
				IPEndPoint[] activeUdpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
				do
				{
					if (!flag)
					{
						num++;
						flag = true;
					}
					if (activeUdpListeners == null)
					{
						break;
					}
					IPEndPoint[] array = activeUdpListeners;
					for (i = 0; i < array.Length; i++)
					{
						if (array[i].Port == num)
						{
							flag = false;
							break;
						}
					}
				}
				while (!flag && num < 65535);
				if (!flag)
				{
					i = -1;
				}
				else
				{
					i = num;
				}
			}
			catch (Exception)
			{
				i = -1;
			}
			return i;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002C38 File Offset: 0x00000E38
		private int FindNextAvailableTCPPort(int startPort)
		{
			int num = startPort;
			bool flag = true;
			int i;
			try
			{
				IPEndPoint[] activeTcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
				do
				{
					if (!flag)
					{
						num++;
						flag = true;
					}
					IPEndPoint[] array = activeTcpListeners;
					for (i = 0; i < array.Length; i++)
					{
						if (array[i].Port == num)
						{
							flag = false;
							break;
						}
					}
				}
				while (!flag && num < 65535);
				if (!flag)
				{
					throw new ApplicationException("Not able to find a free TCP port.");
				}
				i = num;
			}
			catch
			{
				i = -1;
			}
			return i;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002CB8 File Offset: 0x00000EB8
		public void Dispose()
		{
			this.receiveProcess = false;
			this.destIPEndPoint = null;
			if (this.objUDPSocket != null)
			{
				try
				{
					this.objUDPSocket.Shutdown(SocketShutdown.Both);
					this.objUDPSocket.Close();
				}
				catch (Exception ex)
				{
					LogHelper.objLog.Error("UDP Dispose Error," + ex.Message);
				}
				this.objUDPSocket.Dispose();
				this.objUDPSocket = null;
			}
			if (this.receiveThread != null)
			{
				this.receiveThread.Abort();
				this.receiveThread.Join();
				this.receiveThread = null;
			}
			this.StopDataQueue();
		}

		// Token: 0x04000004 RID: 4
		private IPEndPoint destIPEndPoint;

		// Token: 0x04000005 RID: 5
		private Socket objUDPSocket;

		// Token: 0x04000006 RID: 6
		private Thread receiveThread;

		// Token: 0x04000007 RID: 7
		private bool receiveProcess;

		// Token: 0x04000009 RID: 9
		private const int iReceiveBufferSize = 10240;

		// Token: 0x0400000A RID: 10
		private const string strLocalIP = "127.0.0.1";

		// Token: 0x0400000B RID: 11
		public int iLocalPort = -1;

		// Token: 0x0400000C RID: 12
		private const int iProtocollHeadLen = 11;

		// Token: 0x0400000D RID: 13
		private const int iProtocolHeadLenIndex = 3;

		// Token: 0x0400000E RID: 14
		private const int iReceiveErrorCount = 3;

		// Token: 0x0400000F RID: 15
		private readonly ConcurrentQueue<object> objDataQueue = new ConcurrentQueue<object>();

		// Token: 0x04000010 RID: 16
		private bool queueTaskCancel;

		// Token: 0x04000011 RID: 17
		private Task queueTask;

		// Token: 0x04000012 RID: 18
		private const int maxQueueCount = 500;

		// Token: 0x04000013 RID: 19
		private object objLock = new object();

		// Token: 0x04000014 RID: 20
		private static UDPTransTool _instance;
	}
}
