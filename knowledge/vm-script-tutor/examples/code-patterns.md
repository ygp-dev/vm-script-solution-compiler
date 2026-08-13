# VM 脚本代码模式库

本文件提炼常见 VM 脚本场景的代码模式。

**通用约定**：

- 所有模式使用直接赋值方式读写变量，不使用 Get/Set 遗留接口
- `Process()` 内部业务逻辑应包裹在 `try/catch` 中，异常信息写入类字段 `errorStatus`（默认名）
- 类内需声明 `string errorStatus = string.Empty;` 字段；仅展示 `Process()` 方法片段的模式（模式 2/3/5/6/7/8/10/11/13a/13b/13c）省略了类声明，请在复制时自行补充类声明、`processCount` 字段（模式 6/11 用到了它）和 `Init()` 初始化；模式 6 的辅助方法 `SplitRoiGrid` 已包含在片段内
- **默认不生成** `ShowMessageBox` / `ConsoleWrite` / 任何日志调用；下列模式中遇到错误分支时仅写 `errorStatus` 并 `return false`
- 仅当用户在写代码前明确要求时，才在异常分支添加调试输出
- **Mat / Bitmap 等 IDisposable 资源应声明在 try 块外、在 finally 块中释放**（见模式 4 和 4b），避免异常时泄漏非托管内存

---

## 模式 1：基础数据透传

**场景**：获取输入，简单处理，设置输出

```csharp
using System;
using Script.Methods;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;
    string errorStatus = string.Empty;

    public void Init()
    {
        processCount = 0;
        errorStatus = string.Empty;
    }

    /// <summary>
    /// 流程执行函数
    /// </summary>
    public bool Process()
    {
        try
        {
            errorStatus = string.Empty;

            // 直接赋值获取输入
            int inputValue = in0;

            // 处理逻辑
            int result = inputValue * 2;

            // 直接赋值设置输出
            out0 = result;

            processCount++;
            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "Process 异常：" + ex.Message;
            return false;
        }
    }
}
```

---

## 模式 2：多类型数据读写

**场景**：同时处理 int、float、string、byte[] 等多种类型

```csharp
/// <summary>
/// 流程执行函数
/// </summary>
public bool Process()
{
    try
    {
        errorStatus = string.Empty;

        // 直接赋值获取各类型输入
        int intVal = in0;
        float floatVal = in1;
        string strVal = in2;
        byte[] byteVal = in3;

        // 处理逻辑...

        // 直接赋值设置各类型输出
        out0 = intVal;
        out1 = floatVal;
        out2 = strVal;
        out3 = byteVal;

        return true;
    }
    catch (Exception ex)
    {
        errorStatus = "Process 异常：" + ex.Message;
        return false;
    }
}
```

---

## 模式 3：数组数据处理

**场景**：批量处理数组数据

```csharp
/// <summary>
/// 流程执行函数
/// </summary>
public bool Process()
{
    try
    {
        errorStatus = string.Empty;

        // 直接赋值获取数组输入
        int[] inputArr = in0;

        // 处理逻辑 — 例如：每个元素乘以系数
        int factor = factorIn;

        int[] resultArr = new int[inputArr.Length];
        for (int i = 0; i < inputArr.Length; i++)
        {
            resultArr[i] = inputArr[i] * factor;
        }

        // 直接赋值设置数组输出
        out0 = resultArr;

        return true;
    }
    catch (Exception ex)
    {
        errorStatus = "Process 异常：" + ex.Message;
        return false;
    }
}
```

---

## 模式 4：图像处理管道（含 OpenCV）

**场景**：使用 OpenCvSharp 进行图像处理

