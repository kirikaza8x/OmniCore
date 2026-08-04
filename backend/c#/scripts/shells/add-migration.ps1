param(
    [string]$ModuleName,
    [string]$MigrationName,
    [switch]$SkipUpdate
)

$ErrorActionPreference = "Stop"

# Resolve backend root relative to scripts/shells/
$ScriptDir  = $PSScriptRoot
$BackendDir = (Resolve-Path (Join-Path $ScriptDir "../..")).Path

# Force @() array evaluation even when only 1 infrastructure project exists
$AllInfraProjects = @(Get-ChildItem -Path $BackendDir -Filter "OmniCore.Services.*.Infrastructure.csproj" -Recurse)

if ($AllInfraProjects.Count -eq 0) {
    Write-Host "[X] No service infrastructure projects found in $BackendDir." -ForegroundColor Red
    exit 1
}

# ── Module Selection ──────────────────────────────────────────────────────────
if (-not $ModuleName) {
    Write-Host "`nAvailable OmniCore Services:" -ForegroundColor Cyan
    # Force @() array so string indexing doesn't slice characters when count == 1
    $modules = @($AllInfraProjects | ForEach-Object { $_.Name.Replace("OmniCore.Services.", "").Replace(".Infrastructure.csproj", "") } | Sort-Object -Unique)
    
    $i = 1
    foreach ($m in $modules) {
        Write-Host "  [$i] $m"
        $i++
    }

    $pick = Read-Host "`nSelect service number (or type name directly)"
    $ModuleName = if ($pick -match '^\d+$') { $modules[$([int]$pick - 1)] } else { $pick }
}

# Normalize module name (e.g. "auth" -> "Auth")
$ModuleName = (Get-Culture).TextInfo.ToTitleCase($ModuleName.ToLower())
$ServiceName = "OmniCore.Services.$ModuleName"

$InfraProj = $AllInfraProjects | Where-Object { $_.Directory.Name -eq "$ServiceName.Infrastructure" } | Select-Object -First 1

if (-not $InfraProj) {
    Write-Host "[X] No Infrastructure project found for module '$ServiceName'." -ForegroundColor Red
    exit 1
}

# Auto-detect matching API Startup Project
$ApiProjPath = Join-Path $BackendDir "$ServiceName\$ServiceName.Api\$ServiceName.Api.csproj"
if (-not (Test-Path $ApiProjPath)) {
    Write-Host "[X] API project not found at $ApiProjPath." -ForegroundColor Red
    exit 1
}

# ── DbContext Auto-Detection ──────────────────────────────────────────────────
# Force @() array evaluation
$ContextFiles = @(Get-ChildItem -Path $InfraProj.Directory.FullName -Filter "*Context.cs" -Recurse |
                Where-Object { $_.Name -notmatch "Factory" })

if ($ContextFiles.Count -eq 0) {
    Write-Host "[X] No DbContext found in $ServiceName.Infrastructure." -ForegroundColor Red
    exit 1
}

$DbContextName = if ($ContextFiles.Count -eq 1) {
    $ContextFiles[0].BaseName
} else {
    Write-Host "`nMultiple DbContexts found:" -ForegroundColor Cyan
    $i = 1
    foreach ($f in $ContextFiles) { Write-Host "  [$i] $($f.BaseName)"; $i++ }
    $pick = Read-Host "Select DbContext number"
    $ContextFiles[$([int]$pick - 1)].BaseName
}

# ── Migration Naming & Timestamping ───────────────────────────────────────────
$Timestamp = Get-Date -Format "yyyyMMddHHmmss"

if (-not $MigrationName) {
    Write-Host "`nMigration Naming:" -ForegroundColor Cyan
    Write-Host "  [1] Auto timestamp  -> migrate_auto_$Timestamp"
    Write-Host "  [2] Custom name"
    $choice = Read-Host "Choice"

    if ($choice -eq "2") {
        $custom        = Read-Host "Enter migration name"
        $MigrationName = "${custom}_$Timestamp"
    } else {
        $MigrationName = "migrate_auto_$Timestamp"
    }
} else {
    $MigrationName = "${MigrationName}_$Timestamp"
}

# ── Summary & Execution ───────────────────────────────────────────────────────
Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "  Module          : $ModuleName"
Write-Host "  DbContext       : $DbContextName"
Write-Host "  Migration Name  : $MigrationName"
Write-Host "  Infra Project   : $($InfraProj.Name)"
Write-Host "  Startup Project : $(Split-Path $ApiProjPath -Leaf)"
Write-Host "  Output Directory: Persistence/Migrations"
Write-Host "==================================================`n" -ForegroundColor Cyan

$confirm = Read-Host "Proceed? (y/n)"
if ($confirm -ne 'y') { Write-Host "Aborted." -ForegroundColor Yellow; exit 0 }

Write-Host "`n[>] Adding migration..." -ForegroundColor Cyan
dotnet ef migrations add $MigrationName `
    --project "$($InfraProj.FullName)" `
    --startup-project "$ApiProjPath" `
    --context "$DbContextName" `
    --output-dir "Persistence/Migrations"

if ($LASTEXITCODE -ne 0) { Write-Host "`n[X] Migration generation failed." -ForegroundColor Red; exit 1 }

if (-not $SkipUpdate) {
    Write-Host "`n[>] Updating database..." -ForegroundColor Cyan
    dotnet ef database update `
        --project "$($InfraProj.FullName)" `
        --startup-project "$ApiProjPath" `
        --context "$DbContextName"

    if ($LASTEXITCODE -ne 0) { Write-Host "`n[X] Database update failed." -ForegroundColor Red; exit 1 }
    Write-Host "`n[OK] '$MigrationName' generated and applied successfully." -ForegroundColor Green
} else {
    Write-Host "`n[OK] '$MigrationName' generated." -ForegroundColor Green
}