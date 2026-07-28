using System;
using System.Text;

namespace Script.Algorithm
{
	// Token: 0x02000011 RID: 17
	public class ZmqDataContext
	{
		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060000DE RID: 222 RVA: 0x0000594C File Offset: 0x00003B4C
		// (set) Token: 0x060000DF RID: 223 RVA: 0x00005963 File Offset: 0x00003B63
		public string ConnectionString { get; set; }

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000E0 RID: 224 RVA: 0x0000596C File Offset: 0x00003B6C
		// (set) Token: 0x060000E1 RID: 225 RVA: 0x00005983 File Offset: 0x00003B83
		public int RcvTimout { get; set; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000E2 RID: 226 RVA: 0x0000598C File Offset: 0x00003B8C
		// (set) Token: 0x060000E3 RID: 227 RVA: 0x000059A3 File Offset: 0x00003BA3
		public int WriteTimeOut { get; set; }

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060000E4 RID: 228 RVA: 0x000059AC File Offset: 0x00003BAC
		// (set) Token: 0x060000E5 RID: 229 RVA: 0x000059C3 File Offset: 0x00003BC3
		public int ZmqType { get; set; }

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000E6 RID: 230 RVA: 0x000059CC File Offset: 0x00003BCC
		// (set) Token: 0x060000E7 RID: 231 RVA: 0x000059E3 File Offset: 0x00003BE3
		public bool ServerOrClient { get; set; }

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060000E8 RID: 232 RVA: 0x000059EC File Offset: 0x00003BEC
		// (set) Token: 0x060000E9 RID: 233 RVA: 0x00005A03 File Offset: 0x00003C03
		public Encoding Encod { get; set; }

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060000EA RID: 234 RVA: 0x00005A0C File Offset: 0x00003C0C
		// (set) Token: 0x060000EB RID: 235 RVA: 0x00005A23 File Offset: 0x00003C23
		public bool StartReceiveTask { get; set; }

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x060000EC RID: 236 RVA: 0x00005A2C File Offset: 0x00003C2C
		// (set) Token: 0x060000ED RID: 237 RVA: 0x00005A43 File Offset: 0x00003C43
		public int IntervalTime { get; set; }
	}
}