```csharp
using System;
using Script.Methods;
using OpenCvSharp;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;
    string errorStatus = string.Empty;

    public void Init()
    {
        processCount = 0;
        errorStatus = string.Empty;
    }

    /// <summary>
    /// 流程执行函数
    /// </summary>
    public bool Process()
    {
        Mat srcMat = null;
        Mat resultMat = null;
        try
        {
            errorStatus = string.Empty;

            // 1. 直接赋值获取输入图像
            ImageData imgIn = inputImage;

            if (imgIn.Buffer == null || imgIn.Width <= 0 || imgIn.Height <= 0)
            {
                errorStatus = "图像数据无效";
                return false;
            }

            // 2. ImageData → Mat（转换方法见 references/Script.ExMethods.cs）
            srcMat = ImageDataToMat(imgIn);

            // 3. 图像处理逻辑（在此替换为实际算法）
            resultMat = new Mat();
            // ... 算法处理 ...

            // 4. Mat → ImageData
            ImageData imgOut = MatToImageData(resultMat);

            // 5. 直接赋值设置输出
            outputImage = imgOut;

            processCount++;
            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "Process 异常：" + ex.Message;
            return false;
        }
        finally
        {
            if (srcMat != null) srcMat.Dispose();
            if (resultMat != null) resultMat.Dispose();
        }
    }
}
```

> `ImageDataToMat` / `MatToImageData` 的完整实现见 [references/Script.ExMethods.cs](../references/Script.ExMethods.cs) 或 [examples/02-canny-edge-detection.cs](02-canny-edge-detection.cs)

---

## 模式 4b：图像处理管道（含 System.Drawing.Bitmap）

**场景**：使用 `System.Drawing.Bitmap` 进行图像处理（无需 OpenCvSharp）

```csharp
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Script.Methods;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;
    string errorStatus = string.Empty;

    public void Init()
    {
        processCount = 0;
        errorStatus = string.Empty;
    }

    /// <summary>
    /// 流程执行函数
    /// </summary>
    public bool Process()
    {
        Bitmap srcBmp = null;
        Bitmap resultBmp = null;
        try
        {
            errorStatus = string.Empty;

            // 1. 直接赋值获取输入图像
            ImageData imgIn = inputImage;

            if (imgIn.Buffer == null || imgIn.Width <= 0 || imgIn.Height <= 0)
            {
                errorStatus = "图像数据无效";
                return false;
            }

            // 2. ImageData → Bitmap（转换方法见 references/Script.ExMethods.cs）
            srcBmp = ImageDataToBitmap(imgIn);

            // 3. 图像处理逻辑（在此替换为实际算法）
            // 例如：使用 Graphics 绘制、像素级操作等
            resultBmp = ProcessWithBitmap(srcBmp);

            // 4. Bitmap → ImageData
            ImageData imgOut = BitmapToImageData(resultBmp);

            // 5. 直接赋值设置输出
            outputImage = imgOut;

            processCount++;
            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "Process 异常：" + ex.Message;
            return false;
        }
        finally
        {
            if (srcBmp != null) srcBmp.Dispose();
            if (resultBmp != null) resultBmp.Dispose();
        }
    }

    /// <summary>
    /// 使用 Bitmap 处理图像的示例方法
    /// </summary>
    /// <param name="srcBmp">输入 Bitmap</param>
    /// <returns>处理后的 Bitmap</returns>
    private Bitmap ProcessWithBitmap(Bitmap srcBmp)
    {
        // 示例：创建副本并处理
        Bitmap result = new Bitmap(srcBmp.Width, srcBmp.Height, srcBmp.PixelFormat);
        using (Graphics g = Graphics.FromImage(result))
        {
            g.DrawImage(srcBmp, 0, 0);
            // 在此添加绘制/处理逻辑...
        }
        return result;
    }
}
```

> `BitmapToImageData` / `ImageDataToBitmap` 的完整实现见 [references/Script.ExMethods.cs](../references/Script.ExMethods.cs)。支持 `Format8bppIndexed`（MONO8）和 `Format24bppRgb`（RGB24）两种像素格式，转换时自动处理 stride 对齐和 BGR/RGB 通道交换。

---

## 模式 4c：图像处理管道（含 Halcon）

