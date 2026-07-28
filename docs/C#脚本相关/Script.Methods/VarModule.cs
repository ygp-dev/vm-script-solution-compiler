using System;

namespace Script.Methods
{
	// Token: 0x0200000A RID: 10
	public class VarModule : VarBaseModule
	{
		// Token: 0x060000A9 RID: 169 RVA: 0x00006C5A File Offset: 0x00004E5A
		public VarModule(int varModuleid, int nOwnerModuleID) : base(varModuleid, nOwnerModuleID)
		{
		}

		// Token: 0x060000AA RID: 170 RVA: 0x00006C64 File Offset: 0x00004E64
		public override object GetValue(string param)
		{
			string result = "";
			int varValueString = base.GetVarValueString(param, ref result);
			if (varValueString != 0)
			{
				return null;
			}
			return result;
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00006C87 File Offset: 0x00004E87
		public override int SetValue(string param, string paramValue)
		{
			return base.SetVarValueString(param, paramValue);
		}
	}
}
