using System;

namespace Script.Algorithm
{
	// Token: 0x0200000F RID: 15
	public interface ICommunicate
	{
		// Token: 0x060000D7 RID: 215
		bool InitCommuncate();

		// Token: 0x060000D8 RID: 216
		bool SendData(string msg);
	}
}
