---
name: vm-script-tutor
description: "VisionMaster（VM）2D C# 脚本开发辅助。用户提到：帮我编写脚本、编写脚本，输入输出，图像处理，模块参数，全局变量，调试输出，VM 模块 UI 配置、VS 断点调试等相关内容时触发。此外，当工作目录路径包含 VisionMaster 或 UserScript 时，即使用户未提及脚本关键字，也应自动激活并询问用户是否需要编写脚本。优先输出可直接使用的 C# 代码。本 skill 仅覆盖 2D VisionMaster；3D VisionMaster 暂不支持。"
version: 1.2
author: Mr.Buer
tags:
  - VisionMaster
  - 脚本开发
  - C# 编程
  - 数据处理
  - 图像处理
  - 模块参数
  - 全局变量
  - 调试输出
  - UI 配置清单
  - VS 断点调试
---

# VisionMaster 脚本开发辅助技能（vm-script-tutor）

## 目标

帮助用户编写、修改、排查 VisionMaster **2D** C# 脚本；优先输出可直接使用的 C# 代码，并严格遵守 VM 接口约束。在生成脚本的同时附带 **VM 模块 UI 手动配置清单**。

## 触发范围

本 skill 在满足以下**任一**条件时自动激活：

### 条件 A：用户语义触发

用户在对话中提及脚本编程相关关键词：

- 脚本，输入输出
- `UserScript` / `IProcessMethods` / `Script.Methods`
- 变量直接赋值 / 数组 / 图像 / ROI 等数据类型
- 模块参数、全局变量、模块间读写、调试输出
- 生成脚本并希望了解 VM 模块界面要怎么配
- VS 断点调试脚本

### 条件 B：工作目录路径自动检测（0 关键词也能触发）

**即使用户完全未提及脚本编程关键字**，只要当前工作目录路径满足以下任一模式，本 skill 即视为触发：