**场景**：使用 HalconDotNet 进行图像处理与格式转换

```csharp
using System;
using System.Runtime.InteropServices;
using Script.Methods;
using HalconDotNet;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;
    string errorStatus = string.Empty;

    public void Init()
    {
        processCount = 0;
        errorStatus = string.Empty;
    }

    /// <summary>
    /// 流程执行函数
    /// </summary>
    public bool Process()
    {
        HObject halconImage = null;
        try
        {
            errorStatus = string.Empty;

            // 1. 直接赋值获取输入图像
            ImageData imgIn = inputImage;

            if (imgIn.Buffer == null || imgIn.Width <= 0 || imgIn.Height <= 0)
            {
                errorStatus = "输入图像无效";
                return false;
            }

            // 2. ImageData → HObject
            halconImage = ImageDataToHalconImage(imgIn);
            if (halconImage == null)
            {
                errorStatus = "转换为 Halcon 图像失败";
                return false;
            }

            // 3. Halcon 图像处理逻辑（在此替换为实际算法）
            HObject processedImage = null;
            // ... 例如：threshold、connection、select_shape 等 ...
            // HOperatorSet.Threshold(halconImage, out processedImage, 128, 255);

            // 4. HObject → ImageData
            ImageData imgOut = HalconImageToImageData(processedImage);
            if (processedImage != null) processedImage.Dispose();

            // 5. 直接赋值设置输出
            outputImage = imgOut;

            processCount++;
            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "Process 异常：" + ex.Message;
            return false;
        }
        finally
        {
            if (halconImage != null) halconImage.Dispose();
        }
    }

    // HalconImageToImageData / ImageDataToHalconImage 的实现见
    // ../examples/05-halcon-image-conversion.cs
}
```

> `HalconImageToImageData` / `ImageDataToHalconImage` 的完整实现见 [examples/05-halcon-image-conversion.cs](05-halcon-image-conversion.cs)。注意事项：`ImageData` 和 `RoiboxData` 的高度字段拼写随 VM 版本变化（≤4.3 用 `Heigth`，≥4.4 用 `Height`），生成代码前须确认版本；转换函数不释放输入 `HObject`，调用方负责 Dispose；`Marshal.AllocHGlobal` 分配的内存在 finally 中通过 `FreeHGlobal` 释放。

---

## 模式 5：ROI 处理

**场景**：获取 ROI，进行几何变换或分割

> 以下代码片段中的 `roi.Height` / `cell.Height` 适用于 **VM 4.4 及以后**；**VM 4.3 及之前**请把所有 `RoiboxData` 的 `.Height` 替换为 `.Heigth`（历史拼写错误，`RoiboxData` 和 `ImageData` 均受影响，`RectData` 不变）。

```csharp
/// <summary>
/// 流程执行函数
/// </summary>
public bool Process()
{
    try
    {
        errorStatus = string.Empty;

        // 直接赋值获取 ROI 输入
        RoiboxData[] rois = roiIn;

        if (rois == null || rois.Length == 0)
        {
            errorStatus = "ROI 数据为空";
            return false;
        }

        // 处理逻辑 — 例如：计算第一个 ROI 的四个角点
        RoiboxData roi = rois[0];
        float angle = roi.Angle;
        float rad = angle * (float)Math.PI / 180f;
        float cosA = (float)Math.Cos(rad);
        float sinA = (float)Math.Sin(rad);

        float halfW = roi.Width / 2f;
        float halfH = roi.Height / 2f;

        float[][] corners = new float[4][];
        float[][] offsets = new float[][]
        {
            new float[] { -halfW, -halfH },
            new float[] {  halfW, -halfH },
            new float[] {  halfW,  halfH },
            new float[] { -halfW,  halfH }
        };

        for (int i = 0; i < 4; i++)
        {
            float rx = offsets[i][0] * cosA - offsets[i][1] * sinA;
            float ry = offsets[i][0] * sinA + offsets[i][1] * cosA;
            corners[i] = new float[] { roi.CenterX + rx, roi.CenterY + ry };
        }

        // 直接赋值设置输出...

        return true;
    }
    catch (Exception ex)
    {
        errorStatus = "Process 异常：" + ex.Message;
        return false;
    }
}
```

