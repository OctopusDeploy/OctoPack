## This is the OctoPack build script, written in PSAKE. Run with
##
##   Import-Module .\tools\psake\psake.psm1
##   Invoke-psake .\build.ps1
##

Framework "4.5.1"

properties {
    $configuration = "Release"
    $nuget_path = ".\source\tools\nuget.exe"
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

task Build -depends Clean, RunGitVersion {
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
    mkdir .\build\build
    mkdir .\build\content\portable-net4+sl50+netcore45+wpa81+wp8
    mkdir .\build\tools
    dir -recurse .\source\OctoPack.Tasks\bin\$configuration | copy -destination build\build -Force
    dir -recurse .\source\build | copy -destination build\build -Force
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
