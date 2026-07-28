using System;
using System.Reflection;
using Script.Algorithm;

namespace Script.Support
{
	// Token: 0x02000003 RID: 3
	public class HikScriptSupport : BaseScriptSupport
	{
		// Token: 0x0600001A RID: 26 RVA: 0x0000293C File Offset: 0x00000B3C
		public override bool StartAppdomain(string outputAssembly, int nModuleid, Assembly scriptAssembly)
		{
			bool result;
			try
			{
				this.objSetup = new AppDomainSetup();
				this.objSetup.LoaderOptimization = LoaderOptimization.MultiDomain;
				this.objSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
				this.objAppDomain = AppDomain.CreateDomain("myAppdomain", null, this.objSetup);
				this.objRemoteLoader = (RemoteLoaderFactory)this.objAppDomain.CreateInstance(Assembly.GetExecutingAssembly().GetName().FullName, typeof(RemoteLoaderFactory).FullName).Unwrap();
				if (this.objRemoteLoader != null)
				{
					bool flag = this.objRemoteLoader.CreateInstanceFromByte(outputAssembly, "UserScript", null, null);
					if (flag)
					{
						this.objAppDomain.SetData("MyAlgorithm", HikScriptSupport.Algorithm);
						this.objRemoteLoader.SetObjData();
					}
					result = flag;
				}
				else
				{
					LogHelper.Error("StartAppdomain error: CreateInstance failed", nModuleid);
					result = false;
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("StartAppdomain is exception:" + ex.ToString(), nModuleid);
				result = false;
			}
			return result;
		}

		// Token: 0x04000011 RID: 17
		public static MyAlgorithm Algorithm = new MyAlgorithm();
	}
}
