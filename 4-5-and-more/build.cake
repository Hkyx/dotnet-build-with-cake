#tool nuget:?package=NUnit.ConsoleRunner
#tool nuget:?package=NUnit.Extension.NUnitV2Driver
#tool nuget:?package=NUnit.Extension.NUnitV2ResultWriter
#tool "nuget:?package=GitVersion.CommandLine&version=3.6.5"
#tool "nuget:?package=NuGet.CommandLine&version=4.5.1"
#addin "Cake.Compression&version=0.1.4"
/////////////////////////////////////
/////////// GLOBAL VARS /////////////
/////////////////////////////////////
var target = Argument("target", "Default");
var solution = Argument("solution", "YOUR_SOLUTION.sln");
var artifactory = Argument("artifactory", "YOUR ARTEFACTORY URL");
var configuration = Argument("configuration", "Release");
var APPLICATION_NAME = "your_application_name"
var YOUR_COMPANY = "YOUR_COMPANY_NAME"

////////////////////////////////
/////////// TASKS  /////////////
////////////////////////////////
Task("Change-Assembly-Info")
   .Does(() =>
{
    var versionInfo = GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = false,
        NoFetch = true,
        RepositoryPath = "."
    });

    var assemblyVersion = versionInfo.MajorMinorPatch + "." + versionInfo.Sha.Substring(0, 6);
    var versionFileName = Argument("versionFile", @"version.txt");
    System.IO.File.WriteAllText(versionFileName, assemblyVersion);
    Information("Version: {0}", assemblyVersion);
    Information("Assembly Version: {0}", assemblyVersion);

    Func<IFileSystemInfo, bool> exclude_packages =
        fileSystemInfo => !fileSystemInfo.Path.FullPath.Contains(
            "/packages/");

    var files = GetFiles("./Build/AssemblyVersion.cs", exclude_packages);
    var company = Argument("company",  + YOUR COMPANY + " Inc.");
    var copyright = Argument("copyright", "Copyright (c) " + YOUR COMPANY + " Inc. FROM_DATE - " + DateTime.Now.Year);
    var description = Argument("description", "YOUR DESCRIPTION");
    var appVersion = Argument("appVersion", versionInfo.AssemblySemVer);

    foreach (var file in files) {
        var assemblyInfo = ParseAssemblyInfo(file.FullPath);
        
        CreateAssemblyInfo(file, new AssemblyInfoSettings {
            Company = company,
            Copyright = copyright,
            FileVersion = appVersion,
            InformationalVersion = appVersion,
            InternalsVisibleTo = assemblyInfo.InternalsVisibleTo,
            Product = assemblyInfo.Product,
            Trademark = assemblyInfo.Trademark,
            Version = appVersion
        });

        Information(assemblyInfo.Title + " updated to " + appVersion + ".");
    }
});

Task("NuGet-Restore")
    .Does(() =>
{
    NuGetRestore("./" + solution, new NuGetRestoreSettings {
        Source = new [] { artifactory }
    });
}); 

Task("Compile")
    .IsDependentOn("NuGet-Restore")
    .Does(() =>
{
    MSBuild("./" + solution, new MSBuildSettings {
         Configuration = configuration,
         Verbosity = Verbosity.Minimal
    }.WithProperty("OutDir", new []{"../webapp"}));
}); 


Task("Build-Webapp")
    .IsDependentOn("Compile");

Task("Compress-Webapp")
    .IsDependentOn("Build-Webapp")
    .Does(() =>
{
   var fileName = APPLICATION_NAME + ".zip";
   if(FileExists(fileName)) DeleteFile(fileName);
   var files = GetFiles("./YOUR_SOURCE_FILES");
   Zip("./", fileName, files);

 });


RunTarget(target);