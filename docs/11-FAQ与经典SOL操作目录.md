# FAQ 与经典 SOL 操作目录

## FAQ 脚本主题

CHM 共筛选出 29 个脚本相关主题，正文已转换为 UTF-8 文本并记录原 CHM 路径。重点能力包括：

- ShellModule 调试、图像保存、字符串显示、点集合处理、32 位寄存器解析、程序集引用。
- OpenCV/Halcon/Mat/Bitmap 与 VM 图像互转。
- GlobalScript 多流程协作、通信触发流程、方案加载后自动执行、全局变量读写、发送通信数据、加载本地图像。
- 脚本模块与循环模块配合、凸包算法、轮廓质心计算。

权威索引：`knowledge/faq-v1.12/index.json`；每个条目的 `textFile` 指向清洗后的完整正文。

## 经典 SOL

9 个 SOL 共提取出 14 个 C# ShellModule。已观察到的典型调用：

- `GetIntValue`
- `GetFloatArrayValue` / `SetFloatArrayValue`
- `GetImageValue` / `SetImageValue`
- `CurrentProcess.GetModule(...).SetValue(...)`
- DXF 读取与绘制
- 共享内存 `ReadFrame/ReadBytes`

各方案模块、端口、连接、订阅、源码路径和哈希记录在 `knowledge/reference-solutions/inventory.json`，提取源码位于 `knowledge/reference-solutions/scripts`。

这些材料作为生成规则和验收夹具的来源，不会被复制成正式 `.sol` 模板。
