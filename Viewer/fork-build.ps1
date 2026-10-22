# Fork: builds the viewer into one folder.
#   <Output>\                 KCViewer.exe, KCBrowser.exe and their files
# The plugins build on their own into <Output>\Plugins\ (Plugins\build.ps1 next to them).
# Default output is KCViewer\bin\dev (kept apart from bin\<Platform>\Release, which may be running).
param(
	[string]$Output = (Join-Path $PSScriptRoot 'KCViewer\bin\dev'),
	[string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$out = [IO.Path]::GetFullPath($Output).TrimEnd('\') + '\'

function Build([string]$project, [string[]]$properties) {
	Write-Host "== $([IO.Path]::GetFileNameWithoutExtension($project))"
	dotnet build $project -c $Configuration -nologo -v q @properties
	if ($LASTEXITCODE -ne 0) { throw "build failed: $project" }
}

Build (Join-Path $PSScriptRoot 'Browser\Browser.csproj') @("-p:OutputPath=$out")
Build (Join-Path $PSScriptRoot 'KCViewer\KCViewer.csproj') @("-p:OutputPath=$out")

Write-Host "built into $out"
