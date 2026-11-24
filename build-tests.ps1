$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    throw "csc.exe (.NET Framework 4.x) not found. Please install .NET Framework 4.x."
}

$src = Join-Path $root "src"
$exe = Join-Path $root "tests.exe"
$sources = @(
    (Join-Path $root "tests\Tests.cs"),
    (Join-Path $src "ComplexValue.cs"),
    (Join-Path $src "CalcState.cs"),
    (Join-Path $src "Engine.cs"),
    (Join-Path $src "Statistics.cs"),
    (Join-Path $src "MatrixMath.cs"),
    (Join-Path $src "Solvers.cs"),
    (Join-Path $src "BaseN.cs"),
    (Join-Path $src "Data.cs")
)

$cscArgs = @(
    "/nologo",
    "/target:exe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/out:$exe",
    "/reference:System.dll",
    "/reference:System.Core.dll"
)
$cscArgs += $sources

& $csc $cscArgs
if ($LASTEXITCODE -ne 0) { throw "Test build failed: $LASTEXITCODE" }

& $exe
if ($LASTEXITCODE -ne 0) { throw "Tests failed: $LASTEXITCODE" }
