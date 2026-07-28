using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Script.Algorithm;
using Script.Methods;

namespace Script.Support
{
	// Token: 0x02000004 RID: 4
	public class ComScriptSupport : HikScriptSupport
	{
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600001D RID: 29 RVA: 0x00002A58 File Offset: 0x00000C58
		// (set) Token: 0x0600001E RID: 30 RVA: 0x00002A60 File Offset: 0x00000C60
		public override string ShellModulePath
		{
			get
			{
				return this._shellPath;
			}
			set
			{
				this._shellPath = value;
				Debugger.Log(0, null, "ScriptCom:" + this._shellPath);
			}
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002A80 File Offset: 0x00000C80
		public void Init(string path)
		{
			this.objMemoryCfg = new SharedMemoryCfg();
			this._shellPath = path;
			AppDomain.CurrentDomain.AssemblyResolve += this.CurrentDomain_AssemblyResolve;
			ScriptMethods.InitForDLL();
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002AAF File Offset: 0x00000CAF
		public void Dispose()
		{
			if (this.objMemoryCfg != null)
			{
				this.objMemoryCfg.ReleaseMemory();
			}
			AppDomain.CurrentDomain.AssemblyResolve -= this.CurrentDomain_AssemblyResolve;
			base.objectDispose();
			this.objMemoryCfg = null;
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002AE8 File Offset: 0x00000CE8
		private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			Assembly assembly = null;
			AssemblyName assemblyName = new AssemblyName(args.Name);
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			if (assemblies == null)
			{
				return assembly;
			}
			foreach (Assembly assembly2 in assemblies)
			{
				if (assembly2.FullName.Equals(args.Name, StringComparison.CurrentCultureIgnoreCase))
				{
					assembly = assembly2;
					break;
				}
			}
			if (assembly == null)
			{
				string path = this.ShellModulePath + "DLL\\" + assemblyName.Name + ".dll";
				if (!File.Exists(path))
				{
					path = this.ShellModulePath + assemblyName.Name + ".dll";
				}
				if (File.Exists(path))
				{
					byte[] array2 = null;
					using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						int num = (int)fileStream.Length;
						array2 = new byte[num];
						fileStream.Read(array2, 0, array2.Length);
					}
					if (array2 != null)
					{
						assembly = Assembly.Load(array2);
					}
				}
			}
			return assembly;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002BF8 File Offset: 0x00000DF8
		public override bool StartAppdomain(string outputAssembly, int nModuleid, Assembly scriptAssembly)
		{
			bool result;
			try
			{
				this.objRemoteLoader = new RemoteLoaderFactory();
				if (this.objRemoteLoader != null)
				{
					bool flag = this.objRemoteLoader.CreateInstanceFromByte(this.ShellModulePath + outputAssembly, "UserScript", null, scriptAssembly);
					if (flag)
					{
						ComAlgorithm comAlgorithm = new ComAlgorithm
						{
							m_nModuleID = nModuleid
						};
						comAlgorithm.SetMemoryCfgObj(this.objMemoryCfg);
						this.objRemoteLoader.SetAlgorithm(comAlgorithm);
					}
					result = flag;
				}
				else
				{
					LogHelper.Error("StartAppdomain error: create objRemoteLoader failed", nModuleid);
					result = false;
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("start appdomain is error." + ex.ToString(), 0);
				result = false;
			}
			return result;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002CA4 File Offset: 0x00000EA4
		public override bool SetImageIoName(Dictionary<string, ImageIoName> objDict)
		{
			if (this.objRemoteLoader != null)
			{
				this.objRemoteLoader.SetAlgorithmData("image", objDict);
			}
			return true;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002CC0 File Offset: 0x00000EC0
		public override bool SetRoiBoxIoName(Dictionary<string, RoiBoxIoName> objDict)
		{
			if (this.objRemoteLoader != null)
			{
				this.objRemoteLoader.SetAlgorithmData("roibox", objDict);
			}
			return true;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002CDC File Offset: 0x00000EDC
		public override bool SetAnnulusIoName(Dictionary<string, AnnulusIoName> objDict)
		{
			if (this.objRemoteLoader != null)
			{
				this.objRemoteLoader.SetAlgorithmData("roiannulus", objDict);
			}
			return true;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002CF8 File Offset: 0x00000EF8
		public override bool SetPolygonIoName(Dictionary<string, PolygonIoName> objDict)
		{
			if (this.objRemoteLoader != null)
			{
				this.objRemoteLoader.SetAlgorithmData("roipolygon", objDict);
			}
			return true;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002D14 File Offset: 0x00000F14
		public override bool SetPointIoName(Dictionary<string, PointIoName> objDict)
		{
			if (this.objRemoteLoader != null)
			{
				this.objRemoteLoader.SetAlgorithmData("point", objDict);
			}
			return true;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002D30 File Offset: 0x00000F30
		public override bool SetLineIoName(Dictionary<string, LineIoName> objDict)
		{
			if (this.objRemoteLoader != null)
			{
				this.objRemoteLoader.SetAlgorithmData("line", objDict);
			}
			return true;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002D4C File Offset: 0x00000F4C
		public override bool SetFixtureIoName(Dictionary<string, FixtureIoName> objDict)
		{
			if (this.objRemoteLoader != null)
			{
				this.objRemoteLoader.SetAlgorithmData("fixture", objDict);
			}
			return true;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002D68 File Offset: 0x00000F68
		public override bool SetCircleIoName(Dictionary<string, CircleIoName> objDict)
		{
			if (this.objRemoteLoader != null)
			{
				this.objRemoteLoader.SetAlgorithmData("circle", objDict);
			}
			return true;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002D84 File Offset: 0x00000F84
		public override bool SetRectIoName(Dictionary<string, RectIoName> objDict)
		{
			if (this.objRemoteLoader != null)
			{
				this.objRemoteLoader.SetAlgorithmData("rect", objDict);
			}
			return true;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002DA0 File Offset: 0x00000FA0
		public override bool SetEllipseIoName(Dictionary<string, EllipseIoName> objDict)
		{
			if (this.objRemoteLoader != null)
			{
				this.objRemoteLoader.SetAlgorithmData("ellipse", objDict);
			}
			return true;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002DBC File Offset: 0x00000FBC
		public void SetNodeNum(string strNodeNum)
		{
			if (this.objMemoryCfg != null)
			{
				this.objMemoryCfg.SetNodeNum(strNodeNum);
			}
		}

		// Token: 0x04000012 RID: 18
		private string _shellPath = "";

		// Token: 0x04000013 RID: 19
		private SharedMemoryCfg objMemoryCfg;
	}
}
