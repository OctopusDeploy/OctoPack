param($installPath, $toolsPath, $package, $project)

Import-Module (Join-Path $toolsPath "MSBuild.psm1")

function Delete-Temporary-File 
{
    Write-Host "Delete temporary file"

    $project.ProjectItems | Where-Object { $_.Name -eq 'OctoPack-Readme.txt' } | Foreach-Object {
        Remove-Item ( $_.FileNames(0) )
        $_.Remove() 
    }
}

function Get-RelativePath ( $folder, $filePath ) 
{
    Write-Verbose "Resolving paths relative to '$Folder'"
    $from = $Folder = split-path $Folder -NoQualifier -Resolve:$Resolve
    $to = $filePath = split-path $filePath -NoQualifier -Resolve:$Resolve

    while($from -and $to -and ($from -ne $to)) {
        if($from.Length -gt $to.Length) {
            $from = split-path $from
        } else {
            $to = split-path $to
        }
    }

    $filepath = $filepath -replace "^"+[regex]::Escape($to)+"\\"
    $from = $Folder
    while($from -and $to -and $from -gt $to ) {
        $from = split-path $from
        $filepath = join-path ".." $filepath
    }
    Write-Output $filepath
}

function Install-Targets ( $project )
{
    Write-Host ("Installing OctoPack Targets file import into project " + $project.Name)

    $buildProject = Get-MSBuildProject

    $buildProject.Xml.Imports | Where-Object { $_.Project -match "OctoPack" } | foreach-object {     
        Write-Host ("Removing old import:      " + $_.Project)
        $buildProject.Xml.RemoveChild($_) 
    }

    $projectItem = Get-ChildItem $project.FullName
    Write-Host ("The current project is:   " + $project.FullName)
    Write-Host ("Project parent directory: " + $projectItem.Directory)

    $importFile = Join-Path $toolsPath "..\targets\OctoPack.targets"
    $importFile = Resolve-Path $importFile
    $importFile = Get-RelativePath $projectItem.Directory $importFile 

    Write-Host ("Import will be added for: " + $importFile)

    $target = $buildProject.Xml.AddImport( $importFile )

    $project.Save() 

    Write-Host ("Import added!")
}

function Main 
{
    Delete-Temporary-File

    Install-Targets $project
}

Main
