using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VM.Utility;

namespace Script.Algorithm
{
	// Token: 0x02000040 RID: 64
	public class SharedMemoryCfg
	{
		// Token: 0x17000065 RID: 101
		// (get) Token: 0x06000239 RID: 569 RVA: 0x0000DC98 File Offset: 0x0000BE98
		private int ImageHeaderLen
		{
			get
			{
				return Marshal.SizeOf(typeof(SHARED_MEM_HEADER));
			}
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x0600023A RID: 570 RVA: 0x0000DCBC File Offset: 0x0000BEBC
		private int ProcessID
		{
			get
			{
				Process currentProcess = Process.GetCurrentProcess();
				return currentProcess.Id;
			}
		}

		// Token: 0x0600023B RID: 571 RVA: 0x0000DCDC File Offset: 0x0000BEDC
		public SharedMemoryCfg()
		{
			this.strGuid = Guid.NewGuid().ToString("B");
		}

		// Token: 0x0600023C RID: 572 RVA: 0x0000DD28 File Offset: 0x0000BF28
		public void ReleaseMemory()
		{
			foreach (KeyValuePair<int, List<SharedMemoryMappingInfo>> keyValuePair in this.shareMappingInfo)
			{
				List<SharedMemoryMappingInfo> value = keyValuePair.Value;
				for (int i = 0; i < value.Count; i++)
				{
					SharedMemoryMappingInfo value2 = value[i];
					this.CloseMapping(ref value2);
					value[i] = value2;
				}
			}
			this.shareMappingInfo.Clear();
		}

		// Token: 0x0600023D RID: 573 RVA: 0x0000DDC8 File Offset: 0x0000BFC8
		private int GetSetNodeNum()
		{
			int num = 0;
			int result;
			if (ScriptNativeMethods.GetBufferNum(ref num) == 0)
			{
				LogHelper.Error(string.Format("GetSetNodeNum nNodeNum:[{0}],GetNodeNum:[{1}]", num, this.nNodeNum), 0);
				result = Math.Max(num, this.nNodeNum);
			}
			else
			{
				result = this.nNodeNum;
			}
			return result;
		}

		// Token: 0x0600023E RID: 574 RVA: 0x0000DE28 File Offset: 0x0000C028
		public void SetNodeNum(string strNodeNum)
		{
			if (!string.IsNullOrEmpty(strNodeNum))
			{
				int num = 0;
				if (int.TryParse(strNodeNum, out num))
				{
					this.nNodeNum = ((num > this.nNodeNum) ? num : this.nNodeNum);
				}
			}
		}

		// Token: 0x0600023F RID: 575 RVA: 0x0000DE70 File Offset: 0x0000C070
		private string GeneralNewMapName(string strMapName)
		{
			string result;
			if (string.IsNullOrEmpty(strMapName))
			{
				result = null;
			}
			else
			{
				string[] array = strMapName.Split(new char[]
				{
					'{'
				});
				string[] array2 = strMapName.Split(new char[]
				{
					'}'
				});
				if (array.Length > 0 && array2.Length > 0)
				{
					result = string.Format("{0}{1}{2}", array[0], Guid.NewGuid().ToString("B"), array2[1]);
				}
				else
				{
					result = null;
				}
			}
			return result;
		}

		// Token: 0x06000240 RID: 576 RVA: 0x0000DF04 File Offset: 0x0000C104
		public int AllocateSharedMemory(int nModuleId, uint nLen, ref IntPtr ptrMemory, ref string strSharedName, int nUsageType)
		{
			int result;
			if (nLen < 1U)
			{
				result = -536870911;
			}
			else
			{
				try
				{
					if (!this.shareMappingInfo.ContainsKey(nUsageType))
					{
						this.shareMappingInfo.Add(nUsageType, new List<SharedMemoryMappingInfo>());
					}
					List<SharedMemoryMappingInfo> list = this.shareMappingInfo[nUsageType];
					int num = list.Count<SharedMemoryMappingInfo>();
					SharedMemoryMappingInfo item;
					if (num > 0 && list[0].uiMappingSize == 0U)
					{
						item = list.ElementAt(0);
						list.RemoveAt(0);
					}
					else if (num < this.GetSetNodeNum())
					{
						item = default(SharedMemoryMappingInfo);
						item.uiMappingSize = 0U;
						item.strMappingName = string.Format("{0}-{1}-{2}-{3}-{4}", new object[]
						{
							nModuleId,
							nUsageType,
							this.ProcessID,
							this.strGuid,
							num
						});
					}
					else
					{
						item = list.ElementAt(0);
						list.RemoveAt(0);
					}
					if ((ulong)item.uiMappingSize < (ulong)nLen + (ulong)((long)this.ImageHeaderLen))
					{
						if (item.uiMappingSize != 0U)
						{
							this.CloseMapping(ref item);
							string text = this.GeneralNewMapName(item.strMappingName);
							if (string.IsNullOrEmpty(text))
							{
								text = string.Format("{0}-{1}-{2}-{3}-{4}", new object[]
								{
									nModuleId,
									nUsageType,
									this.ProcessID,
									Guid.NewGuid().ToString("B"),
									num
								});
							}
							item.strMappingName = text;
						}
						item.uiMappingSize = nLen;
						if (this.CreateMapping(nModuleId, ref item) != 0)
						{
							LogHelper.Error("CreateMapping is error", nModuleId);
							return -536870910;
						}
					}
					SHARED_MEM_HEADER structure = new SHARED_MEM_HEADER
					{
						nHeaderLen = (ushort)this.ImageHeaderLen,
						nSize = (ulong)nLen,
						nLightCopy = 0,
						szSharedMemName = this.UTF8GetFixLenBytes(item.strMappingName, 64)
					};
					IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SHARED_MEM_HEADER>(structure));
					Marshal.StructureToPtr<SHARED_MEM_HEADER>(structure, intPtr, false);
					byte[] array = new byte[this.ImageHeaderLen];
					Marshal.Copy(intPtr, array, 0, this.ImageHeaderLen);
					Marshal.FreeHGlobal(intPtr);
					Marshal.Copy(array, 0, item.pMappingView, this.ImageHeaderLen);
					ptrMemory = item.pMappingView + this.ImageHeaderLen;
					strSharedName = item.strMappingName;
					list.Add(item);
					result = 0;
				}
				catch (Exception ex)
				{
					LogHelper.Error("AllocateSharedMemory is error:" + ex.ToString(), nModuleId);
					result = -536870911;
				}
			}
			return result;
		}

