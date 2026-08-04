<#
.SYNOPSIS
    Automated service builder for OmniCore microservices.
    Scaffolds Clean Architecture projects, links OmniCore.Shared layers by default,
    installs layer-flexible NuGet packages, generates markers, and updates the solution file.
.PARAMETER ModuleName
    Name of the microservice (e.g., Auth, User, Order, Payment).
.PARAMETER TargetFramework
    Target framework moniker (default: net9.0).
.PARAMETER ContractsPackages
    Array of NuGet package names to install into the Contracts project.
.PARAMETER DomainPackages
    Array of NuGet package names to install into the Domain project.
.PARAMETER AppPackages
    Array of NuGet package names to install into the Application project.
.PARAMETER InfraPackages
    Array of NuGet package names to install into the Infrastructure project.
.PARAMETER ApiPackages
    Array of NuGet package names to install into the API project.
#>
param(
    [string]$ModuleName,
    [string]$TargetFramework = "net9.0",
    [string[]]$ContractsPackages = @(),
    [string[]]$DomainPackages    = @(),
    [string[]]$AppPackages       = @(),
    [string[]]$InfraPackages     = @(
        "Npgsql.EntityFrameworkCore.PostgreSQL",
        "Microsoft.EntityFrameworkCore.Tools"
    ),
    [string[]]$ApiPackages       = @(
        "Microsoft.EntityFrameworkCore.Design",
        "Carter"
    )
)

# Prompt if parameter is missing
if ([string]::IsNullOrWhiteSpace($ModuleName)) { 
    $ModuleName = Read-Host "Enter Service/Module Name (e.g., Auth, User, Order)" 
}

if ([string]::IsNullOrWhiteSpace($ModuleName)) {
    Write-Host "[X] Service name cannot be empty. Aborting." -ForegroundColor Red
    exit 1
}

# Normalize service name (e.g., "auth" -> "Auth")
$ModuleName = (Get-Culture).TextInfo.ToTitleCase($ModuleName.ToLower())

# Resolve backend root path relative to script location
$ScriptDir  = $PSScriptRoot
$BackendDir = (Resolve-Path (Join-Path $ScriptDir "../..")).Path

$ServiceName = "OmniCore.Services.$ModuleName"
$ServiceRoot = Join-Path $BackendDir $ServiceName
$SharedDir   = Join-Path $BackendDir "OmniCore.Shared"

# Auto-detect Solution File (.slnx or .sln)
$SolutionFile = Get-ChildItem -Path $BackendDir -Filter "OmniCore.sln*" | Select-Object -First 1

if (-not $SolutionFile) {
    Write-Host "[X] Could not locate OmniCore.slnx or OmniCore.sln in $BackendDir" -ForegroundColor Red
    exit 1
}
$SolutionPath = $SolutionFile.FullName

# Safeguard against overwriting
if (Test-Path $ServiceRoot) {
    Write-Host "`n[X] Directory already exists: $ServiceRoot" -ForegroundColor Red
    Write-Host "Aborting to prevent overwriting existing service files." -ForegroundColor Yellow
    exit 1
}

Write-Host "`n=====================================================" -ForegroundColor Cyan
Write-Host " [+] Building OmniCore Service: $ServiceName" -ForegroundColor White
Write-Host " > Target Path     : $ServiceRoot" -ForegroundColor DarkGray
Write-Host " > Target Framework: $TargetFramework" -ForegroundColor DarkGray
Write-Host " > Solution        : $($SolutionFile.Name)" -ForegroundColor DarkGray
Write-Host "=====================================================`n" -ForegroundColor Cyan

# ------------------------------------------------------------------------------
# 1. Create Folder Hierarchy
# ------------------------------------------------------------------------------
Write-Host '[1/6] Creating folder structure...' -ForegroundColor Cyan
$ApiDir       = Join-Path $ServiceRoot "$ServiceName.Api"
$AppDir       = Join-Path $ServiceRoot "$ServiceName.Application"
$DomainDir    = Join-Path $ServiceRoot "$ServiceName.Domain"
$InfraDir     = Join-Path $ServiceRoot "$ServiceName.Infrastructure"
$ContractsDir = Join-Path $ServiceRoot "$ServiceName.Contracts"

New-Item -ItemType Directory -Force -Path $ApiDir, $AppDir, $DomainDir, $InfraDir, $ContractsDir | Out-Null

# ------------------------------------------------------------------------------
# 2. Create Projects
# ------------------------------------------------------------------------------
Write-Host "[2/6] Initializing $TargetFramework projects..." -ForegroundColor Cyan

