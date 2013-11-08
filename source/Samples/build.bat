set path=%path%;C:\Program Files (x86)\MSBuild\12.0\Bin\amd64
set path=%path%;C:\Windows\Microsoft.NET\Framework64\v4.0.30319
msbuild Samples.sln /t:Build /p:Configuration=Release /p:RunOctoPack=true