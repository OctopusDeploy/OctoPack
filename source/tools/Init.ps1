param($installPath, $toolsPath, $package)
Import-Module (Join-Path $toolsPath AddTargetsToDatabaseProject.psm1) -ArgumentList $toolsPath, $package