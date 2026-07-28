using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Script.Algorithm
{
	// Token: 0x02000033 RID: 51
	public class UTF8Marshaler : ICustomMarshaler
	{
		// Token: 0x06000221 RID: 545 RVA: 0x0000DA44 File Offset: 0x0000BC44
		public void CleanUpManagedData(object managedObj)
		{
		}

		// Token: 0x06000222 RID: 546 RVA: 0x0000DA47 File Offset: 0x0000BC47
		public void CleanUpNativeData(IntPtr pNativeData)
		{
			Marshal.FreeHGlobal(pNativeData);
		}

		// Token: 0x06000223 RID: 547 RVA: 0x0000DA54 File Offset: 0x0000BC54
		public int GetNativeDataSize()
		{
			return -1;
		}

		// Token: 0x06000224 RID: 548 RVA: 0x0000DA68 File Offset: 0x0000BC68
		public IntPtr MarshalManagedToNative(object managedObj)
		{
			IntPtr result;
			if (object.ReferenceEquals(managedObj, null))
			{
				result = IntPtr.Zero;
			}
			else
			{
				if (!(managedObj is string))
				{
					throw new InvalidOperationException();
				}
				byte[] bytes = Encoding.UTF8.GetBytes(managedObj as string);
				IntPtr intPtr = Marshal.AllocHGlobal(bytes.Length + 1);
				Marshal.Copy(bytes, 0, intPtr, bytes.Length);
				Marshal.WriteByte(intPtr, bytes.Length, 0);
				result = intPtr;
			}
			return result;
		}

		// Token: 0x06000225 RID: 549 RVA: 0x0000DAD8 File Offset: 0x0000BCD8
		public object MarshalNativeToManaged(IntPtr pNativeData)
		{
			object result;
			if (pNativeData == IntPtr.Zero)
			{
				result = null;
			}
			else
			{
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
				result = Encoding.UTF8.GetString(list.ToArray(), 0, list.Count);
			}
			return result;
		}

		// Token: 0x06000226 RID: 550 RVA: 0x0000DB50 File Offset: 0x0000BD50
		public static ICustomMarshaler GetInstance(string cookie)
		{
			return UTF8Marshaler.instance;
		}

		// Token: 0x04000185 RID: 389
		private static UTF8Marshaler instance = new UTF8Marshaler();
	}
}
