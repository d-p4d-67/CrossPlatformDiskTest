# Run in Windows PowerShell 5.1 or PowerShell 7.
# This script only inspects the machine; it does not install or change anything.
$ErrorActionPreference = 'Continue'
function Show-Status([string]$name, [string]$status, [string]$detail) {
    Write-Host ("{0,-22} {1,-9} {2}" -f $name, $status, $detail)
}

Write-Host 'CPDT TV Edition - Windows Xamarin.Android build diagnostics'
Write-Host ('Windows: ' + [Environment]::OSVersion.VersionString)
Write-Host ''

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path $vswhere) {
    $installations = @(& $vswhere -products '*' -format json | ConvertFrom-Json)
    if ($installations.Count -eq 0) {
        Show-Status 'Visual Studio' 'MISSING' 'No installation registered with vswhere'
    } else {
        foreach ($vs in $installations) {
            Show-Status 'Visual Studio' 'FOUND' ("{0} ({1})" -f $vs.displayName,$vs.installationPath)
        }
    }
} else {
    Show-Status 'Visual Studio' 'UNKNOWN' 'vswhere.exe not found'
}

$targets = @()
if (Test-Path $vswhere) {
    $paths = @(& $vswhere -products '*' -property installationPath)
    foreach ($path in $paths) {
        if ($path -and (Test-Path $path)) {
            $targets += @(Get-ChildItem $path -Filter Xamarin.Android.CSharp.targets -File -Recurse -ErrorAction SilentlyContinue)
        }
    }
}
if ($targets.Count -gt 0) {
    Show-Status 'Xamarin.Android' 'FOUND' $targets[0].FullName
} else {
    Show-Status 'Xamarin.Android' 'MISSING' 'Legacy MSBuild targets not found in Visual Studio'
}

$sdk = $env:ANDROID_HOME
if (-not $sdk) { $sdk = $env:ANDROID_SDK_ROOT }
if (-not $sdk) { $sdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk' }
if (Test-Path $sdk) {
    Show-Status 'Android SDK' 'FOUND' $sdk
    $platform = Join-Path $sdk 'platforms\android-33'
    Show-Status 'Android API 33' $(if (Test-Path $platform) {'FOUND'} else {'MISSING'}) $platform
} else {
    Show-Status 'Android SDK' 'MISSING' $sdk
}

$java = Get-Command java -ErrorAction SilentlyContinue
if ($java) {
    Show-Status 'Java runtime' 'FOUND' $java.Source
    & java -version 2>&1 | Select-Object -First 3 | ForEach-Object { Write-Host "    $_" }
} elseif ($env:JAVA_HOME -and (Test-Path (Join-Path $env:JAVA_HOME 'bin\java.exe'))) {
    Show-Status 'Java runtime' 'FOUND' $env:JAVA_HOME
} else {
    Show-Status 'Java runtime' 'MISSING' 'java not on PATH; check JAVA_HOME'
}

foreach ($cmd in @('git','nuget','msbuild')) {
    $item = Get-Command $cmd -ErrorAction SilentlyContinue
    Show-Status $cmd $(if ($item) {'FOUND'} else {'MISSING'}) $(if ($item) {$item.Source} else {'not on PATH'})
}
Write-Host ''
Write-Host 'This checks tool availability only; it does NOT confirm CPDT APK compatibility.'
