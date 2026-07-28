using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Script.Algorithm
{
	// Token: 0x0200001A RID: 26
	public class Libzmq
	{
		// Token: 0x060000F8 RID: 248 RVA: 0x0000620C File Offset: 0x0000440C
		public static IntPtr zmq_ctx_new()
		{
			return NativeMethods.zmq_ctx_new();
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x00006224 File Offset: 0x00004424
		public static IntPtr zmq_socket(IntPtr pContext, int nType)
		{
			return NativeMethods.zmq_socket(pContext, nType);
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00006240 File Offset: 0x00004440
		public static int zmq_close(IntPtr pSocket)
		{
			return NativeMethods.zmq_close(pSocket);
		}

		// Token: 0x060000FB RID: 251 RVA: 0x00006258 File Offset: 0x00004458
		public static int zmq_bind(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.zmq_bind(pSocket, strAddress);
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00006274 File Offset: 0x00004474
		public static int zmq_connect(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.zmq_connect(pSocket, strAddress);
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00006290 File Offset: 0x00004490
		public static int zmq_disconnect(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.zmq_disconnect(pSocket, strAddress);
		}

		// Token: 0x060000FE RID: 254 RVA: 0x000062AC File Offset: 0x000044AC
		public static int zmq_setsockopt(IntPtr pSocket, int nOption, ref int nOptval, int nOptvallen)
		{
			return NativeMethods.zmq_setsockopt(pSocket, nOption, ref nOptval, nOptvallen);
		}

		// Token: 0x060000FF RID: 255 RVA: 0x000062C8 File Offset: 0x000044C8
		public static int zmq_setsockopt(IntPtr pSocket, int nOption, IntPtr pOptval, int nOptvallen)
		{
			return NativeMethods.zmq_setsockopt(pSocket, nOption, pOptval, nOptvallen);
		}

		// Token: 0x06000100 RID: 256 RVA: 0x000062E4 File Offset: 0x000044E4
		public static int zmq_send(IntPtr pSocket, [MarshalAs(UnmanagedType.LPStr)] string strMsg, uint nLenth, int nFlag)
		{
			return NativeMethods.zmq_send(pSocket, strMsg, nLenth, nFlag);
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00006300 File Offset: 0x00004500
		public static int zmq_sendEx(IntPtr pSocket, string msg, uint nLenth, int nFlag)
		{
			IntPtr intPtr = Marshal.AllocHGlobal((int)nLenth);
			Marshal.Copy(Encoding.UTF8.GetBytes(msg), 0, intPtr, (int)nLenth);
			int result = NativeMethods.zmq_send(pSocket, intPtr, nLenth, nFlag);
			Marshal.FreeHGlobal(intPtr);
			return result;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00006340 File Offset: 0x00004540
		public static int zmq_recv(IntPtr pSocket, IntPtr pBuffer, uint nLength, int nFlag)
		{
			return NativeMethods.zmq_recv(pSocket, pBuffer, nLength, nFlag);
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000635C File Offset: 0x0000455C
		public static int zmq_ctx_term(IntPtr pSocket)
		{
			return NativeMethods.zmq_ctx_term(pSocket);
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00006374 File Offset: 0x00004574
		public static int zmq_msg_init(IntPtr pMsg)
		{
			return NativeMethods.zmq_msg_init(pMsg);
		}

		// Token: 0x06000105 RID: 261 RVA: 0x0000638C File Offset: 0x0000458C
		public static int zmq_msg_close(IntPtr pMsg)
		{
			return NativeMethods.zmq_msg_close(pMsg);
		}

		// Token: 0x06000106 RID: 262 RVA: 0x000063A4 File Offset: 0x000045A4
		public static IntPtr zmq_msg_data(IntPtr pMsg)
		{
			return NativeMethods.zmq_msg_data(pMsg);
		}

		// Token: 0x06000107 RID: 263 RVA: 0x000063BC File Offset: 0x000045BC
		public static int zmq_msg_recv(IntPtr pMsg, IntPtr pSocket, int flag)
		{
			return NativeMethods.zmq_msg_recv(pMsg, pSocket, flag);
		}

		// Token: 0x06000108 RID: 264 RVA: 0x000063D8 File Offset: 0x000045D8
		public static int HkrInit(string logpath, int errorLevel)
		{
			return NativeMethods.HKR_MQ_Init(logpath, errorLevel);
		}

		// Token: 0x06000109 RID: 265 RVA: 0x000063F4 File Offset: 0x000045F4
		public static IntPtr HkrCreate(int nType, ref int nret)
		{
			IntPtr zero = IntPtr.Zero;
			nret = NativeMethods.HKR_MQ_Create(ref zero, nType);
			return zero;
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00006418 File Offset: 0x00004618
		public static int HkrClose(IntPtr pSocket)
		{
			return NativeMethods.HKR_MQ_Close(pSocket);
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00006430 File Offset: 0x00004630
		public static int HkrBind(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.HKR_MQ_Bind(pSocket, strAddress);
		}

		// Token: 0x0600010C RID: 268 RVA: 0x0000644C File Offset: 0x0000464C
		public static int HkrUnBind(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.HKR_MQ_Unbind(pSocket, strAddress);
		}

		// Token: 0x0600010D RID: 269 RVA: 0x00006468 File Offset: 0x00004668
		public static int HkrConnect(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.HKR_MQ_Connect(pSocket, strAddress);
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00006484 File Offset: 0x00004684
		public static int HkrDisConnect(IntPtr pSocket, string strAddress)
		{
			return NativeMethods.HKR_MQ_Disconnect(pSocket, strAddress);
		}

		// Token: 0x0600010F RID: 271 RVA: 0x000064A0 File Offset: 0x000046A0
		public static int HkrSetSocketOpt(IntPtr pSocket, int nOption, ref int nOptval, int nOptvallen)
		{
			return NativeMethods.HKR_MQ_SetSocketOpt(pSocket, nOption, ref nOptval, nOptvallen);
		}

		// Token: 0x06000110 RID: 272 RVA: 0x000064BC File Offset: 0x000046BC
		public static int HkrGetSocketOpt(IntPtr pSocket, int nOption, ref string nOptval)
		{
			int cb = 40;
			IntPtr intPtr = Marshal.AllocHGlobal(cb);
			int num = NativeMethods.HKR_MQ_GetSocketOpt(pSocket, nOption, intPtr, ref cb);
			int result;
			if (num != 0)
			{
				result = num;
			}
			else
			{
				nOptval = Marshal.PtrToStringAnsi(intPtr);
				Marshal.FreeHGlobal(intPtr);
				result = num;
			}
			return result;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00006504 File Offset: 0x00004704
		public static int HkrSend(IntPtr pSocket, string msg, int nFlag)
		{
			int result;
			if (string.IsNullOrEmpty(msg))
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

		// Token: 0x06000112 RID: 274 RVA: 0x00006560 File Offset: 0x00004760
		public static int HkrReceive(IntPtr pSocket, ref IntPtr pBuffer, ref int nLength)
		{
			return NativeMethods.HKR_MQ_RecvEx(pSocket, ref pBuffer, ref nLength, 0);
		}

		// Token: 0x040000A7 RID: 167
		public const int ZmqMsgTSize = 32;
	}
}
