using System;

namespace Script.Algorithm
{
	// Token: 0x02000020 RID: 32
	public class GetModuleResultInfo
	{
		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000195 RID: 405 RVA: 0x0000B470 File Offset: 0x00009670
		// (set) Token: 0x06000196 RID: 406 RVA: 0x0000B487 File Offset: 0x00009687
		public string id { get; set; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000197 RID: 407 RVA: 0x0000B490 File Offset: 0x00009690
		// (set) Token: 0x06000198 RID: 408 RVA: 0x0000B4A7 File Offset: 0x000096A7
		public string key { get; set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000199 RID: 409 RVA: 0x0000B4B0 File Offset: 0x000096B0
		// (set) Token: 0x0600019A RID: 410 RVA: 0x0000B4C7 File Offset: 0x000096C7
		public int type { get; set; }

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x0600019B RID: 411 RVA: 0x0000B4D0 File Offset: 0x000096D0
		// (set) Token: 0x0600019C RID: 412 RVA: 0x0000B4E7 File Offset: 0x000096E7
		public int count { get; set; }

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600019D RID: 413 RVA: 0x0000B4F0 File Offset: 0x000096F0
		// (set) Token: 0x0600019E RID: 414 RVA: 0x0000B507 File Offset: 0x00009707
		public int ret { get; set; }

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x0600019F RID: 415 RVA: 0x0000B510 File Offset: 0x00009710
		// (set) Token: 0x060001A0 RID: 416 RVA: 0x0000B527 File Offset: 0x00009727
		public string[] value { get; set; }
	}
}
