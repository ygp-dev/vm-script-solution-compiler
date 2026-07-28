using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace VM.GlobalScript.Methods
{
	// Token: 0x02000008 RID: 8
	public class UTF8Marshaler : ICustomMarshaler
	{
		// Token: 0x0600002F RID: 47 RVA: 0x00002281 File Offset: 0x00000481
		public void CleanUpManagedData(object managedObj)
		{
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002E2C File Offset: 0x0000102C
		public void CleanUpNativeData(IntPtr pNativeData)
		{
			Marshal.FreeHGlobal(pNativeData);
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002E34 File Offset: 0x00001034
		public int GetNativeDataSize()
		{
			return -1;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002E38 File Offset: 0x00001038
		public IntPtr MarshalManagedToNative(object managedObj)
		{
			if (managedObj == null)
			{
				return IntPtr.Zero;
			}
			if (!(managedObj is string))
			{
				throw new InvalidOperationException();
			}
			byte[] bytes = Encoding.UTF8.GetBytes(managedObj as string);
			IntPtr intPtr = Marshal.AllocHGlobal(bytes.Length + 1);
			Marshal.Copy(bytes, 0, intPtr, bytes.Length);
			Marshal.WriteByte(intPtr, bytes.Length, 0);
			return intPtr;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00002E90 File Offset: 0x00001090
		public object MarshalNativeToManaged(IntPtr pNativeData)
		{
			if (pNativeData == IntPtr.Zero)
			{
				return null;
			}
			List<byte> list = new List<byte>();
			int num = 0;
			for (;;)
			{
				byte b = Marshal.ReadByte(pNativeData, num);
				if (b == 0)
				{
					break;
				}
				list.Add(b);
				num++;
			}
			return Encoding.UTF8.GetString(list.ToArray(), 0, list.Count);
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002EE3 File Offset: 0x000010E3
		public static ICustomMarshaler GetInstance(string cookie)
		{
			return UTF8Marshaler.instance;
		}

		// Token: 0x0400001A RID: 26
		private static UTF8Marshaler instance = new UTF8Marshaler();
	}
}
