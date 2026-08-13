using System;
using System.Text;
using System.Windows.Forms;
using Script.Methods;
using CadParser;

/// <summary>
/// VisionMaster CAD 文件转换脚本示例 - DXF 文件解析与渲染
/// </summary>
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
    /// 流程执行函数 - 加载 DXF 文件并渲染为图像输出
    /// </summary>
    public bool Process()
    {
        CadDocument doc = null;
        var bitmap = (System.Drawing.Bitmap)null;
        try
        {
            errorStatus = string.Empty;

            // 直接赋值获取输入参数
            string filePath = 文件路径;
            double scale = 缩放比例;
            double bitmapScale = 渲染比例;

            if (string.IsNullOrEmpty(filePath))
            {
                errorStatus = "文件路径为空";
                return false;
            }

            // 加载 DXF 文件
            doc = CadDocument.Load(filePath);
            if (doc == null)
            {
                errorStatus = "加载 DXF 文件失败：" + filePath;
                return false;
            }

            // 解析实体
            doc.Parse();

            // 应用坐标缩放
            doc.ApplyScale(scale);

            // 渲染为位图
            bitmap = doc.Render(bitmapScale: bitmapScale);

            // 输出图像（将 Bitmap 转为 ImageData 由外部模块处理）
            输出图像路径 = filePath.Replace(".dxf", ".png");
            bitmap.Save(输出图像路径, System.Drawing.Imaging.ImageFormat.Png);

            processCount++;
            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "CAD 文件转换异常：" + ex.Message;
            return false;
        }
        finally
        {
            if (bitmap != null) bitmap.Dispose();
            if (doc != null) doc.Dispose();
        }
    }
}
