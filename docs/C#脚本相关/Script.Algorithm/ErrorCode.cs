using System;
using System.Collections.Generic;

namespace Script.Algorithm
{
	// Token: 0x0200002D RID: 45
	public class ErrorCode
	{
		// Token: 0x060001D8 RID: 472 RVA: 0x0000B7A8 File Offset: 0x000099A8
		public static string GetErrorInfo(uint nErrorCode)
		{
			string result;
			if (ErrorCode.ErrorDesption.ContainsKey(nErrorCode))
			{
				result = ErrorCode.ErrorDesption[nErrorCode];
			}
			else
			{
				result = "未知错误信息";
			}
			return result;
		}

		// Token: 0x04000120 RID: 288
		public const int IMVS_EC_OK = 0;

		// Token: 0x04000121 RID: 289
		public const int IMVS_EC_VERSION = -536870912;

		// Token: 0x04000122 RID: 290
		public const int IMVS_EC_PARAM = -536870911;

		// Token: 0x04000123 RID: 291
		public const int IMVS_EC_RESOURCE_CREATE = -536870910;

		// Token: 0x04000124 RID: 292
		public const int IMVS_EC_OUTOFMEMORY = -536870909;

		// Token: 0x04000125 RID: 293
		public const int IMVS_EC_POINTER_CAST = -536870908;

		// Token: 0x04000126 RID: 294
		public const int IMVS_EC_MEMORY_BEYOND_THRESHOLD = -536870907;

		// Token: 0x04000127 RID: 295
		public const int IMVS_EC_INVALID_HANDLE = -536870906;

		// Token: 0x04000128 RID: 296
		public const int IMVS_EC_NOT_SUPPORT = -536870905;

		// Token: 0x04000129 RID: 297
		public const int IMVS_EC_NOT_READY = -536870904;

		// Token: 0x0400012A RID: 298
		public const int IMVS_EC_WAIT_TIMEOUT = -536870903;

		// Token: 0x0400012B RID: 299
		public const int IMVS_EC_NULL_PTR = -536870902;

		// Token: 0x0400012C RID: 300
		public const int IMVS_EC_PROCESS_START_FAIL = -536870901;

		// Token: 0x0400012D RID: 301
		public const int IMVS_EC_PROCESS_ALREADY_START = -536870900;

		// Token: 0x0400012E RID: 302
		public const int IMVS_EC_SOLUTION_LOADING = -536870899;

		// Token: 0x0400012F RID: 303
		public const int IMVS_EC_SOLUTION_SAVING = -536870898;

		// Token: 0x04000130 RID: 304
		public const int IMVS_EC_CALL_ORDER = -536870897;

		// Token: 0x04000131 RID: 305
		public const int IMVS_EC_LOAD_LIBRARY = -536870896;

		// Token: 0x04000132 RID: 306
		public const int IMVS_EC_GET_FUN_ADDRESS = -536870895;

		// Token: 0x04000133 RID: 307
		public const int IMVS_EC_PARAM_BUF_LEN = -536870894;

		// Token: 0x04000134 RID: 308
		public const int IMVS_EC_GETTING_PLAT_INFO = -536870893;

		// Token: 0x04000135 RID: 309
		public const int IMVS_EC_INDEX_OUT_OF_BOUNDARY = -536870892;

		// Token: 0x04000136 RID: 310
		public const int IMVS_EC_OPEN_FILEMAPPING = -536870891;

		// Token: 0x04000137 RID: 311
		public const int IMVS_EC_THREAD_START_FAIL = -536870890;

		// Token: 0x04000138 RID: 312
		public const int IMVS_EC_PROTOCOL = -536870889;

		// Token: 0x04000139 RID: 313
		public const int IMVS_EC_DATA_ERROR = -536870888;

		// Token: 0x0400013A RID: 314
		public const int IMVS_EC_NOT_IMPLEMENTED = -536870887;

		// Token: 0x0400013B RID: 315
		public const int IMVS_EC_DATA_OVER_SIZE = -536870886;

		// Token: 0x0400013C RID: 316
		public const int IMVS_EC_PRECONDITION = -536870885;

