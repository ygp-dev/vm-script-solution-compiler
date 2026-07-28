using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Script.Algorithm
{
	// Token: 0x02000003 RID: 3
	public class ComAlgorithm : IAlgorithm
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000027 RID: 39 RVA: 0x000020D0 File Offset: 0x000002D0
		// (set) Token: 0x06000028 RID: 40 RVA: 0x000020E7 File Offset: 0x000002E7
		public int m_nModuleID { get; set; }

		// Token: 0x06000029 RID: 41 RVA: 0x000020F0 File Offset: 0x000002F0
		public void SetInOutputHandle(long input, long output)
		{
			this.InputPtr = (IntPtr)input;
			this.OutputPtr = (IntPtr)output;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x0000210B File Offset: 0x0000030B
		public void Dispose()
		{
			this.ClearMemory();
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002118 File Offset: 0x00000318
		public void ClearMemory()
		{
			this.objImageDict.Clear();
			this.objRoiBoxDict.Clear();
			this.objAnnulusDict.Clear();
			this.objPolygonDict.Clear();
			this.objPointDict.Clear();
			this.objLineDict.Clear();
			this.objFixtureDict.Clear();
			this.objCircleDict.Clear();
			this.objRectDict.Clear();
			this.objEllipseDict.Clear();
			this.objImageOutUseCount.Clear();
			this.objMemoryCfg = null;
			foreach (KeyValuePair<string, IntptrInfo> keyValuePair in this.dictImagePtr)
			{
				if (keyValuePair.Value.dataInptr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(keyValuePair.Value.dataInptr);
					keyValuePair.Value.dataInptr = IntPtr.Zero;
				}
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000223C File Offset: 0x0000043C
		public void SetMemoryCfgObj(SharedMemoryCfg memoryCfg)
		{
			this.objMemoryCfg = memoryCfg;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002248 File Offset: 0x00000448
		public void SetData(string key, object obj)
		{
			if (key == "image")
			{
				if (obj != null)
				{
					this.objImageDict = new Dictionary<string, ImageIoName>((Dictionary<string, ImageIoName>)obj);
				}
				this.objImageOutUseCount.Clear();
			}
			else if (key == "roibox")
			{
				if (obj != null)
				{
					this.objRoiBoxDict = new Dictionary<string, RoiBoxIoName>((Dictionary<string, RoiBoxIoName>)obj);
				}
			}
			else if (key == "roiannulus")
			{
				if (obj != null)
				{
					this.objAnnulusDict = new Dictionary<string, AnnulusIoName>((Dictionary<string, AnnulusIoName>)obj);
				}
			}
			else if (key == "roipolygon")
			{
				if (obj != null)
				{
					this.objPolygonDict = new Dictionary<string, PolygonIoName>((Dictionary<string, PolygonIoName>)obj);
				}
			}
			else if (key == "point")
			{
				if (obj != null)
				{
					this.objPointDict = new Dictionary<string, PointIoName>((Dictionary<string, PointIoName>)obj);
				}
			}
			else if (key == "line")
			{
				if (obj != null)
				{
					this.objLineDict = new Dictionary<string, LineIoName>((Dictionary<string, LineIoName>)obj);
				}
			}
			else if (key == "fixture")
			{
				if (obj != null)
				{
					this.objFixtureDict = new Dictionary<string, FixtureIoName>((Dictionary<string, FixtureIoName>)obj);
				}
			}
			else if (key == "circle")
			{
				if (obj != null)
				{
					this.objCircleDict = new Dictionary<string, CircleIoName>((Dictionary<string, CircleIoName>)obj);
				}
			}
			else if (key == "rect")
			{
				if (obj != null)
				{
					this.objRectDict = new Dictionary<string, RectIoName>((Dictionary<string, RectIoName>)obj);
				}
			}
			else if (key == "ellipse")
			{
				if (obj != null)
				{
					this.objEllipseDict = new Dictionary<string, EllipseIoName>((Dictionary<string, EllipseIoName>)obj);
				}
			}
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002460 File Offset: 0x00000660
		public int GetObjectValue(string paramKey, int type, int index, ref object paramValue, ref int arrayCount, int moduleId = -1)
		{
			int num = 0;
			switch (type)
			{
			case 0:
			{
				int num2 = 0;
				num = ScriptNativeMethods.GetIntValue(this.InputPtr, paramKey, index, ref num2, ref arrayCount);
				if (num == 0)
				{
					paramValue = num2.ToString();
				}
				break;
			}
			case 1:
			{
				float num3 = 0f;
				num = ScriptNativeMethods.GetFloatValue(this.InputPtr, paramKey, index, ref num3, ref arrayCount);
				if (num == 0)
				{
					paramValue = num3.ToString();
				}
				break;
			}
			case 2:
			{
				string text = "";
				num = ScriptNativeMethods.GetStringValue(this.InputPtr, paramKey, index, ref text, ref arrayCount);
				if (num == 0)
				{
					paramValue = text;
				}
				break;
			}
			case 3:
			{
				byte[] array = new byte[0];
				num = ScriptNativeMethods.GetBytesValue(this.InputPtr, paramKey, index, ref array, ref arrayCount);
				if (num == 0)
				{
					paramValue = array;
				}
				break;
			}
			case 4:
			{
				byte[] array2 = new byte[0];
				num = ScriptNativeMethods.GetImageValue(this.InputPtr, paramKey, index, ref array2, ref arrayCount);
				if (num == 0)
				{
					paramValue = array2;
				}
				break;
			}
			}
			return num;
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002594 File Offset: 0x00000794
		public int GetFloatArrayValue(string paramKey, ref float[] paramValue)
		{
			int index = 0;
			int num = 0;
			return ScriptNativeMethods.GetFloatArrayValue(this.InputPtr, paramKey, index, ref paramValue, ref num);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x000025BC File Offset: 0x000007BC
		public int GetIntArrayValue(string paramKey, ref int[] paramValue)
		{
			int index = 0;
			int num = 0;
			return ScriptNativeMethods.GetIntArrayValue(this.InputPtr, paramKey, index, ref paramValue, ref num);
		}

		// Token: 0x06000031 RID: 49 RVA: 0x000025E4 File Offset: 0x000007E4
		public int GetObjectArrayValue(string paramKey, int type, ref string[] paramValue, int moduleId = -1)
		{
			int num = 0;
			List<string> list = new List<string>();
			int num2 = 0;
			int num3 = 0;
			if (type == 2)
			{
				do
				{
					string item = "";
					num = ScriptNativeMethods.GetStringValue(this.InputPtr, paramKey, num2, ref item, ref num3);
					if (num != 0)
					{
						break;
					}
					list.Add(item);
					num2++;
				}
				while (num2 < num3);
				paramValue = list.ToArray();
			}
			return num;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002664 File Offset: 0x00000864
		public int SetBasicArrayValue(int type, string paramKey, object paramValue)
		{
			int num = 0;
			int result;
			if (paramValue == null)
			{
				result = -536870911;
			}
			else
			{
				if (type == 0)
				{
					num = ScriptNativeMethods.SetIntArrayValue(this.OutputPtr, paramKey, (int[])paramValue);
				}
				else if (type == 1)
				{
					num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, paramKey, (float[])paramValue);
				}
				result = num;
			}
			return result;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x000026D0 File Offset: 0x000008D0
		public int SetObjectValue(int index, int type, string paramKey, object paramValue)
		{
			int num = 0;
			int result;
			if (string.IsNullOrEmpty(paramKey) || paramValue == null)
			{
				LogHelper.Error("param is error", 0);
				result = -536870911;
			}
			else
			{
				switch (type)
				{
				case 0:
					num = ScriptNativeMethods.SetIntValue(this.OutputPtr, paramKey, index, int.Parse((string)paramValue));
					break;
				case 1:
					num = ScriptNativeMethods.SetFloatValue(this.OutputPtr, paramKey, index, float.Parse((string)paramValue));
					break;
				case 2:
					num = ScriptNativeMethods.SetStringValue(this.OutputPtr, paramKey, index, (string)paramValue);
					break;
				case 3:
				{
					IntPtr intPtr = IntPtr.Zero;
					byte[] array = (byte[])paramValue;
					if (this.dictImagePtr.ContainsKey(paramKey))
					{
						IntptrInfo intptrInfo = this.dictImagePtr[paramKey];
						if (array.Length > intptrInfo.nSize)
						{
							if (intptrInfo.dataInptr != IntPtr.Zero)
							{
								Marshal.FreeHGlobal(intptrInfo.dataInptr);
								intptrInfo.dataInptr = IntPtr.Zero;
							}
							intptrInfo.dataInptr = Marshal.AllocHGlobal(array.Length);
							intptrInfo.nSize = array.Length;
						}
						intPtr = intptrInfo.dataInptr;
					}
					else
					{
						IntptrInfo intptrInfo2 = new IntptrInfo();
						intptrInfo2.dataInptr = Marshal.AllocHGlobal(array.Length);
						intptrInfo2.nSize = array.Length;
						this.dictImagePtr.Add(paramKey, intptrInfo2);
						intPtr = intptrInfo2.dataInptr;
					}
					Marshal.Copy(array, 0, intPtr, array.Length);
					num = ScriptNativeMethods.SetBytesValue(this.OutputPtr, paramKey, index, intPtr, array.Length);
					break;
				}
				case 4:
				{
					IntPtr intPtr = IntPtr.Zero;
					byte[] array = (byte[])paramValue;
					num = ScriptNativeMethods.SetImageValueEx(this.m_nModuleID, this.OutputPtr, paramKey, array, 0);
					break;
				}
				}
				result = num;
			}
			return result;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x000028B8 File Offset: 0x00000AB8
		public int SetImageBaseData(string paramKey, object paramValue, int nUseCount)
		{
			IntPtr zero = IntPtr.Zero;
			byte[] array = (byte[])paramValue;
			IntPtr zero2 = IntPtr.Zero;
			string shareNmae = "";
			int result;
			if (this.objMemoryCfg != null)
			{
				int num = this.objMemoryCfg.AllocateSharedMemory(this.m_nModuleID, (uint)array.Length, ref zero2, ref shareNmae, nUseCount);
				if (num == 0)
				{
					num = ScriptNativeMethods.SetImageValueOwnerMemory(this.m_nModuleID, this.OutputPtr, paramKey, array, zero2, shareNmae);
				}
				result = num;
			}
			else
			{
				LogHelper.Error("objMemoryCfg is null", 0);
				result = -536870910;
			}
			return result;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00002950 File Offset: 0x00000B50
		public int SetImageData(string paramKey, int type, byte[] imageBuffer, int nWidth, int nHeight, int nPxiFormat)
		{
			int result;
			if (!this.objImageDict.ContainsKey(paramKey) || imageBuffer == null || imageBuffer.Length <= 0)
			{
				result = -536870911;
			}
			else
			{
				if (!this.objImageOutUseCount.ContainsKey(paramKey))
				{
					this.objImageOutUseCount.Add(paramKey, this.objImageOutUseCount.Count);
				}
				int num = this.SetImageBaseData(this.objImageDict[paramKey].ImageDataName, imageBuffer, this.objImageOutUseCount[paramKey]);
				if (num != 0)
				{
					result = num;
				}
				else
				{
					num = ScriptNativeMethods.SetIntValue(this.OutputPtr, this.objImageDict[paramKey].WidthName, 0, nWidth);
					if (num != 0)
					{
						result = num;
					}
					else
					{
						num = ScriptNativeMethods.SetIntValue(this.OutputPtr, this.objImageDict[paramKey].HeightName, 0, nHeight);
						if (num != 0)
						{
							result = num;
						}
						else
						{
							num = ScriptNativeMethods.SetIntValue(this.OutputPtr, this.objImageDict[paramKey].FormatName, 0, nPxiFormat);
							if (num != 0)
							{
								result = num;
							}
							else
							{
								result = num;
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002A78 File Offset: 0x00000C78
		public int GetImageData(string paramKey, int type, ref byte[] imageBuffer, ref int nWidth, ref int nHeight, ref int nPxiFormat)
		{
			int result;
			if (!this.objImageDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				object obj = new object();
				int num2 = ScriptNativeMethods.GetImageValue(this.InputPtr, this.objImageDict[paramKey].ImageDataName, 0, ref imageBuffer, ref num);
				if (num2 != 0)
				{
					result = num2;
				}
				else
				{
					num2 = ScriptNativeMethods.GetIntValue(this.InputPtr, this.objImageDict[paramKey].WidthName, 0, ref nWidth, ref num);
					if (num2 != 0)
					{
						result = num2;
					}
					else
					{
						num2 = ScriptNativeMethods.GetIntValue(this.InputPtr, this.objImageDict[paramKey].HeightName, 0, ref nHeight, ref num);
						if (num2 != 0)
						{
							result = num2;
						}
						else
						{
							num2 = ScriptNativeMethods.GetIntValue(this.InputPtr, this.objImageDict[paramKey].FormatName, 0, ref nPxiFormat, ref num);
							if (num2 != 0)
							{
								result = num2;
							}
							else
							{
								result = 0;
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00002B7C File Offset: 0x00000D7C
		public int SetRoiBoxData(string paramKey, int type, int index, float fCenterX, float fCenterY, float fWidth, float fHeight, float fAngle)
		{
			int result;
			if (!this.objRoiBoxDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = ScriptNativeMethods.SetFloatValue(this.OutputPtr, this.objRoiBoxDict[paramKey].CenterXName, index, fCenterX);
				if (num != 0)
				{
					result = num;
				}
				else
				{
					num = ScriptNativeMethods.SetFloatValue(this.OutputPtr, this.objRoiBoxDict[paramKey].CenterYName, index, fCenterY);
					if (num != 0)
					{
						result = num;
					}
					else
					{
						num = ScriptNativeMethods.SetFloatValue(this.OutputPtr, this.objRoiBoxDict[paramKey].WidthName, index, fWidth);
						if (num != 0)
						{
							result = num;
						}
						else
						{
							num = ScriptNativeMethods.SetFloatValue(this.OutputPtr, this.objRoiBoxDict[paramKey].HeightName, index, fHeight);
							if (num != 0)
							{
								result = num;
							}
							else
							{
								num = ScriptNativeMethods.SetFloatValue(this.OutputPtr, this.objRoiBoxDict[paramKey].AngleName, index, fAngle);
								if (num != 0)
								{
									result = num;
								}
								else
								{
									result = num;
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002C94 File Offset: 0x00000E94
		public int GetRoiBoxData(string paramKey, int type, ref float fCenterX, ref float fCenterY, ref float fWidth, ref float fHeight, ref float fAngle)
		{
			int result;
			if (!this.objRoiBoxDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				object obj = new object();
				int floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRoiBoxDict[paramKey].CenterXName, 0, ref fCenterX, ref num);
				if (floatValue != 0)
				{
					result = floatValue;
				}
				else
				{
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRoiBoxDict[paramKey].CenterYName, 0, ref fCenterY, ref num);
					if (floatValue != 0)
					{
						result = floatValue;
					}
					else
					{
						floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRoiBoxDict[paramKey].WidthName, 0, ref fWidth, ref num);
						if (floatValue != 0)
						{
							result = floatValue;
						}
						else
						{
							floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRoiBoxDict[paramKey].HeightName, 0, ref fHeight, ref num);
							if (floatValue != 0)
							{
								result = floatValue;
							}
							else
							{
								floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRoiBoxDict[paramKey].AngleName, 0, ref fAngle, ref num);
								if (floatValue != 0)
								{
									result = floatValue;
								}
								else
								{
									result = 0;
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002DCC File Offset: 0x00000FCC
		public int SetObjectValueForModule(int ModuleID, string paramName, string paramValue, int valuetype)
		{
			int result;
			if (string.IsNullOrEmpty(paramName) || paramValue == null)
			{
				result = -536870911;
			}
			else
			{
				result = ScriptNativeMethods.SetParamValueForModule(this.m_nModuleID, ModuleID, paramName, paramValue, valuetype);
			}
			return result;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002E10 File Offset: 0x00001010
		public int GetModuleParamValue(int ModuleID, string paramName, ref string paramValue)
		{
			int result;
			if (string.IsNullOrEmpty(paramName))
			{
				result = -536870911;
			}
			else
			{
				result = ScriptNativeMethods.GetModuleParamValue(this.m_nModuleID, ModuleID, paramName, ref paramValue);
			}
			return result;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002E48 File Offset: 0x00001048
		public int GetObjectArrayValueForModule(int moduleId, int index, string paramKey, ref int nType, ref Array paramValue)
		{
			bool flag = index == 1;
			int num = 0;
			int num2 = 0;
			int byteValueForModule;
			do
			{
				byte[] array = null;
				byteValueForModule = ScriptNativeMethods.GetByteValueForModule(this.m_nModuleID, moduleId, paramKey, num2, ref array, ref num, ref nType);
				if (byteValueForModule != 0 || array == null || array.Length <= 0)
				{
					break;
				}
				switch (nType)
				{
				case 0:
				case 1:
				case 2:
					if (paramValue == null)
					{
						paramValue = new string[num];
					}
					paramValue.SetValue(Encoding.UTF8.GetString(array), num2);
					break;
				case 3:
				case 4:
					if (paramValue == null)
					{
						paramValue = new byte[num][];
					}
					paramValue.SetValue(array, num2);
					break;
				}
				num2++;
			}
			while (num2 < num && flag);
			return byteValueForModule;
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00002F34 File Offset: 0x00001134
		public int SetRoiBoxArrayData(string paramKey, RoiBoxArrayData roiBoxArray)
		{
			int result;
			if (!this.objRoiBoxDict.ContainsKey(paramKey) || roiBoxArray == null)
			{
				result = -536870911;
			}
			else
			{
				int num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objRoiBoxDict[paramKey].CenterXName, roiBoxArray.CenterXArray);
				if (num != 0)
				{
					result = num;
				}
				else
				{
					num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objRoiBoxDict[paramKey].CenterYName, roiBoxArray.CenterYArray);
					if (num != 0)
					{
						result = num;
					}
					else
					{
						num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objRoiBoxDict[paramKey].WidthName, roiBoxArray.WidthArray);
						if (num != 0)
						{
							result = num;
						}
						else
						{
							num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objRoiBoxDict[paramKey].HeightName, roiBoxArray.HeightArray);
							if (num != 0)
							{
								result = num;
							}
							else
							{
								num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objRoiBoxDict[paramKey].AngleName, roiBoxArray.AngleArray);
								if (num != 0)
								{
									result = num;
								}
								else
								{
									result = num;
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00003068 File Offset: 0x00001268
		public int GetRoiBoxArrayData(string paramKey, ref RoiBoxArrayData roiBoxArray)
		{
			int result;
			if (!this.objRoiBoxDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				int num2 = 0;
				float item = 0f;
				float item2 = 0f;
				float item3 = 0f;
				float item4 = 0f;
				float item5 = 0f;
				List<float> list = new List<float>();
				List<float> list2 = new List<float>();
				List<float> list3 = new List<float>();
				List<float> list4 = new List<float>();
				List<float> list5 = new List<float>();
				int floatValue;
				do
				{
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRoiBoxDict[paramKey].CenterXName, num, ref item, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRoiBoxDict[paramKey].CenterYName, num, ref item2, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRoiBoxDict[paramKey].WidthName, num, ref item3, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRoiBoxDict[paramKey].HeightName, num, ref item4, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRoiBoxDict[paramKey].AngleName, num, ref item5, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					num++;
					list.Add(item);
					list2.Add(item2);
					list3.Add(item3);
					list4.Add(item4);
					list5.Add(item5);
				}
				while (num < num2);
				if (roiBoxArray == null)
				{
					roiBoxArray = new RoiBoxArrayData();
				}
				roiBoxArray.Count = num2;
				roiBoxArray.CenterXArray = list.ToArray();
				roiBoxArray.CenterYArray = list2.ToArray();
				roiBoxArray.WidthArray = list3.ToArray();
				roiBoxArray.HeightArray = list4.ToArray();
				roiBoxArray.AngleArray = list5.ToArray();
				result = floatValue;
			}
			return result;
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00003284 File Offset: 0x00001484
		public int SetAnnulusArrayData(string paramKey, AnnulusArrayData annulusArray)
		{
			int result;
			if (!this.objAnnulusDict.ContainsKey(paramKey) || annulusArray == null)
			{
				result = -536870911;
			}
			else
			{
				int num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objAnnulusDict[paramKey].CenterXName, annulusArray.CenterXArray);
				if (num != 0)
				{
					result = num;
				}
				else
				{
					num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objAnnulusDict[paramKey].CenterYName, annulusArray.CenterYArray);
					if (num != 0)
					{
						result = num;
					}
					else
					{
						num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objAnnulusDict[paramKey].InnerRadiusName, annulusArray.InnerRadiusArray);
						if (num != 0)
						{
							result = num;
						}
						else
						{
							num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objAnnulusDict[paramKey].OuterRadiusName, annulusArray.OuterRadiusArray);
							if (num != 0)
							{
								result = num;
							}
							else
							{
								num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objAnnulusDict[paramKey].StartAngleName, annulusArray.StartAngleArray);
								if (num != 0)
								{
									result = num;
								}
								else
								{
									num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objAnnulusDict[paramKey].AngleExtendName, annulusArray.AngleExtendArray);
									if (num != 0)
									{
										result = num;
									}
									else
									{
										result = num;
									}
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000033EC File Offset: 0x000015EC
		public int GetAnnulusArrayData(string paramKey, ref AnnulusArrayData annulusArray)
		{
			int result;
			if (!this.objAnnulusDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				int num2 = 0;
				float item = 0f;
				float item2 = 0f;
				float item3 = 0f;
				float item4 = 0f;
				float item5 = 0f;
				float item6 = 0f;
				List<float> list = new List<float>();
				List<float> list2 = new List<float>();
				List<float> list3 = new List<float>();
				List<float> list4 = new List<float>();
				List<float> list5 = new List<float>();
				List<float> list6 = new List<float>();
				int floatValue;
				do
				{
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objAnnulusDict[paramKey].CenterXName, num, ref item, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objAnnulusDict[paramKey].CenterYName, num, ref item2, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objAnnulusDict[paramKey].InnerRadiusName, num, ref item3, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objAnnulusDict[paramKey].OuterRadiusName, num, ref item4, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objAnnulusDict[paramKey].StartAngleName, num, ref item5, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objAnnulusDict[paramKey].AngleExtendName, num, ref item6, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					num++;
					list.Add(item);
					list2.Add(item2);
					list3.Add(item3);
					list4.Add(item4);
					list5.Add(item5);
					list6.Add(item6);
				}
				while (num < num2);
				if (annulusArray == null)
				{
					annulusArray = new AnnulusArrayData();
				}
				annulusArray.Count = num2;
				annulusArray.CenterXArray = list.ToArray();
				annulusArray.CenterYArray = list2.ToArray();
				annulusArray.InnerRadiusArray = list3.ToArray();
				annulusArray.OuterRadiusArray = list4.ToArray();
				annulusArray.StartAngleArray = list5.ToArray();
				annulusArray.AngleExtendArray = list6.ToArray();
				result = floatValue;
			}
			return result;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00003660 File Offset: 0x00001860
		public int SetPolygonArrayData(string paramKey, PolygonArrayData polygonArray)
		{
			int result;
			if (!this.objPolygonDict.ContainsKey(paramKey) || polygonArray == null || polygonArray.PointNumArray == null || polygonArray.PointsXArray == null || polygonArray.PointsYArray == null)
			{
				result = -536870911;
			}
			else if (polygonArray.PointNumArray.Length != polygonArray.Count || polygonArray.PointsXArray.Length != polygonArray.Count || polygonArray.PointsYArray.Length != polygonArray.Count)
			{
				result = -536870911;
			}
			else
			{
				List<float> list = new List<float>();
				List<float> list2 = new List<float>();
				for (int i = 0; i < polygonArray.Count; i++)
				{
					if (polygonArray.PointsXArray[i].Length != polygonArray.PointNumArray[i] || polygonArray.PointsYArray[i].Length != polygonArray.PointNumArray[i])
					{
						return -536870911;
					}
					list.AddRange(polygonArray.PointsXArray[i]);
					list2.AddRange(polygonArray.PointsYArray[i]);
				}
				int num = ScriptNativeMethods.SetIntArrayValue(this.OutputPtr, this.objPolygonDict[paramKey].PointNumName, polygonArray.PointNumArray);
				if (num != 0)
				{
					result = num;
				}
				else
				{
					num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objPolygonDict[paramKey].PointsXName, list.ToArray());
					if (num != 0)
					{
						result = num;
					}
					else
					{
						num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objPolygonDict[paramKey].PointsYName, list2.ToArray());
						if (num != 0)
						{
							result = num;
						}
						else
						{
							result = num;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00003820 File Offset: 0x00001A20
		public int GetPolygonArrayData(string paramKey, ref PolygonArrayData polygonArray)
		{
			int result;
			if (!this.objPolygonDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int item = 0;
				float item2 = 0f;
				float item3 = 0f;
				List<int> list = new List<int>();
				List<float> list2 = new List<float>();
				List<float> list3 = new List<float>();
				int num4;
				do
				{
					num4 = ScriptNativeMethods.GetIntValue(this.InputPtr, this.objPolygonDict[paramKey].PointNumName, num, ref item, ref num2);
					if (num4 != 0)
					{
						break;
					}
					num++;
					list.Add(item);
				}
				while (num < num2);
				if (num4 != 0)
				{
					result = num4;
				}
				else if (num2 <= 0)
				{
					result = -536870888;
				}
				else
				{
					if (polygonArray == null)
					{
						polygonArray = new PolygonArrayData();
					}
					polygonArray.Count = num2;
					polygonArray.PointNumArray = list.ToArray();
					polygonArray.PointsXArray = new float[num2][];
					polygonArray.PointsYArray = new float[num2][];
					num = 0;
					num2 = 0;
					do
					{
						num4 = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objPolygonDict[paramKey].PointsXName, num, ref item2, ref num2);
						if (num4 != 0)
						{
							break;
						}
						num4 = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objPolygonDict[paramKey].PointsYName, num, ref item3, ref num2);
						if (num4 != 0)
						{
							break;
						}
						num++;
						list2.Add(item2);
						list3.Add(item3);
						if (list2.Count == polygonArray.PointNumArray[num3])
						{
							polygonArray.PointsXArray[num3] = list2.ToArray();
							polygonArray.PointsYArray[num3] = list3.ToArray();
							list2.Clear();
							list3.Clear();
							num3++;
						}
					}
					while (num < num2 && num3 < polygonArray.Count);
					result = num4;
				}
			}
			return result;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003A2C File Offset: 0x00001C2C
		public int SetPointArrayData(string paramKey, PointArrayData pointArray)
		{
			int result;
			if (!this.objPointDict.ContainsKey(paramKey) || pointArray == null)
			{
				result = -536870911;
			}
			else
			{
				int num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objPointDict[paramKey].PointXName, pointArray.PointXArray);
				if (num != 0)
				{
					result = num;
				}
				else
				{
					num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objPointDict[paramKey].PointYName, pointArray.PointYArray);
					if (num != 0)
					{
						result = num;
					}
					else
					{
						result = num;
					}
				}
			}
			return result;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003AC8 File Offset: 0x00001CC8
		public int GetPointArrayData(string paramKey, ref PointArrayData pointArray)
		{
			int result;
			if (!this.objPointDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				int num2 = 0;
				float item = 0f;
				float item2 = 0f;
				List<float> list = new List<float>();
				List<float> list2 = new List<float>();
				int floatValue;
				do
				{
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objPointDict[paramKey].PointXName, num, ref item, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objPointDict[paramKey].PointYName, num, ref item2, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					num++;
					list.Add(item);
					list2.Add(item2);
				}
				while (num < num2);
				if (pointArray == null)
				{
					pointArray = new PointArrayData();
				}
				pointArray.Count = num2;
				pointArray.PointXArray = list.ToArray();
				pointArray.PointYArray = list2.ToArray();
				result = floatValue;
			}
			return result;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003BDC File Offset: 0x00001DDC
		public int SetLineArrayData(string paramKey, LineArrayData lineArray)
		{
			int result;
			if (!this.objLineDict.ContainsKey(paramKey) || lineArray == null)
			{
				result = -536870911;
			}
			else
			{
				int num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objLineDict[paramKey].StartPointXName, lineArray.StartPointXArray);
				if (num != 0)
				{
					result = num;
				}
				else
				{
					num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objLineDict[paramKey].StartPointYName, lineArray.StartPointYArray);
					if (num != 0)
					{
						result = num;
					}
					else
					{
						num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objLineDict[paramKey].EndPointXName, lineArray.EndPointXArray);
						if (num != 0)
						{
							result = num;
						}
						else
						{
							num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objLineDict[paramKey].EndPointYName, lineArray.EndPointYArray);
							if (num != 0)
							{
								result = num;
							}
							else
							{
								result = num;
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003CE0 File Offset: 0x00001EE0
		public int GetLineArrayData(string paramKey, ref LineArrayData lineArray)
		{
			int result;
			if (!this.objLineDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				int num2 = 0;
				float item = 0f;
				float item2 = 0f;
				float item3 = 0f;
				float item4 = 0f;
				List<float> list = new List<float>();
				List<float> list2 = new List<float>();
				List<float> list3 = new List<float>();
				List<float> list4 = new List<float>();
				int floatValue;
				do
				{
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objLineDict[paramKey].StartPointXName, num, ref item, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objLineDict[paramKey].StartPointYName, num, ref item2, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objLineDict[paramKey].EndPointXName, num, ref item3, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objLineDict[paramKey].EndPointYName, num, ref item4, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					num++;
					list.Add(item);
					list2.Add(item2);
					list3.Add(item3);
					list4.Add(item4);
				}
				while (num < num2);
				if (lineArray == null)
				{
					lineArray = new LineArrayData();
				}
				lineArray.Count = num2;
				lineArray.StartPointXArray = list.ToArray();
				lineArray.StartPointYArray = list2.ToArray();
				lineArray.EndPointXArray = list3.ToArray();
				lineArray.EndPointYArray = list4.ToArray();
				result = floatValue;
			}
			return result;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003EA4 File Offset: 0x000020A4
		public int SetFixtureArrayData(string paramKey, FixtureArrayData fixtureArray)
		{
			int result;
			if (!this.objFixtureDict.ContainsKey(paramKey) || fixtureArray == null)
			{
				result = -536870911;
			}
			else
			{
				int num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objFixtureDict[paramKey].InitPointXName, fixtureArray.InitPointXArray);
				if (num != 0)
				{
					result = num;
				}
				else
				{
					num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objFixtureDict[paramKey].InitPointYName, fixtureArray.InitPointYArray);
					if (num != 0)
					{
						result = num;
					}
					else
					{
						num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objFixtureDict[paramKey].InitAngleName, fixtureArray.InitAngleArray);
						if (num != 0)
						{
							result = num;
						}
						else
						{
							num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objFixtureDict[paramKey].InitScaleXName, fixtureArray.InitScaleXArray);
							if (num != 0)
							{
								result = num;
							}
							else
							{
								num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objFixtureDict[paramKey].InitScaleYName, fixtureArray.InitScaleYArray);
								if (num != 0)
								{
									result = num;
								}
								else
								{
									num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objFixtureDict[paramKey].RunPointXName, fixtureArray.RunPointXArray);
									if (num != 0)
									{
										result = num;
									}
									else
									{
										num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objFixtureDict[paramKey].RunPointYName, fixtureArray.RunPointYArray);
										if (num != 0)
										{
											result = num;
										}
										else
										{
											num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objFixtureDict[paramKey].RunAngleName, fixtureArray.RunAngleArray);
											if (num != 0)
											{
												result = num;
											}
											else
											{
												num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objFixtureDict[paramKey].RunScaleXName, fixtureArray.RunScaleXArray);
												if (num != 0)
												{
													result = num;
												}
												else
												{
													num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objFixtureDict[paramKey].RunScaleYName, fixtureArray.RunScaleYArray);
													if (num != 0)
													{
														result = num;
													}
													else
													{
														result = num;
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x000040D8 File Offset: 0x000022D8
		public int GetFixtureArrayData(string paramKey, ref FixtureArrayData fixtureArray)
		{
			int result;
			if (!this.objFixtureDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				int num2 = 0;
				float item = 0f;
				float item2 = 0f;
				float item3 = 0f;
				float item4 = 0f;
				float item5 = 0f;
				float item6 = 0f;
				float item7 = 0f;
				float item8 = 0f;
				float item9 = 0f;
				float item10 = 0f;
				List<float> list = new List<float>();
				List<float> list2 = new List<float>();
				List<float> list3 = new List<float>();
				List<float> list4 = new List<float>();
				List<float> list5 = new List<float>();
				List<float> list6 = new List<float>();
				List<float> list7 = new List<float>();
				List<float> list8 = new List<float>();
				List<float> list9 = new List<float>();
				List<float> list10 = new List<float>();
				int floatValue;
				do
				{
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objFixtureDict[paramKey].InitPointXName, num, ref item, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objFixtureDict[paramKey].InitPointYName, num, ref item2, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objFixtureDict[paramKey].InitAngleName, num, ref item3, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objFixtureDict[paramKey].InitScaleXName, num, ref item4, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objFixtureDict[paramKey].InitScaleYName, num, ref item5, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objFixtureDict[paramKey].RunPointXName, num, ref item6, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objFixtureDict[paramKey].RunPointYName, num, ref item7, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objFixtureDict[paramKey].RunAngleName, num, ref item8, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objFixtureDict[paramKey].RunScaleXName, num, ref item9, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objFixtureDict[paramKey].RunScaleYName, num, ref item10, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					num++;
					list.Add(item);
					list2.Add(item2);
					list3.Add(item3);
					list4.Add(item4);
					list5.Add(item5);
					list6.Add(item6);
					list7.Add(item7);
					list8.Add(item8);
					list9.Add(item9);
					list10.Add(item10);
				}
				while (num < num2);
				if (fixtureArray == null)
				{
					fixtureArray = new FixtureArrayData();
				}
				fixtureArray.Count = num2;
				fixtureArray.InitPointXArray = list.ToArray();
				fixtureArray.InitPointYArray = list2.ToArray();
				fixtureArray.InitAngleArray = list3.ToArray();
				fixtureArray.InitScaleXArray = list4.ToArray();
				fixtureArray.InitScaleYArray = list5.ToArray();
				fixtureArray.RunPointXArray = list6.ToArray();
				fixtureArray.RunPointYArray = list7.ToArray();
				fixtureArray.RunAngleArray = list8.ToArray();
				fixtureArray.RunScaleXArray = list9.ToArray();
				fixtureArray.RunScaleYArray = list10.ToArray();
				result = floatValue;
			}
			return result;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x000044B0 File Offset: 0x000026B0
		public int SetCircleArrayData(string paramKey, CircleArrayData circleArray)
		{
			int result;
			if (!this.objCircleDict.ContainsKey(paramKey) || circleArray == null)
			{
				result = -536870911;
			}
			else
			{
				int num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objCircleDict[paramKey].RadiusName, circleArray.RadiusArray);
				if (num != 0)
				{
					result = num;
				}
				else
				{
					num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objCircleDict[paramKey].CenterXName, circleArray.CenterXArray);
					if (num != 0)
					{
						result = num;
					}
					else
					{
						num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objCircleDict[paramKey].CenterYName, circleArray.CenterYArray);
						if (num != 0)
						{
							result = num;
						}
						else
						{
							result = num;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00004580 File Offset: 0x00002780
		public int GetCircleArrayData(string paramKey, ref CircleArrayData circleArray)
		{
			int result;
			if (!this.objCircleDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				int num2 = 0;
				float item = 0f;
				float item2 = 0f;
				float item3 = 0f;
				List<float> list = new List<float>();
				List<float> list2 = new List<float>();
				List<float> list3 = new List<float>();
				int floatValue;
				do
				{
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objCircleDict[paramKey].RadiusName, num, ref item, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objCircleDict[paramKey].CenterXName, num, ref item2, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objCircleDict[paramKey].CenterYName, num, ref item3, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					num++;
					list.Add(item);
					list2.Add(item2);
					list3.Add(item3);
				}
				while (num < num2);
				if (circleArray == null)
				{
					circleArray = new CircleArrayData();
				}
				circleArray.Count = num2;
				circleArray.RadiusArray = list.ToArray();
				circleArray.CenterXArray = list2.ToArray();
				circleArray.CenterYArray = list3.ToArray();
				result = floatValue;
			}
			return result;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x000046EC File Offset: 0x000028EC
		public int SetRectArrayData(string paramKey, RectArrayData rectArray)
		{
			int result;
			if (!this.objRectDict.ContainsKey(paramKey) || rectArray == null)
			{
				result = -536870911;
			}
			else
			{
				int num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objRectDict[paramKey].CenterXName, rectArray.CenterXArray);
				if (num != 0)
				{
					result = num;
				}
				else
				{
					num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objRectDict[paramKey].CenterYName, rectArray.CenterYArray);
					if (num != 0)
					{
						result = num;
					}
					else
					{
						num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objRectDict[paramKey].WidthName, rectArray.WidthArray);
						if (num != 0)
						{
							result = num;
						}
						else
						{
							num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objRectDict[paramKey].HeightName, rectArray.HeightArray);
							if (num != 0)
							{
								result = num;
							}
							else
							{
								result = num;
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x000047F0 File Offset: 0x000029F0
		public int GetRectArrayData(string paramKey, ref RectArrayData rectArray)
		{
			int result;
			if (!this.objRectDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				int num2 = 0;
				float item = 0f;
				float item2 = 0f;
				float item3 = 0f;
				float item4 = 0f;
				List<float> list = new List<float>();
				List<float> list2 = new List<float>();
				List<float> list3 = new List<float>();
				List<float> list4 = new List<float>();
				int floatValue;
				do
				{
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRectDict[paramKey].CenterXName, num, ref item, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRectDict[paramKey].CenterYName, num, ref item2, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRectDict[paramKey].WidthName, num, ref item3, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objRectDict[paramKey].HeightName, num, ref item4, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					num++;
					list.Add(item);
					list2.Add(item2);
					list3.Add(item3);
					list4.Add(item4);
				}
				while (num < num2);
				if (rectArray == null)
				{
					rectArray = new RectArrayData();
				}
				rectArray.Count = num2;
				rectArray.CenterXArray = list.ToArray();
				rectArray.CenterYArray = list2.ToArray();
				rectArray.WidthArray = list3.ToArray();
				rectArray.HeightArray = list4.ToArray();
				result = floatValue;
			}
			return result;
		}

		// Token: 0x0600004C RID: 76 RVA: 0x000049B4 File Offset: 0x00002BB4
		public int SetEllipseArrayData(string paramKey, EllipseArrayData ellipseArray)
		{
			int result;
			if (!this.objEllipseDict.ContainsKey(paramKey) || ellipseArray == null)
			{
				result = -536870911;
			}
			else
			{
				int num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objEllipseDict[paramKey].CenterXName, ellipseArray.CenterXArray);
				if (num != 0)
				{
					result = num;
				}
				else
				{
					num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objEllipseDict[paramKey].CenterYName, ellipseArray.CenterYArray);
					if (num != 0)
					{
						result = num;
					}
					else
					{
						num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objEllipseDict[paramKey].MajorRadiusName, ellipseArray.MajorRadiusArray);
						if (num != 0)
						{
							result = num;
						}
						else
						{
							num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objEllipseDict[paramKey].MinorRadiusName, ellipseArray.MinorRadiusArray);
							if (num != 0)
							{
								result = num;
							}
							else
							{
								num = ScriptNativeMethods.SetFloatArrayValue(this.OutputPtr, this.objEllipseDict[paramKey].AngleName, ellipseArray.AngleArray);
								if (num != 0)
								{
									result = num;
								}
								else
								{
									result = num;
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00004AE8 File Offset: 0x00002CE8
		public int GetEllipseArrayData(string paramKey, ref EllipseArrayData ellipseArray)
		{
			int result;
			if (!this.objEllipseDict.ContainsKey(paramKey))
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				int num2 = 0;
				float item = 0f;
				float item2 = 0f;
				float item3 = 0f;
				float item4 = 0f;
				float item5 = 0f;
				List<float> list = new List<float>();
				List<float> list2 = new List<float>();
				List<float> list3 = new List<float>();
				List<float> list4 = new List<float>();
				List<float> list5 = new List<float>();
				int floatValue;
				do
				{
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objEllipseDict[paramKey].CenterXName, num, ref item, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objEllipseDict[paramKey].CenterYName, num, ref item2, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objEllipseDict[paramKey].MajorRadiusName, num, ref item3, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objEllipseDict[paramKey].MinorRadiusName, num, ref item4, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					floatValue = ScriptNativeMethods.GetFloatValue(this.InputPtr, this.objEllipseDict[paramKey].AngleName, num, ref item5, ref num2);
					if (floatValue != 0)
					{
						break;
					}
					num++;
					list.Add(item);
					list2.Add(item2);
					list3.Add(item3);
					list4.Add(item4);
					list5.Add(item5);
				}
				while (num < num2);
				if (ellipseArray == null)
				{
					ellipseArray = new EllipseArrayData();
				}
				ellipseArray.Count = num2;
				ellipseArray.CenterXArray = list.ToArray();
				ellipseArray.CenterYArray = list2.ToArray();
				ellipseArray.MajorRadiusArray = list3.ToArray();
				ellipseArray.MinorRadiusArray = list4.ToArray();
				ellipseArray.AngleArray = list5.ToArray();
				result = floatValue;
			}
			return result;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00004D04 File Offset: 0x00002F04
		public int SetPointsetData(string paramKey, byte[] arrayValue)
		{
			return ScriptNativeMethods.SetPointsetValue(this.OutputPtr, paramKey, arrayValue);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00004D24 File Offset: 0x00002F24
		public int GetPointsetData(string paramKey, ref byte[] arrayValue)
		{
			return ScriptNativeMethods.GetPointsetValue(this.InputPtr, paramKey, ref arrayValue);
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00004D44 File Offset: 0x00002F44
		public int GetLocalVarModuleID(ref int nVarID)
		{
			return ScriptNativeMethods.GetLocalVarIdByID(this.m_nModuleID, ref nVarID);
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000051 RID: 81 RVA: 0x00004D64 File Offset: 0x00002F64
		public int ModuleID
		{
			get
			{
				return this.m_nModuleID;
			}
		}

		// Token: 0x04000001 RID: 1
		private IntPtr InputPtr = IntPtr.Zero;

		// Token: 0x04000002 RID: 2
		private IntPtr OutputPtr = IntPtr.Zero;

		// Token: 0x04000003 RID: 3
		private SharedMemoryCfg objMemoryCfg = null;

		// Token: 0x04000004 RID: 4
		private Dictionary<string, ImageIoName> objImageDict = new Dictionary<string, ImageIoName>();

		// Token: 0x04000005 RID: 5
		private Dictionary<string, RoiBoxIoName> objRoiBoxDict = new Dictionary<string, RoiBoxIoName>();

		// Token: 0x04000006 RID: 6
		private Dictionary<string, AnnulusIoName> objAnnulusDict = new Dictionary<string, AnnulusIoName>();

		// Token: 0x04000007 RID: 7
		private Dictionary<string, PolygonIoName> objPolygonDict = new Dictionary<string, PolygonIoName>();

		// Token: 0x04000008 RID: 8
		private Dictionary<string, PointIoName> objPointDict = new Dictionary<string, PointIoName>();

		// Token: 0x04000009 RID: 9
		private Dictionary<string, LineIoName> objLineDict = new Dictionary<string, LineIoName>();

		// Token: 0x0400000A RID: 10
		private Dictionary<string, FixtureIoName> objFixtureDict = new Dictionary<string, FixtureIoName>();

		// Token: 0x0400000B RID: 11
		private Dictionary<string, CircleIoName> objCircleDict = new Dictionary<string, CircleIoName>();

		// Token: 0x0400000C RID: 12
		private Dictionary<string, RectIoName> objRectDict = new Dictionary<string, RectIoName>();

		// Token: 0x0400000D RID: 13
		private Dictionary<string, EllipseIoName> objEllipseDict = new Dictionary<string, EllipseIoName>();

		// Token: 0x0400000E RID: 14
		private Dictionary<string, int> objImageOutUseCount = new Dictionary<string, int>();

		// Token: 0x0400000F RID: 15
		private Dictionary<string, IntptrInfo> dictImagePtr = new Dictionary<string, IntptrInfo>();
	}
}
