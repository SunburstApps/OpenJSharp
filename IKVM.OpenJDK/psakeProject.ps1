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
Task build -Depends DownloadOpenJDK, GenerateStubJars, Compile

Task DownloadOpenJDK -RequiredVariables IntDir {
    if (-not [System.IO.File]::Exists("Downloaded\openjdk-8u45.zip")) {
        Perform "Downloading OpenJDK-8u45-b14" {
            Get-WebFile -URI 'http://www.frijters.net/openjdk-8u45-b14-stripped.zip' -Path "$($IntDir)\openjdk-8u45.zip"
	    }
	}

	if (-not [System.IO.Directory]::Exists("Downloaded\openjdk-8u45-b14")) {
        Perform "Unpacking download" {
            # This file contains a top-level directory inside it, don't create another one
            Unblock-File "$($IntDir)\openjdk-8u45.zip" -ErrorAction SilentlyContinue
            pushd $IntDir
            & "7z.exe" x "openjdk-8u45.zip"
            popd
    	}
	}
}

Task Compile -RequiredVariables IntDir, OutDir -Depends DownloadOpenJDK, GeneratePropertyConstants, GenerateSourceList, GenerateStubJars {
    New-Item -ItemType Directory "$($IntDir)\classfiles" -ErrorAction SilentlyContinue | Out-Null

    & javac.exe -d "$($IntDir)\classfiles" -J-Xmx1536M -g -nowarn -implicit:none -parameters -cp dummy `
    -bootclasspath "$($IntDir)\mscorlib.jar;$($IntDir)\System.jar;$($IntDir)\System.Core.jar;$($IntDir)\System.Data.jar;$($IntDir)\System.Drawing.jar;$($IntDir)\System.xml.jar;$($IntDir)\IKVM.Runtime.jar;$($IntDir)\IKVM.Runtime.jar" `
    "@$($IntDir)\allsources.gen.lst"
}

Task GeneratePropertyConstants {
    $version_code = [System.IO.File]::ReadAllLines("$($SolutionDir)\IKVM.GlobalAssemblyInfo\GlobalAssemblyInfo.cs")
    $ikvm_version = [regex]::Match($version_code, "AssemblyVersion\(`"[0-9.]+`"\)")
	$ikvm_version = [regex]::Match($ikvm_version, "([0-9.]+)").Value

    $infile = [System.IO.File]::ReadAllText("$($ProjectDir)\ikvm\java\lang\PropertyConstants.java.in")
    $replaced = $infile.Replace("@AWTASSEMBLY@", "IKVM.Awt.WindowsForms")
    $replaced = $replaced.Replace("@VERSION@", $ikvm_version)
    $replaced = $replaced.Replace("@OPENJDK_VERSION@", "1.8.0_45-b14")
    [System.IO.File]::WriteAllText("$($ProjectDir)\ikvm\java\lang\PropertyConstants.java", $replaced)
}

Task GenerateStubJars -RequiredVariables OutDir, SolutionDir, Configuration {
    pushd $IntDir
    $ikvmstub = "$($SolutionDir)\ikvmstub\bin\$($Configuration)\ikvmstub.exe"
	& $ikvmstub -bootstrap mscorlib
	& $ikvmstub -bootstrap System
	& $ikvmstub -bootstrap System.Core
	& $ikvmstub -bootstrap System.Data
	& $ikvmstub -bootstrap System.Drawing
	& $ikvmstub -bootstrap System.Xml
	& $ikvmstub -bootstrap "$($SolutionDir)\IKVM.Runtime.FirstPass\bin\$($Configuration)\IKVM.Runtime.dll"
	popd
}

Task VerifyLicenses -RequiredVariables IntDir, ProjectDir, SolutionDir, Configuration -Depends GenerateSourceList, DownloadOpenJDK {
    pushd $ProjectDir
    & "$($SolutionDir)\IKVM.SourceLicenseAnalyzer\bin\$($Configuration)\IKVM.SourceLicenseAnalyzer.exe" "$($IntDir)\allsources.gen.lst"
    popd
}

Task GenerateSourceList -RequiredVariables IntDir, ProjectDir {
    $list_file = [System.IO.File]::ReadAllText("$($ProjectDir)\allsources.lst")
	$replaced = $list_file.Replace("@OPENJDK@", "`"$($ProjectDir)\Downloaded\openjdk-8u45-b14`"")
	[System.IO.File]::WriteAllText("$($IntDir)\allsources.gen.lst", $replaced)
}

Task clean -RequiredVariables IntDir, OutDir {
    # Don't delete openjdk-8u45.zip or its expanded contents, due to the size of the download and the time required to unpack it.
	Remove-Item -Force "$($IntDir)\allsources.gen.lst"
	Remove-Item -Force "$($IntDir)\*.jar"

	Remove-Item -Recurse -Force "$($IntDir)\classfiles"
}
