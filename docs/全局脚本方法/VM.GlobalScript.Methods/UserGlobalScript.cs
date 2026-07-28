using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using iMVS_6000PlatformSDKCS;
using VM.GlobalScript.Methods;

// Token: 0x02000002 RID: 2
public class UserGlobalScript : UserGlobalMethods, IScriptMethods
{
	// Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
	public int Init()
	{
		return base.InitSDK();
	}

	// Token: 0x06000002 RID: 2 RVA: 0x00002050 File Offset: 0x00000250
	public int Process()
	{
		if (this.m_operateHandle == IntPtr.Zero)
		{
			return -1;
		}
		this.ExecuteMultiProcessOnceSync(new uint[]
		{
			10000U,
			10001U
		});
		this.ExecuteSingleProcessOnceSync(10002U);
		this.ExecuteSingleProcessOnceSync(10000U);
		return -1;
	}

	// Token: 0x06000003 RID: 3 RVA: 0x000020A8 File Offset: 0x000002A8
	private void AddProcessIDToDictResetEvent(uint[] processIDArray)
	{
		if (processIDArray == null || processIDArray.Length == 0)
		{
			return;
		}
		for (int i = 0; i < processIDArray.Length; i++)
		{
			if (!this.dictProcessExecuteResetEvent.ContainsKey(processIDArray[i]))
			{
				this.dictProcessExecuteResetEvent.Add(processIDArray[i], new AutoResetEvent(false));
			}
		}
	}

	// Token: 0x06000004 RID: 4 RVA: 0x000020F0 File Offset: 0x000002F0
	private bool ExecuteSingleProcessOnceSync(uint processID)
	{
		if (!this.dictProcessExecuteResetEvent.ContainsKey(processID))
		{
			this.dictProcessExecuteResetEvent.Add(processID, new AutoResetEvent(false));
		}
		bool result;
		if (ImvsPlatformSDK_API.IMVS_PF_ExecuteOnce_V30_CS(this.m_operateHandle, processID, null) == 0)
		{
			this.dictProcessExecuteResetEvent[processID].WaitOne();
			result = true;
		}
		else
		{
			result = false;
		}
		return result;
	}

	// Token: 0x06000005 RID: 5 RVA: 0x00002148 File Offset: 0x00000348
	private void ExecuteMultiProcessOnceSync(uint[] processIDArray)
	{
		if (processIDArray == null || processIDArray.Length == 0)
		{
			return;
		}
		for (int i = 0; i < processIDArray.Length; i++)
		{
			if (!this.dictProcessExecuteResetEvent.ContainsKey(processIDArray[i]))
			{
				this.dictProcessExecuteResetEvent.Add(processIDArray[i], new AutoResetEvent(false));
			}
			if (ImvsPlatformSDK_API.IMVS_PF_ExecuteOnce_V30_CS(this.m_operateHandle, processIDArray[i], null) != 0)
			{
				this.dictProcessExecuteResetEvent[processIDArray[i]].Set();
			}
		}
		WaitHandle[] waitHandles = (from x in this.dictProcessExecuteResetEvent
		where processIDArray.ToList<uint>().Contains(x.Key)
		select x.Value).ToArray<AutoResetEvent>();
		WaitHandle.WaitAll(waitHandles);
	}

	// Token: 0x06000006 RID: 6 RVA: 0x0000222C File Offset: 0x0000042C
	private void ExecuteProcessSyncCallBack(ImvsSdkPFDefine.IMVS_PF_MODULE_WORK_STAUS workStatus)
	{
		if (workStatus.nWorkStatus != 0U)
		{
			return;
		}
		if (this.dictProcessExecuteResetEvent != null && this.dictProcessExecuteResetEvent.Count != 0 && this.dictProcessExecuteResetEvent.ContainsKey(workStatus.nProcessID))
		{
			this.dictProcessExecuteResetEvent[workStatus.nProcessID].Set();
		}
	}

	// Token: 0x06000007 RID: 7 RVA: 0x00002281 File Offset: 0x00000481
	private void RestProcessEvent(ImvsSdkPFDefine.IMVS_PF_DONGLE_INFO stDongInfo)
	{
	}

	// Token: 0x06000008 RID: 8 RVA: 0x00002284 File Offset: 0x00000484
	public override void ResultDataCallBack(IntPtr outputPlatformInfo, IntPtr puser)
	{
		ImvsSdkPFDefine.IMVS_PF_OUTPUT_PLATFORM_INFO imvs_PF_OUTPUT_PLATFORM_INFO = (ImvsSdkPFDefine.IMVS_PF_OUTPUT_PLATFORM_INFO)Marshal.PtrToStructure(outputPlatformInfo, typeof(ImvsSdkPFDefine.IMVS_PF_OUTPUT_PLATFORM_INFO));
		uint nInfoType = imvs_PF_OUTPUT_PLATFORM_INFO.nInfoType;
		if (nInfoType == 0U)
		{
			ImvsSdkPFDefine.IMVS_PF_MODU_RES_INFO imvs_PF_MODU_RES_INFO = (ImvsSdkPFDefine.IMVS_PF_MODU_RES_INFO)Marshal.PtrToStructure(imvs_PF_OUTPUT_PLATFORM_INFO.pData, typeof(ImvsSdkPFDefine.IMVS_PF_MODU_RES_INFO));
			return;
		}
		if (nInfoType == 3U)
		{
			ImvsSdkPFDefine.IMVS_PF_MODULE_WORK_STAUS stWorkStatus = (ImvsSdkPFDefine.IMVS_PF_MODULE_WORK_STAUS)Marshal.PtrToStructure(imvs_PF_OUTPUT_PLATFORM_INFO.pData, typeof(ImvsSdkPFDefine.IMVS_PF_MODULE_WORK_STAUS));
			delegate()
			{
				this.ExecuteProcessSyncCallBack(stWorkStatus);
			}.BeginInvoke(null, null);
			return;
		}
		if (nInfoType != 7U)
		{
			return;
		}
		ImvsSdkPFDefine.IMVS_PF_DONGLE_INFO imvs_PF_DONGLE_INFO = (ImvsSdkPFDefine.IMVS_PF_DONGLE_INFO)Marshal.PtrToStructure(imvs_PF_OUTPUT_PLATFORM_INFO.pData, typeof(ImvsSdkPFDefine.IMVS_PF_DONGLE_INFO));
	}
}
