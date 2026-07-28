using System;
using System.Collections.Generic;
using Script.Algorithm;

namespace Script.Methods
{
	// Token: 0x02000008 RID: 8
	public class ModuleBase
	{
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000079 RID: 121 RVA: 0x0000646E File Offset: 0x0000466E
		// (set) Token: 0x0600007A RID: 122 RVA: 0x00006476 File Offset: 0x00004676
		public int ModuleID
		{
			get
			{
				return this.moduleID;
			}
			set
			{
				this.moduleID = value;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600007B RID: 123 RVA: 0x0000647F File Offset: 0x0000467F
		// (set) Token: 0x0600007C RID: 124 RVA: 0x00006487 File Offset: 0x00004687
		public string NodeName { get; set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600007D RID: 125 RVA: 0x00006490 File Offset: 0x00004690
		// (set) Token: 0x0600007E RID: 126 RVA: 0x00006498 File Offset: 0x00004698
		public ModuleBase ParentNode { get; set; }

		// Token: 0x0600007F RID: 127 RVA: 0x000064A1 File Offset: 0x000046A1
		public ModuleBase(ModuleBase parentNode)
		{
			this.ParentNode = parentNode;
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000080 RID: 128 RVA: 0x000064B7 File Offset: 0x000046B7
		// (set) Token: 0x06000081 RID: 129 RVA: 0x000064BF File Offset: 0x000046BF
		public IAlgorithm objAlgorithm { get; set; }

		// Token: 0x06000082 RID: 130 RVA: 0x000064C8 File Offset: 0x000046C8
		public ModuleBase()
		{
		}

		// Token: 0x06000083 RID: 131 RVA: 0x000064D8 File Offset: 0x000046D8
		public virtual Array GetArrayValue(string param)
		{
			string paramKey = this.NodeName + "." + param;
			Array array = null;
			int num = -1;
			if (this.objAlgorithm.GetObjectArrayValueForModule(this.ModuleID, 1, paramKey, ref num, ref array) != 0 || array == null || array.Length == 0)
			{
				return null;
			}
			return array;
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00006524 File Offset: 0x00004724
		public virtual object GetValue(string param)
		{
			string paramKey = this.NodeName + "." + param;
			Array array = null;
			int num = -1;
			if (this.objAlgorithm.GetObjectArrayValueForModule(this.ModuleID, 0, paramKey, ref num, ref array) != 0 || array == null || array.Length == 0)
			{
				return null;
			}
			return array.GetValue(0);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00006578 File Offset: 0x00004778
		public virtual int SetValue(string param, string paramValue)
		{
			string paramName = this.NodeName + "." + param;
			return this.objAlgorithm.SetObjectValueForModule(this.ModuleID, paramName, paramValue, 0);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x000065AC File Offset: 0x000047AC
		public virtual int GetParamValue(string param, ref string paramValue)
		{
			string paramName = this.NodeName + "." + param;
			return this.objAlgorithm.GetModuleParamValue(this.ModuleID, paramName, ref paramValue);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x000065E0 File Offset: 0x000047E0
		private List<string> getModuleParamString()
		{
			ModuleBase parentNode = this.ParentNode;
			List<string> list = new List<string>();
			while (parentNode != null)
			{
				if (!string.IsNullOrEmpty(parentNode.NodeName))
				{
					list.Add(parentNode.NodeName);
				}
				parentNode = parentNode.ParentNode;
			}
			list.Reverse();
			list.Add(this.NodeName);
			return list;
		}

		// Token: 0x04000017 RID: 23
		private int moduleID = -1;
	}
}
