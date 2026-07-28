using System;

namespace Script.Algorithm
{
	// Token: 0x02000021 RID: 33
	public class RequestHead
	{
		// Token: 0x17000051 RID: 81
		// (get) Token: 0x060001A2 RID: 418 RVA: 0x0000B538 File Offset: 0x00009738
		// (set) Token: 0x060001A3 RID: 419 RVA: 0x0000B54F File Offset: 0x0000974F
		public int command { get; set; }

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x060001A4 RID: 420 RVA: 0x0000B558 File Offset: 0x00009758
		// (set) Token: 0x060001A5 RID: 421 RVA: 0x0000B56F File Offset: 0x0000976F
		public string type { get; set; }

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x060001A6 RID: 422 RVA: 0x0000B578 File Offset: 0x00009778
		// (set) Token: 0x060001A7 RID: 423 RVA: 0x0000B58F File Offset: 0x0000978F
		public int seqId { get; set; }
	}
}
