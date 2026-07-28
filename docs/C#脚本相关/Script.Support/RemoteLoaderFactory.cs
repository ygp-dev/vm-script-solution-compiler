using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Script.Algorithm;
using Script.Methods;

namespace Script.Support
{
	// Token: 0x02000009 RID: 9
	public class RemoteLoaderFactory : MarshalByRefObject
	{
		// Token: 0x06000043 RID: 67 RVA: 0x000036B8 File Offset: 0x000018B8
		public void LoadAssembly(string assemblyFile, string typeName)
		{
			try
			{
				this.objAssembly = Assembly.LoadFrom(assemblyFile);
				this.objType = this.objAssembly.GetType(typeName);
				if (!(this.objType == null))
				{
					this.objectClass = Activator.CreateInstance(this.objType);
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		// Token: 0x06000044 RID: 68 RVA: 0x0000371C File Offset: 0x0000191C
		public void Unload()
		{
			this.ExecuteDispose();
			this.objiProcessClass = null;
			this.objectClass = null;
			this.objAssembly = null;
			if (this.myAlgorithm != null)
			{
				this.myAlgorithm.ReleaseMapFileHandle();
				this.myAlgorithm = null;
			}
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003754 File Offset: 0x00001954
		public bool CreateInstanceFromByte(string assemblyFile, string typeName, object[] constructArgs, Assembly scriptAssembly = null)
		{
			bool result;
			try
			{
				if (!File.Exists(assemblyFile) && scriptAssembly == null)
				{
					LogHelper.Error("CreateInstance " + assemblyFile + " is not find", 0);
					result = false;
				}
				else
				{
					if (this.objType == null)
					{
						if (scriptAssembly == null)
						{
							byte[] array = null;
							using (FileStream fileStream = new FileStream(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
							{
								int num = (int)fileStream.Length;
								array = new byte[num];
								fileStream.Read(array, 0, array.Length);
							}
							if (array != null)
							{
								this.objAssembly = Assembly.Load(array);
							}
						}
						else
						{
							this.objAssembly = scriptAssembly;
						}
						if (this.objAssembly == null)
						{
							return false;
						}
						Type[] types = this.objAssembly.GetTypes();
						if (types == null)
						{
							return false;
						}
						Type left = null;
						foreach (Type type in types)
						{
							if (!(type == null))
							{
								if (type.FullName == typeName)
								{
									left = type;
									break;
								}
								if (type.BaseType != null && type.BaseType.FullName == typeName)
								{
									left = type;
									break;
								}
								if (type.GetInterface(typeName) != null)
								{
									left = type;
									break;
								}
							}
						}
						if (left == null)
						{
							return false;
						}
						this.objType = left;
					}
					this.objectClass = Activator.CreateInstance(this.objType);
					result = (this.objectClass != null);
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("CreateInstance " + ex.ToString(), 0);
				result = false;
			}
			return result;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003930 File Offset: 0x00001B30
		public bool CreateInstance(string assemblyFile, string typeName, object[] constructArgs)
		{
			bool result;
			try
			{
				if (this.objType == null)
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(assemblyFile);
					if (string.IsNullOrEmpty(fileNameWithoutExtension))
					{
						return false;
					}
					AppDomain.CurrentDomain.Load(fileNameWithoutExtension);
					Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
					if (assemblies == null)
					{
						return false;
					}
					foreach (Assembly assembly in assemblies)
					{
						if (Path.GetFileName(assembly.Location).Equals(assemblyFile, StringComparison.CurrentCultureIgnoreCase))
						{
							this.objAssembly = assembly;
						}
					}
					if (this.objAssembly == null)
					{
						return false;
					}
					Type[] types = this.objAssembly.GetTypes();
					if (types == null)
					{
						return false;
					}
					Type left = null;
					foreach (Type type in types)
					{
						if (!(type == null))
						{
							if (type.FullName == typeName)
							{
								left = type;
								break;
							}
							if (type.BaseType != null && type.BaseType.FullName == typeName)
							{
								left = type;
								break;
							}
							if (type.GetInterface(typeName) != null)
							{
								left = type;
								break;
							}
						}
					}
					if (left == null)
					{
						return false;
					}
					this.objType = left;
				}
				this.objectClass = Activator.CreateInstance(this.objType);
				if (this.objectClass != null)
				{
					result = true;
				}
				else
				{
					result = false;
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("CreateInstance " + ex.ToString(), 0);
				result = false;
			}
			return result;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00003AE0 File Offset: 0x00001CE0
		public bool CheckClass(bool bold, out ArrayList myResut)
		{
			myResut = new ArrayList();
			try
			{
				if (this.objectClass == null)
				{
					myResut.Add(new HikCompileMessage("LanguageCompileCreateObjectError UserScript", 0, 0, false));
					this.objAssembly = null;
					this.objectClass = null;
					return false;
				}
				if (null == this.objectClass.GetType().GetMethod("Init"))
				{
					myResut.Add(new HikCompileMessage("LanguageCompileMissFunctionError Init()", 0, 0, false));
					this.objAssembly = null;
					this.objectClass = null;
					return false;
				}
				if (null == this.objectClass.GetType().GetMethod("Process"))
				{
					myResut.Add(new HikCompileMessage("LanguageCompileMissFunctionError Process()", 0, 0, false));
					this.objAssembly = null;
					this.objectClass = null;
					return false;
				}
				if (!bold)
				{
					this.objiProcessClass = (IProcessMethods)this.objectClass;
				}
				else
				{
					this.objiProcessClass = null;
				}
				if (this.objiProcessClass != null)
				{
					this.objiProcessClass.Init();
				}
				else
				{
					this.objectClass.GetType().InvokeMember("Init", BindingFlags.InvokeMethod, null, this.objectClass, null);
				}
			}
			catch (Exception ex)
			{
				myResut.Add(new HikCompileMessage("LanguageCompileExecuteError Init()，" + ex.Message, 0, 0, false));
				this.objAssembly = null;
				this.objectClass = null;
				return false;
			}
			return true;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00003C60 File Offset: 0x00001E60
		public void SetObjData()
		{
			if (this.myAlgorithm == null)
			{
				this.myAlgorithm = (MyAlgorithm)AppDomain.CurrentDomain.GetData("MyAlgorithm");
				(this.objectClass as ISetData).SetAlgorithm(this.myAlgorithm);
			}
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00003C9A File Offset: 0x00001E9A
		public override object InitializeLifetimeService()
		{
			return null;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003C9D File Offset: 0x00001E9D
		public void SetAlgorithm(IAlgorithm objAlgorithm)
		{
			(this.objectClass as ISetData).SetAlgorithm(objAlgorithm);
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00003CB0 File Offset: 0x00001EB0
		public void SetAlgorithmData(string key, object objAlgorithm)
		{
			(this.objectClass as ISetData).SetAlgorithmData(key, objAlgorithm);
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003CC4 File Offset: 0x00001EC4
		public Assembly GetAssembly()
		{
			return this.objAssembly;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00003CCC File Offset: 0x00001ECC
		public int UpdateCode()
		{
			if (this.myAlgorithm != null)
			{
				return this.myAlgorithm.UpdateScriptCode();
			}
			return 0;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003CE4 File Offset: 0x00001EE4
		public bool ExecuteProcessMethod(long input = 0L, long output = 0L, int moduleid = 0)
		{
			if (this.objectClass == null)
			{
				LogHelper.Error("ExecuteProcessMethod error: objectClass is null", moduleid);
				return false;
			}
			try
			{
				(this.objectClass as ISetData).SetHandle(input, output);
				(this.objectClass as ISetData).Clear();
				if (this.objiProcessClass != null)
				{
					this.objiProcessClass.Process();
				}
				else
				{
					this.objectClass.GetType().InvokeMember("Process", BindingFlags.InvokeMethod, null, this.objectClass, null);
				}
			}
			catch (Exception ex)
			{
				Debugger.Log(0, null, "ScriptCom:ExecuteProcessMethod " + ex.ToString());
				LogHelper.Error("ScriptCom:ExecuteProcessMethod " + ex.ToString(), moduleid);
				return false;
			}
			return true;
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003DAC File Offset: 0x00001FAC
		private void ExecuteDispose()
		{
			if (this.objectClass == null)
			{
				return;
			}
			try
			{
				this.objectClass.GetType().InvokeMember("Dispose", BindingFlags.InvokeMethod, null, this.objectClass, null);
			}
			catch (Exception ex)
			{
				Debugger.Log(0, null, "ScriptCom:ExecuteDispose exception" + ex.ToString());
				LogHelper.Error("ScriptCom:ExecuteDispose exception " + ex.ToString(), 0);
			}
		}

		// Token: 0x04000020 RID: 32
		private const BindingFlags bfi = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

		// Token: 0x04000021 RID: 33
		private Type objType;

		// Token: 0x04000022 RID: 34
		private Assembly objAssembly;

		// Token: 0x04000023 RID: 35
		private object objectClass;

		// Token: 0x04000024 RID: 36
		private IProcessMethods objiProcessClass;

		// Token: 0x04000025 RID: 37
		private MyAlgorithm myAlgorithm;
	}
}
