using System;
using System.Collections.Generic;
using Script.Algorithm;
using Script.Methods;

namespace Conceal
{
	// Token: 0x02000006 RID: 6
	public class InternalMethods
	{
		// Token: 0x0600001F RID: 31 RVA: 0x00002D3E File Offset: 0x00000F3E
		public void SetAlgorithm(IAlgorithm algorithm)
		{
			this.Algorithm = algorithm;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002D48 File Offset: 0x00000F48
		public void Clear()
		{
			this._intDataDict.Clear();
			this._floatDataDict.Clear();
			this._stringDataDict.Clear();
			this._roiBoxDataDict.Clear();
			this._annulusDataDict.Clear();
			this._polygonDataDict.Clear();
			this._pointDataDict.Clear();
			this._lineDataDict.Clear();
			this._fixtureDataDict.Clear();
			this._circleDataDict.Clear();
			this._rectDataDict.Clear();
			this._ellipseDataDict.Clear();
			this._contourPointDataDict.Clear();
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002DE4 File Offset: 0x00000FE4
		public int SetIntArrayValue(string key, int[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.Algorithm.SetBasicArrayValue(0, InternalMethods.RepairName(key), valueArray);
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002E10 File Offset: 0x00001010
		public int GetIntArrayValue(string paramName, ref int[] intData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int result = 0;
			if (this._intDataDict.ContainsKey(paramName) && this._intDataDict[paramName] != null)
			{
				intData = this._intDataDict[paramName];
			}
			else
			{
				result = this.Algorithm.GetIntArrayValue(InternalMethods.RepairName(paramName), ref intData);
				this._intDataDict[paramName] = intData;
			}
			return result;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002E7B File Offset: 0x0000107B
		public int SetFloatArrayValue(string key, float[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.Algorithm.SetBasicArrayValue(1, InternalMethods.RepairName(key), valueArray);
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002EA8 File Offset: 0x000010A8
		public int GetFloatArrayValue(string paramName, ref float[] floatData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int result = 0;
			if (this._floatDataDict.ContainsKey(paramName) && this._floatDataDict[paramName] != null)
			{
				floatData = this._floatDataDict[paramName];
			}
			else
			{
				result = this.Algorithm.GetFloatArrayValue(InternalMethods.RepairName(paramName), ref floatData);
				this._floatDataDict[paramName] = floatData;
			}
			return result;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002F14 File Offset: 0x00001114
		public int SetStringArrayValue(string key, string[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			int num = 0;
			for (int i = 0; i < valueArray.Length; i++)
			{
				num = this.Algorithm.SetObjectValue(i, 2, InternalMethods.RepairName(key), valueArray[i]);
				if (num != 0)
				{
					break;
				}
			}
			return num;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002F64 File Offset: 0x00001164
		public int GetStringArrayValue(string paramName, ref string[] stringData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int result = 0;
			if (this._stringDataDict.ContainsKey(paramName) && this._stringDataDict[paramName] != null)
			{
				stringData = this._stringDataDict[paramName];
			}
			else
			{
				result = this.Algorithm.GetObjectArrayValue(InternalMethods.RepairName(paramName), 2, ref stringData, -1);
				this._stringDataDict[paramName] = stringData;
			}
			return result;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002FD1 File Offset: 0x000011D1
		public int SetRoiBoxArrayValue(string key, RoiboxData[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.Algorithm.SetRoiBoxArrayData(InternalMethods.RepairName(key), DataConvert.RoiboxDataToArray(valueArray));
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00003000 File Offset: 0x00001200
		public int GetRoiBoxArrayValue(string paramName, ref RoiboxData[] roiBoxData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int num = 0;
			if (this._roiBoxDataDict.ContainsKey(paramName) && this._roiBoxDataDict[paramName] != null)
			{
				roiBoxData = this._roiBoxDataDict[paramName];
			}
			else
			{
				RoiBoxArrayData valueArray = new RoiBoxArrayData();
				num = this.Algorithm.GetRoiBoxArrayData(InternalMethods.RepairName(paramName), ref valueArray);
				if (num != 0)
				{
					return num;
				}
				roiBoxData = DataConvert.RoiboxArrayToData(valueArray);
				this._roiBoxDataDict[paramName] = roiBoxData;
			}
			return num;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x0000307F File Offset: 0x0000127F
		public int SetAnnulusArrayValue(string key, AnnulusData[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.Algorithm.SetAnnulusArrayData(InternalMethods.RepairName(key), DataConvert.AnnulusDataToArray(valueArray));
		}

		// Token: 0x0600002A RID: 42 RVA: 0x000030B0 File Offset: 0x000012B0
		public int GetAnnulusArrayValue(string paramName, ref AnnulusData[] annulusData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int num = 0;
			if (this._annulusDataDict.ContainsKey(paramName) && this._annulusDataDict[paramName] != null)
			{
				annulusData = this._annulusDataDict[paramName];
			}
			else
			{
				AnnulusArrayData valueArray = new AnnulusArrayData();
				num = this.Algorithm.GetAnnulusArrayData(InternalMethods.RepairName(paramName), ref valueArray);
				if (num != 0)
				{
					return num;
				}
				annulusData = DataConvert.AnnulusArrayToData(valueArray);
				this._annulusDataDict[paramName] = annulusData;
			}
			return num;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00003130 File Offset: 0x00001330
		public int SetPolygonArrayValue(string key, PolygonData[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			PolygonArrayData polygonArray = null;
			int num = DataConvert.PolygonDataToArray(valueArray, ref polygonArray);
			if (num != 0)
			{
				return num;
			}
			return this.Algorithm.SetPolygonArrayData(InternalMethods.RepairName(key), polygonArray);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00003178 File Offset: 0x00001378
		public int GetPolygonArrayValue(string paramName, ref PolygonData[] polygonData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int num = 0;
			if (this._polygonDataDict.ContainsKey(paramName) && this._polygonDataDict[paramName] != null)
			{
				polygonData = this._polygonDataDict[paramName];
			}
			else
			{
				PolygonArrayData roiPolygonArray = new PolygonArrayData();
				num = this.Algorithm.GetPolygonArrayData(InternalMethods.RepairName(paramName), ref roiPolygonArray);
				if (num != 0)
				{
					return num;
				}
				polygonData = DataConvert.PolygonArrayToData(roiPolygonArray);
				this._polygonDataDict[paramName] = polygonData;
			}
			return num;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x000031F7 File Offset: 0x000013F7
		public int SetPointArrayValue(string key, PointData[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.Algorithm.SetPointArrayData(InternalMethods.RepairName(key), DataConvert.PointDataToArray(valueArray));
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00003228 File Offset: 0x00001428
		public int GetPointArrayValue(string paramName, ref PointData[] pointData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int num = 0;
			if (this._pointDataDict.ContainsKey(paramName) && this._pointDataDict[paramName] != null)
			{
				pointData = this._pointDataDict[paramName];
			}
			else
			{
				PointArrayData valueArray = new PointArrayData();
				num = this.Algorithm.GetPointArrayData(InternalMethods.RepairName(paramName), ref valueArray);
				if (num != 0)
				{
					return num;
				}
				pointData = DataConvert.PointArrayToData(valueArray);
				this._pointDataDict[paramName] = pointData;
			}
			return num;
		}

		// Token: 0x0600002F RID: 47 RVA: 0x000032A7 File Offset: 0x000014A7
		public int SetLineArrayValue(string key, LineData[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.Algorithm.SetLineArrayData(InternalMethods.RepairName(key), DataConvert.LineDataToArray(valueArray));
		}

		// Token: 0x06000030 RID: 48 RVA: 0x000032D8 File Offset: 0x000014D8
		public int GetLineArrayValue(string paramName, ref LineData[] lineData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int num = 0;
			if (this._lineDataDict.ContainsKey(paramName) && this._lineDataDict[paramName] != null)
			{
				lineData = this._lineDataDict[paramName];
			}
			else
			{
				LineArrayData valueArray = new LineArrayData();
				num = this.Algorithm.GetLineArrayData(InternalMethods.RepairName(paramName), ref valueArray);
				if (num != 0)
				{
					return num;
				}
				lineData = DataConvert.LineArrayToData(valueArray);
				this._lineDataDict[paramName] = lineData;
			}
			return num;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003357 File Offset: 0x00001557
		public int SetFixtureArrayValue(string key, FixtureData[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.Algorithm.SetFixtureArrayData(InternalMethods.RepairName(key), DataConvert.FixtureDataToArray(valueArray));
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003388 File Offset: 0x00001588
		public int GetFixtureArrayValue(string paramName, ref FixtureData[] fixtureData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int num = 0;
			if (this._fixtureDataDict.ContainsKey(paramName) && this._fixtureDataDict[paramName] != null)
			{
				fixtureData = this._fixtureDataDict[paramName];
			}
			else
			{
				FixtureArrayData valueArray = new FixtureArrayData();
				num = this.Algorithm.GetFixtureArrayData(InternalMethods.RepairName(paramName), ref valueArray);
				if (num != 0)
				{
					return num;
				}
				fixtureData = DataConvert.FixtureArrayToData(valueArray);
				this._fixtureDataDict[paramName] = fixtureData;
			}
			return num;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003407 File Offset: 0x00001607
		public int SetCircleArrayValue(string key, CircleData[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.Algorithm.SetCircleArrayData(InternalMethods.RepairName(key), DataConvert.CircleDataToArray(valueArray));
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003438 File Offset: 0x00001638
		public int GetCircleArrayValue(string paramName, ref CircleData[] circleData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int num = 0;
			if (this._circleDataDict.ContainsKey(paramName) && this._circleDataDict[paramName] != null)
			{
				circleData = this._circleDataDict[paramName];
			}
			else
			{
				CircleArrayData valueArray = new CircleArrayData();
				num = this.Algorithm.GetCircleArrayData(InternalMethods.RepairName(paramName), ref valueArray);
				if (num != 0)
				{
					return num;
				}
				circleData = DataConvert.CircleArrayToData(valueArray);
				this._circleDataDict[paramName] = circleData;
			}
			return num;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x000034B7 File Offset: 0x000016B7
		public int SetRectArrayValue(string key, RectData[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.Algorithm.SetRectArrayData(InternalMethods.RepairName(key), DataConvert.RectDataToArray(valueArray));
		}

		// Token: 0x06000036 RID: 54 RVA: 0x000034E8 File Offset: 0x000016E8
		public int GetRectArrayValue(string paramName, ref RectData[] rectData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int num = 0;
			if (this._rectDataDict.ContainsKey(paramName) && this._rectDataDict[paramName] != null)
			{
				rectData = this._rectDataDict[paramName];
			}
			else
			{
				RectArrayData valueArray = new RectArrayData();
				num = this.Algorithm.GetRectArrayData(InternalMethods.RepairName(paramName), ref valueArray);
				if (num != 0)
				{
					return num;
				}
				rectData = DataConvert.RectArrayToData(valueArray);
				this._rectDataDict[paramName] = rectData;
			}
			return num;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003567 File Offset: 0x00001767
		public int SetEllipseArrayValue(string key, EllipseData[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.Algorithm.SetEllipseArrayData(InternalMethods.RepairName(key), DataConvert.EllipseDataToArray(valueArray));
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00003598 File Offset: 0x00001798
		public int GetEllipseArrayValue(string paramName, ref EllipseData[] ellipseData)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int num = 0;
			if (this._ellipseDataDict.ContainsKey(paramName) && this._ellipseDataDict[paramName] != null)
			{
				ellipseData = this._ellipseDataDict[paramName];
			}
			else
			{
				EllipseArrayData valueArray = new EllipseArrayData();
				num = this.Algorithm.GetEllipseArrayData(InternalMethods.RepairName(paramName), ref valueArray);
				if (num != 0)
				{
					return num;
				}
				ellipseData = DataConvert.EllipseArrayToData(valueArray);
				this._ellipseDataDict[paramName] = ellipseData;
			}
			return num;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00003617 File Offset: 0x00001817
		public int SetContourPointArrayValue(string key, byte[] valueArray)
		{
			if (this.Algorithm == null || valueArray == null || valueArray.Length == 0)
			{
				return -536870911;
			}
			return this.Algorithm.SetPointsetData(InternalMethods.RepairName(key), valueArray);
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003644 File Offset: 0x00001844
		public int GetContourPointArrayValue(string paramName, ref byte[] valueArray)
		{
			if (this.Algorithm == null)
			{
				return -536870911;
			}
			int num = 0;
			if (this._contourPointDataDict.ContainsKey(paramName) && this._contourPointDataDict[paramName] != null)
			{
				valueArray = this._contourPointDataDict[paramName];
			}
			else
			{
				num = this.Algorithm.GetPointsetData(InternalMethods.RepairName(paramName), ref valueArray);
				if (num == 0)
				{
					this._contourPointDataDict[paramName] = valueArray;
				}
			}
			return num;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x000036B4 File Offset: 0x000018B4
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

		// Token: 0x04000002 RID: 2
		private IAlgorithm Algorithm;

		// Token: 0x04000003 RID: 3
		private Dictionary<string, int[]> _intDataDict = new Dictionary<string, int[]>();

		// Token: 0x04000004 RID: 4
		private Dictionary<string, float[]> _floatDataDict = new Dictionary<string, float[]>();

		// Token: 0x04000005 RID: 5
		private Dictionary<string, string[]> _stringDataDict = new Dictionary<string, string[]>();

		// Token: 0x04000006 RID: 6
		private Dictionary<string, RoiboxData[]> _roiBoxDataDict = new Dictionary<string, RoiboxData[]>();

		// Token: 0x04000007 RID: 7
		private Dictionary<string, AnnulusData[]> _annulusDataDict = new Dictionary<string, AnnulusData[]>();

		// Token: 0x04000008 RID: 8
		private Dictionary<string, PolygonData[]> _polygonDataDict = new Dictionary<string, PolygonData[]>();

		// Token: 0x04000009 RID: 9
		private Dictionary<string, PointData[]> _pointDataDict = new Dictionary<string, PointData[]>();

		// Token: 0x0400000A RID: 10
		private Dictionary<string, LineData[]> _lineDataDict = new Dictionary<string, LineData[]>();

		// Token: 0x0400000B RID: 11
		private Dictionary<string, FixtureData[]> _fixtureDataDict = new Dictionary<string, FixtureData[]>();

		// Token: 0x0400000C RID: 12
		private Dictionary<string, CircleData[]> _circleDataDict = new Dictionary<string, CircleData[]>();

		// Token: 0x0400000D RID: 13
		private Dictionary<string, RectData[]> _rectDataDict = new Dictionary<string, RectData[]>();

		// Token: 0x0400000E RID: 14
		private Dictionary<string, EllipseData[]> _ellipseDataDict = new Dictionary<string, EllipseData[]>();

		// Token: 0x0400000F RID: 15
		private Dictionary<string, byte[]> _contourPointDataDict = new Dictionary<string, byte[]>();
	}
}
