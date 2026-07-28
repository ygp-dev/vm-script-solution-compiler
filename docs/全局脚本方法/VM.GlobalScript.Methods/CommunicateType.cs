using System;

namespace VM.GlobalScript.Methods
{
	// Token: 0x02000010 RID: 16
	public enum CommunicateType
	{
		// Token: 0x04000047 RID: 71
		TCPClient = 1,
		// Token: 0x04000048 RID: 72
		TCPServer,
		// Token: 0x04000049 RID: 73
		UDP,
		// Token: 0x0400004A RID: 74
		COM,
		// Token: 0x0400004B RID: 75
		PLC,
		// Token: 0x0400004C RID: 76
		MODBUS,
		// Token: 0x0400004D RID: 77
		IO
	}
}
