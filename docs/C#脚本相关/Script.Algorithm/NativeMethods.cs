using System;
using System.Runtime.InteropServices;

namespace Script.Algorithm
{
	// Token: 0x0200001B RID: 27
	public class NativeMethods
	{
		// Token: 0x06000114 RID: 276
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr zmq_ctx_new();

		// Token: 0x06000115 RID: 277
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr zmq_socket(IntPtr context, int type);

		// Token: 0x06000116 RID: 278
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_close(IntPtr socket);

		// Token: 0x06000117 RID: 279
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_bind(IntPtr socket, string addr);

		// Token: 0x06000118 RID: 280
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_connect(IntPtr socket, string addr);

		// Token: 0x06000119 RID: 281
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_disconnect(IntPtr socket, string addr);

		// Token: 0x0600011A RID: 282
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_setsockopt(IntPtr socket, int option, ref int optval, int optvallen);

		// Token: 0x0600011B RID: 283
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_setsockopt(IntPtr socket, int option, IntPtr optval, int optvallen);

		// Token: 0x0600011C RID: 284
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_getsockopt(IntPtr socket, int option, IntPtr optval, int optvallen);

		// Token: 0x0600011D RID: 285
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_send(IntPtr socket, string msg, uint lenth, int flag);

		// Token: 0x0600011E RID: 286
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_send(IntPtr socket, IntPtr msg, uint lenth, int flag);

		// Token: 0x0600011F RID: 287
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_recv(IntPtr socket, IntPtr buffer, uint len, int flags);

		// Token: 0x06000120 RID: 288
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_ctx_term(IntPtr context);

		// Token: 0x06000121 RID: 289
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_msg_init(IntPtr msg);

		// Token: 0x06000122 RID: 290
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_msg_recv(IntPtr msg, IntPtr socket, int flag);

		// Token: 0x06000123 RID: 291
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int zmq_msg_close(IntPtr msg);

		// Token: 0x06000124 RID: 292
		[DllImport("libzmq-v120-x64-4_3_2.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr zmq_msg_data(IntPtr msg);

		// Token: 0x06000125 RID: 293
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Init(string pLogPath, int enumLogLevel);

		// Token: 0x06000126 RID: 294
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Create(ref IntPtr pSocketHandle, int emWorkMode);

		// Token: 0x06000127 RID: 295
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Close(IntPtr pSocketHandle);

		// Token: 0x06000128 RID: 296
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Bind(IntPtr hSocketHandle, string pAddr);

		// Token: 0x06000129 RID: 297
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Unbind(IntPtr hSocketHandle, string pAddr);

		// Token: 0x0600012A RID: 298
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_SetSocketOpt(IntPtr hSocketHandle, int emOptType, ref int pInParam, int nParamLen);

		// Token: 0x0600012B RID: 299
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_GetSocketOpt(IntPtr hSocketHandle, int emOptType, IntPtr pInParam, ref int nParamLen);

		// Token: 0x0600012C RID: 300
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Connect(IntPtr hSocketHandle, string pAddr);

		// Token: 0x0600012D RID: 301
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Disconnect(IntPtr hSocketHandle, string pAddr);

		// Token: 0x0600012E RID: 302
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Send(IntPtr hSocketHandle, IntPtr pSendBuf, int nSendBufLen, int nSendTimeOut = 0);

		// Token: 0x0600012F RID: 303
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_Recv(IntPtr hSocketHandle, IntPtr pRecvBuf, ref int nRecvBufLen, int nRecvTimeOut = 0);

		// Token: 0x06000130 RID: 304
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int HKR_MQ_RecvEx(IntPtr hSocketHandle, ref IntPtr pRecvBuf, ref int nRecvBufLen, int nRecvTimeOut = 0);

		// Token: 0x06000131 RID: 305
		[DllImport("SMQComm.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern string HKR_MQ_GetStrError(int nErrorCode);

		// Token: 0x040000A8 RID: 168
		private const string libzmq_x86 = "..\\PublicFile\\x86\\libzmq-v120-x86-4_3_2.dll";

		// Token: 0x040000A9 RID: 169
		private const string libzmq_x64 = "libzmq-v120-x64-4_3_2.dll";

		// Token: 0x040000AA RID: 170
		public const string DIIPath = "libzmq-v120-x64-4_3_2.dll";

		// Token: 0x040000AB RID: 171
		private const string hkrzmq = "SMQComm.dll";
	}
}