dotnet new web -n "$ServiceName.Api"            -o $ApiDir       -f $TargetFramework --no-https | Out-Null
dotnet new classlib -n "$ServiceName.Contracts"      -o $ContractsDir -f $TargetFramework            | Out-Null
dotnet new classlib -n "$ServiceName.Domain"         -o $DomainDir    -f $TargetFramework            | Out-Null
dotnet new classlib -n "$ServiceName.Application"    -o $AppDir       -f $TargetFramework            | Out-Null
dotnet new classlib -n "$ServiceName.Infrastructure" -o $InfraDir     -f $TargetFramework            | Out-Null

# Clean up default Class1.cs files
Get-ChildItem -Path $ServiceRoot -Filter "Class1.cs" -Recurse | Remove-Item -Force

# ------------------------------------------------------------------------------
# 3. Wire Up Internal Clean Architecture & Shared Kernel References
# ------------------------------------------------------------------------------
Write-Host '[3/6] Wiring Clean Architecture and Shared Kernel project references...' -ForegroundColor Cyan

# Service-Internal Layer Dependencies
dotnet add (Join-Path $AppDir "$ServiceName.Application.csproj") reference (Join-Path $DomainDir "$ServiceName.Domain.csproj")       | Out-Null
dotnet add (Join-Path $AppDir "$ServiceName.Application.csproj") reference (Join-Path $ContractsDir "$ServiceName.Contracts.csproj") | Out-Null

dotnet add (Join-Path $InfraDir "$ServiceName.Infrastructure.csproj") reference (Join-Path $AppDir "$ServiceName.Application.csproj") | Out-Null

dotnet add (Join-Path $ApiDir "$ServiceName.Api.csproj") reference (Join-Path $InfraDir "$ServiceName.Infrastructure.csproj") | Out-Null
dotnet add (Join-Path $ApiDir "$ServiceName.Api.csproj") reference (Join-Path $AppDir "$ServiceName.Application.csproj")     | Out-Null

# Default Reference Mapping from OmniCore.Shared Kernel Layers
$SharedDomainProj = Join-Path $SharedDir "OmniCore.Shared.Domain/OmniCore.Shared.Domain.csproj"
$SharedAppProj    = Join-Path $SharedDir "OmniCore.Shared.Application/OmniCore.Shared.Application.csproj"
$SharedInfraProj  = Join-Path $SharedDir "OmniCore.Shared.Infrastructure/OmniCore.Shared.Infrastructure.csproj"
$SharedApiProj    = Join-Path $SharedDir "OmniCore.Shared.Api/OmniCore.Shared.Api.csproj"

if (Test-Path $SharedDomainProj) {
    dotnet add (Join-Path $DomainDir "$ServiceName.Domain.csproj") reference $SharedDomainProj | Out-Null
}

if (Test-Path $SharedAppProj) {
    dotnet add (Join-Path $AppDir "$ServiceName.Application.csproj") reference $SharedAppProj | Out-Null
}

if (Test-Path $SharedInfraProj) {
    dotnet add (Join-Path $InfraDir "$ServiceName.Infrastructure.csproj") reference $SharedInfraProj | Out-Null
}

if (Test-Path $SharedApiProj) {
    dotnet add (Join-Path $ApiDir "$ServiceName.Api.csproj") reference $SharedApiProj | Out-Null
}

# ------------------------------------------------------------------------------
# 4. Install Configured NuGet Packages Across All Layers Flexibly
# ------------------------------------------------------------------------------
Write-Host '[4/6] Installing layer NuGet packages...' -ForegroundColor Cyan

$LayerPackageMap = [ordered]@{
    "Contracts"      = @{ Path = $ContractsDir; Packages = $ContractsPackages }
    "Domain"         = @{ Path = $DomainDir;    Packages = $DomainPackages }
    "Application"    = @{ Path = $AppDir;       Packages = $AppPackages }
    "Infrastructure" = @{ Path = $InfraDir;     Packages = $InfraPackages }
    "Api"            = @{ Path = $ApiDir;       Packages = $ApiPackages }
}

foreach ($layer in $LayerPackageMap.Keys) {
    $dir = $LayerPackageMap[$layer].Path
    $pkgs = $LayerPackageMap[$layer].Packages
    $projFile = Join-Path $dir "$ServiceName.$layer.csproj"

    foreach ($pkg in $pkgs) {
        Write-Host "  > Installing $pkg -> $($ServiceName).$layer" -ForegroundColor DarkGray
        dotnet add $projFile package $pkg | Out-Null
    }
}

