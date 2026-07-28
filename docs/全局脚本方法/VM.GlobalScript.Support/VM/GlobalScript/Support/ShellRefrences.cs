using System;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000013 RID: 19
	[Serializable]
	public class ShellRefrences
	{
		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000063 RID: 99 RVA: 0x000032D9 File Offset: 0x000014D9
		// (set) Token: 0x06000064 RID: 100 RVA: 0x000032E1 File Offset: 0x000014E1
		public string Name { get; set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000065 RID: 101 RVA: 0x000032EA File Offset: 0x000014EA
		// (set) Token: 0x06000066 RID: 102 RVA: 0x000032F2 File Offset: 0x000014F2
		public int refrencesType { get; set; }
	}
}
