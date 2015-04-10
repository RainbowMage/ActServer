Import-Module $PSScriptRoot\PS-Zip.psm1

$root = Join-Path $PSScriptRoot ..
$buildFolder = Join-Path $root "Build\Release"

# Remove build folder if already exists
if ( Test-Path $buildFolder -PathType Container ) {
	Remove-Item -Recurse -Force $buildFolder
}

# Build
../build.bat

if ( $LastExitCode -ne 0 ) {
	Write-Host "Build failed."
	exit 1
}

# Set archive folder name
$mainAssemblyFile = "ActServer.dll"
$assemblyPath = Join-Path $buildFolder $mainAssemblyFile
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($assemblyPath).FileVersion
$archiveFolder = Join-Path $PSScriptRoot "Distribute\ActServer-$version"

# Remove archive folder if exists already
if ( Test-Path $archiveFolder -PathType Container ) {
	Remove-Item -Recurse -Force $archiveFolder
}

# Create archive folder and copy binaries
New-Item -ItemType directory -Path $archiveFolder

xcopy /Y /R /S "$buildFolder\*" "$archiveFolder"
xcopy /Y /R "$(Join-Path $root README.md)" "$archiveFolder"
xcopy /Y /R "$(Join-Path $root LICENSE.txt)" "$archiveFolder"

# Zip it
New-ZipCompress -source $archiveFolder -destination "$archiveFolder.zip" -force
