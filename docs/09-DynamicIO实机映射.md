# VM 4.4 DynamicIO 实机映射

## C# 复杂类型的双层类型名

VM 4.4 ShellModule 的可用复杂逻辑端口在 `Input`/`Output` 中使用 `IMAGE`、`ROIBOX`、`ROIBOX[]`、`ROIANNULUS`、`ROIPOLYGON`、`POINT`、`LINE`、`FIXTURE`、`Rect`、`ELLIPSE`；`pointset` 保持小写。不能把复杂端口写成一个同名的 DynamicIO `Filter`。

`CircleData[]` 和 `GetVarCircle/SetVarCircle` 存在于方法程序集，但用户于 2026-07-17 确认 VM 4.4 ShellModule 的输入/输出类型列表没有 Circle，强行写入 `CIRCLE` 只显示空类型。因此 `circle` 是变量 API 类型，不是可生成的脚本端口类型；编译器返回 `CSHARP_PORT_TYPE_UNSUPPORTED`，不再输出假端口。

2026-07-16 已把用户通过 VM 正常添加并保存的 ShellModule 提取为不含 SOL 的结构签名 `resources/vm/4.4.0/csharp-io-layout-evidence.json`。M8 会逐项比较 ValueType 顺序、StructName 底层字段数、DynamicIO Combination 前序结构、顶层端口数量、UiParamData 键和 AssemblyGuid 长度；资源哈希也由 manifest 校验。

正常由 VM 添加的复杂端口包含三层关联：

1. `Input`/`Output` 的 `Name` 是脚本属性名，`StructName` 是由回车符分隔的底层字段名。
2. `DynamicInData`/`DynamicOutData` 用带 `Style` 的 `Combination` 包住底层 `Filter`，例如图像拆为 image、width、height、pixel-format，ROIBOX 拆为中心、宽、高、角度。
3. `UiParamData` 保存数组类型、数据记录以及输出图像的 `Mapping`/`RelateIO`；C# 模块同时需要非空 `AssemblyGuid`。

缺少第 1、2 层时，端口名称可能仍可见，但数据源无法绑定，脚本的 `GetImageValue`、`GetRoiboxValue` 和 `InternalMethods.Get*ArrayValue` 找不到对应对象。此前将复杂类型改成小写的结论已由用户保存的正常 VM 脚本推翻。

2026-07-15 的全类型实机预编译反馈证明，把运行时大写常量误写到 `Input`/`Output` 会导致 VM 不生成对应属性，并产生 22 个“当前上下文中不存在名称”错误。编译器已据此拆分两层映射；离线 C# companion 只能验证源码与属性契约的类型一致性，不能替代 VM 自身的属性生成预编译。

本映射来自用户在 VisionMaster 4.4 中手工配置端口并保存后的 SOL。样本只位于忽略提交的 `outputs` 中，不纳入正式资源。

样本 SHA-256：

```text
7A1C7D53B53109C96CBE3C94D1B1D5C60D823A5B76717493E5219259D6C08927
```

## 已确认结构

`Input` 和 `Output` 使用：

```xml
<ArrayOfModuleParamItem>
    <ModuleParamItem>
        <Name>%A%</Name>
        <StructName>%A%</StructName>
        <ValueType>float</ValueType>
        <IsForce>true</IsForce>
        <IsShow>true</IsShow>
    </ModuleParamItem>
</ArrayOfModuleParamItem>
```

`DynamicInData` 和 `DynamicOutData` 使用 `ParamRoot/Categorys/Category/Items/Filter`。自定义端口名称同样带 `%`；自定义端口的 `CustomVisible/CanSubscribe/Visible/VisibleInResultTree` 为 `True`。

输出必须包含 VM 内置项：

- `ModuRunTime`
- `ModuStatus`
- ShellModule 额外包含 `ResultShow`
- DynamicOutData 包含四个时间戳：`fArrivalTimeStampLow/High`、`fLeaveTimeStampLow/High`

## 终止符

- ShellModule `Input/Output`：两个 NUL。
- PyShellModule `Input/Output`：一个 NUL。
- 两类模块 `DynamicInData/DynamicOutData`：一个 NUL。

## 回归长度

对于 A、B 两个输入和 Sum 一个输出：

| 模块 | Input | Output | DynamicInData | DynamicOutData |
|---|---:|---:|---:|---:|
| ShellModule float | 504 | 963 | 785 | 1761 |
| PyShellModule int | 499 | 739 | 781 | 1759 |

自动测试按以上精确长度回归，避免再次用解析器可读但 VM 不识别的自定义 XML。

## C# 端口 API

实测证明 ShellModule 中不存在 `Input`/`Output` 上下文，且 `dynamic` 会缺少 `Microsoft.CSharp.RuntimeBinder`。新增的官方反编译源码进一步确认：VM 根据 Input/Output 定义生成 `UserScript` 的强类型 partial 属性。输入属性内部调用 `GetIntValue/GetFloatValue/...`，输出属性内部调用 `SetIntValue/SetFloatValue/...`，因此用户逻辑应直接写 `Sum = A + B`，而不是访问 `Input.A` 或 `Output.Sum`。

完整类型与方法映射位于 `resources/vm/4.4.0/type-system.json`，模块、变量、流程和通信 API 位于 `resources/vm/4.4.0/api-catalog.json`。VM 4.4 的 `ScriptDefine` 没有原生 bool 端口，确定性生成器因此把 IR bool 映射为 VM int：`false=0`、`true=1`；C# 与 Python 生成代码会自动完成布尔表达式和 0/1 端口值之间的转换。含 bool 端口的自定义源码必须显式改用 int 端口，以免生成 VM 无法识别的属性。

VM 4.4 的端口描述参数不包含运行时初始值。Requirement 的 `default` 不能仅靠 `Input`/`DynamicInData` XML 自动出现在模块运行值中；自动生成的 Python 逻辑必须在读取值为 `None` 时显式使用默认值。用户提供的自定义源码仍由源码自身决定空值处理策略。

## 输入初值持久化映射

已保存样本和生成结果中，值关系位于：

```text
/Root/ModulesInfo/ModuleSubscribe/Subscribe/@Relation
```

VM 4.4 关系串格式为：

```text
模块Index . %输入端口名% . 0 . 默认值 . 1 . 0 . All . 1
```

Core 会按流程和显示名定位模块 Index，为 int、float、以 int 0/1 表示的 bool，以及不含关系分隔符的 string 写入关系，并替换同一模块同一端口的旧关系。用户在 VM 4.4 中手工写入并保存的 C# float 值 `1.25/2.5` 和 Python int 值 `11/13` 采用相同编码，另存后的 SOL 仍原样保留，因此 int/float 初值持久化映射已确认。bool 使用同一个 int 结构，但尚无同等级手工样本；string 也仍属于结构编码。数组和复杂类型不会伪造未知的 `ModuleSubscribe` 编码，也不会因此阻断端口生成；Python 对可安全表示的标量/数组另行生成 `None` 回退。输出 `default` 不写入 `ModuleSubscribe`。对于未写入的声明，构建结果会在 `script-contract.json`、`build-report.md` 及 Agent/MCP 返回值的 `defaultPersistenceNotices` 中给出明确诊断，不再静默丢失。

用户实测确认：手工初值保存为上述关系，VM 另存时不会迁移到其他载荷。构建报告只声明 int/float 初值持久化映射已确认，不把它扩大为脚本运行结果；Python 确定性生成器另行生成 `None` 回退，C# 输入采用 VM 强类型属性的实际值。