1. 路径中包含 `VisionMaster`（不区分大小写）—— 例如 `C:\Program Files\VisionMaster4.3.0\...`
2. 路径中包含 `UserScript`（不区分大小写）—— 例如 `...\UserScript\UserScript_3\`

检测到满足条件 B 时，**必须**按以下步骤行动：

1. **检测**：读取当前工作目录（`pwd`），判断路径是否命中上述模式
2. **告知**：向用户说明：
   > "检测到当前工作目录位于 VM 脚本工程路径下：
   > `{{实际路径}}`
   > 您是否需要编写或修改 VisionMaster C# 脚本？"
3. **等待确认**：
   - 用户回复"是/需要/对"等 → 进入 §1 开始正常脚本开发工作流
   - 用户回复"不/否/不用"等 → 退出本 skill，按普通编程请求处理
   - 用户沉默或未直接回答 → **再次追问一次**，避免误判

> 条件 B 的目的是：当用户打开 VM 脚本工程文件夹但发了"帮我看看这个"、"这个怎么改"等不含脚本关键字的消息时，skill 仍能被正确触发。

## 明确不支持

遇到以下请求时，**立刻直接告知用户不支持**：

- **3D VisionMaster**（包括 3D 点云、立体视觉、`PointCloudData`、`VM3DScriptBase` 等所有 3D 相关脚本）。本 skill 仅覆盖 **2D** VM 脚本
- 控制器 IO 发送
- 界面层操作 / UI 自动化
- 非托管资源的外部封装方案
- 通讯协议解析
- Python 脚本模块
- **反编译或读取 VM 安装目录下的 DLL 文件来获取 API 信息**（`.dll` / `.exe` 反编译严格禁止）

## 必须遵守

- **【最高优先级】绝对禁止反编译 VM 安装目录下的任何 DLL/EXE 文件**。所有 C# 接口签名、数据类型用法、转换方法，必须且仅能从本 skill 的 `references/` 和 `examples/` 中获取
- **允许（且要求）读取 VM 安装目录下的 AlgorithmTab XML 文件**，用于查询具体算法模块的可设置参数名（key）和参数值范围（MinValue / MaxValue）。详见 §6
- **模块英文名和所属工具箱必须从 `references/VisionMaster模块映射表.md` 查询**，不得凭记忆或猜测。详见 [references/module-param-workflow.md](./references/module-param-workflow.md) 步骤 0
- 基于 .NET Framework 4.6.1，不得使用 4.6.1 之后引入的 API
- **必须使用 C# 5.0 语法**（VM 内置 Roslyn 编译器锁定在 C# 5）。**禁止**使用 C# 6+ 特性：字符串插值 `$"..."`、空条件运算符 `?.`、`nameof()`、表达式体成员 `=>`（含方法、属性、Lambda 之外的所有形式）、自动属性初始化 `{ get; } = ...`、`using static`、异常筛选器 `catch ... when (...)`、`await` 在 `catch/finally` 中；**禁止** C# 7+ 特性：`out var`、值元组 `(a, b)`、模式匹配 `is X x` / `switch` 模式、局部函数、`throw` 表达式、二进制字面量 `0b...`、数字分隔符 `_`；**禁止** C# 8+ 特性：`??=`、可空引用类型、`switch` 表达式、范围/索引 `..` / `^`。允许：`??`（C# 2）、`var`（C# 3）、LINQ、Lambda、`async/await`（C# 5）、可选参数、命名实参
- 首先尝试读取工作目录中的 `UserProperty.cs` 和 `UserScript.cs`；若无法访问，要求用户提供文件内容或工程路径
- `UserProperty.cs` 为只读定义，**严禁修改**
- 代码组织：`Process()` 放在上方，辅助方法放在下方；每个方法添加 XML 注释；删除 VM 默认模板里的结构化注释
- **变量读写一律使用直接赋值**（如 `out0 = in0`）；唯一例外：需要按字符串动态访问变量名时可用遗留 Get/Set
- `Process()` 必须用 `try/catch` 包裹整个业务逻辑，捕获到异常时把异常信息写入 `errorStatus` 字符串字段（见"异常处理与 errorStatus"章节）
- **默认不添加** `ShowMessageBox` / `ConsoleWrite` / 任何日志/打印调用。这些是**对用户的副作用**，必须在写代码前显式询问用户是否需要；用户未要求即不加（见"调试输出策略"章节）
- 仅在需要显式释放资源时才编写 `Dispose()`；`Mat` 用完 Dispose、大对象用字段缓存避免每帧 new
- 需要的信息不在 `references/` 或 `examples/` 中时，中断生成并提示用户
- **写代码前必须盘点命名空间**：VM 默认 csproj 只引用 `mscorlib` / `System` / `System.Core` / `System.Windows.Forms` / `Script.Methods.dll`。`System.Drawing` / `System.Numerics` 等其它 BCL 程序集均**不在默认引用**中。纯几何/数学计算优先用自定义 struct 或基础数组（零依赖偏好），避免引入 `System.Drawing.PointF`（详见 §5）
- 每次脚本生成完毕后，**必须进行静态审查**（逐项对照自校验清单）；若当前工具支持 shell 执行且用户已导出 VS 工程，可额外用 `./assets/find_msbuild.ps1` 定位 MSBuild 后执行真实编译，但此步骤为**可选**
- **每次生成脚本后必须附 UI 手动配置清单**（填充 [output_report_template.md](./output_report_template.md) 内联返回）

## 生成前的强制确认事项

按 §1–§6 顺序逐项确认，任一项未满足必须中断并向用户提问。

### §1. VisionMaster 版本与产品形态（先自动检测，再请客户确认）

**优先自动检测**，避免不必要的提问：

1. 执行 shell 命令读取系统 `PATH` 环境变量（参考命令：`echo "$PATH" | tr ':;' '\n' | grep -i 'VisionMaster'`，Windows 下也可 `cmd /c echo %PATH%`）
2. 在结果中查找 `VisionMaster4.3.0` / `VisionMaster4.4.0` / 其他 `VisionMasterX.Y.0` 子串
3. 根据匹配数量分支：

| 匹配结果                  | 处理                                                                                                              |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| 仅 1 个 VM 版本           | 直接告知用户："已从 PATH 检测到 **VM X.Y**（路径：`...`），按此版本生成代码。如需切换请告知。" 等用户回复（默认视为确认）   |
| 0 个 VM 版本              | 检测不到。**询问用户**：本机 VM 版本号是？2D 还是 3D？                                                            |
| ≥ 2 个 VM 版本（如同装 4.3 与 4.4） | **告知用户检测到的多个版本并询问**：当前要为哪个版本生成代码？                                                    |

**产品形态**：默认 2D。若 PATH 中检测到任何 3D 相关 VM 子目录或用户明确说 3D，**立即中止**并按"明确不支持"提示。

版本影响 `RoiboxData` 和 `ImageData` 的高度字段拼写：**VM ≤4.3 用 `Heigth`，VM ≥4.4 用 `Height`**。注意 `RectData.Height` / `Mat.Height` / `Bitmap.Height` 在所有版本中均为 `Height`，不受版本影响。

### §2. 多变量场景下，必须由用户明确指定目标参数名

`UserProperty.cs` 中可能定义了多个同类型输入/输出变量。当用户的需求涉及"两个矩形框"、"某个图像"等不指名描述，且 `UserProperty.cs` 中存在 ≥2 个同类型候选时，**必须先列出所有候选并要求用户指认**。

举例：用户说"计算两个输入矩形框的 IOU"，而 `UserProperty.cs` 中存在 `inbox0/inbox1/inbox2` 三个 `RoiboxData[]` 输入。回复必须形如：

> "`UserProperty.cs` 中检测到 3 个 `RoiboxData[]` 输入：`inbox0`、`inbox1`、`inbox2`。请明确：
> - 参与 IOU 计算的两个输入变量名分别是？
> - IOU 结果写入哪个输出变量（候选：`outIou`、`outScore`）？"

规则：

- 输入或输出候选 ≥ 2 个同类型且用户未指名 → **必须提问**
- 候选唯一或用户已显式给出变量名 → 直接进入下一步

### §3. 需求合理性与可行性分析（必须做，不要直接埋头写代码）

把用户需求形式化前，**先做一轮逻辑可行性分析**：

1. **类型匹配**：用户描述的输入/输出，类型/维度是否对得上？
2. **算子语义是否清晰**：用户用的动词（"求 IOU"、"匹配"、"对齐"等）在所给数据上是否有**唯一定义**？
3. **多对一 / 一对多场景**：当一侧是单值、另一侧是数组时，需要明确聚合方式

**触发示例**：用户说"计算输入 ROI 和输入 ROI 数组的 IOU"。`UserProperty.cs` 显示 `ROI: RoiboxData[]`（长度 ≥1）与 `ROIs: RoiboxData[]`（长度 N）。歧义点：

- "ROI" 是数组里第 0 个？还是认为该数组只放了一个？
- 与数组的 N 个分别求 IOU，结果是 `float[]`？还是只输出 **最大 IOU**？还是输出 **最匹配的索引**？
- 如果 `ROIs` 为空，行为是什么（返回 0、NaN、还是 `errorStatus` 报错）？

**处理规则**：

- 任何一个歧义点存在时，**必须列出所有歧义点并给出建议方案**，让用户选一种或自定义
- 当用户表达明确无歧义时，**重述一次**："理解为 X、对每个 Y 算 Z，最终输出 W；如有出入请纠正"
- 用户回复后，进入 §4 出大纲

### §4. 实施大纲（写代码前必须给）

在所有前置确认完成后，**先给一份实施大纲（不写代码）**：

```
【实施大纲】
- VM 版本：X.Y
- 输入变量映射：xx → C# 类型
- 输出变量映射：xx → C# 类型
- 异常字段：errorStatus（string，类字段）
- 算法步骤：
  1. ...
  2. ...
