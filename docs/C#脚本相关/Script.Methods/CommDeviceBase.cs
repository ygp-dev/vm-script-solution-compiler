using System;
using System.Collections.Generic;

namespace Script.Methods
{
	// Token: 0x0200000D RID: 13
	public class CommDeviceBase : ModuleBase
	{
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x060000B0 RID: 176 RVA: 0x00006D0E File Offset: 0x00004F0E
		// (set) Token: 0x060000B1 RID: 177 RVA: 0x00006D16 File Offset: 0x00004F16
		public int DeviceID { get; set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x060000B2 RID: 178 RVA: 0x00006D1F File Offset: 0x00004F1F
		// (set) Token: 0x060000B3 RID: 179 RVA: 0x00006D27 File Offset: 0x00004F27
		public int AddressID { get; set; }

		// Token: 0x060000B4 RID: 180 RVA: 0x00006D30 File Offset: 0x00004F30
		public override int SetValue(string param, string paramValue)
		{
			return base.objAlgorithm.SetObjectValueForModule(base.ModuleID, param, paramValue, (int)this.datatype);
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00006D50 File Offset: 0x00004F50
		public virtual int SendData(string data, DataType dataType = DataType.StringType)
		{
			return 0;
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00006D53 File Offset: 0x00004F53
		public virtual int SendData(byte[] databytes, DataType dataType = DataType.ByteType)
		{
			return 0;
		}

		// Token: 0x0400001C RID: 28
		public int IsUtf8 = 1;

		// Token: 0x0400001D RID: 29
		public ValueType datatype;

		// Token: 0x0400001E RID: 30
		public Dictionary<int, string> DictDataType = new Dictionary<int, string>
		{
			{
				1,
				"TCPClient"
			},
			{
				2,
				"TCPServer"
			},
			{
				3,
				"UDP"
			},
			{
				4,
				"Serial"
			},
			{
				5,
				"IO"
			},
			{
				6,
				"PLC"
			},
			{
				7,
				"Modbus"
			}
		};
	}
}
