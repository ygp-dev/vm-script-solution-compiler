using System;

namespace VM.GlobalScript.Methods
{
	// Token: 0x0200000C RID: 12
	public class PlatFormSDKManager
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00003D52 File Offset: 0x00001F52
		public uint ScriptContinusExecuteInterval
		{
			get
			{
				return this.m_objSDKFunction.ScriptContinusExecuteInterval;
			}
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00003D5F File Offset: 0x00001F5F
		public PlatFormSDKManager()
		{
			this.m_objSDKFunction = PlatformSdkFunction.GetInstance();
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00003D72 File Offset: 0x00001F72
		public void InitPlatformSDKEx(string ipaddress, string repAdress, int serPid, IntPtr skdHandle)
		{
			this.m_objSDKFunction.InitPlatformSDKEx(ipaddress, repAdress, serPid, skdHandle);
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00003D84 File Offset: 0x00001F84
		public int StopRunAllProcess()
		{
			return this.m_objSDKFunction.StopRunAllProcess();
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00003D91 File Offset: 0x00001F91
		public int SetVmRepAddr(string pairAddress)
		{
			return this.m_objSDKFunction.SetVmRepAddr(pairAddress);
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00003D9F File Offset: 0x00001F9F
		public int SilentlyExecuteOnce(int nSlientMode)
		{
			return this.m_objSDKFunction.SilentlyExecuteOnce(nSlientMode);
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00003DAD File Offset: 0x00001FAD
		public void BeforeExecuteProcessContinus()
		{
			this.m_objSDKFunction.BeforeExecuteProcessContinus();
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00003DBA File Offset: 0x00001FBA
		public void UinitSDK()
		{
			this.m_objSDKFunction.UinitSDK();
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00003DC7 File Offset: 0x00001FC7
		public void Enqueue(IntPtr ptrData)
		{
			UDPTransTool.GetInstance().Enqueue(ptrData);
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00003DD4 File Offset: 0x00001FD4
		public void SetRunMode(int mode)
		{
			this.m_objSDKFunction.SetRunMode(mode);
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00003DE2 File Offset: 0x00001FE2
		public void ReportData(string msg)
		{
			this.m_objSDKFunction.ReportData(msg);
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00003DF1 File Offset: 0x00001FF1
		public void Dispose()
		{
			UDPTransTool.GetInstance().Dispose();
		}

		// Token: 0x04000032 RID: 50
		private PlatformSdkFunction m_objSDKFunction;
	}
}
