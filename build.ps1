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
$assets = Join-Path $root "assets"
$outDir = Join-Path $root "dist"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

Get-Process | Where-Object { try { $_.Path -like (Join-Path $outDir '*.exe') } catch { $false } } | Stop-Process -Force
Start-Sleep -Milliseconds 300

$exe = Join-Path $outDir "BraubergSC880.exe"
$sources = @(
    "Program.cs",
    "ComplexValue.cs",
    "CalcState.cs",
    "Engine.cs",
    "Statistics.cs",
    "MatrixMath.cs",
    "Solvers.cs",
    "BaseN.cs",
    "Data.cs",
    "UiTheme.cs",
    "DialogKit.cs",
    "Sc880Form.cs",
    "Sc880Form.Paint.cs",
    "ThreeDHost.cs",
    "ThreeDGeometry.cs",
    "ThreeDTextures.cs",
    "ThreeDView.cs",
    "ThreeDView.UI.cs",
    "Forms.Features.cs",
    "Forms.Base.cs",
    "Forms.Stat.cs",
    "Forms.Eqn.cs",
    "Forms.MatrixVector.cs"
) | ForEach-Object { Join-Path $src $_ }

$assemblyRoot = Join-Path $env:WINDIR "Microsoft.NET\assembly"
$coreBitness = if ($csc -match "Framework64") { "GAC_64" } else { "GAC_32" }
$wpfReferences = @(
    (Get-ChildItem (Join-Path $assemblyRoot "$coreBitness\PresentationCore") -Filter "PresentationCore.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1).FullName,
    (Get-ChildItem (Join-Path $assemblyRoot "GAC_MSIL\PresentationFramework") -Filter "PresentationFramework.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1).FullName,
    (Get-ChildItem (Join-Path $assemblyRoot "GAC_MSIL\WindowsBase") -Filter "WindowsBase.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1).FullName,
    (Get-ChildItem (Join-Path $assemblyRoot "GAC_MSIL\System.Xaml") -Filter "System.Xaml.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1).FullName
)
foreach ($reference in $wpfReferences) {
    if (-not $reference -or -not (Test-Path $reference)) {
        throw "WPF reference assemblies were not found. .NET Framework 4.x with WPF is required."
    }
}

$cscArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/out:$exe",
    "/win32manifest:$(Join-Path $assets 'app.manifest')",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll",
    "/win32icon:$(Join-Path $assets 'icon.ico')"
)
foreach ($reference in $wpfReferences) { $cscArgs += "/reference:$reference" }
$cscArgs += $sources

& $csc $cscArgs
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

Write-Output "Built: $exe"
