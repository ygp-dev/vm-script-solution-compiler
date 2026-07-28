using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Apps.Json;
using Microsoft.CSharp.RuntimeBinder;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000026 RID: 38
	public class ZmqCommunicate : BaseZmqCommunicate
	{
		// Token: 0x060000DD RID: 221 RVA: 0x00005574 File Offset: 0x00003774
		public ZmqCommunicate(ZmqDataContext context) : base(context)
		{
		}

		// Token: 0x060000DE RID: 222 RVA: 0x00005598 File Offset: 0x00003798
		public override bool InitCommuncate()
		{
			try
			{
				this.context = Libzmq.zmq_ctx_new();
				this.socket = Libzmq.zmq_socket(this.context, this.zmqDataContext.ZmqType);
				this.zmqMsg = Marshal.AllocHGlobal(32);
				int rcvTimout = this.zmqDataContext.RcvTimout;
				int writeTimeOut = this.zmqDataContext.WriteTimeOut;
				bool flag = Libzmq.zmq_setsockopt(this.socket, 27, ref rcvTimout, 4) != 0;
				if (flag)
				{
					LogHelper.Error("global script start：mq receive time set faild！");
					return false;
				}
				bool flag2 = Libzmq.zmq_setsockopt(this.socket, 28, ref writeTimeOut, 4) != 0;
				if (flag2)
				{
					LogHelper.Error("global script start：mq write time set faild！");
					return false;
				}
				bool serverOrClient = this.zmqDataContext.ServerOrClient;
				if (serverOrClient)
				{
					int num = Libzmq.zmq_bind(this.socket, this.zmqDataContext.ConnectionString);
					bool flag3 = num != 0;
					if (flag3)
					{
						LogHelper.Error(string.Format("Global Script Start Zmq Listen Faild:{0},ReturnCode:{1}", this.zmqDataContext.ConnectionString, num));
						return false;
					}
				}
				else
				{
					int num = Libzmq.zmq_connect(this.socket, this.zmqDataContext.ConnectionString);
					bool flag4 = num != 0;
					if (flag4)
					{
						LogHelper.Error(string.Format("Global Script Start Zmq Connect Faild:{0},ReturnCode:{1}", this.zmqDataContext.ConnectionString, num));
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
				LogHelper.Error("Init ZMQ Error " + ex.ToString());
				return false;
			}
			return true;
		}

		// Token: 0x060000DF RID: 223 RVA: 0x00005768 File Offset: 0x00003968
		public async Task CallReceived()
		{
			while (!this._dispose)
			{
				IntPtr bufferPtr = Marshal.AllocHGlobal(10240);
				object obj = null;
				int num = 0;
				try
				{
					try
					{
						int bytesReceived = Libzmq.zmq_recv(this.socket, bufferPtr, 10240U, 0);
						bool flag = bytesReceived > 0;
						if (flag)
						{
							bool flag2 = bytesReceived > 10240;
							if (flag2)
							{
								LogHelper.Info("Receive data is too long, message maybe cutoff");
								goto IL_13F;
							}
							byte[] buffer = new byte[bytesReceived];
							Marshal.Copy(bufferPtr, buffer, 0, bytesReceived);
							string message = this.zmqDataContext.Encod.GetString(buffer);
							bool flag3 = this.GetReceiveData != null;
							if (flag3)
							{
								this.GetReceiveData(message);
							}
							buffer = null;
							message = null;
						}
					}
					catch (Exception ex2)
					{
						Exception ex = ex2;
						LogHelper.Error("Receive data zmq error " + ex.Message);
					}
					goto IL_154;
					IL_13F:
					num = 1;
				}
				catch (object obj2)
				{
					obj = obj2;
				}
				IL_154:
				Marshal.FreeCoTaskMem(bufferPtr);
				await Task.Delay(TimeSpan.FromMilliseconds(30.0), this._tokenSource.Token);
				object obj2 = obj;
				if (obj2 != null)
				{
					Exception ex2 = obj2 as Exception;
					if (ex2 == null)
					{
						throw obj2;
					}
					ExceptionDispatchInfo.Capture(ex2).Throw();
				}
				if (num != 1)
				{
					obj = null;
				}
			}
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x000057B0 File Offset: 0x000039B0
		public async Task CallReceivedEx()
		{
			while (!this._dispose)
			{
				int rc = Libzmq.zmq_msg_init(this.zmqMsg);
				bool flag = rc == -1;
				if (!flag)
				{
					int bytesReceived = Libzmq.zmq_msg_recv(this.zmqMsg, this.socket, 0);
					bool flag2 = bytesReceived >= 0;
					if (flag2)
					{
						byte[] buffer = new byte[bytesReceived];
						Marshal.Copy(Libzmq.zmq_msg_data(this.zmqMsg), buffer, 0, bytesReceived);
						string msg = this.zmqDataContext.Encod.GetString(buffer);
						bool flag3 = this.GetReceiveData != null;
						if (flag3)
						{
							this.GetReceiveData(msg);
						}
						buffer = null;
						msg = null;
					}
					Libzmq.zmq_msg_close(this.zmqMsg);
					await Task.Delay(TimeSpan.FromMilliseconds(30.0), this._tokenSource.Token);
				}
			}
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x000057F8 File Offset: 0x000039F8
		~ZmqCommunicate()
		{
			this.Dispose(false);
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0000582C File Offset: 0x00003A2C
		public uint AyscSendMessage(string msg, uint seqID, int cmdID, int receiveTime)
		{
			object obj = this.lockObj;
			uint result;
			lock (obj)
			{
				uint num = 0U;
				try
				{
					bool flag2 = !this.SendData(msg);
					if (flag2)
					{
						return 3758096647U;
					}
					int num2 = 0;
					while (num2 < 3000 && !this._dispose)
					{
						IntPtr intPtr = Marshal.AllocHGlobal(10240);
						try
						{
							int num3 = Libzmq.zmq_recv(this.socket, intPtr, 10240U, 0);
							num2 += 50;
							bool flag3 = num3 > 0;
							if (flag3)
							{
								bool flag4 = num3 > 10240;
								if (flag4)
								{
									LogHelper.Info("Receive data is too long, message maybe cutoff");
									num = 3758096650U;
									break;
								}
								byte[] array = new byte[num3];
								Marshal.Copy(intPtr, array, 0, num3);
								string @string = this.zmqDataContext.Encod.GetString(array);
								int num4 = 0;
								uint num5 = 0U;
								num = this.PraseJsonData(@string, out num5, out num4);
								bool flag5 = num > 0U;
								if (flag5)
								{
									LogHelper.Error("PraseJsonData error,iret = " + num);
									break;
								}
								bool flag6 = num5 != seqID;
								if (flag6)
								{
									LogHelper.Info(string.Format("PraseJsonData fail, protocol error. seqID{0} != seqRet{1}", seqID, num5));
								}
								else
								{
									bool flag7 = num4 != cmdID;
									if (flag7)
									{
										LogHelper.Error(string.Format("PraseJsonData fail, protocol error. cmdID{0} != cmdRet{1}", cmdID, num4));
										break;
									}
								}
							}
							else
							{
								num = 3758096648U;
							}
						}
						catch (Exception ex)
						{
							LogHelper.Error("Receive data zmq error " + ex.Message);
						}
						finally
						{
							Marshal.FreeHGlobal(intPtr);
						}
					}
					bool flag8 = num == 3758096648U;
					if (flag8)
					{
						LogHelper.Error("Receive time out " + 3000);
					}
				}
				catch (Exception ex2)
				{
					LogHelper.Error("AyscSendMessage Error,error = " + ex2.Message);
					num = 3758096639U;
				}
				result = num;
			}
			return result;
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00005AB0 File Offset: 0x00003CB0
		private uint PraseJsonData(string strCommandMsg, out uint seqID, out int cmdID)
		{
			uint result = 0U;
			seqID = 0U;
			cmdID = 0;
			try
			{
				object arg = JsonConvert.DeserializeObject(strCommandMsg);
				if (ZmqCommunicate.<>o__16.<>p__1 == null)
				{
					ZmqCommunicate.<>o__16.<>p__1 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(ZmqCommunicate), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
					}));
				}
				Func<CallSite, object, bool> target = ZmqCommunicate.<>o__16.<>p__1.Target;
				CallSite <>p__ = ZmqCommunicate.<>o__16.<>p__1;
				if (ZmqCommunicate.<>o__16.<>p__0 == null)
				{
					ZmqCommunicate.<>o__16.<>p__0 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Equal, typeof(ZmqCommunicate), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
					}));
				}
				bool flag = target(<>p__, ZmqCommunicate.<>o__16.<>p__0.Target(ZmqCommunicate.<>o__16.<>p__0, arg, null));
				if (flag)
				{
					result = 3758096385U;
				}
				else
				{
					if (ZmqCommunicate.<>o__16.<>p__4 == null)
					{
						ZmqCommunicate.<>o__16.<>p__4 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(ZmqCommunicate), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, bool> target2 = ZmqCommunicate.<>o__16.<>p__4.Target;
					CallSite <>p__2 = ZmqCommunicate.<>o__16.<>p__4;
					if (ZmqCommunicate.<>o__16.<>p__3 == null)
					{
						ZmqCommunicate.<>o__16.<>p__3 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.Equal, typeof(ZmqCommunicate), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
						}));
					}
					Func<CallSite, object, object, object> target3 = ZmqCommunicate.<>o__16.<>p__3.Target;
					CallSite <>p__3 = ZmqCommunicate.<>o__16.<>p__3;
					if (ZmqCommunicate.<>o__16.<>p__2 == null)
					{
						ZmqCommunicate.<>o__16.<>p__2 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(ZmqCommunicate), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					bool flag2 = target2(<>p__2, target3(<>p__3, ZmqCommunicate.<>o__16.<>p__2.Target(ZmqCommunicate.<>o__16.<>p__2, arg), null));
					if (flag2)
					{
						result = 3758096385U;
					}
					else
					{
						if (ZmqCommunicate.<>o__16.<>p__7 == null)
						{
							ZmqCommunicate.<>o__16.<>p__7 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(ZmqCommunicate)));
						}
						Func<CallSite, object, string> target4 = ZmqCommunicate.<>o__16.<>p__7.Target;
						CallSite <>p__4 = ZmqCommunicate.<>o__16.<>p__7;
						if (ZmqCommunicate.<>o__16.<>p__6 == null)
						{
							ZmqCommunicate.<>o__16.<>p__6 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "command", typeof(ZmqCommunicate), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target5 = ZmqCommunicate.<>o__16.<>p__6.Target;
						CallSite <>p__5 = ZmqCommunicate.<>o__16.<>p__6;
						if (ZmqCommunicate.<>o__16.<>p__5 == null)
						{
							ZmqCommunicate.<>o__16.<>p__5 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(ZmqCommunicate), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						string s = target4(<>p__4, target5(<>p__5, ZmqCommunicate.<>o__16.<>p__5.Target(ZmqCommunicate.<>o__16.<>p__5, arg)));
						if (ZmqCommunicate.<>o__16.<>p__10 == null)
						{
							ZmqCommunicate.<>o__16.<>p__10 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(ZmqCommunicate)));
						}
						Func<CallSite, object, string> target6 = ZmqCommunicate.<>o__16.<>p__10.Target;
						CallSite <>p__6 = ZmqCommunicate.<>o__16.<>p__10;
						if (ZmqCommunicate.<>o__16.<>p__9 == null)
						{
							ZmqCommunicate.<>o__16.<>p__9 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "seqId", typeof(ZmqCommunicate), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target7 = ZmqCommunicate.<>o__16.<>p__9.Target;
						CallSite <>p__7 = ZmqCommunicate.<>o__16.<>p__9;
						if (ZmqCommunicate.<>o__16.<>p__8 == null)
						{
							ZmqCommunicate.<>o__16.<>p__8 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(ZmqCommunicate), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						string s2 = target6(<>p__6, target7(<>p__7, ZmqCommunicate.<>o__16.<>p__8.Target(ZmqCommunicate.<>o__16.<>p__8, arg)));
						if (ZmqCommunicate.<>o__16.<>p__13 == null)
						{
							ZmqCommunicate.<>o__16.<>p__13 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(ZmqCommunicate)));
						}
						Func<CallSite, object, string> target8 = ZmqCommunicate.<>o__16.<>p__13.Target;
						CallSite <>p__8 = ZmqCommunicate.<>o__16.<>p__13;
						if (ZmqCommunicate.<>o__16.<>p__12 == null)
						{
							ZmqCommunicate.<>o__16.<>p__12 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "errorCode", typeof(ZmqCommunicate), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object> target9 = ZmqCommunicate.<>o__16.<>p__12.Target;
						CallSite <>p__9 = ZmqCommunicate.<>o__16.<>p__12;
						if (ZmqCommunicate.<>o__16.<>p__11 == null)
						{
							ZmqCommunicate.<>o__16.<>p__11 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "head", typeof(ZmqCommunicate), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						string text = target8(<>p__8, target9(<>p__9, ZmqCommunicate.<>o__16.<>p__11.Target(ZmqCommunicate.<>o__16.<>p__11, arg)));
						uint.TryParse(s2, out seqID);
						int.TryParse(s, out cmdID);
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error("PraseJsonData Error,error = " + ex.Message);
				result = 3758096639U;
			}
			return result;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00005F6C File Offset: 0x0000416C
		public override bool SendData(string msg)
		{
			int num = Libzmq.zmq_sendEx(this.socket, msg, (uint)this.zmqDataContext.Encod.GetBytes(msg).Length, 0);
			return num >= 0;
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x00005FA8 File Offset: 0x000041A8
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
						bool flag = this.zmqMsg != IntPtr.Zero;
						if (flag)
						{
							Marshal.FreeHGlobal(this.zmqMsg);
							this.zmqMsg = IntPtr.Zero;
						}
						Libzmq.zmq_close(this.socket);
						Libzmq.zmq_ctx_term(this.context);
						LogHelper.Info("Dispose zmq success");
					}
					catch (Exception ex)
					{
						LogHelper.Error("Dispose zmq error " + ex.Message);
					}
				}
			}
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00006050 File Offset: 0x00004250
		public override void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x040000F0 RID: 240
		private bool _dispose;

		// Token: 0x040000F1 RID: 241
		private IntPtr context;

		// Token: 0x040000F2 RID: 242
		private IntPtr socket;

		// Token: 0x040000F3 RID: 243
		private Task zmqTask;

		// Token: 0x040000F4 RID: 244
		private IntPtr zmqMsg;

		// Token: 0x040000F5 RID: 245
		private CancellationTokenSource _tokenSource = new CancellationTokenSource();

		// Token: 0x040000F6 RID: 246
		private const int receiveBufferSize = 10240;

		// Token: 0x040000F7 RID: 247
		private const int readDataTimeOut = 50;

		// Token: 0x040000F8 RID: 248
		private const int receiveTimeOut = 3000;

		// Token: 0x040000F9 RID: 249
		private readonly object lockObj = new object();
	}
}
