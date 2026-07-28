using System;

namespace Script.Algorithm
{
	// Token: 0x02000028 RID: 40
	public class ParamEventArgs : EventArgs
	{
		// Token: 0x17000058 RID: 88
		// (get) Token: 0x060001BF RID: 447 RVA: 0x0000B630 File Offset: 0x00009830
		// (set) Token: 0x060001C0 RID: 448 RVA: 0x0000B647 File Offset: 0x00009847
		public int Status { get; set; }

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x060001C1 RID: 449 RVA: 0x0000B650 File Offset: 0x00009850
		// (set) Token: 0x060001C2 RID: 450 RVA: 0x0000B667 File Offset: 0x00009867
		public string ParamName { get; set; }

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x060001C3 RID: 451 RVA: 0x0000B670 File Offset: 0x00009870
		// (set) Token: 0x060001C4 RID: 452 RVA: 0x0000B687 File Offset: 0x00009887
		public string ParamValue { get; set; }
	}
}
