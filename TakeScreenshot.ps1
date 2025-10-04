# PowerShell script to take screenshots for the user manual
param(
    [string]$OutputPath = "Screenshots",
    [string]$FileName = "screenshot.png"
)

# Create Screenshots directory if it doesn't exist
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

# Load required assemblies
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Get the primary screen bounds
$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds

# Create a bitmap to hold the screenshot
$bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height

# Create a graphics object from the bitmap
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Copy the screen to the bitmap
$graphics.CopyFromScreen($bounds.X, $bounds.Y, 0, 0, $bounds.Size)

# Save the screenshot
$fullPath = Join-Path $OutputPath $FileName
$bitmap.Save($fullPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Clean up
$graphics.Dispose()
$bitmap.Dispose()

Write-Host "Screenshot saved to: $fullPath"