using System;

namespace VM.GlobalScript.Methods
{
	// Token: 0x0200000F RID: 15
	public class ReceiveDataInfo
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000098 RID: 152 RVA: 0x0000466D File Offset: 0x0000286D
		// (set) Token: 0x06000099 RID: 153 RVA: 0x00004675 File Offset: 0x00002875
		public CommunicateType CommunicateType { get; set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600009A RID: 154 RVA: 0x0000467E File Offset: 0x0000287E
		// (set) Token: 0x0600009B RID: 155 RVA: 0x00004686 File Offset: 0x00002886
		public int DeviceID { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600009C RID: 156 RVA: 0x0000468F File Offset: 0x0000288F
		// (set) Token: 0x0600009D RID: 157 RVA: 0x00004697 File Offset: 0x00002897
		public int DeviceAddressID { get; set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600009E RID: 158 RVA: 0x000046A0 File Offset: 0x000028A0
		// (set) Token: 0x0600009F RID: 159 RVA: 0x000046A8 File Offset: 0x000028A8
		public byte[] DeviceData { get; set; }
	}
}