- 第三方库依赖：（如有）
- 是否加调试输出（ShowMessageBox / ConsoleWrite）：默认无；如需请告知
```

等待用户**明确确认**（或修订）后再进入"代码生成"阶段。不要在用户没确认前就贴完整代码。

### §5. 程序集可用性（含 .NET BCL 与第三方库）

VM 脚本模块默认 csproj 通常只引用：`mscorlib` / `System` / `System.Core` / `System.Windows.Forms` / `Script.Methods.dll`。**.NET BCL 中其它程序集（哪怕是基础库）都需要显式添加 `<Reference>`**，否则编译报"命名空间中不存在类型或命名空间名称"。

#### 常见非默认引用的 BCL 程序集

| 程序集                  | 触发它的典型 API                                          | 处理                                |
| ----------------------- | --------------------------------------------------------- | ----------------------------------- |
| `System.Drawing`        | `PointF` / `Bitmap` / `Graphics` / `Color`                | 仅处理 Bitmap/Graphics 时引入       |
| `System.Drawing.Imaging`| `PixelFormat` / `ImageFormat`                             | 与 `System.Drawing` 一起加          |
| `System.Numerics`       | `Vector2` / `Vector3` / `Matrix4x4`                       | 需要 SIMD 几何运算时                |
| `System.Xml.Linq`       | `XDocument` / `XElement`                                  | XML 解析                            |
| `System.Net.Http`       | `HttpClient`                                              | HTTP 请求                           |
| `System.IO.Compression` | `ZipArchive`                                              | ZIP 处理                            |

> **`System.Windows.Forms` 不会传递引用 `System.Drawing`**。即便默认引用里有 WinForms，使用 `System.Drawing.PointF` 等类型也必须单独加 `System.Drawing.dll`。

#### 零依赖偏好（重要）

**纯几何/数学计算优先用零依赖类型**，不要图方便引入 `System.Drawing`：

- 二维点用自定义 `internal struct Pt { public float X; public float Y; }` 或 `float[]` 元组
- 浮点向量计算用 `double` / `float` 数组
- 只有在确实要画图、做 `Bitmap` ↔ `ImageData` 转换时才引入 `System.Drawing`

> 示例：计算 IOU / 多边形交集 / 矩形旋转顶点时，**绝不**用 `System.Drawing.PointF`——用本地 `Pt` 结构。

#### 处理流程

1. 写代码前先盘点要用的命名空间，按上表与"默认引用列表"比对
2. 如属于非默认引用：
   - 优先**改实现**消除该依赖（零依赖偏好）
   - 确实无法消除 → 读 `*.csproj` 看 `<Reference Include="...">` 是否已存在
   - 已存在：在 UI 清单注明"程序集已存在，无需重新添加"
   - 不存在：
     - 不要臆造 `<HintPath>`、不要自动改写 csproj
     - **立即中断代码生成**，明确告知用户：
       > "脚本依赖 `XXX.dll`，但当前工程的 csproj 中尚未引用它。请先完成以下步骤，再重新请我生成代码：
       > 1. 在 VM 中双击脚本模块，打开配置窗口
       > 2. 控制栏 → **编辑程序集** → 右上角 **添加** → 找到 `XXX.dll` → 打开
       > 3. 点击 **确定** 保存，VM 会自动更新 csproj
       > 4. 控制栏 → **导出工程**，覆盖原工程目录
       > 完成后告诉我，我将继续生成 `UserScript.cs`。"
     - 在后置 UI 清单的程序集表中同样列出该 DLL，标注"已在生成前告知用户添加"
3. 第三方库（OpenCvSharp、Newtonsoft.Json 等）同流程
4. 找不到合适库或不确定怎么集成 → 中断生成

> VM 工作流约束：脚本模块的 `*.csproj` 是 VM 根据 UI 配置自动生成的；本 skill 不应直接修改 csproj。

## §6. 模块运行参数设置/获取工作流

当用户要在脚本中对 VM 模块设置或获取运行参数时，必须严格执行以下工作流，不得跳过或猜测参数名。完整操作步骤（含降级处理）见 [references/module-param-workflow.md](./references/module-param-workflow.md)。

### 核心接口

```csharp
// 设置模块参数（参数值统一以字符串传入，VM 内部负责类型转换）
CurrentProcess.GetModule("模块名称").SetValue("参数Key", "参数值字符串");

