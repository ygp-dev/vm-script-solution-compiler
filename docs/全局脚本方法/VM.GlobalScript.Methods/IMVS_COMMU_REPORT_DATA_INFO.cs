using System;
using System.Runtime.InteropServices;

namespace VM.GlobalScript.Methods
{
	// Token: 0x02000005 RID: 5
	public struct IMVS_COMMU_REPORT_DATA_INFO
	{
		// Token: 0x04000015 RID: 21
		public int nType;

		// Token: 0x04000016 RID: 22
		public IntPtr pData;

		// Token: 0x04000017 RID: 23
		public int nLen;

		// Token: 0x04000018 RID: 24
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)]
		public uint[] nReserved;
	}
}
