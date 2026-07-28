using System;
using System.Collections.Generic;
using System.Linq;

namespace VM.GlobalScript.Methods
{
	// Token: 0x02000003 RID: 3
	public class GlobalCommunicateTran
	{
		// Token: 0x0600000A RID: 10 RVA: 0x00002339 File Offset: 0x00000539
		public static GlobalCommunicateTran GetInstance()
		{
			if (GlobalCommunicateTran._instance == null)
			{
				GlobalCommunicateTran._instance = new GlobalCommunicateTran();
			}
			return GlobalCommunicateTran._instance;
		}

		// Token: 0x14000001 RID: 1
		// (add) Token: 0x0600000B RID: 11 RVA: 0x00002354 File Offset: 0x00000554
		// (remove) Token: 0x0600000C RID: 12 RVA: 0x0000238C File Offset: 0x0000058C
		public event OnReceiveEventHandler OnReceiveEvent;

		// Token: 0x0600000D RID: 13 RVA: 0x000023C1 File Offset: 0x000005C1
		public bool InitCommunicate()
		{
			if (this._udpTransTool != null)
			{
				this._udpTransTool.Dispose();
				this._udpTransTool = null;
			}
			this._udpTransTool = new UDPTransTool();
			return false;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000023E9 File Offset: 0x000005E9
		public bool GetLocalUdpStatus()
		{
			return this._udpTransTool != null && this._udpTransTool.GetLocalUdpStatus();
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002400 File Offset: 0x00000600
		public int GetLocalPort()
		{
			if (this._udpTransTool != null)
			{
				return this._udpTransTool.iLocalPort;
			}
			return -1;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002418 File Offset: 0x00000618
		private void OnReceiveData(byte[] arg1, int len)
		{
			if (this.OnReceiveEvent != null)
			{
				List<byte> list = arg1.ToList<byte>();
				list.RemoveRange(len, arg1.Length - len);
				ReceiveDataInfo receiveDataInfo = new ReceiveDataInfo();
				receiveDataInfo.DeviceData = list.ToArray();
				this.OnReceiveEvent(receiveDataInfo);
			}
		}

		// Token: 0x06000011 RID: 17 RVA: 0x0000245E File Offset: 0x0000065E
		public void Dispose()
		{
			if (this._udpTransTool != null)
			{
				this._udpTransTool.Dispose();
			}
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002473 File Offset: 0x00000673
		public bool SendData(int communicateIndex, string msg, string PlcAddress = null)
		{
			return false;
		}

		// Token: 0x04000001 RID: 1
		private static GlobalCommunicateTran _instance;

		// Token: 0x04000002 RID: 2
		private UDPTransTool _udpTransTool;
	}
}
