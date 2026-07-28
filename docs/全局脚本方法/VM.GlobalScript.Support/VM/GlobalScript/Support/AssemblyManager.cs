using System;
using System.Collections.Generic;
using System.Reflection;

namespace VM.GlobalScript.Support
{
	// Token: 0x0200002A RID: 42
	public class AssemblyManager
	{
		// Token: 0x0600011E RID: 286 RVA: 0x00008B2C File Offset: 0x00006D2C
		public static Assembly GetExitAssembly(string sourceCode)
		{
			int hashCode = sourceCode.GetHashCode();
			bool flag = AssemblyManager.dictCompileAssembly.ContainsKey(hashCode);
			Assembly result;
			if (flag)
			{
				result = AssemblyManager.dictCompileAssembly[hashCode];
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x0600011F RID: 287 RVA: 0x00008B64 File Offset: 0x00006D64
		public static void AddAssembly(string sourceCode, Assembly assembly)
		{
			int hashCode = sourceCode.GetHashCode();
			bool flag = !AssemblyManager.dictCompileAssembly.ContainsKey(hashCode);
			if (flag)
			{
				AssemblyManager.dictCompileAssembly.Add(hashCode, assembly);
			}
		}

		// Token: 0x06000120 RID: 288 RVA: 0x00008B9A File Offset: 0x00006D9A
		public static void ClearAeembly()
		{
			AssemblyManager.dictCompileAssembly.Clear();
		}

		// Token: 0x04000119 RID: 281
		private const int MaxCacheNum = 5;

		// Token: 0x0400011A RID: 282
		private static Dictionary<int, Assembly> dictCompileAssembly = new Dictionary<int, Assembly>();
	}
}
