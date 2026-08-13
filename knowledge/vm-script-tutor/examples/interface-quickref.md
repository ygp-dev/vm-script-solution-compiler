# VM 脚本接口速查索引

本文件从 `references/` 中的原始 `.cs` 文件提炼，按**使用场景**分类，便于代码生成时快速定位接口签名。

> **重要**：VM 脚本的变量读写优先使用**直接赋值**方式（如 `out0 = in0`），不再使用 Get/Set 遗留接口。遗留接口仅作为动态变量名访问的后备方案保留。

---

## 1. 变量类型映射

### 标量类型（直接读写）

| VM 类型 | 脚本 C# 类型 | 读取示例                 | 写入示例        |
| ------- | ------------ | ------------------------ | --------------- |
| INT     | `int`        | `int val = in0;`         | `out0 = val;`   |
| FLOAT   | `float`      | `float val = in1;`       | `out1 = val;`   |
| STRING  | `string`     | `string val = in2;`      | `out2 = val;`   |
| DOUBLE  | `double`     | `double val = in3;`      | `out3 = val;`   |
| BYTE    | `byte[]`     | `byte[] val = in4;`      | `out4 = val;`   |
| IMAGE   | `ImageData`  | `ImageData img = imgIn;` | `imgOut = img;` |

### 数组类型（直接读写）

| VM 类型     | 脚本 C# 类型 | 读取示例              | 写入示例      |
| ----------- | ------------ | --------------------- | ------------- |
| INT 数组    | `int[]`      | `int[] arr = in0;`    | `out0 = arr;` |
| FLOAT 数组  | `float[]`    | `float[] arr = in1;`  | `out1 = arr;` |
| STRING 数组 | `string[]`   | `string[] arr = in2;` | `out2 = arr;` |
| DOUBLE 数组 | `double[]`   | `double[] arr = in3;` | `out3 = arr;` |

### 复合类型（`<Type>Data[]` 数组）

| VM 类型       | 脚本 C# 类型         | 读取示例                       | 写入示例      | 备注                                   |
| ------------- | -------------------- | ------------------------------ | ------------- | -------------------------------------- |
| POINT         | `PointData[]`        | `PointData[] pts = in0;`       | `out0 = pts;` |                                        |
| CIRCLE        | `CircleData[]`       | `CircleData[] cs = in0;`       | `out0 = cs;`  |                                        |
| ROIBOX        | `RoiboxData[]`       | `RoiboxData[] rs = in0;`       | `out0 = rs;`  | **矩形框/ROI/识别框 默认类型，带角度** |
| RECT          | `RectData[]`         | `RectData[] rs = in0;`         | `out0 = rs;`  | 不带角度的轴对齐矩形，使用较少         |
| LINE          | `LineData[]`         | `LineData[] ls = in0;`         | `out0 = ls;`  |                                        |
| ELLIPSE       | `EllipseData[]`      | `EllipseData[] es = in0;`      | `out0 = es;`  |                                        |
| ANNULUS       | `AnnulusData[]`      | `AnnulusData[] as = in0;`      | `out0 = as;`  |                                        |
| POLYGON       | `PolygonData[]`      | `PolygonData[] ps = in0;`      | `out0 = ps;`  |                                        |
| CONTOUR_POINT | `ContourPointData[]` | `ContourPointData[] cs = in0;` | `out0 = cs;`  |                                        |

---

## 2. 数据结构字段速查

### ImageData

```csharp
public class ImageData
{
    public byte[] Buffer { get; set; }
    public int Width { get; set; }
    // 高度字段名随 VM 版本变化：
    //   ≤ VM 4.3 → Heigth（历史拼写错误）
    //   ≥ VM 4.4 → Height（已修正）
    // 生成访问该字段的代码前，必须先确认用户的 VM 版本
    public ImagePixelFormate PixelFormat { get; set; }
}
```

**ImagePixelFormate**：`MONO8 = 17301505`（灰度），`RGB24 = 35127316`（彩色）

### RoiboxData

```csharp
public class RoiboxData
{
    public float CenterX { get; set; }
    public float CenterY { get; set; }
    public float Width { get; set; }
    public float Angle { get; set; }
    // 高度字段名随 VM 版本变化：
    //   ≤ VM 4.3 → Heigth（历史拼写错误）
    //   ≥ VM 4.4 → Height（已修正）
    // 生成访问该字段的代码前，必须先确认用户的 VM 版本
}
```

> **重要**：`RoiboxData` 是"矩形框 / ROI / 识别框"的默认类型（带角度）。`RectData` 只是不带角度的轴对齐矩形，使用频率较低。判断时以 `UserProperty.cs` 实际定义为准；若用户口语未明确，默认按 `RoiboxData[]`。

### PointData

