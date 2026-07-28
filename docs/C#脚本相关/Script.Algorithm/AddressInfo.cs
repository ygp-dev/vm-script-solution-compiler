using System;

namespace Script.Algorithm
{
	// Token: 0x0200002A RID: 42
	public class AddressInfo
	{
		// Token: 0x1700005C RID: 92
		// (get) Token: 0x060001C9 RID: 457 RVA: 0x0000B6C0 File Offset: 0x000098C0
		// (set) Token: 0x060001CA RID: 458 RVA: 0x0000B6D7 File Offset: 0x000098D7
		public int nModuleID { get; set; }

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x060001CB RID: 459 RVA: 0x0000B6E0 File Offset: 0x000098E0
		// (set) Token: 0x060001CC RID: 460 RVA: 0x0000B6F7 File Offset: 0x000098F7
		public string strHeartPairAddress { get; set; }

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x060001CD RID: 461 RVA: 0x0000B700 File Offset: 0x00009900
		// (set) Token: 0x060001CE RID: 462 RVA: 0x0000B717 File Offset: 0x00009917
		public string strSetParamRepAddress { get; set; }

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x060001CF RID: 463 RVA: 0x0000B720 File Offset: 0x00009920
		// (set) Token: 0x060001D0 RID: 464 RVA: 0x0000B737 File Offset: 0x00009937
		public string strProcessRepAddress { get; set; }

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x060001D1 RID: 465 RVA: 0x0000B740 File Offset: 0x00009940
		// (set) Token: 0x060001D2 RID: 466 RVA: 0x0000B757 File Offset: 0x00009957
		public string strGetParamReqAddress { get; set; }

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x060001D3 RID: 467 RVA: 0x0000B760 File Offset: 0x00009960
		// (set) Token: 0x060001D4 RID: 468 RVA: 0x0000B777 File Offset: 0x00009977
		public int nSmGlobalProfix { get; set; }

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x060001D5 RID: 469 RVA: 0x0000B780 File Offset: 0x00009980
		// (set) Token: 0x060001D6 RID: 470 RVA: 0x0000B797 File Offset: 0x00009997
		public int nProxyID { get; set; }
	}
}
