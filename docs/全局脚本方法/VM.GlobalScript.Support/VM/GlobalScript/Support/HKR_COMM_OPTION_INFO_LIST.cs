using System;
using System.Runtime.InteropServices;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000020 RID: 32
	public struct HKR_COMM_OPTION_INFO_LIST
	{
		// Token: 0x040000DB RID: 219
		public uint num;

		// Token: 0x040000DC RID: 220
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)]
		public HKR_COMM_OPTION_INFO[] ast_option_info;

		// Token: 0x040000DD RID: 221
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public int[] reserved;
	}
}
