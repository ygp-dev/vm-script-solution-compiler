using System;
using System.Diagnostics;

namespace Script.Algorithm
{
	// Token: 0x02000034 RID: 52
	public class Logger
	{
		// Token: 0x06000229 RID: 553 RVA: 0x0000DB7C File Offset: 0x0000BD7C
		public static void Init()
		{
			try
			{
				ScriptSDK.Shell_Init_Logger();
			}
			catch (Exception ex)
			{
				Debugger.Log(0, null, "Shell_Init_Logger is error:" + ex.ToString());
			}
		}

		// Token: 0x0600022A RID: 554 RVA: 0x0000DBC4 File Offset: 0x0000BDC4
		public static void Debug(int moduleid, string info)
		{
			ScriptSDK.Shell_Logger(moduleid, 0, info);
		}

		// Token: 0x0600022B RID: 555 RVA: 0x0000DBD0 File Offset: 0x0000BDD0
		public static void Info(int moduleid, string info)
		{
			ScriptSDK.Shell_Logger(moduleid, 1, info);
		}

		// Token: 0x0600022C RID: 556 RVA: 0x0000DBDC File Offset: 0x0000BDDC
		public static void Warn(int moduleid, string info)
		{
			ScriptSDK.Shell_Logger(moduleid, 2, info);
		}

		// Token: 0x0600022D RID: 557 RVA: 0x0000DBE8 File Offset: 0x0000BDE8
		public static void Error(int moduleid, string info)
		{
			ScriptSDK.Shell_Logger(moduleid, 3, info);
		}

		// Token: 0x0600022E RID: 558 RVA: 0x0000DBF4 File Offset: 0x0000BDF4
		public static void Trace(int moduleid, string info)
		{
			ScriptSDK.Shell_Logger(moduleid, 4, info);
		}
	}
}
