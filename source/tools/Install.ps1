param($installPath, $toolsPath, $package, $project)

Import-Module (Join-Path $toolsPath "MSBuild.psm1")

function Delete-Temporary-File 
{
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

function Install-Targets ( $project, $importFile )
{
    $buildProject = Get-MSBuildProject $project.Name

    $buildProject.Xml.Imports | Where-Object { $_.Project -match "OctoPack" } | foreach-object {     
        Write-Host ("Removing old import:      " + $_.Project)
        $buildProject.Xml.RemoveChild($_) 
    }

    $projectItem = Get-ChildItem $project.FullName
    Write-Host ("Adding MSBuild targets import: " + $importFile)

    $target = $buildProject.Xml.AddImport( $importFile )

    $project.Save()
}

function Get-OctoPackTargetsPath ($project) {
    $projectItem = Get-ChildItem $project.FullName
    $importFile = Join-Path $toolsPath "..\targets\OctoPack.targets"
    $importFile = Resolve-Path $importFile
    $importFile = Get-RelativePath $projectItem.Directory $importFile 
    return $importFile
}

function Copy-OctoPackTargetsToSolutionRoot($project) {
    $solutionDir = Get-SolutionDir
    $octopackFolder = (Join-Path $solutionDir .octopack)

    # Get the target file's path
    $targetsFolder = Join-Path $toolsPath "..\targets" | Resolve-Path
    
    if(!(Test-Path $octopackFolder)) {
        mkdir $octopackFolder | Out-Null
    }

    $octopackFolder = resolve-path $octopackFolder

    Write-Host "Copying OctoPack MSBuild targets to: $octopackFolder"

    Copy-Item "$targetsFolder\*.*" $octopackFolder -Force | Out-Null

    Write-Host "IMPORTANT: You must commit/check in the .octopack folder to your source control system"

    $projectItem = Get-ChildItem $project.FullName
    return '$(SolutionDir)\.octopack\OctoPack.targets'
}

function Main 
{
    Delete-Temporary-File

    $addToSolution = (Get-MSBuildProperty RestorePackages $project.Name).EvaluatedValue

    $importFile = ''

    if($addToSolution){
        Write-Host "NuGet package restore is enabled. Adding OctoPack to the solution directory."
        $importFile = Copy-OctoPackTargetsToSolutionRoot $project
    } else {
        Write-Host "NuGet package restore is not enabled. Adding OctoPack from the package directory."
        $importFile = Get-OctoPackTargetsPath $project
    }

    Install-Targets $project $importFile

    Write-Host ("OctoPack installed successfully")
}

Main