```csharp
public class PointData
{
    public float PointX, PointY;
}
```

### 其他几何结构

| 结构               | 字段                                                                            |
| ------------------ | ------------------------------------------------------------------------------- |
| `CircleData`       | `CenterX`, `CenterY`, `Radius`                                                  |
| `RectData`         | `CenterX`, `CenterY`, `Width`, `Height`（始终为 `Height`，不受 VM 版本影响）   |
| `LineData`         | `StartPointX`, `StartPointY`, `EndPointX`, `EndPointY`                          |
| `EllipseData`      | `CenterX`, `CenterY`, `MajorRadius`, `MinorRadius`, `Angle`                     |
| `AnnulusData`      | `CenterX`, `CenterY`, `InnerRadius`, `OuterRadius`, `StartAngle`, `AngleExtend` |
| `PolygonData`      | `PointNum`, `PointXArray`, `PointYArray`                                        |
| `ContourPointData` | `PointX`, `PointY`, `PointScore`, `PointIndex`                                |

---

## 3. Mat ↔ ImageData 转换

见 [Script.ExMethods.cs](../references/Script.ExMethods.cs)

```csharp
// Mat → ImageData
ImageData imgOut = MatToImageData(matImage);

// ImageData → Mat
Mat matImage = ImageDataToMat(imgData);
```

---

## 3b. Bitmap ↔ ImageData 转换

见 [Script.ExMethods.cs](../references/Script.ExMethods.cs)

```csharp
// Bitmap → ImageData
ImageData imgOut = BitmapToImageData(bmpImage);

// ImageData → Bitmap
Bitmap bmpImage = ImageDataToBitmap(imgData);
```

**支持的像素格式：**

| Bitmap 像素格式 | ImageData 像素格式 | 说明 |
|---|---|---|
| `Format8bppIndexed` | `MONO8` | 灰度图，`ImageDataToBitmap` 会自动设置 256 级灰度调色板 |
| `Format24bppRgb` | `RGB24` | 彩色图，转换时自动交换 BGR↔RGB 通道顺序 |

**注意事项：**
- Bitmap 使用 BGR 通道顺序，ImageData 使用 RGB 通道顺序，转换方法内部自动处理
- `ImageDataToBitmap` 返回的 Bitmap 需要调用方在使用完毕后 `Dispose()`
- 不支持其他像素格式（如 `Format32bppArgb`），如需处理请先转换为上述两种格式

---

## 3c. Halcon ↔ ImageData 转换

见 [../examples/05-halcon-image-conversion.cs](../examples/05-halcon-image-conversion.cs)

```csharp
// HObject → ImageData
ImageData imgOut = HalconImageToImageData(hImageObj);

// ImageData → HObject
HObject halconImage = ImageDataToHalconImage(imgData);
```

**注意事项：**
- `ImageData` 和 `RoiboxData` 的高度字段拼写随 VM 版本变化（≤4.3 用 `Heigth`，≥4.4 用 `Height`），生成代码前须确认版本
- `HObject` 由调用方负责 `Dispose()`；转换函数不释放输入的 `hImageObj`
- `Marshal.AllocHGlobal` 分配的非托管内存必须在 finally 块中通过 `FreeHGlobal` 释放
- 仅支持 8bit 单通道（MONO8）和三通道（RGB24）图像

---

## 4. 全局变量

| 操作 | 接口                                                                         | 备注                                  |
| ---- | ---------------------------------------------------------------------------- | ------------------------------------- |
| 设置 | `GlobalVariableModule.SetValue(string paramName, string paramValue)` → `int` | 值统一为 string                       |
| 获取 | `GlobalVariableModule.GetValue(string paramName)` → `object`                 | 返回 object，需转 string 再转目标类型 |

**代码模式：**

```csharp
// 设置
GlobalVariableModule.SetValue("counter", "100");

// 获取并转换
object val = GlobalVariableModule.GetValue("counter");
int counter = int.Parse(val == null ? "0" : val.ToString());
```

---

## 5. 模块控制

| 操作     | 接口                                                                | 备注                         |
| -------- | ------------------------------------------------------------------- | ---------------------------- |
| 获取模块 | `CurrentProcess.GetModule(string moduleName)` → `Module`            | Group 内用 `"Group1.模块名"` |
| 获取结果 | `Module.GetValue(string paramValueName)` → `object`                 | 返回 null 表示异常           |
| 设置参数 | `Module.SetValue(string paramValueName, string paramValue)` → `int` | 0=成功；参数值统一为 string  |
| 获取运行参数（旧） | `GetModuleParam(uint nModuleID, string key, ref string value)` → `int` | 按模块 ID；0=成功 |

> **重要**：获取模块结果和设置模块运行参数都只能作用于**当前流程**。

