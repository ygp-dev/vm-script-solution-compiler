using System;
using System.Runtime.InteropServices;

namespace Script.Algorithm
{
	// Token: 0x02000032 RID: 50
	public class ScriptSDK
	{
		// Token: 0x06000206 RID: 518
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_SetObjectValue(IntPtr m_hOutput, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, int nValueType, int nIndex, IntPtr pValue, int nDataSize);

		// Token: 0x06000207 RID: 519
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_SetImageValue(int nModuleID, IntPtr hOutput, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, IntPtr pValue, int nDataSize, int nShareMemoryUseCount);

		// Token: 0x06000208 RID: 520
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_SetImageValueEx(int nModuleID, IntPtr hOutput, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, IntPtr pValue, int nDataSize, IntPtr pShareMapping, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szShareName);

		// Token: 0x06000209 RID: 521
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_ReleaseImageMemory(int nModuleID);

		// Token: 0x0600020A RID: 522
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_SetBasicArrayValue(IntPtr m_hOutput, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, int nValueType, IntPtr pValue, int nDataSize);

		// Token: 0x0600020B RID: 523
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_SetIntValue(IntPtr m_hOutput, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, int nIndex, int nValue);

		// Token: 0x0600020C RID: 524
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_SetFloatValue(IntPtr m_hOutput, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, int nIndex, float fValue);

		// Token: 0x0600020D RID: 525
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetIntValue(IntPtr m_hInput, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, int nIndex, ref int nCount, ref int nValue);

		// Token: 0x0600020E RID: 526
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetFloatValue(IntPtr m_hInput, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, int nIndex, ref int nCount, ref float fValue);

		// Token: 0x0600020F RID: 527
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetObjectValue(IntPtr m_hInput, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, int nValueType, int nIndex, int nBufferLen, ref int nCount, IntPtr pBuffer, ref int nDataSize);

		// Token: 0x06000210 RID: 528
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetObjectValueForModule(int nModuleID, int nSetModuleID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, int nIndex, int nBufferLen, ref int nCount, IntPtr pBuffer, ref int nDataSize, ref int nType);

		// Token: 0x06000211 RID: 529
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetModuleParamValue(int nModuleID, int nSetModuleID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, int nBufferLen, IntPtr pBuffer, ref int nDataSize);

		// Token: 0x06000212 RID: 530
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_SetObjectValueForModuleParams(int nModuleID, int nSetModuleID, int type, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string pValue);

		// Token: 0x06000213 RID: 531
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_Logger(int nModuleID, int logLevel, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string pValue);

		// Token: 0x06000214 RID: 532
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_ReportData(int nModuleID, int nType, int nRet);

		// Token: 0x06000215 RID: 533
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_Init_Logger();

		// Token: 0x06000216 RID: 534
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetNodeNum(ref int nNodeNum);

		// Token: 0x06000217 RID: 535
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetPointset(IntPtr m_hInput, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, int nBufferLen, IntPtr pBuffer, ref int nDataLen);

		// Token: 0x06000218 RID: 536
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_SetPointset(IntPtr m_hOutput, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szName, IntPtr pData, int nDataLen);

		// Token: 0x06000219 RID: 537
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_SetVarValue(int nModuleID, int nVarModuleID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szVarName, IntPtr pBuffer, int nBufferLen);

		// Token: 0x0600021A RID: 538
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetVarValue(int nModuleID, int nVarModuleID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szVarName, int nBufferLen, IntPtr pBuffer, ref int pDataLen);

		// Token: 0x0600021B RID: 539
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetVarSubIOInfo(int nVarModuleID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szVarName, int nBufferLen, IntPtr pBuffer, ref int pDataLen);

		// Token: 0x0600021C RID: 540
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetLocalVarModuleByID(int nModuleID, ref int nVarModuleID);

		// Token: 0x0600021D RID: 541
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetVarIOValue(int nVarModuleID, int nSetModuleID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szVarIOName, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szVarSubIOName, int nBufferLen, IntPtr pBuffer, ref int pDataLen);

		// Token: 0x0600021E RID: 542
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_SetVarValueString(int nModuleID, int nVarModuleID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szVarName, IntPtr pBuffer, int nBufferLen);

		// Token: 0x0600021F RID: 543
		[DllImport("ShellSDK.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Shell_GetVarValueString(int nModuleID, int nVarModuleID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Script.Algorithm.UTF8Marshaler)] string szVarName, int nBufferLen, IntPtr pBuffer, ref int pDataLen);

		// Token: 0x04000184 RID: 388
		private const string DLLPath = "ShellSDK.dll";
	}
}
