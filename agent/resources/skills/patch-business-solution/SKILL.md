---
name: patch-business-solution
description: 向已有 VisionMaster 业务 SOL 安全加入脚本，同时保留基底并验证模块、端口、绑定和生成结果。
---

# Patch 业务 SOL

当用户要求向已有 SOL 添加或修改脚本时使用本技能。

1. 确认用户提供了基底 SOL 的绝对路径；没有路径时先询问。
2. 必须调用 `vm_inspect_solution`，不要从文件名推断流程或模块。
3. 若操作引用现有模块或参数，只使用 inspect 证据中存在的名称。
4. 查询所需脚本载体和端口类型的能力证据。
5. Requirement 的 `task.mode` 必须为 `patch`，`task.baseSolution` 必须与已检查路径一致。
6. 调用 `vm_validate_requirement` 和 `vm_plan_solution` 后才能调用 `vm_patch_solution`。
7. 验证基底哈希未变化，并对结果调用 `vm_validate_solution`。
8. 交付新 SOL，不覆盖或移动基底。
