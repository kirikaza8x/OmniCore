<#
.SYNOPSIS
    Scaffold a new OmniCore service from the templates (backend/c#/scripts/templates/service).
.DESCRIPTION
    Thin wrapper around scaffold.py (the renderer; needs Python 3.8+). It writes the five service projects and three
    test projects following the Auth/User pattern, then patches OmniCore.slnx, init-dbs.sql, .env.example and the CI
    test loops. The compose and nginx snippets are printed, not applied. See docs/new-service.md.
    UNVERIFIED: this wrapper has not been run (no pwsh in the agent sandbox); scaffold.py itself was run.
.EXAMPLE
    pwsh scaffold_service.ps1 -Name Notification -Route notifications -Port 5003 -Consumes Auth
#>
param(
    [Parameter(Mandatory = $true)][string]$Name,
    [string]$Route,
    [string]$Schema,
    [int]$Port = 5099,
    [string[]]$Consumes = @(),
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$py = (Get-Command python3 -ErrorAction SilentlyContinue) ?? (Get-Command python -ErrorAction SilentlyContinue)
if (-not $py) { throw 'Python 3 is required (scaffold.py).' }

$script = Join-Path $PSScriptRoot '..' 'scaffold.py'
$argsList = @($script, $Name, '--port', $Port)
if ($Route)    { $argsList += @('--route', $Route) }
if ($Schema)   { $argsList += @('--schema', $Schema) }
if ($Consumes) { $argsList += '--consumes'; $argsList += $Consumes }
if ($DryRun)   { $argsList += '--dry-run' }

& $py.Source @argsList
exit $LASTEXITCODE
