using System;
using Script.Algorithm;

namespace Script.Methods
{
	// Token: 0x0200000E RID: 14
	public class CommDevice : CommDeviceBase
	{
		// Token: 0x060000B8 RID: 184 RVA: 0x00006DD4 File Offset: 0x00004FD4
		public DeviceAddress GetAddress(int addressID)
		{
			return new DeviceAddress
			{
				AddressID = addressID,
				ModuleID = base.ModuleID,
				DeviceID = base.DeviceID,
				datatype = EnumValueType.TypeString,
				objAlgorithm = base.objAlgorithm
			};
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00006E20 File Offset: 0x00005020
		public override int SendData(string data, DataType dataType = DataType.StringType)
		{
			if (string.IsNullOrEmpty(data))
			{
				return -1;
			}
			string param = string.Format("WriteString-{0}", base.DeviceID);
			this.datatype = EnumValueType.TypeString;
			return this.SetValue(param, data);
		}

		// Token: 0x060000BA RID: 186 RVA: 0x00006E64 File Offset: 0x00005064
		public override int SendData(byte[] databytes, DataType dataType = DataType.ByteType)
		{
			if (databytes == null || databytes.Length == 0)
			{
				return -1;
			}
			string param = string.Format("WriteBytes-{0}", base.DeviceID);
			string paramValue = Convert.ToBase64String(databytes);
			this.datatype = EnumValueType.TypeByte;
			return this.SetValue(param, paramValue);
		}
	}
}
