using System;

namespace Script.Methods
{
	// Token: 0x0200000C RID: 12
	public class ModuleGroup : ModuleBase
	{
		// Token: 0x060000AE RID: 174 RVA: 0x00006CD8 File Offset: 0x00004ED8
		public ModuleBase GetModule(string name)
		{
			return new ModuleGroup
			{
				NodeName = name,
				ParentNode = this,
				objAlgorithm = base.objAlgorithm
			};
		}
	}
}
