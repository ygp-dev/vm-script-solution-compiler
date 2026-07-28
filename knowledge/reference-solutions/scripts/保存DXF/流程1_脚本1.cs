using System;
using System.Text;
using System.Windows.Forms;
using Script.Methods;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using System.Collections.Generic;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;

    public void Init()
    {
        processCount = 0;
    }

    public bool Process()
    {
        ContourPointData[] contourPointDatas = new ContourPointData[pointCount];
        BytesToPointset(point, ref contourPointDatas);

        if (contourPointDatas == null || contourPointDatas.Length == 0)
        {
            MessageBox.Show("点数据为空");
            return false;
        }

        DxfDocument doc = new DxfDocument();

        foreach (var pt in contourPointDatas)
        {
            // 图像坐标转 DXF 坐标：X不变，Y需翻转
            double x = pt.PointX * Scale;
            double y = (H - pt.PointY) * Scale;

            Point dxfPoint = new Point(new netDxf.Vector2(x, y));
            doc.Entities.Add(dxfPoint);
        }

        string filePath = strPath;
        try
        {
            doc.Save(filePath);
            //MessageBox.Show("DXF 文件保存成功: " + filePath);
        }
        catch (Exception ex)
        {
            //MessageBox.Show("保存失败: " + ex.Message);
        }

        return true;
    }
}
