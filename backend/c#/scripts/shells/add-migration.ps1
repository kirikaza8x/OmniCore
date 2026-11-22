<#
.SYNOPSIS
  EF Core migrations for any OmniCore service (add / remove / update / list / script).

.EXAMPLES
  .\add-migration.ps1                                   # interactive, as before
  .\add-migration.ps1 -Service User -Name AddBio -Yes   # no prompts (add, then update the database)
  .\add-migration.ps1 -Service Auth -Name Fix -NoUpdate -NoTimestamp
  .\add-migration.ps1 -Service User -Action List
  .\add-migration.ps1 -Service User -Action Remove -Yes # undo the last migration (not yet applied)
  .\add-migration.ps1 -Service User -Action Update -Target InitialUser
  .\add-migration.ps1 -Service User -Action Script -Output .\user.sql   # idempotent SQL script
  .\add-migration.ps1 -Service User -Name X -Connection "Host=localhost;Database=users;Username=postgres;Password=CHANGE_ME"
#>
param(
    [Alias('ModuleName')][string]$Service,
    [Alias('MigrationName')][string]$Name,
    [ValidateSet('Add', 'Remove', 'Update', 'List', 'Script')][string]$Action = 'Add',
    [string]$Context,                 # DbContext class name when a service has several
    [string]$OutputDir = 'Persistence/Migrations',
    [string]$Connection,              # sets Database__ConnectionString for this run only (the services read env vars)
    [string]$Target,                  # Update: migration to move to ('0' reverts everything)
    [string]$Output,                  # Script: output file (default: print to console)
    [switch]$NoTimestamp,             # EF already prefixes its own timestamp; the old script added a second one
    [Alias('SkipUpdate')][switch]$NoUpdate,
    [switch]$Yes                      # do not ask for confirmation
)

$ErrorActionPreference = 'Stop'
$BackendDir = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Fail($msg) { Write-Host "[X] $msg" -ForegroundColor Red; exit 1 }
function Pick($title, [string[]]$items) {
    if ($items.Count -eq 1) { return $items[0] }
    Write-Host "`n$title" -ForegroundColor Cyan
    for ($i = 0; $i -lt $items.Count; $i++) { Write-Host "  [$($i + 1)] $($items[$i])" }
    $p = Read-Host 'Select number (or type the name)'
    if ($p -match '^\d+$' -and [int]$p -ge 1 -and [int]$p -le $items.Count) { return $items[[int]$p - 1] }
    return $p
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Fail 'dotnet SDK not found on PATH.' }
if (-not (& dotnet tool list -g 2>$null | Select-String -Pattern '^dotnet-ef\s')) {
    Write-Host '[!] dotnet-ef is not installed globally. Install: dotnet tool install --global dotnet-ef --version 9.0.9' -ForegroundColor Yellow
}

