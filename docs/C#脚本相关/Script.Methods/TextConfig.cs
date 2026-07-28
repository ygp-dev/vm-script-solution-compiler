using System;

namespace Script.Methods
{
	// Token: 0x02000024 RID: 36
	public class TextConfig : ShapeConfig
	{
		// Token: 0x17000042 RID: 66
		// (get) Token: 0x0600013E RID: 318 RVA: 0x00007464 File Offset: 0x00005664
		// (set) Token: 0x0600013F RID: 319 RVA: 0x0000746C File Offset: 0x0000566C
		public float PositionX
		{
			get
			{
				return this._positionX;
			}
			set
			{
				if (value != this._positionX)
				{
					this._positionX = value;
				}
			}
		}

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000140 RID: 320 RVA: 0x0000747E File Offset: 0x0000567E
		// (set) Token: 0x06000141 RID: 321 RVA: 0x00007486 File Offset: 0x00005686
		public float PositionY
		{
			get
			{
				return this._positionY;
			}
			set
			{
				if (value != this._positionY)
				{
					this._positionY = value;
				}
			}
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x06000142 RID: 322 RVA: 0x00007498 File Offset: 0x00005698
		// (set) Token: 0x06000143 RID: 323 RVA: 0x000074A0 File Offset: 0x000056A0
		public int FontSize
		{
			get
			{
				return this._fontSize;
			}
			set
			{
				if (value != this._fontSize)
				{
					this._fontSize = value;
				}
			}
		}

		// Token: 0x04000100 RID: 256
		private float _positionX;

		// Token: 0x04000101 RID: 257
		private float _positionY;

		// Token: 0x04000102 RID: 258
		private int _fontSize = 10;
	}
}
