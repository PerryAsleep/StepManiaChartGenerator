﻿using System.Diagnostics;
using System.Reflection;

const string fumenDevenv = "FUMEN_DEVENV";
const string fumen7Z = "FUMEN_7Z";

const string appName = "StepManiaChartGenerator";
const string projectName = "StepManiaChartGenerator";
const string readmeName = "Readme.md";
const string relativeProjectRoot = "..\\..\\..\\..";
const string relativeReadme = $"..\\..\\..\\..\\{readmeName}";
const string relativeDocsPath = $"..\\..\\..\\..\\{appName}\\docs";
const string relativeLibraryDocsPath = "..\\..\\..\\..\\StepManiaLibrary\\StepManiaLibrary\\docs";
const string relativeSlnPath = $"{relativeProjectRoot}\\{appName}.sln";
const string relativeExeFolderPath = $"{relativeProjectRoot}\\{projectName}\\bin\\Release";
const string relativeExePath = $"{relativeExeFolderPath}\\{appName}.exe";
const string relativeReleasesFolderPath = $"{relativeProjectRoot}\\Releases";

// Replacements in the Readme document, which will be one level up from docs/
Dictionary<string, string> readmeDocumentationReplacements = new()
{
	{ "(https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/", "(docs/StepManiaLibrary/" },
	{ "(https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/", "(docs/StepManiaLibrary/" },
	{ "(https://perryasleep.github.io/StepManiaChartGenerator/StepManiaChartGenerator/docs/", "(docs/" },
	{ "(StepManiaChartGenerator/docs/", "(docs/" },
	{ "(StepManiaLibrary/StepManiaLibrary/docs/", "(docs/StepManiaLibrary/" },
	{ "<img src=\"StepManiaChartGenerator/docs/", "<img src=\"docs/" },
};

// Replacements for files in the docs/ folder
Dictionary<string, string> docsDocumentationReplacements = new()
{
	{ "https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/", "StepManiaLibrary/" },
	{ "https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/", "StepManiaLibrary/" },
	{ "(https://perryasleep.github.io/StepManiaChartGenerator/StepManiaChartGenerator/docs/", "(" },
	{ "(StepManiaChartGenerator/docs/", "(" },
	{ "(../../StepManiaLibrary/docs/", "(StepManiaLibrary/" },
};

static void CopyDirectory(string sourceDir, string destinationDir,
	IReadOnlyDictionary<string, string> documentationReplacements = null)
{
	var dir = new DirectoryInfo(sourceDir);
	if (!dir.Exists)
		return;

	var subDirectories = dir.GetDirectories();
	Directory.CreateDirectory(destinationDir);

	// Copy files.
	foreach (var fileInfo in dir.GetFiles())
	{
		var targetFilePath = Path.Combine(destinationDir, fileInfo.Name);
		fileInfo.CopyTo(targetFilePath);

		// Update documentation if this is a markdown file.
		if (documentationReplacements != null && fileInfo.Extension.Equals(".md"))
		{
			ReplaceTextInFile(targetFilePath, documentationReplacements);
		}
	}

	// Recursively copy directories.
	foreach (var subDirectory in subDirectories)
	{
		var newDestinationDir = Path.Combine(destinationDir, subDirectory.Name);
		CopyDirectory(subDirectory.FullName, newDestinationDir, documentationReplacements);
	}
}

static void ReplaceTextInFile(string fileName, IReadOnlyDictionary<string, string> replacements)
{
	if (replacements == null || replacements.Count == 0)
		return;

	var text = File.ReadAllText(fileName);
	foreach (var replacement in replacements)
	{
		text = text.Replace(replacement.Key, replacement.Value);
	}

	File.WriteAllText(fileName, text);
}

var devEnvPath = Environment.GetEnvironmentVariable(fumenDevenv);
if (string.IsNullOrEmpty(devEnvPath))
{
	Console.WriteLine(
		$"{fumenDevenv} is not defined. Please set {fumenDevenv} in your environment variables to the path of your devenv.exe executable.");
	return 1;
}

var sevenZipPath = Environment.GetEnvironmentVariable(fumen7Z);
if (string.IsNullOrEmpty(sevenZipPath))
{
	Console.WriteLine(
		$"{fumen7Z} is not defined. Please set {fumen7Z} in your environment variables to the path of your 7z.exe executable.");
	return 1;
}

// Rebuild project.
Console.WriteLine($"Rebuilding {projectName} project.");
var process = new Process();
process.StartInfo.FileName = devEnvPath;
process.StartInfo.Arguments = $"{relativeSlnPath} /Rebuild Release /Project {projectName}";
process.Start();
process.WaitForExit();
if (process.ExitCode != 0)
{
	Console.WriteLine($"Rebuilding {projectName} failed with error code {process.ExitCode}.");
	return 1;
}

// Get version.
var version = AssemblyName.GetAssemblyName(relativeExePath).Version;
if (version == null)
{
	Console.WriteLine($"Unable to determine {appName} version.");
	return 1;
}

Console.WriteLine($"{appName} version is {version.Major}.{version.Minor}.{version.Build}.");

// Copy Release product to temp directory with desired folder name for packaging.
var tempDirectory = Path.Combine(Path.GetTempPath(), appName);
Console.WriteLine($"Creating temporary directory: {tempDirectory}");
if (Directory.Exists(tempDirectory))
	Directory.Delete(tempDirectory, true);
Directory.CreateDirectory(tempDirectory);
Console.WriteLine("Copying Release product to temporary directory.");
CopyDirectory(relativeExeFolderPath, tempDirectory);

// Copy documentation.
var destinationReadme = Path.Combine(tempDirectory, readmeName);
File.Copy(relativeReadme, destinationReadme);
ReplaceTextInFile(destinationReadme, readmeDocumentationReplacements);

var destinationDocsDir = Path.Combine(tempDirectory, "docs");
Directory.CreateDirectory(destinationDocsDir);
CopyDirectory(relativeDocsPath, destinationDocsDir, docsDocumentationReplacements);

var destinationDocsLibraryDir = Path.Combine(destinationDocsDir, "StepManiaLibrary");
Directory.CreateDirectory(destinationDocsLibraryDir);
CopyDirectory(relativeLibraryDocsPath, destinationDocsLibraryDir);

// Remove existing release package
var packageFile = $"{relativeReleasesFolderPath}\\{appName}-v{version.Major}.{version.Minor}.{version.Build}.zip";
if (File.Exists(packageFile))
{
	Console.WriteLine($"Deleting existing package file: {packageFile}");
	File.Delete(packageFile);
}

// Package.
Console.WriteLine($"Archiving to: {packageFile}");
process = new Process();
process.StartInfo.FileName = sevenZipPath;
process.StartInfo.Arguments = $"a {packageFile} \"{tempDirectory}\"";
process.Start();
process.WaitForExit();
if (process.ExitCode != 0)
{
	Console.WriteLine($"Packaging {appName} failed with error code {process.ExitCode}.");
	return 1;
}

Console.WriteLine("Done.");
return 0;