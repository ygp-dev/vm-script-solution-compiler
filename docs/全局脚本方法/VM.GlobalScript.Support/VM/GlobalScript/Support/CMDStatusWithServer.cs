using System;

namespace VM.GlobalScript.Support
{
	// Token: 0x0200000F RID: 15
	public enum CMDStatusWithServer
	{
		// Token: 0x04000026 RID: 38
		ShowWindow = 4001,
		// Token: 0x04000027 RID: 39
		ExcuteOnce,
		// Token: 0x04000028 RID: 40
		ExcuteContinue,
		// Token: 0x04000029 RID: 41
		StopExcute,
		// Token: 0x0400002A RID: 42
		SaveSolution,
		// Token: 0x0400002B RID: 43
		LoadSolution,
		// Token: 0x0400002C RID: 44
		CloseScript,
		// Token: 0x0400002D RID: 45
		SetMsgFromUI,
		// Token: 0x0400002E RID: 46
		GetMsgToUI,
		// Token: 0x0400002F RID: 47
		SetVMZmqPair = 4011,
		// Token: 0x04000030 RID: 48
		ReleaseSharedMemory,
		// Token: 0x04000031 RID: 49
		SilentlyExecuteOnce,
		// Token: 0x04000032 RID: 50
		LoadSolutionEnd,
		// Token: 0x04000033 RID: 51
		SetCommunicateData,
		// Token: 0x04000034 RID: 52
		UnKnow
	}
}