// 获取模块结果
object val = CurrentProcess.GetModule("模块名称").GetValue("结果参数名");

// 获取模块运行参数（按模块ID，旧接口）
int ret = GetModuleParam(nModuleID, "参数Key", ref paramValue);
```

> **Tip**：获取模块结果和设置模块运行参数都只能作用于**当前流程**。如果模块在 Group 中，需要加上 Group 名：`GetModule("Group1.模块名称")`。

### 查询规则（必须执行，不得猜测）

**绝对禁止**凭记忆、猜测、通用命名推断参数名（Key）。必须先从 `references/VisionMaster模块映射表.md` 查询模块所属的**工具箱英文名**和**模块英文名**（显式告知用户确认），再用确认后的英文名构造路径 `{VM根目录}\Applications\Module(sp)\x64\{工具箱英文名}\{模块英文名}\{模块英文名}AlgorithmTab.xml` 读取 XML，从 `<Name>` 节点提取参数 Key、从 `<MinValue>` / `<MaxValue>` 校验范围。

### 强制确认事项

1. **必须要求用户提供模块名称**（流程中显示的名称，如"单点抓取1"）——不得用默认名"模块1"等占位
2. **必须要求用户提供待设置的参数描述**（如"示教点坐标 X=100, Y=100, Z=1"）
3. 映射表查询结果（工具箱英文名 + 模块英文名）**必须告知用户确认**
4. 从 XML 提取参数 Key 和范围后，**必须向用户复述确认**
5. 参数值超出 MinValue–MaxValue 范围时，**必须明确告知用户并阻断生成**
6. 若用户要设置多个参数，每个参数都执行上述查询流程

> **核心原则**：任何一步失败（映射表查不到、工具箱/模块目录不存在、XML 找不到、参数节点不存在），都要**显式告知用户缺失了什么**并明确说明需要提供的信息（工具箱英文名 / 模块英文名 / 参数英文名 / 参数值），不得自行推断、跳过或用占位符代替。

---

## 生命周期

| 方法        | 修饰                  | 触发时机              | 用途                                     |
| ----------- | --------------------- | --------------------- | ---------------------------------------- |
| `Init()`    | `public void`         | 加载方案 / 预编译时   | 初始化变量、创建句柄、加载资源           |
| `Process()` | `public bool`         | 每次流程执行          | 业务逻辑、数据处理；返回 `true` 表示成功 |
| `Dispose()` | `public virtual void` | 关闭方案 / 重新编译时 | 释放资源、关闭句柄                       |

## 变量输入与输出（直接赋值）

VM 脚本的输入输出变量在 `UserProperty.cs` 中定义，在 `UserScript.cs` 中**直接通过变量名访问**。

### 类型映射规则

| 规则                  | VM 类型示例                                                                 | 脚本 C# 类型                                 | 示例                                   |
| --------------------- | --------------------------------------------------------------------------- | -------------------------------------------- | -------------------------------------- |
| 标量 → 基础类型       | INT, FLOAT, STRING, DOUBLE, BYTE                                            | `int`, `float`, `string`, `double`, `byte[]` | `int val = in0; out0 = val;`           |
| IMAGE → ImageData     | IMAGE                                                                       | `ImageData`（特例，非数组）                  | `ImageData img = imgIn; imgOut = img;` |
| 复合 → `<Type>Data[]` | POINT, ROIBOX, CIRCLE, RECT, LINE, ELLIPSE, ANNULUS, POLYGON, CONTOUR_POINT | `PointData[]`, `RoiboxData[]` …              | `PointData[] pts = in0; out0 = pts;`   |
| 数组 → 直接用         | INT 数组, FLOAT 数组, STRING 数组, DOUBLE 数组                              | `int[]`, `float[]`, `string[]`, `double[]`   | `int[] arr = in0; out0 = arr;`         |

#### 矩形框 / ROI 的默认类型是 RoiboxData

口语中说"矩形框/ROI/识别框"时，**默认对应 ROIBOX → `RoiboxData[]`**（带角度）。`RECT → RectData[]` 是不带角度的轴对齐矩形。判断顺序：先看 `UserProperty.cs` 实际定义；若用户未明确，按 `RoiboxData[]`。

#### ImageData 赋值语义

`ImageData` 的直接赋值（`imgOut = imgIn`）为**引用传递**。如需独立图像副本，必须通过 `ImageDataToMat` 获取 Mat，处理后再用 `MatToImageData` 生成新图像。

> 完整类型映射表见 [interface-quickref.md](./examples/interface-quickref.md#1-变量类型映射)

> 常见运行时错误排查见 [examples/interface-quickref.md](./examples/interface-quickref.md#10-常见运行时错误排查)。

## 异常处理与 errorStatus

### 强制规则

1. **`Process()` 必须用 `try/catch` 包裹整个业务逻辑**，避免输入数据为 null/NaN/空数组时直接抛到 VM 引擎
2. 类内声明一个 `string errorStatus`（默认名）字段，用于存储最近一次异常信息
3. **变量名冲突处理**：若 `UserProperty.cs` 中已有名为 `errorStatus` 的输入/输出变量，改用 `_errorStatus`、`__errorStatus`、`scriptErrorStatus` 等不冲突的名字，并在 UI 配置清单中告知用户
4. 异常发生时：在 `catch` 中把 `ex.Message` 或 `ex.ToString()` 写入 `errorStatus`，`Process` 返回 `false`
5. **`errorStatus` 默认只在类内可见，不映射到 VM 输出**。如果用户希望在 VM 流程里看到异常信息：在 UI 配置清单中提示用户新增一个 STRING 输出变量（如 `errorOut`），并把 `errorOut = errorStatus;` 加到 `catch` 末尾

### 标准模板（含 try/catch）

```csharp
using System;
using System.Text;
using System.Windows.Forms;
using Script.Methods;

