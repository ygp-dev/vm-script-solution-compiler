# vm-script-tutor

VisionMaster (VM) **2D** C# 脚本开发辅助技能 —— 生成、修改、排查 VM 脚本，优先输出可直接使用的代码，并附 VM 模块 UI 手动配置清单。

- **作者**：Mr.Buer
- **版本**：1.2
- **范围**：仅 2D VisionMaster；**3D VM 暂不支持**
- **适用工具**：Claude Code、Trae、Kimi Code 等支持 skill 加载的 AI 编程工具

## 快速开始

1. 在 AI 工具中打开导出工程目录，输入 `/vm-script-tutor` 触发技能
2. 技能会自动检测 VM 版本、读取 `UserProperty.cs`，按流程确认后生成 `UserScript.cs`
3. 详细规则见 [SKILL.md](SKILL.md)

## 项目结构

```
vm-script-tutor/
├── SKILL.md                    # 技能定义（规则、工作流、文件索引）—— 所有工具通用
├── README.md                   # 本文件（用户使用指南 + 项目概览）
├── .gitignore                  # Git 忽略规则（.workbuddy / .claude / .codebuddy）
├── output_report_template.md   # 生成后给用户的 UI 手动配置清单模板
├── assets/
│   └── find_msbuild.ps1        # 定位本机 MSBuild.exe 路径（可选，支持 shell 的工具可用于执行真实编译）
├── examples/                   # 代码生成参考（示例 + 映射表 + 模式库）
│   ├── 01-basic-template.cs
│   ├── 02-canny-edge-detection.cs
│   ├── 03-roi.cs
│   ├── 04-trans-CAD-file.cs
│   ├── 05-halcon-image-conversion.cs
│   ├── interface-quickref.md   # 变量类型映射 + 接口速查
│   └── code-patterns.md        # 代码模式库（模式 1–13，含 4b/4c/13a/13b/13c 变体）
└── references/                 # 原始接口与数据结构定义
    ├── Script.Interface.cs     # ScriptMethods 标量 Get/Set 公开签名
    ├── Script.DataStruct.cs    # 几何与图像数据结构定义
    ├── Script.ExMethods.cs     # Mat / Bitmap ↔ ImageData 转换实现
    ├── InternalMethods.cs      # Conceal.InternalMethods（数组 Get/Set 2 参版本）
    ├── VisionMaster模块映射表.md # 工具箱/模块中文名 ↔ 英文名映射（AlgorithmTab.xml 路径查询前置依赖）
    ├── module-param-workflow.md # §6 模块参数设置/获取完整工作流（步骤 0–4 + 降级处理汇总）
    ├── csharp_api.md           # 官方开放接口完整列表
    └── csharp_debug.md         # VS 附加进程断点调试操作步骤
```

## 运行依赖

| 依赖           | 版本要求                  | 说明                                        |
| -------------- | ------------------------- | ------------------------------------------- |
| .NET Framework | 4.6.1                     | VM 脚本运行时                               |
| VisionMaster   | 2D，VM 4.x（4.3 / 4.4+） | 目标平台，需告知技能准确版本                |
| Windows        | 10 / 11                   | 操作系统                                    |
| Visual Studio  | 2013+（可选）             | 仅在支持 shell 的工具中用于真实编译校验或断点调试 |

## 核心范式

变量读写一律使用**直接赋值**，不使用 Get/Set 遗留接口：

```csharp
int val = in0;              // 读取标量
out0 = val * 2;             // 写入标量
PointData[] pts = in0;      // 读取复合类型
out0 = pts;                 // 写入复合类型
ImageData img = imgIn;      // 读取图像
imgOut = img;               // 写入图像
```

Get/Set 遗留接口仅在需要按字符串动态访问变量名时才使用。

---

## 标准使用流程

### Step 1：在 VM 中新建脚本模块

