// ============================================================
// 本文件为 Script.Methods.dll 反编译产物整理（来自 VM 4.3.0 安装目录），
// 仅供 *签名参考*，不可直接复制编译。语法已规范为 C# 5.0 兼容。
// 注意：文件中的内部包装类型（RoiBoxArrayData / RoiAnnulusArrayData /
// RoiPolygonArrayData / FixtureArrayData / CONTOUR_POINT_DATA 等）以及
// RoiPolygonData / RoiAnnulusData / FixtureData 未收录于 Script.DataStruct.cs，
// 用户侧脚本实际使用的类型以 Script.DataStruct.cs 与 SKILL.md 类型映射表为准。
// ============================================================
#region 程序集 Script.Methods, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ccd2b1cd14d6416e
// C:\Program Files\VisionMaster4.3.0\Applications\Module(sp)\x64\Logic\ShellModule\Script.Methods.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.Collections.Generic;
using Script.Algorithm;
using Script.Methods;

namespace Conceal
{

    public class InternalMethods
    {
        private IAlgorithm Algorithm;

        private Dictionary<string, int[]> _intDataDict = new Dictionary<string, int[]>();

        private Dictionary<string, float[]> _floatDataDict = new Dictionary<string, float[]>();

        private Dictionary<string, string[]> _stringDataDict = new Dictionary<string, string[]>();

        private Dictionary<string, RoiboxData[]> _roiBoxDataDict = new Dictionary<string, RoiboxData[]>();

        private Dictionary<string, RoiAnnulusData[]> _roiAnnulusDataDict = new Dictionary<string, RoiAnnulusData[]>();

        private Dictionary<string, RoiPolygonData[]> _roiPolygonDataDict = new Dictionary<string, RoiPolygonData[]>();

        private Dictionary<string, PointData[]> _pointDataDict = new Dictionary<string, PointData[]>();

        private Dictionary<string, LineData[]> _lineDataDict = new Dictionary<string, LineData[]>();

        private Dictionary<string, FixtureData[]> _fixtureDataDict = new Dictionary<string, FixtureData[]>();

        private Dictionary<string, CircleData[]> _circleDataDict = new Dictionary<string, CircleData[]>();

        private Dictionary<string, AnnulusData[]> _annulusDataDict = new Dictionary<string, AnnulusData[]>();

        private Dictionary<string, RectData[]> _rectDataDict = new Dictionary<string, RectData[]>();

        private Dictionary<string, EllipseData[]> _ellipseDataDict = new Dictionary<string, EllipseData[]>();

        private Dictionary<string, ContourPointData[]> _contourPointDataDict = new Dictionary<string, ContourPointData[]>();

        public void SetAlgorithm(IAlgorithm algorithm)
        {
            Algorithm = algorithm;
        }

        public void Clear()
        {
            _intDataDict.Clear();
            _floatDataDict.Clear();
            _stringDataDict.Clear();
            _roiBoxDataDict.Clear();
            _roiAnnulusDataDict.Clear();
            _roiPolygonDataDict.Clear();
            _pointDataDict.Clear();
            _lineDataDict.Clear();
            _fixtureDataDict.Clear();
            _circleDataDict.Clear();
            _annulusDataDict.Clear();
            _rectDataDict.Clear();
            _ellipseDataDict.Clear();
            _contourPointDataDict.Clear();
        }

        public int SetIntArrayValue(string key, int[] valueArray)
        {
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

            return Algorithm.SetBasicArrayValue(0, RepairName(key), (object)valueArray);
        }

        public int GetIntArrayValue(string paramName, ref int[] intData)
        {
            if (Algorithm == null)
            {
                return -536870911;
            }

            int result = 0;
            if (_intDataDict.ContainsKey(paramName) && _intDataDict[paramName] != null)
            {
                intData = _intDataDict[paramName];
            }
            else
            {
                result = Algorithm.GetIntArrayValue(RepairName(paramName), ref intData);
                _intDataDict[paramName] = intData;
            }

            return result;
        }

        public int SetFloatArrayValue(string key, float[] valueArray)
        {
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

            return Algorithm.SetBasicArrayValue(1, RepairName(key), (object)valueArray);
        }