---

## 模式 6：ROI 网格分割

**场景**：将一个 ROI 分割为 rows × cols 的子 ROI 数组

```csharp
/// <summary>
/// 流程执行函数
/// </summary>
public bool Process()
{
    try
    {
        errorStatus = string.Empty;

        // 直接赋值获取输入
        int rows = rowsIn;
        int cols = colsIn;
        RoiboxData[] roiArr = roiIn;

        if (rows <= 0 || cols <= 0 || roiArr == null || roiArr.Length == 0)
        {
            errorStatus = "输入参数无效";
            return false;
        }

        RoiboxData roi = roiArr[0];
        RoiboxData[] result = SplitRoiGrid(roi, rows, cols);

        // 直接赋值设置输出
        roiOut = result;

        processCount++;
        return true;
    }
    catch (Exception ex)
    {
        errorStatus = "Process 异常：" + ex.Message;
        return false;
    }
}

/// <summary>
/// 将 ROI 按行列分割为子 ROI 数组
/// </summary>
/// <param name="roi">原始 ROI</param>
/// <param name="rows">行数</param>
/// <param name="cols">列数</param>
/// <returns>子 ROI 数组</returns>
private RoiboxData[] SplitRoiGrid(RoiboxData roi, int rows, int cols)
{
    float cellWidth = roi.Width / cols;
    float cellHeight = roi.Height / rows;
    float rad = roi.Angle * (float)Math.PI / 180f;
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
            cell.Angle = roi.Angle;
            result[index++] = cell;
        }
    }

    return result;
}
```

---

## 模式 7：全局变量控制流程

**场景**：通过全局变量在模块间传递状态或配置

```csharp
/// <summary>
/// 流程执行函数
/// </summary>
public bool Process()
{
    try
    {
        errorStatus = string.Empty;

        // 读取全局变量
        object val = GlobalVariableModule.GetValue("mode");
        int mode = int.Parse(val == null ? "0" : val.ToString());

        // 根据全局变量选择逻辑分支
        int result = 0;
        switch (mode)
        {
            case 0:
                result = ProcessModeA();
                break;
            case 1:
                result = ProcessModeB();
                break;
            default:
                errorStatus = "未知模式: " + mode;
                return false;
        }

        // 写入全局变量 + 直接赋值输出
        GlobalVariableModule.SetValue("result", result.ToString());
        out0 = result;

        return true;
    }
    catch (Exception ex)
    {
        errorStatus = "Process 异常：" + ex.Message;
        return false;
    }
}

/// <summary>
/// 模式 A 处理
/// </summary>
private int ProcessModeA()
{
    // 逻辑...
    return 0;
}

/// <summary>
/// 模式 B 处理
/// </summary>
private int ProcessModeB()
{
    // 逻辑...
    return 0;
}
```

---

## 模式 8：模块参数动态调整

**场景**：根据条件动态设置其他模块的参数

```csharp
/// <summary>
/// 流程执行函数
/// </summary>
public bool Process()
{
    try
    {
        errorStatus = string.Empty;

        // 获取某个模块的结果
        object obj = CurrentProcess.GetModule("图像源1").GetValue("Height");
        if (obj == null)
        {
            errorStatus = "获取模块结果失败";
            return false;
        }

        int height = int.Parse(obj.ToString());

        // 根据结果调整另一个模块的参数
        if (height > 1000)
        {
            CurrentProcess.GetModule("BLOB分析1").SetValue("FindNum", "10");
        }
        else
        {
            CurrentProcess.GetModule("BLOB分析1").SetValue("FindNum", "5");
        }

        return true;
    }
    catch (Exception ex)
    {
        errorStatus = "Process 异常：" + ex.Message;
        return false;
    }
}
```

