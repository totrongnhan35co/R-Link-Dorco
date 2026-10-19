# Script tạo database test cho Woka
# 160 mã QR với ký tự GS1 (ASCII 29) theo định dạng mẫu

$testDataDir = "D:\R-Link-Branch-Nhan\R_Link\TestData\Woka"
if (-not (Test-Path $testDataDir)) {
    New-Item -ItemType Directory -Path $testDataDir -Force | Out-Null
}

$outputFile = Join-Path $testDataDir "TestData_Woka_160codes.csv"
$gs = [char]0x1D  # Group Separator (GS1)

$lines = New-Object System.Collections.Generic.List[string]
for ($i = 1; $i -le 160; $i++) {
    $serial1 = $i.ToString("D4")
    $serial2 = ([char[]](65..90) | Get-Random -Count 6) -join ""
    $signature = "eEt9s6xWFplEMIkCQM3+qZJ4lmsEvw4zjWyloVGDMTY="
    $qr = "0108938532509888215Gc.2G'cID1P2${gs}91EE${serial1}${gs}92${serial2}${signature};"
    $lines.Add($qr)
}

[System.IO.File]::WriteAllLines($outputFile, $lines, [System.Text.Encoding]::UTF8)
Write-Host "Da tao file test: $outputFile"
Write-Host "Tong so ma: $($lines.Count)"
