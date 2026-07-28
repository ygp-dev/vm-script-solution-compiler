using System;
using Script.Algorithm;

namespace Script.Methods
{
	// Token: 0x0200000F RID: 15
	public class DeviceAddress : CommDeviceBase
	{
		// Token: 0x060000BC RID: 188 RVA: 0x00006EB4 File Offset: 0x000050B4
		public override int SendData(string strSend, DataType dataType)
		{
			string param = string.Format("{0}-{1}#{2}", this.GetSendPrefixString(dataType), base.DeviceID, base.AddressID);
			return this.SetValue(param, strSend);
		}

		// Token: 0x060000BD RID: 189 RVA: 0x00006EF4 File Offset: 0x000050F4
		public override int SendData(byte[] databytes, DataType dataType = DataType.ByteType)
		{
			string param = string.Format("{0}-{1}#{2}", this.GetSendPrefixString(dataType), base.DeviceID, base.AddressID);
			string paramValue = Convert.ToBase64String(databytes);
			return this.SetValue(param, paramValue);
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00006F38 File Offset: 0x00005138
		private string GetSendPrefixString(DataType dataType)
		{
			string text = "Write";
			switch (dataType)
			{
			case DataType.StringType:
				text += "String";
				this.datatype = EnumValueType.TypeString;
				break;
			case DataType.IntType:
				text += "Int";
				this.datatype = EnumValueType.TypeInt;
				break;
			case DataType.FloatType:
				text += "Float";
				this.datatype = EnumValueType.TypeFloat;
				break;
			case DataType.ByteType:
				text += "Bytes";
				this.datatype = EnumValueType.TypeByte;
				break;
			}
			return text;
		}
	}
}
