using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.CSharp;
using VM.GlobalScript.Methods;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000029 RID: 41
	public class RemoteLoader : MarshalByRefObject
	{
		// Token: 0x0600010E RID: 270 RVA: 0x00007D58 File Offset: 0x00005F58
		public RemoteLoader(string applicatPath)
		{
			this.ApplicationPath = applicatPath;
		}

		// Token: 0x0600010F RID: 271 RVA: 0x00007DB0 File Offset: 0x00005FB0
		public RemoteLoader()
		{
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00007E04 File Offset: 0x00006004
		public void LoadAssembly(string assemblyFile, string typeName)
		{
			try
			{
				bool flag = string.IsNullOrEmpty(assemblyFile) || string.IsNullOrEmpty(typeName);
				if (!flag)
				{
					this._objAssembly = Assembly.LoadFrom(assemblyFile);
					this._Type = this._objAssembly.GetType(typeName);
					bool flag2 = this._Type == null;
					if (!flag2)
					{
						this._objectClass = Activator.CreateInstance(this._Type);
					}
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00007E84 File Offset: 0x00006084
		public void Unload()
		{
			this._iProcessClass = null;
			this._objectClass = null;
			this._objAssembly = null;
			this.objCSharpCodePrivoder.Dispose();
			this.objCSharpCodePrivoder = null;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00007EB0 File Offset: 0x000060B0
		public override object InitializeLifetimeService()
		{
			return null;
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00007EC4 File Offset: 0x000060C4
		public void CreateInstance(string assemblyFile, string typeName, object[] constructArgs)
		{
			bool flag = this._Type == null;
			if (flag)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(assemblyFile);
				AppDomain.CurrentDomain.Load(fileNameWithoutExtension);
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (Assembly assembly in assemblies)
				{
					bool flag2 = assembly == null;
					if (!flag2)
					{
						bool flag3 = Path.GetFileName(assembly.Location).Equals(assemblyFile, StringComparison.CurrentCultureIgnoreCase);
						if (flag3)
						{
							this._objAssembly = assembly;
						}
					}
				}
				bool flag4 = this._objAssembly == null;
				if (flag4)
				{
					return;
				}
				Type[] types = this._objAssembly.GetTypes();
				Type type = null;
				foreach (Type type2 in types)
				{
					bool flag5 = type2 == null;
					if (!flag5)
					{
						bool flag6 = type2.FullName == typeName;
						if (flag6)
						{
							type = type2;
							break;
						}
						bool flag7 = type2.BaseType != null;
						if (flag7)
						{
							bool flag8 = type2.BaseType.FullName == typeName;
							if (flag8)
							{
								type = type2;
								break;
							}
						}
						bool flag9 = type2.GetInterface(typeName) != null;
						if (flag9)
						{
							type = type2;
							break;
						}
					}
				}
				bool flag10 = type == null;
				if (flag10)
				{
					return;
				}
				this._Type = type;
			}
			this._objectClass = Activator.CreateInstance(this._Type);
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000804C File Offset: 0x0000624C
		public bool CreateInstanceFromByte(string assemblyFile, string typeName, object[] constructArgs)
		{
			bool result;
			try
			{
				bool flag = this._Type == null;
				if (flag)
				{
					bool flag2 = this._objAssembly == null;
					if (flag2)
					{
						byte[] array = null;
						using (FileStream fileStream = new FileStream(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						{
							int num = (int)fileStream.Length;
							array = new byte[num];
							int num2 = fileStream.Read(array, 0, array.Length);
						}
						bool flag3 = array != null;
						if (flag3)
						{
							this._objAssembly = Assembly.Load(array);
						}
					}
					bool flag4 = this._objAssembly == null;
					if (flag4)
					{
						return false;
					}
					Type[] types = this._objAssembly.GetTypes();
					bool flag5 = types == null;
					if (flag5)
					{
						return false;
					}
					Type type = null;
					foreach (Type type2 in types)
					{
						bool flag6 = type2 == null;
						if (!flag6)
						{
							bool flag7 = type2.FullName == typeName;
							if (flag7)
							{
								type = type2;
								break;
							}
							bool flag8 = type2.BaseType != null && type2.BaseType.FullName == typeName;
							if (flag8)
							{
								type = type2;
								break;
							}
							bool flag9 = type2.GetInterface(typeName) != null;
							if (flag9)
							{
								type = type2;
								break;
							}
						}
					}
					bool flag10 = type == null;
					if (flag10)
					{
						return false;
					}
					this._Type = type;
				}
				this._objectClass = Activator.CreateInstance(this._Type);
				result = (this._objectClass != null);
			}
			catch (Exception ex)
			{
				LogHelper.Error("CreateInstance " + ex.ToString());
				result = false;
			}
			return result;
		}

		// Token: 0x06000115 RID: 277 RVA: 0x00008244 File Offset: 0x00006444
		public bool Compile(string source, ArrayList references, bool compileDebug, out ArrayList ResultInfo, ref DateTime lastDateTime)
		{
			LogHelper.Debug("user compile start");
			ResultInfo = new ArrayList();
			bool flag = references == null;
			bool result;
			if (flag)
			{
				ResultInfo.Add(new HikCompileMessage("LanguageCompileExeception", 0, 0, false));
				result = false;
			}
			else
			{
				try
				{
					GC.Collect();
					bool flag2 = File.Exists(this.ApplicationPath + "GlobalUserScript.dll");
					if (flag2)
					{
						File.Delete(this.ApplicationPath + "GlobalUserScript.dll");
					}
					bool flag3 = File.Exists(this.ApplicationPath + "GlobalUserScript.pdb");
					if (flag3)
					{
						File.Delete(this.ApplicationPath + "GlobalUserScript.pdb");
					}
					CompilerParameters compilerParameters = new CompilerParameters();
					compilerParameters.GenerateExecutable = false;
					compilerParameters.GenerateInMemory = false;
					compilerParameters.IncludeDebugInformation = false;
					compilerParameters.OutputAssembly = this.ApplicationPath + "GlobalUserScript.dll";
					compilerParameters.WarningLevel = 4;
					string location = typeof(HikCompileMessage).Assembly.Location;
					bool flag4 = !references.Contains(location);
					if (flag4)
					{
						references.Add(typeof(HikCompileMessage).Assembly.Location);
					}
					foreach (object obj in references)
					{
						string value = (string)obj;
						compilerParameters.ReferencedAssemblies.Add(value);
					}
					string text = "/lib:";
					string text2 = this.ApplicationPath + "DLL";
					string compilerOptions = compilerParameters.CompilerOptions;
					compilerParameters.CompilerOptions = string.Concat(new string[]
					{
						compilerOptions,
						" \"",
						text,
						text2,
						"\""
					});
					CompilerParameters compilerParameters2 = compilerParameters;
					compilerParameters2.CompilerOptions += " /unsafe";
					DateTime now = DateTime.Now;
					CompilerResults compilerResults = null;
					try
					{
						LogHelper.Debug("CompileAssemblyFromSource start");
						compilerResults = this.objCSharpCodePrivoder.CompileAssemblyFromSource(compilerParameters, new string[]
						{
							source
						});
						LogHelper.Debug("CompileAssemblyFromSource end");
						bool flag5 = File.Exists(this.ApplicationPath + "GlobalUserScript.dll");
						if (flag5)
						{
							FileInfo fileInfo = new FileInfo(this.ApplicationPath + "GlobalUserScript.dll");
							lastDateTime = fileInfo.LastWriteTime;
						}
						else
						{
							lastDateTime = DateTime.Now;
						}
					}
					catch (Exception ex)
					{
						LogHelper.Error("compile is exception:" + ex.ToString());
						ResultInfo.Add(new HikCompileMessage("编译错误：可能未正确添加引用集", 0, 0, false));
						return false;
					}
					DateTime now2 = DateTime.Now;
					bool flag6 = false;
					for (int i = 0; i < compilerResults.Errors.Count; i++)
					{
						bool flag7 = !compilerResults.Errors[i].IsWarning;
						if (flag7)
						{
							flag6 = true;
						}
						HikCompileMessage value2 = new HikCompileMessage(compilerResults.Errors[i].ErrorText, compilerResults.Errors[i].Line, compilerResults.Errors[i].Column, compilerResults.Errors[i].IsWarning);
						ResultInfo.Add(value2);
					}
					bool flag8 = flag6;
					if (flag8)
					{
						return false;
					}
					LogHelper.Debug("user compile end");
				}
				catch (Exception ex2)
				{
					ResultInfo.Add(new HikCompileMessage("LanguageCompileExeception，" + ex2.Message, 0, 0, false));
					LogHelper.Error("编译异常信息:" + ex2.ToString());
					this._objAssembly = null;
					this._objectClass = null;
					return false;
				}
				result = true;
			}
			return result;
		}

		// Token: 0x06000116 RID: 278 RVA: 0x00008660 File Offset: 0x00006860
		public bool CreateShellInstance(ref ArrayList ResultInfo)
		{
			bool flag = this.CreateInstanceFromByte(this.ApplicationPath + "GlobalUserScript.dll", "UserGlobalScript", null);
			bool flag2 = !flag;
			bool result;
			if (flag2)
			{
				result = false;
			}
			else
			{
				flag = this.CheckClass(out ResultInfo);
				result = flag;
			}
			return result;
		}

		// Token: 0x06000117 RID: 279 RVA: 0x000086A8 File Offset: 0x000068A8
		public bool CreateShellInstance(string source, ArrayList references, bool compileDebug, out ArrayList ResultInfo, ref DateTime lastDateTime, bool isCompile)
		{
			ResultInfo = new ArrayList();
			bool flag2;
			if (isCompile)
			{
				Assembly exitAssembly = AssemblyManager.GetExitAssembly(source);
				bool flag = exitAssembly != null;
				if (flag)
				{
					this._objAssembly = exitAssembly;
					flag2 = this.CreateShellInstance(ref ResultInfo);
				}
				else
				{
					flag2 = this.Compile(source, references, compileDebug, out ResultInfo, ref lastDateTime);
					bool flag3 = flag2;
					if (flag3)
					{
						flag2 = this.CreateShellInstance(ref ResultInfo);
						bool flag4 = flag2;
						if (flag4)
						{
							AssemblyManager.AddAssembly(source, this._objAssembly);
						}
					}
				}
			}
			else
			{
				flag2 = this.CreateShellInstance(ref ResultInfo);
			}
			return flag2;
		}

		// Token: 0x06000118 RID: 280 RVA: 0x0000873C File Offset: 0x0000693C
		public bool GetIsReLoadAssembly(ref DateTime ldatetime)
		{
			bool flag = !File.Exists(this.ApplicationPath + "GlobalUserScript.dll");
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				FileInfo fileInfo = new FileInfo(this.ApplicationPath + "GlobalUserScript.dll");
				DateTime lastWriteTime = fileInfo.LastWriteTime;
				bool flag2 = lastWriteTime.Subtract(ldatetime).TotalSeconds > 5.0;
				if (flag2)
				{
					ldatetime = lastWriteTime;
					result = true;
				}
				else
				{
					result = false;
				}
			}
			return result;
		}

		// Token: 0x06000119 RID: 281 RVA: 0x000087C4 File Offset: 0x000069C4
		public bool CheckClass(out ArrayList myResut)
		{
			myResut = new ArrayList();
			bool flag = this._objectClass == null;
			bool result;
			if (flag)
			{
				myResut.Add(new HikCompileMessage("LanguageCompileCreateObjectError UserGlobalScript", 0, 0, false));
				this._objAssembly = null;
				result = false;
			}
			else
			{
				bool flag2 = null == this._objectClass.GetType().GetMethod("Init");
				if (flag2)
				{
					myResut.Add(new HikCompileMessage("LanguageCompileMissFunctionError Init()", 0, 0, false));
					this._objAssembly = null;
					this._objectClass = null;
					result = false;
				}
				else
				{
					bool flag3 = null == this._objectClass.GetType().GetMethod("Process");
					if (flag3)
					{
						myResut.Add(new HikCompileMessage("LanguageCompileMissFunctionError Process()", 0, 0, false));
						this._objAssembly = null;
						this._objectClass = null;
						result = false;
					}
					else
					{
						this._iProcessClass = (IScriptMethods)this._objectClass;
						try
						{
							bool flag4 = this._iProcessClass != null;
							if (flag4)
							{
								int num = this._iProcessClass.Init();
								bool flag5 = num != 0;
								if (flag5)
								{
									myResut.Add(new HikCompileMessage("LanguageCompileExecuteError Init()，" + num, 0, 0, false));
									this._objAssembly = null;
									this._objectClass = null;
									return false;
								}
							}
							else
							{
								this._objectClass.GetType().InvokeMember("Init", BindingFlags.InvokeMethod, null, this._objectClass, null);
							}
						}
						catch (Exception ex)
						{
							myResut.Add(new HikCompileMessage("LanguageCompileExecuteError Init()，" + ex.Message, 0, 0, false));
							this._objAssembly = null;
							this._objectClass = null;
							return false;
						}
						result = true;
					}
				}
			}
			return result;
		}

		// Token: 0x0600011A RID: 282 RVA: 0x00008984 File Offset: 0x00006B84
		public void SetObjData()
		{
			PlatformSdkFunction.GetInstance().m_operateHandle = (IntPtr)AppDomain.CurrentDomain.GetData("sdkfunction");
		}

		// Token: 0x0600011B RID: 283 RVA: 0x000089A8 File Offset: 0x00006BA8
		public uint GetScriptContinusExecuteInterval()
		{
			return PlatformSdkFunction.GetInstance().ScriptContinusExecuteInterval;
		}

		// Token: 0x0600011C RID: 284 RVA: 0x000089C4 File Offset: 0x00006BC4
		public object ExecuteMethod(string methodName, object[] args, ref bool bReturn)
		{
			bReturn = false;
			bool flag = this._objectClass == null;
			object result;
			if (flag)
			{
				bReturn = false;
				result = null;
			}
			else
			{
				try
				{
					bReturn = true;
					result = this._objectClass.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, this._objectClass, args);
				}
				catch (Exception ex)
				{
					bReturn = false;
					result = null;
				}
			}
			return result;
		}

		// Token: 0x0600011D RID: 285 RVA: 0x00008A2C File Offset: 0x00006C2C
		public bool ExecuteProcessMethod(ref int errorCode)
		{
			errorCode = 0;
			bool flag = this._objectClass == null || this._objAssembly == null;
			bool result;
			if (flag)
			{
				errorCode = -536870911;
				result = false;
			}
			else
			{
				try
				{
					bool flag2 = this._iProcessClass != null;
					if (flag2)
					{
						errorCode = this._iProcessClass.Process();
					}
					else
					{
						object obj = this._objectClass.GetType().InvokeMember("Process", BindingFlags.InvokeMethod, null, this._objectClass, null);
						bool flag3 = obj != null;
						if (flag3)
						{
							int.TryParse(obj.ToString(), out errorCode);
						}
					}
					bool flag4 = (long)errorCode != 0L;
					if (flag4)
					{
						result = false;
					}
					else
					{
						result = true;
					}
				}
				catch (Exception ex)
				{
					errorCode = -536870657;
					LogHelper.Error("process is exception:" + ex.ToString());
					Debugger.Log(0, null, "process is exception:" + ex.ToString());
					result = false;
				}
			}
			return result;
		}

		// Token: 0x04000111 RID: 273
		private Type _Type = null;

		// Token: 0x04000112 RID: 274
		private Assembly _objAssembly = null;

		// Token: 0x04000113 RID: 275
		private object _objectClass = null;

		// Token: 0x04000114 RID: 276
		private IScriptMethods _iProcessClass = null;

		// Token: 0x04000115 RID: 277
		private const string OUT_ASSEMBLY_NAME = "GlobalUserScript.dll";

		// Token: 0x04000116 RID: 278
		private const string OUT_ASSEMBLY_PDB_NAME = "GlobalUserScript.pdb";

		// Token: 0x04000117 RID: 279
		private CSharpCodeProvider objCSharpCodePrivoder = new CSharpCodeProvider();

		// Token: 0x04000118 RID: 280
		private string ApplicationPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
	}
}
