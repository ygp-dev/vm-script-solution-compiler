using System;
using System.Runtime.InteropServices;
using System.Text;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000024 RID: 36
	public class Libzmq
	{
		// Token: 0x060000A3 RID: 163 RVA: 0x000051E4 File Offset: 0x000033E4
		public static IntPtr zmq_ctx_new()
		{
			return NativeMethods.zmq_ctx_new();
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x000051FC File Offset: 0x000033FC
		public static IntPtr zmq_socket(IntPtr pContext, int nType)
		{
			return NativeMethods.zmq_socket(pContext, nType);
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00005218 File Offset: 0x00003418
		public static int zmq_close(IntPtr pSocket)
		{
			return NativeMethods.zmq_close(pSocket);
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00005230 File Offset: 0x00003430
		public static int zmq_bind(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.zmq_bind(pSocket, strAddress);
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x0000524C File Offset: 0x0000344C
		public static int zmq_connect(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.zmq_connect(pSocket, strAddress);
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x00005268 File Offset: 0x00003468
		public static int zmq_disconnect(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.zmq_disconnect(pSocket, strAddress);
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00005284 File Offset: 0x00003484
		public static int zmq_setsockopt(IntPtr pSocket, int nOption, ref int nOptval, int nOptvallen)
		{
			return NativeMethods.zmq_setsockopt(pSocket, nOption, ref nOptval, nOptvallen);
		}

		// Token: 0x060000AA RID: 170 RVA: 0x000052A0 File Offset: 0x000034A0
		public static int zmq_setsockopt(IntPtr pSocket, int nOption, IntPtr pOptval, int nOptvallen)
		{
			return NativeMethods.zmq_setsockopt(pSocket, nOption, pOptval, nOptvallen);
		}

		// Token: 0x060000AB RID: 171 RVA: 0x000052BC File Offset: 0x000034BC
		public static int zmq_send(IntPtr pSocket, [MarshalAs(UnmanagedType.LPStr)] string strMsg, uint nLenth, int nFlag)
		{
			return NativeMethods.zmq_send(pSocket, strMsg, nLenth, nFlag);
		}

		// Token: 0x060000AC RID: 172 RVA: 0x000052D8 File Offset: 0x000034D8
		public static int zmq_sendEx(IntPtr pSocket, string msg, uint nLenth, int nFlag)
		{
			IntPtr intPtr = Marshal.AllocHGlobal((int)nLenth);
			Marshal.Copy(Encoding.UTF8.GetBytes(msg), 0, intPtr, (int)nLenth);
			int result = NativeMethods.zmq_send(pSocket, intPtr, nLenth, nFlag);
			Marshal.FreeHGlobal(intPtr);
			return result;
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00005318 File Offset: 0x00003518
		public static int zmq_recv(IntPtr pSocket, IntPtr pBuffer, uint nLength, int nFlag)
		{
			return NativeMethods.zmq_recv(pSocket, pBuffer, nLength, nFlag);
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00005334 File Offset: 0x00003534
		public static int zmq_ctx_term(IntPtr pSocket)
		{
			return NativeMethods.zmq_ctx_term(pSocket);
		}

		// Token: 0x060000AF RID: 175 RVA: 0x0000534C File Offset: 0x0000354C
		public static int zmq_msg_init(IntPtr pMsg)
		{
			return NativeMethods.zmq_msg_init(pMsg);
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00005364 File Offset: 0x00003564
		public static int zmq_msg_close(IntPtr pMsg)
		{
			return NativeMethods.zmq_msg_close(pMsg);
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x0000537C File Offset: 0x0000357C
		public static IntPtr zmq_msg_data(IntPtr pMsg)
		{
			return NativeMethods.zmq_msg_data(pMsg);
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00005394 File Offset: 0x00003594
		public static int zmq_msg_recv(IntPtr pMsg, IntPtr pSocket, int flag)
		{
			return NativeMethods.zmq_msg_recv(pMsg, pSocket, flag);
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x000053B0 File Offset: 0x000035B0
		public static IntPtr HkrCreate(int nType, ref int nret)
		{
			IntPtr zero = IntPtr.Zero;
			nret = NativeMethods.HKR_MQ_Create(ref zero, nType);
			return zero;
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x000053D4 File Offset: 0x000035D4
		public static int HkrClose(IntPtr pSocket)
		{
			return NativeMethods.HKR_MQ_Close(pSocket);
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x000053EC File Offset: 0x000035EC
		public static int HkrBind(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.HKR_MQ_Bind(pSocket, strAddress);
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00005408 File Offset: 0x00003608
		public static int HkrUnBind(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.HKR_MQ_Unbind(pSocket, strAddress);
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00005424 File Offset: 0x00003624
		public static int HkrConnect(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.HKR_MQ_Connect(pSocket, strAddress);
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00005440 File Offset: 0x00003640
		public static int HkrDisConnect(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.HKR_MQ_Disconnect(pSocket, strAddress);
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x0000545C File Offset: 0x0000365C
		public static int HkrSetSocketOpt(IntPtr pSocket, int nOption, ref int nOptval, int nOptvallen)
		{
			return NativeMethods.HKR_MQ_SetSocketOpt(pSocket, nOption, ref nOptval, nOptvallen);
		}

		// Token: 0x060000BA RID: 186 RVA: 0x00005478 File Offset: 0x00003678
		public static int HkrSend(IntPtr pSocket, string msg, int nFlag)
		{
			bool flag = string.IsNullOrEmpty(msg);
			int result;
			if (flag)
			{
				result = -1;
			}
			else
			{
				byte[] bytes = Encoding.UTF8.GetBytes(msg);
				IntPtr intPtr = Marshal.AllocHGlobal(bytes.Length);
				Marshal.Copy(bytes, 0, intPtr, bytes.Length);
				int num = NativeMethods.HKR_MQ_Send(pSocket, intPtr, bytes.Length, nFlag);
				Marshal.FreeHGlobal(intPtr);
				result = num;
			}
			return result;
		}

		// Token: 0x060000BB RID: 187 RVA: 0x000054D4 File Offset: 0x000036D4
		public static int HkrReceive(IntPtr pSocket, ref IntPtr pBuffer, ref int nLength)
		{
			return NativeMethods.HKR_MQ_RecvEx(pSocket, ref pBuffer, ref nLength, 0);
		}

		// Token: 0x060000BC RID: 188 RVA: 0x000054F0 File Offset: 0x000036F0
		public static int HkrInitLogPath(string dirPath, Libzmq.ENUM_HKR_LOG_LEVEL enumLogLevel = Libzmq.ENUM_HKR_LOG_LEVEL.HKR_LOG_LEVEL_INFO)
		{
			bool flag = string.IsNullOrEmpty(dirPath);
			int result;
			if (flag)
			{
				result = -1;
			}
			else
			{
				byte[] bytes = Encoding.UTF8.GetBytes(dirPath);
				IntPtr intPtr = Marshal.AllocHGlobal(bytes.Length);
				int num = 0;
				try
				{
					Marshal.Copy(bytes, 0, intPtr, bytes.Length);
					num = NativeMethods.HKR_MQ_Init(intPtr, enumLogLevel);
				}
				catch
				{
					return -1;
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
				result = num;
			}
			return result;
		}

		// Token: 0x040000EB RID: 235
		public const int ZmqMsgTSize = 32;

		// Token: 0x02000033 RID: 51
		public enum Socket_Types
		{
			// Token: 0x0400017B RID: 379
			ZMQ_PAIR,
			// Token: 0x0400017C RID: 380
			ZMQ_PUB,
			// Token: 0x0400017D RID: 381
			ZMQ_SUB,
			// Token: 0x0400017E RID: 382
			ZMQ_REQ,
			// Token: 0x0400017F RID: 383
			ZMQ_REP,
			// Token: 0x04000180 RID: 384
			ZMQ_DEALER,
			// Token: 0x04000181 RID: 385
			ZMQ_ROUTER,
			// Token: 0x04000182 RID: 386
			ZMQ_PULL,
			// Token: 0x04000183 RID: 387
			ZMQ_PUSH,
			// Token: 0x04000184 RID: 388
			ZMQ_XPUB,
			// Token: 0x04000185 RID: 389
			ZMQ_XSUB
		}

		// Token: 0x02000034 RID: 52
		public enum Deprecated_Aliases
		{
			// Token: 0x04000187 RID: 391
			ZMQ_XREQ = 5,
			// Token: 0x04000188 RID: 392
			ZMQ_XREP
		}

		// Token: 0x02000035 RID: 53
		public enum Socket_Options
		{
			// Token: 0x0400018A RID: 394
			ZMQ_AFFINITY = 4,
			// Token: 0x0400018B RID: 395
			ZMQ_IDENTITY,
			// Token: 0x0400018C RID: 396
			ZMQ_SUBSCRIBE,
			// Token: 0x0400018D RID: 397
			ZMQ_UNSUBSCRIBE,
			// Token: 0x0400018E RID: 398
			ZMQ_RATE,
			// Token: 0x0400018F RID: 399
			ZMQ_RECOVERY_IVL,
			// Token: 0x04000190 RID: 400
			ZMQ_SNDBUF = 11,
			// Token: 0x04000191 RID: 401
			ZMQ_RCVBUF,
			// Token: 0x04000192 RID: 402
			ZMQ_RCVMORE,
			// Token: 0x04000193 RID: 403
			ZMQ_FD,
			// Token: 0x04000194 RID: 404
			ZMQ_EVENTS,
			// Token: 0x04000195 RID: 405
			ZMQ_TYPE,
			// Token: 0x04000196 RID: 406
			ZMQ_LINGER,
			// Token: 0x04000197 RID: 407
			ZMQ_RECONNECT_IVL,
			// Token: 0x04000198 RID: 408
			ZMQ_BACKLOG,
			// Token: 0x04000199 RID: 409
			ZMQ_RECONNECT_IVL_MAX = 21,
			// Token: 0x0400019A RID: 410
			ZMQ_MAXMSGSIZE,
			// Token: 0x0400019B RID: 411
			ZMQ_SNDHWM,
			// Token: 0x0400019C RID: 412
			ZMQ_RCVHWM,
			// Token: 0x0400019D RID: 413
			ZMQ_MULTICAST_HOPS,
			// Token: 0x0400019E RID: 414
			ZMQ_RCVTIMEO = 27,
			// Token: 0x0400019F RID: 415
			ZMQ_SNDTIMEO,
			// Token: 0x040001A0 RID: 416
			ZMQ_IPV4ONLY = 31,
			// Token: 0x040001A1 RID: 417
			ZMQ_LAST_ENDPOINT,
			// Token: 0x040001A2 RID: 418
			ZMQ_ROUTER_MANDATORY,
			// Token: 0x040001A3 RID: 419
			ZMQ_TCP_KEEPALIVE,
			// Token: 0x040001A4 RID: 420
			ZMQ_TCP_KEEPALIVE_CNT,
			// Token: 0x040001A5 RID: 421
			ZMQ_TCP_KEEPALIVE_IDLE,
			// Token: 0x040001A6 RID: 422
			ZMQ_TCP_KEEPALIVE_INTVL,
			// Token: 0x040001A7 RID: 423
			ZMQ_TCP_ACCEPT_FILTER,
			// Token: 0x040001A8 RID: 424
			ZMQ_DELAY_ATTACH_ON_CONNECT,
			// Token: 0x040001A9 RID: 425
			ZMQ_XPUB_VERBOSE
		}

		// Token: 0x02000036 RID: 54
		public enum SocketFlags
		{
			// Token: 0x040001AB RID: 427
			None,
			// Token: 0x040001AC RID: 428
			DontWait,
			// Token: 0x040001AD RID: 429
			SendMore
		}

		// Token: 0x02000037 RID: 55
		public enum ENUM_HKR_MQ_MODE
		{
			// Token: 0x040001AF RID: 431
			HKR_MQ_MODE_PAIR_C,
			// Token: 0x040001B0 RID: 432
			HKR_MQ_MODE_PAIR_S,
			// Token: 0x040001B1 RID: 433
			HKR_MQ_MODE_PUB,
			// Token: 0x040001B2 RID: 434
			HKR_MQ_MODE_SUB,
			// Token: 0x040001B3 RID: 435
			HKR_MQ_MODE_REP,
			// Token: 0x040001B4 RID: 436
			HKR_MQ_MODE_REQ,
			// Token: 0x040001B5 RID: 437
			HKR_MQ_MODE_COUNT
		}

		// Token: 0x02000038 RID: 56
		public enum ENUM_HKR_OPT_TYPE
		{
			// Token: 0x040001B7 RID: 439
			HKR_OPT_TYPE_ReadTimeOut,
			// Token: 0x040001B8 RID: 440
			HKR_OPT_TYPE_WriteTimeOut,
			// Token: 0x040001B9 RID: 441
			HKR_OPT_TYPE_LingerTime,
			// Token: 0x040001BA RID: 442
			HKR_OPT_TYPE_Subscribe,
			// Token: 0x040001BB RID: 443
			HKR_OPT_TYPE_Unsubscribe,
			// Token: 0x040001BC RID: 444
			HKR_OPT_TYPE_MAX_COUNT
		}

		// Token: 0x02000039 RID: 57
		public enum ENUM_HKR_LOG_LEVEL
		{
			// Token: 0x040001BE RID: 446
			HKR_LOG_LEVEL_TRACE,
			// Token: 0x040001BF RID: 447
			HKR_LOG_LEVEL_DEBUG,
			// Token: 0x040001C0 RID: 448
			HKR_LOG_LEVEL_INFO,
			// Token: 0x040001C1 RID: 449
			HKR_LOG_LEVEL_WARN,
			// Token: 0x040001C2 RID: 450
			HKR_LOG_LEVEL_CRITICAL,
			// Token: 0x040001C3 RID: 451
			HKR_LOG_LEVEL_ERROR,
			// Token: 0x040001C4 RID: 452
			HKR_LOG_LEVEL_OFF
		}
	}
}
