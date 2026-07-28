using System;

namespace Script.Support
{
	// Token: 0x02000007 RID: 7
	public class HikCompileMessage : MarshalByRefObject
	{
		// Token: 0x0600003B RID: 59 RVA: 0x0000365D File Offset: 0x0000185D
		public HikCompileMessage(string text, int line, int column, bool warning)
		{
			this._text = text;
			this._line = line;
			this._column = column;
			this._isWarning = warning;
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600003C RID: 60 RVA: 0x00003682 File Offset: 0x00001882
		public bool IsWarning
		{
			get
			{
				return this._isWarning;
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x0600003D RID: 61 RVA: 0x0000368A File Offset: 0x0000188A
		public bool IsError
		{
			get
			{
				return !this._isWarning;
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600003E RID: 62 RVA: 0x00003695 File Offset: 0x00001895
		public string Text
		{
			get
			{
				return this._text;
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600003F RID: 63 RVA: 0x0000369D File Offset: 0x0000189D
		public int Line
		{
			get
			{
				return this._line;
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000040 RID: 64 RVA: 0x000036A5 File Offset: 0x000018A5
		public int Column
		{
			get
			{
				return this._column;
			}
		}

		// Token: 0x0400001C RID: 28
		private bool _isWarning;

		// Token: 0x0400001D RID: 29
		private string _text;

		// Token: 0x0400001E RID: 30
		private int _line;

		// Token: 0x0400001F RID: 31
		private int _column;
	}
}
