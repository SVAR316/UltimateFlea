<#
 Builds a release zip into dist/. Run from the repo root:  powershell -File tools/package.ps1
 Extract the zip into the SPT game folder root.
#>
param(
    [string]$TarkovDir = "C:\SPT",
    [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$csproj = Join-Path $root "UltimateFlea.csproj"
$version = ([xml](Get-Content $csproj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw "Version not found in UltimateFlea.csproj" }

Write-Host "Building server mod ($Configuration)..."
dotnet build $csproj -c $Configuration --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Server build failed" }
$serverOut = Join-Path $root "bin\$Configuration\UltimateFlea"
if (-not (Test-Path (Join-Path $serverOut "UltimateFlea.dll"))) { throw "UltimateFlea.dll not found in $serverOut" }

$clientDll = $null
$clientProj = Join-Path $root "client\UltimateFlea.Client\UltimateFlea.Client.csproj"
Write-Host "Building client plugin..."
$clientDir = Join-Path $root "client\UltimateFlea.Client"
$clientStart = Get-Date
dotnet build $clientProj -c $Configuration --nologo -v q "-p:TarkovDir=$TarkovDir"
if ($LASTEXITCODE -eq 0) {
    $clientDll = Get-ChildItem (Join-Path $clientDir "bin\$Configuration") -Recurse -Filter "UltimateFlea.Client.dll" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
}
if (-not $clientDll) {
    Write-Warning "=========================================================="
    Write-Warning "CLIENT BUILD FAILED (TarkovDir=$TarkovDir). Producing a SERVER-ONLY zip."
    Write-Warning "=========================================================="
}

$stage = Join-Path $root "dist\_stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
$modDir = Join-Path $stage "SPT_Runtime\user\mods\UltimateFlea"
New-Item -ItemType Directory -Force (Join-Path $modDir "config") | Out-Null

Copy-Item (Join-Path $serverOut "UltimateFlea.dll") $modDir
$deps = Join-Path $serverOut "UltimateFlea.deps.json"
if (Test-Path $deps) { Copy-Item $deps $modDir }
Copy-Item (Join-Path $serverOut "wwwroot") (Join-Path $modDir "wwwroot") -Recurse
Copy-Item (Join-Path $root "docs") (Join-Path $modDir "docs") -Recurse
foreach ($f in "levels_bundled.json", "pool_bundled.json") {
    Copy-Item (Join-Path $root "config\$f") (Join-Path $modDir "config\$f")
}
if ($clientDll) {
    $plug = Join-Path $stage "BepInEx\plugins\UltimateFlea"
    New-Item -ItemType Directory -Force $plug | Out-Null
    Copy-Item $clientDll.FullName $plug
}
foreach ($f in "README.md", "CHANGELOG.md", "CHANGELOG.ru.md") {
    $p = Join-Path $root $f
    if (Test-Path $p) { Copy-Item $p $stage } else { Write-Warning "$f missing" }
}

# Never ship debug symbols or backups.
Get-ChildItem $stage -Recurse -File | Where-Object { $_.Extension -in ".pdb", ".bak" } | Remove-Item -Force

$dist = Join-Path $root "dist"
$zip = Join-Path $dist "UltimateFlea-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($zip, "Create")
try {
    Get-ChildItem $stage -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($stage.Length + 1).Replace("\", "/")
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $rel)
    }
} finally { $archive.Dispose() }
Remove-Item $stage -Recurse -Force

Write-Host ""
Write-Host "Created $zip"
$z = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $z.Entries | ForEach-Object { "{0,10}  {1}" -f $_.Length, $_.FullName } | Write-Host
    Write-Host ("{0} files, {1:N0} bytes zipped" -f $z.Entries.Count, (Get-Item $zip).Length)
} finally { $z.Dispose() }
if (-not $clientDll) { Write-Warning "Zip is SERVER-ONLY: client plugin missing." }
