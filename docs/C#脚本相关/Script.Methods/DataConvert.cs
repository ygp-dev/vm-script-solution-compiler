using System;
using System.Collections.Generic;
using Script.Algorithm;

namespace Script.Methods
{
	// Token: 0x02000002 RID: 2
	public class DataConvert
	{
		// Token: 0x06000001 RID: 1 RVA: 0x000020D0 File Offset: 0x000002D0
		public static PointArrayData PointDataToArray(PointData[] valueArray)
		{
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			for (int i = 0; i < valueArray.Length; i++)
			{
				list.Add(valueArray[i].PointX);
				list2.Add(valueArray[i].PointY);
			}
			return new PointArrayData
			{
				Count = valueArray.Length,
				PointXArray = list.ToArray(),
				PointYArray = list2.ToArray()
			};
		}

		// Token: 0x06000002 RID: 2 RVA: 0x0000213C File Offset: 0x0000033C
		public static PointData[] PointArrayToData(PointArrayData valueArray)
		{
			PointData[] array = new PointData[valueArray.Count];
			for (int i = 0; i < valueArray.Count; i++)
			{
				array[i] = new PointData();
				array[i].PointX = valueArray.PointXArray[i];
				array[i].PointY = valueArray.PointYArray[i];
			}
			return array;
		}