---

## 模式 9：带状态的多次执行

**场景**：Process() 需要在多次执行间保持状态

```csharp
public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;
    int totalCount;
    float accumulatedValue;
    string errorStatus = string.Empty;

    public void Init()
    {
        processCount = 0;
        totalCount = 0;
        accumulatedValue = 0f;
        errorStatus = string.Empty;
    }

    /// <summary>
    /// 流程执行函数
    /// </summary>
    public bool Process()
    {
        try
        {
            errorStatus = string.Empty;

            processCount++;
            totalCount++;

            // 直接赋值获取输入
            float inputVal = in0;

            // 累积计算
            accumulatedValue += inputVal;

            // 每 N 次输出平均值
            if (totalCount % 10 == 0)
            {
                float avg = accumulatedValue / totalCount;
                out0 = avg;
                countOut = totalCount;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "Process 异常：" + ex.Message;
            return false;
        }
    }

    // 本模式无非托管资源，按"仅在需要显式释放资源时才编写 Dispose()"规则省略
}
```

---

## 模式 10：通信数据发送

**场景**：通过 TCP/PLC/Modbus 发送处理结果

```csharp
/// <summary>
/// 流程执行函数
/// </summary>
public bool Process()
{
    try
    {
        errorStatus = string.Empty;

        // 直接赋值获取处理结果
        int result = resultIn;

        // TCP/UDP/串口发送
        string msg = result.ToString();
        int ret = GlobalCommunicateModule.GetDevice(1).SendData(msg);
        if (ret != 0)
        {
            errorStatus = "TCP 发送失败";
            return false;
        }

        // PLC 发送
        ret = GlobalCommunicateModule.GetDevice(2).GetAddress(1).SendData(result.ToString(), DataType.IntType);
        if (ret != 0)
        {
            errorStatus = "PLC 发送失败";
            return false;
        }

        return true;
    }
    catch (Exception ex)
    {
        errorStatus = "Process 异常：" + ex.Message;
        return false;
    }
}
```

---

## 模式 11：异常处理模板

**场景**：标准 Process() 方法骨架，所有模式均以此为基础

```csharp
/// <summary>
/// 流程执行函数
/// </summary>
public bool Process()
{
    try
    {
        errorStatus = string.Empty;

        // 业务逻辑...

        processCount++;
        return true;
    }
    catch (Exception ex)
    {
        errorStatus = "Process 异常：" + ex.Message;
        return false;
    }
}
```

---

## 模式 12：资源管理模板

**场景**：需要使用非托管资源（Mat、文件句柄等）

```csharp
public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;
    string errorStatus = string.Empty;
    private Mat cachedMat;

    public void Init()
    {
        processCount = 0;
        errorStatus = string.Empty;
        cachedMat = new Mat();
    }

    /// <summary>
    /// 流程执行函数
    /// </summary>
    public bool Process()
    {
        try
        {
            errorStatus = string.Empty;

            // 使用缓存的资源
            // ...

            processCount++;
            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "Process 异常：" + ex.Message;
            return false;
        }
    }

    public virtual void Dispose()
    {
        if (cachedMat != null) cachedMat.Dispose();
    }
}
```

---

## 模式 13：模块运行参数设置与获取

**场景**：在脚本中动态设置算法模块的参数（如角度范围、查找数量），或读取模块的运行结果

> **参数 Key 必须从对应模块的 `AlgorithmTab.xml` 中查询，不得猜测**。查询前必须先查 `VisionMaster模块映射表.md`（位于 references/ 目录内）获取工具箱英文名和模块英文名，并向用户确认。完整流程见 SKILL.md §6。

### 13a：设置模块参数

