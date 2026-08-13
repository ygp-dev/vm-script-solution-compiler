using System;
using System.Text;
using System.Windows.Forms;
using Script.Methods;

// 注意：本示例假定 VM 4.4 及以后，RoiboxData 和 ImageData 高度字段名为 Height。
//      VM 4.3 及之前请把 .Height 全部替换为 .Heigth（历史拼写错误）。

public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;
    string errorStatus = string.Empty;

    /// <summary>
    /// 预编译时变量初始化
    /// </summary>
    public void Init()
    {
        processCount = 0;
        errorStatus = string.Empty;
    }

    /// <summary>
    /// 流程执行函数 - ROI 网格分割
    /// </summary>
    public bool Process()
    {
        try
        {
            errorStatus = string.Empty;

            int rows = 行数;
            int cols = 列数;
            RoiboxData roi = ROI;

            if (rows <= 0 || cols <= 0)
            {
                errorStatus = "行数和列数必须大于 0";
                return false;
            }

            if (roi == null)
            {
                errorStatus = "输入 ROI 为空";
                return false;
            }

            float cellWidth = roi.Width / cols;
            float cellHeight = roi.Height / rows;
            float angle = roi.Angle;
            float rad = angle * (float)Math.PI / 180f;
            float cosA = (float)Math.Cos(rad);
            float sinA = (float)Math.Sin(rad);

            RoiboxData[] result = new RoiboxData[rows * cols];
            int index = 0;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float offsetX = (c - (cols - 1) / 2f) * cellWidth;
                    float offsetY = (r - (rows - 1) / 2f) * cellHeight;

                    float rotatedX = offsetX * cosA - offsetY * sinA;
                    float rotatedY = offsetX * sinA + offsetY * cosA;

                    RoiboxData cell = new RoiboxData();
                    cell.CenterX = roi.CenterX + rotatedX;
                    cell.CenterY = roi.CenterY + rotatedY;
                    cell.Width = cellWidth;
                    cell.Height = cellHeight;
                    cell.Angle = angle;
                    result[index++] = cell;
                }
            }

            ROI数组 = result;

            processCount++;
            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "ROI 网格分割异常：" + ex.Message;
            return false;
        }
    }
}
