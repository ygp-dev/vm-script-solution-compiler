using System;

namespace Script.Methods
{
	// Token: 0x02000015 RID: 21
	public class ImageData
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x060000C0 RID: 192 RVA: 0x00006FD6 File Offset: 0x000051D6
		// (set) Token: 0x060000C1 RID: 193 RVA: 0x00006FDE File Offset: 0x000051DE
		public byte[] Buffer { get; set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x060000C2 RID: 194 RVA: 0x00006FE7 File Offset: 0x000051E7
		// (set) Token: 0x060000C3 RID: 195 RVA: 0x00006FEF File Offset: 0x000051EF
		public int Width { get; set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x060000C4 RID: 196 RVA: 0x00006FF8 File Offset: 0x000051F8
		// (set) Token: 0x060000C5 RID: 197 RVA: 0x00007000 File Offset: 0x00005200
		public int Height { get; set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x060000C6 RID: 198 RVA: 0x00007009 File Offset: 0x00005209
		// (set) Token: 0x060000C7 RID: 199 RVA: 0x00007011 File Offset: 0x00005211
		public ImagePixelFormate PixelFormat { get; set; }
	}
}
