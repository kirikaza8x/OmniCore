# ==============================================================================
# OmniCore Developer Task Launcher
# ==============================================================================

param(
    [string]$Script,                                  # run a script by (partial) name without the menu, e.g. -Script migration
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ScriptArgs                             # everything after is passed on, e.g. -Script migration -Service User -Action List
)

# Ensure execution policy allows current process script execution
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process -ErrorAction SilentlyContinue

$ScriptFolder = $PSScriptRoot
$SelfName     = $MyInvocation.MyCommand.Name

function Get-ScriptMap {
    # Dynamically find all .ps1 scripts in this folder EXCEPT this launcher script
    $scriptFiles = Get-ChildItem -Path $ScriptFolder -Filter *.ps1 | Where-Object { $_.Name -ne $SelfName }
    $map = @{}
    $index = 1
    foreach ($file in $scriptFiles) {
        $map[$index] = $file.FullName
        $index++
    }
    return $map
}

function Show-Header {
    Write-Host "=====================================================" -ForegroundColor Cyan
    Write-Host "            [+] OMNICORE SCRIPT LAUNCHER             " -ForegroundColor White
    Write-Host "=====================================================" -ForegroundColor Cyan
}

function Show-Menu ($map) {
    Show-Header
    if ($map.Count -eq 0) {
        Write-Host "No runnable scripts found in:" -ForegroundColor Yellow
        Write-Host " -> $ScriptFolder" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  [0] Exit" -ForegroundColor Red
        return
    }

    Write-Host "Available Automation Tools:" -ForegroundColor Yellow
    # Sort keys numerically for display
    foreach ($key in ($map.Keys | Sort-Object)) {
        $fileName = [System.IO.Path]::GetFileName($map[$key])
        Write-Host "  [$key] $fileName" -ForegroundColor Green
    }
    Write-Host ""
    Write-Host "  [0] Exit" -ForegroundColor Red
    Write-Host "=====================================================" -ForegroundColor Cyan
}

if ($Script) {
    $hits = @((Get-ScriptMap).Values | Where-Object { [IO.Path]::GetFileNameWithoutExtension($_) -like "*$Script*" })
    if ($hits.Count -ne 1) {
        Write-Host "[X] '$Script' matches $($hits.Count) scripts. Use a more specific name." -ForegroundColor Red
        exit 1
    }
    # a child process so "-Service User" style named parameters bind correctly
    & (Get-Process -Id $PID).Path -NoProfile -File $hits[0] @ScriptArgs
    exit $LASTEXITCODE
}

do {
    $scriptMap = Get-ScriptMap
    Show-Menu -map $scriptMap
    
    $choice = Read-Host "`nEnter option"
    
    if ($choice -eq "0" -or $choice -eq "exit") {
        Write-Host "`nExiting OmniCore Launcher. Happy coding!" -ForegroundColor Magenta
        break
    }

    [int]$option = 0
    $isValidInt = [int]::TryParse($choice, [ref]$option)

    if (-not $isValidInt -or -not $scriptMap.ContainsKey($option)) {
        Write-Host "`n[X] Invalid selection. Please select a valid number from the menu." -ForegroundColor Red
        Start-Sleep -Seconds 1
        continue
    }

    $selectedScriptPath = $scriptMap[$option]
    $selectedScriptName = [System.IO.Path]::GetFileNameWithoutExtension($selectedScriptPath)

    Write-Host "`n=====================================================" -ForegroundColor DarkGray
    Write-Host "[>] Executing: $selectedScriptName" -ForegroundColor Cyan
    Write-Host "=====================================================`n" -ForegroundColor DarkGray

    # Execute selected script directly
    & $selectedScriptPath

    Write-Host "`n=====================================================" -ForegroundColor DarkGray
    Write-Host "[OK] Task completed: $selectedScriptName" -ForegroundColor Green
    Write-Host "=====================================================" -ForegroundColor DarkGray
    
    Write-Host "`nPress Enter to return to main menu..." -ForegroundColor DarkGray
    $null = Read-Host

} while ($true)