1. 拖入 **脚本** 模块至流程编辑区
2. 双击打开配置窗口，在 **输入设置** / **输出设置** 中手动添加变量
   - 变量名将作为 C# 代码中的字段名直接引用，命名需符合 C# 标识符规范
   - 矩形框/ROI 默认使用 **ROIBOX**（对应 `RoiboxData[]`），仅在确需轴对齐时选 **RECT**

### Step 2：导出工程

单击编辑区上方的 **导出工程** 按钮，选择导出目录后复制该目录的绝对路径备用。

> 导出工程包含 `UserProperty.cs`（VM 自动生成，**只读**）、`UserScript.cs`（用户编辑入口）、`*.csproj`。

### Step 3：进入 AI 工具并触发技能

1. 用 Claude Code / Trae 等支持本技能的 AI 工具打开 **导出工程目录**
2. 在对话窗口输入 `/vm-script-tutor` 触发技能

### Step 4：描述脚本功能需求

用自然语言说明：

- 输入参数的含义与来源
- 输出参数的含义与计算方式
- 算法关键步骤或参考算子
- 如需动态设置其他模块的参数，说明模块名称、参数描述及目标值
- 如有第三方库依赖（如 OpenCvSharp）一并说明

### Step 5：等待技能给出实施大纲并请求确认

技能在写代码前会主动发起确认：

| 确认项 | 触发原因 |
| --- | --- |
| **VM 版本确认** | `RoiboxData` 和 `ImageData` 在 VM ≤ 4.3 拼写为 `Heigth`，VM ≥ 4.4 修正为 `Height`。技能先从系统 PATH 自动检测，再告知用户结果 |
| **需求歧义澄清** | 同类型输入变量 ≥ 2 个未指名、聚合方式不明、一对多场景等，技能会列出候选与建议方案 |
| **模块参数 Key 确认** | 设置算法模块参数时，技能先从映射表查工具箱/模块英文名并请用户确认，再从 `AlgorithmTab.xml` 提取参数名和值域，超限时告知用户 |
| **程序集依赖中断** | 代码依赖的 DLL 不在当前 csproj 中时，技能**中断生成**，引导用户先在 VM 中添加程序集并重新导出工程 |

实施大纲格式示例：

```
【实施大纲】
- VM 版本：4.4
- 输入变量映射：in0 → float、inArr → float[]
- 输出变量映射：outArr → float[]
- 异常字段：errorStatus（类字段，不映射到 VM 输出）
- 算法步骤：
  1. 校验 inArr 非空
  2. 对每个 inArr[i] 计算 inArr[i] + in0
  3. 写入 outArr
- 第三方库依赖：无
- 是否加调试输出：否（默认）
```

### Step 6：澄清并回复"确认"

逐项回答版本、歧义点；如对大纲有调整，直接提出修订意见即可。

### Step 7：等待技能完成脚本编程

技能行为：

- 直接 **覆盖写入** 导出工程内的 `UserScript.cs`（若文件已含用户业务代码，先备份为 `UserScript.cs.bak`）
- 完成代码后执行**静态审查**：C# 5.0 语法、变量名拼写、RoiboxData / ImageData 高度字段版本、命名空间、Mat Dispose 等
- 返回 **UI 手动配置清单**（变量表、程序集需求、备份信息、预编译提醒等）
- `UserProperty.cs` 与 `*.csproj` 永远不动

### Step 8：回到 VM 完成配置与验证

1. 双击脚本模块（无需重新导入；如先前选择手动粘贴模式，则在控制栏 → **导入**）
2. **（若 UI 清单中有程序集要求）** 控制栏 → **编辑程序集** → 逐一添加所列 DLL
3. 单击编辑区下方的 **预编译** 按钮
4. 观察 **编译结果显示** 区域无报错后，单击 **执行** 跑一次校验输出
5. **确定** 保存退出

---

## 典型场景速查

