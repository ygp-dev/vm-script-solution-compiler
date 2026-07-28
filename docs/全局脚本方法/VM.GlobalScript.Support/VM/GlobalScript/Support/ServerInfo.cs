using System;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000011 RID: 17
	public class ServerInfo
	{
		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000049 RID: 73 RVA: 0x000031E8 File Offset: 0x000013E8
		// (set) Token: 0x0600004A RID: 74 RVA: 0x000031F0 File Offset: 0x000013F0
		public string ServerPairAddr { get; set; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600004B RID: 75 RVA: 0x000031F9 File Offset: 0x000013F9
		// (set) Token: 0x0600004C RID: 76 RVA: 0x00003201 File Offset: 0x00001401
		public string ServerRepAddr { get; set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x0600004D RID: 77 RVA: 0x0000320A File Offset: 0x0000140A
		// (set) Token: 0x0600004E RID: 78 RVA: 0x00003212 File Offset: 0x00001412
		public string ClientCommAddr { get; set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600004F RID: 79 RVA: 0x0000321B File Offset: 0x0000141B
		// (set) Token: 0x06000050 RID: 80 RVA: 0x00003223 File Offset: 0x00001423
		public string ServerName { get; set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000051 RID: 81 RVA: 0x0000322C File Offset: 0x0000142C
		// (set) Token: 0x06000052 RID: 82 RVA: 0x00003234 File Offset: 0x00001434
		public int ServerPID { get; set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000053 RID: 83 RVA: 0x0000323D File Offset: 0x0000143D
		// (set) Token: 0x06000054 RID: 84 RVA: 0x00003245 File Offset: 0x00001445
		public string ReportPairAddr { get; set; }

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000055 RID: 85 RVA: 0x0000324E File Offset: 0x0000144E
		// (set) Token: 0x06000056 RID: 86 RVA: 0x00003256 File Offset: 0x00001456
		public bool IsCrash { get; set; }

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000057 RID: 87 RVA: 0x0000325F File Offset: 0x0000145F
		// (set) Token: 0x06000058 RID: 88 RVA: 0x00003267 File Offset: 0x00001467
		public bool IsSmGlobalProfix { get; set; }
	}
}
