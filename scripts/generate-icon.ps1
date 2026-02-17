param(
    [string]$SourcePng = "E:\EProjects\wcar\Gemini_Generated_Image_tp0c0stp0c0stp0c.png",
    [string]$OutputIco = "E:\EProjects\wcar\Wcar\wcar.ico"
)

Add-Type -AssemblyName System.Drawing

$source = [System.Drawing.Image]::FromFile($SourcePng)

# Generate resized bitmaps for each ICO size
$sizes = @(16, 20, 24, 32, 48, 256)
$bitmaps = @()

foreach ($sz in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $sz, $sz
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($source, 0, 0, $sz, $sz)
    $g.Dispose()
    $bitmaps += $bmp
}

$source.Dispose()

# Write multi-size ICO file
$ms = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter $ms

$writer.Write([uint16]0)               # reserved
$writer.Write([uint16]1)               # type: icon
$writer.Write([uint16]$bitmaps.Count)  # image count

# Encode each bitmap as PNG
$pngStreams = @()
foreach ($bmp in $bitmaps) {
    $pngMs = New-Object System.IO.MemoryStream
    $bmp.Save($pngMs, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngStreams += $pngMs
}

# Directory entries
[int]$offset = 6 + ($bitmaps.Count * 16)

for ($i = 0; $i -lt $bitmaps.Count; $i++) {
    $bmp = $bitmaps[$i]
    $pngMs = $pngStreams[$i]
    [byte]$w = if ($bmp.Width -ge 256) { 0 } else { $bmp.Width }
    [byte]$h = if ($bmp.Height -ge 256) { 0 } else { $bmp.Height }

    $writer.Write([byte]$w)
    $writer.Write([byte]$h)
    $writer.Write([byte]0)              # color palette
    $writer.Write([byte]0)              # reserved
    $writer.Write([uint16]1)            # color planes
    $writer.Write([uint16]32)           # bits per pixel
    $writer.Write([uint32]$pngMs.Length)
    $writer.Write([uint32]$offset)
    $offset += [int]$pngMs.Length
}

foreach ($pngMs in $pngStreams) {
    $writer.Write($pngMs.ToArray())
    $pngMs.Dispose()
}

$writer.Flush()
[System.IO.File]::WriteAllBytes($OutputIco, $ms.ToArray())
$ms.Dispose()
foreach ($bmp in $bitmaps) { $bmp.Dispose() }

Write-Host "Icon created: $OutputIco" -ForegroundColor Green
Write-Host "Source: $SourcePng" -ForegroundColor Gray
Write-Host "Sizes: 16, 20, 24, 32, 48, 256 px" -ForegroundColor Gray
