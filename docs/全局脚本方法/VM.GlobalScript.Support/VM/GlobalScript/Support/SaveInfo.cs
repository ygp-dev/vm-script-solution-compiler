using System;
using System.Collections.Generic;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000012 RID: 18
	[Serializable]
	public class SaveInfo
	{
		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600005A RID: 90 RVA: 0x00003270 File Offset: 0x00001470
		// (set) Token: 0x0600005B RID: 91 RVA: 0x00003288 File Offset: 0x00001488
		public string Version
		{
			get
			{
				return this._version;
			}
			set
			{
				this._version = value;
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x0600005C RID: 92 RVA: 0x00003292 File Offset: 0x00001492
		// (set) Token: 0x0600005D RID: 93 RVA: 0x0000329A File Offset: 0x0000149A
		public string ScriptPassword { get; set; }

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600005E RID: 94 RVA: 0x000032A3 File Offset: 0x000014A3
		// (set) Token: 0x0600005F RID: 95 RVA: 0x000032AB File Offset: 0x000014AB
		public string ScriptContent { get; set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000060 RID: 96 RVA: 0x000032B4 File Offset: 0x000014B4
		// (set) Token: 0x06000061 RID: 97 RVA: 0x000032BC File Offset: 0x000014BC
		public List<ShellRefrences> ScriptRefences { get; set; }

		// Token: 0x04000044 RID: 68
		private string _version = "V3.4.0";
	}
}
