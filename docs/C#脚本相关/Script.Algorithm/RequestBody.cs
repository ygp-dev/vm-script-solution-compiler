using System;

namespace Script.Algorithm
{
	// Token: 0x02000022 RID: 34
	public class RequestBody
	{
		// Token: 0x17000054 RID: 84
		// (get) Token: 0x060001A9 RID: 425 RVA: 0x0000B5A0 File Offset: 0x000097A0
		// (set) Token: 0x060001AA RID: 426 RVA: 0x0000B5B7 File Offset: 0x000097B7
		public string extrainfo { get; set; }

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x060001AB RID: 427 RVA: 0x0000B5C0 File Offset: 0x000097C0
		// (set) Token: 0x060001AC RID: 428 RVA: 0x0000B5D7 File Offset: 0x000097D7
		public GetModuleResultInfo[] resultinfo { get; set; }
	}
}
