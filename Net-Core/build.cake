#tool nuget:?package=NUnit.ConsoleRunner&version=3.6.1
#tool "nuget:?package=GitVersion.CommandLine&version=3.6.5"
#addin "Cake.Compression&version=0.1.4"
#addin "nuget:?package=Cake.Sonar"
#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool"


//////////////////////////////////////////////////////////////////////
// ARGUMENTS
/////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var solution = Argument("solution", "YOUR_SOLUTION_FILE.sln");
var artifactory = Argument("artifactory", "YOUR_ARTIFACTORY");
var configuration = Argument("configuration", "Release");
var PackageOutDir = "./publish";
var company = "YOUR_COMPANY"
//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Change-Assembly-Info")
    .Does(() =>
{
    var versionInfo = GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = false,
        NoFetch = true,
        RepositoryPath = "."
    });

    var assemblyVersion = versionInfo.MajorMinorPatch + '.' + versionInfo.CommitsSinceVersionSource;
    var repoVersion = versionInfo.SemVer + '-' + versionInfo.CommitsSinceVersionSource;
    var versionFileName = Argument("versionFile", @"version.txt");
    System.IO.File.WriteAllText(versionFileName, repoVersion);
    Information("Version: {0}", repoVersion);
    Information("Assembly Version: {0}", assemblyVersion);

    Func<IFileSystemInfo, bool> exclude_packages =
        fileSystemInfo => !fileSystemInfo.Path.FullPath.Contains(
            "/packages/");

    var files = GetFiles("./**/AssemblyInfo.cs", exclude_packages);
    var company = Argument("company", "" + company + " Inc.");
    var copyright = Argument("copyright", "Copyright (c) " + company + "FROM_DATE - " + DateTime.Now.Year);
    var description = Argument("description", company + "'s Platform.");
    var appVersion = Argument("appVersion", assemblyVersion);

    foreach (var file in files) {
        var assemblyInfo = ParseAssemblyInfo(file.FullPath);

        CreateAssemblyInfo(file, new AssemblyInfoSettings {
            CLSCompliant = assemblyInfo.ClsCompliant,
            Company = company,
            ComVisible = assemblyInfo.ComVisible,
            Configuration = configuration,
            Copyright = copyright,
            Description = description,
            FileVersion = appVersion,
            Guid = assemblyInfo.Guid,
            InformationalVersion = appVersion,
            InternalsVisibleTo = assemblyInfo.InternalsVisibleTo,
            Product = assemblyInfo.Product,
            Title = assemblyInfo.Title,
            Trademark = assemblyInfo.Trademark,
            Version = appVersion
        });

        Information(assemblyInfo.Title + " updated to " + appVersion + ".");

    }
});

Task("Clean")
    .Does(() =>
    {
    var cleanSetting =new DotNetCoreCleanSettings
     {
        Framework = "netcoreapp2.0",
        Configuration = "Release",
        Verbosity =  DotNetCoreVerbosity.Minimal
     };
    DotNetCoreClean(".",cleanSetting);

    });

Task("Restore")
    .Does(() => {
        var restoreSetting = new DotNetCoreRestoreSettings
        {
            Verbosity = DotNetCoreVerbosity.Minimal,
            NoCache = true,
            DiagnosticOutput = true
        };
        DotNetCoreRestore(".",restoreSetting);
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() => {
        var buildSettings = new DotNetCoreBuildSettings
     {
        Framework = "netcoreapp2.0",
        Configuration = "Release",
        Verbosity = DotNetCoreVerbosity.Minimal,
        NoIncremental = true ,
        DiagnosticOutput = true,
        MSBuildSettings = new DotNetCoreMSBuildSettings
        {
            DiagnosticOutput = true,
            Verbosity = DotNetCoreVerbosity.Minimal
        }
     };
	 DotNetCoreBuild(".",buildSettings);
     });

Task("Tests")
     .Does(() =>
    {
     var settings = new DotNetCoreTestSettings
     {
        Framework = "netcoreapp2.0",
        Configuration = "Release",
        Verbosity =  DotNetCoreVerbosity.Minimal
     };
    var testProjectFiles = GetFiles("./*.Test*/*.csproj");
    foreach(var file in testProjectFiles)
    {
        Information("Tests project file find : ");
        Information(file.FullPath);
        DotNetCoreTest(file.FullPath, settings);

    }
    });

Task("Scan")
    .IsDependentOn("SonarBegin")
    .IsDependentOn("Build")
    .IsDependentOn("SonarEnd");

// SONARQUBE TESTS STEP
Task("SonarBegin")
    .Does(() => {
        var login = Argument("SONAR_LOGIN", "");
        var url = Argument("SONAR_URL", "");
        var versionInfo = GitVersion(new GitVersionSettings {
            UpdateAssemblyInfo = false,
            NoFetch = true,
            RepositoryPath = "."
        });
        var version = versionInfo.SemVer + '-' + versionInfo.CommitsSinceVersionSource;

        SonarBegin(new SonarBeginSettings{
            Name = "IPS",
            Key = "com."+ company +".dotnet:IPS",
            Url = url,
            Login = login,
            Version = version
        });

    });

Task("SonarEnd")
    .Does(() => {
        var login = Argument("SONAR_LOGIN", "");

        SonarEnd(new SonarEndSettings{
            Login = login
        });
    });

Task("Publish")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() => {

        if (DirectoryExists(PackageOutDir))
        {
            DeleteDirectory(PackageOutDir, new DeleteDirectorySettings {
                Recursive = true,
                Force = true
            });
        }
        var settings = new DotNetCorePublishSettings
        {
            Framework = "netcoreapp2.0",
            Configuration = "Release",
            OutputDirectory  = PackageOutDir,
        };

        DotNetCorePublish("./YOUR_WEBSERVICES_FILE.WebServices/", settings);

    });

Task("Package-Publish-For-Windows-IIS")
    .IsDependentOn("Publish")
    .Does(() => {

        DeleteFile(PackageOutDir+"/appsettings.json");
        var fileName = solution + ".zip";
        var package = Argument("package",fileName);
        Zip(PackageOutDir, package);
    });

Task("Package-Publish")
    .IsDependentOn("Publish")
    .Does(() => {
        var package = Argument("package",solution + ".zip");
        Zip(PackageOutDir, package);

    });

Task("IIS-Package")
    .IsDependentOn("Package-Publish-For-Windows-IIS");


Task("Generate-Artifacts")
    .IsDependentOn("Publish");


Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Publish")

    ;

RunTarget(target);