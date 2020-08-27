param($installPath, $toolsPath, $package, $project)

  # Need to load MSBuild assembly if it's not loaded yet.
  Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

  # Grab the loaded MSBuild project for the project
  $msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName) | Select-Object -First 1

  # Find all the imports and targets added by this package.
  $itemsToRemove = @()

  # Allow many in case a past package was incorrectly uninstalled
  $itemsToRemove += $msbuild.Xml.Imports | Where-Object { $_.Project -imatch "$($package.Id)\.targets`$" }
  $itemsToRemove += $msbuild.Xml.Targets | Where-Object { $_.Name -eq "EnsureOctoPackImported" }

  $saveProject = $false

  # Remove the elements and save the project
  $saveProject = ($itemsToRemove -and $itemsToRemove.length)
  $itemsToRemove | ForEach-Object { <#$null = #> $msbuild.Xml.RemoveChild($_) }

  $msbuild.Xml.Targets |
      Where-Object { $_.Name -eq "EnsureNuGetPackageBuildImports" } |
      ForEach-Object {
          $target = $_;
          $target.Children |
              Where-Object { $_.Condition -imatch "$($package.Id)\.targets" } |
              ForEach-Object {
                  # This only gets evaluated if the collection has items to enumerate over...
                  if (!$saveProject) { $saveProject = $true }
                  $target.RemoveChild($_)
              }
      }

  if ($saveProject)
  {
      $isFSharpProject = ($project.Type -eq "F#")
      if ($isFSharpProject)
      {
          $project.Save("")
      }
      else
      {
          $project.Save()
      }
  }