public partial class UserScript : ScriptMethods, IProcessMethods
{
    int processCount;
    string errorStatus = string.Empty;

    /// <summary>
    /// 初始化字段
    /// </summary>
    public void Init()
    {
        processCount = 0;
        errorStatus = string.Empty;
    }

    /// <summary>
    /// 流程执行入口
    /// </summary>
    public bool Process()
    {
        try
        {
            errorStatus = string.Empty;

            // 业务逻辑（变量读写、计算、输出赋值）

            processCount++;
            return true;
        }
        catch (Exception ex)
        {
            errorStatus = "Process 异常：" + ex.Message;
            return false;
        }
    }

    // 仅在需要释放非托管资源时添加 Dispose()
}
```

### 输入数据健壮性

`try/catch` 之外，业务代码应主动判断常见空/NaN 情况而不是依赖异常：

- `if (inArr == null || inArr.Length == 0) { errorStatus = "输入 inArr 为空"; return false; }`
- 浮点：`if (float.IsNaN(x) || float.IsInfinity(x)) { errorStatus = "输入 x 非法 (NaN/Inf)"; return false; }`
- 图像：`if (imgIn.Buffer == null || imgIn.Width <= 0 || imgIn.Height <= 0) { errorStatus = "输入图像无效"; return false; }`

判断失败时写 `errorStatus` 并 `return false`；**不要弹窗、不要打印**（除非用户在 §4 大纲确认时明确要求）。

## 调试输出策略（默认 OFF）

`ShowMessageBox` / `ConsoleWrite` / `GlobalVariableModule.SetValue("调试信息", ...)` 都属于**对用户和流程的副作用**：

- `ShowMessageBox` 会**暂停整个流程**，生产环境绝对不能用
- `ConsoleWrite` 把信息推到 DebugView，需要外部工具才能看
- 即使是写全局变量，也会污染流程数据

**规则**：

1. **默认不生成**任何 `ShowMessageBox` / `ConsoleWrite` / 日志调用
2. 在 §4 实施大纲中**显式询问**：本次脚本是否需要在异常分支添加调试输出？输出形式（弹窗/DebugView/全局变量字符串）？
3. 用户答"不需要" → 仅写 `errorStatus`，`Process` 返回 `false`
4. 用户答"需要" → 按用户指定的形式添加；若用户指定弹窗，注明"仅供调试，发布前请删除"

## 标准模板（最小版本，无调试输出）

见上方"异常处理与 errorStatus § 标准模板"。

> 真实导出工程 `UserScript.cs` 默认就包含 `using System.Text;` 与 `using System.Windows.Forms;`。`UserProperty.cs` 还会额外 `using Conceal;`，本侧脚本无需手写。

## 工作原理（必读）

`UserProperty.cs` 与 `UserScript.cs` 是同一个 `partial class UserScript` 的两半：

- `UserProperty.cs`（VM 自动生成，**只读**）：把每个 UI 上添加的输入/输出变量声明为 C# property。`get` 内部对标量调用 `GetIntValue` 等；对数组调用 `(InternalObject as InternalMethods).GetXxxArrayValue(...)` 2 参版本（见 [references/InternalMethods.cs](./references/InternalMethods.cs)）。每次调用都会把返回码写入基类字段 `nErrorCode`
- `UserScript.cs`（用户编辑）：在 `Process()` 里像普通字段一样读写 `in0` / `out0`

直接赋值就是 partial property 对遗留接口的封装。**优先用直接赋值**，仅在按字符串动态访问变量名时才下沉到遗留接口。

## 输出方式（生成代码后的交付方式）

生成代码后**默认直接覆盖** `UserScript.cs`，避免用户手动粘贴出错。

1. **判定目标文件路径**：
   - 已读取过同目录下的 `UserScript.cs` → 目标即该文件
   - 未读取过 → 询问用户提供绝对路径
2. **判断目标是否含用户改动**：
   - 默认模板（仅 `processCount = 0` + 空 `Process()`）→ 直接写入覆盖
   - 已含用户业务代码 → 先备份为 `UserScript.cs.bak`，再覆盖；告知备份位置
3. **覆盖完成后**告知：替换的绝对路径、是否生成 `.bak`、提醒在 VM 中重新 **预编译** 验证
4. **改回粘贴模式**：用户明确说"我自己粘贴"、没有可访问本地路径、首次合作未授权写盘时

> `UserProperty.cs` 与 `*.csproj` 永远不动。

## VS 断点调试

完整步骤见 [references/csharp_debug.md](./references/csharp_debug.md)。

## 文件索引

| 文件                                                                         | 用途                                                                         |
| ---------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| [references/Script.Interface.cs](./references/Script.Interface.cs)           | `ScriptMethods` 遗留接口签名（标量 Get/Set）                                 |
| [references/Script.DataStruct.cs](./references/Script.DataStruct.cs)         | 数据结构字段定义                                                             |
| [references/Script.ExMethods.cs](./references/Script.ExMethods.cs)           | Mat / Bitmap ↔ ImageData 转换方法                                            |
| [references/InternalMethods.cs](./references/InternalMethods.cs)             | `Conceal.InternalMethods`（数组 Get/Set 2 参实现）                           |
| [references/VisionMaster模块映射表.md](./references/VisionMaster模块映射表.md) | VM 工具箱中文名 ↔ 英文名 ↔ 模块英文名映射表（AlgorithmTab.xml 路径查询前置依赖） |
| [references/csharp_api.md](./references/csharp_api.md)                       | 官方开放接口完整列表                                                         |
| [references/module-param-workflow.md](./references/module-param-workflow.md) | §6 模块参数设置/获取完整工作流（步骤 0–4 + 降级处理汇总）                    |
| [references/csharp_debug.md](./references/csharp_debug.md)                   | VS 附加进程断点调试操作步骤                                                  |
| [examples/interface-quickref.md](./examples/interface-quickref.md)           | 变量类型映射 + 接口速查                                                      |
| [examples/code-patterns.md](./examples/code-patterns.md)                     | 代码模式库                                                                   |
| [examples/01-basic-template.cs](./examples/01-basic-template.cs)             | 基础脚本模板                                                                 |
| [examples/02-canny-edge-detection.cs](./examples/02-canny-edge-detection.cs) | OpenCvSharp 图像处理示例                                                     |
| [examples/03-roi.cs](./examples/03-roi.cs)                                   | ROI 处理示例                                                                 |
| [examples/04-trans-CAD-file.cs](./examples/04-trans-CAD-file.cs)             | CAD 文件转换示例                                                             |
| [examples/05-halcon-image-conversion.cs](./examples/05-halcon-image-conversion.cs) | Halcon ↔ ImageData 互转示例                                                  |
| [output_report_template.md](./output_report_template.md)                     | 生成后给用户的 UI 手动配置清单模板                                           |
| [assets/find_msbuild.ps1](./assets/find_msbuild.ps1)                         | 定位本机 MSBuild.exe 路径（可选，供支持 shell 的工具执行真实编译时使用）      |

---

## 代码生成工作流

### Step 0：工作目录自动检测（触发条件 B）

**每次 skill 激活时先执行此步骤**（无论是由条件 A 还是条件 B 触发）。

1. 使用 `pwd`（或等效方式）获取当前工作目录路径
2. 判断路径是否命中条件 B 的模式（包含 `VisionMaster` 或 `UserScript`）
3. 若命中：
   - 若 skill 由条件 B 触发（用户未提脚本关键字）→ 按"触发范围 §条件 B"的步骤告知用户并请求确认
   - 若 skill 已由条件 A 触发（用户明确提了脚本相关关键字）→ 直接向下执行，无需再次确认。但告知用户已检测到 VM 脚本工程路径，后续将针对此路径操作
4. 若未命中 → 继续向下执行（用户可能是在其他目录讨论 VM 脚本）

### Step 1：自动检测 VM 版本并请用户确认（§1）

执行 shell 命令读取 PATH，按 §1 的规则定位 VM 版本号；告知用户检测结果，等用户确认或修正。

### Step 2：读取项目文件

`UserProperty.cs`、`UserScript.cs`、`*.csproj`。无法访问则要求用户提供。

### Step 2b：模块参数查询（仅当用户需要设置/获取模块参数时执行）

若用户需求包含对某算法模块的参数设置或获取：

1. 要求用户确认：模块名称（流程中的显示名）、待操作的参数描述及目标值
2. 按 [module-param-workflow.md](./references/module-param-workflow.md) 步骤 0 读取 `references/VisionMaster模块映射表.md`，查询模块对应的**工具箱英文名**和**模块英文名**，**显式告知用户确认**
3. 按 [module-param-workflow.md](./references/module-param-workflow.md) 步骤 1 用确认后的工具箱英文名和模块英文名构造路径，定位并读取 `AlgorithmTab.xml`
4. 从 XML 中提取参数 Key 和范围（MinValue / MaxValue）
5. 执行 [module-param-workflow.md](./references/module-param-workflow.md) 步骤 3 的范围校验；超限则告知并等待用户修正
6. 向用户复述确认后进入 Step 3

### Step 3：消歧 —— 变量名指认（§2）

按 §2 规则检查所有语义角色，候选 ≥2 个时列出来让用户选。

### Step 4：需求合理性分析（§3）

按 §3 列出歧义点并要用户选定方案，或重述并请确认。

### Step 5：实施大纲（§4）

按 §4 模板给出大纲（含异常字段名、是否加调试输出、第三方库需求），**等待用户确认后再写代码**。

### Step 6：组装代码

1. **using 声明**：根据需求添加
2. **类声明**：`UserScript : ScriptMethods, IProcessMethods`
3. **字段声明**：`processCount` + `errorStatus`（必有）+ 其他业务字段
4. **Init()**：初始化字段
5. **Process()**：`try { errorStatus = string.Empty; 业务逻辑; return true; } catch (Exception ex) { errorStatus = "..." + ex.Message; return false; }`
6. **辅助方法**：放在 Process() 下方，每个加 XML 注释
7. **Dispose()**：仅在需要释放非托管资源时编写
8. **绝不擅自加 `ShowMessageBox` / `ConsoleWrite`**，除非用户在 Step 5 大纲确认时说要

代码组装完成后，立即执行静态审查（代替真实编译的核心防线）：

- **C# 5.0 语法**：逐行扫描，禁用特性见"必须遵守"条款
- **变量名**：与 `UserProperty.cs` 逐一比对，拼写完全一致
- **RoiboxData / ImageData 高度字段**：按确认的 VM 版本选 `Heigth`（≤4.3）或 `Height`（≥4.4）
- **命名空间**：列出所有 `using`，对照默认引用列表，非默认的在 UI 清单中标出
- **errorStatus 命名冲突**：确认与 `UserProperty.cs` 无重名
- **Mat Dispose**：所有 `new Mat()` 均有对应 `Dispose()` 或 `using`

### Step 7：交付（覆盖 UserScript.cs）

按"输出方式"章节执行，直接写入覆盖目标文件，必要时先备份为 `.bak`。

### Step 8：填写 UI 配置清单

读取 [output_report_template.md](./output_report_template.md)，按生成脚本实际用到的输入/输出变量、第三方程序集、VM 版本假设、`errorStatus` 是否需要输出等填充，内联返回。

### Step 9：自校验

- [ ] 已执行 Step 0：获取工作目录路径，判断是否命中 VisionMaster/UserScript 模式；如命中且为条件 B 触发，已向用户确认
- [ ] **未反编译/读取 VM 安装目录任何 DLL/EXE 二进制文件**（仅允许读取 AlgorithmTab.xml）
- [ ] **未修改 `UserProperty.cs`（只读）与 `*.csproj`（由 VM 根据 UI 配置自动生成）**
- [ ] 已自动检测 VM 版本并经用户确认；产品形态为 2D
- [ ] 用户已在 Step 5 大纲处确认方案
- [ ] 变量名与 `UserProperty.cs` 中定义一致；多候选场景下用户已指认
- [ ] 矩形框/ROI 默认 `RoiboxData[]`；用 `RectData[]` 已对照 `UserProperty.cs`
- [ ] `RoiboxData` / `ImageData` 高度字段拼写与 VM 版本匹配
- [ ] 所有变量读写使用直接赋值
- [ ] `Process()` 已用 try/catch 包裹，`errorStatus` 字段已声明，`catch` 内对其赋值
- [ ] `errorStatus` 命名未与 `UserProperty.cs` 中的变量冲突；如有冲突已改名并在清单中告知
- [ ] **默认无任何 `ShowMessageBox`/`ConsoleWrite`/日志输出**；除非用户在大纲处明确要求
- [ ] `Mat` 对象已 Dispose 或缓存
- [ ] 第三方库需求已在清单中说明，未私自改 csproj
- [ ] **已盘点代码用到的所有命名空间，非默认引用（如 `System.Drawing`、`System.Numerics`）已在 UI 清单中列出或已改用零依赖实现**；纯几何代码未误用 `System.Drawing.PointF`
- [ ] 代码基于 .NET Framework 4.6.1
- [ ] **代码仅使用 C# 5.0 语法**：无 `$"..."`、`?.`、`nameof`、表达式体成员、`out var`、元组、模式匹配、`??=` 等 C# 6+ 特性
- [ ] `Process()` 位于类上部，辅助方法位于下部
- [ ] 每个方法都有 XML 注释；默认模板的结构化注释已删除
- [ ] 已写入覆盖 `UserScript.cs`（或按用户要求改回粘贴），并报告路径与备份
- [ ] UI 配置清单已附在回复末尾
- [ ] **静态审查已完成**：C# 5.0 语法、变量名拼写、RoiboxData 高度字段版本、命名空间、errorStatus 命名冲突、Mat Dispose 逐项通过
- [ ] （可选）若工具支持 shell 且用户已导出 VS 工程：已用 `find_msbuild.ps1` 定位 MSBuild 并执行真实编译，结果告知用户
- [ ] **（模块参数场景）**：已从 `references/VisionMaster模块映射表.md` 查询模块所属工具箱和英文名，并显式告知用户确认；已用确认后的工具箱英文名和模块英文名定位 AlgorithmTab.xml；模块参数 Key 已从 XML 中提取，未凭猜测；用户提供的参数值未超出 MinValue–MaxValue 范围；已向用户复述确认

## 注意事项

- 非托管资源（如 DLL 句柄）必须在 `Dispose()` 中释放
- 若本文与 `references/` 文件冲突，以 `references/` 文件为准
