using System;
using System.Runtime.InteropServices;
using System.Text;
using Apps.XmlParser.Variable;

namespace Script.Algorithm
{
	// Token: 0x02000031 RID: 49
	public class ScriptNativeMethods
	{
		// Token: 0x060001E1 RID: 481 RVA: 0x0000C8E4 File Offset: 0x0000AAE4
		public static int SetIntValue(IntPtr output, string paramName, int index, int nValue)
		{
			return ScriptSDK.Shell_SetIntValue(output, paramName, index, nValue);
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x0000C904 File Offset: 0x0000AB04
		public static int SetFloatValue(IntPtr output, string paramName, int index, float fValue)
		{
			return ScriptSDK.Shell_SetFloatValue(output, paramName, index, fValue);
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x0000C924 File Offset: 0x0000AB24
		public static int SetStringValue(IntPtr output, string paramName, int index, string strValue)
		{
			int result;
			if (strValue == null)
			{
				result = -536870911;
			}
			else
			{
				byte[] bytes = Encoding.UTF8.GetBytes(strValue);
				IntPtr intPtr = Marshal.AllocHGlobal(bytes.Length);
				Marshal.Copy(bytes, 0, intPtr, bytes.Length);
				int num = ScriptSDK.Shell_SetObjectValue(output, paramName, 2, index, intPtr, bytes.Length);
				Marshal.FreeHGlobal(intPtr);
				result = num;
			}
			return result;
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x0000C984 File Offset: 0x0000AB84
		public static int SetBytesValue(IntPtr output, string paramName, int index, byte[] byteValue)
		{
			int result;
			if (byteValue == null || byteValue.Length <= 0)
			{
				result = -536870911;
			}
			else
			{
				IntPtr intPtr = Marshal.AllocHGlobal(byteValue.Length);
				Marshal.Copy(byteValue, 0, intPtr, byteValue.Length);
				int num = ScriptSDK.Shell_SetObjectValue(output, paramName, 3, index, intPtr, byteValue.Length);
				Marshal.FreeHGlobal(intPtr);
				result = num;
			}
			return result;
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x0000C9DC File Offset: 0x0000ABDC
		public static int SetBytesValue(IntPtr output, string paramName, int index, IntPtr ptrByteValue, int nSize)
		{
			return ScriptSDK.Shell_SetObjectValue(output, paramName, 3, index, ptrByteValue, nSize);
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x0000C9FC File Offset: 0x0000ABFC
		public static int SetImageValue(IntPtr output, string paramName, int index, IntPtr ptrByteValue, int nSize)
		{
			return ScriptSDK.Shell_SetObjectValue(output, paramName, 4, index, ptrByteValue, nSize);
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x0000CA1C File Offset: 0x0000AC1C
		public static int SetImageValue(IntPtr output, string paramName, int index, byte[] byteValue)
		{
			int result;
			if (byteValue == null || byteValue.Length <= 0)
			{
				result = -536870911;
			}
			else
			{
				IntPtr intPtr = Marshal.AllocHGlobal(byteValue.Length);
				Marshal.Copy(byteValue, 0, intPtr, byteValue.Length);
				int num = ScriptSDK.Shell_SetObjectValue(output, paramName, 4, index, intPtr, byteValue.Length);
				Marshal.FreeHGlobal(intPtr);
				result = num;
			}
			return result;
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x0000CA74 File Offset: 0x0000AC74
		public static int SetImageValueEx(int moduleid, IntPtr output, string paramName, byte[] byteValue, int useMemoryCount)
		{
			int result;
			if (byteValue == null)
			{
				result = -536870911;
			}
			else
			{
				IntPtr intPtr = IntPtr.Zero;
				int num = -536870657;
				try
				{
					intPtr = Marshal.AllocHGlobal(byteValue.Length);
					Marshal.Copy(byteValue, 0, intPtr, byteValue.Length);
					num = ScriptSDK.Shell_SetImageValue(moduleid, output, paramName, intPtr, byteValue.Length, useMemoryCount);
				}
				catch (Exception ex)
				{
					LogHelper.Error("SetImageValueEx error:" + ex.ToString(), 0);
				}
				finally
				{
					if (IntPtr.Zero != intPtr)
					{
						Marshal.FreeHGlobal(intPtr);
						intPtr = IntPtr.Zero;
					}
				}
				result = num;
			}
			return result;
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x0000CB34 File Offset: 0x0000AD34
		public static int SetImageValueOwnerMemory(int moduleid, IntPtr output, string paramName, byte[] byteValue, IntPtr pShareMaping, string shareNmae)
		{
			int result;
			if (byteValue == null)
			{
				result = -536870911;
			}
			else
			{
				IntPtr intPtr = IntPtr.Zero;
				int num = -536870657;
				try
				{
					intPtr = Marshal.AllocHGlobal(byteValue.Length);
					Marshal.Copy(byteValue, 0, intPtr, byteValue.Length);
					num = ScriptSDK.Shell_SetImageValueEx(moduleid, output, paramName, intPtr, byteValue.Length, pShareMaping, shareNmae);
				}
				catch (Exception ex)
				{
					LogHelper.Error("SetImageValueEx error:" + ex.ToString(), 0);
				}
				finally
				{
					if (IntPtr.Zero != intPtr)
					{
						Marshal.FreeHGlobal(intPtr);
						intPtr = IntPtr.Zero;
					}
				}
				result = num;
			}
			return result;
		}

		// Token: 0x060001EA RID: 490 RVA: 0x0000CBF8 File Offset: 0x0000ADF8
		public static int ReleaseImageMemory(int moduleid)
		{
			return ScriptSDK.Shell_ReleaseImageMemory(moduleid);
		}

		// Token: 0x060001EB RID: 491 RVA: 0x0000CC10 File Offset: 0x0000AE10
		public static int SetIntArrayValue(IntPtr output, string paramName, int[] intArray)
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
					num = ScriptSDK.Shell_SetBasicArrayValue(output, paramName, 0, intPtr, intArray.Length);
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

		// Token: 0x060001EC RID: 492 RVA: 0x0000CCBC File Offset: 0x0000AEBC
		public static int SetFloatArrayValue(IntPtr output, string paramName, float[] floatArray)
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
					num = ScriptSDK.Shell_SetBasicArrayValue(output, paramName, 1, intPtr, floatArray.Length);
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

		// Token: 0x060001ED RID: 493 RVA: 0x0000CD68 File Offset: 0x0000AF68
		public static int GetIntValue(IntPtr input, string paramName, int index, ref int nValue, ref int nCount)
		{
			return ScriptSDK.Shell_GetIntValue(input, paramName, index, ref nCount, ref nValue);
		}

		// Token: 0x060001EE RID: 494 RVA: 0x0000CD88 File Offset: 0x0000AF88
		public static int GetFloatValue(IntPtr input, string paramName, int index, ref float fValue, ref int nCount)
		{
			return ScriptSDK.Shell_GetFloatValue(input, paramName, index, ref nCount, ref fValue);
		}

		// Token: 0x060001EF RID: 495 RVA: 0x0000CDA8 File Offset: 0x0000AFA8
		public static int GetIntArrayValue(IntPtr input, string paramName, int index, ref int[] iValue, ref int nCount)
		{
			int num = 2048;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetObjectValue(input, paramName, 0, index, num, ref nCount, intPtr, ref num2);
			if (num3 == -536870894 && num2 >= num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetObjectValue(input, paramName, 0, index, num, ref nCount, intPtr, ref num2);
			}
			if (num3 == 0)
			{
				byte[] array = new byte[num2];
				Marshal.Copy(intPtr, array, 0, num2);
				iValue = new int[nCount];
				int num4 = 4;
				for (int i = 0; i < num2 / num4; i++)
				{
					iValue[i] = BitConverter.ToInt32(array, i * num4);
				}
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x0000CE94 File Offset: 0x0000B094
		public static int GetFloatArrayValue(IntPtr input, string paramName, int index, ref float[] fValue, ref int nCount)
		{
			int num = 2048;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetObjectValue(input, paramName, 1, index, num, ref nCount, intPtr, ref num2);
			if (num3 == -536870894 && num2 >= num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetObjectValue(input, paramName, 1, index, num, ref nCount, intPtr, ref num2);
			}
			if (num3 == 0)
			{
				byte[] array = new byte[num2];
				Marshal.Copy(intPtr, array, 0, num2);
				fValue = new float[nCount];
				int num4 = 4;
				for (int i = 0; i < num2 / num4; i++)
				{
					fValue[i] = BitConverter.ToSingle(array, i * num4);
				}
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x0000CF80 File Offset: 0x0000B180
		public static int GetStringValue(IntPtr input, string paramName, int index, ref string strValue, ref int nCount)
		{
			byte[] array = null;
			int byteValue = ScriptNativeMethods.GetByteValue(input, paramName, 2, index, ref array, ref nCount);
			if (array != null)
			{
				string @string = Encoding.UTF8.GetString(array);
				char[] trimChars = new char[1];
				strValue = @string.TrimEnd(trimChars);
			}
			return byteValue;
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x0000CFCC File Offset: 0x0000B1CC
		public static int GetBytesValue(IntPtr input, string paramName, int index, ref byte[] arrayValue, ref int nCount)
		{
			return ScriptNativeMethods.GetByteValue(input, paramName, 3, index, ref arrayValue, ref nCount);
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x0000CFEC File Offset: 0x0000B1EC
		public static int GetImageValue(IntPtr input, string paramName, int index, ref byte[] arrayValue, ref int nCount)
		{
			return ScriptNativeMethods.GetByteValue(input, paramName, 4, index, ref arrayValue, ref nCount);
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x0000D00C File Offset: 0x0000B20C
		private static int GetByteValue(IntPtr input, string paramName, int type, int index, ref byte[] arrayValue, ref int nCount)
		{
			int num = 1024;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetObjectValue(input, paramName, type, index, num, ref nCount, intPtr, ref num2);
			if (num3 == -536870894 && num2 >= num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetObjectValue(input, paramName, type, index, num, ref nCount, intPtr, ref num2);
			}
			if (num3 == 0)
			{
				arrayValue = new byte[num2];
				Marshal.Copy(intPtr, arrayValue, 0, num2);
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x0000D0C0 File Offset: 0x0000B2C0
		public static int GetByteValueForModule(int nModuleID, int nSetModuleID, string paramName, int nIndex, ref byte[] arrayValue, ref int nCount, ref int nType)
		{
			int num = 1024;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetObjectValueForModule(nModuleID, nSetModuleID, paramName, nIndex, num, ref nCount, intPtr, ref num2, ref nType);
			if (num3 == -536870894 && num2 >= num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetObjectValueForModule(nModuleID, nSetModuleID, paramName, nIndex, num, ref nCount, intPtr, ref num2, ref nType);
			}
			if (num3 == 0)
			{
				arrayValue = new byte[num2];
				Marshal.Copy(intPtr, arrayValue, 0, num2);
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x0000D178 File Offset: 0x0000B378
		public static int SetParamValueForModule(int nModuleID, int nSetModuleID, string paramName, string paramValue, int valuetype)
		{
			return ScriptSDK.Shell_SetObjectValueForModuleParams(nModuleID, nSetModuleID, valuetype, paramName, paramValue);
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x0000D198 File Offset: 0x0000B398
		public static int GetModuleParamValue(int nModuleID, int nSetModuleID, string paramName, ref string paramValue)
		{
			int num = 1024;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetModuleParamValue(nModuleID, nSetModuleID, paramName, num, intPtr, ref num2);
			if (num3 == -536870894 && num2 >= num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2 + 1;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetModuleParamValue(nModuleID, nSetModuleID, paramName, num, intPtr, ref num2);
			}
			if (num3 == 0)
			{
				byte[] array = new byte[num2];
				Marshal.Copy(intPtr, array, 0, num2);
				string @string = Encoding.UTF8.GetString(array);
				char[] trimChars = new char[1];
				paramValue = @string.TrimEnd(trimChars);
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x0000D264 File Offset: 0x0000B464
		public static int ReportData(int nModuleID, int nType, int nRet)
		{
			return ScriptSDK.Shell_ReportData(nModuleID, nType, nRet);
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x0000D280 File Offset: 0x0000B480
		public static int GetBufferNum(ref int nNodeNum)
		{
			return ScriptSDK.Shell_GetNodeNum(ref nNodeNum);
		}

		// Token: 0x060001FA RID: 506 RVA: 0x0000D298 File Offset: 0x0000B498
		public static int SetPointsetValue(IntPtr output, string paramName, byte[] arrayValue)
		{
			int result;
			if (arrayValue == null || arrayValue.Length <= 0)
			{
				result = -536870911;
			}
			else
			{
				int num = 0;
				IntPtr intPtr = Marshal.AllocHGlobal(arrayValue.Length);
				try
				{
					Marshal.Copy(arrayValue, 0, intPtr, arrayValue.Length);
					num = ScriptSDK.Shell_SetPointset(output, paramName, intPtr, arrayValue.Length);
				}
				catch (Exception ex)
				{
					ScriptSDK.Shell_Logger(0, 0, ex.Message);
					num = -536870888;
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
			return result;
		}

		// Token: 0x060001FB RID: 507 RVA: 0x0000D350 File Offset: 0x0000B550
		public static int GetPointsetValue(IntPtr input, string paramName, ref byte[] arrayValue)
		{
			int num = 1024;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetPointset(input, paramName, num, intPtr, ref num2);
			if (num3 == -536870894 && num2 > num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetPointset(input, paramName, num, intPtr, ref num2);
			}
			if (num3 == 0)
			{
				arrayValue = new byte[num2];
				Marshal.Copy(intPtr, arrayValue, 0, num2);
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}

		// Token: 0x060001FC RID: 508 RVA: 0x0000D3FC File Offset: 0x0000B5FC
		public static int SetVarValue(int nVarModuleID, string varName, IntPtr pBuffer, int nDataLen, int nModuleID = -1)
		{
			return ScriptSDK.Shell_SetVarValue(nModuleID, nVarModuleID, DataUtil.RepairName(varName), pBuffer, nDataLen);
		}

		// Token: 0x060001FD RID: 509 RVA: 0x0000D420 File Offset: 0x0000B620
		public static int SetVarValueWithNoType(int nVarModuleID, string varName, string strValue, int nModuleID = -1)
		{
			int result;
			if (strValue == null)
			{
				result = -536870911;
			}
			else
			{
				byte[] array = DataUtil.UTF8GetBytesPadZero(strValue);
				int num = array.Length;
				IntPtr intPtr = Marshal.AllocHGlobal(num);
				if (intPtr == IntPtr.Zero)
				{
					result = -536870910;
				}
				else
				{
					int num2 = 0;
					try
					{
						Marshal.Copy(array, 0, intPtr, num);
						num2 = ScriptSDK.Shell_SetVarValueString(nModuleID, nVarModuleID, DataUtil.RepairName(varName), intPtr, num);
					}
					catch (Exception ex)
					{
						ScriptSDK.Shell_Logger(0, 0, ex.Message);
					}
					finally
					{
						Marshal.FreeHGlobal(intPtr);
						intPtr = IntPtr.Zero;
					}
					result = num2;
				}
			}
			return result;
		}

		// Token: 0x060001FE RID: 510 RVA: 0x0000D4E4 File Offset: 0x0000B6E4
		public static int GetVarValueWithNoType(int nVarModuleID, string varName, ref string stVarValue, int nModuleID = -1)
		{
			int num = 2048;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetVarValueString(nModuleID, nVarModuleID, DataUtil.RepairName(varName), num, intPtr, ref num2);
			while (num3 == -536870894 && num2 >= num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetVarValueString(nModuleID, nVarModuleID, DataUtil.RepairName(varName), num, intPtr, ref num2);
			}
			if (num3 == 0)
			{
				byte[] array = new byte[num2];
				Marshal.Copy(intPtr, array, 0, num2);
				string @string = Encoding.UTF8.GetString(array);
				char[] trimChars = new char[1];
				stVarValue = @string.TrimEnd(trimChars);
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}

		// Token: 0x060001FF RID: 511 RVA: 0x0000D5BC File Offset: 0x0000B7BC
		public static int GetVarSubIOInfo(int nVarModuleID, string varName, ref VariableManager.IMVS_SUB_VALUE[] stVarValue)
		{
			varName = DataUtil.RepairName(varName);
			int num = 2048;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetVarSubIOInfo(nVarModuleID, varName, num, intPtr, ref num2);
			if (num3 == -536870894 && num2 >= num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetVarSubIOInfo(nVarModuleID, varName, num, intPtr, ref num2);
			}
			if (num3 == 0)
			{
				int num4 = Marshal.SizeOf(typeof(VariableManager.IMVS_SUB_VALUE));
				int num5 = num2 / num4;
				stVarValue = new VariableManager.IMVS_SUB_VALUE[num5];
				for (int i = 0; i < num5; i++)
				{
					stVarValue[i] = (VariableManager.IMVS_SUB_VALUE)Marshal.PtrToStructure(intPtr + i * num4, typeof(VariableManager.IMVS_SUB_VALUE));
				}
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}

		// Token: 0x06000200 RID: 512 RVA: 0x0000D6C0 File Offset: 0x0000B8C0
		public static int GetLocalVarIdByID(int nModuleID, ref int nLocalVarID)
		{
			return ScriptSDK.Shell_GetLocalVarModuleByID(nModuleID, ref nLocalVarID);
		}

		// Token: 0x06000201 RID: 513 RVA: 0x0000D6DC File Offset: 0x0000B8DC
		public static int GetVarIntValue(int nVarModuleID, int nModuleID, string varIoName, string varSubIoName, ref int[] iValue)
		{
			int num = 2048;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetVarIOValue(nVarModuleID, nModuleID, varIoName, varSubIoName, num, intPtr, ref num2);
			while (num3 == -536870894 && num2 >= num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetVarIOValue(nVarModuleID, nModuleID, varIoName, varSubIoName, num, intPtr, ref num2);
			}
			if (num3 == 0)
			{
				byte[] array = new byte[num2];
				Marshal.Copy(intPtr, array, 0, num2);
				int num4 = num2 / Marshal.SizeOf(typeof(int));
				iValue = new int[num4];
				Buffer.BlockCopy(array, 0, iValue, 0, array.Length);
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}

		// Token: 0x06000202 RID: 514 RVA: 0x0000D7BC File Offset: 0x0000B9BC
		public static int GetVarFloatValue(int nVarModuleID, int nModuleID, string varIoName, string varSubIoName, ref float[] fValue)
		{
			int num = 2048;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetVarIOValue(nVarModuleID, nModuleID, varIoName, varSubIoName, num, intPtr, ref num2);
			while (num3 == -536870894 && num2 >= num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetVarIOValue(nVarModuleID, nModuleID, varIoName, varSubIoName, num, intPtr, ref num2);
			}
			if (num3 == 0)
			{
				byte[] array = new byte[num2];
				Marshal.Copy(intPtr, array, 0, num2);
				int num4 = num2 / Marshal.SizeOf(typeof(float));
				fValue = new float[num4];
				Buffer.BlockCopy(array, 0, fValue, 0, array.Length);
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}

		// Token: 0x06000203 RID: 515 RVA: 0x0000D89C File Offset: 0x0000BA9C
		public static int GetVarStringValue(int nVarModuleID, int nModuleID, string varIoName, string varSubIoName, ref string[] strValue)
		{
			int num = 2048;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetVarIOValue(nVarModuleID, nModuleID, varIoName, varSubIoName, num, intPtr, ref num2);
			while (num3 == -536870894 && num2 >= num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetVarIOValue(nVarModuleID, nModuleID, varIoName, varSubIoName, num, intPtr, ref num2);
			}
			if (num3 == 0)
			{
				byte[] array = new byte[num2];
				Marshal.Copy(intPtr, array, 0, num2);
				string @string = Encoding.UTF8.GetString(array);
				char[] array2 = new char[1];
				string text = @string.TrimEnd(array2);
				string text2 = text;
				array2 = new char[1];
				strValue = text2.Split(array2);
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}

		// Token: 0x06000204 RID: 516 RVA: 0x0000D980 File Offset: 0x0000BB80
		public static int GetVarByteValue(int nVarModuleID, int nModuleID, string varIoName, string varSubIoName, ref byte[] byteValue)
		{
			int num = 2048;
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			int num2 = 0;
			int num3 = ScriptSDK.Shell_GetVarIOValue(nVarModuleID, nModuleID, varIoName, varSubIoName, num, intPtr, ref num2);
			while (num3 == -536870894 && num2 >= num)
			{
				Marshal.FreeHGlobal(intPtr);
				num = num2;
				intPtr = Marshal.AllocHGlobal(num);
				num3 = ScriptSDK.Shell_GetVarIOValue(nVarModuleID, nModuleID, varIoName, varSubIoName, num, intPtr, ref num2);
			}
			if (num3 == 0)
			{
				byteValue = new byte[num2];
				Marshal.Copy(intPtr, byteValue, 0, num2);
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
			return num3;
		}
	}
}
