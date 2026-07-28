using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using VM.GlobalScript.Methods;

namespace VM.GlobalScript.Support
{
	// Token: 0x0200002D RID: 45
	public class UserGlobalScriptSupport
	{
		// Token: 0x1700003A RID: 58
		// (get) Token: 0x06000162 RID: 354 RVA: 0x0000B9E0 File Offset: 0x00009BE0
		// (set) Token: 0x06000163 RID: 355 RVA: 0x0000B9F8 File Offset: 0x00009BF8
		public bool IsUsePlatformSDK
		{
			get
			{
				return this.isUsePlatformSDK;
			}
			set
			{
				this.isUsePlatformSDK = value;
			}
		}

		// Token: 0x06000164 RID: 356 RVA: 0x0000BA04 File Offset: 0x00009C04
		public static UserGlobalScriptSupport GetScriptInstance()
		{
			bool flag = UserGlobalScriptSupport._userGlobalScriptSupportInstance == null;
			if (flag)
			{
				UserGlobalScriptSupport._userGlobalScriptSupportInstance = new UserGlobalScriptSupport();
			}
			return UserGlobalScriptSupport._userGlobalScriptSupportInstance;
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x06000165 RID: 357 RVA: 0x0000BA34 File Offset: 0x00009C34
		// (set) Token: 0x06000166 RID: 358 RVA: 0x0000BA4C File Offset: 0x00009C4C
		public string AppBaseDirectory
		{
			get
			{
				return this.appBaseDirectory;
			}
			set
			{
				try
				{
					bool flag = value != null;
					if (flag)
					{
						this.appBaseDirectory = value;
						DirectoryInfo directoryInfo = new DirectoryInfo(this.appBaseDirectory);
						this.VMBaseDllPath = directoryInfo.Parent.FullName;
					}
				}
				catch (Exception ex)
				{
					LogHelper.Error("Set app base directory error: " + ex.Message);
				}
			}
		}

		// Token: 0x06000167 RID: 359 RVA: 0x0000BAB8 File Offset: 0x00009CB8
		private UserGlobalScriptSupport()
		{
			this._objectClass = null;
			this._objSetup = null;
			this._objAppDomain = null;
			this.ResultInfo = null;
			AppDomain.CurrentDomain.AssemblyResolve += this.CurrentDomain_AssemblyResolve;
		}

		// Token: 0x06000168 RID: 360 RVA: 0x0000BB3C File Offset: 0x00009D3C
		private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			Assembly assembly = null;
			AssemblyName assemblyName = new AssemblyName(args.Name);
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			bool flag = assemblies == null;
			Assembly result;
			if (flag)
			{
				result = assembly;
			}
			else
			{
				foreach (Assembly assembly2 in assemblies)
				{
					bool flag2 = assembly2.FullName.Equals(args.Name, StringComparison.CurrentCultureIgnoreCase);
					if (flag2)
					{
						assembly = assembly2;
						break;
					}
				}
				bool flag3 = assembly == null;
				if (flag3)
				{
					string path = this.AppBaseDirectory + "DLL\\" + assemblyName.Name + ".dll";
					bool flag4 = !File.Exists(path);
					if (flag4)
					{
						path = this.AppBaseDirectory + assemblyName.Name + ".dll";
					}
					bool flag5 = File.Exists(path);
					if (flag5)
					{
						byte[] array2 = null;
						using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						{
							int num = (int)fileStream.Length;
							array2 = new byte[num];
							int num2 = fileStream.Read(array2, 0, array2.Length);
						}
						bool flag6 = array2 != null;
						if (flag6)
						{
							assembly = Assembly.Load(array2);
						}
					}
				}
				result = assembly;
			}
			return result;
		}

		// Token: 0x06000169 RID: 361 RVA: 0x0000BC8C File Offset: 0x00009E8C
		private bool InitSource()
		{
			this._objSetup = new AppDomainSetup();
			bool flag = this._objSetup == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				this._objSetup.ApplicationBase = this.AppBaseDirectory;
				this._objAppDomain = AppDomain.CreateDomain("MyAppDomain", null, this._objSetup);
				bool flag2 = this._objAppDomain == null;
				result = !flag2;
			}
			return result;
		}

