using System;
using System.Runtime.InteropServices;

namespace VM.GlobalScript.Support
{
	// Token: 0x0200001B RID: 27
	public class libHkrCommunication
	{
		// Token: 0x06000097 RID: 151
		[DllImport("hkr_communication.dll")]
		public static extern int HKR_COMM_CreateHandle(ref IntPtr handle, int communication_type);

		// Token: 0x06000098 RID: 152
		[DllImport("hkr_communication.dll")]
		public static extern int HKR_COMM_DestroyHandle(IntPtr handle);

		// Token: 0x06000099 RID: 153
		[DllImport("hkr_communication.dll")]
		public static extern int HKR_COMM_Init(IntPtr handle, IntPtr pst_init_info);

		// Token: 0x0600009A RID: 154
		[DllImport("hkr_communication.dll")]
		public static extern int HKR_COMM_Write(IntPtr handle, ref HKR_COMM_WRITE_INFO pst_write_info);

		// Token: 0x0600009B RID: 155
		[DllImport("hkr_communication.dll")]
		public static extern int HKR_COMM_Read(IntPtr handle, ref HKR_COMM_READ_INFO pst_read_info);

		// Token: 0x0600009C RID: 156
		[DllImport("hkr_communication.dll")]
		public static extern int HKR_COMM_SetOption(IntPtr handle, IntPtr pst_option_info);

		// Token: 0x0600009D RID: 157
		[DllImport("hkr_communication.dll")]
		public static extern int HKR_COMM_GetOption(IntPtr handle, ref HKR_COMM_OPTION_INFO pst_option_info);

		// Token: 0x0600009E RID: 158
		[DllImport("hkr_communication.dll")]
		public static extern int HKR_COMM_SetOptionList(IntPtr handle, IntPtr pst_option_info_list);

		// Token: 0x0600009F RID: 159
		[DllImport("hkr_communication.dll")]
		public static extern int HKR_COMM_GetOptionList(IntPtr handle, ref HKR_COMM_OPTION_INFO_LIST pst_option_info_list);

		// Token: 0x060000A0 RID: 160
		[DllImport("hkr_communication.dll")]
		public static extern IntPtr HKR_COMM_GetLastErrorMsg(IntPtr handle, int msg_type);

		// Token: 0x040000A5 RID: 165
		public const string ZeroMQPath = "hkr_communication.dll";
	}
}
