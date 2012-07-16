**OctoPack is an open source project that makes it easy to create [Octopus Deploy](http://octopusdeploy.com) compatible NuGet packages.**

Sounds confusing? Well, NuGet was originally designed for packaging up open-source code libraries for developers to use in Visual Studio. And it also happens to be the perfect format for packaging applications that you want to deploy. As we discuss on [the packaging page](http://octopusdeploy.com/documentation/packaging "Packaging NuGet packages for Octopus"), however, some of the default NuGet conventions and assumptions don't work quite so well for tools like Octopus. So to help you create Octopus-ready NuGet packages, we created a tool called **OctoPack**. 

## Configuring OctoPack

Assuming you have an ASP.NET web site or Windows Service C# (or VB.NET) project, creating a NuGet package that works with Octopus is easy. 

1. Ensure you have installed NuGet into your Visual Studio
2. From the View menu, open Other Windows -> Package Manager Console
3. In the Default Project drop down, choose the ASP.NET web site or Windows Service project that you would like to package

Install the OctoPack package by typing:

    Install-Package OctoPack 

You will see output similar to this:

![Installing OctoPack](https://octopus-images.s3.amazonaws.com/blog/install-package.png "Installing OctoPack")

## Adding a NuSpec

Before you can use OctoPack, you'll also have to add a [simple .nuspec file](http://docs.nuget.org/docs/reference/nuspec-reference "NuSpec file format") to your project. The file name should be the same as the name for your project - for example, **YourApp.Web.nuspec**.

Here is an example of the .nuspec file contents:

	<?xml version="1.0"?>
	<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
	  <metadata>
	    <id>YourApp.Web</id>
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
 
## Building packages

Now change to Release mode, and compile your project. 

After the build completes, in the output directory you will find a NuGet package. This package is ready to install into Octopus!

## Web Application Publishing

While simply compiling in release mode works for Windows Services and other applications, there is a small problem for ASP.NET applications produced this way.

On closer inspection, the package produced from an ASP.NET application includes content files that you wouldn't normally want to deploy - for example, we wouldn't normally deploy .cs and .csproj files. 

![Package contents](https://octopus-images.s3.amazonaws.com/blog/package-contents.png "Package contents")

This can be solved by performing a "publish" from the command line with the following arguments:

    msbuild MyWebApplication.csproj "/t:Rebuild" "/t:ResolveReferences" "/t:_CopyWebApplication" /p:Configuration=Release /p:WebProjectOutputDir=publish\ /p:OutDir=publish\bin\

This compiles the application in release mode, but also performs a "publish". The properties WebProjectOutputDir and OutDir can be anything you like, but they **must** end with a backslash. 

After using the above command, we'll now find that our NuGet package contains only the files we expect to deploy:

![Package with content files](https://octopus-images.s3.amazonaws.com/blog/simpler-package.png "Package with content files")

## Version numbers

When you use OctoPack, the NuGet package version number will come from (in order of priority):

 1. The command line, if you pass `/p:OctopusPackageVersion=<version>` as an MSBuild parameter when building your project
 2. The `[assembly: AssemblyVersion]` attribute in your `AssemblyInfo.cs` file

## Publishing

To publish your package to a NuGet feed, you can optionally use some extra MSBuild properties:

 - `/p:OctopusPublishPackageToFileShare=C:\MyPackages` - copies the package to the path given
 - `/p:OctopusPublishPackageToHttp=http://my-nuget-server/api/v2/package` - pushes the package to the NuGet server
 - `/p:OctopusPublishApiKey=ABCDEFGMYAPIKEY` - API key to use when publishing
 
## From your Automated Build Server

Of course no one wants to produce production deployment packages from their development machine, so you will probably want to hook this up in your build server. 

For Windows Services/console applications you will just need to make sure your build server is compiling the project in Release mode via MSBuild. This is enough for OctoPack to produce the package.

For web applications as described above, you will need to find a way to force the "publish" to happen. From TeamCity, you can do this manually by adding build parameters. For TFS users, TFS automatically publishes ASP.NET websites into a _PublishedWebSites folder, so this should be automatic.  