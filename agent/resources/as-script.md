# AS（AngelScript）脚本能力

用户要求写 `.as` 脚本、AngelScript、AS 脚本模块、`BaseAlgorithm`/`MyAlgorithm`/`helper.Get*`/`helper.Set*`，或要求把 VM C# 脚本转成 AS 脚本时，使用本节能力。

## 与编译器的边界

- **AS 不是 Requirement 载体。** `carrier` 只有 `global-csharp`、`csharp-module`、`python-module`，没有 `as-module`。
- 不要为 AS 需求调用 `vm_update_requirement` 或 `vm_compile_solution`，确定性 Core 无法把 `.as` 编译进 SOL。
- **交付物就是 `.as` 源码文本本身**，直接在回复里给出，并附输入/输出配置清单。不生成报告或其它文件。
- 文件编码必须 UTF-8（含中文注释）。
- 告知用户后续步骤：在 AS 脚本模块中导入 → 按清单配置输入/输出变量 → **编译**（改名称或类型后必须重新编译）→ 执行验证。
- `vm_inspect_solution` 能识别 `AsShellModule` 并返回其已有脚本与端口；改已有 AS 模块时同样适用「Patch 必须继承基底已有内容」。

## 固定结构（必须一字不差地遵守）

```angelscript
class MyAlgorithm:BaseAlgorithm
{
    int process()
    {
        int Ret = 0;
        try
        {
            // 业务逻辑
        }
        catch
        {
            string info = getExceptionInfo();
            helper.Print("getExceptionInfo: " + info);
            Ret = -1;
        }
        return Ret;
    }
    // 辅助函数放这里
}
```

- 类名固定 `MyAlgorithm`，必须继承 `BaseAlgorithm`；`int process()` 必须存在。
- 返回 `0` 成功，`-1` 异常；其它负值为用户自定义错误码，同样视为失败。
- `getExceptionInfo()` 只能在 `catch` 中调用。
- 辅助函数放 `process()` 下方，数组参数用 `@` 引用（`array<float>@ data`）。

## helper 接口

输入（带返回值版本返回 0 表示成功，参数按引用传递、无 `@` 前缀）：

```
int helper.GetInt/GetFloat/GetString(string name, T value)
int helper.GetIntArray/GetFloatArray/GetStringArray(string name, array<T> value)
int helper.GetRoiBox(string name, ROIBOX value)
int helper.GetRoiBoxArray(string name, array<ROIBOX> value)
```

基本类型和单值复合类型也可直接用属性名读：`int x = in0;`、`array<float> a = in3;`、`ROIBOX r = in6;`

输出：

```
void helper.SetInt/SetFloat/SetString(string name, T value)
void helper.SetIntArray/SetFloatArray/SetStringArray(string name, array<T> value)
void helper.SetRoiBox(string name, ROIBOX value)
void helper.SetRoiBoxArray/SetPointArray/SetCircleArray/SetRectArray/SetLineArray/
     SetEllipseArray/SetRoiAnnulusArray/SetRoiPolygonArray/SetDefectArray/SetFixtureArray
```

- **数组和复合数组的输出必须走 `helper.SetXxxArray()`**，不能用输出参数名直接赋值。这是 AS 与 C# 脚本最大的差异。
- 复合/几何**单值**（如 `ROIBOX`）可用属性名直接赋值（`out0 = roi;`）或 `helper.SetRoiBox`。
- helper **没有** `SetBool` / `SetDouble`；`bool` 与 `double` 输出只能用属性名，需要字符串输出时转换后 `SetString`。
- `helper.Print` 官方重载仅 `int`/`float`/`string`、`array<int>`/`array<float>`/`array<string>`、10 种几何单值、`array<ROIBOX>`、`array<RoiPolygon>`。**无 `bool`/`double` 重载**。默认不生成调试打印，需要时以注释形式给出。

## 类型系统

