using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using Conceal;
using Script.Algorithm;
using Script.Render;

namespace Script.Methods
{
	// Token: 0x02000007 RID: 7
	public class ScriptMethods : ISetData
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600003D RID: 61 RVA: 0x000037A8 File Offset: 0x000019A8
		public VarModule GlobalVariableModule
		{
			get
			{
				if (this._GlobalVariableModule == null)
				{
					this._GlobalVariableModule = new VarModule(13000, (this.Algorithm == null) ? -1 : this.Algorithm.ModuleID);
					this._GlobalVariableModule.objAlgorithm = this.Algorithm;
				}
				return this._GlobalVariableModule;
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600003E RID: 62 RVA: 0x000037FC File Offset: 0x000019FC
		public VarModule LocalVariable
		{
			get
			{
				if (this._LocalVariableModule == null)
				{
					int num = -1;
					if (this.Algorithm == null)
					{
						return null;
					}
					if (this.Algorithm.GetLocalVarModuleID(ref num) == 0 && num > 0)
					{
						this._LocalVariableModule = new VarModule(num, this.Algorithm.ModuleID);
						this._LocalVariableModule.objAlgorithm = this.Algorithm;
					}
				}
				return this._LocalVariableModule;
			}
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00003860 File Offset: 0x00001A60
		public static void InitForDLL()
		{
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000040 RID: 64 RVA: 0x00003862 File Offset: 0x00001A62
		protected object InternalObject
		{
			get
			{
				if (this._InternalObject == null)
				{
					this._InternalObject = new InternalMethods();
				}
				return this._InternalObject;
			}
		}

		// Token: 0x06000041 RID: 65 RVA: 0x0000387D File Offset: 0x00001A7D
		public void SetAlgorithm(IAlgorithm algorithm)
		{
			this.Algorithm = algorithm;
			this.CurrentProcess.objAlgorithm = this.Algorithm;
			this.GlobalCommunicateModule.objAlgorithm = this.Algorithm;
			(this.InternalObject as InternalMethods).SetAlgorithm(algorithm);
		}

		// Token: 0x06000042 RID: 66 RVA: 0x000038B9 File Offset: 0x00001AB9
		public void SetAlgorithmData(string key, object objData)
		{
			if (this.Algorithm != null)
			{
				this.Algorithm.SetData(key, objData);
			}
		}

		// Token: 0x06000043 RID: 67 RVA: 0x000038D0 File Offset: 0x00001AD0
		public int SetHandle(long input, long output)
		{
			this.Algorithm.SetInOutputHandle(input, output);
			return 0;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x000038E0 File Offset: 0x00001AE0
		public void Clear()
		{
			(this.InternalObject as InternalMethods).Clear();
		}

		// Token: 0x06000045 RID: 69 RVA: 0x000038F4 File Offset: 0x00001AF4
		public int GetIntValue(string paramName, ref int paramValue)
		{
			object empty = string.Empty;
			int num = 0;
			int objectValue = this.Algorithm.GetObjectValue(ScriptMethods.RepairName(paramName), 0, 0, ref empty, ref num, -1);
			if (empty != null && !string.IsNullOrEmpty((string)empty) && objectValue == 0)
			{
				int.TryParse((string)empty, out paramValue);
			}
			return objectValue;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003944 File Offset: 0x00001B44
		public int GetFloatValue(string paramName, ref float paramValue)
		{
			object empty = string.Empty;
			int num = 0;
			int objectValue = this.Algorithm.GetObjectValue(ScriptMethods.RepairName(paramName), 1, 0, ref empty, ref num, -1);
			if (empty != null && !string.IsNullOrEmpty((string)empty) && objectValue == 0)
			{
				float.TryParse((string)empty, out paramValue);
			}
			return objectValue;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00003994 File Offset: 0x00001B94
		public int GetStringValue(string paramName, ref string paramValue)
		{
			int num = 0;
			object empty = string.Empty;
			int objectValue = this.Algorithm.GetObjectValue(ScriptMethods.RepairName(paramName), 2, 0, ref empty, ref num, -1);
			if (objectValue != 0)
			{
				return objectValue;
			}
			paramValue = (empty as string);
			return objectValue;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x000039D0 File Offset: 0x00001BD0
		public int GetBytesValue(string paramName, ref byte[] paramValue)
		{
			int num = 0;
			object obj = new object();
			int objectValue = this.Algorithm.GetObjectValue(ScriptMethods.RepairName(paramName), 3, 0, ref obj, ref num, -1);
			if (objectValue != 0)
			{
				return objectValue;
			}
			if (obj is byte[])
			{
				paramValue = (byte[])obj;
			}
			return objectValue;
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00003A14 File Offset: 0x00001C14
		public int GetIntArrayValue(string paramName, ref int[] paramValue, out int arrayCount)
		{
			arrayCount = 0;
			int num = paramValue.Length;
			int[] array = null;
			int intArrayValue = this.Algorithm.GetIntArrayValue(ScriptMethods.RepairName(paramName), ref array);
			if (intArrayValue != 0 || array == null)
			{
				return intArrayValue;
			}
			arrayCount = array.Length;
			try
			{
				int num2 = 0;
				while (num2 < arrayCount && num > num2)
				{
					try
					{
						paramValue[num2] = array[num2];
					}
					catch (Exception)
					{
						paramValue[num2] = 0;
						return -536870892;
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				ScriptMethods.ConsoleWrite(ex.Message);
				return -536870657;
			}
			return intArrayValue;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003AB0 File Offset: 0x00001CB0
		public int GetFloatArrayValue(string paramName, ref float[] paramValue, out int arrayCount)
		{
			int num = paramValue.Length;
			arrayCount = 0;
			if (num == 0)
			{
				return -536870892;
			}
			float[] array = null;
			int floatArrayValue = this.Algorithm.GetFloatArrayValue(ScriptMethods.RepairName(paramName), ref array);
			if (floatArrayValue != 0 || array == null)
			{
				return floatArrayValue;
			}
			arrayCount = array.Length;
			try
			{
				int num2 = 0;
				while (num2 < arrayCount && num > num2)
				{
					try
					{
						paramValue[num2] = array[num2];
					}
					catch (Exception)
					{
						paramValue[num2] = 0f;
						return -536870892;
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				ScriptMethods.ConsoleWrite(ex.Message);
				return -536870657;
			}
			return floatArrayValue;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00003B58 File Offset: 0x00001D58
		public int GetStringArrayValue(string paramName, ref string[] paramValue, out int arrayCount)
		{
			int num = paramValue.Length;
			arrayCount = 0;
			if (num == 0)
			{
				return -536870892;
			}
			string[] array = null;
			int objectArrayValue = this.Algorithm.GetObjectArrayValue(ScriptMethods.RepairName(paramName), 2, ref array, -1);
			if (objectArrayValue != 0 || array == null)
			{
				return objectArrayValue;
			}
			arrayCount = array.Length;
			try
			{
				int num2 = 0;
				while (num2 < arrayCount && num > num2)
				{
					try
					{
						paramValue[num2] = array[num2];
					}
					catch (Exception)
					{
						paramValue[num2] = string.Empty;
						return -536870892;
					}
					num2++;
				}
			}
			catch (Exception ex)
			{
				ScriptMethods.ConsoleWrite(ex.Message);
				return -536870657;
			}
			return objectArrayValue;
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00003C04 File Offset: 0x00001E04
		public int GetObjectArrayValueForModule(int moduleId, int index, string paramKey, ref int nType, ref string[] paramValue)
		{
			return 0;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00003C07 File Offset: 0x00001E07
		public int SetObjectValueForModule(int moduleId, string paramKey, string paramValue, int valueType = 0)
		{
			return this.Algorithm.SetObjectValueForModule(moduleId, paramKey, paramValue, valueType);
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003C1C File Offset: 0x00001E1C
		public int SetIntValue(string key, int value)
		{
			return this.Algorithm.SetObjectValue(0, 0, ScriptMethods.RepairName(key), value.ToString());
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003C48 File Offset: 0x00001E48
		public int SetFloatValue(string key, float value)
		{
			return this.Algorithm.SetObjectValue(0, 1, ScriptMethods.RepairName(key), value.ToString());
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00003C74 File Offset: 0x00001E74
		public int SetStringValue(string key, string value)
		{
			return this.Algorithm.SetObjectValue(0, 2, ScriptMethods.RepairName(key), value);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00003C98 File Offset: 0x00001E98
		public int SetBytesValue(string key, byte[] value)
		{
			return this.Algorithm.SetObjectValue(0, 3, ScriptMethods.RepairName(key), value);
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00003CBB File Offset: 0x00001EBB
		public int SetImageValue(string key, ImageData imageData)
		{
			return this.Algorithm.SetImageData(ScriptMethods.RepairName(key), 4, imageData.Buffer, imageData.Width, imageData.Height, (int)imageData.PixelFormat);
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00003CE8 File Offset: 0x00001EE8
		public int GetImageValue(string key, ref ImageData imageData)
		{
			byte[] buffer = null;
			int width = -1;
			int height = -1;
			int pixelFormat = -1;
			int imageData2 = this.Algorithm.GetImageData(ScriptMethods.RepairName(key), 4, ref buffer, ref width, ref height, ref pixelFormat);
			if (imageData2 != 0)
			{
				return imageData2;
			}
			imageData = new ImageData();
			imageData.Buffer = buffer;
			imageData.Width = width;
			imageData.Height = height;
			imageData.PixelFormat = (ImagePixelFormate)pixelFormat;
			return imageData2;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00003D4C File Offset: 0x00001F4C
		public int SetRoiboxValue(string key, RoiboxData roiboxData)
		{
			return this.Algorithm.SetRoiBoxData(ScriptMethods.RepairName(key), 5, 0, roiboxData.CenterX, roiboxData.CenterY, roiboxData.Width, roiboxData.Height, roiboxData.Angle);
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00003D8C File Offset: 0x00001F8C
		public int GetRoiboxValue(string key, ref RoiboxData roiboxData)
		{
			float centerX = -1f;
			float centerY = -1f;
			float width = -1f;
			float height = -1f;
			float angle = -1f;
			int roiBoxData = this.Algorithm.GetRoiBoxData(ScriptMethods.RepairName(key), 5, ref centerX, ref centerY, ref width, ref height, ref angle);
			if (roiBoxData != 0)
			{
				return roiBoxData;
			}
			roiboxData = new RoiboxData();
			roiboxData.CenterX = centerX;
			roiboxData.CenterY = centerY;
			roiboxData.Width = width;
			roiboxData.Height = height;
			roiboxData.Angle = angle;
			return roiBoxData;
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00003E10 File Offset: 0x00002010
		public int SetRoiBoxArrayValue(string key, RoiboxData[] valueArray, int index, int len)
		{
			if (valueArray == null || index < 0 || len < 0)
			{
				return -536870911;
			}
			if (valueArray.Length < index + len)
			{
				return -536870911;
			}
			key = ScriptMethods.RepairName(key);
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			List<float> list3 = new List<float>();
			List<float> list4 = new List<float>();
			List<float> list5 = new List<float>();
			for (int i = index; i < len + index; i++)
			{
				list.Add(valueArray[i].CenterX);
				list2.Add(valueArray[i].CenterY);
				list3.Add(valueArray[i].Width);
				list4.Add(valueArray[i].Height);
				list5.Add(valueArray[i].Angle);
			}
			RoiBoxArrayData roiBoxArrayData = new RoiBoxArrayData();
			roiBoxArrayData.Count = len;
			roiBoxArrayData.CenterXArray = list.ToArray();
			roiBoxArrayData.CenterYArray = list2.ToArray();
			roiBoxArrayData.WidthArray = list3.ToArray();
			roiBoxArrayData.HeightArray = list4.ToArray();
			roiBoxArrayData.AngleArray = list5.ToArray();
			return this.Algorithm.SetRoiBoxArrayData(ScriptMethods.RepairName(key), roiBoxArrayData);
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00003F34 File Offset: 0x00002134
		public int GetRoiBoxArrayValue(string paramName, ref RoiboxData[] roiboxData, out int arrayCount)
		{
			arrayCount = 0;
			RoiBoxArrayData roiBoxArrayData = new RoiBoxArrayData();
			int roiBoxArrayData2 = this.Algorithm.GetRoiBoxArrayData(ScriptMethods.RepairName(paramName), ref roiBoxArrayData);
			if (roiBoxArrayData2 != 0)
			{
				return roiBoxArrayData2;
			}
			arrayCount = roiBoxArrayData.Count;
			roiboxData = new RoiboxData[arrayCount];
			for (int i = 0; i < arrayCount; i++)
			{
				roiboxData[i] = new RoiboxData();
				roiboxData[i].CenterX = roiBoxArrayData.CenterXArray[i];
				roiboxData[i].CenterY = roiBoxArrayData.CenterYArray[i];
				roiboxData[i].Width = roiBoxArrayData.WidthArray[i];
				roiboxData[i].Height = roiBoxArrayData.HeightArray[i];
				roiboxData[i].Angle = roiBoxArrayData.AngleArray[i];
			}
			return roiBoxArrayData2;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00003FE4 File Offset: 0x000021E4
		public int SetStringValueByIndex(string key, string value, int index, int total)
		{
			return this.Algorithm.SetObjectValue(index, 2, ScriptMethods.RepairName(key), value);
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00004008 File Offset: 0x00002208
		public int SetStringArrayValue(string key, string[] valueArray, int index, int len)
		{
			int num = 0;
			if (valueArray == null || index < 0 || len < 0)
			{
				return -536870911;
			}
			if (valueArray.Length < index + len)
			{
				return -536870911;
			}
			key = ScriptMethods.RepairName(key);
			for (int i = index; i < len + index; i++)
			{
				num = this.Algorithm.SetObjectValue(i, 2, key, valueArray[i]);
				if (num != 0)
				{
					break;
				}
			}
			return num;
		}

		// Token: 0x0600005A RID: 90 RVA: 0x0000406C File Offset: 0x0000226C
		public int SetIntValueByIndex(string key, int value, int index, int total)
		{
			return this.Algorithm.SetObjectValue(index, 0, ScriptMethods.RepairName(key), value.ToString());
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00004098 File Offset: 0x00002298
		public int SetIntArrayValue(string key, int[] valueArray, int index, int len)
		{
			if (valueArray == null || index < 0 || len < 0)
			{
				return -536870911;
			}
			if (valueArray.Length < index + len)
			{
				return -536870911;
			}
			key = ScriptMethods.RepairName(key);
			int[] array = new int[len];
			Array.Copy(valueArray, index, array, 0, len);
			return this.Algorithm.SetBasicArrayValue(0, key, array);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x000040F8 File Offset: 0x000022F8
		public int SetFloatValueByIndex(string key, float value, int index, int total)
		{
			return this.Algorithm.SetObjectValue(index, 1, ScriptMethods.RepairName(key), value.ToString());
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00004124 File Offset: 0x00002324
		public int SetFloatArrayValue(string key, float[] valueArray, int index, int len)
		{
			if (valueArray == null || index < 0 || len < 0)
			{
				return -536870911;
			}
			if (valueArray.Length < index + len)
			{
				return -536870911;
			}
			key = ScriptMethods.RepairName(key);
			float[] array = new float[len];
			Array.Copy(valueArray, index, array, 0, len);
			return this.Algorithm.SetBasicArrayValue(1, key, array);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00004184 File Offset: 0x00002384
		public int GetModuleParam(uint nModuleID, string paramKey, ref string paramValue)
		{
			return this.Algorithm.GetModuleParamValue((int)nModuleID, paramKey, ref paramValue);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x000041A1 File Offset: 0x000023A1
		public int SetModuleParam(uint nModuleID, string paramKey, string paramValue)
		{
			return this.Algorithm.SetObjectValueForModule((int)nModuleID, paramKey, paramValue, 0);
		}

		// Token: 0x06000060 RID: 96 RVA: 0x000041B4 File Offset: 0x000023B4
		public unsafe int BytesToPointset(byte[] inVariant, ref ContourPointData[] contourPointArray)
		{
			if (inVariant == null || inVariant.Length == 0)
			{
				return -536870911;
			}
			int num = Marshal.SizeOf(typeof(CONTOUR_POINT_DATA));
			int num2 = inVariant.Length / num;
			if (num2 > 0)
			{
				CONTOUR_POINT_DATA[] array = new CONTOUR_POINT_DATA[num2];
				fixed (CONTOUR_POINT_DATA* ptr = array)
				{
					byte* ptr2 = (byte*)ptr;
					for (int i = 0; i < inVariant.Length; i++)
					{
						ptr2[i] = inVariant[i];
					}
				}
				contourPointArray = new ContourPointData[num2];
				for (int j = 0; j < num2; j++)
				{
					contourPointArray[j] = new ContourPointData();
					contourPointArray[j].PointX = array[j].PointX;
					contourPointArray[j].PointY = array[j].PointY;
					contourPointArray[j].PointScore = array[j].PointScore;
				}
				return 0;
			}
			return -536870888;
		}

		// Token: 0x06000061 RID: 97 RVA: 0x000042A0 File Offset: 0x000024A0
		public byte[] PointsetToBytes(ContourPointData[] contourPointArray)
		{
			if (contourPointArray == null || contourPointArray.Length == 0)
			{
				return null;
			}
			int num = Marshal.SizeOf(typeof(CONTOUR_POINT_DATA));
			byte[] array = new byte[num * contourPointArray.Length];
			IntPtr intPtr = IntPtr.Zero;
			int num2 = 0;
			try
			{
				intPtr = Marshal.AllocHGlobal(num);
				for (int i = 0; i < contourPointArray.Length; i++)
				{
					if (contourPointArray[i] == null)
					{
						num2 = -536870888;
						LogHelper.Error("contourPointArray[" + i.ToString() + "] is null.", 0);
						break;
					}
					Marshal.StructureToPtr<CONTOUR_POINT_DATA>(new CONTOUR_POINT_DATA
					{
						PointX = contourPointArray[i].PointX,
						PointY = contourPointArray[i].PointY,
						PointScore = contourPointArray[i].PointScore,
						PointIndex = i
					}, intPtr, false);
					Marshal.Copy(intPtr, array, i * num, num);
				}
			}
			catch (Exception ex)
			{
				num2 = -536870888;
				LogHelper.Error("Set pointset data error: " + ex.Message, 0);
			}
			finally
			{
				if (intPtr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(intPtr);
					intPtr = IntPtr.Zero;
				}
			}
			if (num2 == 0)
			{
				return array;
			}
			return null;
		}

		// Token: 0x06000062 RID: 98 RVA: 0x000043DC File Offset: 0x000025DC
		public bool ShowImage(ImageData imageData)
		{
			if (RenderManager.Instance.CheckVersion())
			{
				return false;
			}
			if (imageData != null)
			{
				string format = "Gray8";
				if (imageData.PixelFormat == ImagePixelFormate.RGB24)
				{
					format = "Rgb24";
				}
				return RenderManager.Instance.ShowImage(imageData.Width, imageData.Height, imageData.Buffer, format);
			}
			return RenderManager.Instance.ShowImage(0, 0, null, "Gray8");
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00004444 File Offset: 0x00002644
		public bool DrawShape(object shapeData, ShapeConfig shapeConfig = null)
		{
			if (RenderManager.Instance.CheckVersion())
			{
				return false;
			}
			bool result = false;
			if (shapeData.GetType() == typeof(RoiboxData[]) || shapeData.GetType() == typeof(RoiboxData))
			{
				RoiboxData[] roiBoxDataArray;
				if (shapeData.GetType() == typeof(RoiboxData))
				{
					roiBoxDataArray = new RoiboxData[]
					{
						shapeData as RoiboxData
					};
				}
				else
				{
					roiBoxDataArray = (shapeData as RoiboxData[]);
				}
				if (shapeConfig == null)
				{
					result = this.DrawRoiBox(roiBoxDataArray, ShapeColor.Blue, ShapeColor.None, 2.0, 100);
				}
				else
				{
					if (shapeConfig.Color == ShapeColor.None)
					{
						shapeConfig.Color = ShapeColor.Blue;
					}
					result = this.DrawRoiBox(roiBoxDataArray, shapeConfig.Color, shapeConfig.FillColor, shapeConfig.Thickness, shapeConfig.Opacity);
				}
			}
			else if (shapeData.GetType() == typeof(AnnulusData[]) || shapeData.GetType() == typeof(AnnulusData))
			{
				AnnulusData[] annulusDataArray;
				if (shapeData.GetType() == typeof(AnnulusData))
				{
					annulusDataArray = new AnnulusData[]
					{
						shapeData as AnnulusData
					};
				}
				else
				{
					annulusDataArray = (shapeData as AnnulusData[]);
				}
				if (shapeConfig == null)
				{
					result = this.DrawAnnulus(annulusDataArray, ShapeColor.Blue, ShapeColor.None, 2.0, 100);
				}
				else
				{
					if (shapeConfig.Color == ShapeColor.None)
					{
						shapeConfig.Color = ShapeColor.Blue;
					}
					result = this.DrawAnnulus(annulusDataArray, shapeConfig.Color, shapeConfig.FillColor, shapeConfig.Thickness, shapeConfig.Opacity);
				}
			}
			else if (shapeData.GetType() == typeof(PolygonData[]) || shapeData.GetType() == typeof(PolygonData))
			{
				PolygonData[] polygonDataArray;
				if (shapeData.GetType() == typeof(PolygonData))
				{
					polygonDataArray = new PolygonData[]
					{
						shapeData as PolygonData
					};
				}
				else
				{
					polygonDataArray = (shapeData as PolygonData[]);
				}
				if (shapeConfig == null)
				{
					result = this.DrawPolygon(polygonDataArray, ShapeColor.Blue, ShapeColor.OrangeRed, 2.0, 50);
				}
				else
				{
					if (shapeConfig.Color == ShapeColor.None)
					{
						shapeConfig.Color = ShapeColor.Blue;
					}
					if (shapeConfig.FillColor == ShapeColor.None)
					{
						shapeConfig.FillColor = ShapeColor.OrangeRed;
					}
					result = this.DrawPolygon(polygonDataArray, shapeConfig.Color, shapeConfig.FillColor, shapeConfig.Thickness, shapeConfig.Opacity);
				}
			}
			else if (shapeData.GetType() == typeof(PointData[]) || shapeData.GetType() == typeof(PointData))
			{
				PointData[] pointDataArray;
				if (shapeData.GetType() == typeof(PointData))
				{
					pointDataArray = new PointData[]
					{
						shapeData as PointData
					};
				}
				else
				{
					pointDataArray = (shapeData as PointData[]);
				}
				if (shapeConfig == null)
				{
					result = this.DrawPoint(pointDataArray, ShapeColor.LightGreen, 1.0, 100);
				}
				else
				{
					if (shapeConfig.Color == ShapeColor.None)
					{
						shapeConfig.Color = ShapeColor.LightGreen;
					}
					result = this.DrawPoint(pointDataArray, shapeConfig.Color, shapeConfig.Thickness, shapeConfig.Opacity);
				}
			}
			else if (shapeData.GetType() == typeof(LineData[]) || shapeData.GetType() == typeof(LineData))
			{
				LineData[] lineDataArray;
				if (shapeData.GetType() == typeof(LineData))
				{
					lineDataArray = new LineData[]
					{
						shapeData as LineData
					};
				}
				else
				{
					lineDataArray = (shapeData as LineData[]);
				}
				if (shapeConfig == null)
				{
					result = this.DrawLine(lineDataArray, ShapeColor.LightGreen, 2.0, 100, false);
				}
				else
				{
					bool isThroughLine = false;
					if (shapeConfig is LineConfig)
					{
						isThroughLine = (shapeConfig as LineConfig).IsThroughLine;
					}
					if (shapeConfig.Color == ShapeColor.None)
					{
						shapeConfig.Color = ShapeColor.LightGreen;
					}
					result = this.DrawLine(lineDataArray, shapeConfig.Color, shapeConfig.Thickness, shapeConfig.Opacity, isThroughLine);
				}
			}
			else if (shapeData.GetType() == typeof(FixtureData[]) || shapeData.GetType() == typeof(FixtureData))
			{
				FixtureData[] fixtureDataArray;
				if (shapeData.GetType() == typeof(FixtureData))
				{
					fixtureDataArray = new FixtureData[]
					{
						shapeData as FixtureData
					};
				}
				else
				{
					fixtureDataArray = (shapeData as FixtureData[]);
				}
				if (shapeConfig == null)
				{
					result = this.DrawFixture(fixtureDataArray, ShapeColor.LightGreen, ShapeColor.Red, 1.0, 100);
				}
				else
				{
					ShapeColor initColor = ShapeColor.LightGreen;
					ShapeColor runColor = ShapeColor.Red;
					if (shapeConfig is FixtureConfig)
					{
						FixtureConfig fixtureConfig = shapeConfig as FixtureConfig;
						initColor = fixtureConfig.InitColor;
						runColor = fixtureConfig.RunColor;
					}
					result = this.DrawFixture(fixtureDataArray, initColor, runColor, shapeConfig.Thickness, shapeConfig.Opacity);
				}
			}
			else if (shapeData.GetType() == typeof(CircleData[]) || shapeData.GetType() == typeof(CircleData))
			{
				CircleData[] circleDataArray;
				if (shapeData.GetType() == typeof(CircleData))
				{
					circleDataArray = new CircleData[]
					{
						shapeData as CircleData
					};
				}
				else
				{
					circleDataArray = (shapeData as CircleData[]);
				}
				if (shapeConfig == null)
				{
					result = this.DrawCircle(circleDataArray, ShapeColor.LightGreen, ShapeColor.None, 1.0, 100);
				}
				else
				{
					if (shapeConfig.Color == ShapeColor.None)
					{
						shapeConfig.Color = ShapeColor.LightGreen;
					}
					result = this.DrawCircle(circleDataArray, shapeConfig.Color, shapeConfig.FillColor, shapeConfig.Thickness, shapeConfig.Opacity);
				}
			}
			else if (shapeData.GetType() == typeof(RectData[]) || shapeData.GetType() == typeof(RectData))
			{
				RectData[] rectDataArray;
				if (shapeData.GetType() == typeof(RectData))
				{
					rectDataArray = new RectData[]
					{
						shapeData as RectData
					};
				}
				else
				{
					rectDataArray = (shapeData as RectData[]);
				}
				if (shapeConfig == null)
				{
					result = this.DrawRect(rectDataArray, ShapeColor.LightGreen, ShapeColor.None, 2.0, 100);
				}
				else
				{
					if (shapeConfig.Color == ShapeColor.None)
					{
						shapeConfig.Color = ShapeColor.LightGreen;
					}
					result = this.DrawRect(rectDataArray, shapeConfig.Color, shapeConfig.FillColor, shapeConfig.Thickness, shapeConfig.Opacity);
				}
			}
			else if (shapeData.GetType() == typeof(EllipseData[]) || shapeData.GetType() == typeof(EllipseData))
			{
				EllipseData[] ellipseDataArray;
				if (shapeData.GetType() == typeof(EllipseData))
				{
					ellipseDataArray = new EllipseData[]
					{
						shapeData as EllipseData
					};
				}
				else
				{
					ellipseDataArray = (shapeData as EllipseData[]);
				}
				if (shapeConfig == null)
				{
					result = this.DrawEllipse(ellipseDataArray, ShapeColor.LightGreen, ShapeColor.None, 1.0, 100);
				}
				else
				{
					if (shapeConfig.Color == ShapeColor.None)
					{
						shapeConfig.Color = ShapeColor.LightGreen;
					}
					result = this.DrawEllipse(ellipseDataArray, shapeConfig.Color, shapeConfig.FillColor, shapeConfig.Thickness, shapeConfig.Opacity);
				}
			}
			else if (shapeData.GetType() == typeof(string[]) || shapeData.GetType() == typeof(string))
			{
				string[] array;
				if (shapeData.GetType() == typeof(string))
				{
					array = new string[]
					{
						shapeData as string
					};
				}
				else
				{
					array = (shapeData as string[]);
				}
				if (array != null && array.Length > 0)
				{
					if (shapeConfig == null)
					{
						for (int i = 0; i < array.Length; i++)
						{
							result = this.DrawText(array[i], 0f, 0f, ShapeColor.LightGreen, 10, 100);
						}
					}
					else
					{
						float positionX = 0f;
						float positionY = 0f;
						int fontSize = 10;
						if (shapeConfig is TextConfig)
						{
							TextConfig textConfig = shapeConfig as TextConfig;
							positionX = textConfig.PositionX;
							positionY = textConfig.PositionY;
							fontSize = textConfig.FontSize;
						}
						if (shapeConfig.Color == ShapeColor.None)
						{
							shapeConfig.Color = ShapeColor.LightGreen;
						}
						for (int j = 0; j < array.Length; j++)
						{
							result = this.DrawText(array[j], positionX, positionY, shapeConfig.Color, fontSize, shapeConfig.Opacity);
						}
					}
				}
			}
			else if (shapeData.GetType() == typeof(byte[]))
			{
				byte[] byteArray = shapeData as byte[];
				if (shapeConfig == null)
				{
					result = this.DrawPointset(byteArray, ShapeColor.LightGreen, 1.0, 100);
				}
				else
				{
					if (shapeConfig.Color == ShapeColor.None)
					{
						shapeConfig.Color = ShapeColor.LightGreen;
					}
					result = this.DrawPointset(byteArray, shapeConfig.Color, shapeConfig.Thickness, shapeConfig.Opacity);
				}
			}
			else
			{
				LogHelper.Error("Not supported shape type.", 0);
			}
			return result;
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00004CE8 File Offset: 0x00002EE8
		private bool DrawRoiBox(RoiboxData[] roiBoxDataArray, ShapeColor color = ShapeColor.Blue, ShapeColor fillColor = ShapeColor.None, double thickness = 2.0, int opacity = 100)
		{
			bool result = false;
			if (roiBoxDataArray != null && roiBoxDataArray.Length > 0)
			{
				string strColor = this.ConvertColor(color);
				string strFillColor = this.ConvertColor(fillColor);
				double dThickness = thickness;
				int num = opacity;
				this.CheckThickness(ref dThickness);
				this.CheckOpacity(ref num);
				double dOpacity = (double)num / 100.0;
				for (int i = 0; i < roiBoxDataArray.Length; i++)
				{
					result = RenderManager.Instance.DrawRect(roiBoxDataArray[i].CenterX, roiBoxDataArray[i].CenterY, roiBoxDataArray[i].Width, roiBoxDataArray[i].Height, roiBoxDataArray[i].Angle, strColor, strFillColor, dThickness, dOpacity);
				}
			}
			return result;
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00004D8C File Offset: 0x00002F8C
		private bool DrawAnnulus(AnnulusData[] annulusDataArray, ShapeColor color = ShapeColor.Blue, ShapeColor fillColor = ShapeColor.None, double thickness = 2.0, int opacity = 100)
		{
			bool result = false;
			if (annulusDataArray != null && annulusDataArray.Length > 0)
			{
				string strColor = this.ConvertColor(color);
				string strFillColor = this.ConvertColor(fillColor);
				double dThickness = thickness;
				int num = opacity;
				this.CheckThickness(ref dThickness);
				this.CheckOpacity(ref num);
				double dOpacity = (double)num / 100.0;
				for (int i = 0; i < annulusDataArray.Length; i++)
				{
					result = RenderManager.Instance.DrawAnnular(annulusDataArray[i].CenterX, annulusDataArray[i].CenterY, annulusDataArray[i].InnerRadius, annulusDataArray[i].OuterRadius, annulusDataArray[i].StartAngle, annulusDataArray[i].AngleExtend, strColor, strFillColor, dThickness, dOpacity);
				}
			}
			return result;
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00004E3C File Offset: 0x0000303C
		private bool DrawPolygon(PolygonData[] polygonDataArray, ShapeColor color = ShapeColor.Blue, ShapeColor fillColor = ShapeColor.OrangeRed, double thickness = 2.0, int opacity = 50)
		{
			bool result = false;
			if (polygonDataArray != null && polygonDataArray.Length > 0)
			{
				string strColor = this.ConvertColor(color);
				string strFillColor = this.ConvertColor(fillColor);
				double dThickness = thickness;
				int num = opacity;
				this.CheckThickness(ref dThickness);
				this.CheckOpacity(ref num);
				double dOpacity = (double)num / 100.0;
				for (int i = 0; i < polygonDataArray.Length; i++)
				{
					result = RenderManager.Instance.DrawPolygon(polygonDataArray[i].PointXArray, polygonDataArray[i].PointYArray, strColor, strFillColor, dThickness, dOpacity);
				}
			}
			return result;
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00004EC0 File Offset: 0x000030C0
		private bool DrawPoint(PointData[] pointDataArray, ShapeColor color = ShapeColor.LightGreen, double thickness = 1.0, int opacity = 100)
		{
			bool result = false;
			if (pointDataArray != null && pointDataArray.Length > 0)
			{
				string strColor = this.ConvertColor(color);
				double dThickness = thickness;
				int num = opacity;
				this.CheckThickness(ref dThickness);
				this.CheckOpacity(ref num);
				double dOpacity = (double)num / 100.0;
				for (int i = 0; i < pointDataArray.Length; i++)
				{
					result = RenderManager.Instance.DrawPoint(pointDataArray[i].PointX, pointDataArray[i].PointY, strColor, dThickness, dOpacity);
				}
			}
			return result;
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00004F38 File Offset: 0x00003138
		private bool DrawLine(LineData[] lineDataArray, ShapeColor color = ShapeColor.LightGreen, double thickness = 2.0, int opacity = 100, bool isThroughLine = false)
		{
			bool result = false;
			if (lineDataArray != null && lineDataArray.Length > 0)
			{
				string strColor = this.ConvertColor(color);
				double dThickness = thickness;
				int num = opacity;
				this.CheckThickness(ref dThickness);
				this.CheckOpacity(ref num);
				double dOpacity = (double)num / 100.0;
				for (int i = 0; i < lineDataArray.Length; i++)
				{
					result = RenderManager.Instance.DrawLine(lineDataArray[i].StartPointX, lineDataArray[i].StartPointY, lineDataArray[i].EndPointX, lineDataArray[i].EndPointY, strColor, dThickness, dOpacity, isThroughLine);
				}
			}
			return result;
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00004FC4 File Offset: 0x000031C4
		private bool DrawFixture(FixtureData[] fixtureDataArray, ShapeColor initColor = ShapeColor.LightGreen, ShapeColor runColor = ShapeColor.Red, double thickness = 1.0, int opacity = 100)
		{
			bool result = false;
			if (fixtureDataArray != null && fixtureDataArray.Length > 0)
			{
				string strColor = this.ConvertColor(initColor);
				string strColor2 = this.ConvertColor(runColor);
				double dThickness = thickness;
				int num = opacity;
				this.CheckThickness(ref dThickness);
				this.CheckOpacity(ref num);
				double dOpacity = (double)num / 100.0;
				for (int i = 0; i < fixtureDataArray.Length; i++)
				{
					result = RenderManager.Instance.DrawPoint(fixtureDataArray[i].InitPointX, fixtureDataArray[i].InitPointY, strColor, dThickness, dOpacity);
					result = RenderManager.Instance.DrawPoint(fixtureDataArray[i].RunPointX, fixtureDataArray[i].RunPointY, strColor2, dThickness, dOpacity);
				}
			}
			return result;
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00005070 File Offset: 0x00003270
		private bool DrawCircle(CircleData[] circleDataArray, ShapeColor color = ShapeColor.LightGreen, ShapeColor fillColor = ShapeColor.None, double thickness = 1.0, int opacity = 100)
		{
			bool result = false;
			if (circleDataArray != null && circleDataArray.Length > 0)
			{
				string strColor = this.ConvertColor(color);
				string strFillColor = this.ConvertColor(fillColor);
				double dThickness = thickness;
				int num = opacity;
				this.CheckThickness(ref dThickness);
				this.CheckOpacity(ref num);
				double dOpacity = (double)num / 100.0;
				for (int i = 0; i < circleDataArray.Length; i++)
				{
					result = RenderManager.Instance.DrawCircle(circleDataArray[i].CenterX, circleDataArray[i].CenterY, circleDataArray[i].Radius, strColor, strFillColor, dThickness, dOpacity);
				}
			}
			return result;
		}

		// Token: 0x0600006B RID: 107 RVA: 0x000050FC File Offset: 0x000032FC
		private bool DrawRect(RectData[] rectDataArray, ShapeColor color = ShapeColor.LightGreen, ShapeColor fillColor = ShapeColor.None, double thickness = 2.0, int opacity = 100)
		{
			bool result = false;
			if (rectDataArray != null && rectDataArray.Length > 0)
			{
				string strColor = this.ConvertColor(color);
				string strFillColor = this.ConvertColor(fillColor);
				double dThickness = thickness;
				int num = opacity;
				this.CheckThickness(ref dThickness);
				this.CheckOpacity(ref num);
				double dOpacity = (double)num / 100.0;
				for (int i = 0; i < rectDataArray.Length; i++)
				{
					result = RenderManager.Instance.DrawRect(rectDataArray[i].CenterX, rectDataArray[i].CenterY, rectDataArray[i].Width, rectDataArray[i].Height, 0f, strColor, strFillColor, dThickness, dOpacity);
				}
			}
			return result;
		}

		// Token: 0x0600006C RID: 108 RVA: 0x0000519C File Offset: 0x0000339C
		private bool DrawEllipse(EllipseData[] ellipseDataArray, ShapeColor color = ShapeColor.LightGreen, ShapeColor fillColor = ShapeColor.None, double thickness = 1.0, int opacity = 100)
		{
			bool result = false;
			if (ellipseDataArray != null && ellipseDataArray.Length > 0)
			{
				string strColor = this.ConvertColor(color);
				string strFillColor = this.ConvertColor(fillColor);
				double dThickness = thickness;
				int num = opacity;
				this.CheckThickness(ref dThickness);
				this.CheckOpacity(ref num);
				double dOpacity = (double)num / 100.0;
				for (int i = 0; i < ellipseDataArray.Length; i++)
				{
					result = RenderManager.Instance.DrawEllipse(ellipseDataArray[i].CenterX, ellipseDataArray[i].CenterY, ellipseDataArray[i].MajorRadius, ellipseDataArray[i].MinorRadius, ellipseDataArray[i].Angle, strColor, strFillColor, dThickness, dOpacity);
				}
			}
			return result;
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00005240 File Offset: 0x00003440
		private bool DrawText(string text, float positionX = 0f, float positionY = 0f, ShapeColor color = ShapeColor.LightGreen, int fontSize = 10, int opacity = 100)
		{
			bool result = false;
			if (!string.IsNullOrEmpty(text))
			{
				string strColor = this.ConvertColor(color);
				float fPositionX = (positionX < 0f) ? 0f : positionX;
				float fPositionY = (positionY < 0f) ? 0f : positionY;
				int nFontSize = fontSize;
				int num = opacity;
				this.CheckFontSize(ref nFontSize);
				this.CheckOpacity(ref num);
				double dOpacity = (double)num / 100.0;
				result = RenderManager.Instance.DrawText(text, fPositionX, fPositionY, strColor, nFontSize, dOpacity);
			}
			return result;
		}

		// Token: 0x0600006E RID: 110 RVA: 0x000052C0 File Offset: 0x000034C0
		private bool DrawPointset(byte[] byteArray, ShapeColor color = ShapeColor.LightGreen, double thickness = 1.0, int opacity = 100)
		{
			bool result = false;
			if (byteArray != null && byteArray.Length > 0)
			{
				string strColor = this.ConvertColor(color);
				double dThickness = thickness;
				int num = opacity;
				this.CheckThickness(ref dThickness);
				this.CheckOpacity(ref num);
				double dOpacity = (double)num / 100.0;
				result = RenderManager.Instance.DrawPointset(byteArray, strColor, dThickness, dOpacity);
			}
			return result;
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00005314 File Offset: 0x00003514
		private void CheckThickness(ref double dThickness)
		{
			if (dThickness < 1.0)
			{
				dThickness = 1.0;
				return;
			}
			if (dThickness > 10.0)
			{
				dThickness = 10.0;
			}
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00005347 File Offset: 0x00003547
		private void CheckOpacity(ref int nOpacity)
		{
			if (nOpacity < 0)
			{
				nOpacity = 0;
				return;
			}
			if (nOpacity > 100)
			{
				nOpacity = 100;
			}
		}

		// Token: 0x06000071 RID: 113 RVA: 0x0000535C File Offset: 0x0000355C
		private void CheckFontSize(ref int nFontSize)
		{
			if (nFontSize < 6)
			{
				nFontSize = 6;
				return;
			}
			if (nFontSize > 72)
			{
				nFontSize = 72;
			}
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00005374 File Offset: 0x00003574
		private string ConvertColor(ShapeColor shapeColor)
		{
			string result = null;
			switch (shapeColor)
			{
			case ShapeColor.AliceBlue:
				result = Colors.AliceBlue.ToString();
				break;
			case ShapeColor.PaleGoldenrod:
				result = Colors.PaleGoldenrod.ToString();
				break;
			case ShapeColor.Orchid:
				result = Colors.Orchid.ToString();
				break;
			case ShapeColor.OrangeRed:
				result = Colors.OrangeRed.ToString();
				break;
			case ShapeColor.Orange:
				result = Colors.Orange.ToString();
				break;
			case ShapeColor.OliveDrab:
				result = Colors.OliveDrab.ToString();
				break;
			case ShapeColor.Olive:
				result = Colors.Olive.ToString();
				break;
			case ShapeColor.OldLace:
				result = Colors.OldLace.ToString();
				break;
			case ShapeColor.Navy:
				result = Colors.Navy.ToString();
				break;
			case ShapeColor.NavajoWhite:
				result = Colors.NavajoWhite.ToString();
				break;
			case ShapeColor.Moccasin:
				result = Colors.Moccasin.ToString();
				break;
			case ShapeColor.MistyRose:
				result = Colors.MistyRose.ToString();
				break;
			case ShapeColor.MintCream:
				result = Colors.MintCream.ToString();
				break;
			case ShapeColor.MidnightBlue:
				result = Colors.MidnightBlue.ToString();
				break;
			case ShapeColor.MediumVioletRed:
				result = Colors.MediumVioletRed.ToString();
				break;
			case ShapeColor.MediumTurquoise:
				result = Colors.MediumTurquoise.ToString();
				break;
			case ShapeColor.MediumSpringGreen:
				result = Colors.MediumSpringGreen.ToString();
				break;
			case ShapeColor.MediumSlateBlue:
				result = Colors.MediumSlateBlue.ToString();
				break;
			case ShapeColor.LightSkyBlue:
				result = Colors.LightSkyBlue.ToString();
				break;
			case ShapeColor.LightSlateGray:
				result = Colors.LightSlateGray.ToString();
				break;
			case ShapeColor.LightSteelBlue:
				result = Colors.LightSteelBlue.ToString();
				break;
			case ShapeColor.LightYellow:
				result = Colors.LightYellow.ToString();
				break;
			case ShapeColor.Lime:
				result = Colors.Lime.ToString();
				break;
			case ShapeColor.LimeGreen:
				result = Colors.LimeGreen.ToString();
				break;
			case ShapeColor.PaleGreen:
				result = Colors.PaleGreen.ToString();
				break;
			case ShapeColor.Linen:
				result = Colors.Linen.ToString();
				break;
			case ShapeColor.Maroon:
				result = Colors.Maroon.ToString();
				break;
			case ShapeColor.MediumAquamarine:
				result = Colors.MediumAquamarine.ToString();
				break;
			case ShapeColor.MediumBlue:
				result = Colors.MediumBlue.ToString();
				break;
			case ShapeColor.MediumOrchid:
				result = Colors.MediumOrchid.ToString();
				break;
			case ShapeColor.MediumPurple:
				result = Colors.MediumPurple.ToString();
				break;
			case ShapeColor.MediumSeaGreen:
				result = Colors.MediumSeaGreen.ToString();
				break;
			case ShapeColor.Magenta:
				result = Colors.Magenta.ToString();
				break;
			case ShapeColor.PaleTurquoise:
				result = Colors.PaleTurquoise.ToString();
				break;
			case ShapeColor.PaleVioletRed:
				result = Colors.PaleVioletRed.ToString();
				break;
			case ShapeColor.PapayaWhip:
				result = Colors.PapayaWhip.ToString();
				break;
			case ShapeColor.SlateGray:
				result = Colors.SlateGray.ToString();
				break;
			case ShapeColor.Snow:
				result = Colors.Snow.ToString();
				break;
			case ShapeColor.SpringGreen:
				result = Colors.SpringGreen.ToString();
				break;
			case ShapeColor.SteelBlue:
				result = Colors.SteelBlue.ToString();
				break;
			case ShapeColor.Tan:
				result = Colors.Tan.ToString();
				break;
			case ShapeColor.Teal:
				result = Colors.Teal.ToString();
				break;
			case ShapeColor.SlateBlue:
				result = Colors.SlateBlue.ToString();
				break;
			case ShapeColor.Thistle:
				result = Colors.Thistle.ToString();
				break;
			case ShapeColor.Transparent:
				result = Colors.Transparent.ToString();
				break;
			case ShapeColor.Turquoise:
				result = Colors.Turquoise.ToString();
				break;
			case ShapeColor.Violet:
				result = Colors.Violet.ToString();
				break;
			case ShapeColor.Wheat:
				result = Colors.Wheat.ToString();
				break;
			case ShapeColor.White:
				result = Colors.White.ToString();
				break;
			case ShapeColor.WhiteSmoke:
				result = Colors.WhiteSmoke.ToString();
				break;
			case ShapeColor.Tomato:
				result = Colors.Tomato.ToString();
				break;
			case ShapeColor.LightSeaGreen:
				result = Colors.LightSeaGreen.ToString();
				break;
			case ShapeColor.SkyBlue:
				result = Colors.SkyBlue.ToString();
				break;
			case ShapeColor.Sienna:
				result = Colors.Sienna.ToString();
				break;
			case ShapeColor.PeachPuff:
				result = Colors.PeachPuff.ToString();
				break;
			case ShapeColor.Peru:
				result = Colors.Peru.ToString();
				break;
			case ShapeColor.Pink:
				result = Colors.Pink.ToString();
				break;
			case ShapeColor.Plum:
				result = Colors.Plum.ToString();
				break;
			case ShapeColor.PowderBlue:
				result = Colors.PowderBlue.ToString();
				break;
			case ShapeColor.Purple:
				result = Colors.Purple.ToString();
				break;
			case ShapeColor.Silver:
				result = Colors.Silver.ToString();
				break;
			case ShapeColor.Red:
				result = Colors.Red.ToString();
				break;
			case ShapeColor.RoyalBlue:
				result = Colors.RoyalBlue.ToString();
				break;
			case ShapeColor.SaddleBrown:
				result = Colors.SaddleBrown.ToString();
				break;
			case ShapeColor.Salmon:
				result = Colors.Salmon.ToString();
				break;
			case ShapeColor.SandyBrown:
				result = Colors.SandyBrown.ToString();
				break;
			case ShapeColor.SeaGreen:
				result = Colors.SeaGreen.ToString();
				break;
			case ShapeColor.SeaShell:
				result = Colors.SeaShell.ToString();
				break;
			case ShapeColor.RosyBrown:
				result = Colors.RosyBrown.ToString();
				break;
			case ShapeColor.Yellow:
				result = Colors.Yellow.ToString();
				break;
			case ShapeColor.LightSalmon:
				result = Colors.LightSalmon.ToString();
				break;
			case ShapeColor.LightGreen:
				result = Colors.LightGreen.ToString();
				break;
			case ShapeColor.DarkRed:
				result = Colors.DarkRed.ToString();
				break;
			case ShapeColor.DarkOrchid:
				result = Colors.DarkOrchid.ToString();
				break;
			case ShapeColor.DarkOrange:
				result = Colors.DarkOrange.ToString();
				break;
			case ShapeColor.DarkOliveGreen:
				result = Colors.DarkOliveGreen.ToString();
				break;
			case ShapeColor.DarkMagenta:
				result = Colors.DarkMagenta.ToString();
				break;
			case ShapeColor.DarkKhaki:
				result = Colors.DarkKhaki.ToString();
				break;
			case ShapeColor.DarkGreen:
				result = Colors.DarkGreen.ToString();
				break;
			case ShapeColor.DarkGray:
				result = Colors.DarkGray.ToString();
				break;
			case ShapeColor.DarkGoldenrod:
				result = Colors.DarkGoldenrod.ToString();
				break;
			case ShapeColor.DarkCyan:
				result = Colors.DarkCyan.ToString();
				break;
			case ShapeColor.DarkBlue:
				result = Colors.DarkBlue.ToString();
				break;
			case ShapeColor.Cyan:
				result = Colors.Cyan.ToString();
				break;
			case ShapeColor.Crimson:
				result = Colors.Crimson.ToString();
				break;
			case ShapeColor.Cornsilk:
				result = Colors.Cornsilk.ToString();
				break;
			case ShapeColor.CornflowerBlue:
				result = Colors.CornflowerBlue.ToString();
				break;
			case ShapeColor.Coral:
				result = Colors.Coral.ToString();
				break;
			case ShapeColor.Chocolate:
				result = Colors.Chocolate.ToString();
				break;
			case ShapeColor.AntiqueWhite:
				result = Colors.AntiqueWhite.ToString();
				break;
			case ShapeColor.Aqua:
				result = Colors.Aqua.ToString();
				break;
			case ShapeColor.Aquamarine:
				result = Colors.Aquamarine.ToString();
				break;
			case ShapeColor.Azure:
				result = Colors.Azure.ToString();
				break;
			case ShapeColor.Beige:
				result = Colors.Beige.ToString();
				break;
			case ShapeColor.Bisque:
				result = Colors.Bisque.ToString();
				break;
			case ShapeColor.DarkSalmon:
				result = Colors.DarkSalmon.ToString();
				break;
			case ShapeColor.Black:
				result = Colors.Black.ToString();
				break;
			case ShapeColor.Blue:
				result = Colors.Blue.ToString();
				break;
			case ShapeColor.BlueViolet:
				result = Colors.BlueViolet.ToString();
				break;
			case ShapeColor.Brown:
				result = Colors.Brown.ToString();
				break;
			case ShapeColor.BurlyWood:
				result = Colors.BurlyWood.ToString();
				break;
			case ShapeColor.CadetBlue:
				result = Colors.CadetBlue.ToString();
				break;
			case ShapeColor.Chartreuse:
				result = Colors.Chartreuse.ToString();
				break;
			case ShapeColor.BlanchedAlmond:
				result = Colors.BlanchedAlmond.ToString();
				break;
			case ShapeColor.DarkSeaGreen:
				result = Colors.DarkSeaGreen.ToString();
				break;
			case ShapeColor.DarkSlateBlue:
				result = Colors.DarkSlateBlue.ToString();
				break;
			case ShapeColor.DarkSlateGray:
				result = Colors.DarkSlateGray.ToString();
				break;
			case ShapeColor.HotPink:
				result = Colors.HotPink.ToString();
				break;
			case ShapeColor.IndianRed:
				result = Colors.IndianRed.ToString();
				break;
			case ShapeColor.Indigo:
				result = Colors.Indigo.ToString();
				break;
			case ShapeColor.Ivory:
				result = Colors.Ivory.ToString();
				break;
			case ShapeColor.Khaki:
				result = Colors.Khaki.ToString();
				break;
			case ShapeColor.Lavender:
				result = Colors.Lavender.ToString();
				break;
			case ShapeColor.Honeydew:
				result = Colors.Honeydew.ToString();
				break;
			case ShapeColor.LavenderBlush:
				result = Colors.LavenderBlush.ToString();
				break;
			case ShapeColor.LemonChiffon:
				result = Colors.LemonChiffon.ToString();
				break;
			case ShapeColor.LightBlue:
				result = Colors.LightBlue.ToString();
				break;
			case ShapeColor.LightCoral:
				result = Colors.LightCoral.ToString();
				break;
			case ShapeColor.LightCyan:
				result = Colors.LightCyan.ToString();
				break;
			case ShapeColor.LightGoldenrodYellow:
				result = Colors.LightGoldenrodYellow.ToString();
				break;
			case ShapeColor.LightGray:
				result = Colors.LightGray.ToString();
				break;
			case ShapeColor.LawnGreen:
				result = Colors.LawnGreen.ToString();
				break;
			case ShapeColor.LightPink:
				result = Colors.LightPink.ToString();
				break;
			case ShapeColor.GreenYellow:
				result = Colors.GreenYellow.ToString();
				break;
			case ShapeColor.Gray:
				result = Colors.Gray.ToString();
				break;
			case ShapeColor.DarkTurquoise:
				result = Colors.DarkTurquoise.ToString();
				break;
			case ShapeColor.DarkViolet:
				result = Colors.DarkViolet.ToString();
				break;
			case ShapeColor.DeepPink:
				result = Colors.DeepPink.ToString();
				break;
			case ShapeColor.DeepSkyBlue:
				result = Colors.DeepSkyBlue.ToString();
				break;
			case ShapeColor.DimGray:
				result = Colors.DimGray.ToString();
				break;
			case ShapeColor.DodgerBlue:
				result = Colors.DodgerBlue.ToString();
				break;
			case ShapeColor.Green:
				result = Colors.Green.ToString();
				break;
			case ShapeColor.Firebrick:
				result = Colors.Firebrick.ToString();
				break;
			case ShapeColor.ForestGreen:
				result = Colors.ForestGreen.ToString();
				break;
			case ShapeColor.Fuchsia:
				result = Colors.Fuchsia.ToString();
				break;
			case ShapeColor.Gainsboro:
				result = Colors.Gainsboro.ToString();
				break;
			case ShapeColor.GhostWhite:
				result = Colors.GhostWhite.ToString();
				break;
			case ShapeColor.Gold:
				result = Colors.Gold.ToString();
				break;
			case ShapeColor.Goldenrod:
				result = Colors.Goldenrod.ToString();
				break;
			case ShapeColor.FloralWhite:
				result = Colors.FloralWhite.ToString();
				break;
			case ShapeColor.YellowGreen:
				result = Colors.YellowGreen.ToString();
				break;
			}
			return result;
		}

		// Token: 0x06000073 RID: 115 RVA: 0x000063B1 File Offset: 0x000045B1
		public virtual void Dispose()
		{
			if (this.Algorithm != null)
			{
				this.Algorithm.Dispose();
			}
		}

		// Token: 0x06000074 RID: 116 RVA: 0x000063C6 File Offset: 0x000045C6
		public static void ConsoleWrite(string content)
		{
			Debugger.Log(0, null, content);
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000063D0 File Offset: 0x000045D0
		public static void Sleep(int millisecondsTimeout)
		{
			Thread.Sleep(millisecondsTimeout);
		}

		// Token: 0x06000076 RID: 118 RVA: 0x000063D8 File Offset: 0x000045D8
		public static void ShowMessageBox(string msg)
		{
			Interop.ShowMessageBox(msg, "tips");
		}

		// Token: 0x06000077 RID: 119 RVA: 0x000063E8 File Offset: 0x000045E8
		private static string RepairName(string paraName)
		{
			if (string.IsNullOrEmpty(paraName))
			{
				return paraName;
			}
			if (paraName.Length > 0 && paraName[0] != '%' && paraName[paraName.Length - 1] != '%')
			{
				paraName = "%" + paraName + "%";
			}
			return paraName;
		}

		// Token: 0x04000010 RID: 16
		public ModuleGroup CurrentProcess = new ModuleGroup();

		// Token: 0x04000011 RID: 17
		public GlobalCommModule GlobalCommunicateModule = new GlobalCommModule
		{
			ModuleID = 11000
		};

		// Token: 0x04000012 RID: 18
		private VarModule _GlobalVariableModule;

		// Token: 0x04000013 RID: 19
		private VarModule _LocalVariableModule;

		// Token: 0x04000014 RID: 20
		public IAlgorithm Algorithm;

		// Token: 0x04000015 RID: 21
		public int nErrorCode;

		// Token: 0x04000016 RID: 22
		private object _InternalObject;
	}
}
