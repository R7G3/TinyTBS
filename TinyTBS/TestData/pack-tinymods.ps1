# Rebuild TestData/Tinymods/*.tinymod.zip from Modules/ (module.json at zip root).
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$modulesRoot = Join-Path $here 'Modules'
$tinymodsRoot = Join-Path $here 'Tinymods'

if (-not (Test-Path $modulesRoot)) {
    throw "Modules folder not found: $modulesRoot"
}

New-Item -ItemType Directory -Path $tinymodsRoot -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem

$moduleIds = @('test_theme', 'test_units', 'test_buildings', 'test_scenario')
foreach ($moduleId in $moduleIds) {
    $moduleDir = Join-Path $modulesRoot $moduleId
    if (-not (Test-Path $moduleDir)) {
        throw "Missing module folder: $moduleDir"
    }

    $zipPath = Join-Path $tinymodsRoot ($moduleId + '.tinymod.zip')
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $moduleDir,
        $zipPath,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $false)

    Write-Host "Packed $moduleId -> $zipPath"
}

Write-Host 'Done.'
