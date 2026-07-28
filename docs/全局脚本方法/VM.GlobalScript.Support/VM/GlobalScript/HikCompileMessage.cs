using System;

namespace VM.GlobalScript
{
	// Token: 0x0200000E RID: 14
	public class HikCompileMessage : MarshalByRefObject
	{
		// Token: 0x06000043 RID: 67 RVA: 0x00003143 File Offset: 0x00001343
		public HikCompileMessage(string text, int line, int column, bool warning)
		{
			this._text = text;
			this._line = line;
			this._column = column;
			this._isWarning = warning;
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000044 RID: 68 RVA: 0x0000316C File Offset: 0x0000136C
		public bool IsWarning
		{
			get
			{
				return this._isWarning;
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000045 RID: 69 RVA: 0x00003184 File Offset: 0x00001384
		public bool IsError
		{
			get
			{
				return !this._isWarning;
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000046 RID: 70 RVA: 0x000031A0 File Offset: 0x000013A0
		public string Text
		{
			get
			{
				return this._text;
			}
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000047 RID: 71 RVA: 0x000031B8 File Offset: 0x000013B8
		public int Line
		{
			get
			{
				return this._line;
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000048 RID: 72 RVA: 0x000031D0 File Offset: 0x000013D0
		public int Column
		{
			get
			{
				return this._column;
			}
		}

		// Token: 0x04000021 RID: 33
		private bool _isWarning;

		// Token: 0x04000022 RID: 34
		private string _text;

		// Token: 0x04000023 RID: 35
		private int _line;

		// Token: 0x04000024 RID: 36
		private int _column;
	}
}