		// Token: 0x06000003 RID: 3 RVA: 0x00002190 File Offset: 0x00000390
		public static CircleArrayData CircleDataToArray(CircleData[] valueArray)
		{
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			List<float> list3 = new List<float>();
			for (int i = 0; i < valueArray.Length; i++)
			{
				list.Add(valueArray[i].Radius);
				list2.Add(valueArray[i].CenterX);
				list3.Add(valueArray[i].CenterY);
			}
			return new CircleArrayData
			{
				Count = valueArray.Length,
				RadiusArray = list.ToArray(),
				CenterXArray = list2.ToArray(),
				CenterYArray = list3.ToArray()
			};
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002224 File Offset: 0x00000424
		public static CircleData[] CircleArrayToData(CircleArrayData valueArray)
		{
			CircleData[] array = new CircleData[valueArray.Count];
			for (int i = 0; i < valueArray.Count; i++)
			{
				array[i] = new CircleData();
				array[i].Radius = valueArray.RadiusArray[i];
				array[i].CenterX = valueArray.CenterXArray[i];
				array[i].CenterY = valueArray.CenterYArray[i];
			}
			return array;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002288 File Offset: 0x00000488
		public static LineArrayData LineDataToArray(LineData[] valueArray)
		{
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			List<float> list3 = new List<float>();
			List<float> list4 = new List<float>();
			for (int i = 0; i < valueArray.Length; i++)
			{
				list.Add(valueArray[i].StartPointX);
				list2.Add(valueArray[i].StartPointY);
				list3.Add(valueArray[i].EndPointX);
				list4.Add(valueArray[i].EndPointY);
			}
			return new LineArrayData
			{
				Count = valueArray.Length,
				StartPointXArray = list.ToArray(),
				StartPointYArray = list2.ToArray(),
				EndPointXArray = list3.ToArray(),
				EndPointYArray = list4.ToArray()
			};
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002344 File Offset: 0x00000544
		public static LineData[] LineArrayToData(LineArrayData valueArray)
		{
			LineData[] array = new LineData[valueArray.Count];
			for (int i = 0; i < valueArray.Count; i++)
			{
				array[i] = new LineData();
				array[i].StartPointX = valueArray.StartPointXArray[i];
				array[i].StartPointY = valueArray.StartPointYArray[i];
				array[i].EndPointX = valueArray.EndPointXArray[i];
				array[i].EndPointY = valueArray.EndPointYArray[i];
			}
			return array;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000023B8 File Offset: 0x000005B8
		public static RectArrayData RectDataToArray(RectData[] valueArray)
		{
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			List<float> list3 = new List<float>();
			List<float> list4 = new List<float>();
			for (int i = 0; i < valueArray.Length; i++)
			{
				list.Add(valueArray[i].CenterX);
				list2.Add(valueArray[i].CenterY);
				list3.Add(valueArray[i].Width);
				list4.Add(valueArray[i].Height);
			}
			return new RectArrayData
			{
				Count = valueArray.Length,
				CenterXArray = list.ToArray(),
				CenterYArray = list2.ToArray(),
				WidthArray = list3.ToArray(),
				HeightArray = list4.ToArray()
			};
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002474 File Offset: 0x00000674
		public static RectData[] RectArrayToData(RectArrayData valueArray)
		{
			RectData[] array = new RectData[valueArray.Count];
			for (int i = 0; i < valueArray.Count; i++)
			{
				array[i] = new RectData();
				array[i].CenterX = valueArray.CenterXArray[i];
				array[i].CenterY = valueArray.CenterYArray[i];
				array[i].Width = valueArray.WidthArray[i];
				array[i].Height = valueArray.HeightArray[i];
			}
			return array;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000024E8 File Offset: 0x000006E8
		public static EllipseArrayData EllipseDataToArray(EllipseData[] valueArray)
		{
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			List<float> list3 = new List<float>();
			List<float> list4 = new List<float>();
			List<float> list5 = new List<float>();
			for (int i = 0; i < valueArray.Length; i++)
			{
				list.Add(valueArray[i].CenterX);
				list2.Add(valueArray[i].CenterY);
				list3.Add(valueArray[i].MajorRadius);
				list4.Add(valueArray[i].MinorRadius);
				list5.Add(valueArray[i].Angle);
			}
			return new EllipseArrayData
			{
				Count = valueArray.Length,
				CenterXArray = list.ToArray(),
				CenterYArray = list2.ToArray(),
				MajorRadiusArray = list3.ToArray(),
				MinorRadiusArray = list4.ToArray(),
				AngleArray = list5.ToArray()
			};
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000025C8 File Offset: 0x000007C8
		public static EllipseData[] EllipseArrayToData(EllipseArrayData valueArray)
		{
			EllipseData[] array = new EllipseData[valueArray.Count];
			for (int i = 0; i < valueArray.Count; i++)
			{
				array[i] = new EllipseData();
				array[i].CenterX = valueArray.CenterXArray[i];
				array[i].CenterY = valueArray.CenterYArray[i];
				array[i].MajorRadius = valueArray.MajorRadiusArray[i];
				array[i].MinorRadius = valueArray.MinorRadiusArray[i];
				array[i].Angle = valueArray.AngleArray[i];
			}
			return array;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x0000264C File Offset: 0x0000084C
		public static RoiBoxArrayData RoiboxDataToArray(RoiboxData[] valueArray)
		{
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			List<float> list3 = new List<float>();
			List<float> list4 = new List<float>();
			List<float> list5 = new List<float>();
			for (int i = 0; i < valueArray.Length; i++)
			{
				list.Add(valueArray[i].CenterX);
				list2.Add(valueArray[i].CenterY);
				list3.Add(valueArray[i].Width);
				list4.Add(valueArray[i].Height);
				list5.Add(valueArray[i].Angle);
			}
			return new RoiBoxArrayData
			{
				Count = valueArray.Length,
				CenterXArray = list.ToArray(),
				CenterYArray = list2.ToArray(),
				WidthArray = list3.ToArray(),
				HeightArray = list4.ToArray(),
				AngleArray = list5.ToArray()
			};
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000272C File Offset: 0x0000092C
		public static RoiboxData[] RoiboxArrayToData(RoiBoxArrayData valueArray)
		{
			RoiboxData[] array = new RoiboxData[valueArray.Count];
			for (int i = 0; i < valueArray.Count; i++)
			{
				array[i] = new RoiboxData();
				array[i].CenterX = valueArray.CenterXArray[i];
				array[i].CenterY = valueArray.CenterYArray[i];
				array[i].Width = valueArray.WidthArray[i];
				array[i].Height = valueArray.HeightArray[i];
				array[i].Angle = valueArray.AngleArray[i];
			}
			return array;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000027B0 File Offset: 0x000009B0
		public static AnnulusArrayData AnnulusDataToArray(AnnulusData[] valueArray)
		{
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			List<float> list3 = new List<float>();
			List<float> list4 = new List<float>();
			List<float> list5 = new List<float>();
			List<float> list6 = new List<float>();
			for (int i = 0; i < valueArray.Length; i++)
			{
				list.Add(valueArray[i].CenterX);
				list2.Add(valueArray[i].CenterY);
				list3.Add(valueArray[i].InnerRadius);
				list4.Add(valueArray[i].OuterRadius);
				list5.Add(valueArray[i].StartAngle);
				list6.Add(valueArray[i].AngleExtend);
			}
			return new AnnulusArrayData
			{
				Count = valueArray.Length,
				CenterXArray = list.ToArray(),
				CenterYArray = list2.ToArray(),
				InnerRadiusArray = list3.ToArray(),
				OuterRadiusArray = list4.ToArray(),
				StartAngleArray = list5.ToArray(),
				AngleExtendArray = list6.ToArray()
			};
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000028B4 File Offset: 0x00000AB4
		public static AnnulusData[] AnnulusArrayToData(AnnulusArrayData valueArray)
		{
			AnnulusData[] array = new AnnulusData[valueArray.Count];
			for (int i = 0; i < valueArray.Count; i++)
			{
				array[i] = new AnnulusData();
				array[i].CenterX = valueArray.CenterXArray[i];
				array[i].CenterY = valueArray.CenterYArray[i];
				array[i].InnerRadius = valueArray.InnerRadiusArray[i];
				array[i].OuterRadius = valueArray.OuterRadiusArray[i];
				array[i].StartAngle = valueArray.StartAngleArray[i];
				array[i].AngleExtend = valueArray.AngleExtendArray[i];
			}
			return array;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002948 File Offset: 0x00000B48
		public static int PolygonDataToArray(PolygonData[] valueArray, ref PolygonArrayData roiPolygonArray)
		{
			List<int> list = new List<int>();
			float[][] array = new float[valueArray.Length][];
			float[][] array2 = new float[valueArray.Length][];
			for (int i = 0; i < valueArray.Length; i++)
			{
				if (valueArray[i].PointXArray == null || valueArray[i].PointYArray == null || valueArray[i].PointXArray.Length != valueArray[i].PointNum || valueArray[i].PointYArray.Length != valueArray[i].PointNum)
				{
					return -536870911;
				}
				list.Add(valueArray[i].PointNum);
				array[i] = valueArray[i].PointXArray;
				array2[i] = valueArray[i].PointYArray;
			}
			roiPolygonArray = new PolygonArrayData();
			roiPolygonArray.Count = valueArray.Length;
			roiPolygonArray.PointNumArray = list.ToArray();
			roiPolygonArray.PointsXArray = array;
			roiPolygonArray.PointsYArray = array2;
			return 0;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002A10 File Offset: 0x00000C10
		public static PolygonData[] PolygonArrayToData(PolygonArrayData roiPolygonArray)
		{
			PolygonData[] array = new PolygonData[roiPolygonArray.Count];
			for (int i = 0; i < roiPolygonArray.Count; i++)
			{
				array[i] = new PolygonData();
				array[i].PointNum = roiPolygonArray.PointNumArray[i];
				array[i].PointXArray = roiPolygonArray.PointsXArray[i];
				array[i].PointYArray = roiPolygonArray.PointsYArray[i];
			}
			return array;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002A74 File Offset: 0x00000C74
		public static FixtureArrayData FixtureDataToArray(FixtureData[] valueArray)
		{
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			List<float> list3 = new List<float>();
			List<float> list4 = new List<float>();
			List<float> list5 = new List<float>();
			List<float> list6 = new List<float>();
			List<float> list7 = new List<float>();
			List<float> list8 = new List<float>();
			List<float> list9 = new List<float>();
			List<float> list10 = new List<float>();
			for (int i = 0; i < valueArray.Length; i++)
			{
				list.Add(valueArray[i].InitPointX);
				list2.Add(valueArray[i].InitPointY);
				list3.Add(valueArray[i].InitAngle);
				list4.Add(valueArray[i].InitScaleX);
				list5.Add(valueArray[i].InitScaleY);
				list6.Add(valueArray[i].RunPointX);
				list7.Add(valueArray[i].RunPointY);
				list8.Add(valueArray[i].RunAngle);
				list9.Add(valueArray[i].RunScaleX);
				list10.Add(valueArray[i].RunScaleY);
			}
			return new FixtureArrayData
			{
				Count = valueArray.Length,
				InitPointXArray = list.ToArray(),
				InitPointYArray = list2.ToArray(),
				InitAngleArray = list3.ToArray(),
				InitScaleXArray = list4.ToArray(),
				InitScaleYArray = list5.ToArray(),
				RunPointXArray = list6.ToArray(),
				RunPointYArray = list7.ToArray(),
				RunAngleArray = list8.ToArray(),
				RunScaleXArray = list9.ToArray(),
				RunScaleYArray = list10.ToArray()
			};
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002C14 File Offset: 0x00000E14
		public static FixtureData[] FixtureArrayToData(FixtureArrayData valueArray)
		{
			FixtureData[] array = new FixtureData[valueArray.Count];
			for (int i = 0; i < valueArray.Count; i++)
			{
				array[i] = new FixtureData();
				array[i].InitPointX = valueArray.InitPointXArray[i];
				array[i].InitPointY = valueArray.InitPointYArray[i];
				array[i].InitAngle = valueArray.InitAngleArray[i];
				array[i].InitScaleX = valueArray.InitScaleXArray[i];
				array[i].InitScaleY = valueArray.InitScaleYArray[i];
				array[i].RunPointX = valueArray.RunPointXArray[i];
				array[i].RunPointY = valueArray.RunPointYArray[i];
				array[i].RunAngle = valueArray.RunAngleArray[i];
				array[i].RunScaleX = valueArray.RunScaleXArray[i];
				array[i].RunScaleY = valueArray.RunScaleYArray[i];
			}
			return array;
		}
	}
}