| 场景 | 提示 |
| --- | --- |
| 需要在异常分支看到具体错误 | 在 UI 中新增 STRING 输出（如 `errorOut`），并告知技能"请把 `errorStatus` 映射到 `errorOut`" |
| 需要 OpenCvSharp 等第三方库 | 技能检测到 csproj 缺少该 DLL 时会中断并引导添加；完成后重新导出工程，再继续请技能生成代码 |
| 需要动态设置算法模块参数 | 告知技能**模块名称**（如"单点抓取1"）和**参数描述与目标值**；技能自动从映射表查工具箱/模块英文名，再从 AlgorithmTab.xml 查参数 Key 和值域，生成 `SetValue()` 调用 |
| 需要读取算法模块的运行结果 | 告知模块名称和结果参数名（可从 SDK 手册查），技能生成 `GetValue()` 调用 |
| 需要调试输出 | 在确认大纲阶段明确说"加 ShowMessageBox / ConsoleWrite"，并指定输出形式 |
| 需要 VS 断点调试 | 控制栏 → **导出工程** → VS 打开 `.sln` → Build → **调试 → 附加到进程** → 选 VM 主进程；详见 `references/csharp_debug.md` |
| 多版本 VM 并存 | 技能会列出 PATH 中检测到的全部版本请用户指定 |

---

## 常见问题

| 现象 | 原因 / 处理 |
| --- | --- |
| 预编译报"命名空间中不存在类型" | 漏加非默认引用程序集；按 UI 清单"编辑程序集"补齐，重新导出工程后预编译 |
| 预编译报 `RoiboxData` 或 `ImageData` 高度字段不存在 | VM 版本与生成时假设不一致（≤4.3 拼写为 `Heigth`，≥4.4 为 `Height`）；告知技能正确版本并重新生成 |
| 技能提示"请先添加程序集" | DLL 未在 csproj 中，技能已中断等待；在 VM 中添加程序集并重新导出工程后告知技能继续 |
| `NullReferenceException` | 输入变量未连线或上游为空；脚本已通过 `try/catch` 写入 `errorStatus`，可查看类字段定位 |
| 模块状态变红 | `Process()` 返回 `false`，检查 `errorStatus` 内容 |
| 内存持续增长 | `Mat` 未 Dispose 或每帧重复创建大对象；技能默认会处理，必要时反馈现象让其复查 |
| 设置模块参数无效 | 参数 Key 名不正确；技能会从映射表查英文名、再从 AlgorithmTab.xml 验证，若 XML 找不到会要求用户提供正确参数名 |

---

## 不支持范围

- **3D VisionMaster**（点云、立体视觉、`PointCloudData`、`VM3DScriptBase` 等所有 3D 脚本）
- Python 脚本模块
- 控制器 IO 发送、UI 自动化、通讯协议解析
- 反编译或读取 VM 安装目录下的 DLL/EXE 文件

遇到上述请求，技能会直接告知不支持，请改用其他技能或人工实现。

---

## 约束摘要（技能自动保证）

- 基于 **.NET Framework 4.6.1** + **C# 5.0** 语法（无 `?.` / `$"..."` / `nameof` / `out var` / 元组 / 模式匹配 / `??=` 等 C# 6+ 特性）
- 所有 C# 接口信息仅来自技能内 `references/` 与 `examples/`，绝不反编译 VM 安装目录 DLL
- 算法模块参数名（Key）从映射表查英文名、再从 `AlgorithmTab.xml` 查询，不猜测；超限值会告知用户
- 变量读写一律直接赋值；`UserProperty.cs` 与 `*.csproj` 严禁修改
- `Process()` 必含 `try/catch` 与 `errorStatus` 字段，默认无调试输出副作用
- 矩形框/ROI 口语默认按 `RoiboxData[]`（带角度）处理
- 多同类型候选时必先请用户指认，不猜测
- 代码交付前完成静态审查（语法 / 变量名 / 版本拼写 / 命名空间 / Mat Dispose）
