using System;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000017 RID: 23
	public class BaseZmqCommunicate : ICommunicate, IDisposable
	{
		// Token: 0x0600006D RID: 109 RVA: 0x00004502 File Offset: 0x00002702
		public BaseZmqCommunicate(ZmqDataContext contex)
		{
			this.zmqDataContext = contex;
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00004514 File Offset: 0x00002714
		public virtual bool InitCommuncate()
		{
			return false;
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00004528 File Offset: 0x00002728
		public virtual bool SendData(string msg)
		{
			return false;
		}

		// Token: 0x06000070 RID: 112 RVA: 0x0000453B File Offset: 0x0000273B
		public virtual void Dispose()
		{
			throw new NotImplementedException();
		}

		// Token: 0x0400008B RID: 139
		public ZmqDataContext zmqDataContext;

		// Token: 0x0400008C RID: 140
		public Func<string, string> GetReceiveData;
	}
}