| 类别 | 类型 |
| --- | --- |
| 标量 | `int` `float` `double` `string` `bool`（另有 `int8/16/32/64`、`uint/uint8/16/32/64`） |
| 数组 | `array<int>` `array<float>` `array<string>` |
| 几何 | `ROIBOX` `Point` `Line` `Circle` `Rect` `Ellipse` `RoiAnnulus` `RoiPolygon` `Defect` `Fixture` |
| 几何数组 | `array<ROIBOX>` `array<Point>` `array<Circle>` 等 |
| 对象 | `datetime` `dictionary` `file` `filesystem` |

- `ROIBOX` 字段：`CenterX` `CenterY` `Width` `Height` `Angle`（均 float），`Init(cx, cy, w, h, angle)`。
- 几何类型均支持 `Init(...)` 与 `ToString()`。
- 变量名只能以下划线或字母开头，**不能含中文**（输入输出变量名同理）。
- 数组声明：`array<int> a;` / `array<int> a(5);` / `array<int> a(3, 42);` / `array<float> a = {1.1f, 2.2f};`

## 硬性写法规则

- 数值转字符串**只能**用 `formatFloat` / `formatInt` / `formatUInt`。**禁止 `string(数值)`** —— AS 的 `string` 没有数值构造，会报 `No matching signatures`。
  例：`"avg=" + formatFloat(avg, "f", 0, 3)`、`"count=" + formatInt(count, "d", 0)`。
- 空判断用 `is null` / `!is null`，**仅适用于引用类型**（`array<T>`、`@` 句柄）。`string` 判空用 `isEmpty()` 或 `length() == 0`；值类型用 `is null` 会编译错误。数组空判断写 `arr is null || arr.length() == 0`。
- 数组长度 `arr.length()`，不是 `.Length`；扩容 `arr.resize(n)`。
- 循环索引用 `uint`：`for (uint i = 0; i < arr.length(); i++)`。
- 浮点字面量必须带 `f` 后缀**且带小数点**：`2.0f` 合法，`100f` 非法（须写 `100.0f`）。
- 整数除法要显式转换：`sum / float(arr.length())`。
- 类型转换写 `float(x)` / `int(x)`。
- `foreach(auto val : arr)`；字典 `foreach(auto value, auto key : dict)`。

## 可用全局函数

- 数学：`sqrt` `pow` `abs` `ceil` `floor` `sin` `cos` `tan` `asin` `acos` `atan` `atan2` `log` `log10` `closeTo`
- 常量：`PI` `E` `null`
- 字符串：`join` `parseInt` `parseUInt` `parseFloat` `formatInt` `formatUInt` `formatFloat`
- 异常/线程：`throw("...")` `getExceptionInfo()` `sleep(ms)` `yield()`

## 明确不支持（遇到必须中断并询问，不得臆造）

- IMAGE 图像类型及任何 helper 图像接口
- `byte` / `byte[]`
- `ContourPointData` 等无 AS 对应物的类型
- 全局变量读写接口、其它模块参数读写接口（`GetModule` / `SetValue` 等）
- `float.MinValue` 等未在 AS 文档中出现的成员

## C# 脚本 → AS 脚本转换

先读 `UserProperty.cs`（只读，仅提取参数名与类型）与 `UserScript.cs`（业务代码）。读不到就要求用户提供内容，**不要猜参数**。

**第三方库检查（先做，命中即中断）**：

- 立即拒绝：`OpenCvSharp`、`Newtonsoft.Json`、`HalconDotNet`、任何自定义 DLL / NuGet / COM。
- BCL 拒绝：`System.Drawing(.Imaging)`、`System.Xml`、`System.Net`。
- BCL 可改写：`System.Linq` → 手写循环；`System.IO` → AS `file`/`filesystem`。改写必须在说明中注明。

提示语示例：「检测到该 C# 脚本使用了第三方外部库（OpenCvSharp），AS 脚本环境不支持，无法自动转换。请移除这些依赖后重试，或人工重写。」

**类型映射**：

