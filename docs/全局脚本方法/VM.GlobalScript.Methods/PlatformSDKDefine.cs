using System;
using System.Runtime.InteropServices;

namespace VM.GlobalScript.Methods
{
	// Token: 0x02000009 RID: 9
	public class PlatformSDKDefine
	{
		// Token: 0x0400001B RID: 27
		public const int IMVS_MAX_USERNAME_LENGTH = 16;

		// Token: 0x0400001C RID: 28
		public const int IMVS_MAX_PASSWORD_LENGTH = 32;

		// Token: 0x0400001D RID: 29
		public const int IMVS_MAX_VENDORNAME_LENGTH = 16;

		// Token: 0x0400001E RID: 30
		public const int IMVS_BINARY_DATA_PARAM_LENGTH = 256;

		// Token: 0x0400001F RID: 31
		public const int IMVS_CAMPICINFO_LIST_NUM = 256;

		// Token: 0x04000020 RID: 32
		public const int IMVS_MAX_PATH_UTF8_LENGTH = 780;

		// Token: 0x02000019 RID: 25
		public struct IMVS_PLATFORM_BASIC_INFO
		{
			// Token: 0x04000065 RID: 101
			public uint nIp;

			// Token: 0x04000066 RID: 102
			public ushort nPort;

			// Token: 0x04000067 RID: 103
			public uint nPubIp;

			// Token: 0x04000068 RID: 104
			public ushort nPubPort;

			// Token: 0x04000069 RID: 105
			public uint nNetAdapterIp;

			// Token: 0x0400006A RID: 106
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] strUserName;

			// Token: 0x0400006B RID: 107
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public byte[] strPassWord;

			// Token: 0x0400006C RID: 108
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] strVendorName;

			// Token: 0x0400006D RID: 109
			public uint nMacLow;

			// Token: 0x0400006E RID: 110
			public ushort nMacHigh;

			// Token: 0x0400006F RID: 111
			public uint nClientType;

			// Token: 0x04000070 RID: 112
			public uint nServerType;

			// Token: 0x04000071 RID: 113
			public uint nHandleType;

			// Token: 0x04000072 RID: 114
			public uint nServerRepIp;

			// Token: 0x04000073 RID: 115
			public ushort nServerRepPort;

			// Token: 0x04000074 RID: 116
			public int nServerProcID;

			// Token: 0x04000075 RID: 117
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public uint[] nReserved;
		}

		// Token: 0x0200001A RID: 26
		public struct IMVS_CAMERA_PIC_INFO
		{
			// Token: 0x04000076 RID: 118
			public int nCameraId;

			// Token: 0x04000077 RID: 119
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 780)]
			public byte[] szLocalPicPath;
		}

		// Token: 0x0200001B RID: 27
		public struct IMVS_CAMERA_PIC_INFO_LIST
		{
			// Token: 0x04000078 RID: 120
			public int nNum;

			// Token: 0x04000079 RID: 121
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256, ArraySubType = UnmanagedType.Struct)]
			public PlatformSDKDefine.IMVS_CAMERA_PIC_INFO[] stCamPicInfoList;

			// Token: 0x0400007A RID: 122
			public int nTimeout;

			// Token: 0x0400007B RID: 123
			public int nIsAllModuRun;

			// Token: 0x0400007C RID: 124
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
			public uint[] nReserved;
		}

		// Token: 0x0200001C RID: 28
		public enum E_REPORT_RESULT_TYPE
		{
			// Token: 0x0400007E RID: 126
			REPORT_RESULT_TYPE_INVALID,
			// Token: 0x0400007F RID: 127
			REPORT_RESULT_TYPE_NONE,
			// Token: 0x04000080 RID: 128
			REPORT_RESULT_TYPE_ALL,
			// Token: 0x04000081 RID: 129
			REPORT_RESULT_TYPE_PART
		}

		// Token: 0x0200001D RID: 29
		public struct IMVS_SET_BINARY_DATA_INFO
		{
			// Token: 0x04000082 RID: 130
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public byte[] strName;

			// Token: 0x04000083 RID: 131
			public IntPtr pBinaryData;

			// Token: 0x04000084 RID: 132
			public uint nBinaryLenth;

			// Token: 0x04000085 RID: 133
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public uint[] nReserved;
		}

		// Token: 0x0200001E RID: 30
		public struct IMVS_GET_BINARY_DATA_INFO
		{
			// Token: 0x04000086 RID: 134
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public byte[] strName;

			// Token: 0x04000087 RID: 135
			public uint nBinaryDataMallocSize;

			// Token: 0x04000088 RID: 136
			public IntPtr pBinaryData;

			// Token: 0x04000089 RID: 137
			public uint nBinaryLength;

			// Token: 0x0400008A RID: 138
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public uint[] nReserved;
		}

		// Token: 0x0200001F RID: 31
		public struct IMVS_GET_BINARY_LENGTH_INFO
		{
			// Token: 0x0400008B RID: 139
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public byte[] strName;

			// Token: 0x0400008C RID: 140
			public uint nBinaryLength;

			// Token: 0x0400008D RID: 141
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public uint[] nReserved;
		}
	}
}
