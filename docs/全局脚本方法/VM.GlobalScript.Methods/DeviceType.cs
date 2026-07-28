using System;

namespace VM.GlobalScript.Methods
{
	// Token: 0x02000011 RID: 17
	public enum DeviceType
	{
		// Token: 0x0400004F RID: 79
		TCPClient = 1,
		// Token: 0x04000050 RID: 80
		TCPServer,
		// Token: 0x04000051 RID: 81
		UDP,
		// Token: 0x04000052 RID: 82
		Serial,
		// Token: 0x04000053 RID: 83
		PLC,
		// Token: 0x04000054 RID: 84
		Modbus
	}
}