        public int GetFloatArrayValue(string paramName, ref float[] floatData)
        {
            if (Algorithm == null)
            {
                return -536870911;
            }

            int result = 0;
            if (_floatDataDict.ContainsKey(paramName) && _floatDataDict[paramName] != null)
            {
                floatData = _floatDataDict[paramName];
            }
            else
            {
                result = Algorithm.GetFloatArrayValue(RepairName(paramName), ref floatData);
                _floatDataDict[paramName] = floatData;
            }

            return result;
        }

        public int SetStringArrayValue(string key, string[] valueArray)
        {
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

            int num = 0;
            for (int i = 0; i < valueArray.Length; i++)
            {
                num = Algorithm.SetObjectValue(i, 2, RepairName(key), (object)valueArray[i]);
                if (num != 0)
                {
                    break;
                }
            }

            return num;
        }

        public int GetStringArrayValue(string paramName, ref string[] stringData)
        {
            if (Algorithm == null)
            {
                return -536870911;
            }

            int result = 0;
            if (_stringDataDict.ContainsKey(paramName) && _stringDataDict[paramName] != null)
            {
                stringData = _stringDataDict[paramName];
            }
            else
            {
                result = Algorithm.GetObjectArrayValue(RepairName(paramName), 2, ref stringData, -1);
                _stringDataDict[paramName] = stringData;
            }

            return result;
        }

        public int SetRoiBoxArrayValue(string key, RoiboxData[] valueArray)
        {
            //IL_0093: Unknown result type (might be due to invalid IL or missing references)
            //IL_009a: Expected O, but got Unknown
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

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
                list4.Add(valueArray[i].Heigth);
                list5.Add(valueArray[i].Angle);
            }

            RoiBoxArrayData val = new RoiBoxArrayData();
            val.Count = valueArray.Length;
            val.CenterXArray = list.ToArray();
            val.CenterYArray = list2.ToArray();
            val.WidthArray = list3.ToArray();
            val.HeightArray = list4.ToArray();
            val.AngleArray = list5.ToArray();
            return Algorithm.SetRoiBoxArrayData(RepairName(key), val);
        }

        public int GetRoiBoxArrayValue(string paramName, ref RoiboxData[] roiBoxData)
        {
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0045: Expected O, but got Unknown
            if (Algorithm == null)
            {
                return -536870911;
            }

            int num = 0;
            if (_roiBoxDataDict.ContainsKey(paramName) && _roiBoxDataDict[paramName] != null)
            {
                roiBoxData = _roiBoxDataDict[paramName];
            }
            else
            {
                RoiBoxArrayData val = new RoiBoxArrayData();
                num = Algorithm.GetRoiBoxArrayData(RepairName(paramName), ref val);
                if (num != 0)
                {
                    return num;
                }

                roiBoxData = new RoiboxData[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    roiBoxData[i] = new RoiboxData();
                    roiBoxData[i].CenterX = val.CenterXArray[i];
                    roiBoxData[i].CenterY = val.CenterYArray[i];
                    roiBoxData[i].Width = val.WidthArray[i];
                    roiBoxData[i].Heigth = val.HeightArray[i];
                    roiBoxData[i].Angle = val.AngleArray[i];
                }

                _roiBoxDataDict[paramName] = roiBoxData;
            }

            return num;
        }

        public int SetRoiAnnulusArrayValue(string key, RoiAnnulusData[] valueArray)
        {
            //IL_00aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b1: Expected O, but got Unknown
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

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

            RoiAnnulusArrayData val = new RoiAnnulusArrayData();
            val.Count = valueArray.Length;
            val.CenterXArray = list.ToArray();
            val.CenterYArray = list2.ToArray();
            val.InnerRadiusArray = list3.ToArray();
            val.OuterRadiusArray = list4.ToArray();
            val.StartAngleArray = list5.ToArray();
            val.AngleExtendArray = list6.ToArray();
            return Algorithm.SetRoiAnnulusArrayData(RepairName(key), val);
        }