```csharp
/// <summary>
/// 流程执行函数 — 动态设置轮廓匹配角度范围后，触发匹配
/// </summary>
public bool Process()
{
    try
    {
        errorStatus = string.Empty;

        // 从输入变量获取角度限制
        float angleStart = angleStartIn;  // 直接赋值，UserProperty.cs 中定义的 FLOAT 输入
        float angleEnd   = angleEndIn;

        // 设置模块运行参数（参数 Key 从 AlgorithmTab.xml 中查得）
        int ret = CurrentProcess.GetModule("轮廓匹配1").SetValue("AngleStart", angleStart.ToString());
        if (ret != 0)
        {
            errorStatus = "设置 AngleStart 失败，返回码：" + ret.ToString();
            return false;
        }

        ret = CurrentProcess.GetModule("轮廓匹配1").SetValue("AngleEnd", angleEnd.ToString());
        if (ret != 0)
        {
            errorStatus = "设置 AngleEnd 失败，返回码：" + ret.ToString();
            return false;
        }

        processCount++;
        return true;
    }
    catch (Exception ex)
    {
        errorStatus = "Process 异常：" + ex.Message;
        return false;
    }
}
```

### 13b：获取模块结果数据

```csharp
/// <summary>
/// 流程执行函数 — 读取上游模块的结果并写入本模块输出
/// </summary>
public bool Process()
{
    try
    {
        errorStatus = string.Empty;

        // 获取图像源模块的图像高度结果
        object heightObj = CurrentProcess.GetModule("图像源1").GetValue("Height");
        if (heightObj == null)
        {
            errorStatus = "获取模块结果失败：图像源1.Height 返回 null";
            return false;
        }

        int imgHeight = int.Parse(heightObj.ToString());

        // 获取 Group 内模块的结果
        object scoreObj = CurrentProcess.GetModule("Group1.轮廓匹配1").GetValue("MatchScore");
        float score = scoreObj == null ? 0f : float.Parse(scoreObj.ToString());

        // 写入本模块输出
        heightOut = imgHeight;
        scoreOut  = score;

        processCount++;
        return true;
    }
    catch (Exception ex)
    {
        errorStatus = "Process 异常：" + ex.Message;
        return false;
    }
}
```

### 13c：先设置参数再读结果（组合场景）

```csharp
/// <summary>
/// 流程执行函数 — 动态调整 BLOB 查找数量，再读取查找结果
/// </summary>
public bool Process()
{
    try
    {
        errorStatus = string.Empty;

        // 获取输入：期望的查找数量
        int findNum = findNumIn;

        // 设置 BLOB 分析模块参数（Key 来自 AlgorithmTab.xml）
        int ret = CurrentProcess.GetModule("BLOB分析1").SetValue("FindNum", findNum.ToString());
        if (ret != 0)
        {
            errorStatus = "设置 FindNum 失败，返回码：" + ret.ToString();
            return false;
        }

        // 读取结果（流程执行后由 VM 自动更新）
        object resultObj = CurrentProcess.GetModule("BLOB分析1").GetValue("BlobNum");
        int blobNum = resultObj == null ? 0 : int.Parse(resultObj.ToString());

        blobNumOut = blobNum;

        processCount++;
        return true;
    }
    catch (Exception ex)
    {
        errorStatus = "Process 异常：" + ex.Message;
        return false;
    }
}
```

---

## 模式组合指南

| 用户需求       | 推荐模式组合           |
| -------------- | ---------------------- |
| 简单计算       | 模式 1                 |
| 多类型数据转发 | 模式 2                 |
| 批量数据处理   | 模式 3                 |
| 图像滤波/检测  | 模式 4 + 模式 12       |
| 图像处理（无需 OpenCV） | 模式 4b + 模式 12 |
| 图像处理（Halcon） | 模式 4c             |
| ROI 分割/变换  | 模式 5 或 模式 6       |
| 流程控制/分支  | 模式 7 + 模式 8        |
| 统计/累积计算  | 模式 9                 |
| 结果通信发送   | 任意处理模式 + 模式 10 |
| 模块参数动态调整 | 模式 13a / 13c       |
| 读取模块运行结果 | 模式 13b             |
