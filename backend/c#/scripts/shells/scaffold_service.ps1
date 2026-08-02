<#
.SYNOPSIS
    Automated service builder for OmniCore microservices.
    Scaffolds Clean Architecture projects, links OmniCore.Shared layers by default,
    generates markers, and updates the solution file.
.PARAMETER ModuleName
    Name of the microservice (e.g., Auth, User, Order, Payment).
#>
param(
    [string]$ModuleName
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

# Resolve backend root path relative to script location (scripts/powershell -> backend/c#)
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
Write-Host " 🚀 Building OmniCore Service: $ServiceName" -ForegroundColor White
Write-Host " 📁 Target Path : $ServiceRoot" -ForegroundColor DarkGray
Write-Host " 📄 Solution    : $($SolutionFile.Name)" -ForegroundColor DarkGray
Write-Host "=====================================================`n" -ForegroundColor Cyan

# ------------------------------------------------------------------------------
# 1. Create Folder Hierarchy
# ------------------------------------------------------------------------------
Write-Host "[1/5] Creating folder structure..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path `
    "$ServiceRoot/$ServiceName.Api",
    "$ServiceRoot/$ServiceName.Application",
    "$ServiceRoot/$ServiceName.Domain",
    "$ServiceRoot/$ServiceName.Infrastructure",
    "$ServiceRoot/$ServiceName.Contracts" | Out-Null

# ------------------------------------------------------------------------------
# 2. Create .NET 9 Projects
# ------------------------------------------------------------------------------
Write-Host "[2/5] Initializing .NET 9 projects..." -ForegroundColor Cyan

# API Layer (Minimal Web Host)
dotnet new web -n "$ServiceName.Api" -o "$ServiceRoot/$ServiceName.Api" --no-https | Out-Null

# Class Libraries
dotnet new classlib -n "$ServiceName.Contracts"      -o "$ServiceRoot/$ServiceName.Contracts"      | Out-Null
dotnet new classlib -n "$ServiceName.Domain"         -o "$ServiceRoot/$ServiceName.Domain"         | Out-Null
dotnet new classlib -n "$ServiceName.Application"    -o "$ServiceRoot/$ServiceName.Application"    | Out-Null
dotnet new classlib -n "$ServiceName.Infrastructure" -o "$ServiceRoot/$ServiceName.Infrastructure" | Out-Null

# Clean up default Class1.cs files
Get-ChildItem -Path $ServiceRoot -Filter "Class1.cs" -Recurse | Remove-Item -Force

# ------------------------------------------------------------------------------
# 3. Wire Up Internal Clean Architecture & Shared Kernel References
# ------------------------------------------------------------------------------
Write-Host "[3/5] Wiring Clean Architecture & Shared Kernel project references..." -ForegroundColor Cyan

# Service-Internal Layer Dependencies
dotnet add "$ServiceRoot/$ServiceName.Application/$ServiceName.Application.csproj" reference "$ServiceRoot/$ServiceName.Domain/$ServiceName.Domain.csproj"       | Out-Null
dotnet add "$ServiceRoot/$ServiceName.Application/$ServiceName.Application.csproj" reference "$ServiceRoot/$ServiceName.Contracts/$ServiceName.Contracts.csproj" | Out-Null

dotnet add "$ServiceRoot/$ServiceName.Infrastructure/$ServiceName.Infrastructure.csproj" reference "$ServiceRoot/$ServiceName.Application/$ServiceName.Application.csproj" | Out-Null

dotnet add "$ServiceRoot/$ServiceName.Api/$ServiceName.Api.csproj" reference "$ServiceRoot/$ServiceName.Infrastructure/$ServiceName.Infrastructure.csproj" | Out-Null
dotnet add "$ServiceRoot/$ServiceName.Api/$ServiceName.Api.csproj" reference "$ServiceRoot/$ServiceName.Application/$ServiceName.Application.csproj"    | Out-Null

# Default Reference Mapping from OmniCore.Shared Kernel Layers
$SharedDomainProj = Join-Path $SharedDir "OmniCore.Shared.Domain/OmniCore.Shared.Domain.csproj"
$SharedAppProj    = Join-Path $SharedDir "OmniCore.Shared.Application/OmniCore.Shared.Application.csproj"
$SharedInfraProj  = Join-Path $SharedDir "OmniCore.Shared.Infrastructure/OmniCore.Shared.Infrastructure.csproj"
$SharedApiProj    = Join-Path $SharedDir "OmniCore.Shared.Api/OmniCore.Shared.Api.csproj"

if (Test-Path $SharedDomainProj) {
    dotnet add "$ServiceRoot/$ServiceName.Domain/$ServiceName.Domain.csproj" reference "$SharedDomainProj" | Out-Null
}

if (Test-Path $SharedAppProj) {
    dotnet add "$ServiceRoot/$ServiceName.Application/$ServiceName.Application.csproj" reference "$SharedAppProj" | Out-Null
}

if (Test-Path $SharedInfraProj) {
    dotnet add "$ServiceRoot/$ServiceName.Infrastructure/$ServiceName.Infrastructure.csproj" reference "$SharedInfraProj" | Out-Null
}

if (Test-Path $SharedApiProj) {
    dotnet add "$ServiceRoot/$ServiceName.Api/$ServiceName.Api.csproj" reference "$SharedApiProj" | Out-Null
}

# ------------------------------------------------------------------------------
# 4. Generate AssemblyReference Markers & Program.cs
# ------------------------------------------------------------------------------
Write-Host "[4/5] Writing AssemblyReference markers & starter host..." -ForegroundColor Cyan

@"
namespace $ServiceName.Contracts;

public static class ContractsAssemblyReference
{
    public static readonly System.Reflection.Assembly Assembly = typeof(ContractsAssemblyReference).Assembly;
}
"@ | Set-Content "$ServiceRoot/$ServiceName.Contracts/AssemblyReference.cs"

@"
namespace $ServiceName.Domain;

public static class DomainAssemblyReference
{
    public static readonly System.Reflection.Assembly Assembly = typeof(DomainAssemblyReference).Assembly;
}
"@ | Set-Content "$ServiceRoot/$ServiceName.Domain/AssemblyReference.cs"

@"
namespace $ServiceName.Application;

public static class ApplicationAssemblyReference
{
    public static readonly System.Reflection.Assembly Assembly = typeof(ApplicationAssemblyReference).Assembly;
}
"@ | Set-Content "$ServiceRoot/$ServiceName.Application/AssemblyReference.cs"

@"
namespace $ServiceName.Infrastructure;

public static class InfrastructureAssemblyReference
{
    public static readonly System.Reflection.Assembly Assembly = typeof(InfrastructureAssemblyReference).Assembly;
}
"@ | Set-Content "$ServiceRoot/$ServiceName.Infrastructure/AssemblyReference.cs"

@"
namespace $ServiceName.Api;

public static class ApiAssemblyReference
{
    public static readonly System.Reflection.Assembly Assembly = typeof(ApiAssemblyReference).Assembly;
}
"@ | Set-Content "$ServiceRoot/$ServiceName.Api/AssemblyReference.cs"

# Generate starter Program.cs using Carter and Shared Presentation extensions
@"
using Carter;

var builder = WebApplication.CreateBuilder(args);

// Register presentation layer extensions from Shared Kernel
builder.Services.AddPresentationLayer();
builder.Services.AddCarter();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();

app.MapCarter();

app.Run();
"@ | Set-Content "$ServiceRoot/$ServiceName.Api/Program.cs"

# ------------------------------------------------------------------------------
# 5. Add Projects to Solution
# ------------------------------------------------------------------------------
Write-Host "[5/5] Registering projects in solution file..." -ForegroundColor Cyan

dotnet sln "$SolutionPath" add "$ServiceRoot/$ServiceName.Contracts/$ServiceName.Contracts.csproj"           | Out-Null
dotnet sln "$SolutionPath" add "$ServiceRoot/$ServiceName.Domain/$ServiceName.Domain.csproj"                  | Out-Null
dotnet sln "$SolutionPath" add "$ServiceRoot/$ServiceName.Application/$ServiceName.Application.csproj"     | Out-Null
dotnet sln "$SolutionPath" add "$ServiceRoot/$ServiceName.Infrastructure/$ServiceName.Infrastructure.csproj"  | Out-Null
dotnet sln "$SolutionPath" add "$ServiceRoot/$ServiceName.Api/$ServiceName.Api.csproj"                     | Out-Null

# ------------------------------------------------------------------------------
# Done!
# ------------------------------------------------------------------------------
Write-Host "`n[OK] Service '$ServiceName' created successfully!" -ForegroundColor Green
Write-Host "Next step: Build the solution via 'dotnet build $SolutionPath'" -ForegroundColor Yellow