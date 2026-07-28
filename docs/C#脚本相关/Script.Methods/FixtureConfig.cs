using System;

namespace Script.Methods
{
	// Token: 0x02000023 RID: 35
	public class FixtureConfig : ShapeConfig
	{
		// Token: 0x17000040 RID: 64
		// (get) Token: 0x06000139 RID: 313 RVA: 0x00007418 File Offset: 0x00005618
		// (set) Token: 0x0600013A RID: 314 RVA: 0x00007420 File Offset: 0x00005620
		public ShapeColor InitColor
		{
			get
			{
				return this._initColor;
			}
			set
			{
				if (value != this._initColor)
				{
					this._initColor = value;
				}
			}
		}

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x0600013B RID: 315 RVA: 0x00007432 File Offset: 0x00005632
		// (set) Token: 0x0600013C RID: 316 RVA: 0x0000743A File Offset: 0x0000563A
		public ShapeColor RunColor
		{
			get
			{
				return this._runColor;
			}
			set
			{
				if (value != this._runColor)
				{
					this._runColor = value;
				}
			}
		}

		// Token: 0x040000FE RID: 254
		private ShapeColor _initColor = ShapeColor.LightGreen;

		// Token: 0x040000FF RID: 255
		private ShapeColor _runColor = ShapeColor.Red;
	}
}
