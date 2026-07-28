using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Apps.BaseLog;
using Apps.BaseLog.Config;

namespace VM.GlobalScript
{
	// Token: 0x0200000D RID: 13
	public class LogHelper
	{
		// Token: 0x0600003D RID: 61 RVA: 0x00002C68 File Offset: 0x00000E68
		static LogHelper()
		{
			try
			{
				string text = Assembly.GetExecutingAssembly().Location;
				text = text.Substring(0, text.LastIndexOf('\\') + 1);
				string text2 = text + "Apps.BaseLog.config";
				Debugger.Log(0, null, "GS:" + text2);
				GlobalContext.Properties["LogName"] = AppDomain.CurrentDomain.BaseDirectory + "\\log\\GlobalScript\\GlobalScript";
				GlobalContext.Properties["UserLogName"] = AppDomain.CurrentDomain.BaseDirectory + "\\log\\GlobalScript\\UserGlobalScript";
				XmlConfigurator.ConfigureAndWatch(new FileInfo(text2));
				LogHelper.objLog = LogManager.GetLogger("GlobalScriptLog");
				LogHelper._currentId = Process.GetCurrentProcess().Id;
				LogHelper.refreshParamthread = new Thread(delegate()
				{
					for (;;)
					{
						bool flag = LogHelper.dataNumForReceiveStore > 0;
						if (flag)
						{
							object[] array = LogHelper.forSendStore;
							Thread obj = LogHelper.refreshParamthread;
							lock (obj)
							{
								LogHelper.forSendStore = LogHelper.forReceiveStore;
								LogHelper.dataNumForSendStore = LogHelper.dataNumForReceiveStore;
								LogHelper.dataNumForReceiveStore = 0;
								LogHelper.forReceiveStore = array;
							}
							for (int i = 0; i < LogHelper.dataNumForSendStore; i++)
							{
								Tuple<string, string> tuple = LogHelper.forSendStore[i] as Tuple<string, string>;
								string item = tuple.Item1;
								if (!(item == "Info"))
								{
									if (!(item == "Debug"))
									{
										if (item == "Error")
										{
											LogHelper.objLog.Error(tuple.Item2);
										}
									}
									else
									{
										LogHelper.objLog.Debug(tuple.Item2);
									}
								}
								else
								{
									LogHelper.objLog.Info(tuple.Item2);
								}
								LogHelper.forSendStore[i] = null;
							}
							LogHelper.dataNumForSendStore = 0;
						}
						Thread.Sleep(1000);
					}
				})
				{
					IsBackground = true,
					Priority = ThreadPriority.BelowNormal
				};
				LogHelper.refreshParamthread.Start();
			}
			catch (Exception ex)
			{
				Debugger.Log(0, "", "GS:LogHelper error:" + ex.ToString());
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002DD0 File Offset: 0x00000FD0
		public static void Debug(object message)
		{
			DateTime now = DateTime.Now;
			string arg = string.Format("ProcessID-{0}-,{1}.{2} [{3}] [{4}] ", new object[]
			{
				LogHelper._currentId,
				now.ToString("yyyy-MM-dd HH:mm:ss"),
				now.Millisecond.ToString("000"),
				Thread.CurrentThread.ManagedThreadId,
				LogHelper.GetMethodName()
			});
			string item = arg + message;
			Tuple<string, string> tuple = new Tuple<string, string>("Debug", item);
			bool flag = LogHelper.dataNumForReceiveStore < LogHelper.maxStoreLen;
			if (flag)
			{
				Thread obj = LogHelper.refreshParamthread;
				lock (obj)
				{
					LogHelper.forReceiveStore[LogHelper.dataNumForReceiveStore] = tuple;
					LogHelper.dataNumForReceiveStore++;
				}
			}
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002EB8 File Offset: 0x000010B8
		public static void Error(object message)
		{
			DateTime now = DateTime.Now;
			string arg = string.Format("ProcessID-{0}-,{1}.{2} [{3}] [{4}] ", new object[]
			{
				LogHelper._currentId,
				now.ToString("yyyy-MM-dd HH:mm:ss"),
				now.Millisecond.ToString("000"),
				Thread.CurrentThread.ManagedThreadId,
				LogHelper.GetMethodName()
			});
			string item = arg + message;
			Tuple<string, string> tuple = new Tuple<string, string>("Error", item);
			bool flag = LogHelper.dataNumForReceiveStore < LogHelper.maxStoreLen;
			if (flag)
			{
				Thread obj = LogHelper.refreshParamthread;
				lock (obj)
				{
					LogHelper.forReceiveStore[LogHelper.dataNumForReceiveStore] = tuple;
					LogHelper.dataNumForReceiveStore++;
				}
			}
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00002FA0 File Offset: 0x000011A0
		public static void Info(object message)
		{
			DateTime now = DateTime.Now;
			string arg = string.Format("ProcessID-{0}-,{1}.{2} [{3}] [{4}] ", new object[]
			{
				LogHelper._currentId,
				now.ToString("yyyy-MM-dd HH:mm:ss"),
				now.Millisecond.ToString("000"),
				Thread.CurrentThread.ManagedThreadId,
				LogHelper.GetMethodName()
			});
			string item = arg + message;
			Tuple<string, string> tuple = new Tuple<string, string>("Info", item);
			bool flag = LogHelper.dataNumForReceiveStore < LogHelper.maxStoreLen;
			if (flag)
			{
				Thread obj = LogHelper.refreshParamthread;
				lock (obj)
				{
					LogHelper.forReceiveStore[LogHelper.dataNumForReceiveStore] = tuple;
					LogHelper.dataNumForReceiveStore++;
				}
			}
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00003088 File Offset: 0x00001288
		private static string GetMethodName()
		{
			StackTrace stackTrace = new StackTrace();
			StringBuilder stringBuilder = new StringBuilder();
			StackFrame frame = stackTrace.GetFrame(2);
			string text;
			if (frame == null)
			{
				text = null;
			}
			else
			{
				MethodBase method = frame.GetMethod();
				if (method == null)
				{
					text = null;
				}
				else
				{
					Type declaringType = method.DeclaringType;
					text = ((declaringType != null) ? declaringType.Name : null);
				}
			}
			string text2 = text;
			StackFrame frame2 = stackTrace.GetFrame(2);
			string text3;
			if (frame2 == null)
			{
				text3 = null;
			}
			else
			{
				MethodBase method2 = frame2.GetMethod();
				if (method2 == null)
				{
					text3 = null;
				}
				else
				{
					string name = method2.Name;
					text3 = ((name != null) ? name.ToString() : null);
				}
			}
			string text4 = text3;
			bool flag = text2 != null;
			if (flag)
			{
				stringBuilder.Append(text2);
			}
			bool flag2 = text4 != null;
			if (flag2)
			{
				stringBuilder.Append("::");
				stringBuilder.Append(text4);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x04000019 RID: 25
		private static Thread refreshParamthread = null;

		// Token: 0x0400001A RID: 26
		private static int dataNumForReceiveStore = 0;

		// Token: 0x0400001B RID: 27
		private static int dataNumForSendStore = 0;

		// Token: 0x0400001C RID: 28
		private static int maxStoreLen = 10000;

		// Token: 0x0400001D RID: 29
		private static object[] forReceiveStore = new object[LogHelper.maxStoreLen];

		// Token: 0x0400001E RID: 30
		private static object[] forSendStore = new object[LogHelper.maxStoreLen];

		// Token: 0x0400001F RID: 31
		private static int _currentId;

		// Token: 0x04000020 RID: 32
		private static readonly ILog objLog = null;
	}
}
