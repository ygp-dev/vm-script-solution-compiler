using System;
using System.Runtime.InteropServices;

namespace Script.Algorithm
{
	// Token: 0x0200003F RID: 63
	public struct SHARED_MEM_HEADER
	{
		// Token: 0x040001BD RID: 445
		public ulong nSize;

		// Token: 0x040001BE RID: 446
		public byte nLightCopy;

		// Token: 0x040001BF RID: 447
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
		public byte[] byReserve1;

		// Token: 0x040001C0 RID: 448
		public ushort nHeaderLen;

		// Token: 0x040001C1 RID: 449
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
		public byte[] szSharedMemName;

		// Token: 0x040001C2 RID: 450
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
		public byte[] byReserve2;
	}
}
