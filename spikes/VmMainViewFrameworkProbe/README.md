# VM MainViewControl .NET Framework control probe

Control experiment for the .NET 8 probe. It runs the same hidden WPF visual-tree
test in the framework targeted by VisionMaster 4.4 (`net461`, x64).

## Result (2026-07-22)

The control constructs successfully, but adding it to a WPF window blocks in
`Window.Show()` exactly like the .NET 8 probe. A separate Framework process by
itself is therefore insufficient; it would also need VisionMaster's undocumented
application-host initialization sequence.
