using System;

namespace Script.Algorithm
{
	// Token: 0x02000002 RID: 2
	public interface IAlgorithm
	{
		// Token: 0x06000001 RID: 1
		void Dispose();

		// Token: 0x06000002 RID: 2
		void SetInOutputHandle(long input, long output);

		// Token: 0x06000003 RID: 3
		void SetData(string key, object obj);

		// Token: 0x06000004 RID: 4
		int GetObjectValue(string paramKey, int type, int index, ref object paramValue, ref int arrayCount, int moduleId = -1);

		// Token: 0x06000005 RID: 5
		int GetFloatArrayValue(string paramKey, ref float[] paramValue);

		// Token: 0x06000006 RID: 6
		int GetIntArrayValue(string paramKey, ref int[] paramValue);

		// Token: 0x06000007 RID: 7
		int GetObjectArrayValue(string paramKey, int type, ref string[] paramValue, int moduleId = -1);

		// Token: 0x06000008 RID: 8
		int SetObjectValue(int index, int type, string paramKey, object paramValue);

		// Token: 0x06000009 RID: 9
		int SetBasicArrayValue(int type, string paramKey, object paramValue);

		// Token: 0x0600000A RID: 10
		int SetImageData(string paramKey, int type, byte[] imageBuffer, int nWidth, int nHeight, int nPxiFormat);

		// Token: 0x0600000B RID: 11
		int GetImageData(string paramKey, int type, ref byte[] imageBuffer, ref int nWidth, ref int nHeight, ref int nPxiFormat);

		// Token: 0x0600000C RID: 12
		int SetRoiBoxData(string paramKey, int type, int index, float fCenterX, float fCenterY, float fWidth, float fHeight, float fAngle);

		// Token: 0x0600000D RID: 13
		int GetRoiBoxData(string paramKey, int type, ref float fCenterX, ref float fCenterY, ref float fWidth, ref float fHeight, ref float fAngle);

		// Token: 0x0600000E RID: 14
		int SetObjectValueForModule(int ModuleID, string paramName, string paramValue, int valuetype);

		// Token: 0x0600000F RID: 15
		int GetObjectArrayValueForModule(int moduleId, int index, string paramKey, ref int nType, ref Array paramValue);

		// Token: 0x06000010 RID: 16
		int GetModuleParamValue(int ModuleID, string paramName, ref string paramValue);

		// Token: 0x06000011 RID: 17
		int SetRoiBoxArrayData(string paramKey, RoiBoxArrayData roiBoxArray);

		// Token: 0x06000012 RID: 18
		int GetRoiBoxArrayData(string paramKey, ref RoiBoxArrayData roiBoxArray);

		// Token: 0x06000013 RID: 19
		int SetAnnulusArrayData(string paramKey, AnnulusArrayData annulusArray);

		// Token: 0x06000014 RID: 20
		int GetAnnulusArrayData(string paramKey, ref AnnulusArrayData annulusArray);

		// Token: 0x06000015 RID: 21
		int SetPolygonArrayData(string paramKey, PolygonArrayData polygonArray);

		// Token: 0x06000016 RID: 22
		int GetPolygonArrayData(string paramKey, ref PolygonArrayData polygonArray);

		// Token: 0x06000017 RID: 23
		int SetPointArrayData(string paramKey, PointArrayData pointArray);

		// Token: 0x06000018 RID: 24
		int GetPointArrayData(string paramKey, ref PointArrayData pointArray);

		// Token: 0x06000019 RID: 25
		int SetLineArrayData(string paramKey, LineArrayData lineArray);

		// Token: 0x0600001A RID: 26
		int GetLineArrayData(string paramKey, ref LineArrayData lineArray);

		// Token: 0x0600001B RID: 27
		int SetFixtureArrayData(string paramKey, FixtureArrayData fixtureArray);

		// Token: 0x0600001C RID: 28
		int GetFixtureArrayData(string paramKey, ref FixtureArrayData fixtureArray);

		// Token: 0x0600001D RID: 29
		int SetCircleArrayData(string paramKey, CircleArrayData circleArray);

		// Token: 0x0600001E RID: 30
		int GetCircleArrayData(string paramKey, ref CircleArrayData circleArray);

		// Token: 0x0600001F RID: 31
		int SetRectArrayData(string paramKey, RectArrayData rectArray);

		// Token: 0x06000020 RID: 32
		int GetRectArrayData(string paramKey, ref RectArrayData rectArray);

		// Token: 0x06000021 RID: 33
		int SetEllipseArrayData(string paramKey, EllipseArrayData ellipseArray);

		// Token: 0x06000022 RID: 34
		int GetEllipseArrayData(string paramKey, ref EllipseArrayData ellipseArray);

		// Token: 0x06000023 RID: 35
		int SetPointsetData(string paramKey, byte[] pointset);

		// Token: 0x06000024 RID: 36
		int GetPointsetData(string paramKey, ref byte[] pointset);

		// Token: 0x06000025 RID: 37
		int GetLocalVarModuleID(ref int nVarID);

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000026 RID: 38
		int ModuleID { get; }
	}
}
