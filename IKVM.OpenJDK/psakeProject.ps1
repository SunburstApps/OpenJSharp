function Perform {
    param(
	    [Parameter(Mandatory=$true)][string]$Description,
		[Parameter(Mandatory=$true)][scriptblock]$Task
	)

	Write-Host "$($Description)"
	$Task.Invoke()
	Write-Host "$($Description) - " -NoNewline
	Write-Host "done" -ForegroundColor Green
}

function Get-WebFile {
    param(
	    [Parameter(Mandatory=$true)][uri]$URI,
		[Parameter(Mandatory=$true)][string]$Path
	)

	$client = (New-Object System.Net.WebClient)
	$client.DownloadFile($URI, $Path)
}

Properties {
    $IntDir = $null
    $OutDir = $null
	$ProjectDir = $null
	$SolutionDir = $null
	$Configuration = $null
}

Task default -Depends build
Task build -Depends DownloadOpenJDK, VerifyLicenses, GenerateStubJars

Task DownloadOpenJDK -RequiredVariables IntDir {
    if (-not [System.IO.File]::Exists("$($IntDir)\openjdk-8u45.zip")) {
        Perform "Downloading OpenJDK-8u45-b14" {
            Get-WebFile -URI 'http://www.frijters.net/openjdk-8u45-b14-stripped.zip' -Path "$($IntDir)\openjdk-8u45.zip"
	    }
	}

	if (-not [System.IO.Directory]::Exists("$($IntDir)\openjdk-8u45-b14")) {
        Perform "Unpacking download" {
            # This file contains a top-level directory inside it, don't create another one
            Unblock-File "$($IntDir)\openjdk-8u45.zip" -ErrorAction SilentlyContinue
            pushd $IntDir
            & "7z.exe" x "openjdk-8u45.zip"
            popd
    	}
	}
}

Task GenerateStubJars -RequiredVariables OutDir, Configuration {
    pushd $IntDir
    $ikvmstub = "$($SolutionDir)\ikvmstub\bin\$($Configuration)\ikvmstub.exe"
	& $ikvmstub -bootstrap mscorlib
	& $ikvmstub -bootstrap System
	& $ikvmstub -bootstrap System.Core
	& $ikvmstub -bootstrap System.Data
	& $ikvmstub -bootstrap System.Drawing
	& $ikvmstub -bootstrap System.Xml
	popd
}

Task VerifyLicenses -RequiredVariables IntDir, ProjectDir, SolutionDir, Configuration -Depends GenerateSourceList, DownloadOpenJDK {
    pushd $ProjectDir
    & "$($SolutionDir)\IKVM.SourceLicenseAnalyzer\bin\$($Configuration)\IKVM.SourceLicenseAnalyzer.exe" "$($IntDir)\allsources.gen.lst"
    popd
}

Task GenerateSourceList -RequiredVariables IntDir, ProjectDir {
    $list_file = [System.IO.File]::ReadAllText("$($ProjectDir)\allsources.lst")
	$replaced = $list_file.Replace("@OPENJDK@", "$($IntDir)\openjdk-8u45-b14")
	[System.IO.File]::WriteAllText("$($IntDir)\allsources.gen.lst", $replaced)
}

Task clean -RequiredVariables IntDir, OutDir {
    # Don't delete openjdk-8u45.zip or its expanded contents, due to the size of the download and the time required to unpack it.
	Remove-File -Force "$($IntDir)\allsources.gen.lst"
	Remove-Item -Force "$($IntDir)\*.jar"
}
