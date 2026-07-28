using System;

namespace Script.Algorithm
{
	// Token: 0x02000010 RID: 16
	public class BaseZmqCommunicate : ICommunicate, IDisposable
	{
		// Token: 0x060000D9 RID: 217 RVA: 0x000058FF File Offset: 0x00003AFF
		public BaseZmqCommunicate(ZmqDataContext contex)
		{
			this.zmqDataContext = contex;
		}

		// Token: 0x060000DA RID: 218 RVA: 0x00005914 File Offset: 0x00003B14
		public virtual bool InitCommuncate()
		{
			return false;
		}

		// Token: 0x060000DB RID: 219 RVA: 0x00005928 File Offset: 0x00003B28
		public virtual bool SendData(string msg)
		{
			return false;
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000593B File Offset: 0x00003B3B
		public virtual void Dispose()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060000DD RID: 221 RVA: 0x00005943 File Offset: 0x00003B43
		public virtual bool ReceiveData(ref string msg)
		{
			throw new NotImplementedException();
		}

		// Token: 0x04000049 RID: 73
		public ZmqDataContext zmqDataContext;

		// Token: 0x0400004A RID: 74
		public Action<string> GetReceiveData;
	}
}
