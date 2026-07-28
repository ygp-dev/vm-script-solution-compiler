param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $SkipBuild) {
    & dotnet build (Join-Path $root 'VmScriptCompiler.sln') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
}

$mcp = Join-Path $root 'src\VmScriptCompiler.Mcp\bin\Release\net8.0\vm-script-compiler-mcp.dll'
$desktop = Join-Path $root 'src\VmScriptCompiler.Desktop\bin\Release\net8.0-windows\vm-script-compiler-desktop.dll'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('vm-script-compiler-m6-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
$oldInputEncoding = [Console]::InputEncoding
$oldOutputEncoding = [Console]::OutputEncoding
$utf8 = New-Object System.Text.UTF8Encoding($false)
[Console]::InputEncoding = $utf8
[Console]::OutputEncoding = $utf8

$startInfo = New-Object Diagnostics.ProcessStartInfo
$startInfo.FileName = 'dotnet'
$startInfo.Arguments = '"' + $mcp + '" --repository-root "' + $root + '"'
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardInput = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$process = New-Object Diagnostics.Process
$process.StartInfo = $startInfo

function Invoke-McpRequest([int] $id, [string] $method, $params) {
    $request = @{ jsonrpc = '2.0'; id = $id; method = $method; params = $params } | ConvertTo-Json -Depth 20 -Compress
    $process.StandardInput.WriteLine($request)
    $process.StandardInput.Flush()
    $line = $process.StandardOutput.ReadLine()
    if ([string]::IsNullOrWhiteSpace($line)) { throw "MCP returned no response for $method." }
    $response = $line | ConvertFrom-Json
    if ($null -ne $response.error) { throw "MCP protocol error for ${method}: $($response.error.message) [$($response.error.data)]" }
    return $response.result
}

function Read-ToolText($result) {
    if ($result.isError) { throw "MCP tool error: $($result.content[0].text)" }
    return $result.content[0].text | ConvertFrom-Json
}

try {
    [void]$process.Start()
    $initialize = Invoke-McpRequest 1 'initialize' @{ protocolVersion = '2024-11-05'; capabilities = @{}; clientInfo = @{ name = 'm6-smoke'; version = '1' } }
    if ($initialize.serverInfo.name -ne 'vm-script-solution-compiler') { throw 'Unexpected MCP server identity.' }
    $tools = Invoke-McpRequest 2 'tools/list' @{}
    $expected = @('detect_environment', 'inspect_solution', 'query_capability', 'validate_requirement', 'plan_solution', 'build_solution', 'patch_solution', 'validate_solution', 'read_build_report')
    foreach ($name in $expected) { if ($name -notin @($tools.tools.name)) { throw "Missing MCP tool: $name" } }

    $environment = Read-ToolText (Invoke-McpRequest 3 'tools/call' @{ name = 'detect_environment'; arguments = @{} })
    if (-not $environment.Found) { throw 'MCP environment detection failed.' }
    $plan = Read-ToolText (Invoke-McpRequest 4 'tools/call' @{ name = 'plan_solution'; arguments = @{ spec = (Join-Path $root 'tests\fixtures\m2-global-create.json') } })
    if (-not $plan.Ok) { throw 'MCP planning failed.' }

    $create = Read-ToolText (Invoke-McpRequest 5 'tools/call' @{ name = 'build_solution'; arguments = @{ spec = (Join-Path $root 'tests\fixtures\m2-global-create.json'); output = (Join-Path $temp 'create') } })
    if (-not (Test-Path -LiteralPath $create.SolutionFile)) { throw 'MCP build did not create result.sol.' }
    $validation = Read-ToolText (Invoke-McpRequest 6 'tools/call' @{ name = 'validate_solution'; arguments = @{ file = $create.SolutionFile } })
    if (-not $validation.Ok) { throw 'MCP validation failed.' }
    $inspection = Read-ToolText (Invoke-McpRequest 10 'tools/call' @{ name = 'inspect_solution'; arguments = @{ file = $create.SolutionFile } })
    if (-not $inspection.Ok -or [string]::IsNullOrWhiteSpace($inspection.sha256)) { throw 'MCP inspection failed.' }
    $capability = Read-ToolText (Invoke-McpRequest 11 'tools/call' @{ name = 'query_capability'; arguments = @{ query = 'point'; vmVersion = '4.4.0' } })
    if (@($capability.matches).Count -eq 0) { throw 'MCP capability query failed.' }
    $requirement = Read-ToolText (Invoke-McpRequest 12 'tools/call' @{ name = 'validate_requirement'; arguments = @{ spec = (Join-Path $root 'tests\fixtures\m2-global-create.json') } })
    if (-not $requirement.Ok) { throw 'MCP requirement validation failed.' }
    $report = Read-ToolText (Invoke-McpRequest 13 'tools/call' @{ name = 'read_build_report'; arguments = @{ file = $create.ReportFile } })
    if (-not $report.Ok -or @($report.artifacts).Count -eq 0) { throw 'MCP report read failed.' }

    $base = Join-Path $temp 'business.sol'
    & (Join-Path $root 'scripts\materialize_script_base.ps1') -OutputFile $base | Out-Null
    $baseHash = (Get-FileHash -LiteralPath $base -Algorithm SHA256).Hash
    $patch = Read-ToolText (Invoke-McpRequest 7 'tools/call' @{ name = 'patch_solution'; arguments = @{ baseSolution = $base; spec = (Join-Path $root 'tests\fixtures\m2-global-patch.json'); output = (Join-Path $temp 'patch') } })
    if (-not (Test-Path -LiteralPath $patch.SolutionFile)) { throw 'MCP patch did not create result.sol.' }
    if ((Get-FileHash -LiteralPath $base -Algorithm SHA256).Hash -ne $baseHash) { throw 'MCP patch modified the base SOL.' }

    $process.StandardInput.Close()
    if (-not $process.WaitForExit(10000)) { $process.Kill(); throw 'MCP server did not stop after stdin closed.' }
    if ($process.ExitCode -ne 0) { throw "MCP server exited with code $($process.ExitCode): $($process.StandardError.ReadToEnd())" }

    & dotnet $desktop --smoke-test
    if ($LASTEXITCODE -ne 0) { throw 'Desktop composition smoke test failed.' }

    [PSCustomObject]@{ ok = $true; mcpTools = $expected.Count; mcpBuild = $true; mcpPatch = $true; inspect = $true; capability = $true; report = $true; baseUnchanged = $true; desktopStartup = $true } | ConvertTo-Json
}
finally {
    if (-not $process.HasExited) { $process.Kill() }
    $process.Dispose()
    [Console]::InputEncoding = $oldInputEncoding
    [Console]::OutputEncoding = $oldOutputEncoding
    if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force }
}
