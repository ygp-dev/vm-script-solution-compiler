using System;
using System.Reflection;
using Apps.BaseLog;

namespace Script.Algorithm
{
	// Token: 0x0200001C RID: 28
	public class LogHelper
	{
		// Token: 0x06000133 RID: 307 RVA: 0x0000658C File Offset: 0x0000478C
		public static void InitLog(string fileName, bool iscom = false)
		{
			if (!iscom)
			{
				GlobalContext.Properties["LogName"] = fileName;
				LogHelper.objLog = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
			}
			else
			{
				Logger.Init();
			}
			LogHelper.IsCom = iscom;
		}

		// Token: 0x06000134 RID: 308 RVA: 0x000065D8 File Offset: 0x000047D8
		public static void Debug(string info, int moduleID = 0)
		{
			if (LogHelper.IsCom)
			{
				Logger.Debug(moduleID, info);
			}
			else if (LogHelper.objLog != null)
			{
				LogHelper.objLog.Debug(info);
			}
		}

		// Token: 0x06000135 RID: 309 RVA: 0x0000661C File Offset: 0x0000481C
		public static void Info(string info, int moduleID = 0)
		{
			if (LogHelper.IsCom)
			{
				Logger.Info(moduleID, info);
			}
			else if (LogHelper.objLog != null)
			{
				LogHelper.objLog.Info(info);
			}
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00006660 File Offset: 0x00004860
		public static void Warn(string info, int moduleID = 0)
		{
			if (LogHelper.IsCom)
			{
				Logger.Warn(moduleID, info);
			}
			else if (LogHelper.objLog != null)
			{
				LogHelper.objLog.Warn(info);
			}
		}

		// Token: 0x06000137 RID: 311 RVA: 0x000066A4 File Offset: 0x000048A4
		public static void Error(string info, int moduleID = 0)
		{
			if (LogHelper.IsCom)
			{
				Logger.Error(moduleID, info);
			}
			else if (LogHelper.objLog != null)
			{
				LogHelper.objLog.Error(info);
			}
		}

		// Token: 0x06000138 RID: 312 RVA: 0x000066E8 File Offset: 0x000048E8
		public static void Trace(string info, int moduleID = 0)
		{
			if (LogHelper.IsCom)
			{
				Logger.Trace(moduleID, info);
			}
		}

		// Token: 0x040000AC RID: 172
		public static bool IsCom = true;

		// Token: 0x040000AD RID: 173
		public static ILog objLog = null;
	}
}
