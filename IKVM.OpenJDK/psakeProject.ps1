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
}

Task default -Depends build
Task build -Depends DownloadOpenJDK

Task DownloadOpenJDK -RequiredVariables IntDir {
    if (-not [System.IO.File]::Exists("$($IntDir)\openjdk-8u45.zip")) {
        Perform "Downloading OpenJDK-8u45-b14" {
            Get-WebFile -URI 'http://www.frijters.net/openjdk-8u45-b14-stripped.zip' -Path "$($IntDir)\openjdk-8u45.zip"
	    }
	}

	Perform "Unpacking download" {
	    # This file contains a top-level directory inside it, don't create another one
		Unblock-File "$($IntDir)\openjdk-8u45.zip" -ErrorAction SilentlyContinue
		pushd $IntDir
		& "7z.exe" x "openjdk-8u45.zip"
		popd
	}
}

Task clean -RequiredVariables IntDir, OutDir {
    # Don't delete openjdk-8u45.zip, due to the size of the download.
    Remove-Item -Recurse -Force "$($IntDir)\openjdk-8u45"
}
