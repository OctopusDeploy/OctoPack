param($installPath, $toolsPath, $package, $project)
Import-Module (Join-Path $toolsPath AddTargetsToDatabaseProject.psm1) -ArgumentList $toolsPath, $package
Install-OctoPackTargetsToProject($project)   