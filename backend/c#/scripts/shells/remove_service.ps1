<#
.SYNOPSIS
    Automated service teardown tool for OmniCore microservices.
    Unlinks service projects from the solution file and completely deletes the folder structure.
.PARAMETER ModuleName
    Name of the microservice to remove (e.g., Auth, User, Order, Dummy).
#>
param(
    [string]$ModuleName
)

# Prompt if parameter is missing
if ([string]::IsNullOrWhiteSpace($ModuleName)) { 
    $ModuleName = Read-Host "Enter Service/Module Name to remove (e.g., Auth, Dummy)" 
}

if ([string]::IsNullOrWhiteSpace($ModuleName)) {
    Write-Host "[X] Service name cannot be empty. Aborting." -ForegroundColor Red
    exit 1
}

# Normalize service name
$ModuleName = (Get-Culture).TextInfo.ToTitleCase($ModuleName.ToLower())

# Resolve backend root path relative to script location
$ScriptDir  = $PSScriptRoot
$BackendDir = (Resolve-Path (Join-Path $ScriptDir "../..")).Path

$ServiceName = "OmniCore.Services.$ModuleName"
$ServiceRoot = Join-Path $BackendDir $ServiceName

# Auto-detect Solution File (.slnx or .sln)
$SolutionFile = Get-ChildItem -Path $BackendDir -Filter "OmniCore.sln*" | Select-Object -First 1

if (-not $SolutionFile) {
    Write-Host "[X] Could not locate OmniCore.slnx or OmniCore.sln in $BackendDir" -ForegroundColor Red
    exit 1
}
$SolutionPath = $SolutionFile.FullName

# Check if target directory exists
if (-not (Test-Path $ServiceRoot)) {
    Write-Host "`n[X] Service directory not found: $ServiceRoot" -ForegroundColor Red
    Write-Host "Nothing to remove." -ForegroundColor Yellow
    exit 1
}

# Confirmation Prompt
Write-Host "`n=====================================================" -ForegroundColor Red
Write-Host " [!] WARNING: DESTROYING SERVICE: $ServiceName" -ForegroundColor White
Write-Host " > Target Path : $ServiceRoot" -ForegroundColor DarkGray
Write-Host " > Solution    : $($SolutionFile.Name)" -ForegroundColor DarkGray
Write-Host "=====================================================`n" -ForegroundColor Red

$confirm = Read-Host "Are you sure you want to PERMANENTLY delete $ServiceName? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "`n[X] Operation cancelled by user." -ForegroundColor Yellow
    exit 0
}

# Shut down active MSBuild / Roslyn compiler daemons holding bin/obj locks
Write-Host "`n[1/3] Releasing background process file locks..." -ForegroundColor Cyan
dotnet build-server shutdown | Out-Null

# ------------------------------------------------------------------------------
# 2. Unregister Projects from Solution
# ------------------------------------------------------------------------------
Write-Host '[2/3] Unlinking projects from solution...' -ForegroundColor Cyan

$projects = Get-ChildItem -Path $ServiceRoot -Filter "*.csproj" -Recurse
if ($projects) {
    foreach ($proj in $projects) {
        Write-Host " > Removing from solution: $($proj.Name)" -ForegroundColor DarkGray
        dotnet sln "$SolutionPath" remove "$($proj.FullName)" | Out-Null
    }
} else {
    Write-Host " > No .csproj files found inside service folder." -ForegroundColor Yellow
}

# ------------------------------------------------------------------------------
# 3. Delete Folder Hierarchy with Retry Logic
# ------------------------------------------------------------------------------
Write-Host '[3/3] Deleting folder hierarchy...' -ForegroundColor Cyan

$deleted = $false
for ($i = 1; $i -le 3; $i++) {
    try {
        Remove-Item -Path $ServiceRoot -Recurse -Force -ErrorAction Stop
        $deleted = $true
        break
    } catch {
        Start-Sleep -Milliseconds 500
    }
}

if (-not $deleted) {
    # Fallback force attempt
    Remove-Item -Path $ServiceRoot -Recurse -Force -ErrorAction SilentlyContinue
}

# ------------------------------------------------------------------------------
# Done!
# ------------------------------------------------------------------------------
if (-not (Test-Path $ServiceRoot)) {
    Write-Host "`n[OK] Service '$ServiceName' and all associated projects removed!" -ForegroundColor Green
} else {
    Write-Host "`n[!] Folder partially deleted. VS Code extension is locking files in 'obj'. Reload VS Code window to unlock." -ForegroundColor Yellow
}