		// Token: 0x06000241 RID: 577 RVA: 0x0000E224 File Offset: 0x0000C424
		private int CloseMapping(ref SharedMemoryMappingInfo mapinfo)
		{
			int result;
			try
			{
				if (mapinfo.pMappingView != IntPtr.Zero)
				{
					MemoryHelper.UnmapViewOfFile(mapinfo.pMappingView);
					mapinfo.pMappingView = IntPtr.Zero;
				}
				if (mapinfo.hMapping != IntPtr.Zero)
				{
					MemoryHelper.CloseHandle(mapinfo.hMapping);
					mapinfo.hMapping = IntPtr.Zero;
				}
				mapinfo.uiMappingSize = 0U;
				result = 0;
			}
			catch (Exception ex)
			{
				LogHelper.Error("CloseMapping is error:" + ex.ToString(), 0);
				result = -536870911;
			}
			return result;
		}

		// Token: 0x06000242 RID: 578 RVA: 0x0000E2D4 File Offset: 0x0000C4D4
		private int CreateMapping(int nModuleID, ref SharedMemoryMappingInfo mapinfo)
		{
			IntPtr hFile = new IntPtr(-1);
			uint num = (uint)(mapinfo.uiMappingSize * 1.1 + (double)this.ImageHeaderLen);
			LogHelper.Error(string.Format("CreateMapping [{0}] [{1}] [{2}]", mapinfo.uiMappingSize, num, mapinfo.strMappingName), nModuleID);
			try
			{
				IntPtr intPtr = MemoryHelper.CreateFileMapping(hFile, IntPtr.Zero, 4, 0, num, mapinfo.strMappingName);
				if (intPtr == IntPtr.Zero)
				{
					LogHelper.Error("create filemap error " + Marshal.GetLastWin32Error(), nModuleID);
					return -536870891;
				}
				IntPtr intPtr2 = MemoryHelper.MapViewOfFile(intPtr, 2, 0, 0, new IntPtr((long)((ulong)num)));
				if (intPtr2 == IntPtr.Zero)
				{
					LogHelper.Error("create MapViewOfFile error " + Marshal.GetLastWin32Error(), nModuleID);
					MemoryHelper.CloseHandle(intPtr);
					return -536870891;
				}
				mapinfo.hMapping = intPtr;
				mapinfo.pMappingView = intPtr2;
				mapinfo.uiMappingSize = num;
			}
			catch (Exception ex)
			{
				LogHelper.Error("create MapViewOfFile error," + ex.ToString(), nModuleID);
				return -536870657;
			}
			LogHelper.Error("CreateMapping end", 0);
			return 0;
		}

		// Token: 0x06000243 RID: 579 RVA: 0x0000E454 File Offset: 0x0000C654
		private byte[] UTF8GetFixLenBytes(string str, int len)
		{
			byte[] array = new byte[len];
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			Buffer.BlockCopy(bytes, 0, array, 0, Math.Min(bytes.Length, len));
			return array;
		}

		// Token: 0x040001C3 RID: 451
		private const int INVALID_HANDLE_VALUE = -1;

		// Token: 0x040001C4 RID: 452
		private const int PAGE_READWRITE = 4;

		// Token: 0x040001C5 RID: 453
		private const int FILE_MAP_ALL_ACCESS = 2;

		// Token: 0x040001C6 RID: 454
		private string strGuid = "";

		// Token: 0x040001C7 RID: 455
		private int nNodeNum = 1;

		// Token: 0x040001C8 RID: 456
		private Dictionary<int, List<SharedMemoryMappingInfo>> shareMappingInfo = new Dictionary<int, List<SharedMemoryMappingInfo>>();
	}
}