		// Token: 0x0400013D RID: 317
		public const int IMVS_EC_RUNTIME = -536870884;

		// Token: 0x0400013E RID: 318
		public const int IMVS_EC_UNKNOWN = -536870657;

		// Token: 0x0400013F RID: 319
		public const int IMVS_EC_COMMU_SOCKET_CREAT = -536870656;

		// Token: 0x04000140 RID: 320
		public const int IMVS_EC_COMMU_SOCKET_INVALID = -536870655;

		// Token: 0x04000141 RID: 321
		public const int IMVS_EC_COMMU_SERIAL_OPEN = -536870654;

		// Token: 0x04000142 RID: 322
		public const int IMVS_EC_COMMU_INVALID_ADDRESS = -536870653;

		// Token: 0x04000143 RID: 323
		public const int IMVS_EC_COMMU_ADDRESS_INUSE = -536870652;

		// Token: 0x04000144 RID: 324
		public const int IMVS_EC_COMMU_CONNECT = -536870651;

		// Token: 0x04000145 RID: 325
		public const int IMVS_EC_COMMU_DISCONNECT = -536870650;

		// Token: 0x04000146 RID: 326
		public const int IMVS_EC_COMMU_SEND_FAIL = -536870649;

		// Token: 0x04000147 RID: 327
		public const int IMVS_EC_COMMU_RECV_TIMEOUT = -536870648;

		// Token: 0x04000148 RID: 328
		public const int IMVS_EC_COMMU_MESSAGE_FORMAT = -536870647;

		// Token: 0x04000149 RID: 329
		public const int IMVS_EC_COMMU_MSG_TOO_LONG = -536870646;

		// Token: 0x0400014A RID: 330
		public const int IMVS_EC_COMMU_HEARTBEAT = -536870645;

		// Token: 0x0400014B RID: 331
		public const int IMVS_EC_FILE_MKDIR = -536870400;

		// Token: 0x0400014C RID: 332
		public const int IMVS_EC_FILE_OPEN = -536870399;

		// Token: 0x0400014D RID: 333
		public const int IMVS_EC_FILE_SAVE = -536870398;

		// Token: 0x0400014E RID: 334
		public const int IMVS_EC_FILE_NOT_FOUND = -536870397;

		// Token: 0x0400014F RID: 335
		public const int IMVS_EC_FILE_FORMAT = -536870396;

		// Token: 0x04000150 RID: 336
		public const int IMVS_EC_FILE_COMPRESS = -536870395;

		// Token: 0x04000151 RID: 337
		public const int IMVS_EC_FILE_DECOMPRESS = -536870394;

		// Token: 0x04000152 RID: 338
		public const int IMVS_EC_FILE_XML_ELEMENT = -536870393;

		// Token: 0x04000153 RID: 339
		public const int IMVS_EC_FILE_XML_ATTRIBUTE = -536870392;

		// Token: 0x04000154 RID: 340
		public const int IMVS_EC_FILE_PATH_TOO_LONG = -536870391;

		// Token: 0x04000155 RID: 341
		public const int IMVS_EC_FILE_BE_OCCUPIED = -536870390;

		// Token: 0x04000156 RID: 342
		public const int IMVS_EC_AUTH_SOLU_PASSWORD = -536868864;

		// Token: 0x04000157 RID: 343
		public const int IMVS_EC_AUTH_USER_PASSWORD = -536868863;

		// Token: 0x04000158 RID: 344
		public const int IMVS_EC_MODULE_GLOBALSCRIPT_PROCESSING = -536870113;

		// Token: 0x04000159 RID: 345
		public const int IMVS_EC_MODULE_GLOBALSCRIPT_COMPILE_FAIL = -536870112;

