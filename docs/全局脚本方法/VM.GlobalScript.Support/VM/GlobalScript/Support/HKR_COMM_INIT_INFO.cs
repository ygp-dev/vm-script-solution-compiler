using System;
using System.Runtime.InteropServices;

namespace VM.GlobalScript.Support
{
	// Token: 0x0200001F RID: 31
	public struct HKR_COMM_INIT_INFO
	{
		// Token: 0x040000D9 RID: 217
		public HKR_COMM_OPTION_INFO_LIST option_info_list;

		// Token: 0x040000DA RID: 218
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public int[] reserved;
	}
}