        public int GetRoiAnnulusArrayValue(string paramName, ref RoiAnnulusData[] roiAnnulusData)
        {
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0045: Expected O, but got Unknown
            if (Algorithm == null)
            {
                return -536870911;
            }

            int num = 0;
            if (_roiAnnulusDataDict.ContainsKey(paramName) && _roiAnnulusDataDict[paramName] != null)
            {
                roiAnnulusData = _roiAnnulusDataDict[paramName];
            }
            else
            {
                RoiAnnulusArrayData val = new RoiAnnulusArrayData();
                num = Algorithm.GetRoiAnnulusArrayData(RepairName(paramName), ref val);
                if (num != 0)
                {
                    return num;
                }

                roiAnnulusData = new RoiAnnulusData[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    roiAnnulusData[i] = new RoiAnnulusData();
                    roiAnnulusData[i].CenterX = val.CenterXArray[i];
                    roiAnnulusData[i].CenterY = val.CenterYArray[i];
                    roiAnnulusData[i].InnerRadius = val.InnerRadiusArray[i];
                    roiAnnulusData[i].OuterRadius = val.OuterRadiusArray[i];
                    roiAnnulusData[i].StartAngle = val.StartAngleArray[i];
                    roiAnnulusData[i].AngleExtend = val.AngleExtendArray[i];
                }

                _roiAnnulusDataDict[paramName] = roiAnnulusData;
            }

            return num;
        }

        public int SetRoiPolygonArrayValue(string key, RoiPolygonData[] valueArray)
        {
            //IL_00a2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a9: Expected O, but got Unknown
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

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

            RoiPolygonArrayData val = new RoiPolygonArrayData();
            val.Count = valueArray.Length;
            val.PointNumArray = list.ToArray();
            val.PointsXArray = array;
            val.PointsYArray = array2;
            return Algorithm.SetRoiPolygonArrayData(RepairName(key), val);
        }

        public int GetRoiPolygonArrayValue(string paramName, ref RoiPolygonData[] roiPolygonData)
        {
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0045: Expected O, but got Unknown
            if (Algorithm == null)
            {
                return -536870911;
            }

            int num = 0;
            if (_roiPolygonDataDict.ContainsKey(paramName) && _roiPolygonDataDict[paramName] != null)
            {
                roiPolygonData = _roiPolygonDataDict[paramName];
            }
            else
            {
                RoiPolygonArrayData val = new RoiPolygonArrayData();
                num = Algorithm.GetRoiPolygonArrayData(RepairName(paramName), ref val);
                if (num != 0)
                {
                    return num;
                }

                roiPolygonData = new RoiPolygonData[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    roiPolygonData[i] = new RoiPolygonData();
                    roiPolygonData[i].PointNum = val.PointNumArray[i];
                    roiPolygonData[i].PointXArray = val.PointsXArray[i];
                    roiPolygonData[i].PointYArray = val.PointsYArray[i];
                }

                _roiPolygonDataDict[paramName] = roiPolygonData;
            }

            return num;
        }

        public int SetPointArrayValue(string key, PointData[] valueArray)
        {
            //IL_004c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0052: Expected O, but got Unknown
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

            List<float> list = new List<float>();
            List<float> list2 = new List<float>();
            for (int i = 0; i < valueArray.Length; i++)
            {
                list.Add(valueArray[i].PointX);
                list2.Add(valueArray[i].PointY);
            }

            PointArrayData val = new PointArrayData();
            val.Count = valueArray.Length;
            val.PointXArray = list.ToArray();
            val.PointYArray = list2.ToArray();
            return Algorithm.SetPointArrayData(RepairName(key), val);
        }

        public int GetPointArrayValue(string paramName, ref PointData[] pointData)
        {
            //IL_003c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0042: Expected O, but got Unknown
            if (Algorithm == null)
            {
                return -536870911;
            }

            int num = 0;
            if (_pointDataDict.ContainsKey(paramName) && _pointDataDict[paramName] != null)
            {
                pointData = _pointDataDict[paramName];
            }
            else
            {
                PointArrayData val = new PointArrayData();
                num = Algorithm.GetPointArrayData(RepairName(paramName), ref val);
                if (num != 0)
                {
                    return num;
                }

                pointData = new PointData[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    pointData[i] = new PointData();
                    pointData[i].PointX = val.PointXArray[i];
                    pointData[i].PointY = val.PointYArray[i];
                }

                _pointDataDict[paramName] = pointData;
            }

            return num;
        }

