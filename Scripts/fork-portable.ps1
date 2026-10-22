# Builds the portable viewer (VIEWER-PLAN.md, stage 6 (2)) from ..\Viewer. Self-contained .NET 10: nothing to install.
#   <Output>\           KCViewer.exe and KCBrowser.exe, each a single file with the .NET runtime and the
#                       managed libraries inside (user's choice 2026-10-02); next to them the native libraries (bundled
#                       ones would be extracted to %TEMP% at start, outside the folder) and the viewer's data files
#   <Output>\KCUpdater.exe  the program updater, published with NativeAOT (UPDATER-DESIGN.md, section 7)
#   <Output>\LICENSE, Licenses\  the MIT notice and the libraries' licenses (UPDATER-DESIGN.md 8-8)
#   <Output>\Plugins\, Skins\  empty but for a README each; not in a release (UPDATER-DESIGN.md, section 4: an update
#                       refuses a zip with files outside its install list)
#   <Output>\Data\      not built: made at first start (Data\live for the live instance, Data\test for the test one)
#   <Output>\GameAssets\ not built: the game's images the viewer fetches when no assets folder has them (ForkGameAssets)
# The default output is ..\dist\KCViewer, for a release ..\dist\v<version>\KCViewer (..\.gitignore keeps dist out).
# The viewer and its plugins find the local server (server\, assets\) by walking up from the exe. -Zip also writes
# KCViewer-<date>-<commit>.zip next to the output folder, or for a version KCViewer-<version>.zip.
# UI languages: Korean, Japanese and English only (fork-portable.targets).
# -Folder: the layout until 2026-10-02, every assembly and the runtime as files of their own (about 700 files).
# -Version: a release's version (1.0.0), shown in the version window with the commit and written as the files' version;
# without it, a tag such as v1.0.0 or 1.0.0 on the commit is the version (KCViewer.csproj, target ForkCommitInfo),
# else it is a development build (the commit and its date only).
# -Release: a release of the program update (UPDATER-DESIGN.md, sections 3, 4 and 8-2). Needs a version, no uncommitted
# change in Viewer\ or Scripts\ (-AllowDirty only to rehearse), the public key Viewer\Portable\update-public-key.txt
# (the build puts it into the viewer) and the private key -KeyFile, which stays outside every repository. Writes
# install-manifest.json into the folder and next to the folder KCViewer-<version>.zip, kcviewer-update.json and its
# signature kcviewer-update.json.sig (ReleaseTool), then checks them as the viewer will.
#   -UrlBase: where the zip will be, default the GitHub release of the tag v<version>. -NoteFile: the update's note.
# -Publish (with -Release): gh release create v<version> with those three files. The tag must be on GitHub already.
#   -Title: the release's title, default "KCViewer v<version>".
param(
	[string]$Output,
	[switch]$Zip,
	[switch]$Folder,
	[string]$Version,
	[switch]$Release,
	[switch]$AllowDirty,
	[string]$KeyFile = (Join-Path $PSScriptRoot '..\..\keys\kcviewer-update-private.txt'),
	[string]$UrlBase,
	[string]$NoteFile,
	[switch]$Publish,
	[string]$Title
)

$ErrorActionPreference = 'Stop'
$viewer = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Viewer'))
$repository = 'MallangMochi/KCViewer'
$publicKey = Join-Path $viewer 'Portable\update-public-key.txt'
# Languages and the original bootstrapper (see the file)
$targets = Join-Path $PSScriptRoot 'fork-portable.targets'
# A single-file publish builds first into this folder (not bin\x64\Release, which the original setup uses)
$singleFileBuild = Join-Path $viewer 'KCViewer\bin\singlefile\'

# git under 'Continue': Windows PowerShell stops on a native command's stderr (git's line ending warnings) under 'Stop'
# (not named Git: PowerShell names are case-insensitive, so git inside it would call the function itself)
function GitText([string[]]$arguments) {
	$ErrorActionPreference = 'Continue'
	return (& git.exe -C $viewer @arguments 2>$null | Out-String).Trim()
}

$commit = GitText @('rev-parse', '--short', 'HEAD')
if (-not $commit) { throw "no git commit in $viewer" }
# Viewer\ and Scripts\ make the program; Data\ (the translations) is not part of it
$dirty = [bool](GitText @('status', '--porcelain', '--', $viewer, $PSScriptRoot))
$commitText = if ($dirty) { "$commit-dirty" } else { $commit }
# the release's version: -Version, else the first [v]1.2.3 tag on the commit (as KCViewer.csproj reads it)
$ver = $Version
if (-not $ver) {
	$ver = (GitText @('tag', '--points-at', 'HEAD')) -split "`r?`n" |
		ForEach-Object { if ($_ -match '^v?(\d+\.\d+\.\d+.*)$') { $Matches[1] } } | Select-Object -First 1
}

