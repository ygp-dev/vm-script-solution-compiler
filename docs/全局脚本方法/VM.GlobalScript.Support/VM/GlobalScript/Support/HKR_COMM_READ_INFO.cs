using System;
using System.Runtime.InteropServices;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000023 RID: 35
	public struct HKR_COMM_READ_INFO
	{
		// Token: 0x040000E6 RID: 230
		public int read_flag;

		// Token: 0x040000E7 RID: 231
		public IntPtr read_buffer;

		// Token: 0x040000E8 RID: 232
		public int read_buffer_size;

		// Token: 0x040000E9 RID: 233
		public int read_buffer_length;

		// Token: 0x040000EA RID: 234
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public int[] reserved;
	}
}