        public int SetLineArrayValue(string key, LineData[] valueArray)
        {
            //IL_007c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0083: Expected O, but got Unknown
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

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

            LineArrayData val = new LineArrayData();
            val.Count = valueArray.Length;
            val.StartPointXArray = list.ToArray();
            val.StartPointYArray = list2.ToArray();
            val.EndPointXArray = list3.ToArray();
            val.EndPointYArray = list4.ToArray();
            return Algorithm.SetLineArrayData(RepairName(key), val);
        }

        public int GetLineArrayValue(string paramName, ref LineData[] lineData)
        {
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0045: Expected O, but got Unknown
            if (Algorithm == null)
            {
                return -536870911;
            }

            int num = 0;
            if (_lineDataDict.ContainsKey(paramName) && _lineDataDict[paramName] != null)
            {
                lineData = _lineDataDict[paramName];
            }
            else
            {
                LineArrayData val = new LineArrayData();
                num = Algorithm.GetLineArrayData(RepairName(paramName), ref val);
                if (num != 0)
                {
                    return num;
                }

                lineData = new LineData[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    lineData[i] = new LineData();
                    lineData[i].StartPointX = val.StartPointXArray[i];
                    lineData[i].StartPointY = val.StartPointYArray[i];
                    lineData[i].EndPointX = val.EndPointXArray[i];
                    lineData[i].EndPointY = val.EndPointYArray[i];
                }

                _lineDataDict[paramName] = lineData;
            }

            return num;
        }

        public int SetFixtureArrayValue(string key, FixtureData[] valueArray)
        {
            //IL_010c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0113: Expected O, but got Unknown
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

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

            FixtureArrayData val = new FixtureArrayData();
            val.Count = valueArray.Length;
            val.InitPointXArray = list.ToArray();
            val.InitPointYArray = list2.ToArray();
            val.InitAngleArray = list3.ToArray();
            val.InitScaleXArray = list4.ToArray();
            val.InitScaleYArray = list5.ToArray();
            val.RunPointXArray = list6.ToArray();
            val.RunPointYArray = list7.ToArray();
            val.RunAngleArray = list8.ToArray();
            val.RunScaleXArray = list9.ToArray();
            val.RunScaleYArray = list10.ToArray();
            return Algorithm.SetFixtureArrayData(RepairName(key), val);
        }

        public int GetFixtureArrayValue(string paramName, ref FixtureData[] fixtureData)
        {
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0045: Expected O, but got Unknown
            if (Algorithm == null)
            {
                return -536870911;
            }

            int num = 0;
            if (_fixtureDataDict.ContainsKey(paramName) && _fixtureDataDict[paramName] != null)
            {
                fixtureData = _fixtureDataDict[paramName];
            }
            else
            {
                FixtureArrayData val = new FixtureArrayData();
                num = Algorithm.GetFixtureArrayData(RepairName(paramName), ref val);
                if (num != 0)
                {
                    return num;
                }

                fixtureData = new FixtureData[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    fixtureData[i] = new FixtureData();
                    fixtureData[i].InitPointX = val.InitPointXArray[i];
                    fixtureData[i].InitPointY = val.InitPointYArray[i];
                    fixtureData[i].InitAngle = val.InitAngleArray[i];
                    fixtureData[i].InitScaleX = val.InitScaleXArray[i];
                    fixtureData[i].InitScaleY = val.InitScaleYArray[i];
                    fixtureData[i].RunPointX = val.RunPointXArray[i];
                    fixtureData[i].RunPointY = val.RunPointYArray[i];
                    fixtureData[i].RunAngle = val.RunAngleArray[i];
                    fixtureData[i].RunScaleX = val.RunScaleXArray[i];
                    fixtureData[i].RunScaleY = val.RunScaleYArray[i];
                }

                _fixtureDataDict[paramName] = fixtureData;
            }

            return num;
        }

        public int SetCircleArrayValue(string key, CircleData[] valueArray)
        {
            //IL_0060: Unknown result type (might be due to invalid IL or missing references)
            //IL_0067: Expected O, but got Unknown
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

            List<float> list = new List<float>();
            List<float> list2 = new List<float>();
            List<float> list3 = new List<float>();
            for (int i = 0; i < valueArray.Length; i++)
            {
                list.Add(valueArray[i].Radius);
                list2.Add(valueArray[i].CenterX);
                list3.Add(valueArray[i].CenterY);
            }

            CircleArrayData val = new CircleArrayData();
            val.Count = valueArray.Length;
            val.RadiusArray = list.ToArray();
            val.CenterXArray = list2.ToArray();
            val.CenterYArray = list3.ToArray();
            return Algorithm.SetCircleArrayData(RepairName(key), val);
        }

