using System;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000016 RID: 22
	public interface ICommunicate
	{
		// Token: 0x0600006B RID: 107
		bool InitCommuncate();

		// Token: 0x0600006C RID: 108
		bool SendData(string msg);
	}
}
