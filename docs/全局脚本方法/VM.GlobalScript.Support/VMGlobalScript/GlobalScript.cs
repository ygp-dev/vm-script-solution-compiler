using System;
using System.Diagnostics;
using VM.GlobalScript;
using VM.GlobalScript.Support;

namespace VMGlobalScript
{
	// Token: 0x0200000C RID: 12
	public class GlobalScript
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x06000034 RID: 52 RVA: 0x00002A90 File Offset: 0x00000C90
		// (remove) Token: 0x06000035 RID: 53 RVA: 0x00002AC8 File Offset: 0x00000CC8
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public event GlobalScriptReportMsg GlobalReportCallBackEvent = null;

		// Token: 0x06000036 RID: 54 RVA: 0x00002B00 File Offset: 0x00000D00
		public static GlobalScript GetInstance()
		{
			bool flag = GlobalScript._instance == null;
			if (flag)
			{
				GlobalScript._instance = new GlobalScript();
			}
			return GlobalScript._instance;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00002B30 File Offset: 0x00000D30
		public int Init(GloablScriptMode mode, IntPtr sdkHandle, string msg = null)
		{
			int result;
			try
			{
				bool flag = this.bInit;
				if (flag)
				{
					result = 0;
				}
				else
				{
					this.bInit = true;
					LogHelper.Info(" ====================================================V4.3.0 GlobalScript Start Print Log====================================================");
					bool flag2 = string.IsNullOrEmpty(msg) && mode == GloablScriptMode.EXE;
					if (flag2)
					{
						result = -536870911;
					}
					else
					{
						bool flag3 = mode == GloablScriptMode.EXE;
						if (flag3)
						{
							result = GlobalManager.GetInstance().StartByExe(msg);
						}
						else
						{
							int num = GlobalManager.GetInstance().StartByDll(sdkHandle);
							result = num;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debugger.Log(0, null, "Globals:Init is error:" + ex.ToString());
				result = -536870910;
			}
			return result;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002BDC File Offset: 0x00000DDC
		public int DeInit()
		{
			GlobalManager.GetInstance().Dispose();
			this.bInit = false;
			return 0;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002C04 File Offset: 0x00000E04
		public int HandleMsg(int cmd, IntPtr InMsg, int nMsgLen, ref string reMsg)
		{
			return GlobalManager.GetInstance().HandleMsg(cmd, InMsg, nMsgLen, ref reMsg);
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002C28 File Offset: 0x00000E28
		public int RegsiterRepotCallBack(GlobalScriptReportMsg reportMsg)
		{
			GlobalManager.GetInstance().RegsiterRepotCallBack(reportMsg);
			return 0;
		}

		// Token: 0x04000015 RID: 21
		private const string version = "V4.3.0";

		// Token: 0x04000017 RID: 23
		private static GlobalScript _instance = null;

		// Token: 0x04000018 RID: 24
		private bool bInit = false;
	}
}