# ------------------------------------------------------------------------------
# 5. Generate AssemblyReference Markers & Program.cs
# ------------------------------------------------------------------------------
Write-Host '[5/6] Writing AssemblyReference markers and starter host...' -ForegroundColor Cyan

# Contracts AssemblyReference.cs
Set-Content -Path (Join-Path $ContractsDir "AssemblyReference.cs") -Value @(
    "namespace ${ServiceName}.Contracts;",
    "",
    "public static class ContractsAssemblyReference",
    "{",
    "    public static readonly System.Reflection.Assembly Assembly = typeof(ContractsAssemblyReference).Assembly;",
    "}"
)

# Domain AssemblyReference.cs
Set-Content -Path (Join-Path $DomainDir "AssemblyReference.cs") -Value @(
    "namespace ${ServiceName}.Domain;",
    "",
    "public static class DomainAssemblyReference",
    "{",
    "    public static readonly System.Reflection.Assembly Assembly = typeof(DomainAssemblyReference).Assembly;",
    "}"
)

# Application AssemblyReference.cs
Set-Content -Path (Join-Path $AppDir "AssemblyReference.cs") -Value @(
    "namespace ${ServiceName}.Application;",
    "",
    "public static class ApplicationAssemblyReference",
    "{",
    "    public static readonly System.Reflection.Assembly Assembly = typeof(ApplicationAssemblyReference).Assembly;",
    "}"
)

# Infrastructure AssemblyReference.cs
Set-Content -Path (Join-Path $InfraDir "AssemblyReference.cs") -Value @(
    "namespace ${ServiceName}.Infrastructure;",
    "",
    "public static class InfrastructureAssemblyReference",
    "{",
    "    public static readonly System.Reflection.Assembly Assembly = typeof(InfrastructureAssemblyReference).Assembly;",
    "}"
)

# Api AssemblyReference.cs
Set-Content -Path (Join-Path $ApiDir "AssemblyReference.cs") -Value @(
    "namespace ${ServiceName}.Api;",
    "",
    "public static class ApiAssemblyReference",
    "{",
    "    public static readonly System.Reflection.Assembly Assembly = typeof(ApiAssemblyReference).Assembly;",
    "}"
)

# Starter Program.cs configured with OmniCore.Shared.Api
Set-Content -Path (Join-Path $ApiDir "Program.cs") -Value @(
    "using Carter;",
    "using OmniCore.Shared.Api;",
    "using ${ServiceName}.Api;",
    "using ${ServiceName}.Application;",
    "",
    "var builder = WebApplication.CreateBuilder(args);",
    "",
    "// Register Shared API Kernel (Carter, Auth, Rate Limiting, CORS)",
    "builder.Services.AddApi(",
    "    new[]",
    "    {",
    "        ApiAssemblyReference.Assembly,",
    "        ApplicationAssemblyReference.Assembly",
    "    },",
    "    builder.Configuration",
    ");",
    "",
    "var app = builder.Build();",
    "",
    "app.UseCors();",
    "app.UseRateLimiter();",
    "app.UseAuthentication();",
    "app.UseAuthorization();",
    "",
    "app.UseApi();",
    "app.MapCarter();",
    "",
    "app.Run();"
)

# ------------------------------------------------------------------------------
# 6. Add Projects to Solution
# ------------------------------------------------------------------------------
Write-Host '[6/6] Registering projects in solution file...' -ForegroundColor Cyan

dotnet sln "$SolutionPath" add (Join-Path $ContractsDir "$ServiceName.Contracts.csproj") | Out-Null
dotnet sln "$SolutionPath" add (Join-Path $DomainDir "$ServiceName.Domain.csproj")       | Out-Null
dotnet sln "$SolutionPath" add (Join-Path $AppDir "$ServiceName.Application.csproj")     | Out-Null
dotnet sln "$SolutionPath" add (Join-Path $InfraDir "$ServiceName.Infrastructure.csproj")| Out-Null
dotnet sln "$SolutionPath" add (Join-Path $ApiDir "$ServiceName.Api.csproj")             | Out-Null

# ------------------------------------------------------------------------------
# Done!
# ------------------------------------------------------------------------------
Write-Host "`n[OK] Service '$ServiceName' ($TargetFramework) created successfully!" -ForegroundColor Green
Write-Host "Next step: Build the solution via 'dotnet build $SolutionPath'" -ForegroundColor Yellow