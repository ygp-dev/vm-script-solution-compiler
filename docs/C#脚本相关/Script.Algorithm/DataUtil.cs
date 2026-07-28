using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Script.Algorithm
{
	// Token: 0x0200000E RID: 14
	public class DataUtil
	{
		// Token: 0x060000CD RID: 205 RVA: 0x00005584 File Offset: 0x00003784
		public static byte[] UTF8GetBytesPadZero(string str)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			byte[] array = new byte[bytes.Length + 1];
			Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
			array[array.Length - 1] = 0;
			return array;
		}

		// Token: 0x060000CE RID: 206 RVA: 0x000055C4 File Offset: 0x000037C4
		public static string UTF8GetString(byte[] bt)
		{
			string @string = Encoding.UTF8.GetString(bt);
			char[] trimChars = new char[1];
			return @string.TrimEnd(trimChars);
		}

		// Token: 0x060000CF RID: 207 RVA: 0x000055F0 File Offset: 0x000037F0
		public static byte[] Structure2Bytes(object structObj)
		{
			IntPtr intPtr = IntPtr.Zero;
			byte[] result;
			try
			{
				int num = Marshal.SizeOf(structObj);
				byte[] array = new byte[num];
				intPtr = Marshal.AllocHGlobal(num);
				if (intPtr == IntPtr.Zero)
				{
					throw new Exception("AllocHGlobal memory is Error in Structure2Bytes!");
				}
				Marshal.StructureToPtr(structObj, intPtr, false);
				Marshal.Copy(intPtr, array, 0, num);
				Marshal.FreeHGlobal(intPtr);
				result = array;
			}
			catch (Exception ex)
			{
				if (intPtr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(intPtr);
				}
				throw new Exception("Error in Structure2Bytes! " + ex.Message);
			}
			return result;
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x000056A4 File Offset: 0x000038A4
		public static byte[] IntToByte(int val)
		{
			return BitConverter.GetBytes(val);
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x000056C0 File Offset: 0x000038C0
		public static byte[] UIntToByte(uint val)
		{
			return BitConverter.GetBytes(val);
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x000056DC File Offset: 0x000038DC
		public static byte[] FloatToByte(float val)
		{
			return BitConverter.GetBytes(val);
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x000056F8 File Offset: 0x000038F8
		public static T[] IntPtr2Structures<T>(IntPtr pt, int lenth)
		{
			T[] result;
			try
			{
				T[] array = new T[lenth];
				int num = Marshal.SizeOf(typeof(T));
				for (int i = 0; i < lenth; i++)
				{
					IntPtr ptr = IntPtr.Zero;
					if (8 == IntPtr.Size)
					{
						ptr = IntPtr.Add(pt, i * num);
					}
					else
					{
						ptr = (IntPtr)((long)((ulong)((int)pt) + (ulong)((long)(i * num))));
					}
					array[i] = (T)((object)Marshal.PtrToStructure(ptr, typeof(T)));
				}
				result = array;
			}
			catch (Exception ex)
			{
				throw new Exception("Error in IntPtr2Structures: " + ex.Message);
			}
			return result;
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x000057BC File Offset: 0x000039BC
		public static T[] IntPtr2Ts<T>(IntPtr ptr, int size)
		{
			T[] result;
			if (size == 0 || ptr == IntPtr.Zero)
			{
				result = null;
			}
			else
			{
				T[] array = new T[size];
				try
				{
					if (array is int[])
					{
						int[] destination = array as int[];
						Marshal.Copy(ptr, destination, 0, size);
					}
					else if (array is byte[])
					{
						byte[] destination2 = array as byte[];
						Marshal.Copy(ptr, destination2, 0, size);
					}
					else
					{
						if (!(array is float[]))
						{
							return null;
						}
						float[] destination3 = array as float[];
						Marshal.Copy(ptr, destination3, 0, size);
					}
					result = array;
				}
				catch
				{
					result = array;
				}
			}
			return result;
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x00005890 File Offset: 0x00003A90
		public static string RepairName(string paraName)
		{
			string result;
			if (string.IsNullOrEmpty(paraName))
			{
				result = paraName;
			}
			else
			{
				if (paraName.Length > 0 && paraName[0] != '%' && paraName[paraName.Length - 1] != '%')
				{
					paraName = "%" + paraName + "%";
				}
				result = paraName;
			}
			return result;
		}
	}
}