        public int GetCircleArrayValue(string paramName, ref CircleData[] circleData)
        {
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0045: Expected O, but got Unknown
            if (Algorithm == null)
            {
                return -536870911;
            }

            int num = 0;
            if (_circleDataDict.ContainsKey(paramName) && _circleDataDict[paramName] != null)
            {
                circleData = _circleDataDict[paramName];
            }
            else
            {
                CircleArrayData val = new CircleArrayData();
                num = Algorithm.GetCircleArrayData(RepairName(paramName), ref val);
                if (num != 0)
                {
                    return num;
                }

                circleData = new CircleData[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    circleData[i] = new CircleData();
                    circleData[i].Radius = val.RadiusArray[i];
                    circleData[i].CenterX = val.CenterXArray[i];
                    circleData[i].CenterY = val.CenterYArray[i];
                }

                _circleDataDict[paramName] = circleData;
            }

            return num;
        }

        public int SetAnnulusArrayValue(string key, AnnulusData[] valueArray)
        {
            //IL_00aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b1: Expected O, but got Unknown
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

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

            AnnulusArrayData val = new AnnulusArrayData();
            val.Count = valueArray.Length;
            val.CenterXArray = list.ToArray();
            val.CenterYArray = list2.ToArray();
            val.InnerRadiusArray = list3.ToArray();
            val.OuterRadiusArray = list4.ToArray();
            val.StartAngleArray = list5.ToArray();
            val.AngleExtendArray = list6.ToArray();
            return Algorithm.SetAnnulusArrayData(RepairName(key), val);
        }

        public int GetAnnulusArrayValue(string paramName, ref AnnulusData[] annulusData)
        {
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0045: Expected O, but got Unknown
            if (Algorithm == null)
            {
                return -536870911;
            }

            int num = 0;
            if (_annulusDataDict.ContainsKey(paramName) && _annulusDataDict[paramName] != null)
            {
                annulusData = _annulusDataDict[paramName];
            }
            else
            {
                AnnulusArrayData val = new AnnulusArrayData();
                num = Algorithm.GetAnnulusArrayData(RepairName(paramName), ref val);
                if (num != 0)
                {
                    return num;
                }

                annulusData = new AnnulusData[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    annulusData[i] = new AnnulusData();
                    annulusData[i].CenterX = val.CenterXArray[i];
                    annulusData[i].CenterY = val.CenterYArray[i];
                    annulusData[i].InnerRadius = val.InnerRadiusArray[i];
                    annulusData[i].OuterRadius = val.OuterRadiusArray[i];
                    annulusData[i].StartAngle = val.StartAngleArray[i];
                    annulusData[i].AngleExtend = val.AngleExtendArray[i];
                }

                _annulusDataDict[paramName] = annulusData;
            }

            return num;
        }

        public int SetRectArrayValue(string key, RectData[] valueArray)
        {
            //IL_007c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0083: Expected O, but got Unknown
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

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

            RectArrayData val = new RectArrayData();
            val.Count = valueArray.Length;
            val.CenterXArray = list.ToArray();
            val.CenterYArray = list2.ToArray();
            val.WidthArray = list3.ToArray();
            val.HeightArray = list4.ToArray();
            return Algorithm.SetRectArrayData(RepairName(key), val);
        }

        public int GetRectArrayValue(string paramName, ref RectData[] rectData)
        {
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0045: Expected O, but got Unknown
            if (Algorithm == null)
            {
                return -536870911;
            }

            int num = 0;
            if (_rectDataDict.ContainsKey(paramName) && _rectDataDict[paramName] != null)
            {
                rectData = _rectDataDict[paramName];
            }
            else
            {
                RectArrayData val = new RectArrayData();
                num = Algorithm.GetRectArrayData(RepairName(paramName), ref val);
                if (num != 0)
                {
                    return num;
                }

                rectData = new RectData[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    rectData[i] = new RectData();
                    rectData[i].CenterX = val.CenterXArray[i];
                    rectData[i].CenterY = val.CenterYArray[i];
                    rectData[i].Width = val.WidthArray[i];
                    rectData[i].Height = val.HeightArray[i];
                }

                _rectDataDict[paramName] = rectData;
            }

            return num;
        }