# ---- service ---------------------------------------------------------------
$infra = @(Get-ChildItem -Path $BackendDir -Filter 'OmniCore.Services.*.Infrastructure.csproj' -Recurse |
           Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
if ($infra.Count -eq 0) { Fail "No service Infrastructure projects found under $BackendDir." }

$services = @($infra | ForEach-Object { $_.Name -replace '^OmniCore\.Services\.', '' -replace '\.Infrastructure\.csproj$', '' } | Sort-Object -Unique)
if (-not $Service) { $Service = Pick 'Available OmniCore services:' $services }

# case-insensitive match, no ToTitleCase (it mangled names like "UserProfile")
$Service = $services | Where-Object { $_ -ieq $Service } | Select-Object -First 1
if (-not $Service) { Fail "Unknown service. Known: $($services -join ', ')" }

$ServiceName = "OmniCore.Services.$Service"
$InfraProj   = $infra | Where-Object { $_.Name -eq "$ServiceName.Infrastructure.csproj" } | Select-Object -First 1
$ApiProj     = Join-Path $BackendDir "$ServiceName\$ServiceName.Api\$ServiceName.Api.csproj"
if (-not (Test-Path $ApiProj)) { Fail "API (startup) project not found: $ApiProj" }

# ---- DbContext -------------------------------------------------------------
if (-not $Context) {
    $ctx = @(Get-ChildItem -Path $InfraProj.Directory.FullName -Filter '*Context.cs' -Recurse |
             Where-Object { $_.Name -notmatch 'Factory' -and $_.FullName -notmatch '[\\/](bin|obj|Migrations)[\\/]' } |
             ForEach-Object { $_.BaseName })
    if ($ctx.Count -eq 0) { Fail "No DbContext found in $ServiceName.Infrastructure." }
    $Context = Pick 'Multiple DbContexts found:' $ctx
}

# ---- connection override (env var lives only in this process) ----------------
if ($Connection) { $env:Database__ConnectionString = $Connection }

$common = @('--project', $InfraProj.FullName, '--startup-project', $ApiProj, '--context', $Context)

function Run-Ef([string[]]$efArgs) {
    Write-Host "`n[>] dotnet ef $($efArgs -join ' ')" -ForegroundColor Cyan
    & dotnet ef @efArgs
    if ($LASTEXITCODE -ne 0) { Fail "dotnet ef failed (exit $LASTEXITCODE)." }
}
function Confirm-Or-Exit([string]$what) {
    if ($Yes) { return }
    if ((Read-Host "$what Proceed? (y/n)") -ne 'y') { Write-Host 'Aborted.' -ForegroundColor Yellow; exit 0 }
}
function Show-Summary($extra) {
    Write-Host "`n==================================================" -ForegroundColor Cyan
    Write-Host "  Action          : $Action"
    Write-Host "  Service         : $Service"
    Write-Host "  DbContext       : $Context"
    Write-Host "  Infra project   : $($InfraProj.Name)"
    Write-Host "  Startup project : $(Split-Path $ApiProj -Leaf)"
    foreach ($e in $extra) { Write-Host "  $e" }
    Write-Host "==================================================" -ForegroundColor Cyan
}

switch ($Action) {
    'List' {
        Run-Ef (@('migrations', 'list') + $common)
    }
    'Add' {
        if (-not $Name) {
            $Name = Read-Host 'Migration name (e.g. AddUserBio; empty = Auto)'
            if (-not $Name) { $Name = 'Auto' }
        }
        if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') { Fail "Migration name '$Name' must be a valid C# identifier." }
        if (-not $NoTimestamp) { $Name = "${Name}_$(Get-Date -Format 'yyyyMMddHHmmss')" }
        Show-Summary @("Migration       : $Name", "Output dir      : $OutputDir", "Update database : $(-not $NoUpdate)")
        Confirm-Or-Exit ''
        Run-Ef (@('migrations', 'add', $Name, '--output-dir', $OutputDir) + $common)
        if ($NoUpdate) { Write-Host "`n[OK] '$Name' generated." -ForegroundColor Green }
        else {
            Run-Ef (@('database', 'update') + $common)
            Write-Host "`n[OK] '$Name' generated and applied." -ForegroundColor Green
        }
    }
    'Remove' {
        Show-Summary @('Removes the LAST migration (must not be applied to the database).')
        Confirm-Or-Exit ''
        Run-Ef (@('migrations', 'remove') + $common)
        Write-Host '[OK] Last migration removed.' -ForegroundColor Green
    }
    'Update' {
        Show-Summary @("Target          : $(if ($Target) { $Target } else { '(latest)' })")
        Confirm-Or-Exit ''
        $targetArg = if ($Target) { @($Target) } else { @() }
        Run-Ef (@('database', 'update') + $targetArg + $common)
        Write-Host '[OK] Database updated.' -ForegroundColor Green
    }
    'Script' {
        $out = if ($Output) { @('--output', $Output) } else { @() }
        Run-Ef (@('migrations', 'script', '--idempotent') + $out + $common)
    }
}
