## This is the OctoPack build script, written in PSAKE. Run with 
## 
##   Import-Module .\tools\psake\psake.psm1
##   Invoke-psake .\build.ps1
##

Framework "4.0"

properties {
	$build_number = "2.0.0"
    $configuration = "Release"
    $nuget_path = "tools\nuget.exe"
}

task default -depends VerifyProperties, Test, Package

task Clean {
	write-host "Clean"

    $directories = ".\build"

    $directories | ForEach-Object { 
        Write-Output "Clean directory $_"
        Remove-Item $_ -Force -Recurse -ErrorAction Ignore
        New-Item -Path $_ -ItemType Directory -Force
    }
}

task Versions {
	write-host "Apply version stamp"
	
	Generate-Assembly-Info `
        -file "source\OctoPack.Tasks\Properties\AssemblyInfo.cs" `
        -title "OctoPack Tasks $version" `
        -description "MSBuild tasks for OctoPack" `
        -company "Octopus Deploy Pty. Ltd." `
        -product "OctoPack $version" `
        -version $version `
        -copyright "Octopus Deploy Pty. Ltd. 2011 - 2012"	
}

task Build -depends Clean, Versions {
	write-host "Build"
    
    exec {
        msbuild .\source\OctoPack.sln /p:Configuration=$configuration /t:Build
    }
}

task Test -depends Build {
	write-host "Run unit tests"
}

task Package -depends Build {
	write-host "Package"

	Copy-Item .\source\OctoPack.Tasks\bin\$configuration\* .\build\targets
	Copy-Item .\source\targets\* .\build\targets
	Copy-Item .\source\tools\* .\build\tools
	Copy-Item .\source\content\* .\build\content
	Copy-Item .\source\OctoPack.nuspec .\build

	exec {
        & $nuget_path pack .\build\OctoPack.nuspec -basepath .\build -outputdirectory .\build -version $build_number -NoPackageAnalysis
    }	
}

task PackageBrain {
	write-host "Package Brain"

    exec {
        & $nuget_path pack .\source\Kraken.Brain\bin\$configuration\Kraken.Brain.nuspec -basepath .\source\Kraken.Brain\bin\$configuration\ -outputdirectory .\build\artifacts -version $build_number -NoPackageAnalysis
    }
}

task PackagePortal {
	write-host "Package Portal"

    $publish_to = resolve-path ".\build\temp\portal"
    exec {
        msbuild .\source\kraken.portal\Kraken.Portal.csproj /t:Rebuild /p:Configuration=$configuration /p:UseWPP_CopyWebApplication=true /p:PipelineDependsOnBuild=false "/p:OutDir=$publish_to\bin" "/p:WebProjectOutputDir=$publish_to\"
    }

    exec {
        & $nuget_path pack $publish_to\Kraken.Portal.nuspec -basepath $publish_to -outputdirectory .\build\artifacts -version $build_number -NoPackageAnalysis
    }
}

## Helpers

task VerifyProperties {
	Assert (![string]::IsNullOrEmpty($build_number)) 'Property build_number was null or empty'

    Write-Output "Build number: $build_number"
}

Import-Module .\tools\psake\teamcity.psm1

TaskSetup {
	if ($env:TEAMCITY_VERSION) {
	    TeamCity-ReportBuildProgress "Running task $($psake.context.Peek().currentTaskName)"	
	}
}
