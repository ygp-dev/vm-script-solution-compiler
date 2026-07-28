using System;
using System.Runtime.InteropServices;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000021 RID: 33
	public struct HKR_COMM_OPTION_INFO
	{
		// Token: 0x040000DE RID: 222
		public int option_type;

		// Token: 0x040000DF RID: 223
		public IntPtr option_value;

		// Token: 0x040000E0 RID: 224
		public int option_value_size;

		// Token: 0x040000E1 RID: 225
		public int option_value_length;

		// Token: 0x040000E2 RID: 226
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public int[] reserved;
	}
}
