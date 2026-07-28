using netDxf;
using netDxf.Entities;
using Script.Methods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using VisionDesigner;
using VisionDesigner.PositionFix;
using VM.Core;

/************************************
Shell Module default code: using .NET Framwwork 4.6.1
*************************************/
public partial class UserScript : ScriptMethods, IProcessMethods
{
    // 方案路径
    string strSolPath = "";
    // 文件夹路径
    string strDir = "";
    // dxf路径
    string strDxfPath = "";
    // dxf对象数据
    List<DxfEntityInfo> dxfData = new List<DxfEntityInfo>();

    public void Init()
    {

    }

    public bool Process()
    {
        // 方案路径
        strSolPath = VmSolution.Instance.SolutionPath;
        // 文件夹路径
        strDir = Path.GetDirectoryName(strSolPath);
        // dxf路径
        strDxfPath = Path.Combine(strDir, dxfPath + ".dxf");
        if (File.Exists(strDxfPath))
        {
            ReadDxf(strDxfPath);
            // 获取所有点
            List<System.Drawing.PointF> points = GetAllPoints(dxfData);
            // 位置修正变化所有的点集 
            List<PointData> outPoints = AdjustPoints(points, new System.Drawing.PointF(BasePoint[0].PointX, BasePoint[0].PointY), BaseAngle, new System.Drawing.PointF(RunPoint[0].PointX, RunPoint[0].PointY), RunAngle, width, height);
            // 输出位置修正的点集
            outPoint = outPoints.ToArray();
            // scale = -2 不缩放不平移(原始坐标) scale = -1 不缩放自适应中心   scale = 0 自动缩放   scale = 2 放大2倍
            ImageData img = DrawDxfEntities(dxfData,width,height,-2);
            // 输出图像
            SetImageValue("outImage", img);

        }

        return true;
    }

    public void ReadDxf(string path)
    {
        try
        {
            DxfDocument doc = DxfDocument.Load(path);
            dxfData.Clear();
            foreach (EntityObject entity in doc.En...