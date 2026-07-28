using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.CSharp;
using Microsoft.Win32;
using Script.Algorithm;

namespace Script.Support
{
	// Token: 0x02000002 RID: 2
	public class BaseScriptSupport
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		// (set) Token: 0x06000002 RID: 2 RVA: 0x00002058 File Offset: 0x00000258
		public string AssemblyGuid { get; set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002061 File Offset: 0x00000261
		public string VmAssemblyPath
		{
			get
			{
				if (string.IsNullOrEmpty(this.vmAssemblyPath))
				{
					this.vmAssemblyPath = this.GetRegisterValueByName(this._registrykeyPathName, "");
				}
				return this.vmAssemblyPath;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000004 RID: 4 RVA: 0x0000208D File Offset: 0x0000028D
		// (set) Token: 0x06000005 RID: 5 RVA: 0x00002099 File Offset: 0x00000299
		public virtual string ShellModulePath
		{
			get
			{
				return AppDomain.CurrentDomain.BaseDirectory;
			}
			set
			{
			}
		}

		// Token: 0x06000006 RID: 6 RVA: 0x0000209C File Offset: 0x0000029C
		public BaseScriptSupport()
		{
			this.codeVer = 131328;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000020FC File Offset: 0x000002FC
		private int CheckCodeVersion(string code)
		{
			int result;
			if (-1 != code.IndexOf("ScriptMethods,IProcessMethods", 0))
			{
				result = 131584;
			}
			else
			{
				result = 131328;
			}
			return result;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x0000212C File Offset: 0x0000032C
		public bool Compile(string source, string property, ArrayList references, out ArrayList myResut, bool compileDebug, int nModuleid, bool isFromLoad = false)
		{
			this.currentAssemblyName = string.Format("UserScript_{0}.dll", nModuleid);
			this.currentAssemblyPdbName = string.Format("UserScript_{0}.pdb", nModuleid);
			myResut = new ArrayList();
			if (references == null)
			{
				myResut.Add(new HikCompileMessage("Program compilation parameters are abnormal.", 0, 0, false));
				return false;
			}
			bool result;
			try
			{
				this.objectDispose();
				this.compilerResults = null;
				bool flag = true;
				if (isFromLoad)
				{
					string text = this.ShellModulePath + this.currentAssemblyName;
					if (File.Exists(text))
					{
						FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(text);
						if (versionInfo.ProductName == this.AssemblyGuid)
						{
							flag = false;
						}
					}
				}
				string sourceCode = source + property + nModuleid.ToString();
				Assembly exitAssembly = AssemblyManager.GetExitAssembly(sourceCode);
				if (exitAssembly == null && flag)
				{
					if (File.Exists(this.ShellModulePath + this.currentAssemblyName))
					{
						File.Delete(this.ShellModulePath + this.currentAssemblyName);
					}
					if (File.Exists(this.ShellModulePath + this.currentAssemblyPdbName))
					{
						File.Delete(this.ShellModulePath + this.currentAssemblyPdbName);
					}
					CompilerParameters compilerParameters = new CompilerParameters();
					compilerParameters.GenerateExecutable = false;
					compilerParameters.GenerateInMemory = false;
					compilerParameters.IncludeDebugInformation = false;
					compilerParameters.OutputAssembly = this.ShellModulePath + this.currentAssemblyName;
					compilerParameters.WarningLevel = 4;
					string text2 = this.ShellModulePath + "TEMP\\";
					if (Directory.Exists(text2))
					{
						compilerParameters.GenerateInMemory = false;
						compilerParameters.GenerateExecutable = false;
						compilerParameters.TempFiles = new TempFileCollection(text2, true);
						compilerParameters.OutputAssembly = this.ShellModulePath + this.currentAssemblyName;
						compilerParameters.IncludeDebugInformation = true;
						compilerParameters.TempFiles.KeepFiles = true;
					}
					foreach (object obj in references)
					{
						string value = (string)obj;
						compilerParameters.ReferencedAssemblies.Add(value);
					}
					string text3 = "/lib:";
					string text4 = this.ShellModulePath + "DLL";
					string compilerOptions = compilerParameters.CompilerOptions;
					compilerParameters.CompilerOptions = string.Concat(new string[]
					{
						compilerOptions,
						" \"",
						text3,
						text4,
						"\""
					});
					if (!string.IsNullOrEmpty(this.VmAssemblyPath))
					{
						CompilerParameters compilerParameters2 = compilerParameters;
						string compilerOptions2 = compilerParameters2.CompilerOptions;
						compilerParameters2.CompilerOptions = string.Concat(new string[]
						{
							compilerOptions2,
							" \"",
							text3,
							this.VmAssemblyPath,
							"\""
						});
					}
					CompilerParameters compilerParameters3 = compilerParameters;
					compilerParameters3.CompilerOptions += " /unsafe";
					LogHelper.Info("CompilerOptions: " + compilerParameters.CompilerOptions, nModuleid);
					string text5 = Guid.NewGuid().ToString("D");
					try
					{
						this.AssemblyMsg = this.AssemblyMsg.Replace("guid", text5);
						this.compilerResults = this.objCSharpCodePrivoder.CompileAssemblyFromSource(compilerParameters, new string[]
						{
							source,
							property,
							this.AssemblyMsg
						});
						if (File.Exists(this.ShellModulePath + this.currentAssemblyName))
						{
							FileInfo fileInfo = new FileInfo(this.ShellModulePath + this.currentAssemblyName);
							this.lastDateTime = fileInfo.LastWriteTime;
						}
						else
						{
							this.lastDateTime = DateTime.Now;
						}
					}
					catch (Exception ex)
					{
						LogHelper.Error("编译错误:" + ex.ToString(), 0);
						myResut.Add(new HikCompileMessage("Compilation error: reference assemblies may not have been added correctly.", 0, 0, false));
						text5 = "";
						this.AssemblyGuid = "";
						return false;
					}
					bool flag2 = false;
					for (int i = 0; i < this.compilerResults.Errors.Count; i++)
					{
						if (!this.compilerResults.Errors[i].IsWarning)
						{
							flag2 = true;
						}
						HikCompileMessage value2 = new HikCompileMessage(this.compilerResults.Errors[i].ErrorText, this.compilerResults.Errors[i].Line, this.compilerResults.Errors[i].Column, this.compilerResults.Errors[i].IsWarning);
						myResut.Add(value2);
					}
					if (flag2)
					{
						this.AssemblyGuid = "";
						return false;
					}
					this.AssemblyGuid = text5;
				}
				if (!this.StartAppdomain(this.currentAssemblyName, nModuleid, exitAssembly))
				{
					result = false;
				}
				else
				{
					this.codeVer = this.CheckCodeVersion(source);
					bool flag3 = this.codeVer > 131328;
					if (this.objRemoteLoader == null)
					{
						LogHelper.Error("objRemoteLoader is null", 0);
						result = false;
					}
					else
					{
						flag3 = this.objRemoteLoader.CheckClass(flag3, out myResut);
						if (flag3)
						{
							if (exitAssembly == null && flag)
							{
								AssemblyManager.AddAssembly(sourceCode, this.objRemoteLoader.GetAssembly());
							}
							else
							{
								this.lastDateTime = DateTime.Now;
							}
						}
						result = flag3;
					}
				}
			}
			catch (Exception ex2)
			{
				myResut.Add(new HikCompileMessage("Compilation error: " + ex2.Message, 0, 0, false));
				LogHelper.Error("Compilation error: " + ex2.ToString(), 0);
				result = false;
			}
			return result;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002718 File Offset: 0x00000918
		private string GetRegisterValueByName(string path, string name)
		{
			string result = string.Empty;
			try
			{
				RegistryKey localMachine = Registry.LocalMachine;
				if (localMachine != null)
				{
					RegistryKey registryKey = localMachine.OpenSubKey(path);
					if (registryKey != null)
					{
						object value = registryKey.GetValue(name);
						if (value != null)
						{
							string text = value as string;
							if (!string.IsNullOrWhiteSpace(text))
							{
								result = text;
							}
						}
						registryKey.Close();
					}
					localMachine.Close();
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("GetRegisterValueByName error: " + ex.Message, 0);
			}
			return result;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x00002798 File Offset: 0x00000998
		public void objectDispose()
		{
			if (this.objRemoteLoader != null)
			{
				this.objRemoteLoader.Unload();
			}
			this.objRemoteLoader = null;
			if (this.objAppDomain != null)
			{
				AppDomain.Unload(this.objAppDomain);
				this.objAppDomain = null;
				this.objSetup = null;
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000027D8 File Offset: 0x000009D8
		private bool GetIsReLoadAssembly()
		{
			if (!File.Exists(this.ShellModulePath + this.currentAssemblyName))
			{
				return false;
			}
			FileInfo fileInfo = new FileInfo(this.ShellModulePath + this.currentAssemblyName);
			DateTime lastWriteTime = fileInfo.LastWriteTime;
			if (lastWriteTime.Subtract(this.lastDateTime).TotalSeconds > 5.0)
			{
				this.lastDateTime = lastWriteTime;
				return true;
			}
			return false;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002848 File Offset: 0x00000A48
		public bool LoadExternAssembly(ref bool updateCode, int nModuleid, Action act)
		{
			bool result;
			try
			{
				if (!this.GetIsReLoadAssembly())
				{
					result = true;
				}
				else
				{
					this.objectDispose();
					if (!this.StartAppdomain(this.currentAssemblyName, nModuleid, null))
					{
						result = false;
					}
					else
					{
						if (this.objRemoteLoader != null)
						{
							ArrayList arrayList;
							if (!this.objRemoteLoader.CheckClass(true, out arrayList))
							{
								return false;
							}
							if (act != null)
							{
								act();
							}
						}
						result = true;
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("LoadExternAssembly is error," + ex.ToString(), 0);
				result = false;
			}
			return result;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000028DC File Offset: 0x00000ADC
		public bool CodeRun(long input = 0L, long output = 0L, int moduleid = 0)
		{
			if (this.objRemoteLoader == null)
			{
				LogHelper.Error("CodeRun error: objRemoteLoader is null", moduleid);
				return false;
			}
			return this.objRemoteLoader.ExecuteProcessMethod(input, output, moduleid);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x00002901 File Offset: 0x00000B01
		public bool UpdateCode()
		{
			return this.objRemoteLoader != null && this.objRemoteLoader.UpdateCode() == 0;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x0000291B File Offset: 0x00000B1B
		public virtual bool StartAppdomain(string outputAssembly, int nModuleid, Assembly scriptAssembly)
		{
			return true;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000291E File Offset: 0x00000B1E
		public virtual bool SetImageIoName(Dictionary<string, ImageIoName> objDict)
		{
			return true;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002921 File Offset: 0x00000B21
		public virtual bool SetRoiBoxIoName(Dictionary<string, RoiBoxIoName> objDict)
		{
			return true;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002924 File Offset: 0x00000B24
		public virtual bool SetAnnulusIoName(Dictionary<string, AnnulusIoName> objDict)
		{
			return true;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002927 File Offset: 0x00000B27
		public virtual bool SetPolygonIoName(Dictionary<string, PolygonIoName> objDict)
		{
			return true;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x0000292A File Offset: 0x00000B2A
		public virtual bool SetPointIoName(Dictionary<string, PointIoName> objDict)
		{
			return true;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x0000292D File Offset: 0x00000B2D
		public virtual bool SetLineIoName(Dictionary<string, LineIoName> objDict)
		{
			return true;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002930 File Offset: 0x00000B30
		public virtual bool SetFixtureIoName(Dictionary<string, FixtureIoName> objDict)
		{
			return true;
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002933 File Offset: 0x00000B33
		public virtual bool SetCircleIoName(Dictionary<string, CircleIoName> objDict)
		{
			return true;
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002936 File Offset: 0x00000B36
		public virtual bool SetRectIoName(Dictionary<string, RectIoName> objDict)
		{
			return true;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002939 File Offset: 0x00000B39
		public virtual bool SetEllipseIoName(Dictionary<string, EllipseIoName> objDict)
		{
			return true;
		}

		// Token: 0x04000001 RID: 1
		private const int OldVersion = 131328;

		// Token: 0x04000002 RID: 2
		private const int NewVersion = 131584;

		// Token: 0x04000003 RID: 3
		public AppDomainSetup objSetup;

		// Token: 0x04000004 RID: 4
		public AppDomain objAppDomain;

		// Token: 0x04000005 RID: 5
		private int codeVer;

		// Token: 0x04000006 RID: 6
		private CompilerResults compilerResults;

		// Token: 0x04000007 RID: 7
		public RemoteLoaderFactory objRemoteLoader;

		// Token: 0x04000008 RID: 8
		private string currentAssemblyName = "";

		// Token: 0x04000009 RID: 9
		private string currentAssemblyPdbName = "";

		// Token: 0x0400000A RID: 10
		private DateTime lastDateTime;

		// Token: 0x0400000B RID: 11
		public ArrayList ResultInfo;

		// Token: 0x0400000C RID: 12
		private string AssemblyMsg = "using System.Reflection;\r\nusing System.Runtime.CompilerServices;\r\nusing System.Runtime.InteropServices;\r\n\r\n[assembly: AssemblyProduct(\"guid\")]\r\n";

		// Token: 0x0400000D RID: 13
		private readonly string _registrykeyPathName = "SOFTWARE\\WOW6432Node\\Microsoft\\.NETFramework\\v4.0.30319\\AssemblyFoldersEx\\VisionMaster";

		// Token: 0x0400000E RID: 14
		private string vmAssemblyPath = "";

		// Token: 0x0400000F RID: 15
		private CSharpCodeProvider objCSharpCodePrivoder = new CSharpCodeProvider();
	}
}
