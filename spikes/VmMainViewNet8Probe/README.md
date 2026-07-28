# VM MainViewControl .NET 8 compatibility probe

This isolated probe tests whether the VisionMaster 4.4 `.NET Framework 4.6.1`
`VMControls.WPF.MainViewControl` can be referenced and instantiated inside a
`net8.0-windows` x64 WPF process. It is intentionally not part of the product
solution and must not be used as a shipping dependency without a successful
runtime result.

## Result (2026-07-22)

- Direct reference builds successfully with 0 warnings and 0 errors.
- The `.NET Framework 4.6.1` control can be constructed in a .NET 8 process after VM assembly probing is configured.
- The control requires the VisionMaster-relative `myLibs` configuration layout.
- After that layout is supplied, attaching the control to a real WPF window blocks in `Window.Show()` and never completes within the 20-second watchdog.
- The matching `net461` control experiment blocks at the same stage, so this is not only a CLR compatibility issue; `MainViewControl` depends on initialization performed by the VisionMaster application host.

Conclusion: do not embed this control directly in the shipping .NET 8 Desktop application.
