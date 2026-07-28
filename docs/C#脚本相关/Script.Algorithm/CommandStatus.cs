using System;

namespace Script.Algorithm
{
	// Token: 0x0200002B RID: 43
	public enum CommandStatus
	{
		// Token: 0x04000105 RID: 261
		HeartBeat = 4001,
		// Token: 0x04000106 RID: 262
		SetShellContent,
		// Token: 0x04000107 RID: 263
		GetParams,
		// Token: 0x04000108 RID: 264
		GetSubResult,
		// Token: 0x04000109 RID: 265
		Process,
		// Token: 0x0400010A RID: 266
		SetModuleOutput,
		// Token: 0x0400010B RID: 267
		CloseModule,
		// Token: 0x0400010C RID: 268
		SendAgain,
		// Token: 0x0400010D RID: 269
		SetRefrences,
		// Token: 0x0400010E RID: 270
		UpdateCode,
		// Token: 0x0400010F RID: 271
		ExportSln
	}
}
