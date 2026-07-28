using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Apps.XmlParser.Variable;

namespace Script.Algorithm
{
	// Token: 0x02000041 RID: 65
	public class VarAlgorithm
	{
		// Token: 0x17000067 RID: 103
		// (get) Token: 0x06000244 RID: 580 RVA: 0x0000E490 File Offset: 0x0000C690
		// (set) Token: 0x06000245 RID: 581 RVA: 0x0000E4A7 File Offset: 0x0000C6A7
		public int nVarModuleID { get; set; }

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x06000246 RID: 582 RVA: 0x0000E4B0 File Offset: 0x0000C6B0
		// (set) Token: 0x06000247 RID: 583 RVA: 0x0000E4C7 File Offset: 0x0000C6C7
		public int nShellModuleID { get; set; }

		// Token: 0x06000248 RID: 584 RVA: 0x0000E4FC File Offset: 0x0000C6FC
		private Dictionary<string, VariableManager.IO_VALUE_TYPE> GetVarIOInfo(string varName)
		{
			VariableManager.IMVS_SUB_VALUE[] array = null;
			Dictionary<string, VariableManager.IO_VALUE_TYPE> result;
			if (ScriptNativeMethods.GetVarSubIOInfo(this.nVarModuleID, varName, ref array) == 0 && array != null)
			{
				Dictionary<string, VariableManager.IO_VALUE_TYPE> dictTmp = new Dictionary<string, VariableManager.IO_VALUE_TYPE>();
				Array.ForEach<VariableManager.IMVS_SUB_VALUE>(array, delegate(VariableManager.IMVS_SUB_VALUE x)
				{
					dictTmp.Add(DataUtil.UTF8GetString(x.chszValueName), x.emValueType);
				});
				result = dictTmp;
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06000249 RID: 585 RVA: 0x0000E560 File Offset: 0x0000C760
		public int SetVarValueString(string varName, string varValue)
		{
			int result;
			if (string.IsNullOrEmpty(varName) || varValue == null)
			{
				result = -536870911;
			}
			else
			{
				result = ScriptNativeMethods.SetVarValueWithNoType(this.nVarModuleID, varName, varValue, -1);
			}
			return result;
		}

		// Token: 0x0600024A RID: 586 RVA: 0x0000E5A0 File Offset: 0x0000C7A0
		public int GetVarValueString(string varName, ref string varValue)
		{
			int result;
			if (string.IsNullOrEmpty(varName))
			{
				result = -536870911;
			}
			else
			{
				result = ScriptNativeMethods.GetVarValueWithNoType(this.nVarModuleID, varName, ref varValue, -1);
			}
			return result;
		}

		// Token: 0x0600024B RID: 587 RVA: 0x0000E5D8 File Offset: 0x0000C7D8
		public int SetVarInt(string varName, int[] intArray)
		{
			int result;
			if (intArray == null || intArray.Length <= 0)
			{
				result = -536870911;
			}
			else
			{
				int cb = intArray.Length * 4;
				int num = 0;
				IntPtr intPtr = Marshal.AllocHGlobal(cb);
				try
				{
					Marshal.Copy(intArray, 0, intPtr, intArray.Length);
					num = ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, intPtr, intArray.Length, -1);
				}
				catch (Exception ex)
				{
					ScriptSDK.Shell_Logger(0, 0, ex.Message);
				}
				finally
				{
					bool flag = 1 == 0;
					Marshal.FreeHGlobal(intPtr);
					intPtr = IntPtr.Zero;
				}
				result = num;
			}
			return result;
		}

		// Token: 0x0600024C RID: 588 RVA: 0x0000E688 File Offset: 0x0000C888
		public int SetVarFloat(string varName, float[] floatArray)
		{
			int result;
			if (floatArray == null || floatArray.Length <= 0)
			{
				result = -536870911;
			}
			else
			{
				int cb = floatArray.Length * 4;
				int num = 0;
				IntPtr intPtr = Marshal.AllocHGlobal(cb);
				try
				{
					Marshal.Copy(floatArray, 0, intPtr, floatArray.Length);
					num = ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, intPtr, floatArray.Length, -1);
				}
				catch (Exception ex)
				{
					ScriptSDK.Shell_Logger(0, 0, ex.Message);
				}
				finally
				{
					bool flag = 1 == 0;
					Marshal.FreeHGlobal(intPtr);
					intPtr = IntPtr.Zero;
				}
				result = num;
			}
			return result;
		}

