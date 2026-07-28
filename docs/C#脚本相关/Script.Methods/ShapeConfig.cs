using System;

namespace Script.Methods
{
	// Token: 0x02000021 RID: 33
	public class ShapeConfig
	{
		// Token: 0x1700003B RID: 59
		// (get) Token: 0x0600012D RID: 301 RVA: 0x0000736F File Offset: 0x0000556F
		// (set) Token: 0x0600012E RID: 302 RVA: 0x00007377 File Offset: 0x00005577
		public ShapeColor Color
		{
			get
			{
				return this._color;
			}
			set
			{
				if (value != this._color)
				{
					this._color = value;
				}
			}
		}

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x0600012F RID: 303 RVA: 0x00007389 File Offset: 0x00005589
		// (set) Token: 0x06000130 RID: 304 RVA: 0x00007391 File Offset: 0x00005591
		public ShapeColor FillColor
		{
			get
			{
				return this._fillColor;
			}
			set
			{
				if (value != this._fillColor)
				{
					this._fillColor = value;
				}
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x06000131 RID: 305 RVA: 0x000073A3 File Offset: 0x000055A3
		// (set) Token: 0x06000132 RID: 306 RVA: 0x000073AB File Offset: 0x000055AB
		public double Thickness
		{
			get
			{
				return this._thickness;
			}
			set
			{
				if (value != this._thickness)
				{
					this._thickness = value;
				}
			}
		}

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x06000133 RID: 307 RVA: 0x000073BD File Offset: 0x000055BD
		// (set) Token: 0x06000134 RID: 308 RVA: 0x000073C5 File Offset: 0x000055C5
		public int Opacity
		{
			get
			{
				return this._opacity;
			}
			set
			{
				if (value != this._opacity)
				{
					this._opacity = value;
				}
			}
		}

		// Token: 0x040000F9 RID: 249
		private ShapeColor _color;

		// Token: 0x040000FA RID: 250
		private ShapeColor _fillColor;

		// Token: 0x040000FB RID: 251
		private double _thickness = 2.0;

		// Token: 0x040000FC RID: 252
		private int _opacity = 100;
	}
}
