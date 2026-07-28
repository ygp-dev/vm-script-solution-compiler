using System;

namespace Script.Support
{
	// Token: 0x02000008 RID: 8
	internal interface IRemoteMethod
	{
		// Token: 0x06000041 RID: 65
		object Invoke(string lcMethod, object[] Parameters);
	}
}