| C# | AS | 输出方式 |
| --- | --- | --- |
| `int`/`float`/`string` | 同名 | 属性名或 `helper.SetInt/SetFloat/SetString` |
| `bool` | `bool` | 仅属性名 |
| `double` | `double` | 仅属性名 |
| `int[]`/`float[]`/`string[]` | `array<int>`/`array<float>`/`array<string>` | `helper.SetXxxArray` |
| `RoiboxData[]` | `array<ROIBOX>`（单个用 `ROIBOX`） | `helper.SetRoiBoxArray` / `SetRoiBox` |
| `PointData[]` | `array<Point>` | `helper.SetPointArray`（`PointX`→`X`、`PointY`→`Y`） |
| `CircleData[]` | `array<Circle>` | `helper.SetCircleArray`（`CenterX/CenterY/Radius` 同名） |
| `RectData[]` | `array<Rect>` | `helper.SetRectArray` |
| `LineData[]` | `array<Line>` | `helper.SetLineArray`（`StartPointX`→`StartX` 等） |
| `EllipseData[]` | `array<Ellipse>` | `helper.SetEllipseArray` |
| `AnnulusData[]` | `array<RoiAnnulus>` | `helper.SetRoiAnnulusArray` |
| `PolygonData[]` | `array<RoiPolygon>` | `helper.SetRoiPolygonArray` |
| `DefectData[]` | `array<Defect>` | `helper.SetDefectArray` |
| `FixtureData[]` | `array<Fixture>` | `helper.SetFixtureArray` |
| `ImageData`/`byte[]`/`ContourPointData[]` | 不支持 | 中断，告知文档未定义 |

**逻辑重写**：`arr.Length`→`arr.length()`；`for (int i...)`→`for (uint i...)`；`== null`→`is null`；`(int)x`→`int(x)`；浮点补 `f`；`string.Format`/内插→`+` 拼接配合 `formatFloat`/`formatInt`；LINQ→手写循环；`errorStatus` + `return false` → `catch { getExceptionInfo(); helper.Print(...); Ret = -1; }`；输出数组先拷贝到新数组再 `helper.SetXxxArray`。

lambda / 委托 / 事件 / 泛型集合无法等价改写时中断并提示不支持。

转换是**语义等价重写，不是逐行翻译**；被降级、改写或省略的特性必须在交付说明中列出。

## 交付前静态自检

- 类名 `MyAlgorithm` 继承 `BaseAlgorithm`，`process()` 返回 `int`
- `process()` 有 `try/catch`，`catch` 内 `getExceptionInfo()` + `helper.Print()` + `Ret = -1`
- 数组/几何数组输出全部走 `helper.SetXxxArray`，没有属性名直接赋值数组
- `is null` 只用于引用类型；`string` 用 `isEmpty()`/`length() == 0`
- 用 `arr.length()` 而非 `.Length`；循环索引 `uint`
- 浮点字面量带 `f` 且带小数点（`100.0f` 而非 `100f`）
- 数值转字符串用 `formatFloat`/`formatInt`/`formatUInt`，无 `string(数值)`
- 变量名无中文，下划线或字母开头
- 未使用不支持清单里的 API/类型
- 输出的角度值已归一化到 `[-180, 180]`
- 已附输入/输出配置清单（名称 + AS 类型）

## 参考示例

```angelscript
class MyAlgorithm:BaseAlgorithm
{
    int process()
    {
        int Ret = 0;
        try
        {
            array<float> data = in0;
            if (data is null || data.length() == 0)
            {
                helper.SetString("out2", "input array is empty");
                return -1;
            }

            float sum = 0.0f;
            float maxValue = data[0];
            for (uint i = 0; i < data.length(); i++)
            {
                sum += data[i];
                if (data[i] > maxValue) maxValue = data[i];
            }
            float avg = sum / float(data.length());

            array<float> scaled;
            scaled.resize(data.length());
            for (uint i = 0; i < data.length(); i++)
            {
                scaled[i] = data[i] * 2.0f;
            }

            out0 = avg;
            out1 = maxValue;
            helper.SetFloatArray("out3", scaled);
        }
        catch
        {
            string info = getExceptionInfo();
            helper.Print("getExceptionInfo: " + info);
            Ret = -1;
        }
        return Ret;
    }
}
```
