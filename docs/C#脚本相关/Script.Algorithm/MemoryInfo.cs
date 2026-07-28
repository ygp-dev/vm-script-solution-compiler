using System;

namespace Script.Algorithm
{
	// Token: 0x0200001E RID: 30
	public class MemoryInfo
	{
		// Token: 0x040000DE RID: 222
		public int index = 0;

		// Token: 0x040000DF RID: 223
		public int dataLen = 0;

		// Token: 0x040000E0 RID: 224
		public string memoryFileName = "";

		// Token: 0x040000E1 RID: 225
		public IntPtr hShareMemoryHandle = IntPtr.Zero;

		// Token: 0x040000E2 RID: 226
		public IntPtr hBufferView = IntPtr.Zero;
	}
}
