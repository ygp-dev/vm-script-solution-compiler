using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using iMVS_6000PlatformSDKCS;

namespace VM.GlobalScript.Methods
{
	// Token: 0x0200000B RID: 11
	public class PlatformSdkFunction
	{
		// Token: 0x06000044 RID: 68 RVA: 0x00002EF6 File Offset: 0x000010F6
		public static PlatformSdkFunction GetInstance()
		{
			if (PlatformSdkFunction._instance == null)
			{
				PlatformSdkFunction._instance = new PlatformSdkFunction();
			}
			return PlatformSdkFunction._instance;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00002F0E File Offset: 0x0000110E
		public void SetRunMode(int nMode)
		{
			this.nRunMode = nMode;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00002F17 File Offset: 0x00001117
		public bool GetRunMode()
		{
			return this.nRunMode == 0;
		}

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x06000047 RID: 71 RVA: 0x00002F24 File Offset: 0x00001124
		// (remove) Token: 0x06000048 RID: 72 RVA: 0x00002F5C File Offset: 0x0000115C
		public event Action BeforeExecuteProcessContinusEvent;

		// Token: 0x06000049 RID: 73 RVA: 0x00002F94 File Offset: 0x00001194
		public void InitPlatformSDKEx(string ipaddress, string repAdress, int serPid, IntPtr skdHandle)
		{
			try
			{
				if (this.m_operateHandle == IntPtr.Zero)
				{
					if (skdHandle != IntPtr.Zero)
					{
						this.m_operateHandle = skdHandle;
						this.RegesiterResultBack();
					}
					else
					{
						LogHelper.objLog.Info("CreateHandle begin");
						PlatformSDKDefine.IMVS_PLATFORM_BASIC_INFO imvs_PLATFORM_BASIC_INFO = default(PlatformSDKDefine.IMVS_PLATFORM_BASIC_INFO);
						try
						{
							if (string.IsNullOrEmpty(ipaddress) || ipaddress == "null")
							{
								imvs_PLATFORM_BASIC_INFO.nPubIp = 0U;
								imvs_PLATFORM_BASIC_INFO.nPubPort = 0;
							}
							else
							{
								string[] array = ipaddress.Split(new char[]
								{
									':'
								});
								if (array.Length != 2)
								{
									LogHelper.objLog.Error("ClientCommAddr explain error," + ipaddress);
									return;
								}
								imvs_PLATFORM_BASIC_INFO.nPubIp = this.StringToInt(array[0]);
								imvs_PLATFORM_BASIC_INFO.nPubPort = Convert.ToUInt16(array[1]);
							}
							if (string.IsNullOrEmpty(repAdress) || repAdress == "null")
							{
								imvs_PLATFORM_BASIC_INFO.nServerRepIp = 0U;
								imvs_PLATFORM_BASIC_INFO.nServerRepPort = 0;
							}
							else
							{
								string[] array2 = repAdress.Split(new char[]
								{
									':'
								});
								if (array2.Length != 2)
								{
									LogHelper.objLog.Error("ServerRepAddr explain error," + repAdress);
									return;
								}
								imvs_PLATFORM_BASIC_INFO.nServerRepIp = this.StringToInt(array2[0]);
								imvs_PLATFORM_BASIC_INFO.nServerRepPort = Convert.ToUInt16(array2[1]);
							}
							imvs_PLATFORM_BASIC_INFO.nClientType = 1U;
							imvs_PLATFORM_BASIC_INFO.nHandleType = 2U;
							imvs_PLATFORM_BASIC_INFO.nServerProcID = serPid;
						}
						catch
						{
							LogHelper.objLog.Error("ClientCommAddr explain error," + ipaddress);
							return;
						}
						int num = PlatformSDKApi.IMVS_CreateHandle(ref this.m_operateHandle, ref imvs_PLATFORM_BASIC_INFO);
						if (num != 0)
						{
							LogHelper.objLog.Error("IMVS_PF_CreateHandle_CS faild," + Convert.ToString(num, 16));
						}
						else
						{
							LogHelper.objLog.Info("IMVS_PF_CreateHandle_CS succeed");
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.objLog.Error("InitPlatformSDK error," + ex.ToString());
				this.m_operateHandle = IntPtr.Zero;
			}
		}

		// Token: 0x0600004A RID: 74 RVA: 0x000031B0 File Offset: 0x000013B0
		public void RegesiterResultBack()
		{
			IntPtr zero = IntPtr.Zero;
			this.m_delegateDataCallBac = new delegateOutputCallBack(this.delegateDataCallBack);
			int num = ImvsPlatformSDK_API.IMVS_PF_RegisterResultCallBack_P_CS(this.m_operateHandle, this.m_delegateDataCallBac, zero);
			if (num != 0)
			{
				LogHelper.objLog.Error("IMVS_PF_RegisterResultCallBack_P_CS faild," + Convert.ToString(num, 16));
				return;
			}
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00003208 File Offset: 0x00001408
		public void UnRegesitResultBack()
		{
			IntPtr zero = IntPtr.Zero;
			int num = ImvsPlatformSDK_API.IMVS_PF_RegisterResultCallBack_P_CS(this.m_operateHandle, null, zero);
			if (num != 0)
			{
				LogHelper.objLog.Error("IMVS_PF_RegisterResultCallBack_P_CS faild," + Convert.ToString(num, 16));
				return;
			}
		}

		// Token: 0x0600004C RID: 76 RVA: 0x0000324C File Offset: 0x0000144C
		public void RegesiterProcessResultBack()
		{
			this.m_delegateResExportCallBac = new delegateResExportCallBack(this.delegateProcessExportCallBack);
			int num = ImvsPlatformSDK_API.IMVS_PF_RegisterResultCallBack_V32_P_CS(this.m_operateHandle, this.m_delegateResExportCallBac, IntPtr.Zero, 0U);
			if (num != 0)
			{
				LogHelper.objLog.Error("IMVS_PF_RegisterResultCallBack_V32_P_CS faild," + Convert.ToString(num, 16));
				return;
			}
		}

		// Token: 0x0600004D RID: 77 RVA: 0x000032A4 File Offset: 0x000014A4
		public void UnRegesiterProcessResultBack()
		{
			int num = ImvsPlatformSDK_API.IMVS_PF_RegisterResultCallBack_V32_P_CS(this.m_operateHandle, null, IntPtr.Zero, 0U);
			if (num != 0)
			{
				LogHelper.objLog.Error("IMVS_PF_RegisterResultCallBack_V32_P_CS faild," + Convert.ToString(num, 16));
				return;
			}
		}

		// Token: 0x0600004E RID: 78 RVA: 0x000032E4 File Offset: 0x000014E4
		public void UinitSDK()
		{
			try
			{
				if (this.m_operateHandle != IntPtr.Zero)
				{
					this.UnRegesitResultBack();
					if (this.nRunMode == 0)
					{
						PlatformSDKApi.IMVS_DestroyHandle(this.m_operateHandle, 0U);
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.objLog.Error("UinitSDK faild," + ex.Message);
			}
			finally
			{
				this.m_operateHandle = IntPtr.Zero;
			}
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003368 File Offset: 0x00001568
		public int SetVmRepAddr(string address)
		{
			try
			{
				if (this.m_operateHandle != IntPtr.Zero)
				{
					int num = PlatformSDKApi.IMVS_SetVmRepAddr4GlobalScript(this.m_operateHandle, address);
					if (num != 0)
					{
						LogHelper.objLog.Error("SetVmRepAddr errorCode: " + num);
					}
					return num;
				}
				return -536870902;
			}
			catch (Exception ex)
			{
				LogHelper.objLog.Error("SetVmRepAddr Error" + ex.Message);
			}
			return -536870657;
		}

		// Token: 0x06000050 RID: 80 RVA: 0x000033F4 File Offset: 0x000015F4
		public int StopRunAllProcess()
		{
			if (this.m_operateHandle == IntPtr.Zero)
			{
				return -536870902;
			}
			int num = ImvsPlatformSDK_API.IMVS_PF_StopExecute_CS(this.m_operateHandle, 3U);
			if (num != 0)
			{
				LogHelper.objLog.Error("IMVS_PF_StopExecute_CS error," + num);
			}
			return num;
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00003444 File Offset: 0x00001644
		private int IMVS_MakeModulesPrepared(IntPtr handle, PlatformSDKDefine.IMVS_CAMERA_PIC_INFO_LIST stCameraInfoList)
		{
			PlatformSDKDefine.IMVS_CAMERA_PIC_INFO_LIST imvs_CAMERA_PIC_INFO_LIST = default(PlatformSDKDefine.IMVS_CAMERA_PIC_INFO_LIST);
			imvs_CAMERA_PIC_INFO_LIST.stCamPicInfoList = new PlatformSDKDefine.IMVS_CAMERA_PIC_INFO[256];
			imvs_CAMERA_PIC_INFO_LIST.nNum = stCameraInfoList.nNum;
			imvs_CAMERA_PIC_INFO_LIST.nIsAllModuRun = stCameraInfoList.nIsAllModuRun;
			if (stCameraInfoList.stCamPicInfoList != null && stCameraInfoList.nNum > 0)
			{
				for (int i = 0; i < stCameraInfoList.nNum; i++)
				{
					imvs_CAMERA_PIC_INFO_LIST.stCamPicInfoList[i] = stCameraInfoList.stCamPicInfoList[i];
				}
			}
			if (stCameraInfoList.nReserved != null)
			{
				for (int i = 0; i < 4; i++)
				{
					imvs_CAMERA_PIC_INFO_LIST.nReserved[i] = stCameraInfoList.nReserved[i];
				}
			}
			IntPtr intPtr = new IntPtr(0);
			intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PlatformSDKDefine.IMVS_CAMERA_PIC_INFO_LIST)));
			Marshal.StructureToPtr<PlatformSDKDefine.IMVS_CAMERA_PIC_INFO_LIST>(imvs_CAMERA_PIC_INFO_LIST, intPtr, true);
			int result = PlatformSDKApi.IMVS_MakeModulesPreparedBySelfRun(handle, intPtr);
			Marshal.FreeHGlobal(intPtr);
			intPtr = IntPtr.Zero;
			return result;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x0000351C File Offset: 0x0000171C
		public int SilentlyExecuteOnce(int nSlientMode)
		{
			if (this.m_operateHandle == IntPtr.Zero)
			{
				return -536870902;
			}
			PlatformSDKDefine.IMVS_CAMERA_PIC_INFO_LIST stCameraInfoList = default(PlatformSDKDefine.IMVS_CAMERA_PIC_INFO_LIST);
			stCameraInfoList.nIsAllModuRun = nSlientMode;
			int num = this.IMVS_MakeModulesPrepared(this.m_operateHandle, stCameraInfoList);
			if (num != 0)
			{
				LogHelper.objLog.Error("IMVS_PF_MakeModulesPrepared_CS error," + num);
			}
			return num;
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00003580 File Offset: 0x00001780
		public int SetModuleResultReport(uint nModuleID, bool bEnable, int isAllEnable = -1)
		{
			if (this.m_operateHandle == IntPtr.Zero)
			{
				return -536870902;
			}
			PlatformSDKDefine.E_REPORT_RESULT_TYPE nMode;
			if (isAllEnable == 0)
			{
				nMode = PlatformSDKDefine.E_REPORT_RESULT_TYPE.REPORT_RESULT_TYPE_NONE;
			}
			else if (isAllEnable == 1)
			{
				nMode = PlatformSDKDefine.E_REPORT_RESULT_TYPE.REPORT_RESULT_TYPE_ALL;
			}
			else
			{
				nMode = PlatformSDKDefine.E_REPORT_RESULT_TYPE.REPORT_RESULT_TYPE_PART;
			}
			int num = PlatformSDKApi.IMVS_SetReportModuleResult_V2(this.m_operateHandle, nMode, nModuleID, bEnable);
			if (num != 0)
			{
				LogHelper.objLog.Error("IMVS_PF_MakeModulesPrepared_CS error," + num);
			}
			return num;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x000035E8 File Offset: 0x000017E8
		public bool GetAllProcess(ref ImvsSdkPFDefine.IMVS_PF_PROCESS_INFO_LIST procInfoList)
		{
			bool result;
			try
			{
				procInfoList = default(ImvsSdkPFDefine.IMVS_PF_PROCESS_INFO_LIST);
				procInfoList.astProcessInfo = new ImvsSdkPFDefine.IMVS_PF_PROCESS_INFO[1000];
				int num = ImvsPlatformSDK_API.IMVS_PF_GetAllProcessList_CS(this.m_operateHandle, ref procInfoList);
				if (num != 0)
				{
					LogHelper.objLog.Error("IMVS_PF_GetAllProcessList_CS faild");
					result = false;
				}
				else
				{
					result = true;
				}
			}
			catch (Exception ex)
			{
				LogHelper.objLog.Error("IMVS_PF_GetAllProcessList_CS faild " + ex.Message);
				result = false;
			}
			return result;
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00003664 File Offset: 0x00001864
		public int GetGlobalVariable(string variableName, ref string variableValue)
		{
			if (this.m_operateHandle == IntPtr.Zero)
			{
				return -536870906;
			}
			if (string.IsNullOrEmpty(variableName))
			{
				return -536870911;
			}
			PlatformSDKDefine.IMVS_GET_BINARY_LENGTH_INFO imvs_GET_BINARY_LENGTH_INFO = default(PlatformSDKDefine.IMVS_GET_BINARY_LENGTH_INFO);
			imvs_GET_BINARY_LENGTH_INFO.strName = this.UTF8GetFixLenBytes(this.strGlobalVariablePrex + variableName, 256);
			int num = PlatformSDKApi.IMVS_GetBinaryLength(this.m_operateHandle, this.uGlobalScriptModuleID, ref imvs_GET_BINARY_LENGTH_INFO, 3000U);
			if (num != 0)
			{
				LogHelper.objLog.Error("GetGlobalVariable error ret " + num);
				return num;
			}
			uint nBinaryLength = imvs_GET_BINARY_LENGTH_INFO.nBinaryLength;
			IntPtr intPtr = Marshal.AllocHGlobal((int)nBinaryLength);
			PlatformSDKDefine.IMVS_GET_BINARY_DATA_INFO imvs_GET_BINARY_DATA_INFO = default(PlatformSDKDefine.IMVS_GET_BINARY_DATA_INFO);
			imvs_GET_BINARY_DATA_INFO.strName = this.UTF8GetFixLenBytes(this.strGlobalVariablePrex + variableName, 256);
			imvs_GET_BINARY_DATA_INFO.pBinaryData = intPtr;
			imvs_GET_BINARY_DATA_INFO.nBinaryLength = nBinaryLength;
			imvs_GET_BINARY_DATA_INFO.nBinaryDataMallocSize = nBinaryLength;
			num = PlatformSDKApi.IMVS_GetBinaryData(this.m_operateHandle, this.uGlobalScriptModuleID, ref imvs_GET_BINARY_DATA_INFO, 3000U);
			if (num == 0)
			{
				byte[] array = new byte[nBinaryLength];
				Marshal.Copy(intPtr, array, 0, (int)nBinaryLength);
				variableValue = Encoding.UTF8.GetString(array).TrimEnd(new char[1]);
			}
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
			}
			if (num != 0)
			{
				LogHelper.objLog.Error("GetGlobalVariable error ret " + num);
				return num;
			}
			return 0;
		}

		// Token: 0x06000056 RID: 86 RVA: 0x000037BC File Offset: 0x000019BC
		public int SetGlobalVariable(string variableName, string variableValue)
		{
			if (this.m_operateHandle == IntPtr.Zero)
			{
				return -536870906;
			}
			if (string.IsNullOrEmpty(variableName))
			{
				return -536870911;
			}
			PlatformSDKDefine.IMVS_SET_BINARY_DATA_INFO imvs_SET_BINARY_DATA_INFO = default(PlatformSDKDefine.IMVS_SET_BINARY_DATA_INFO);
			byte[] array = PlatformSdkFunction.UTF8GetBytesPadZero(variableValue);
			uint num = (uint)array.Length;
			IntPtr intPtr = Marshal.AllocHGlobal((int)num);
			Marshal.Copy(array, 0, intPtr, (int)num);
			imvs_SET_BINARY_DATA_INFO.pBinaryData = intPtr;
			imvs_SET_BINARY_DATA_INFO.nBinaryLenth = num;
			imvs_SET_BINARY_DATA_INFO.strName = this.UTF8GetFixLenBytes(this.strGlobalVariablePrex + variableName, 256);
			int num2 = PlatformSDKApi.IMVS_SetBinaryData(this.m_operateHandle, this.uGlobalScriptModuleID, ref imvs_SET_BINARY_DATA_INFO, 300U);
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
			}
			if (num2 != 0)
			{
				LogHelper.objLog.Error("SetGlobalVariable error ret " + num2);
				return num2;
			}
			return 0;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x0000388C File Offset: 0x00001A8C
		public int GetGlobalCommunicatePort(ref int localPort, ref int destPort)
		{
			if (this.m_operateHandle == IntPtr.Zero)
			{
				return -536870906;
			}
			if (string.IsNullOrEmpty(this.strGlobalCommunicateKey))
			{
				return -536870911;
			}
			string text = "";
			uint nStrValueSize = 512U;
			int num = ImvsPlatformSDK_API.IMVS_PF_GetParamValue_CS(this.m_operateHandle, this.uGlobalCommModuleID, this.strGlobalCommunicateKey, nStrValueSize, ref text);
			if (num != 0)
			{
				LogHelper.objLog.Error("GetGlobalCommunicatePort error ret " + num);
				return num;
			}
			if (!text.Contains("local"))
			{
				return -536870911;
			}
			string[] array = text.Split(new char[]
			{
				';'
			});
			if (array.Length != 2)
			{
				return -536870911;
			}
			if (!int.TryParse(array[0].Substring(array[0].IndexOf(':') + 1), out destPort))
			{
				return -536870911;
			}
			if (!int.TryParse(array[1].Substring(array[1].IndexOf(':') + 1), out localPort))
			{
				return -536870911;
			}
			LogHelper.objLog.Info(string.Format("GetGlobalCommunicatePort succeed globalscript:{0},globalcom:{1}", localPort, destPort));
			return 0;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x000039A8 File Offset: 0x00001BA8
		public int SetGlobalCommunicatePort(int port)
		{
			if (this.m_operateHandle == IntPtr.Zero)
			{
				return -536870906;
			}
			if (string.IsNullOrEmpty(this.strGlobalCommunicateKey))
			{
				return -536870911;
			}
			int num = ImvsPlatformSDK_API.IMVS_PF_SetParamValue_CS(this.m_operateHandle, this.uGlobalCommModuleID, this.strGlobalCommunicateKey, port.ToString());
			if (num != 0)
			{
				LogHelper.objLog.Error("SetGlobalCommunicatePort error ret " + num);
				return num;
			}
			return 0;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00003A20 File Offset: 0x00001C20
		public int SendNormalData(byte[] bytes, int deceiveID, int addressID = -1, int dataType = 1, uint deceiveType = 1U)
		{
			try
			{
				if (this.m_operateHandle == IntPtr.Zero)
				{
					return -536870906;
				}
				if (bytes == null || bytes.Length == 0)
				{
					return -536870911;
				}
				ImvsSdkPFDefine.IMVS_PF_COMM_BINARY_DATA_INFO imvs_PF_COMM_BINARY_DATA_INFO = new ImvsSdkPFDefine.IMVS_PF_COMM_BINARY_DATA_INFO
				{
					nDeviceId = deceiveID,
					nAddressId = addressID,
					nDataLenth = (uint)bytes.Length,
					nDataType = dataType,
					pData = Marshal.AllocHGlobal(bytes.Length)
				};
				Marshal.Copy(bytes, 0, imvs_PF_COMM_BINARY_DATA_INFO.pData, bytes.Length);
				ImvsSdkPFDefine.IMVS_PF_SET_BINARY_DATA_INFO stBinaryDataInfo = default(ImvsSdkPFDefine.IMVS_PF_SET_BINARY_DATA_INFO);
				stBinaryDataInfo.nModuleType = deceiveType;
				stBinaryDataInfo.stCommBinaryData = imvs_PF_COMM_BINARY_DATA_INFO;
				stBinaryDataInfo.stNormalBinaryData = default(ImvsSdkPFDefine.IMVS_PF_NORMAL_BINARY_DATA_INFO);
				stBinaryDataInfo.nReserved = new uint[4];
				int result = ImvsPlatformSDK_API.IMVS_PF_SetBinaryData_CS(this.m_operateHandle, stBinaryDataInfo);
				Marshal.FreeCoTaskMem(imvs_PF_COMM_BINARY_DATA_INFO.pData);
				return result;
			}
			catch (Exception ex)
			{
				LogHelper.objLog.Error("IMVS_PF_SetBinaryData_CS Error" + ex.Message);
			}
			return -536870657;
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00003B28 File Offset: 0x00001D28
		public void BeforeExecuteProcessContinus()
		{
			if (this.BeforeExecuteProcessContinusEvent != null)
			{
				this.BeforeExecuteProcessContinusEvent();
			}
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00003B40 File Offset: 0x00001D40
		public int ReportData(string msg)
		{
			int result;
			try
			{
				int num = 0;
				if (string.IsNullOrEmpty(msg))
				{
					result = num;
				}
				else
				{
					byte[] bytes = Encoding.UTF8.GetBytes(msg);
					byte[] array = new byte[bytes.Length + 1];
					Array.Copy(bytes, array, bytes.Length);
					array[array.Length - 1] = 0;
					IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
					Marshal.Copy(array, 0, intPtr, array.Length);
					num = PlatformSDKApi.IMVS_ReportData(this.m_operateHandle, intPtr, (uint)array.Length);
					Marshal.FreeHGlobal(intPtr);
					result = num;
				}
			}
			catch (Exception ex)
			{
				Debugger.Log(0, null, ex.ToString());
				result = -1;
			}
			return result;
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00003BDC File Offset: 0x00001DDC
		private void delegateDataCallBack(IntPtr outputPlatformInfo, IntPtr puser)
		{
			try
			{
				Action<IntPtr, IntPtr> resultCallBack = this.ResultCallBack;
				if (resultCallBack != null)
				{
					resultCallBack(outputPlatformInfo, puser);
				}
			}
			catch (Exception ex)
			{
				LogHelper.objLog.Error("ResultCallBack is exception:" + ex.ToString());
			}
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00003C2C File Offset: 0x00001E2C
		private void delegateProcessExportCallBack(IntPtr pInputStruct, IntPtr pUser)
		{
			if (this.ExprotResultCallBack != null)
			{
				this.ExprotResultCallBack(pInputStruct, pUser);
			}
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00003C44 File Offset: 0x00001E44
		private uint StringToInt(string ip)
		{
			char[] separator = new char[]
			{
				'.'
			};
			string[] array = ip.Split(separator);
			return uint.Parse(array[0]) << 24 | uint.Parse(array[1]) << 16 | uint.Parse(array[2]) << 8 | uint.Parse(array[3]);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00003C90 File Offset: 0x00001E90
		private byte[] UTF8GetFixLenBytes(string str, int len)
		{
			byte[] array = new byte[len];
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			Buffer.BlockCopy(bytes, 0, array, 0, Math.Min(bytes.Length, len));
			return array;
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00003CC4 File Offset: 0x00001EC4
		public static byte[] UTF8GetBytesPadZero(string str)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			byte[] array = new byte[bytes.Length + 1];
			Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
			array[array.Length - 1] = 0;
			return array;
		}

		// Token: 0x04000024 RID: 36
		public static PlatformSdkFunction _instance;

		// Token: 0x04000025 RID: 37
		private int nRunMode;

		// Token: 0x04000026 RID: 38
		public IntPtr m_operateHandle = IntPtr.Zero;

		// Token: 0x04000027 RID: 39
		public delegateOutputCallBack m_delegateDataCallBac;

		// Token: 0x04000028 RID: 40
		public Action<IntPtr, IntPtr> ResultCallBack;

		// Token: 0x04000029 RID: 41
		public delegateResExportCallBack m_delegateResExportCallBac;

		// Token: 0x0400002A RID: 42
		public Action<IntPtr, IntPtr> ExprotResultCallBack;

		// Token: 0x0400002C RID: 44
		public ImvsSdkPFDefine.IMVS_PF_PROCESS_INFO_LIST stProcInfoList;

		// Token: 0x0400002D RID: 45
		public uint ScriptContinusExecuteInterval = 100U;

		// Token: 0x0400002E RID: 46
		public uint uGlobalScriptModuleID = 13000U;

		// Token: 0x0400002F RID: 47
		public uint uGlobalCommModuleID = 11000U;

		// Token: 0x04000030 RID: 48
		private string strGlobalVariablePrex = "GlobalScipt-";

		// Token: 0x04000031 RID: 49
		private string strGlobalCommunicateKey = "GlobalScriptPort_UDP";
	}
}
