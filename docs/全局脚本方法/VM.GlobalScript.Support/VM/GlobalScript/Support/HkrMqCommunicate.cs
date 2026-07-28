using System;
using System.Threading;
using System.Threading.Tasks;

namespace VM.GlobalScript.Support
{
	// Token: 0x0200001A RID: 26
	public class HkrMqCommunicate : BaseZmqCommunicate
	{
		// Token: 0x0600008F RID: 143 RVA: 0x00004E5F File Offset: 0x0000305F
		public HkrMqCommunicate(ZmqDataContext context) : base(context)
		{
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00004E8C File Offset: 0x0000308C
		public override bool InitCommuncate()
		{
			try
			{
				int num = 0;
				this.socket = Libzmq.HkrCreate(this.zmqDataContext.ZmqType, ref num);
				bool flag = num != 0;
				if (flag)
				{
					LogHelper.Error("global script start：create hkr mq faild！");
					return false;
				}
				int rcvTimout = this.zmqDataContext.RcvTimout;
				int writeTimeOut = this.zmqDataContext.WriteTimeOut;
				bool flag2 = Libzmq.HkrSetSocketOpt(this.socket, 0, ref rcvTimout, 4) != 0;
				if (flag2)
				{
					LogHelper.Error("global script start：hkr mq receive time set faild！");
					return false;
				}
				bool flag3 = Libzmq.HkrSetSocketOpt(this.socket, 1, ref writeTimeOut, 4) != 0;
				if (flag3)
				{
					LogHelper.Error("global script start：hkr mq write time set faild！");
					return false;
				}
				bool serverOrClient = this.zmqDataContext.ServerOrClient;
				if (serverOrClient)
				{
					int num2 = Libzmq.HkrBind(this.socket, this.zmqDataContext.ConnectionString);
					bool flag4 = num2 != 0;
					if (flag4)
					{
						LogHelper.Error(string.Format("Global Script Start hkr zmq Listen Faild:{0},ReturnCode:{1}", this.zmqDataContext.ConnectionString, num2));
						return false;
					}
				}
				else
				{
					int num2 = Libzmq.HkrConnect(this.socket, this.zmqDataContext.ConnectionString);
					bool flag5 = num2 != 0;
					if (flag5)
					{
						LogHelper.Error(string.Format("Global Script Start hkr zmq Connect Faild:{0},ReturnCode:{1}", this.zmqDataContext.ConnectionString, num2));
						return false;
					}
				}
				bool startReceiveTask = this.zmqDataContext.StartReceiveTask;
				if (startReceiveTask)
				{
					this.zmqTask = Task.Run(async delegate()
					{
						await this.CallReceived();
					});
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("Init hkr ZMQ Error " + ex.ToString());
				return false;
			}
			return true;
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00005060 File Offset: 0x00003260
		public Task CallReceived()
		{
			/*
An exception occurred when decompiling this method (06000091)

ICSharpCode.Decompiler.DecompilerException: Error decompiling System.Threading.Tasks.Task VM.GlobalScript.Support.HkrMqCommunicate::CallReceived()

 ---> System.Collections.Generic.KeyNotFoundException: The given key 'IL_14:' was not present in the dictionary.
   at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
   at ICSharpCode.Decompiler.ILAst.SimpleControlFlow.SimplifyShortCircuit(List`1 body, ILBasicBlock head, Int32 pos) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\SimpleControlFlow.cs:line 262
   at ICSharpCode.Decompiler.ILAst.ILAstOptimizerExtensionMethods.RunOptimization(ILBlock block, Func`4 optimization) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstOptimizer.cs:line 2130
   at ICSharpCode.Decompiler.ILAst.ILAstOptimizer.Optimize(DecompilerContext context, ILBlock method, AutoPropertyProvider autoPropertyProvider, StateMachineKind& stateMachineKind, MethodDef& inlinedMethod, AsyncMethodDebugInfo& asyncInfo, ILAstOptimizationStep abortBeforeStep) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\ILAst\ILAstOptimizer.cs:line 202
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(IEnumerable`1 parameters, MethodDebugInfoBuilder& builder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 123
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 88
   --- End of inner exception stack trace ---
   at ICSharpCode.Decompiler.Ast.AstMethodBodyBuilder.CreateMethodBody(MethodDef methodDef, DecompilerContext context, AutoPropertyProvider autoPropertyProvider, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, StringBuilder sb, MethodDebugInfoBuilder& stmtsBuilder) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstMethodBodyBuilder.cs:line 92
   at ICSharpCode.Decompiler.Ast.AstBuilder.AddMethodBody(EntityDeclaration methodNode, EntityDeclaration& updatedNode, MethodDef method, IEnumerable`1 parameters, Boolean valueParameterIsKeyword, MethodKind methodKind) in D:\a\dnSpy\dnSpy\Extensions\ILSpy.Decompiler\ICSharpCode.Decompiler\ICSharpCode.Decompiler\Ast\AstBuilder.cs:line 1533
*/;
		}

		// Token: 0x06000092 RID: 146 RVA: 0x000050A8 File Offset: 0x000032A8
		public override bool SendData(string msg)
		{
			int num = Libzmq.HkrSend(this.socket, msg, 0);
			return num >= 0;
		}

		// Token: 0x06000093 RID: 147 RVA: 0x000050D0 File Offset: 0x000032D0
		~HkrMqCommunicate()
		{
			this.Dispose(false);
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00005104 File Offset: 0x00003304
		private void Dispose(bool dispose)
		{
			bool dispose2 = this._dispose;
			if (!dispose2)
			{
				if (dispose)
				{
					this._dispose = true;
					try
					{
						bool serverOrClient = this.zmqDataContext.ServerOrClient;
						if (serverOrClient)
						{
						}
						Libzmq.HkrClose(this.socket);
						LogHelper.Info("Dispose zmq success");
					}
					catch (Exception ex)
					{
						LogHelper.Error("Dispose zmq error " + ex.Message);
					}
				}
			}
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00005188 File Offset: 0x00003388
		public override void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x0400009C RID: 156
		private bool _dispose;

		// Token: 0x0400009D RID: 157
		private IntPtr socket;

		// Token: 0x0400009E RID: 158
		private IntPtr receiveBufferPtr = IntPtr.Zero;

		// Token: 0x0400009F RID: 159
		private Task zmqTask;

		// Token: 0x040000A0 RID: 160
		private CancellationTokenSource _tokenSource = new CancellationTokenSource();

		// Token: 0x040000A1 RID: 161
		private const int receiveBufferSize = 10240;

		// Token: 0x040000A2 RID: 162
		private const int readDataTimeOut = 50;

		// Token: 0x040000A3 RID: 163
		private const int receiveTimeOut = 3000;

		// Token: 0x040000A4 RID: 164
		private readonly object lockObj = new object();
	}
}