### 参数 Key 查询规则（必须执行）

`SetValue` / `GetValue` 的参数名（Key）**必须从对应模块的 `AlgorithmTab.xml` 中查询，绝对禁止猜测**。

**查询前置步骤**：XML 位于以模块英文名命名的子目录下，因此必须先从 [`VisionMaster模块映射表.md`](../references/VisionMaster模块映射表.md) 查询模块所属的**工具箱英文名**和**模块英文名**，并**显式告知用户确认**后，再构造路径。

**路径构造公式**（确认后的英文名）：
```
{VM根目录}\Applications\Module(sp)\x64\{工具箱英文名}\{模块英文名}\{模块英文名}AlgorithmTab.xml
```

例如单点抓取：`{VM根目录}\Applications\Module(sp)\x64\Calculation\SinglePointGrabModu\SinglePointGrabModuAlgorithmTab.xml`

XML 节点示例：
```xml
<Param>
  <Name>AngleStart</Name>   <!-- ← Key 值 -->
  <MinValue>-180</MinValue>
  <MaxValue>180</MaxValue>
</Param>
```

详细查询流程（含映射表查询 + 目录校验 + 降级处理）见 SKILL.md §6。

**代码模式：**

```csharp
// 获取模块结果
object height = CurrentProcess.GetModule("图像源1").GetValue("Height");
int h = int.Parse(height == null ? "0" : height.ToString());

// 设置模块参数（Key 来自 AlgorithmTab.xml，工具箱/模块英文名来自映射表）
int ret = CurrentProcess.GetModule("单点抓取1").SetValue("TeachPointX", "100");
if (ret != 0) { errorStatus = "设置参数失败：" + ret.ToString(); return false; }

// Group 内模块
object result = CurrentProcess.GetModule("Group1.图像源1").GetValue("Height");
```

---

## 6. 通信发送

---

## 7. 调试与显示

> **默认不使用**。skill 在生成代码时不会自动添加任何 `ConsoleWrite` / `ShowMessageBox` / `ShowImage` 调用——仅当用户在实施大纲阶段明确要求"加调试输出"时才添加。错误处理一律走 `errorStatus` 字段。

| 操作           | 接口                                                          | 备注                 |
| -------------- | ------------------------------------------------------------- | -------------------- |
| DebugView 输出 | `ConsoleWrite(string content)`                                | 不会暂停流程；需外部 DebugView 工具查看 |
| 弹窗提示       | `ShowMessageBox(string msg)`                                  | **会暂停整个流程**，生产环境严禁使用 |
| 显示图像       | `ShowImage(ImageData imageData)`                              | 需 6210/7120 加密狗  |
| 绘制图形       | `DrawShape(object shapeData, ShapeConfig shapeConfig = null)` | 需 6210/7120 加密狗  |

---

## 8. 生命周期方法

| 方法        | 修饰                  | 时机              | 用途                     |
| ----------- | --------------------- | ----------------- | ------------------------ |
| `Init()`    | `public void`         | 加载方案/预编译   | 初始化变量、创建句柄     |
| `Process()` | `public bool`         | 每次流程执行      | 业务逻辑；返回 true=成功 |
| `Dispose()` | `public virtual void` | 关闭方案/重新编译 | 释放资源、关闭句柄       |

---

## 9. 遗留 Get/Set 接口（不推荐，仅后备）

以下接口保留用于方案兼容，**新代码不要使用**。只有当需要按变量名字符串动态访问时才考虑。

> **机制说明**：直接赋值（`int v = in0; out0 = v;`）并不是"和遗留接口对立"，而是 `UserProperty.cs` 中 partial property 对底层 Get/Set 的封装：
> - 标量（INT/FLOAT/STRING/ROIBOX 等）：property 内部调用 `GetIntValue` / `SetIntValue` / `GetRoiboxValue` 等 `ScriptMethods` 基类方法。
> - 数组（INT/FLOAT/STRING/复合类型数组）：property 内部调用 `(InternalObject as InternalMethods).GetXxxArrayValue(name, ref arr)` / `SetXxxArrayValue(key, arr)` 的 **2 参重载**（不是文档里的 3/4 参 `out arrayCount` 版本），完整签名见 [InternalMethods.cs](../references/InternalMethods.cs)。
>
> 因此普通脚本只需直接赋值；动态变量名访问时，标量走 `references/Script.Interface.cs`，数组走 `references/InternalMethods.cs`。

### 标量 Get 接口

