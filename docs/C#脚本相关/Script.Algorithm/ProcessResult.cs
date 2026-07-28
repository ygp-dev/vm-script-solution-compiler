using System;

namespace Script.Algorithm
{
	// Token: 0x02000023 RID: 35
	public class ProcessResult
	{
		// Token: 0x17000056 RID: 86
		// (get) Token: 0x060001AE RID: 430 RVA: 0x0000B5E8 File Offset: 0x000097E8
		// (set) Token: 0x060001AF RID: 431 RVA: 0x0000B5FF File Offset: 0x000097FF
		public RequestHead head { get; set; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x060001B0 RID: 432 RVA: 0x0000B608 File Offset: 0x00009808
		// (set) Token: 0x060001B1 RID: 433 RVA: 0x0000B61F File Offset: 0x0000981F
		public RequestBody body { get; set; }
	}
}
