$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $root "assets"
$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$pngs = @()

function RoundPath([System.Drawing.RectangleF]$r, [double]$radius) {
    $d = [double][Math]::Min($radius * 2.0, [Math]::Min($r.Width, $r.Height))
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    if ($d -le 0.5) { $p.AddRectangle($r); return $p }
    $p.AddArc($r.X, $r.Y, $d, $d, 180, 90)
    $p.AddArc($r.Right - $d, $r.Y, $d, $d, 270, 90)
    $p.AddArc($r.Right - $d, $r.Bottom - $d, $d, $d, 0, 90)
    $p.AddArc($r.X, $r.Bottom - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function FillRound($g, [System.Drawing.RectangleF]$r, [double]$radius, $brush) {
    $p = RoundPath $r $radius
    $g.FillPath($brush, $p)
    $p.Dispose()
}

foreach ($size in $sizes) {
    $u = $size / 256.0
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $rect = New-Object System.Drawing.RectangleF ([float](16 * $u)), ([float](16 * $u)), ([float](224 * $u)), ([float](224 * $u))
    $gradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(46, 46, 43)), ([System.Drawing.Color]::FromArgb(18, 18, 16)), 90.0
    FillRound $g $rect (44 * $u) $gradient
    $gradient.Dispose()

    $clip = RoundPath $rect (44 * $u)
    $state = $g.Save()
    $g.SetClip($clip)
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(252, 251, 248)), ([float](49 * $u))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Flat
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Flat
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Miter
    $points = @(
        (New-Object System.Drawing.PointF ([float](16 + 224 * -0.10) * $u), ([float](16 + 224 * 0.33) * $u)),
        (New-Object System.Drawing.PointF ([float](16 + 224 * 0.36) * $u), ([float](16 + 224 * 0.77) * $u)),
        (New-Object System.Drawing.PointF ([float](16 + 224 * 0.87) * $u), ([float](16 + 224 * -0.13) * $u))
    )
    $g.DrawLines($pen, $points)
    $pen.Dispose()
    $g.Restore($state)
    $clip.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += ,($ms.ToArray())
    $ms.Dispose()
    $g.Dispose()
    $bmp.Dispose()
}

$ico = Join-Path $dir "icon.ico"
$fs = [System.IO.File]::Open($ico, [System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))
    $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$pngs[$i].Length)
    $bw.Write([UInt32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($data in $pngs) { $bw.Write($data) }
$bw.Flush()
$bw.Close()
$fs.Close()

$previewStream = New-Object System.IO.MemoryStream (,$pngs[$sizes.Count - 1])
$preview = [System.Drawing.Image]::FromStream($previewStream)
$preview.Save((Join-Path $dir "icon-256.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$previewStream.Dispose()

Write-Output "icon: $ico"