		// Token: 0x0600016A RID: 362 RVA: 0x0000BCF8 File Offset: 0x00009EF8
		private void unLoad()
		{
			try
			{
				bool flag = this.objRemoteLoader != null;
				if (flag)
				{
					this.objRemoteLoader.Unload();
					this.objRemoteLoader = null;
				}
				bool flag2 = this._objAppDomain != null;
				if (flag2)
				{
					AppDomain.Unload(this._objAppDomain);
					this._objAppDomain = null;
					this._objSetup = null;
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("unLoad is error," + ex.ToString());
			}
		}

		// Token: 0x0600016B RID: 363 RVA: 0x0000BD80 File Offset: 0x00009F80
		public void DisposeAndUnload()
		{
			this.DisposeObjectClass();
			this._objectClass = null;
			this.unLoad();
		}

		// Token: 0x0600016C RID: 364 RVA: 0x0000BD98 File Offset: 0x00009F98
		private bool StartAppdomain(string shellContent, bool isCompile = true)
		{
			bool result;
			try
			{
				bool flag = !this.IsUsePlatformSDK;
				if (flag)
				{
					LogHelper.Debug("StartAppdomain start");
					this._objSetup = new AppDomainSetup();
					this._objSetup.ApplicationBase = this.AppBaseDirectory;
					this._objSetup.ShadowCopyFiles = "false";
					this._objAppDomain = AppDomain.CreateDomain("myAppdomain", null, this._objSetup);
					this.objRemoteLoader = (RemoteLoader)this._objAppDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().GetName().FullName, typeof(RemoteLoader).FullName);
					bool flag2 = this.objRemoteLoader != null;
					if (flag2)
					{
						bool flag3 = true;
						if (isCompile)
						{
							flag3 = this.objRemoteLoader.Compile(shellContent, this.GetRefrences(), false, out this.ResultInfo, ref this.lastDateTime);
						}
						bool flag4 = flag3;
						if (flag4)
						{
							this._objAppDomain.SetData("sdkfunction", PlatformSdkFunction.GetInstance().m_operateHandle);
							this.objRemoteLoader.SetObjData();
							flag3 = this.objRemoteLoader.CreateShellInstance(ref this.ResultInfo);
						}
						LogHelper.Debug("StartAppdomain end");
						result = flag3;
					}
					else
					{
						result = false;
					}
				}
				else
				{
					this.objRemoteLoader = new RemoteLoader(this.AppBaseDirectory);
					bool flag5 = this.objRemoteLoader.CreateShellInstance(shellContent, this.GetRefrences(), false, out this.ResultInfo, ref this.lastDateTime, isCompile);
					LogHelper.Debug("StartAppdomain end");
					result = flag5;
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("start appdomain is error." + ex.ToString());
				result = false;
			}
			return result;
		}

		// Token: 0x0600016D RID: 365 RVA: 0x0000BF58 File Offset: 0x0000A158
		public string CompileCode(string shellContent, out bool compileOK)
		{
			string text = "";
			compileOK = false;
			try
			{
				this.DisposeObjectClass();
				this.unLoad();
				this.StartAppdomain(shellContent, true);
				this.GetErrorInfo(this.ResultInfo, out text, out compileOK);
				LogHelper.Info(text);
			}
			catch (Exception ex)
			{
				LogHelper.Error("complie code error," + ex.Message);
			}
			return text;
		}

		// Token: 0x0600016E RID: 366 RVA: 0x0000BFD4 File Offset: 0x0000A1D4
		public void DisposeObjectClass()
		{
			try
			{
				bool flag = this.objRemoteLoader != null;
				if (flag)
				{
					bool flag2 = false;
					this.objRemoteLoader.ExecuteMethod("Dispose", null, ref flag2);
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("DisposeObjectClass Error " + ex.Message);
			}
		}

		// Token: 0x0600016F RID: 367 RVA: 0x0000C038 File Offset: 0x0000A238
		public void InitProcessMsg()
		{
			try
			{
				bool flag = this._objectClass != null;
				if (flag)
				{
					this._objectClass.GetType().InvokeMember("InitProcessID", BindingFlags.InvokeMethod, null, this._objectClass, null);
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("init processid error，" + ex.Message);
			}
		}

		// Token: 0x06000170 RID: 368 RVA: 0x0000C0A8 File Offset: 0x0000A2A8
		public void LoadSolutionInit()
		{
			try
			{
				bool flag = AppDomain.CurrentDomain == null;
				if (flag)
				{
					LogHelper.Error("LoadSolutionInit AppDomain is null ");
				}
				else
				{
					bool flag2 = this.objRemoteLoader != null;
					if (flag2)
					{
						bool flag3 = false;
						RemoteLoader remoteLoader = this.objRemoteLoader;
						if (remoteLoader != null)
						{
							remoteLoader.ExecuteMethod("InitAfterLoadSol", null, ref flag3);
						}
						RemoteLoader remoteLoader2 = this.objRemoteLoader;
						if (remoteLoader2 != null)
						{
							remoteLoader2.ExecuteMethod("StartTryGlobalCommunicate", null, ref flag3);
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("LoadSolutionInit Error " + ex.Message);
			}
		}

		// Token: 0x06000171 RID: 369 RVA: 0x0000C144 File Offset: 0x0000A344
		public void GetErrorInfo(ArrayList compileResult, out string compileError, out bool compileOk)
		{
			bool flag = compileResult == null;
			if (flag)
			{
				compileError = string.Format("{0} : Compile complete -- no compile result \r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"));
				compileOk = false;
			}
			else
			{
				int num = 0;
				int num2 = 0;
				string text = "";
				foreach (object obj in compileResult)
				{
					bool flag2 = obj == null;
					if (!flag2)
					{
						HikCompileMessage hikCompileMessage = (HikCompileMessage)obj;
						bool isError = hikCompileMessage.IsError;
						if (isError)
						{
							num2++;
							text += string.Format("Line:{0} -- Error:{1}\r\n", hikCompileMessage.Line, hikCompileMessage.Text);
						}
						else
						{
							num++;
							text += string.Format("Line:{0} -- Warnings:{1}\r\n", hikCompileMessage.Line, hikCompileMessage.Text);
						}
					}
				}
				string str = string.Format("{0} : Compile complete -- {1} Errors, {2} Warnings \r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"), num2, num);
				compileError = str + text;
				this._isCompileOK = (compileOk = (num2 == 0));
			}
		}

		// Token: 0x06000172 RID: 370 RVA: 0x0000C2A0 File Offset: 0x0000A4A0
		private ArrayList GetRefrences()
		{
			bool flag = this.ArrayRefrences != null && this.ArrayRefrences.Count > 0;
			ArrayList arrayRefrences;
			if (flag)
			{
				arrayRefrences = this.ArrayRefrences;
			}
			else
			{
				this.ArrayRefrences = new ArrayList();
				this.ArrayRefrences.Add("System.dll");
				this.ArrayRefrences.Add("System.Core.dll");
				this.ArrayRefrences.Add("System.Windows.dll");
				this.ArrayRefrences.Add("System.Windows.Forms.dll");
				this.ArrayRefrences.Add("System.Drawing.dll");
				this.ArrayRefrences.Add(this.AppBaseDirectory + "iMVS-6000PlatformSDKCS.dll");
				this.ArrayRefrences.Add(this.AppBaseDirectory + "VM.GlobalScript.Methods.dll");
				this.ArrayRefrences.Add(this.AppBaseDirectory + "Apps.Json.dll");
				bool flag2 = this.IsUsePlatformSDK;
				if (flag2)
				{
					this.ArrayRefrences.Add(this.GetVMAssemblyPath(this.VMRegisterPath + "VM.Core.dll", "VM.Core"));
					this.ArrayRefrences.Add(this.GetVMAssemblyPath(this.VMRegisterPath + "VM.PlatformSDKCS.dll", "VM.PlatformSDKCS"));
					this.ArrayRefrences.Add(this.GetVMAssemblyPath(this.VMRegisterPath + "VMControls.BaseInterface.dll", "VMControls.BaseInterface"));
					this.ArrayRefrences.Add(this.GetVMAssemblyPath(this.VMRegisterPath + "VMControls.Interface.dll", "VMControls.Interface"));
					this.ArrayRefrences.Add(this.GetVMAssemblyPath(this.VMRegisterPath + "VMControls.RenderInterface.dll", "VMControls.RenderInterface"));
					string vmassemblyPath = this.GetVMAssemblyPath(this.VMRegisterPath + "ImageSourceModuleCs.dll", "ImageSourceModuleCs");
					bool flag3 = !File.Exists(vmassemblyPath);
					if (flag3)
					{
						LogHelper.Error(string.Format("{0} is not exit", vmassemblyPath));
					}
					else
					{
						this.ArrayRefrences.Add(vmassemblyPath);
					}
					vmassemblyPath = this.GetVMAssemblyPath(this.VMRegisterPath + "IMVSFastFeatureMatchModuCs.dll", "IMVSFastFeatureMatchModuCs");
					bool flag4 = !File.Exists(vmassemblyPath);
					if (flag4)
					{
						LogHelper.Error(string.Format("{0} is not exit", vmassemblyPath));
					}
					else
					{
						this.ArrayRefrences.Add(vmassemblyPath);
					}
				}
				arrayRefrences = this.ArrayRefrences;
			}
			return arrayRefrences;
		}

		// Token: 0x06000173 RID: 371 RVA: 0x0000C508 File Offset: 0x0000A708
		private string GetVMAssemblyPath(string assemblyPath, string assemblyName)
		{
			bool flag = !string.IsNullOrEmpty(assemblyName);
			if (flag)
			{
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				Assembly assembly = assemblies.FirstOrDefault((Assembly x) => x.GetName().Name == assemblyName);
				bool flag2 = assembly != null;
				if (flag2)
				{
					return assembly.Location;
				}
			}
			return assemblyPath;
		}

		// Token: 0x06000174 RID: 372 RVA: 0x0000C574 File Offset: 0x0000A774
		public void SetRefrences(string[] refresces)
		{
			bool flag = refresces == null || refresces.Length == 0;
			if (!flag)
			{
				bool flag2 = this.ArrayRefrences == null;
				if (flag2)
				{
					this.ArrayRefrences = new ArrayList();
				}
				this.ArrayRefrences.Clear();
				Array.ForEach<string>(refresces, delegate(string x)
				{
					this.ArrayRefrences.Add(x);
				});
			}
		}

		// Token: 0x06000175 RID: 373 RVA: 0x0000C5D0 File Offset: 0x0000A7D0
		public bool CodeInitFunction(bool bExecuteContinues, bool isCrash)
		{
			bool flag = this.objRemoteLoader == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool flag2 = !this._isCompileOK;
				if (flag2)
				{
					result = false;
				}
				else
				{
					try
					{
						bool flag3 = false;
						this.objRemoteLoader.ExecuteMethod("DefaultInitProcess", new object[]
						{
							bExecuteContinues,
							isCrash
						}, ref flag3);
						result = flag3;
					}
					catch (Exception ex)
					{
						LogHelper.Error("Global Script Execute CodeInitFunction " + ex.StackTrace);
						result = false;
					}
				}
			}
			return result;
		}

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x06000176 RID: 374 RVA: 0x0000C664 File Offset: 0x0000A864
		// (remove) Token: 0x06000177 RID: 375 RVA: 0x0000C69C File Offset: 0x0000A89C
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public event Action<string, int, string> UpdateUIScriptEvent = null;

		// Token: 0x06000178 RID: 376 RVA: 0x0000C6D4 File Offset: 0x0000A8D4
		public bool LoadExternAssembly(bool bExecuteContinues, bool isCrash)
		{
			bool result;
			try
			{
				bool isReLoadAssembly = this.objRemoteLoader.GetIsReLoadAssembly(ref this.lastDateTime);
				bool flag = !isReLoadAssembly;
				if (flag)
				{
					result = true;
				}
				else
				{
					this.DisposeObjectClass();
					this._objectClass = null;
					this.unLoad();
					GC.Collect();
					bool flag2 = this.StartAppdomain("", false);
					bool flag3 = !flag2;
					if (flag3)
					{
						result = false;
					}
					else
					{
						bool flag4 = this.UpdateUIScriptEvent != null;
						if (flag4)
						{
							this.UpdateUIScriptEvent("updateScript", 0, this.AppBaseDirectory + "\\GlobalUserScript\\UserGlobalScript.cs");
						}
						result = this.CodeInitFunction(bExecuteContinues, isCrash);
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("LoadExternAssembly is error," + ex.ToString());
				result = false;
			}
			return result;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x0000C7AC File Offset: 0x0000A9AC
		public bool CodeRun(ref int errorCode)
		{
			errorCode = 0;
			bool flag = this.objRemoteLoader != null;
			if (flag)
			{
				bool flag2 = this.objRemoteLoader.ExecuteProcessMethod(ref errorCode);
			}
			bool flag3 = (long)errorCode != 0L;
			return !flag3;
		}

		// Token: 0x0600017A RID: 378 RVA: 0x0000C7F0 File Offset: 0x0000A9F0
		public uint GetScriptContinusExecuteInterval()
		{
			bool flag = this.objRemoteLoader != null;
			uint result;
			if (flag)
			{
				result = this.objRemoteLoader.GetScriptContinusExecuteInterval();
			}
			else
			{
				result = 100U;
			}
			return result;
		}

		// Token: 0x04000150 RID: 336
		private AppDomainSetup _objSetup;

		// Token: 0x04000151 RID: 337
		private AppDomain _objAppDomain;

		// Token: 0x04000152 RID: 338
		private object _objectClass;

		// Token: 0x04000153 RID: 339
		public ArrayList ResultInfo;

		// Token: 0x04000154 RID: 340
		private RemoteLoader objRemoteLoader = null;

		// Token: 0x04000155 RID: 341
		private DateTime lastDateTime;

		// Token: 0x04000156 RID: 342
		private bool _isExecuteInit = false;

		// Token: 0x04000157 RID: 343
		private bool _isCompileOK = false;

		// Token: 0x04000158 RID: 344
		private bool isUsePlatformSDK = true;

		// Token: 0x04000159 RID: 345
		private static UserGlobalScriptSupport _userGlobalScriptSupportInstance = null;

		// Token: 0x0400015A RID: 346
		private string appBaseDirectory;

		// Token: 0x0400015B RID: 347
		private string VMBaseDllPath = "D:\\Program\\VM4.3.0\\Public_Release\\myLibs\\";

		// Token: 0x0400015C RID: 348
		public string VMRegisterPath = null;

		// Token: 0x0400015D RID: 349
		public ArrayList ArrayRefrences = null;
	}
}
