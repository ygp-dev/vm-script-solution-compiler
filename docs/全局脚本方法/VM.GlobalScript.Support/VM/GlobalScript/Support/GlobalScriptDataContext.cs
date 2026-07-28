using System;
using System.Collections.Generic;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000028 RID: 40
	public class GlobalScriptDataContext
	{
		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060000FD RID: 253 RVA: 0x00007B7C File Offset: 0x00005D7C
		// (set) Token: 0x060000FE RID: 254 RVA: 0x00007B94 File Offset: 0x00005D94
		public string GlobalScriptContent
		{
			get
			{
				return this._globalScriptContent;
			}
			set
			{
				object @lock = this._lock;
				lock (@lock)
				{
					this._globalScriptContent = value;
				}
			}
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060000FF RID: 255 RVA: 0x00007BDC File Offset: 0x00005DDC
		// (set) Token: 0x06000100 RID: 256 RVA: 0x00007BF4 File Offset: 0x00005DF4
		public bool IsEnablePassword
		{
			get
			{
				return this._isEnablePassword;
			}
			set
			{
				this._isEnablePassword = value;
			}
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x06000101 RID: 257 RVA: 0x00007C00 File Offset: 0x00005E00
		// (set) Token: 0x06000102 RID: 258 RVA: 0x00007C18 File Offset: 0x00005E18
		public bool IsComplieOK
		{
			get
			{
				return this._isComplieOK;
			}
			set
			{
				this._isComplieOK = value;
			}
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x06000103 RID: 259 RVA: 0x00007C24 File Offset: 0x00005E24
		// (set) Token: 0x06000104 RID: 260 RVA: 0x00007C3C File Offset: 0x00005E3C
		public bool IsComplieFinish
		{
			get
			{
				return this._isComplieFinish;
			}
			set
			{
				this._isComplieFinish = value;
			}
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x06000105 RID: 261 RVA: 0x00007C48 File Offset: 0x00005E48
		// (set) Token: 0x06000106 RID: 262 RVA: 0x00007C60 File Offset: 0x00005E60
		public string GlobalScriptPassword
		{
			get
			{
				return this._globalScriptPassword;
			}
			set
			{
				this._globalScriptPassword = value;
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000107 RID: 263 RVA: 0x00007C6C File Offset: 0x00005E6C
		// (set) Token: 0x06000108 RID: 264 RVA: 0x00007C84 File Offset: 0x00005E84
		public string GlobalScriptComplieResult
		{
			get
			{
				return this._globalScriptComplieResult;
			}
			set
			{
				this._globalScriptComplieResult = value;
			}
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x06000109 RID: 265 RVA: 0x00007C90 File Offset: 0x00005E90
		// (set) Token: 0x0600010A RID: 266 RVA: 0x00007CA8 File Offset: 0x00005EA8
		public string GlobalScriptDefault
		{
			get
			{
				return this._globalScriptDefault;
			}
			set
			{
				this._globalScriptDefault = value;
			}
		}

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x0600010B RID: 267 RVA: 0x00007CB4 File Offset: 0x00005EB4
		// (set) Token: 0x0600010C RID: 268 RVA: 0x00007CCC File Offset: 0x00005ECC
		public List<ShellRefrences> GlobalScriptRefences
		{
			get
			{
				return this._globalScriptRefences;
			}
			set
			{
				bool flag = this._globalScriptRefences == null;
				if (flag)
				{
					this._globalScriptRefences = new List<ShellRefrences>();
				}
				bool flag2 = value != null;
				if (flag2)
				{
					this._globalScriptRefences.Clear();
					this._globalScriptRefences.AddRange(value);
				}
			}
		}

		// Token: 0x04000108 RID: 264
		private string _globalScriptContent;

		// Token: 0x04000109 RID: 265
		private bool _isEnablePassword = false;

		// Token: 0x0400010A RID: 266
		private string _globalScriptPassword = string.Empty;

		// Token: 0x0400010B RID: 267
		private string _globalScriptComplieResult;

		// Token: 0x0400010C RID: 268
		private string _globalScriptDefault;

		// Token: 0x0400010D RID: 269
		private bool _isComplieOK = false;

		// Token: 0x0400010E RID: 270
		private bool _isComplieFinish = false;

		// Token: 0x0400010F RID: 271
		private object _lock = new object();

		// Token: 0x04000110 RID: 272
		private List<ShellRefrences> _globalScriptRefences = new List<ShellRefrences>();
	}
}
