---
name: patch-business-solution
description: 向已有 VisionMaster 业务 SOL 安全加入脚本，同时保留基底并验证模块、端口、绑定和生成结果。
---

# Patch 业务 SOL

当用户要求向已有 SOL 添加或修改脚本时使用本技能。

1. 确认用户提供了基底 SOL 的绝对路径；没有路径时先询问。
2. 必须调用 `vm_inspect_solution`，不要从文件名推断流程或模块。
3. 若操作引用现有模块或参数，只使用 inspect 证据中存在的名称。
4. 使用项目内能力目录确认脚本载体和端口类型；目录没有覆盖的能力先向用户索取已验证样例。
5. Requirement 的 `task.mode` 必须为 `patch`，`task.baseSolution` 必须与已检查路径一致。
6. 调用 `vm_update_requirement` 提交完整 IR，再直接调用 `vm_compile_solution`；它内部执行 Patch、校验和离线验证。
7. 验证基底哈希未变化；`vm_compile_solution` 已返回构建后的离线验证结果，除非诊断需要，不要再次调用 `vm_validate_solution`。
8. 交付新 SOL，不覆盖或移动基底。
