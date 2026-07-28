using System;
using System.Runtime.InteropServices;

namespace VM.GlobalScript.Support
{
	// Token: 0x02000025 RID: 37
	public class NativeMethods
	{
		// Token: 0x060000BE RID: 190
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr zmq_ctx_new();

		// Token: 0x060000BF RID: 191
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr zmq_socket(IntPtr context, int type);

		// Token: 0x060000C0 RID: 192
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_close(IntPtr socket);

		// Token: 0x060000C1 RID: 193
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_bind(IntPtr socket, string addr);

		// Token: 0x060000C2 RID: 194
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_connect(IntPtr socket, string addr);

		// Token: 0x060000C3 RID: 195
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_disconnect(IntPtr socket, string addr);

		// Token: 0x060000C4 RID: 196
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_setsockopt(IntPtr socket, int option, ref int optval, int optvallen);

		// Token: 0x060000C5 RID: 197
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_setsockopt(IntPtr socket, int option, IntPtr optval, int optvallen);

		// Token: 0x060000C6 RID: 198
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_getsockopt(IntPtr socket, int option, IntPtr optval, int optvallen);

		// Token: 0x060000C7 RID: 199
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_send(IntPtr socket, string msg, uint lenth, int flag);

		// Token: 0x060000C8 RID: 200
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_send(IntPtr socket, IntPtr msg, uint lenth, int flag);

		// Token: 0x060000C9 RID: 201
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_recv(IntPtr socket, IntPtr buffer, uint len, int flags);

		// Token: 0x060000CA RID: 202
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_ctx_term(IntPtr context);

		// Token: 0x060000CB RID: 203
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_msg_init(IntPtr msg);

		// Token: 0x060000CC RID: 204
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_msg_recv(IntPtr msg, IntPtr socket, int flag);

		// Token: 0x060000CD RID: 205
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_msg_close(IntPtr msg);

		// Token: 0x060000CE RID: 206
		[DllImport("..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr zmq_msg_data(IntPtr msg);

		// Token: 0x060000CF RID: 207
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Create(ref IntPtr pSocketHandle, int emWorkMode);

		// Token: 0x060000D0 RID: 208
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Close(IntPtr pSocketHandle);

		// Token: 0x060000D1 RID: 209
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Bind(IntPtr hSocketHandle, string pAddr);

		// Token: 0x060000D2 RID: 210
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Unbind(IntPtr hSocketHandle, string pAddr);

		// Token: 0x060000D3 RID: 211
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_SetSocketOpt(IntPtr hSocketHandle, int emOptType, ref int pInParam, int nParamLen);

		// Token: 0x060000D4 RID: 212
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_GetSocketOpt(IntPtr hSocketHandle, Libzmq.ENUM_HKR_OPT_TYPE emOptType, ref string pInParam, ref int nParamLen);

		// Token: 0x060000D5 RID: 213
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Connect(IntPtr hSocketHandle, string pAddr);

		// Token: 0x060000D6 RID: 214
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Disconnect(IntPtr hSocketHandle, string pAddr);

		// Token: 0x060000D7 RID: 215
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Send(IntPtr hSocketHandle, IntPtr pSendBuf, int nSendBufLen, int nSendTimeOut = 0);

		// Token: 0x060000D8 RID: 216
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Recv(IntPtr hSocketHandle, IntPtr pRecvBuf, ref int nRecvBufLen, int nRecvTimeOut = 0);

		// Token: 0x060000D9 RID: 217
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_RecvEx(IntPtr hSocketHandle, ref IntPtr pRecvBuf, ref int nRecvBufLen, int nRecvTimeOut = 0);

		// Token: 0x060000DA RID: 218
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern string HKR_MQ_GetStrError(int nErrorCode);

		// Token: 0x060000DB RID: 219
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Init(IntPtr pLogPath, Libzmq.ENUM_HKR_LOG_LEVEL enumLogLevel);

		// Token: 0x040000EC RID: 236
		private const string libzmq_x86 = "..\\PublicFile\\x86\\libzmq-v120-x86-4_3_2.dll";

		// Token: 0x040000ED RID: 237
		private const string libzmq_x64 = "..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll";

		// Token: 0x040000EE RID: 238
		public const string DIIPath = "..\\PublicFile\\x64\\libzmq-v120-x64-4_3_2.dll";

		// Token: 0x040000EF RID: 239
		public const string HkrDLLPath = "SMQComm.dll";
	}
}
