using System;

namespace Script.Methods
{
	// Token: 0x02000018 RID: 24
	public class PolygonData
	{
		// Token: 0x17000019 RID: 25
		// (get) Token: 0x060000E1 RID: 225 RVA: 0x000070ED File Offset: 0x000052ED
		// (set) Token: 0x060000E2 RID: 226 RVA: 0x000070F5 File Offset: 0x000052F5
		public int PointNum { get; set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x060000E3 RID: 227 RVA: 0x000070FE File Offset: 0x000052FE
		// (set) Token: 0x060000E4 RID: 228 RVA: 0x00007106 File Offset: 0x00005306
		public float[] PointXArray { get; set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x060000E5 RID: 229 RVA: 0x0000710F File Offset: 0x0000530F
		// (set) Token: 0x060000E6 RID: 230 RVA: 0x00007117 File Offset: 0x00005317
		public float[] PointYArray { get; set; }
	}
}
