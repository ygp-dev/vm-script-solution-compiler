# DXF 确定性渲染与 Agent 错误恢复

## 责任边界

DXF 转图不再由大模型生成 C# 源码。模型只提交 `dxfRender` Requirement 操作及端口映射，Compiler Core 生成并预编译固定实现。这样实体 API、VM `ImageData` 构造和内存策略不随模型输出变化。

## DXF 端口契约

- 输入：DXF 路径 `string`、宽度 `int`（默认 1920）、高度 `int`（默认 1080）。宽高是普通 VM 输入，用户可设置最终尺寸。
- 输出：图像 `image`、成功 `bool`、错误消息 `string`、源实体数 `int`、已绘制实体数 `int`。
- 依赖：必须声明 `netDxf.dll`，绘图使用 Framework `System.Drawing.dll`。
- 支持：Line、Circle、Arc、Ellipse、Polyline2D、Polyline3D、Spline、Insert/Block 和 Point。Insert 使用 `Explode()` 后递归绘制，最大深度为 8。

如果实体集合中没有支持的可绘制对象，返回 `DXF_NO_DRAWABLE_ENTITIES` 且图像为 null；如果绘制后没有任何非白像素，返回 `DXF_RENDER_WHITE_IMAGE`。两种情况都不会把白图当作成功。

## 内存策略

默认预览由历史 5472×3648 降至 1920×1080。转换阶段只分配一份最终 RGB24 数组和一行临时数组；Bitmap 自身之外不再创建第二张全尺寸 Bitmap 或第二个全尺寸像素缓冲。尺寸限制为单边 64..8192，且总像素不超过 33,554,432。

## Agent 错误恢复

- 单轮 Requirement 校验最多 3 次。
- 同类自动编译错误最多连续 2 次。
- 同一 `task.name` 跨轮累计最多 6 个 Requirement 版本；只有真正的新任务才重置累计值。
- 新 Requirement 会清除上一版本已经解决的 compiler unresolved 项；仍存在的问题由下一次校验重新加入。
- C# 预编译失败返回 `details.diagnostics[]`，包含 `file`、`line`、`column`、`code`、`category`、`message`。Agent 必须按诊断修复，不得反复猜测第三方 API。

## 自动验证

`tests/run-m13-smoke.ps1` 使用项目内 `测试1.dxf` 做真实运行验证：20 个 Polyline2D 与 2 个 Spline 均被绘制，1920×1080 RGB24 输出必须包含非白像素。测试还动态生成 Ellipse + Insert/Block DXF、空 DXF，并验证结构化 C# 编译错误。
