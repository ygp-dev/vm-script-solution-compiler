using System;

namespace Script.Methods
{
	// Token: 0x02000022 RID: 34
	public class LineConfig : ShapeConfig
	{
		// Token: 0x1700003F RID: 63
		// (get) Token: 0x06000136 RID: 310 RVA: 0x000073F6 File Offset: 0x000055F6
		// (set) Token: 0x06000137 RID: 311 RVA: 0x000073FE File Offset: 0x000055FE
		public bool IsThroughLine
		{
			get
			{
				return this._isThroughLine;
			}
			set
			{
				if (value != this._isThroughLine)
				{
					this._isThroughLine = value;
				}
			}
		}

		// Token: 0x040000FD RID: 253
		private bool _isThroughLine;
	}
}