		// Token: 0x0600024D RID: 589 RVA: 0x0000E738 File Offset: 0x0000C938
		public int SetVarString(string varName, string[] stringArray)
		{
			int result;
			if (stringArray == null || stringArray.Length <= 0)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				IntPtr intPtr = IntPtr.Zero;
				VariableManager.ByteDataArray[] array = new VariableManager.ByteDataArray[stringArray.Length];
				for (int i = 0; i < stringArray.Length; i++)
				{
					byte[] array2 = DataUtil.UTF8GetBytesPadZero(stringArray[i]);
					array[i].pData = Marshal.AllocHGlobal(array2.Length);
					array[i].nDataLen = (uint)array2.Length;
					Marshal.Copy(array2, 0, array[i].pData, array2.Length);
				}
				List<byte> list = new List<byte>();
				foreach (VariableManager.ByteDataArray byteDataArray in array)
				{
					list.AddRange(DataUtil.Structure2Bytes(byteDataArray));
				}
				intPtr = Marshal.AllocHGlobal(stringArray.Length * Marshal.SizeOf(typeof(VariableManager.ByteDataArray)));
				Marshal.Copy(list.ToArray(), 0, intPtr, list.Count);
				try
				{
					num = ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, intPtr, array.Length, -1);
				}
				catch (Exception ex)
				{
					ScriptSDK.Shell_Logger(0, 0, ex.Message);
				}
				finally
				{
					for (int i = 0; i < array.Length; i++)
					{
						Marshal.FreeHGlobal(array[i].pData);
						array[i].pData = IntPtr.Zero;
					}
					bool flag = 1 == 0;
					Marshal.FreeHGlobal(intPtr);
					intPtr = IntPtr.Zero;
				}
				result = num;
			}
			return result;
		}

		// Token: 0x0600024E RID: 590 RVA: 0x0000E8F4 File Offset: 0x0000CAF4
		public int SetVarByte(string varName, byte[] stBytesData)
		{
			int result;
			if (stBytesData == null || stBytesData.Length <= 0)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				IntPtr intPtr = IntPtr.Zero;
				intPtr = Marshal.AllocHGlobal(stBytesData.Length);
				Marshal.Copy(stBytesData, 0, intPtr, stBytesData.Length);
				try
				{
					num = ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, intPtr, stBytesData.Length, -1);
				}
				catch (Exception ex)
				{
					ScriptSDK.Shell_Logger(0, 0, ex.Message);
				}
				finally
				{
					bool flag = 1 == 0;
					Marshal.FreeHGlobal(intPtr);
					intPtr = IntPtr.Zero;
				}
				result = num;
			}
			return result;
		}

		// Token: 0x0600024F RID: 591 RVA: 0x0000EAB4 File Offset: 0x0000CCB4
		public int SetVarImage(string varName, byte[] imageBuffer, int nWidth, int nHeight, int nPxiFormat)
		{
			int result;
			if (imageBuffer == null || imageBuffer.Length <= 0)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
				if (varIOInfo == null)
				{
					num = -536870888;
					result = num;
				}
				else
				{
					IntPtr intPtr = IntPtr.Zero;
					try
					{
						List<int[]> list = new List<int[]>();
						list.Add(new int[]
						{
							nWidth
						});
						list.Add(new int[]
						{
							nHeight
						});
						list.Add(new int[]
						{
							nPxiFormat
						});
						Dictionary<string, VariableManager.IO_VALUE_TYPE> dictionary = (from x in varIOInfo
						where x.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_INT
						select x).ToDictionary((KeyValuePair<string, VariableManager.IO_VALUE_TYPE> k) => k.Key, (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> k) => k.Value);
						if (dictionary == null)
						{
							num = -536870888;
							return num;
						}
						num = VariableManager.AssemblySubIOValue<int>(list, dictionary, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, this.nShellModuleID));
						if (num != 0)
						{
							return num;
						}
						Dictionary<string, VariableManager.IO_VALUE_TYPE> dictionary2 = (from x in varIOInfo
						where x.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_IMAGE
						select x).ToDictionary((KeyValuePair<string, VariableManager.IO_VALUE_TYPE> k) => k.Key, (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> k) => k.Value);
						if (dictionary2 == null)
						{
							num = -536870888;
							return num;
						}
						intPtr = Marshal.AllocHGlobal(imageBuffer.Length);
						if (intPtr == IntPtr.Zero)
						{
							return -536870888;
						}
						Marshal.Copy(imageBuffer, 0, intPtr, imageBuffer.Length);
						num = VariableManager.AssemblySubIOValue<IntPtrData>(new List<IntPtrData[]>
						{
							new IntPtrData[]
							{
								new IntPtrData
								{
									ptrData = intPtr,
									nDataLen = (uint)imageBuffer.Length
								}
							}
						}, dictionary2, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, this.nShellModuleID));
					}
					catch (Exception ex)
					{
						num = -536870657;
						ScriptSDK.Shell_Logger(0, 0, ex.Message);
					}
					finally
					{
						if (intPtr != IntPtr.Zero)
						{
							Marshal.FreeHGlobal(intPtr);
							intPtr = IntPtr.Zero;
						}
					}
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000250 RID: 592 RVA: 0x0000EE0C File Offset: 0x0000D00C
		public int SetVarPoint(string varName, PointArrayData pointList)
		{
			int result;
			if (pointList == null)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
				if (varIOInfo == null)
				{
					num = -536870888;
					result = num;
				}
				else
				{
					try
					{
						num = VariableManager.AssemblySubIOValue<float>(new List<float[]>
						{
							pointList.PointXArray,
							pointList.PointYArray
						}, varIOInfo, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, -1));
						if (num != 0)
						{
							return num;
						}
					}
					catch (Exception ex)
					{
						num = -536870657;
						ScriptSDK.Shell_Logger(0, 0, ex.Message);
					}
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000251 RID: 593 RVA: 0x0000EF34 File Offset: 0x0000D134
		public int SetVarRoiBox(string varName, RoiBoxArrayData stRoiBox)
		{
			int result;
			if (stRoiBox == null)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
				if (varIOInfo == null)
				{
					num = -536870888;
					result = num;
				}
				else
				{
					try
					{
						num = VariableManager.AssemblySubIOValue<float>(new List<float[]>
						{
							stRoiBox.CenterXArray,
							stRoiBox.CenterYArray,
							stRoiBox.WidthArray,
							stRoiBox.HeightArray,
							stRoiBox.AngleArray
						}, varIOInfo, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, -1));
						if (num != 0)
						{
							return num;
						}
					}
					catch (Exception ex)
					{
						num = -536870657;
						ScriptSDK.Shell_Logger(0, 0, ex.Message);
					}
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000252 RID: 594 RVA: 0x0000F088 File Offset: 0x0000D288
		public int SetVarAnnulus(string varName, AnnulusArrayData stAnnulus)
		{
			int result;
			if (stAnnulus == null)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
				if (varIOInfo == null)
				{
					num = -536870888;
					result = num;
				}
				else
				{
					try
					{
						num = VariableManager.AssemblySubIOValue<float>(new List<float[]>
						{
							stAnnulus.CenterXArray,
							stAnnulus.CenterYArray,
							stAnnulus.InnerRadiusArray,
							stAnnulus.OuterRadiusArray,
							stAnnulus.StartAngleArray,
							stAnnulus.AngleExtendArray
						}, varIOInfo, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, -1));
						if (num != 0)
						{
							return num;
						}
					}
					catch (Exception ex)
					{
						num = -536870657;
						ScriptSDK.Shell_Logger(0, 0, ex.Message);
					}
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000253 RID: 595 RVA: 0x0000F1E8 File Offset: 0x0000D3E8
		public int SetVarCircle(string varName, CircleArrayData stCircle)
		{
			int result;
			if (stCircle == null)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
				if (varIOInfo == null)
				{
					num = -536870888;
					result = num;
				}
				else
				{
					try
					{
						num = VariableManager.AssemblySubIOValue<float>(new List<float[]>
						{
							stCircle.CenterXArray,
							stCircle.CenterYArray,
							stCircle.RadiusArray
						}, varIOInfo, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, -1));
						if (num != 0)
						{
							return num;
						}
					}
					catch (Exception ex)
					{
						num = -536870657;
						ScriptSDK.Shell_Logger(0, 0, ex.Message);
					}
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000254 RID: 596 RVA: 0x0000F320 File Offset: 0x0000D520
		public int SetVarEllipse(string varName, EllipseArrayData stEllipse)
		{
			int result;
			if (stEllipse == null)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
				if (varIOInfo == null)
				{
					num = -536870888;
					result = num;
				}
				else
				{
					try
					{
						num = VariableManager.AssemblySubIOValue<float>(new List<float[]>
						{
							stEllipse.CenterXArray,
							stEllipse.CenterYArray,
							stEllipse.MajorRadiusArray,
							stEllipse.MinorRadiusArray,
							stEllipse.AngleArray
						}, varIOInfo, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, -1));
						if (num != 0)
						{
							return num;
						}
					}
					catch (Exception ex)
					{
						num = -536870657;
						ScriptSDK.Shell_Logger(0, 0, ex.Message);
					}
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000255 RID: 597 RVA: 0x0000F474 File Offset: 0x0000D674
		public int SetVarLine(string varName, LineArrayData stLine)
		{
			int result;
			if (stLine == null)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
				if (varIOInfo == null)
				{
					num = -536870888;
					result = num;
				}
				else
				{
					try
					{
						num = VariableManager.AssemblySubIOValue<float>(new List<float[]>
						{
							stLine.StartPointXArray,
							stLine.StartPointYArray,
							stLine.EndPointXArray,
							stLine.EndPointYArray
						}, varIOInfo, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, -1));
						if (num != 0)
						{
							return num;
						}
					}
					catch (Exception ex)
					{
						num = -536870657;
						ScriptSDK.Shell_Logger(0, 0, ex.Message);
					}
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000256 RID: 598 RVA: 0x0000F5BC File Offset: 0x0000D7BC
		public int SetVarRect(string varName, RectArrayData stRectF)
		{
			int result;
			if (stRectF == null)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
				if (varIOInfo == null)
				{
					num = -536870888;
					result = num;
				}
				else
				{
					try
					{
						num = VariableManager.AssemblySubIOValue<float>(new List<float[]>
						{
							stRectF.CenterXArray,
							stRectF.CenterYArray,
							stRectF.WidthArray,
							stRectF.HeightArray
						}, varIOInfo, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, -1));
						if (num != 0)
						{
							return num;
						}
					}
					catch (Exception ex)
					{
						num = -536870657;
						ScriptSDK.Shell_Logger(0, 0, ex.Message);
					}
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000257 RID: 599 RVA: 0x0000F704 File Offset: 0x0000D904
		public int SetVarPointset(string varName, byte[] arrayValue)
		{
			int result;
			if (arrayValue == null || arrayValue.Length == 0)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
				if (varIOInfo == null)
				{
					num = -536870888;
					result = num;
				}
				else
				{
					GCHandle gchandle = GCHandle.Alloc(arrayValue, GCHandleType.Pinned);
					try
					{
						IntPtr ptrData2 = gchandle.AddrOfPinnedObject();
						num = VariableManager.AssemblySubIOValue<IntPtrData>(new List<IntPtrData[]>
						{
							new IntPtrData[]
							{
								new IntPtrData
								{
									ptrData = ptrData2,
									nDataLen = (uint)arrayValue.Length
								}
							}
						}, varIOInfo, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, -1));
					}
					catch (Exception ex)
					{
						num = -536870657;
						ScriptSDK.Shell_Logger(0, 0, ex.Message);
					}
					finally
					{
						if (gchandle.IsAllocated)
						{
							gchandle.Free();
						}
					}
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000258 RID: 600 RVA: 0x0000F880 File Offset: 0x0000DA80
		public int SetVarFixture(string varName, FixtureArrayData fixtureArray)
		{
			int result;
			if (fixtureArray == null)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
				if (varIOInfo == null)
				{
					num = -536870888;
					result = num;
				}
				else
				{
					try
					{
						num = VariableManager.AssemblySubIOValue<float>(new List<float[]>
						{
							fixtureArray.InitPointXArray,
							fixtureArray.InitPointYArray,
							fixtureArray.InitAngleArray,
							fixtureArray.InitScaleXArray,
							fixtureArray.InitScaleYArray,
							fixtureArray.RunPointXArray,
							fixtureArray.RunPointYArray,
							fixtureArray.RunAngleArray,
							fixtureArray.RunScaleXArray,
							fixtureArray.RunScaleYArray
						}, varIOInfo, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, -1));
						if (num != 0)
						{
							return num;
						}
					}
					catch (Exception ex)
					{
						num = -536870657;
						ScriptSDK.Shell_Logger(0, 0, ex.Message);
					}
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000259 RID: 601 RVA: 0x0000FADC File Offset: 0x0000DCDC
		public int SetVarPolygon(string varName, PolygonArrayData polygonArray)
		{
			int result;
			if (polygonArray == null)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				if (polygonArray.PointNumArray.Length != polygonArray.Count || polygonArray.PointsXArray.Length != polygonArray.Count || polygonArray.PointsYArray.Length != polygonArray.Count)
				{
					result = -536870911;
				}
				else
				{
					Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
					if (varIOInfo == null)
					{
						num = -536870888;
						result = num;
					}
					else
					{
						try
						{
							List<float[]> list = new List<float[]>();
							List<float> list2 = new List<float>();
							List<float> list3 = new List<float>();
							for (int i = 0; i < polygonArray.Count; i++)
							{
								if (polygonArray.PointsXArray[i].Length != polygonArray.PointNumArray[i] || polygonArray.PointsYArray[i].Length != polygonArray.PointNumArray[i])
								{
									return -536870911;
								}
								list2.AddRange(polygonArray.PointsXArray[i]);
								list3.AddRange(polygonArray.PointsYArray[i]);
							}
							list.Add(list2.ToArray());
							list.Add(list3.ToArray());
							Dictionary<string, VariableManager.IO_VALUE_TYPE> dictionary = (from x in varIOInfo
							where x.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_INT
							select x).ToDictionary((KeyValuePair<string, VariableManager.IO_VALUE_TYPE> k) => k.Key, (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> k) => k.Value);
							if (dictionary == null)
							{
								num = -536870888;
								return num;
							}
							num = VariableManager.AssemblySubIOValue<int>(new List<int[]>
							{
								polygonArray.PointNumArray
							}, dictionary, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, -1));
							Dictionary<string, VariableManager.IO_VALUE_TYPE> dictionary2 = (from x in varIOInfo
							where x.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_FLOAT
							select x).ToDictionary((KeyValuePair<string, VariableManager.IO_VALUE_TYPE> k) => k.Key, (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> k) => k.Value);
							if (dictionary2 == null)
							{
								num = -536870888;
								return num;
							}
							num = VariableManager.AssemblySubIOValue<float>(list, dictionary2, (IntPtr ptrData, int nlen) => ScriptNativeMethods.SetVarValue(this.nVarModuleID, varName, ptrData, nlen, -1));
							if (num != 0)
							{
								return num;
							}
						}
						catch (Exception ex)
						{
							num = -536870657;
							ScriptSDK.Shell_Logger(0, 0, ex.Message);
						}
						result = num;
					}
				}
			}
			return result;
		}

		// Token: 0x0600025A RID: 602 RVA: 0x0000FDF8 File Offset: 0x0000DFF8
		public int GetVarInt(string varName, ref int[] intList)
		{
			return ScriptNativeMethods.GetVarIntValue(this.nVarModuleID, this.nShellModuleID, varName, "", ref intList);
		}

		// Token: 0x0600025B RID: 603 RVA: 0x0000FE24 File Offset: 0x0000E024
		public int GetVarFloat(string varName, ref float[] floatList)
		{
			return ScriptNativeMethods.GetVarFloatValue(this.nVarModuleID, this.nShellModuleID, varName, "", ref floatList);
		}

		// Token: 0x0600025C RID: 604 RVA: 0x0000FE50 File Offset: 0x0000E050
		public int GetVarString(string varName, ref string[] stringList)
		{
			return ScriptNativeMethods.GetVarStringValue(this.nVarModuleID, this.nShellModuleID, varName, "", ref stringList);
		}

		// Token: 0x0600025D RID: 605 RVA: 0x0000FE7C File Offset: 0x0000E07C
		public int GetVarByte(string varName, ref byte[] stBytesData)
		{
			return ScriptNativeMethods.GetVarByteValue(this.nVarModuleID, this.nShellModuleID, varName, "", ref stBytesData);
		}

		// Token: 0x0600025E RID: 606 RVA: 0x0000FEA8 File Offset: 0x0000E0A8
		public int GetVarImage(string varName, ref byte[] imageBuffer, ref int nWidth, ref int nHeight, ref int nPxiFormat)
		{
			int num = 0;
			Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
			int result;
			if (varIOInfo == null)
			{
				num = -536870888;
				result = num;
			}
			else
			{
				foreach (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> keyValuePair in varIOInfo)
				{
					string key = keyValuePair.Key;
					if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_INT)
					{
						int[] array = null;
						num = ScriptNativeMethods.GetVarIntValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array);
						if (num != 0)
						{
							return num;
						}
						if (array != null && array.Length > 0)
						{
							if (key.ToLower().Contains("width"))
							{
								nWidth = array[0];
							}
							else if (key.ToLower().Contains("height"))
							{
								nHeight = array[0];
							}
							else if (key.ToLower().Contains("format"))
							{
								nPxiFormat = array[0];
							}
						}
					}
					else if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_IMAGE)
					{
						byte[] array2 = null;
						num = ScriptNativeMethods.GetVarByteValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array2);
						if (num != 0)
						{
							return num;
						}
						if (array2 == null || array2.Length == 0)
						{
							return -536870888;
						}
						imageBuffer = array2;
					}
				}
				if (num == 0)
				{
					if (nPxiFormat == 17301505)
					{
						return (nWidth * nHeight == imageBuffer.Length) ? 0 : -536870888;
					}
					if (nPxiFormat == 35127316)
					{
						return (nWidth * nHeight * 3 == imageBuffer.Length) ? 0 : -536870888;
					}
				}
				result = num;
			}
			return result;
		}

		// Token: 0x0600025F RID: 607 RVA: 0x000100E0 File Offset: 0x0000E2E0
		public int GetVarPoint(string varName, ref PointArrayData pointList)
		{
			int num = 0;
			Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
			int result;
			if (varIOInfo == null)
			{
				num = -536870888;
				result = num;
			}
			else
			{
				List<float[]> list = new List<float[]>();
				int num2 = 0;
				foreach (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> keyValuePair in varIOInfo)
				{
					string key = keyValuePair.Key;
					if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_FLOAT)
					{
						float[] array = null;
						num = ScriptNativeMethods.GetVarFloatValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array);
						if (num != 0)
						{
							return num;
						}
						if (array == null || array.Length <= 0)
						{
							return -536870888;
						}
						if (num2 > 0 && num2 != array.Length)
						{
							return -536870888;
						}
						num2 = array.Length;
						list.Add(array);
					}
				}
				if (list.Count < 2)
				{
					result = -536870888;
				}
				else
				{
					pointList = new PointArrayData();
					pointList.Count = num2;
					pointList.PointXArray = list[0];
					pointList.PointYArray = list[1];
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000260 RID: 608 RVA: 0x00010250 File Offset: 0x0000E450
		public int GetVarCircle(string varName, ref CircleArrayData stCircle)
		{
			int num = 0;
			Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
			int result;
			if (varIOInfo == null)
			{
				num = -536870888;
				result = num;
			}
			else
			{
				List<float[]> list = new List<float[]>();
				int num2 = 0;
				foreach (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> keyValuePair in varIOInfo)
				{
					string key = keyValuePair.Key;
					if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_FLOAT)
					{
						float[] array = null;
						num = ScriptNativeMethods.GetVarFloatValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array);
						if (num != 0)
						{
							return num;
						}
						if (array == null || array.Length <= 0)
						{
							return -536870888;
						}
						if (num2 > 0 && num2 != array.Length)
						{
							return -536870888;
						}
						num2 = array.Length;
						list.Add(array);
					}
				}
				if (list.Count < 3)
				{
					result = -536870888;
				}
				else
				{
					stCircle = new CircleArrayData();
					stCircle.Count = num2;
					stCircle.CenterXArray = list[0];
					stCircle.CenterYArray = list[1];
					stCircle.RadiusArray = list[2];
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000261 RID: 609 RVA: 0x000103D4 File Offset: 0x0000E5D4
		public int GetVarEllipse(string varName, ref EllipseArrayData stEllipse)
		{
			int num = 0;
			Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
			int result;
			if (varIOInfo == null)
			{
				num = -536870888;
				result = num;
			}
			else
			{
				List<float[]> list = new List<float[]>();
				int num2 = 0;
				foreach (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> keyValuePair in varIOInfo)
				{
					string key = keyValuePair.Key;
					if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_FLOAT)
					{
						float[] array = null;
						num = ScriptNativeMethods.GetVarFloatValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array);
						if (num != 0)
						{
							return num;
						}
						if (array == null || array.Length <= 0)
						{
							return -536870888;
						}
						if (num2 > 0 && num2 != array.Length)
						{
							return -536870888;
						}
						num2 = array.Length;
						list.Add(array);
					}
				}
				if (list.Count < 5)
				{
					result = -536870888;
				}
				else
				{
					stEllipse = new EllipseArrayData();
					stEllipse.Count = num2;
					stEllipse.CenterXArray = list[0];
					stEllipse.CenterYArray = list[1];
					stEllipse.MajorRadiusArray = list[2];
					stEllipse.MinorRadiusArray = list[3];
					stEllipse.AngleArray = list[4];
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000262 RID: 610 RVA: 0x00010574 File Offset: 0x0000E774
		public int GetVarLine(string varName, ref LineArrayData stLine)
		{
			int num = 0;
			Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
			int result;
			if (varIOInfo == null)
			{
				num = -536870888;
				result = num;
			}
			else
			{
				List<float[]> list = new List<float[]>();
				int num2 = 0;
				foreach (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> keyValuePair in varIOInfo)
				{
					string key = keyValuePair.Key;
					if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_FLOAT)
					{
						float[] array = null;
						num = ScriptNativeMethods.GetVarFloatValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array);
						if (num != 0)
						{
							return num;
						}
						if (array == null || array.Length <= 0)
						{
							return -536870888;
						}
						if (num2 > 0 && num2 != array.Length)
						{
							return -536870888;
						}
						num2 = array.Length;
						list.Add(array);
					}
				}
				if (list.Count < 4)
				{
					result = -536870888;
				}
				else
				{
					stLine = new LineArrayData();
					stLine.Count = num2;
					stLine.StartPointXArray = list[0];
					stLine.StartPointYArray = list[1];
					stLine.EndPointXArray = list[2];
					stLine.EndPointYArray = list[3];
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000263 RID: 611 RVA: 0x00010708 File Offset: 0x0000E908
		public int GetVarRoiBox(string varName, ref RoiBoxArrayData stRoiBox)
		{
			int num = 0;
			Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
			int result;
			if (varIOInfo == null)
			{
				num = -536870888;
				result = num;
			}
			else
			{
				List<float[]> list = new List<float[]>();
				int num2 = 0;
				foreach (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> keyValuePair in varIOInfo)
				{
					string key = keyValuePair.Key;
					if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_FLOAT)
					{
						float[] array = null;
						num = ScriptNativeMethods.GetVarFloatValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array);
						if (num != 0)
						{
							return num;
						}
						if (array == null || array.Length <= 0)
						{
							return -536870888;
						}
						if (num2 > 0 && num2 != array.Length)
						{
							return -536870888;
						}
						num2 = array.Length;
						list.Add(array);
					}
				}
				if (list.Count < 5)
				{
					result = -536870888;
				}
				else
				{
					stRoiBox = new RoiBoxArrayData();
					stRoiBox.Count = num2;
					stRoiBox.CenterXArray = list[0];
					stRoiBox.CenterYArray = list[1];
					stRoiBox.WidthArray = list[2];
					stRoiBox.HeightArray = list[3];
					stRoiBox.AngleArray = list[4];
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000264 RID: 612 RVA: 0x000108A8 File Offset: 0x0000EAA8
		public int GetVarRect(string varName, ref RectArrayData stRectF)
		{
			int num = 0;
			Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
			int result;
			if (varIOInfo == null)
			{
				num = -536870888;
				result = num;
			}
			else
			{
				List<float[]> list = new List<float[]>();
				int num2 = 0;
				foreach (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> keyValuePair in varIOInfo)
				{
					string key = keyValuePair.Key;
					if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_FLOAT)
					{
						float[] array = null;
						num = ScriptNativeMethods.GetVarFloatValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array);
						if (num != 0)
						{
							return num;
						}
						if (array == null || array.Length <= 0)
						{
							return -536870888;
						}
						if (num2 > 0 && num2 != array.Length)
						{
							return -536870888;
						}
						num2 = array.Length;
						list.Add(array);
					}
				}
				if (list.Count < 4)
				{
					result = -536870888;
				}
				else
				{
					stRectF = new RectArrayData();
					stRectF.Count = num2;
					stRectF.CenterXArray = list[0];
					stRectF.CenterYArray = list[1];
					stRectF.WidthArray = list[2];
					stRectF.HeightArray = list[3];
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000265 RID: 613 RVA: 0x00010A3C File Offset: 0x0000EC3C
		public int GetVarAnnulus(string varName, ref AnnulusArrayData stAnnulus)
		{
			int num = 0;
			Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
			int result;
			if (varIOInfo == null)
			{
				num = -536870888;
				result = num;
			}
			else
			{
				List<float[]> list = new List<float[]>();
				int num2 = 0;
				foreach (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> keyValuePair in varIOInfo)
				{
					string key = keyValuePair.Key;
					if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_FLOAT)
					{
						float[] array = null;
						num = ScriptNativeMethods.GetVarFloatValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array);
						if (num != 0)
						{
							return num;
						}
						if (array == null || array.Length <= 0)
						{
							return -536870888;
						}
						if (num2 > 0 && num2 != array.Length)
						{
							return -536870888;
						}
						num2 = array.Length;
						list.Add(array);
					}
				}
				if (list.Count < 6)
				{
					result = -536870888;
				}
				else
				{
					stAnnulus = new AnnulusArrayData();
					stAnnulus.Count = num2;
					stAnnulus.CenterXArray = list[0];
					stAnnulus.CenterYArray = list[1];
					stAnnulus.InnerRadiusArray = list[2];
					stAnnulus.OuterRadiusArray = list[3];
					stAnnulus.StartAngleArray = list[4];
					stAnnulus.AngleExtendArray = list[5];
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000266 RID: 614 RVA: 0x00010BEC File Offset: 0x0000EDEC
		public int GetVarPointset(string varName, ref byte[] arrayValue)
		{
			return ScriptNativeMethods.GetVarByteValue(this.nVarModuleID, this.nShellModuleID, varName, "", ref arrayValue);
		}

		// Token: 0x06000267 RID: 615 RVA: 0x00010C18 File Offset: 0x0000EE18
		public int GetVarFixture(string varName, ref FixtureArrayData fixtureArray)
		{
			int num = 0;
			Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
			int result;
			if (varIOInfo == null)
			{
				num = -536870888;
				result = num;
			}
			else
			{
				List<float[]> list = new List<float[]>();
				int num2 = 0;
				foreach (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> keyValuePair in varIOInfo)
				{
					string key = keyValuePair.Key;
					if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_FLOAT)
					{
						float[] array = null;
						num = ScriptNativeMethods.GetVarFloatValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array);
						if (num != 0)
						{
							return num;
						}
						if (array == null || array.Length <= 0)
						{
							return -536870888;
						}
						if (num2 > 0 && num2 != array.Length)
						{
							return -536870888;
						}
						num2 = array.Length;
						list.Add(array);
					}
				}
				if (list.Count < 10)
				{
					result = -536870888;
				}
				else
				{
					fixtureArray = new FixtureArrayData();
					fixtureArray.Count = num2;
					fixtureArray.InitPointXArray = list[0];
					fixtureArray.InitPointYArray = list[1];
					fixtureArray.InitAngleArray = list[2];
					fixtureArray.InitScaleXArray = list[3];
					fixtureArray.InitScaleYArray = list[4];
					fixtureArray.RunPointXArray = list[5];
					fixtureArray.RunPointYArray = list[6];
					fixtureArray.RunAngleArray = list[7];
					fixtureArray.RunScaleXArray = list[8];
					fixtureArray.RunScaleYArray = list[9];
					result = num;
				}
			}
			return result;
		}

		// Token: 0x06000268 RID: 616 RVA: 0x00010E1C File Offset: 0x0000F01C
		public int GetVarPolygon(string varName, ref PolygonArrayData polygonArray)
		{
			int num = 0;
			Dictionary<string, VariableManager.IO_VALUE_TYPE> varIOInfo = this.GetVarIOInfo(varName);
			int result;
			if (varIOInfo == null)
			{
				num = -536870888;
				result = num;
			}
			else
			{
				List<float[]> list = new List<float[]>();
				int[] array = new int[0];
				int num2 = 0;
				foreach (KeyValuePair<string, VariableManager.IO_VALUE_TYPE> keyValuePair in varIOInfo)
				{
					string key = keyValuePair.Key;
					if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_FLOAT)
					{
						float[] array2 = null;
						num = ScriptNativeMethods.GetVarFloatValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array2);
						if (num != 0)
						{
							return num;
						}
						if (array2 == null || array2.Length <= 0)
						{
							return -536870888;
						}
						if (num2 > 0 && num2 != array2.Length)
						{
							return -536870888;
						}
						num2 = array2.Length;
						list.Add(array2);
					}
					else if (keyValuePair.Value == VariableManager.IO_VALUE_TYPE.IO_VALUE_TYPE_INT)
					{
						int[] array3 = null;
						num = ScriptNativeMethods.GetVarIntValue(this.nVarModuleID, this.nShellModuleID, varName, key, ref array3);
						if (array3 == null || array3.Length <= 0)
						{
							return -536870888;
						}
						array = array3;
					}
				}
				int nTotalCount = 0;
				Array.ForEach<int>(array, delegate(int x)
				{
					nTotalCount += x;
				});
				if (list.Count < 2 || nTotalCount != num2)
				{
					result = -536870888;
				}
				else
				{
					polygonArray = new PolygonArrayData();
					polygonArray.Count = array.Length;
					polygonArray.PointNumArray = array;
					polygonArray.PointsXArray = new float[array.Length][];
					polygonArray.PointsYArray = new float[array.Length][];
					int num3 = 0;
					for (int i = 0; i < array.Length; i++)
					{
						polygonArray.PointsXArray[i] = new float[array[i]];
						polygonArray.PointsYArray[i] = new float[array[i]];
						Array.Copy(list[0], num3, polygonArray.PointsXArray[i], 0, array[i]);
						Array.Copy(list[1], num3, polygonArray.PointsYArray[i], 0, array[i]);
						num3 += array[i];
					}
					result = num;
				}
			}
			return result;
		}

		// Token: 0x040001C9 RID: 457
		private const int PIXEL_FORMAT_MONO8 = 17301505;

		// Token: 0x040001CA RID: 458
		private const int PIXEL_FORMAT_RGB24 = 35127316;
	}
}
