param($installPath, $toolsPath, $package, $project)

Import-Module (Join-Path $toolsPath "MSBuild.psm1")

function Uninstall-Targets ( $project )
{
    Write-Host ("Removing OctoPack targets import from project: " + $project.Name)

    $buildProject = Get-MSBuildProject $project.Name

    $buildProject.Xml.Imports | Where-Object { $_.Project -match "OctoPack" } | foreach-object {     
        Write-Host ("Removing old import:      " + $_.Project)
        $buildProject.Xml.RemoveChild($_) 
    }

    $project.Save() 
}

function Main 
{
    Uninstall-Targets $project

    Write-Host ("OctoPack uninstalled successfully")
}

Main
