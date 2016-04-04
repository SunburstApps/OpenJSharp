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
Task build -Depends DownloadOpenJDK, GenerateStubJars, Compile, CreateRmiStubs, MakeVFS

Task MakeVFS -RequiredVariables IntDir, OutDir -Depends DownloadOpenJDK {
    $files = @(
	    "lib/calendars.properties",
		"lib/logging.properties",
		"lib/management/management.properties",
		"lib/net.properties",
		"lib/psfontj2d.properties",
		"lib/sound.properties",
		"lib/cmm/*",
		"lib/tzdb.dat",
		"lib/currency.data",
		"lib/security/java.policy",
		"lib/security/java.security",
		"lib/security/US_export_policy.jar"
	)

    pushd "Downloaded\openjdk-8u45-b14\build\linux-x86_64-normal-server-release\jdk"
	& "7z.exe" a "$($OutDir)\vfs.zip" @files
	popd

	pushd "Downloaded\openjdk-8u45-b14\jdk\src\windows"
	& "7z.exe" a "$($OutDir)\vfs.zip" lib/flavormap.properties lib/content-types.properties
	popd
}

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

    if (-not [System.IO.File]::Exists("$($IntDir)\classfiles.done")) {
        & javac.exe -d "$($IntDir)\classfiles" -J-Xmx1536M -g -nowarn -implicit:none -parameters -cp dummy `
        -bootclasspath "$($IntDir)\mscorlib.jar;$($IntDir)\System.jar;$($IntDir)\System.Core.jar;$($IntDir)\System.Data.jar;$($IntDir)\System.Drawing.jar;$($IntDir)\System.xml.jar;$($IntDir)\IKVM.Runtime.jar;$($IntDir)\IKVM.Runtime.jar" `
        "@$($IntDir)\allsources.gen.lst"
		Set-Content "$($IntDir)\classfiles.done" -Value "Remove this file to force a rebuild of the OpenJDK Java sources."
    }
}

Task CreateRmiStubs -RequiredVariables IntDir, OutDir -Depends Compile {
    New-Item -ItemType Directory "$($IntDir)\rmistubs" -ErrorAction SilentlyContinue | Out-Null

	$QuoteString = [scriptblock]{
        param([string]$x)
        return "`"$($x)`""
	}

	$classpath = @(
	    "$($IntDir)\mscorlib.jar",
		"$($IntDir)\System.Xml.jar",
		"Downloaded\openjdk-8u45-b14\jdk\src\share\classes",
		"Downloaded\openjdk-8u45-b14\corba\src\share\classes",
		"Downloaded\openjdk-8u45-b14\build\linux-x86_64-normal-server-release\jdk\gensrc"
	)

	$args = @(
	    "-J-client",
		"-J-Xmx896m",
		"-J-Xms128m",
		"-nowarn",
		"-bootclasspath",
		[string]::Join(';', $classpath),
		"-classpath",
		"$($IntDir)\classfiles",
		"-d",
		"$($IntDir)\rmistubs"
	)

    & rmic @args "-v1.1" sun.rmi.registry.RegistryImpl
    & rmic @args "-v1.1" sun.rmi.transport.DGCImpl
    & rmic @args "-v1.2" sun.rmi.server.Activation`$ActivationSystemImpl
    & rmic @args "-v1.2" java.rmi.activation.ActivationGroup
    & rmic @args "-v1.2" com.sun.jndi.rmi.registry.ReferenceWrapper
    & rmic @args "-v1.2" javax.management.remote.rmi.RMIConnectionImpl
    & rmic @args "-v1.2" -iiop javax.management.remote.rmi.RMIConnectionImpl
    & rmic @args "-v1.2" -iiop -standardPackage javax.management.remote.rmi.RMIConnectionImpl
    & rmic @args "-v1.2" javax.management.remote.rmi.RMIServerImpl
    & rmic @args "-v1.2" -iiop javax.management.remote.rmi.RMIServerImpl
    & rmic @args "-v1.2" -iiop -standardPackage javax.management.remote.rmi.RMIServerImpl
    & rmic @args -iiop javax.management.remote.rmi.RMIConnection
    & rmic @args -iiop -standardPackage javax.management.remote.rmi.RMIConnection
    & rmic @args -iiop javax.management.remote.rmi.RMIServer
    & rmic @args -iiop -standardPackage javax.management.remote.rmi.RMIServer
}

Task RunNASGen -RequiredVariables IntDir -Depends Compile {
    $sources = (New-Object System.Collections.Generic.List[string])
    dir "Downloaded\openjdk-8u45-b14\nashorn\src\jdk\nashorn\internal\objects\*.java" | % { $sources.Add($_.FullName) }
	dir "Downloaded\openjdk-8u45-b14\nashorn\buildtools\nasgen\src\jdk\nashorn\internal\tools\*.java" | % { $sources.Add($_.FullName) }

	$classpath = @(
	    "$($IntDir)\mscorlib.jar",
		"$($IntDir)\System.Xml.jar",
		"Downloaded\openjdk-8u45-b14\jdk\src\share\classes",
		"Downloaded\openjdk-8u45-b14\corba\src\share\classes",
		"Downloaded\openjdk-8u45-b14\build\linux-x86_64-normal-server-release\jdk\gensrc",
		"Downloaded\nashorn\src",
		"$($IntDir)\IKVM.Runtime.jar"
	)

	& javac "-XDignore.symbol.file" -g -nowarn -implicit:none -bootclasspath ([string]::Join(';', $classpath)) @sources
	& java "-Xbootclasspath/p:Downloaded\openjdk-8u45-b14\nashorn\buildtools\nasgen\src;Downloaded\openjdk-8u45-b14\nashorn\src" `
	jdk.nashorn.internal.tools.nasgen.Main Downloaded\nashorn\src jdk.nashorn.internal.objects Downloaded\nashorn\src
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
	$replaced = $list_file.Replace("@OPENJDK@", "Downloaded\openjdk-8u45-b14")
	[System.IO.File]::WriteAllText("$($IntDir)\allsources.gen.lst", $replaced)
}

Task clean -RequiredVariables IntDir, OutDir {
    # Don't delete openjdk-8u45.zip or its expanded contents, due to the size of the download and the time required to unpack it.
	Remove-Item -Force "$($IntDir)\allsources.gen.lst"
	Remove-Item -Force "$($IntDir)\classfiles.done"
	Remove-Item -Force "$($IntDir)\*.jar"
	Remove-Item -Force "$($OutDir)\vfs.zip"

	Remove-Item -Recurse -Force "$($IntDir)\classfiles"
	Remove-Item -Recurse -Force "$($IntDir)\rmistubs"
}
