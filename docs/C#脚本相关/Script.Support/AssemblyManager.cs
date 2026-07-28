using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Script.Support
{
	// Token: 0x0200000A RID: 10
	public class AssemblyManager
	{
		// Token: 0x06000050 RID: 80 RVA: 0x00003E28 File Offset: 0x00002028
		public static Assembly GetExitAssembly(string sourceCode)
		{
			int hashCode = sourceCode.GetHashCode();
			if (AssemblyManager.dictCompileAssembly.ContainsKey(hashCode))
			{
				return AssemblyManager.dictCompileAssembly[hashCode];
			}
			return null;
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00003E58 File Offset: 0x00002058
		public static void AddAssembly(string sourceCode, Assembly assembly)
		{
			if (assembly == null || AssemblyManager.dictCompileAssembly.Count >= 500)
			{
				return;
			}
			int hashCode = sourceCode.GetHashCode();
			if (!AssemblyManager.dictCompileAssembly.ContainsKey(hashCode))
			{
				AssemblyManager.dictCompileAssembly.TryAdd(hashCode, assembly);
			}
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00003EA1 File Offset: 0x000020A1
		public static void ClearAeembly()
		{
			AssemblyManager.dictCompileAssembly.Clear();
		}

		// Token: 0x04000026 RID: 38
		private const int MaxCacheNum = 500;

		// Token: 0x04000027 RID: 39
		private static ConcurrentDictionary<int, Assembly> dictCompileAssembly = new ConcurrentDictionary<int, Assembly>();
	}
}
