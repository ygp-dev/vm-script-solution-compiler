using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Apps.BaseLog;
using Apps.BaseLog.Config;

namespace VM.GlobalScript.Methods
{
	// Token: 0x02000007 RID: 7
	public class LogHelper
	{
		// Token: 0x0600002D RID: 45 RVA: 0x00002DAC File Offset: 0x00000FAC
		static LogHelper()
		{
			try
			{
				string text = Assembly.GetExecutingAssembly().Location;
				text = text.Substring(0, text.LastIndexOf('\\') + 1);
				XmlConfigurator.ConfigureAndWatch(new FileInfo(text + "Apps.BaseLog.config"));
				LogHelper.objLog = LogManager.GetLogger("UserGlobalScriptLog");
			}
			catch (Exception ex)
			{
				Debugger.Log(0, "", "GS:LogHelper error:" + ex.ToString());
			}
		}

		// Token: 0x04000019 RID: 25
		public static ILog objLog;
	}
}
