using System;
using System.Runtime.InteropServices;

namespace VM.GlobalScript.Methods
{
	// Token: 0x0200000A RID: 10
	public class PlatformSDKApi
	{
		// Token: 0x06000038 RID: 56
		[DllImport(".\\iMVS-6000PlatformSDK.dll")]
		public static extern int IMVS_CreateHandle(ref IntPtr handle, ref PlatformSDKDefine.IMVS_PLATFORM_BASIC_INFO pstPlatformBasicInfo);

		// Token: 0x06000039 RID: 57
		[DllImport(".\\iMVS-6000PlatformSDK.dll")]
		public static extern int IMVS_DestroyHandle(IntPtr handle, uint nTakeoverType = 0U);

		// Token: 0x0600003A RID: 58
		[DllImport(".\\iMVS-6000PlatformSDK.dll")]
		public static extern int IMVS_SetVmRepAddr4GlobalScript(IntPtr handle, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = VM.GlobalScript.Methods.UTF8Marshaler)] string strVMRepAddr);

		// Token: 0x0600003B RID: 59
		[DllImport(".\\iMVS-6000PlatformSDK.dll")]
		public static extern int IMVS_MakeModulesPrepared(IntPtr handle, IntPtr pstCamPicInfoList);

		// Token: 0x0600003C RID: 60
		[DllImport(".\\iMVS-6000PlatformSDK.dll")]
		public static extern int IMVS_MakeModulesPreparedBySelfRun(IntPtr handle, IntPtr pstCamPicInfoList);

		// Token: 0x0600003D RID: 61
		[DllImport(".\\iMVS-6000PlatformSDK.dll")]
		public static extern int IMVS_PF_ExecuteOnce(IntPtr handle, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = VM.GlobalScript.Methods.UTF8Marshaler)] string strCommand);

		// Token: 0x0600003E RID: 62
		[DllImport(".\\iMVS-6000PlatformSDK.dll")]
		public static extern int IMVS_SetReportModuleResult_V2(IntPtr handle, PlatformSDKDefine.E_REPORT_RESULT_TYPE nMode, uint nModuId, bool bIsEnable);

		// Token: 0x0600003F RID: 63
		[DllImport(".\\iMVS-6000PlatformSDK.dll")]
		public static extern int IMVS_ReportData(IntPtr handle, IntPtr pData, uint nDataLen);

		// Token: 0x06000040 RID: 64
		[DllImport(".\\iMVS-6000PlatformSDK.dll")]
		public static extern int IMVS_GetBinaryData(IntPtr handle, uint nModuleID, ref PlatformSDKDefine.IMVS_GET_BINARY_DATA_INFO strValue, uint nRecvWaitTime);

		// Token: 0x06000041 RID: 65
		[DllImport(".\\iMVS-6000PlatformSDK.dll")]
		public static extern int IMVS_SetBinaryData(IntPtr handle, uint nModuleID, ref PlatformSDKDefine.IMVS_SET_BINARY_DATA_INFO strValue, uint nRecvWaitTime);

		// Token: 0x06000042 RID: 66
		[DllImport(".\\iMVS-6000PlatformSDK.dll")]
		public static extern int IMVS_GetBinaryLength(IntPtr handle, uint nModuleID, ref PlatformSDKDefine.IMVS_GET_BINARY_LENGTH_INFO strValue, uint nRecvWaitTime);

		// Token: 0x04000021 RID: 33
		private const string ThirdPath = "..\\PublicFile\\x64\\iMVS-6000PlatformSDK.dll";

		// Token: 0x04000022 RID: 34
		private const string CurrenPath = ".\\iMVS-6000PlatformSDK.dll";

		// Token: 0x04000023 RID: 35
		private const string DLLPath = ".\\iMVS-6000PlatformSDK.dll";
	}
}
