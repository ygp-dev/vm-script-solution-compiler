using System;

namespace Script.Methods
{
	// Token: 0x0200000B RID: 11
	public class GlobalCommModule : ModuleBase
	{
		// Token: 0x060000AC RID: 172 RVA: 0x00006C94 File Offset: 0x00004E94
		public CommDevice GetDevice(int deviceID)
		{
			return new CommDevice
			{
				DeviceID = deviceID,
				ModuleID = base.ModuleID,
				ParentNode = null,
				objAlgorithm = base.objAlgorithm
			};
		}
	}
}
