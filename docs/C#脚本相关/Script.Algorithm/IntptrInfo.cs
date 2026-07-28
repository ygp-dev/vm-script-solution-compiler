using System;

namespace Script.Algorithm
{
	// Token: 0x02000030 RID: 48
	public class IntptrInfo
	{
		// Token: 0x17000063 RID: 99
		// (get) Token: 0x060001DC RID: 476 RVA: 0x0000C884 File Offset: 0x0000AA84
		// (set) Token: 0x060001DD RID: 477 RVA: 0x0000C89B File Offset: 0x0000AA9B
		public IntPtr dataInptr { get; set; }

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x060001DE RID: 478 RVA: 0x0000C8A4 File Offset: 0x0000AAA4
		// (set) Token: 0x060001DF RID: 479 RVA: 0x0000C8BB File Offset: 0x0000AABB
		public int nSize { get; set; }

		// Token: 0x060001E0 RID: 480 RVA: 0x0000C8C4 File Offset: 0x0000AAC4
		public IntptrInfo()
		{
			this.dataInptr = IntPtr.Zero;
			this.nSize = 0;
		}
	}
}
