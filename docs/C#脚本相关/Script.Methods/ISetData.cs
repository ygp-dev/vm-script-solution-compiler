using System;
using Script.Algorithm;

namespace Script.Methods
{
	// Token: 0x02000005 RID: 5
	public interface ISetData
	{
		// Token: 0x0600001B RID: 27
		int SetHandle(long input, long output);

		// Token: 0x0600001C RID: 28
		void SetAlgorithm(IAlgorithm algorithm);

		// Token: 0x0600001D RID: 29
		void SetAlgorithmData(string key, object objData);

		// Token: 0x0600001E RID: 30
		void Clear();
	}
}
