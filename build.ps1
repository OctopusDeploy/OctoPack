## This is the OctoPack build script, written in PSAKE. Run with 
## 
##   Import-Module .\tools\psake\psake.psm1
##   Invoke-psake .\build.ps1
##

Framework "4.0"

properties {
    $configuration = "Release"
    $nuget_path = "tools\nuget.exe"
    $gitversion_exe = ".\tools\GitVersion\GitVersion.exe"
    $gitversion_remote_username = $ENV:GITVERSION_REMOTE_USERNAME
    $gitversion_remote_password = $ENV:GITVERSION_REMOTE_PASSWORD
}

task default -depends VerifyProperties, Package

task Clean {
	write-host "Clean"

    $directories = ".\build"

    $directories | ForEach-Object { 
        Write-Output "Clean directory $_"
        Remove-Item $_ -Force -Recurse -ErrorAction Ignore
        New-Item -Path $_ -ItemType Directory -Force
    }
}

task RunGitVersion {
    exec {
        # Log the current status, branch and latest log to help when things get confused...
        Write-Host "Executing 'git status'"
        & git status | Write-Host
        Write-Host "Executing 'git log -n 1'"
        & git log -n 1 | Write-Host

        if ($gitversion_remote_username) {
            $output = . $gitversion_exe /u "$gitversion_remote_username" /p "$gitversion_remote_password"
        } else {
            $output = . $gitversion_exe
        }

        $formattedOutput = $output -join "`n"
        Write-Host "Output from gitversion.exe"
        Write-Host $formattedOutput

        $versionInfo = $formattedOutput | ConvertFrom-Json
        $script:package_version = $versionInfo.NuGetVersion
        write-host "Package version:    $script:package_version"
        
        if ($env:TEAMCITY_VERSION) {
            TeamCity-SetBuildNumber $versionInfo.FullSemVer
            write-host "TeamCity version: " + $versionInfo.FullSemVer
        }
    }
}

task PackageRestore -depends Clean {
    $userProfilePath = [Environment]::GetFolderPath("UserProfile")
    $globalNuGetCachePath = Join-Path $userProfilePath ".nuget\packages"
    $matches = Get-ChildItem -Path $globalNuGetCachePath -Filter "NuGet*"
    if ($matches) {
        write-host "Deleting NuGet packages from the global nuget cache RE https://github.com/NuGet/Home/issues/2690"
        $matches | Format-Table
        $matches | Remove-Item -Recurse -Force
    }

    write-host "Restoring packages"
    & $nuget_path restore .\source\OctoPack.sln
}

task Build -depends Clean, PackageRestore, RunGitVersion {
	write-host "Build"
    
    exec {
        msbuild .\source\OctoPack.sln /p:Configuration=$configuration /t:Rebuild
    }
}

task Package -depends Build {
	write-host "Package"

    mkdir .\build\content
    mkdir .\build\content\net35
    mkdir .\build\content\net40
    mkdir .\build\content\netcore45
    mkdir .\build\tools
    dir -recurse .\source\OctoPack.Tasks\bin\$configuration | copy -destination build\tools -Force
    dir -recurse .\source\tools | copy -destination build\tools -Force
    Copy-Item .\source\OctoPack.nuspec .\build 
    Copy-Item .\license.txt .\build 

    $base = (resolve-path "build")
    write-host $base
	exec {
        & $nuget_path pack build\OctoPack.nuspec -basepath $base -outputdirectory $base -version $script:package_version -NoPackageAnalysis
    }
}

## Helpers

task VerifyProperties {
}

Import-Module .\tools\psake\teamcity.psm1

TaskSetup {
	if ($env:TEAMCITY_VERSION) {
	    TeamCity-ReportBuildProgress "Running task $($psake.context.Peek().currentTaskName)"	
	}
}
