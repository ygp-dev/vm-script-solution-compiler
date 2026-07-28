using System;
using Script.Algorithm;

namespace Script.Methods
{
	// Token: 0x02000009 RID: 9
	public class VarBaseModule : ModuleBase
	{
		// Token: 0x06000088 RID: 136 RVA: 0x00006634 File Offset: 0x00004834
		public VarBaseModule(int varModuleid, int nOwnerModuleID)
		{
			base.ModuleID = varModuleid;
			this.varAlgorithm = new VarAlgorithm
			{
				nVarModuleID = varModuleid,
				nShellModuleID = nOwnerModuleID
			};
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00006669 File Offset: 0x00004869
		public int SetVarValueString(string varName, string varValue)
		{
			return this.varAlgorithm.SetVarValueString(varName, varValue);
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00006678 File Offset: 0x00004878
		public int GetVarValueString(string varName, ref string varValue)
		{
			return this.varAlgorithm.GetVarValueString(varName, ref varValue);
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00006687 File Offset: 0x00004887
		public int SetVarInt(string varName, int[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarInt(varName, valueArray);
		}

		// Token: 0x0600008C RID: 140 RVA: 0x000066AC File Offset: 0x000048AC
		public int SetVarFloat(string varName, float[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarFloat(varName, valueArray);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x000066D1 File Offset: 0x000048D1
		public int SetVarString(string varName, string[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarString(varName, valueArray);
		}

		// Token: 0x0600008E RID: 142 RVA: 0x000066F6 File Offset: 0x000048F6
		public int SetVarByte(string varName, byte[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarByte(varName, valueArray);
		}

		// Token: 0x0600008F RID: 143 RVA: 0x0000671C File Offset: 0x0000491C
		public int SetVarImage(string varName, ImageData stImageData)
		{
			if (this.varAlgorithm == null || stImageData == null)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarImage(varName, stImageData.Buffer, stImageData.Width, stImageData.Height, Convert.ToInt32(stImageData.PixelFormat));
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00006768 File Offset: 0x00004968
		public int SetVarPoint(string varName, PointData[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarPoint(varName, DataConvert.PointDataToArray(valueArray));
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00006792 File Offset: 0x00004992
		public int SetVarBox(string varName, RoiboxData[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarRoiBox(varName, DataConvert.RoiboxDataToArray(valueArray));
		}

		// Token: 0x06000092 RID: 146 RVA: 0x000067BC File Offset: 0x000049BC
		public int SetVarAnnulus(string varName, AnnulusData[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarAnnulus(varName, DataConvert.AnnulusDataToArray(valueArray));
		}

		// Token: 0x06000093 RID: 147 RVA: 0x000067E6 File Offset: 0x000049E6
		public int SetVarCircle(string varName, CircleData[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarCircle(varName, DataConvert.CircleDataToArray(valueArray));
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00006810 File Offset: 0x00004A10
		public int SetVarEllipse(string varName, EllipseData[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarEllipse(varName, DataConvert.EllipseDataToArray(valueArray));
		}

		// Token: 0x06000095 RID: 149 RVA: 0x0000683A File Offset: 0x00004A3A
		public int SetVarLine(string varName, LineData[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarLine(varName, DataConvert.LineDataToArray(valueArray));
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00006864 File Offset: 0x00004A64
		public int SetVarRect(string varName, RectData[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarRect(varName, DataConvert.RectDataToArray(valueArray));
		}

		// Token: 0x06000097 RID: 151 RVA: 0x0000688E File Offset: 0x00004A8E
		public int SetVarPointset(string varName, byte[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarPointset(varName, valueArray);
		}

		// Token: 0x06000098 RID: 152 RVA: 0x000068B3 File Offset: 0x00004AB3
		public int SetVarFixture(string varName, FixtureData[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.varAlgorithm.SetVarFixture(varName, DataConvert.FixtureDataToArray(valueArray));
		}

		// Token: 0x06000099 RID: 153 RVA: 0x000068E0 File Offset: 0x00004AE0
		public int SetVarPolygon(string varName, PolygonData[] valueArray)
		{
			if (this.varAlgorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			PolygonArrayData polygonArray = null;
			int num = DataConvert.PolygonDataToArray(valueArray, ref polygonArray);
			if (num != 0)
			{
				return num;
			}
			return this.varAlgorithm.SetVarPolygon(varName, polygonArray);
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00006920 File Offset: 0x00004B20
		public int GetVarInt(string varName, ref int[] intArray)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			return this.varAlgorithm.GetVarInt(varName, ref intArray);
		}

		// Token: 0x0600009B RID: 155 RVA: 0x0000693D File Offset: 0x00004B3D
		public int GetVarFloat(string varName, ref float[] floatArray)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			return this.varAlgorithm.GetVarFloat(varName, ref floatArray);
		}

		// Token: 0x0600009C RID: 156 RVA: 0x0000695A File Offset: 0x00004B5A
		public int GetVarString(string varName, ref string[] stringArray)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			return this.varAlgorithm.GetVarString(varName, ref stringArray);
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00006977 File Offset: 0x00004B77
		public int GetVarByte(string varName, ref byte[] bytesData)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			return this.varAlgorithm.GetVarByte(varName, ref bytesData);
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00006994 File Offset: 0x00004B94
		public int GetVarImage(string varName, ref ImageData stImageData)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			byte[] buffer = null;
			int width = -1;
			int height = -1;
			int pixelFormat = -1;
			int varImage = this.varAlgorithm.GetVarImage(varName, ref buffer, ref width, ref height, ref pixelFormat);
			if (varImage != 0)
			{
				return varImage;
			}
			stImageData = new ImageData();
			stImageData.Buffer = buffer;
			stImageData.Width = width;
			stImageData.Height = height;
			stImageData.PixelFormat = (ImagePixelFormate)pixelFormat;
			return varImage;
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00006A00 File Offset: 0x00004C00
		public int GetVarPoint(string varName, ref PointData[] pointList)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			PointArrayData valueArray = new PointArrayData();
			int varPoint = this.varAlgorithm.GetVarPoint(varName, ref valueArray);
			if (varPoint != 0)
			{
				return varPoint;
			}
			pointList = DataConvert.PointArrayToData(valueArray);
			return varPoint;
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00006A40 File Offset: 0x00004C40
		public int GetVarCircle(string varName, ref CircleData[] stCircle)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			CircleArrayData valueArray = new CircleArrayData();
			int varCircle = this.varAlgorithm.GetVarCircle(varName, ref valueArray);
			if (varCircle != 0)
			{
				return varCircle;
			}
			stCircle = DataConvert.CircleArrayToData(valueArray);
			return varCircle;
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00006A80 File Offset: 0x00004C80
		public int GetVarEllipse(string varName, ref EllipseData[] stEllipse)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			EllipseArrayData valueArray = new EllipseArrayData();
			int varEllipse = this.varAlgorithm.GetVarEllipse(varName, ref valueArray);
			if (varEllipse != 0)
			{
				return varEllipse;
			}
			stEllipse = DataConvert.EllipseArrayToData(valueArray);
			return varEllipse;
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00006AC0 File Offset: 0x00004CC0
		public int GetVarLine(string varName, ref LineData[] stLine)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			LineArrayData valueArray = new LineArrayData();
			int varLine = this.varAlgorithm.GetVarLine(varName, ref valueArray);
			if (varLine != 0)
			{
				return varLine;
			}
			stLine = DataConvert.LineArrayToData(valueArray);
			return varLine;
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00006B00 File Offset: 0x00004D00
		public int GetVarBox(string varName, ref RoiboxData[] stRoiBox)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			RoiBoxArrayData valueArray = new RoiBoxArrayData();
			int varRoiBox = this.varAlgorithm.GetVarRoiBox(varName, ref valueArray);
			if (varRoiBox != 0)
			{
				return varRoiBox;
			}
			stRoiBox = DataConvert.RoiboxArrayToData(valueArray);
			return varRoiBox;
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00006B40 File Offset: 0x00004D40
		public int GetVarRect(string varName, ref RectData[] stRect)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			RectArrayData valueArray = new RectArrayData();
			int varRect = this.varAlgorithm.GetVarRect(varName, ref valueArray);
			if (varRect != 0)
			{
				return varRect;
			}
			stRect = DataConvert.RectArrayToData(valueArray);
			return varRect;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00006B80 File Offset: 0x00004D80
		public int GetVarAnnulus(string varName, ref AnnulusData[] stAnnulus)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			AnnulusArrayData valueArray = new AnnulusArrayData();
			int varAnnulus = this.varAlgorithm.GetVarAnnulus(varName, ref valueArray);
			if (varAnnulus != 0)
			{
				return varAnnulus;
			}
			stAnnulus = DataConvert.AnnulusArrayToData(valueArray);
			return varAnnulus;
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00006BBE File Offset: 0x00004DBE
		public int GetVarPointset(string varName, ref byte[] byteArray)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			return this.varAlgorithm.GetVarPointset(varName, ref byteArray);
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00006BDC File Offset: 0x00004DDC
		public int GetVarFixture(string varName, ref FixtureData[] stFixtureData)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			FixtureArrayData valueArray = new FixtureArrayData();
			int varFixture = this.varAlgorithm.GetVarFixture(varName, ref valueArray);
			if (varFixture != 0)
			{
				return varFixture;
			}
			stFixtureData = DataConvert.FixtureArrayToData(valueArray);
			return varFixture;
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x00006C1C File Offset: 0x00004E1C
		public int GetVarPolygon(string varName, ref PolygonData[] polygonData)
		{
			if (this.varAlgorithm == null)
			{
				return -536870911;
			}
			PolygonArrayData roiPolygonArray = new PolygonArrayData();
			int varPolygon = this.varAlgorithm.GetVarPolygon(varName, ref roiPolygonArray);
			if (varPolygon != 0)
			{
				return varPolygon;
			}
			polygonData = DataConvert.PolygonArrayToData(roiPolygonArray);
			return varPolygon;
		}

		// Token: 0x0400001B RID: 27
		private VarAlgorithm varAlgorithm;
	}
}
