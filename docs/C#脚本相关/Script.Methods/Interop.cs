using System;
using System.Runtime.InteropServices;

namespace Script.Methods
{
	// Token: 0x02000003 RID: 3
	public class Interop
	{
		// Token: 0x06000014 RID: 20 RVA: 0x00002CF8 File Offset: 0x00000EF8
		public static void ShowMessageBox(string message, string title)
		{
			int num = 0;
			Interop.WTSSendMessage(Interop.WTS_CURRENT_SERVER_HANDLE, Interop.WTSGetActiveConsoleSessionId(), title, title.Length, message, message.Length, 0, 0, out num, false);
		}

		// Token: 0x06000015 RID: 21
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern int WTSGetActiveConsoleSessionId();

		// Token: 0x06000016 RID: 22
		[DllImport("wtsapi32.dll", SetLastError = true)]
		public static extern bool WTSSendMessage(IntPtr hServer, int SessionId, string pTitle, int TitleLength, string pMessage, int MessageLength, int Style, int Timeout, out int pResponse, bool bWait);

		// Token: 0x04000001 RID: 1
		public static IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;
	}
}
