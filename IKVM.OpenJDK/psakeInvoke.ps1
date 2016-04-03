param(
    [Parameter(Mandatory=$true)][string[]]$Targets
)

$mypath = (Split-Path -Parent $MyInvocation.MyCommand.Path)
$propnames = $env:_MSB_PROPERTIES
$properties = @{}

if ($propnames -ne $null) {
    foreach ($name in $propnames.Split(';')) {
	    $properties[$name] = (cat Env:\$name)
	}
}

Import-Module C:\ProgramData\chocolatey\lib\psake\tools\psake.psm1
Invoke-Psake -buildFile $mypath\psakeProject.ps1 -taskList $Targets -properties $properties