| 接口                                                        | 返回值 |
| ----------------------------------------------------------- | ------ |
| `GetIntValue(string name, ref int value)` → `int`           | 0=成功 |
| `GetFloatValue(string name, ref float value)` → `int`       | 0=成功 |
| `GetStringValue(string name, ref string value)` → `int`     | 0=成功 |
| `GetBytesValue(string name, ref byte[] value)` → `int`      | 0=成功 |
| `GetDoubleValue(string name, ref double value)` → `int`     | 0=成功 |
| `GetIMAGEValue(string name, ref Image value)` → `int`       | 0=成功 |
| `GetRoiboxValue(string name, ref RoiboxData value)` → `int` | 0=成功 |

### 标量 Set 接口

| 接口                                                    | 返回值 |
| ------------------------------------------------------- | ------ |
| `SetIntValue(string key, int value)` → `int`            | 0=成功 |
| `SetFloatValue(string key, float value)` → `int`        | 0=成功 |
| `SetStringValue(string key, string value)` → `int`      | 0=成功 |
| `SetBytesValue(string key, byte[] value)` → `int`       | 0=成功 |
| `SetImageValue(string key, ImageData value)` → `int`    | 0=成功 |
| `SetRoiboxValue(string name, RoiboxData value)` → `int` | 0=成功 |

### 数组 Get/Set 接口

下表为 `ScriptMethods` 基类公开签名（3/4 参版本）。**partial property 实际使用的是 `Conceal.InternalMethods` 的 2 参重载**（见 [InternalMethods.cs](../references/InternalMethods.cs)），动态变量名访问时建议直接调用 2 参版本。

| 接口                                                                       | 备注           |
| -------------------------------------------------------------------------- | -------------- |
| `GetIntArrayValue(string name, ref int[] value, out int count)`            | count 用 `out` |
| `SetIntArrayValue(string key, int[] valueArray, int index, int len)`       | index 通常为 0 |
| `GetFloatArrayValue(string name, ref float[] value, out int count)`        | 同上           |
| `SetFloatArrayValue(string key, float[] valueArray, int index, int len)`   | 同上           |
| `GetStringArrayValue(string name, ref string[] value, out int count)`      | 同上           |
| `SetStringArrayValue(string key, string[] valueArray, int index, int len)` | 同上           |

**`InternalMethods` 2 参重载（partial property 实际走的版本）**：

| 接口                                                          | 用途           |
| ------------------------------------------------------------- | -------------- |
| `GetIntArrayValue(string name, ref int[] data)`               | 读 INT 数组    |
| `SetIntArrayValue(string key, int[] valueArray)`              | 写 INT 数组    |
| `GetFloatArrayValue(string name, ref float[] data)`           | 读 FLOAT 数组  |
| `SetFloatArrayValue(string key, float[] valueArray)`          | 写 FLOAT 数组  |
| `GetStringArrayValue(string name, ref string[] data)`         | 读 STRING 数组 |
| `SetStringArrayValue(string key, string[] valueArray)`        | 写 STRING 数组 |
| `GetRoiBoxArrayValue(string name, ref RoiboxData[] data)`     | 读 ROIBOX 数组 |
| `GetPointArrayValue(string name, ref PointData[] data)`       | 读 POINT 数组  |
| 其他复合类型（Line/Fixture/Circle/Annulus/Rect/Ellipse/...）  | 同模式         |

完整 2 参重载签名见 [InternalMethods.cs](../references/InternalMethods.cs)。

完整遗留接口签名见 [Script.Interface.cs](../references/Script.Interface.cs)（标量）与 [InternalMethods.cs](../references/InternalMethods.cs)（数组类 2 参版本）。

---

## 常见陷阱

1. **非数组 VM 类型 → `Data[]` 数组**：POINT 不是 `PointData`，而是 `PointData[]`
2. **IMAGE 特殊**：IMAGE 对应 `ImageData`（不是数组），其他复合类型都是 `Data[]`
3. **变量名唯一性**：多个变量不能使用同一个名称
4. **全局变量类型**：`SetValue` 和 `GetValue` 都用 `string` 传递，需手动转换

---

## 10. 常见运行时错误排查

| 现象                      | 可能原因                                       | 检查方式                                                       |
| ------------------------- | ---------------------------------------------- | -------------------------------------------------------------- |
| `NullReferenceException`  | 输入变量未连线或数据为空                       | 检查输入是否已绑定模块输出；脚本里应已通过 try/catch 写入 errorStatus |
| 类型转换错误              | 变量类型与赋值不匹配                           | 核对 `UserProperty.cs` 与脚本中的类型                          |
| 内存占用持续增长          | 未释放 Mat、每帧重复创建大对象                 | 所有 new Mat 用完 Dispose；大数组缓存为字段                    |
| 输出变量值不更新          | 未对输出变量赋值，或赋值放在了未执行的分支     | 确认输出变量在每个 `Process()` 分支末尾都被赋值                |
| 图像显示异常（黑图/错位） | 直接修改了只读的输入图像                       | 对需要修改的图像先 Mat 克隆                                    |
