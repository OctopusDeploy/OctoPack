$Version = 1

do {
    $Version = $Version + 1
} while ( Test-Path (".\build\OctoPack.1.0." + $Version + ".nupkg" ) )

.\tools\NuGet.exe pack source\OctoPack.nuspec -OutputDirectory build -Version ("1.0." + $Version) -BasePath source

# .\tools\NuGet.exe push (".\build\OctoPack.1.0." + $Version + ".nupkg") "31f41c07-78c7-4f87-adb4-b3bd8aabb992"
