using System;
using System.Runtime.InteropServices;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000022 RID: 34
	public struct HKR_COMM_WRITE_INFO
	{
		// Token: 0x040000E3 RID: 227
		public IntPtr write_buffer;

		// Token: 0x040000E4 RID: 228
		public int write_buffer_length;

		// Token: 0x040000E5 RID: 229
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public int[] reserved;
	}
}