        public int SetEllipseArrayValue(string key, EllipseData[] valueArray)
        {
            //IL_0093: Unknown result type (might be due to invalid IL or missing references)
            //IL_009a: Expected O, but got Unknown
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

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

            EllipseArrayData val = new EllipseArrayData();
            val.Count = valueArray.Length;
            val.CenterXArray = list.ToArray();
            val.CenterYArray = list2.ToArray();
            val.MajorRadiusArray = list3.ToArray();
            val.MinorRadiusArray = list4.ToArray();
            val.AngleArray = list5.ToArray();
            return Algorithm.SetEllipseArrayData(RepairName(key), val);
        }

        public int GetEllipseArrayValue(string paramName, ref EllipseData[] ellipseData)
        {
            //IL_003f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0045: Expected O, but got Unknown
            if (Algorithm == null)
            {
                return -536870911;
            }

            int num = 0;
            if (_ellipseDataDict.ContainsKey(paramName) && _ellipseDataDict[paramName] != null)
            {
                ellipseData = _ellipseDataDict[paramName];
            }
            else
            {
                EllipseArrayData val = new EllipseArrayData();
                num = Algorithm.GetEllipseArrayData(RepairName(paramName), ref val);
                if (num != 0)
                {
                    return num;
                }

                ellipseData = new EllipseData[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    ellipseData[i] = new EllipseData();
                    ellipseData[i].CenterX = val.CenterXArray[i];
                    ellipseData[i].CenterY = val.CenterYArray[i];
                    ellipseData[i].MajorRadius = val.MajorRadiusArray[i];
                    ellipseData[i].MinorRadius = val.MinorRadiusArray[i];
                    ellipseData[i].Angle = val.AngleArray[i];
                }

                _ellipseDataDict[paramName] = ellipseData;
            }

            return num;
        }

        public int SetContourPointArrayValue(string key, ContourPointData[] valueArray)
        {
            if (Algorithm == null || valueArray == null || valueArray.Length == 0)
            {
                return -536870911;
            }

            CONTOUR_POINT_DATA[] array = (CONTOUR_POINT_DATA[])(object)new CONTOUR_POINT_DATA[valueArray.Length];
            for (int i = 0; i < valueArray.Length; i++)
            {
                if (valueArray[i] != null)
                {
                    array[i].PointX = valueArray[i].PointX;
                    array[i].PointY = valueArray[i].PointY;
                    array[i].PointScore = valueArray[i].PointScore;
                    array[i].PointIndex = valueArray[i].PointIndex;
                }
            }

            return Algorithm.SetPointsetData(RepairName(key), array);
        }

        public int GetContourPointArrayValue(string paramName, ref ContourPointData[] contourPointData)
        {
            if (Algorithm == null)
            {
                return -536870911;
            }

            int num = 0;
            if (_contourPointDataDict.ContainsKey(paramName) && _contourPointDataDict[paramName] != null)
            {
                contourPointData = _contourPointDataDict[paramName];
            }
            else
            {
                CONTOUR_POINT_DATA[] array = null;
                num = Algorithm.GetPointsetData(RepairName(paramName), ref array);
                if (array == null || array.Length == 0)
                {
                    return -536870910;
                }

                if (num != 0)
                {
                    return num;
                }

                contourPointData = new ContourPointData[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    contourPointData[i] = new ContourPointData();
                    contourPointData[i].PointX = array[i].PointX;
                    contourPointData[i].PointY = array[i].PointY;
                    contourPointData[i].PointScore = array[i].PointScore;
                    contourPointData[i].PointIndex = array[i].PointIndex;
                }

                _contourPointDataDict[paramName] = contourPointData;
            }

            return num;
        }

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
    }
}
#if false // 反编译日志
缓存中的 5 项
------------------
解析: "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
找到单个程序集: "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
从以下位置加载: "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1\mscorlib.dll"
------------------
解析: "Script.Algorithm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=924359b2a806567d"
无法按名称“Script.Algorithm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=924359b2a806567d”查找
#endif