if ($Publish -and -not $Release) { throw '-Publish needs -Release' }
if ($Release) {
	if (-not $ver) { throw '-Release needs -Version or a tag v1.2.3 on the commit' }
	if ($dirty -and -not $AllowDirty) { throw 'Viewer\ or Scripts\ has uncommitted changes: commit (and tag) first, or -AllowDirty to rehearse' }
	if ($dirty -and $Publish) { throw '-Publish never publishes uncommitted changes' }
	if (-not (Test-Path $publicKey)) { throw "no public key: $publicKey" }
	if (-not (Test-Path $KeyFile)) { throw "no private key: $KeyFile" }
	if (-not $UrlBase) { $UrlBase = "https://github.com/$repository/releases/download/v$ver/" }
}
if (-not $Output) { $Output = if ($Release) { Join-Path $PSScriptRoot "..\dist\v$ver\KCViewer" } else { Join-Path $PSScriptRoot '..\dist\KCViewer' } }
$out = [IO.Path]::GetFullPath($Output).TrimEnd('\') + '\'

# A folder that was run or filled by hand keeps logins, records, plugins, skins and fetched game images: never delete it.
if (Test-Path $out) {
	# (not $folder: Where-Object runs in this scope, where $Folder is the switch)
	$kept = @('Data', 'Plugins', 'Skins', 'GameAssets', 'Update') | Where-Object {
		$userFolder = Join-Path $out $_
		(Test-Path $userFolder) -and (Get-ChildItem $userFolder -Recurse -File | Where-Object Name -NotLike '*README.txt')
	}
	if ($kept) { throw "$out has $($kept -join ', ') with files in it. Move them away or choose another -Output." }
	Remove-Item $out -Recurse -Force
}

# the version as the files' version too (AssemblyVersion stays 5.4.3.0: saved window states name the types with it)
$versionArgs = @()
if ($ver) {
	$numbers = ($ver -replace '[-+].*$', '')
	$versionArgs = @("-p:ForkReleaseVersion=$ver", "-p:FileVersion=$numbers.0", "-p:InformationalVersion=$ver", '-p:IncludeSourceRevisionInInformationalVersion=false')
}

function Build([string]$project) {
	Write-Host "== $([IO.Path]::GetFileNameWithoutExtension($project))"
	if (-not $Folder) {
		# publish makes the bundle. Assembly.Location is empty inside it, so the viewer finds its folder with
		# AppContext.BaseDirectory (FormBrowserHost, FileService). Debug symbols go inside the assemblies; the API
		# documentation (xml) is left out. The original publish step of Browser.csproj only deletes three files from
		# a fixed folder that is not there.
		dotnet publish $project -c Release -nologo -v q -o $out "-p:OutputPath=$singleFileBuild" "-p:DirectoryBuildTargetsPath=$targets" -p:RuntimeIdentifier=win-x64 -p:SelfContained=true -p:PublishSingleFile=true -p:DebugType=embedded -p:PublishDocumentationFile=false -p:PublishReferencesDocumentationFiles=false @versionArgs
		if ($LASTEXITCODE -ne 0) { throw "publish failed: $project" }
		return
	}
	# -Folder: build rather than publish (the publish steps above). No runtime bootstrapper: it only asks for an
	# installed .NET, which a self-contained build does not need.
	dotnet build $project -c Release -nologo -v q "-p:OutputPath=$out" "-p:DirectoryBuildTargetsPath=$targets" -p:RuntimeIdentifier=win-x64 -p:SelfContained=true -p:BootstrapperEnabled=false @versionArgs
	if ($LASTEXITCODE -ne 0) { throw "build failed: $project" }
}

Build (Join-Path $viewer 'Browser\Browser.csproj')
Build (Join-Path $viewer 'KCViewer\KCViewer.csproj')

# The updater: one native exe. NativeAOT links with the MSVC tools, whose vcvarsall.bat calls vswhere.exe by name
# (UPDATER-DESIGN.md 11-4).
Write-Host '== KCUpdater'
$updaterBuild = Join-Path $viewer 'KCUpdater\bin\portable\'
$env:PATH = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer;$env:PATH"
dotnet publish (Join-Path $viewer 'KCUpdater\KCUpdater.csproj') -c Release -r win-x64 -nologo -v q -o $updaterBuild @versionArgs
if ($LASTEXITCODE -ne 0) { throw 'publish failed: KCUpdater' }
Copy-Item (Join-Path $updaterBuild 'KCUpdater.exe') $out

# What the original publish step removes (a software Vulkan renderer the browser does not need)
foreach ($file in 'vk_swiftshader.dll', 'vk_swiftshader_icd.json', 'vulkan-1.dll') {
	Remove-Item (Join-Path $out $file) -ErrorAction SilentlyContinue
}
# Debug symbols of the native SkiaSharp and HarfBuzz libraries (about 100 MB); the managed ones are small and stay
Remove-Item (Join-Path $out 'lib*.pdb') -ErrorAction SilentlyContinue

$portable = Join-Path $viewer 'Portable'
Copy-Item (Join-Path $portable 'README.txt') $out
Copy-Item (Join-Path $viewer 'LICENSE') $out
Copy-Item (Join-Path $viewer 'Licenses') $out -Recurse
if (-not $Release) {
	New-Item -ItemType Directory -Force (Join-Path $out 'Plugins'), (Join-Path $out 'Skins') | Out-Null
	# A file in each, as a zip keeps no empty folders
	Copy-Item (Join-Path $portable 'Plugins-README.txt') (Join-Path $out 'Plugins\README.txt')
	Copy-Item (Join-Path $portable 'Skins-README.txt') (Join-Path $out 'Skins\README.txt')
}

$versionLine = if ($ver) { "version $ver`r`n" } else { '' }
Set-Content (Join-Path $out 'version.txt') "${versionLine}commit $commitText`r`nbuilt $(Get-Date -Format 'yyyy-MM-dd HH:mm')" -Encoding ASCII

# Only program files: nothing that a run writes in its working folder (settings, records, logs, browser profiles)
$personal = @('Data', 'Record', 'WebView2', 'BrowserCache', 'DataAndTranslations', 'PluginData', 'eolog.log',
	'Settings\Configuration.xml', 'Settings\Fork.json') | Where-Object { Test-Path (Join-Path $out $_) }
if ($personal) { throw "unexpected files in the build: $($personal -join ', ')" }

$size = (Get-ChildItem $out -Recurse -File | Measure-Object -Sum Length).Sum
Write-Host ("built into {0} ({1:N0} MB, {2}commit {3})" -f $out, ($size / 1MB), $(if ($ver) { "version $ver, " } else { '' }), $commitText)

if ($Release) {
	Write-Host '== release files'
	$toolProject = Join-Path $PSScriptRoot 'ReleaseTool\ReleaseTool.csproj'
	dotnet build $toolProject -c Release -nologo -v q
	if ($LASTEXITCODE -ne 0) { throw 'build failed: ReleaseTool' }
	$tool = Join-Path $PSScriptRoot 'ReleaseTool\bin\Release\net10.0\ReleaseTool.dll'
	$releaseDir = Split-Path $out.TrimEnd('\')
	$toolArgs = @('release', $out.TrimEnd('\'), $ver, $commitText, $releaseDir, $KeyFile, $UrlBase)
	if ($NoteFile) { $toolArgs += [IO.Path]::GetFullPath($NoteFile) }
	dotnet $tool @toolArgs
	if ($LASTEXITCODE -ne 0) { throw 'the release files failed' }
	dotnet $tool verify $releaseDir $publicKey
	if ($LASTEXITCODE -ne 0) { throw 'the release files do not verify' }

	if ($Publish) {
		# also its install folder: a shell started before gh was installed keeps the old PATH
		$gh = (Get-Command gh -ErrorAction SilentlyContinue).Source
		if (-not $gh) { $gh = Join-Path $env:ProgramFiles 'GitHub CLI\gh.exe' }
		if (-not (Test-Path $gh)) { throw 'gh (GitHub CLI) is not installed' }
		$files = "KCViewer-$ver.zip", 'kcviewer-update.json', 'kcviewer-update.json.sig' | ForEach-Object { Join-Path $releaseDir $_ }
		# --notes= rather than an empty argument, which Windows PowerShell drops
		$notes = if ($NoteFile) { @('--notes-file', [IO.Path]::GetFullPath($NoteFile)) } else { @('--notes=') }
		# a version with a suffix (1.0.0-rc1) is a pre-release, which the viewers' update never takes (releases/latest)
		$pre = if ($ver -match '-') { @('--prerelease') } else { @() }
		$name = if ($Title) { $Title } else { "KCViewer v$ver" }
		& $gh release create "v$ver" @files --repo $repository --title $name --verify-tag @notes @pre
		if ($LASTEXITCODE -ne 0) { throw 'gh release create failed' }
	}
}
elseif ($Zip) {
	Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
	# a version by its version (-dirty when built with uncommitted changes), a development build by date and commit
	$zipName = if ($ver) { "KCViewer-$ver$(if ($dirty) { '-dirty' }).zip" } else { "KCViewer-{0}-{1}.zip" -f (Get-Date -Format 'yyyyMMdd'), $commitText }
	$zipPath = Join-Path (Split-Path $out.TrimEnd('\')) $zipName
	Remove-Item $zipPath -ErrorAction SilentlyContinue
	$root = Split-Path $out.TrimEnd('\') -Leaf
	$archive = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
	try {
		foreach ($file in Get-ChildItem $out -Recurse -File) {
			# Forward slashes, as the zip format has them (Windows PowerShell's CreateFromDirectory writes backslashes)
			$name = $root + '/' + $file.FullName.Substring($out.Length).Replace('\', '/')
			[void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $name, [IO.Compression.CompressionLevel]::Optimal)
		}
	}
	finally { $archive.Dispose() }
	Write-Host ("zip {0} ({1:N0} MB)" -f $zipPath, ((Get-Item $zipPath).Length / 1MB))
}