		// Token: 0x0400015A RID: 346
		public static Dictionary<uint, string> ErrorDesption = new Dictionary<uint, string>
		{
			{
				0U,
				"无错误"
			},
			{
				3758096384U,
				"版本错误"
			},
			{
				3758096385U,
				"参数错误"
			},
			{
				3758096386U,
				"资源创建失败"
			},
			{
				3758096387U,
				"内存不足"
			},
			{
				3758096388U,
				"指针转换"
			},
			{
				3758096389U,
				"系统内存使用率超过阈值"
			},
			{
				3758096390U,
				"句柄无效"
			},
			{
				3758096391U,
				"操作不支持"
			},
			{
				3758096392U,
				"资源未初始化或未准备好"
			},
			{
				3758096393U,
				"等待超时"
			},
			{
				3758096394U,
				"指针为空"
			},
			{
				3758096395U,
				"进程启动失败"
			},
			{
				3758096396U,
				"进程已启动"
			},
			{
				3758096397U,
				"正在加载方案"
			},
			{
				3758096398U,
				"正在保存方案"
			},
			{
				3758096399U,
				"接口调用顺序错误"
			},
			{
				3758096400U,
				"动态库加载失败"
			},
			{
				3758096401U,
				"获取函数地址失败"
			},
			{
				3758096402U,
				"参数缓冲区长度不足"
			},
			{
				3758096403U,
				"正在获取底层信息"
			},
			{
				3758096404U,
				"索引值越界"
			},
			{
				3758096405U,
				"打开共享内存失败"
			},
			{
				3758096406U,
				"开启线程失败"
			},
			{
				3758096407U,
				"协议解析错误"
			},
			{
				3758096408U,
				"数据错误"
			},
			{
				3758096409U,
				"操作未实现"
			},
			{
				3758096410U,
				"数据大小超过上限"
			},
			{
				3758096411U,
				"前置条件有误"
			},
			{
				3758096412U,
				"运行环境错误"
			},
			{
				3758096413U,
				"正在关闭方案"
			},
			{
				3758096639U,
				"未知错误"
			},
			{
				3758096640U,
				"socket创建失败"
			},
			{
				3758096641U,
				"socket无效"
			},
			{
				3758096642U,
				"打开串口失败"
			},
			{
				3758096643U,
				"地址无效"
			},
			{
				3758096644U,
				"地址已被使用"
			},
			{
				3758096645U,
				"连接失败"
			},
			{
				3758096646U,
				"断开连接失败"
			},
			{
				3758096647U,
				"发送失败"
			},
			{
				3758096648U,
				"接收超时"
			},
			{
				3758096649U,
				"消息格式错误"
			},
			{
				3758096650U,
				"报文长度超出限制"
			},
			{
				3758096651U,
				"心跳异常"
			},
			{
				3758096896U,
				"创建路径错误"
			},
			{
				3758096897U,
				"文件无法打开"
			},
			{
				3758096898U,
				"保存文件数据失败"
			},
			{
				3758096899U,
				"文件不存在"
			},
			{
				3758096900U,
				"文件格式错误"
			},
			{
				3758096901U,
				"文件压缩失败"
			},
			{
				3758096902U,
				"文件解压失败"
			},
			{
				3758096903U,
				"xml中element不存在"
			},
			{
				3758096904U,
				"xml中Attribute不存在"
			},
			{
				3758096905U,
				"文件路径长度超过系统最大值"
			},
			{
				3758096906U,
				"文件被占用"
			},
			{
				3758097152U,
				"流程处于忙碌状态"
			},
			{
				3758097153U,
				"模块个数超出限制"
			},
			{
				3758097154U,
				"模块不存在"
			},
			{
				3758097155U,
				"模块已存在"
			},
			{
				3758097156U,
				"模块数量为0"
			},
			{
				3758097157U,
				"模块未注册"
			},
			{
				3758097158U,
				"模块订阅失败"
			},
			{
				3758097159U,
				"流程控制模块异常"
			},
			{
				3758097160U,
				"模块输入未配置完成"
			},
			{
				3758097161U,
				"模块输入无法找到"
			},
			{
				3758097162U,
				"模块输入状态错误"
			},
			{
				3758097163U,
				"模块输入个数错误"
			},
			{
				3758097164U,
				"模块输入缓冲区长度太小"
			},
			{
				3758097165U,
				"参数不支持"
			},
			{
				3758097166U,
				"参数值无效"
			},
			{
				3758097167U,
				"参数类型错误"
			},
			{
				3758097168U,
				"导入数据格式错误"
			},
			{
				3758097169U,
				"正在连续执行"
			},
			{
				3758097170U,
				"流程内的模块数量为0"
			},
			{
				3758097171U,
				"模块心跳出现异常"
			},
			{
				3758097172U,
				"未找到订阅结果值"
			},
			{
				3758097173U,
				"模块输出无法找到"
			},
			{
				3758097174U,
				"流程不存在"
			},
			{
				3758097175U,
				"流程已存在"
			},
			{
				3758097176U,
				"创建算法模块失败"
			},
			{
				3758097177U,
				"循环已存在"
			},
			{
				3758097178U,
				"循环不存在"
			},
			{
				3758097179U,
				"未找到订阅记录"
			},
			{
				3758097180U,
				"订阅参数有误"
			},
			{
				3758097181U,
				"流程处于禁用状态"
			},
			{
				3758097182U,
				"触发字符不匹配"
			},
			{
				3758097183U,
				"全局脚本流程正在执行中"
			},
			{
				3758097184U,
				"全局脚本预编译失败"
			},
			{
				3758097408U,
				"运行环境有问题"
			},
			{
				3758097409U,
				"命令不被设备支持"
			},
			{
				3758097410U,
				"设备无访问权限"
			},
			{
				3758097411U,
				"设备忙，或网络断开"
			},
			{
				3758097412U,
				"网络包数据错误"
			},
			{
				3758097413U,
				"读USB出错"
			},
			{
				3758097414U,
				"写USB出错"
			},
			{
				3758097415U,
				"设备异常"
			},
			{
				3758097416U,
				"USB带宽不足"
			},
			{
				3758097417U,
				"相机无数据(相机配置有误或获取图像超时)"
			},
			{
				3758097424U,
				"未连接相机"
			},
			{
				3758097425U,
				"未发现类型匹配的相机"
			},
			{
				3758097664U,
				"图像数据存储地址为空（某个分量）"
			},
			{
				3758097665U,
				"图像宽高与step参数不匹配"
			},
			{
				3758097666U,
				"图像宽高不正确或者超出范围"
			},
			{
				3758097667U,
				"图像格式不正确或者不支持"
			},
			{
				3758097668U,
				"内存空间大小不满足对齐要求"
			},
			{
				3758097669U,
				"内存空间大小不够"
			},
			{
				3758097670U,
				"内存对齐不满足要求"
			},
			{
				3758097671U,
				"ABILITY存在无效成员变量"
			},
			{
				3758097672U,
				"cpu不支持优化代码中的指令集"
			},
			{
				3758097673U,
				"数据大小不正确"
			},
			{
				3758097674U,
				"回调函数出错"
			},
			{
				3758097675U,
				"超过HKA限定最大内存"
			},
			{
				3758097676U,
				"数据STEP错误"
			},
			{
				3758097677U,
				"参数index错误"
			},
			{
				3758097678U,
				"参数个数错误"
			},
			{
				3758097679U,
				"算法库未初始化完成"
			},
			{
				3758097680U,
				"获取输入图像失败"
			},
			{
				3758097681U,
				"获取输入ROI失败"
			},
			{
				3758097682U,
				"获取位置修正信息失败"
			},
			{
				3758097683U,
				"模型数据为空"
			},
			{
				3758097684U,
				"未定义的ROI类型"
			},
			{
				3758097920U,
				"创建服务失败"
			},
			{
				3758097921U,
				"删除服务失败"
			},
			{
				3758097922U,
				"打开服务失败"
			},
			{
				3758097923U,
				"服务启动失败"
			},
			{
				3758097924U,
				"服务停止失败"
			},
			{
				3758098176U,
				"加密狗未检测到或检测异常"
			},
			{
				3758098177U,
				"算法平台老版本狗试用时间过期"
			},
			{
				3758098178U,
				"算法库检测授权失败"
			},
			{
				3758098179U,
				"算法库使用期已过"
			},
			{
				3758098180U,
				"软锁未检测到或检测异常"
			},
			{
				3758098181U,
				"软件未激活，是否进行授权激活？"
			},
			{
				3758098182U,
				"软锁不支持的功能ID"
			},
			{
				3758098183U,
				"软件授权已过期，是否进行重新授权？"
			},
			{
				3758098184U,
				"访问被拒绝"
			},
			{
				3758098185U,
				"时钟不可用"
			},
			{
				3758098186U,
				"未安装软加密RTE"
			},
			{
				3758098187U,
				"程序在终端运行"
			},
			{
				3758098188U,
				"程序在远程端运行"
			},
			{
				3758098189U,
				"程序在虚拟机运行"
			},
			{
				3758098190U,
				"软加密功能未找到"
			},
			{
				3758098191U,
				"软加密内部实现错误"
			},
			{
				3758098192U,
				"软加密产品未找到"
			},
			{
				3758098193U,
				"查询结果为空"
			},
			{
				3758098432U,
				"方案密码错误"
			},
			{
				3758098433U,
				"用户或密码错误(预留)"
			},
			{
				3758098688U,
				"模块输入未订阅"
			},
			{
				3758098689U,
				"算法库中出现警告"
			},
			{
				3758100480U,
				"模块算法类初始化结果"
			},
			{
				3758100736U,
				"模型无法打开"
			},
			{
				3758100737U,
				"模型不存在"
			},
			{
				3758100738U,
				"模型格式错误"
			},
			{
				3758100739U,
				"模型文件被占用"
			},
			{
				3758100740U,
				"模型数据异常"
			},
			{
				3758100741U,
				"模型数据长度异常"
			},
			{
				3774873600U,
				"启动参数文件异常"
			},
			{
				536870913U,
				"文件不存在"
			},
			{
				536870914U,
				"字符串为空"
			},
			{
				536870915U,
				"图像解码器错误"
			},
			{
				536870916U,
				"打开文件错误"
			},
			{
				536870917U,
				"文件读取错误"
			},
			{
				536870918U,
				"文件写错误"
			},
			{
				536870919U,
				"文件读取大小错误"
			},
			{
				536870920U,
				"文件类型错误"
			},
			{
				268435456U,
				"不确定类型错误（接口函数共用）"
			},
			{
				268435457U,
				"ABILITY存在无效参数"
			},
			{
				268435458U,
				"内存地址为空"
			},
			{
				268435459U,
				"内存对齐不满足要求"
			},
			{
				268435460U,
				"内存空间大小不够"
			},
			{
				268435461U,
				"内存空间大小不满足对齐要求"
			},
			{
				268435462U,
				"内存地址不满足对齐要求"
			},
			{
				268435463U,
				"图像格式不正确或者不支持"
			},
			{
				268435464U,
				"图像宽高不正确或者超出范围"
			},
			{
				268435465U,
				"图像宽高与step参数不匹配"
			},
			{
				268435466U,
				"图像数据存储地址为空（某个分量）"
			},
			{
				268435467U,
				"设置、获取参数类型不正确"
			},
			{
				268435468U,
				"设置、获取参数输入、输出结构体大小不正确"
			},
			{
				268435469U,
				"处理类型不正确"
			},
			{
				268435470U,
				"处理时输入、输出参数大小不正确"
			},
			{
				268435471U,
				"子处理类型不正确"
			},
			{
				268435472U,
				"子处理时输入、输出参数大小不正确"
			},
			{
				268435473U,
				"index参数不正确"
			},
			{
				268435474U,
				"value参数不正确或者超出范围"
			},
			{
				268435475U,
				"param_num参数不正确"
			},
			{
				268435476U,
				"函数参数指针为空（共用）"
			},
			{
				268435477U,
				"超过HKA限定最大内存"
			},
			{
				268435478U,
				"回调函数出错"
			},
			{
				268435479U,
				"加密错误"
			},
			{
				268435480U,
				"算法库使用期限错误"
			},
			{
				268435481U,
				"参数范围不正确"
			},
			{
				268435482U,
				"数据大小不正确（一维数据len，二维数据的HKA_SIZE）"
			},
			{
				268435483U,
				"数据step不正确（除HKA_IMAGE结构体之外）"
			},
			{
				268435484U,
				"cpu不支持优化代码中的指令集"
			},
			{
				268435485U,
				"警告"
			},
			{
				268435486U,
				"算法库超时"
			},
			{
				268435487U,
				"算法版本号出错"
			},
			{
				268435488U,
				"模型版本号出错：模板版本号与当前版本不符"
			},
			{
				268435489U,
				"GPU内存分配错误"
			},
			{
				268435490U,
				"文件不存在"
			},
			{
				268435491U,
				"字符串为空"
			},
			{
				268435492U,
				"图像解码器错误"
			},
			{
				268435493U,
				"打开文件错误"
			},
			{
				268435494U,
				"文件读取错误"
			},
			{
				268435495U,
				"文件写错误"
			},
			{
				268435496U,
				"文件读取大小错误"
			},
			{
				268435497U,
				"文件类型错误"
			},
			{
				269484032U,
				"区域内无特征数据"
			},
			{
				269484033U,
				"模板版本号与匹配版本不符"
			},
			{
				269484034U,
				"模板版本号与当前版本不符"
			},
			{
				269484035U,
				"程序可用内存不足"
			},
			{
				269500416U,
				"ROI区域比算法要求的最小宽高、模块要求的最小参数值小"
			},
			{
				269500417U,
				"图像能力集太小"
			},
			{
				269500418U,
				"字符宽高超出当前最大限制"
			},
			{
				269500419U,
				"字库增加处理样本数量错误"
			},
			{
				269500420U,
				"字符识别处理缺失字库文件"
			},
			{
				269500421U,
				"字库训练处理缺失训练样本"
			},
			{
				269500422U,
				"文本检测区域的中心点不在图像内部"
			},
			{
				269500423U,
				"文本检测区域的高、宽小于算法支持的最小字符大小"
			},
			{
				269500424U,
				"字库类型不匹配"
			},
			{
				269500425U,
				"字库没有与待识别字符类型一致的样本"
			},
			{
				270532608U,
				"cuda参数错误"
			},
			{
				270532609U,
				"cuda分配内存失败"
			},
			{
				270532610U,
				"cuda初始化错误"
			},
			{
				270532611U,
				"cuda runtime库未加载"
			},
			{
				270532612U,
				"不存在支持CUDA的显卡"
			},
			{
				270532613U,
				"无效CUDA的显卡"
			},
			{
				270532614U,
				"CUDA其他错误"
			},
			{
				270532615U,
				"CUBLAS未初始化"
			},
			{
				270532616U,
				"CUBLAS分配失败"
			},
			{
				270532617U,
				"CUBLAS参数错误"
			},
			{
				270532618U,
				"CUBLAS架构不匹配"
			},
			{
				270532619U,
				"CUBLAS映射错误"
			},
			{
				270532620U,
				"CUBLAS执行错误"
			},
			{
				270532621U,
				"CUBLAS内部错误"
			},
			{
				270532622U,
				"CUBLAS功能不支持"
			},
			{
				270532623U,
				"CUBLAS的LICENSE不符"
			},
			{
				270532624U,
				"CUBLAS其他错误"
			},
			{
				270532625U,
				"CUDNN未初始化"
			},
			{
				270532626U,
				"CUDNN分配失败"
			},
			{
				270532627U,
				"CUDNN参数错误"
			},
			{
				270532628U,
				"CUDNN内部错误"
			},
			{
				270532629U,
				"CUDNN无效值"
			},
			{
				270532630U,
				"CUDNN架构不匹配"
			},
			{
				270532631U,
				"CUDNN映射错误"
			},
			{
				270532632U,
				"CUDNN执行错误"
			},
			{
				270532633U,
				"CUDNN功能不支持"
			},
			{
				270532634U,
				"CUDNN的LICENSE不符"
			},
			{
				270532635U,
				"CUDNN其他错误"
			}
		};
	}
}
