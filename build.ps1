[CmdletBinding(PositionalBinding = $false)]
param(
    [bool] $CreatePackages = $true
)

Write-Host "Run Parameters:" -ForegroundColor Cyan
Write-Host "  CreatePackages: $CreatePackages"
Write-Host "  dotnet --version:" (dotnet --version)

$packageOutputFolder = "$PSScriptRoot\.nupkgs"
$projectsToBuild =
    'SkyBuilding',
    'SkyBuilding.Defaults',
    'SkyBuilding.Mvc',
    'SkyBuilding.ORM',
    'SkyBuilding.MySql',
    'SkyBuilding.SqlServer',
    'SkyBuilding.Dapper'

mkdir -Force $packageOutputFolder | Out-Null
Write-Host "Clearing existing $packageOutputFolder..." -NoNewline
Get-ChildItem $packageOutputFolder | Remove-Item
Write-Host "done." -ForegroundColor "Green"

Write-Host "Building all packages" -ForegroundColor "Green"
if ($CreatePackages) {
	foreach ($project in $projectsToBuild) {
		Write-Host "Packing $project (dotnet pack)..." -ForegroundColor "Magenta"
		dotnet pack ".\src\$project\$project.csproj" --no-build -c $configuration /p:PackageOutputPath=$packageOutputFolder #/p:NoPackageAnalysis=true /p:CI=true
		dotnet pack ".\src\$project\$project.csproj" --no-build -c $configuration /p:PackageOutputPath=$packageOutputFolder /p:NoPackageAnalysis=true #/p:CI=true
		dotnet pack ".\src\$project\$project.csproj" --no-build -c $configuration /p:PackageOutputPath=$packageOutputFolder /p:NoPackageAnalysis=true /p:CI=true
		Write-Host ""
	}
}
Write-Host "Build Complete." -ForegroundColor "Green"