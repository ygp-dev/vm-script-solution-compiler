using System;
using System.Text;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000018 RID: 24
	public class ZmqDataContext
	{
		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000071 RID: 113 RVA: 0x00004543 File Offset: 0x00002743
		// (set) Token: 0x06000072 RID: 114 RVA: 0x0000454B File Offset: 0x0000274B
		public string ConnectionString { get; set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000073 RID: 115 RVA: 0x00004554 File Offset: 0x00002754
		// (set) Token: 0x06000074 RID: 116 RVA: 0x0000455C File Offset: 0x0000275C
		public int RcvTimout { get; set; }

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000075 RID: 117 RVA: 0x00004565 File Offset: 0x00002765
		// (set) Token: 0x06000076 RID: 118 RVA: 0x0000456D File Offset: 0x0000276D
		public int WriteTimeOut { get; set; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000077 RID: 119 RVA: 0x00004576 File Offset: 0x00002776
		// (set) Token: 0x06000078 RID: 120 RVA: 0x0000457E File Offset: 0x0000277E
		public int ZmqType { get; set; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000079 RID: 121 RVA: 0x00004587 File Offset: 0x00002787
		// (set) Token: 0x0600007A RID: 122 RVA: 0x0000458F File Offset: 0x0000278F
		public bool ServerOrClient { get; set; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x0600007B RID: 123 RVA: 0x00004598 File Offset: 0x00002798
		// (set) Token: 0x0600007C RID: 124 RVA: 0x000045A0 File Offset: 0x000027A0
		public Encoding Encod { get; set; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x0600007D RID: 125 RVA: 0x000045A9 File Offset: 0x000027A9
		// (set) Token: 0x0600007E RID: 126 RVA: 0x000045B1 File Offset: 0x000027B1
		public bool StartReceiveTask { get; set; }
	}
}
