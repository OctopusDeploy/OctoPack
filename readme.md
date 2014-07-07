**OctoPack** is an open source project that makes it easy to create [Octopus Deploy](http://octopusdeploy.com)-compatible NuGet packages.

Sounds confusing? Well, NuGet was originally designed for packaging up open-source code libraries for developers to use in Visual Studio. And it also happens to be the perfect format for packaging applications that you want to deploy. As we discuss on [the packaging page](http://octopusdeploy.com/documentation/packaging "Packaging NuGet packages for Octopus"), however, some of the default NuGet conventions and assumptions don't work quite so well for tools like Octopus. So to help you create Octopus-ready NuGet packages, we created a tool called **OctoPack**. 

**Please raise and track issues for this project [here](https://github.com/OctopusDeploy/Issues/issues/).**

## Installing OctoPack

Assuming you have an ASP.NET web site or Windows Service C# (or VB.NET) project, creating a NuGet package that works with Octopus is easy. 

1. Ensure you have installed NuGet into your Visual Studio
2. From the View menu, open Other Windows -> Package Manager Console
3. In the Default Project drop down, choose the ASP.NET web site or Windows Service project that you would like to package

Install the OctoPack package by typing:

    Install-Package OctoPack 

You will see output similar to this:

![Installing OctoPack](https://s3.amazonaws.com/octopus-images/doc/octopack/octopack-install.png "Installing OctoPack")
 
## Building packages

To have OctoPack create a NuGet package from your build, set the `RunOctoPack` MSBuild property to `true`. For example, if you are compiling from the command line, you might use:

    msbuild MySolution.sln /t:Build /p:RunOctoPack=true

After the build completes, in the output directory you will find a NuGet package. This package is ready to be deployed using your [Octopus Deploy](http://octopusdeploy.com) server.

## Adding a NuSpec

A `.nuspec` file describes the contents of your NuGet package. OctoPack automatically creates one if you haven't provided one, by guessing some of the settings from your project. But you may wish to provide your own [simple .nuspec file](http://docs.nuget.org/docs/reference/nuspec-reference "NuSpec file format") to your project. The file name should match the name of your C# project - for example, **Sample.Web.nuspec** if your ASP.NET project is named **Sample.Web**.

Here is an example of the .nuspec file contents:

	<?xml version="1.0"?>
	<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
	  <metadata>
	    <id>Sample.Web</id>
	    <title>Your Web Application</title>
	    <version>1.0.0</version>
	    <authors>Your name</authors>
	    <owners>Your name</owners>
	    <licenseUrl>http://yourcompany.com</licenseUrl>
	    <projectUrl>http://yourcompany.com</projectUrl>
	    <requireLicenseAcceptance>false</requireLicenseAcceptance>
	    <description>A sample project</description>
	    <releaseNotes>This release contains the following changes...</releaseNotes>
	  </metadata>
	</package>

## What is packaged?

OctoPack is smart enough to only package files required for deployment. If you are packaging a Windows Service or Console application, then it will package all of the output files in the `bin\Release` folder (assuming you have done a release build). 

![Build action = Content](https://s3.amazonaws.com/octopus-images/doc/octopack/octopack-new-package.png "Build action = Content")

If you need to include other files in your package for deployment, use the Visual Studio properties panel to set the *Copy to Output Directory* attribute to **Copy always**. 

Web applications require additional files to run, such as Razor/ASPX files, configuration files, and assets such as images, CSS and JavaScript files. When packaging a web application, OctoPack will include any files marked with the *Build Action* set to **Content** in the Solution Explorer properties window:

![Build action = Content](https://s3.amazonaws.com/octopus-images/doc/octopack/octopack-file-properties.png "Build action = Content")

When web applications are packaged, only the binaries and content files are included:

![ASP.NET package](https://s3.amazonaws.com/octopus-images/doc/octopack/octopack-new-package-web.png "ASP.NET package")

*(Note: OctoPack won't run web.config transformation files, because [these will be run as part of the deployment](http://octopusdeploy.com/documentation/features/xml-config) instead)*

If you need to go beyond this and include additional files, you can do so using the `<files>` element in your custom NuSpec file. For example:

    <files>
      <file src="bin\*.dll" target="bin" />
      <file src="Content\*.css" target="Content" />
    </files>

_(Note: this only works in OctoPack >= 2.0.24)_

If the `<files>` section exists, OctoPack won't attempt to automatically add any extra files to your package, so you'll need to be explicit about which files you want to include.

See the [NuSpec files reference documentation](http://docs.nuget.org/docs/reference/nuspec-reference#Specifying_Files_to_Include_in_the_Package) for more examples on how to specify which files to include.

## Version numbers

NuGet packages have version numbers. 
When you use OctoPack, the NuGet package version number will come from (in order of priority):

 1. The command line, if you pass `/p:OctoPackPackageVersion=<version>` as an MSBuild parameter when building your project
 2. The `[assembly: AssemblyVersion]` attribute in your `AssemblyInfo.cs` file

## Adding release notes

NuSpec files can contain release notes, which show up on the Octopus Deploy release page. OctoPack can add these notes to your NuGet package if you pass a path to a file containing the notes. For example:

    msbuild MySolution.sln /t:Build /p:RunOctoPack=true /p:OctoPackReleaseNotesFile=..\ReleaseNotes.txt

Note that the file path should always be relative to the C#/VB project file (not the solution file). 

## Publishing

To publish your package to a NuGet feed, you can optionally use some extra MSBuild properties:

 - `/p:OctoPackPublishPackageToFileShare=C:\MyPackages` - copies the package to the path given
 - `/p:OctoPackPublishPackageToHttp=http://my-nuget-server/api/v2/package` - pushes the package to the NuGet server
 - `/p:OctoPackPublishApiKey=ABCDEFGMYAPIKEY` - API key to use when publishing
