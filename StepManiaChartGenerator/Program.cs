﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static StepManiaLibrary.Constants;
using StepManiaLibrary.PerformedChart;
using static Fumen.Converters.SMCommon;
using Fumen;
using Fumen.Converters;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fumen.ChartDefinition;
using StepManiaLibrary;
using System.Reflection;

namespace StepManiaChartGenerator;

/// <summary>
/// StepManiaChartGenerator Program.
/// Generates charts for stepmania files based on Config settings.
/// See Config for configuring Program behavior.
/// </summary>
public class Program
{
	/// <summary>
	/// Format for recording the Version into a Chart's Description field.
	/// </summary>
	private const string FumenGeneratedFormattedVersion = "[FG v{0}]";

	/// <summary>
	/// Regular expression for parsing the deprecated version out of a Chart's Description field.
	/// </summary>
	private const string FumenGeneratedFormattedDeprecatedVersionRegexPattern = @"^\[FG v([0-9]+)\.([0-9]+)\].*";

	/// <summary>
	/// Regular expression for parsing the semantic version out of a Chart's Description field.
	/// </summary>
	private const string FumenGeneratedFormattedSemanticVersionRegexPattern = @"^\[FG v([0-9]+)\.([0-9]+)\.([0-9]+)\].*";

	/// <summary>
	/// Tag for logging messages.
	/// </summary>
	private const string LogTag = "Main";

	/// <summary>
	/// StepGraph to use for parsing input Charts.
	/// </summary>
	private static StepGraph InputStepGraph;

	/// <summary>
	/// StepGraph to use for generating output Charts.
	/// </summary>
	private static StepGraph OutputStepGraph;

	/// <summary>
	/// GraphNodes to use as roots for trying to write output Charts.
	/// Outer List is sorted by preference of Nodes at that level. Level 0 contains the most preferable root GraphNodes.
	/// Inner Lists contain all GraphNodes of equal preference.
	/// Example for a doubles StepGraph:
	///  Tier 0 is both middles.
	///  Tier 1 is one middle and one up/down.
	///  etc.
	/// </summary>
	private static readonly List<List<GraphNode>> OutputStartNodes = new();

	/// <summary>
	/// StepTypeFallbacks to use for PerformedCharts.
	/// </summary>
	private static StepTypeFallbacks StepTypeFallbacks;

	/// <summary>
	/// Supported file formats for reading and writing.
	/// </summary>
	private static readonly List<FileFormatType> SupportedFileFormats = new()
		{ FileFormatType.SM, FileFormatType.SSC };

	/// <summary>
	/// Time of the start of the export.
	/// </summary>
	private static DateTime ExportTime;

	/// <summary>
	/// Application Version.
	/// </summary>
	private static SemanticVersion Version;

	/// <summary>
	/// Formatted version string for recording in a Chart's Description field.
	/// </summary>
	private static string FormattedVersionString;

	/// <summary>
	/// Directory to record visualizations for this export.
	/// Export visualization directories are based on the ExportTime.
	/// </summary>
	private static string VisualizationDir;

	/// <summary>
	/// Whether or not the application can output visualizations.
	/// True if Config specifies to output visualizations and the StepsTypes are supported by
	/// the Visualizer.
	/// </summary>
	private static bool CanOutputVisualizations;

	/// <summary>
	/// HashSet for keeping track of which song directories have had their non-chart files copied.
	/// Songs may have multiple song files (e.g. an sm and an ssc file). We want to only copy
	/// non-chart files once per song.
	/// </summary>
	private static readonly HashSet<string> CopiedDirectories = new();

	/// <summary>
	/// Class for holding arguments for an individual song being processed and for
	/// managing the task to convert that song's charts.
	/// </summary>
	private class SongTaskData
	{
		/// <summary>
		/// FileInfo for the Song file.
		/// </summary>
		public readonly FileInfo FileInfo;

		/// <summary>
		/// String path of directory containing the Song file.
		/// </summary>
		public readonly string CurrentDir;

		/// <summary>
		/// String path to the Song file relative to the Config InputDirectory.
		/// </summary>
		public readonly string RelativePath;

		/// <summary>
		/// String path to the directory to save the Song file to.
		/// </summary>
		public readonly string SaveDir;

		/// <summary>
		/// Function for declaring without starting an async task to process the song.
		/// </summary>
		private readonly Func<Task> TaskFunc;

		/// <summary>
		/// Size of the song file in bytes.
		/// </summary>
		private readonly long SongSize;

		/// <summary>
		/// Async task for processing the song.
		/// </summary>
		private Task Task;

		/// <summary>
		/// Total number of charts converted for the song.
		/// </summary>
		private int NumConvertedCharts;

		public SongTaskData(FileInfo fileInfo, string currentDur, string relativePath, string saveDir)
		{
			FileInfo = fileInfo;
			CurrentDir = currentDur;
			RelativePath = relativePath;
			SaveDir = saveDir;
			SongSize = FileInfo.Length;
			TaskFunc = async () => await ProcessSong(this);
		}

		public void Start()
		{
			Task = TaskFunc();
		}

		public bool IsDone()
		{
			return Task?.IsCompleted ?? false;
		}

		public long GetSize()
		{
			return SongSize;
		}

		public void AddConvertedChartCount(int count)
		{
			NumConvertedCharts += count;
		}

		public int GetNumConvertedCharts()
		{
			return NumConvertedCharts;
		}
	}

	/// <summary>
	/// Main entry point into the program.
	/// </summary>
	/// <remarks>See Config for configuration.</remarks>
	private static async Task Main()
	{
		ExportTime = DateTime.Now;

		// Create a temporary logger for logging exceptions from loading Config.
		Logger.StartUp(new Logger.Config
		{
			WriteToConsole = true,
			Level = LogLevel.Error,
		});

		// Load Config.
		var config = await Config.Load();
		if (config == null)
			Exit(false);

		// Create the logger as soon as possible. We need to load Config first for Logger configuration.
		var loggerSuccess = CreateLogger();

		// Validate Config, even if creating the logger failed. This will still log errors to the console.
		// ReSharper disable once PossibleNullReferenceException
		if (!config.Validate() || !loggerSuccess)
			Exit(false);

		// Determine the application version.
		if (!DetermineVersion())
			Exit(false);

		// Set whether we can output visualizations based on the configured StepsTypes.
		SetCanOutputVisualizations();
		// Create a directory for outputting visualizations.
		if (!InitializeVisualizationDir())
			Exit(false);

		// Create StepGraphs.
		var stepGraphCreationSuccess = await LoadPadDataAndStepGraphs();
		if (!stepGraphCreationSuccess)
			Exit(false);

		// Load the default StepTypeFallbacks.
		if (!InputStepGraph.PadData.CanFitWithin(OutputStepGraph.PadData))
		{
			StepTypeFallbacks = await StepTypeFallbacks.Load(StepTypeFallbacks.DefaultFallbacksFileName);
			if (StepTypeFallbacks == null)
				Exit(false);
		}

		// Find and process all charts.
		FindAndProcessCharts();

		LogInfo("Done.");
		Exit(true);
	}

	/// <summary>
	/// Exits the application.
	/// Will wait for input to close if configured to do so.
	/// </summary>
	/// <param name="bSuccess">
	/// If true then the application will exit with a 0 status code.
	/// If false, the application will exit with a 1 status code.
	/// </param>
	private static void Exit(bool bSuccess)
	{
		Logger.Shutdown();
		if (!(Config.Instance?.CloseAutomaticallyWhenComplete ?? false))
			Console.ReadLine();
		Environment.Exit(bSuccess ? 0 : 1);
	}

	/// <summary>
	/// Determines the application version from the executing assembly.
	/// Sets Version and FormattedVersionString.
	/// </summary>
	/// <returns>True if the version was determined successfully and false otherwise.</returns>
	private static bool DetermineVersion()
	{
		try
		{
			var assembly = Assembly.GetExecutingAssembly();
			var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
			Version = new SemanticVersion(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart,
				fileVersionInfo.FileBuildPart);
			FormattedVersionString = string.Format(FumenGeneratedFormattedVersion, Version);
			return true;
		}
		catch (Exception e)
		{
			LogError($"Failed to determine application version: {e}");
		}

		return false;
	}

	/// <summary>
	/// Loads PadData and creates the InputStepGraph and OutputStepGraph.
	/// </summary>
	/// <returns>
	/// True if no errors were generated and false otherwise.
	/// </returns>
	private static async Task<bool> LoadPadDataAndStepGraphs()
	{
		// If the types are the same, just create one StepGraph.
		if (Config.Instance.InputChartType == Config.Instance.OutputChartType)
		{
			// Load the input PadData.
			var padData = await LoadPadData(Config.Instance.InputChartType);
			if (padData == null)
				return false;

			// Create the StepGraph and use it for both the InputStepGraph and the OutputStepGraph.
			LogInfo("Loading StepGraph.");
			InputStepGraph = await LoadStepGraph(Config.Instance.InputChartType, padData);
			OutputStepGraph = InputStepGraph;
			if (!CreateOutputStartNodes())
				return false;
			LogInfo("Finished loading StepGraph.");
		}

		// If the types are separate, create two graphs.
		else
		{
			// Load the PadData for both the input and output type.
			var inputPadDataTask = LoadPadData(Config.Instance.InputChartType);
			var outputPadDataTask = LoadPadData(Config.Instance.OutputChartType);
			await Task.WhenAll(inputPadDataTask, outputPadDataTask);
			var inputPadData = await inputPadDataTask;
			var outputPadData = await outputPadDataTask;
			if (inputPadData == null || outputPadData == null)
				return false;

			LogInfo("Loading StepGraphs.");

			// Create each graph on their own thread as these can take a few seconds.
			var inputGraphTask = LoadStepGraph(Config.Instance.InputChartType, inputPadData);
			var outputGraphTask = LoadStepGraph(Config.Instance.OutputChartType, outputPadData);
			await Task.WhenAll(inputGraphTask, outputGraphTask);
			InputStepGraph = await inputGraphTask;
			OutputStepGraph = await outputGraphTask;
			if (!CreateOutputStartNodes())
				return false;
			LogInfo("Finished creating StepGraphs.");
		}

		return true;
	}

	/// <summary>
	/// Loads PadData for the given stepsType.
	/// </summary>
	/// <param name="stepsType">Stepmania StepsType to load PadData for.</param>
	/// <returns>Loaded PadData or null if any errors were generated.</returns>
	private static async Task<PadData> LoadPadData(string stepsType)
	{
		var fileName = $"{stepsType}.json";
		LogInfo($"Loading PadData from {fileName}.");
		var padData = await PadData.LoadPadData(stepsType, fileName);
		if (padData == null)
			return null;
		LogInfo($"Finished loading {stepsType} PadData.");
		return padData;
	}

	/// <summary>
	/// Loads the StepGraph for the given stepsType.
	/// </summary>
	/// <param name="stepsType">Stepmania StepsType to load the StepGraph for.</param>
	/// <param name="padData">PadData for the given StepsType.</param>
	/// <returns>Loaded StepGraph or null if any errors were generated.</returns>
	private static async Task<StepGraph> LoadStepGraph(string stepsType, PadData padData)
	{
		var fileName = $"{stepsType}.fsg";
		LogInfo($"Loading StepGraph from {fileName}.");
		var stepGraph = await StepGraph.LoadAsync(fileName, padData);
		if (stepGraph == null)
			return null;
		LogInfo($"Finished loading {stepsType} StepGraph.");
		return stepGraph;
	}

	/// <summary>
	/// Creates the output start nodes and stores them in OutputStartNodes.
	/// </summary>
	/// <returns>
	/// True if all output start nodes were added successfully and false otherwise.
	/// </returns>
	private static bool CreateOutputStartNodes()
	{
		// Add the root node as the first tier.
		OutputStartNodes.Add(new List<GraphNode> { OutputStepGraph.GetRoot() });

		// Loop over the remaining tiers.
		for (var tier = 1; tier < OutputStepGraph.PadData.StartingPositions.Length; tier++)
		{
			var nodesAtTier = new List<GraphNode>();
			foreach (var pos in OutputStepGraph.PadData.StartingPositions[tier])
			{
				var node = OutputStepGraph.FindGraphNode(pos[L], GraphArrowState.Resting, pos[R], GraphArrowState.Resting);
				if (node == null)
				{
					LogError(
						$"Could not find a node in the {Config.Instance.OutputChartType} StepGraph for StartingPosition with"
						+ $" left on {pos[L]} and right on {pos[R]}.");
					return false;
				}

				nodesAtTier.Add(node);
			}

			OutputStartNodes.Add(nodesAtTier);
		}

		return true;
	}

	/// <summary>
	/// Creates the Logger for the application.
	/// </summary>
	/// <returns>True if successful and false if any error occurred.</returns>
	private static bool CreateLogger()
	{
		try
		{
			var config = Config.Instance.LoggerConfig;
			if (config.LogToFile)
			{
				Directory.CreateDirectory(config.LogDirectory);
				var logFileName = "StepManiaChartGenerator " + ExportTime.ToString("yyyy-MM-dd HH-mm-ss") + ".log";
				var logFilePath = Fumen.Path.Combine(config.LogDirectory, logFileName);
				Logger.StartUp(new Logger.Config
				{
					Level = config.LogLevel,
					WriteToConsole = config.LogToConsole,
					WriteToFile = config.LogToFile,
					LogFilePath = logFilePath,
					LogFileFlushIntervalSeconds = config.LogFlushIntervalSeconds,
					LogFileBufferSizeBytes = config.LogBufferSizeBytes,
					WriteToBuffer = false,
				});
			}
			else if (config.LogToConsole)
			{
				Logger.StartUp(new Logger.Config
				{
					Level = config.LogLevel,
					WriteToConsole = true,
					WriteToFile = false,
					WriteToBuffer = false,
				});
			}
		}
		catch (Exception e)
		{
			LogError($"Failed to create Logger. {e}");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Sets CanOutputVisualizations based on whether OutputVisualizations is configured and
	/// the configured StepsTypes are supported.
	/// </summary>
	private static void SetCanOutputVisualizations()
	{
		if (Config.Instance.OutputVisualizations)
		{
			CanOutputVisualizations = true;
			if (!Visualizer.IsStepsTypeSupported(Config.Instance.InputChartType))
			{
				LogWarn($"{Config.Instance.InputChartType} is not currently supported for outputting visualizations."
				        + " Visualization output will be skipped.");
				CanOutputVisualizations = false;
			}
			else if (Config.Instance.OutputChartType != Config.Instance.InputChartType
			         && !Visualizer.IsStepsTypeSupported(Config.Instance.OutputChartType))
			{
				LogWarn($"{Config.Instance.InputChartType} is not currently supported for outputting visualizations."
				        + " Visualization output will be skipped.");
				CanOutputVisualizations = false;
			}
		}
	}

	/// <summary>
	/// If configured to write visualizations, initializes the configured directory.
	/// Creates the directory if it does not exist, and copies src assets to it.
	/// </summary>
	/// <returns>True if no errors and false otherwise.</returns>
	private static bool InitializeVisualizationDir()
	{
		if (Config.Instance.OutputVisualizations && CanOutputVisualizations)
		{
			try
			{
				var visualizationSubDir = ExportTime.ToString("yyyy-MM-dd HH-mm-ss");
				VisualizationDir = Config.Instance.VisualizationsDirectory;
				VisualizationDir = Fumen.Path.Combine(VisualizationDir, visualizationSubDir);
				LogInfo($"Initializing directory for outputting visualizations: {VisualizationDir}.");
				Visualizer.InitializeVisualizationDir(VisualizationDir);
			}
			catch (Exception e)
			{
				LogError($"Failed to initialize directory for outputting visualizations. {e}");
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Searches for songs matching Config parameters and processes each.
	/// Will add charts, copy the charts and non-chart files to the output directory,
	/// and write visualizations for the conversion.
	/// </summary>
	private static void FindAndProcessCharts()
	{
		if (!Directory.Exists(Config.Instance.InputDirectory))
		{
			LogError($"Could not find InputDirectory \"{Config.Instance.InputDirectory}\".");
			return;
		}

		var songTasks = new List<SongTaskData>();
		var totalSongBytes = 0L;

		var pathSep = System.IO.Path.DirectorySeparatorChar.ToString();

		LogInfo($"Searching for songs in \"{Config.Instance.InputDirectory}\"...");

		// Search through the configured InputDirectory and all subdirectories.
		var dirs = new Stack<string>();
		dirs.Push(Config.Instance.InputDirectory);
		while (dirs.Count > 0)
		{
			// Get the directory to process.
			var currentDir = dirs.Pop();

			// Get sub directories for the next loop.
			try
			{
				var subDirs = Directory.GetDirectories(currentDir);
				// Reverse sort the subdirectories since we use a queue to pop.
				// Sorting helps the user get a rough idea of progress, and makes it easier to tell if a song pack is complete.
				Array.Sort(subDirs, (a, b) => string.Compare(b, a, StringComparison.CurrentCultureIgnoreCase));
				foreach (var str in subDirs)
					dirs.Push(str);
			}
			catch (Exception e)
			{
				LogWarn($"Could not get directories in \"{currentDir}\". {e}");
				continue;
			}

			// Get all files in this directory.
			string[] files;
			try
			{
				files = Directory.GetFiles(currentDir);
			}
			catch (Exception e)
			{
				LogWarn($"Could not get files in \"{currentDir}\". {e}");
				continue;
			}

			// Cache some paths needed for processing the charts.
			var relativePath = currentDir.Substring(
				Config.Instance.InputDirectory.Length,
				currentDir.Length - Config.Instance.InputDirectory.Length);
			if (relativePath.StartsWith(pathSep))
				relativePath = relativePath.Substring(1, relativePath.Length - 1);
			if (!relativePath.EndsWith(pathSep))
				relativePath += pathSep;
			var saveDir = Fumen.Path.Combine(Config.Instance.OutputDirectory, relativePath);

			// Check each file.
			var hasSong = false;
			foreach (var file in files)
			{
				// Get the FileInfo for this file so we can check its name.
				FileInfo fi;
				try
				{
					fi = new FileInfo(file);
				}
				catch (Exception e)
				{
					LogWarn($"Could not get file info for \"{file}\". {e}");
					continue;
				}

				// Check that this is a supported file format.
				var fileFormat = FileFormat.GetFileFormatByExtension(fi.Extension);
				if (fileFormat == null || !SupportedFileFormats.Contains(fileFormat.Type))
					continue;

				// Check if the matches the expression for files to convert.
				if (!Config.Instance.InputNameMatches(fi.Name))
					continue;

				// Create the save directory before starting any Tasks which write into it.
				if (!hasSong)
				{
					hasSong = true;
					Directory.CreateDirectory(saveDir);
				}

				// Create a task for processing this song, but do not start it.
				var taskData = new SongTaskData(fi, currentDir, relativePath, saveDir);
				songTasks.Add(taskData);
				totalSongBytes += taskData.GetSize();
			}
		}

		// Sort by largest data first so we don't leave long songs processing at the end while other threads are idle.
		songTasks.Sort((a, b) => b.GetSize().CompareTo(a.GetSize()));

		var totalNumChartsProcessed = 0;
		var totalProcessedBytes = 0L;
		var totalSongCount = songTasks.Count;
		var lastKnownRemainingCount = totalSongCount;
		var inProgressTasks = new List<SongTaskData>();

		// If the ConcurrentSongCount is not set (it is 0 or less) then use the logical processor count for the task limit.
		var concurrentSongCount = Config.Instance.ConcurrentSongCount < 1
			? Environment.ProcessorCount
			: Config.Instance.ConcurrentSongCount;

		// Process all song tasks.
		var stopWatch = new Stopwatch();
		stopWatch.Start();
		LogInfo($"Found {totalSongCount} songs to process.");
		while (songTasks.Count > 0 || inProgressTasks.Count > 0)
		{
			// See if any in progress tasks are now done.
			var numTasksToAdd = concurrentSongCount;
			var tasksToRemove = new List<SongTaskData>();
			foreach (var inProgressTask in inProgressTasks)
			{
				if (inProgressTask.IsDone())
				{
					totalProcessedBytes += inProgressTask.GetSize();
					totalNumChartsProcessed += inProgressTask.GetNumConvertedCharts();
					tasksToRemove.Add(inProgressTask);
				}
				else
				{
					numTasksToAdd--;
				}
			}

			// Remove completed tasks.
			foreach (var taskToRemove in tasksToRemove)
				inProgressTasks.Remove(taskToRemove);
			tasksToRemove.Clear();

			// Add more tasks.
			var numTasksStarted = 0;
			while (numTasksToAdd > 0 && songTasks.Count > 0)
			{
				var taskToStart = songTasks[0];
				inProgressTasks.Add(taskToStart);
				songTasks.RemoveAt(0);
				taskToStart.Start();
				numTasksToAdd--;
				numTasksStarted++;
			}

			// If we have completed more tasks this loop, log a progress update.
			if (lastKnownRemainingCount != songTasks.Count + inProgressTasks.Count)
			{
				lastKnownRemainingCount = songTasks.Count + inProgressTasks.Count;
				var processedCount = totalSongCount - lastKnownRemainingCount;
				var songPercent = 100.0 * ((double)processedCount / totalSongCount);
				var bytePercent = 100.0 * ((double)totalProcessedBytes / totalSongBytes);
				var totalProcessedMb = totalProcessedBytes / 1024.0 / 1024.0;
				var totalSongMb = totalSongBytes / 1024.0 / 1024.0;
				LogInfo(
					$"Progress: {processedCount}/{totalSongCount} songs ({songPercent:F2}%). {totalProcessedMb:F2}/{totalSongMb:F2} MB ({bytePercent:F2}%).");
			}

			// If we added a lot of tasks then it means we are processing quickly and can speed up.
			// If we added a small number of tasks then it means we are processing slowly and can wait.
			var sleepTime = (int)Interpolation.Lerp(100, 10, 0, concurrentSongCount, numTasksStarted);
			Thread.Sleep(sleepTime);
		}

		stopWatch.Stop();
		LogInfo($"Processed {totalSongCount} songs and created {totalNumChartsProcessed} charts in {stopWatch.Elapsed}.");
	}

	/// <summary>
	/// Process one song.
	/// Song in this context is a song file.
	/// Some songs have multiple song files (an sm and an ssc version).
	/// Will add charts, copy the charts and non-chart files to the output directory,
	/// and write visualizations for the conversion.
	/// </summary>
	/// <param name="songArgs">SongTaskData for the song file.</param>
	private static async Task ProcessSong(SongTaskData songArgs)
	{
		LogInfo("Loading Song.", songArgs.FileInfo, songArgs.RelativePath);

		// Load the song.
		Song song;
		try
		{
			var reader = Reader.CreateReader(songArgs.FileInfo);
			if (reader == null)
			{
				LogError("Unsupported file format. Cannot parse.", songArgs.FileInfo, songArgs.RelativePath);
				return;
			}

			song = await reader.LoadAsync(CancellationToken.None);
		}
		catch (Exception e)
		{
			LogError($"Failed to load file. {e}", songArgs.FileInfo, songArgs.RelativePath);
			return;
		}

		// Add new charts.
		AddCharts(song, songArgs);

		// Save
		var saveFile = Fumen.Path.GetWin32FileSystemFullPath(Fumen.Path.Combine(songArgs.SaveDir, songArgs.FileInfo.Name));
		var config = new SMWriterBase.SMWriterBaseConfig
		{
			FilePath = saveFile,
			Song = song,
			MeasureSpacingBehavior = SMWriterBase.MeasureSpacingBehavior.UseSourceExtraOriginalMeasurePosition,
			PropertyEmissionBehavior = SMWriterBase.PropertyEmissionBehavior.MatchSource,
			WriteTemposFromExtras = true,
			WriteStopsFromExtras = true,
			WriteDelaysFromExtras = true,
			WriteWarpsFromExtras = true,
			WriteScrollsFromExtras = true,
			WriteSpeedsFromExtras = true,
			WriteTimeSignaturesFromExtras = true,
			WriteTickCountsFromExtras = true,
			WriteLabelsFromExtras = true,
			WriteFakesFromExtras = true,
			WriteCombosFromExtras = true,
		};
		var fileFormat = FileFormat.GetFileFormatByExtension(songArgs.FileInfo.Extension);
		switch (fileFormat.Type)
		{
			case FileFormatType.SM:
				await new SMWriter(config).SaveAsync();
				break;
			case FileFormatType.SSC:
				await new SSCWriter(config).SaveAsync();
				break;
			default:
				LogError("Unsupported file format. Cannot save.", songArgs.FileInfo, songArgs.RelativePath);
				break;
		}

		// Copy the non-chart files.
		CopyNonChartFiles(songArgs.CurrentDir, songArgs.SaveDir);

		// TODO: Copy the song's pack assets.
	}

	/// <summary>
	/// Adds charts to the given song and write a visualization per chart, if configured to do so.
	/// </summary>
	/// <param name="song">Song to add charts to.</param>
	/// <param name="songArgs">SongTaskData for the song file.</param>
	private static void AddCharts(Song song, SongTaskData songArgs)
	{
		LogInfo("Processing Song.", songArgs.FileInfo, songArgs.RelativePath, song);

		var fileNameNoExtension = songArgs.FileInfo.Name;
		if (!string.IsNullOrEmpty(songArgs.FileInfo.Extension))
		{
			fileNameNoExtension =
				fileNameNoExtension.Substring(0, songArgs.FileInfo.Name.Length - songArgs.FileInfo.Extension.Length);
		}

		var extension = songArgs.FileInfo.Extension.ToLower();
		if (extension.StartsWith("."))
			extension = extension.Substring(1);

		var newCharts = new List<Chart>();
		var chartsIndicesToRemove = new List<int>();
		foreach (var chart in song.Charts)
		{
			if (chart.Layers.Count == 1
			    && chart.Type == Config.Instance.InputChartType
			    && chart.NumPlayers == 1
			    && chart.NumInputs == InputStepGraph.NumArrows
			    && Config.Instance.DifficultyMatches(chart.DifficultyType))
			{
				// Check if there is an existing chart.
				var (currentChart, currentChartIndex) = FindChart(
					song,
					Config.Instance.OutputChartType,
					chart.DifficultyType,
					OutputStepGraph.NumArrows);
				if (currentChart != null)
				{
					var fumenGenerated = GetFumenGeneratedVersion(currentChart, out var version);

					// Check if we should skip or overwrite the chart.
					switch (Config.Instance.OverwriteBehavior)
					{
						case OverwriteBehavior.DoNotOverwrite:
							continue;
						case OverwriteBehavior.IfFumenGenerated:
							if (!fumenGenerated)
								continue;
							break;
						case OverwriteBehavior.IfFumenGeneratedAndNewerVersion:
							if (!fumenGenerated || version >= Version)
								continue;
							break;
						case OverwriteBehavior.Always:
						default:
							break;
					}
				}

				// Create an ExpressedChart.
				var (ecc, eccName) = Config.Instance.GetExpressedChartConfig(songArgs.FileInfo, chart.DifficultyType);
				var expressedChart = ExpressedChart.CreateFromSMEvents(
					chart.Layers[0].Events,
					InputStepGraph,
					ecc,
					chart.DifficultyRating,
					GetLogIdentifier(songArgs.FileInfo, songArgs.RelativePath, song, chart));
				if (expressedChart == null)
				{
					LogError("Failed to create ExpressedChart.", songArgs.FileInfo, songArgs.RelativePath, song, chart);
					continue;
				}

				// Create a PerformedChart.
				var (pcc, pccName) = Config.Instance.GetPerformedChartConfig(songArgs.FileInfo, chart.DifficultyType);
				var performedChart = PerformedChart.CreateFromExpressedChart(
					OutputStepGraph,
					pcc,
					OutputStartNodes,
					StepTypeFallbacks,
					expressedChart,
					GeneratePerformedChartRandomSeed(songArgs.FileInfo.Name),
					GetLogIdentifier(songArgs.FileInfo, songArgs.RelativePath, song, chart));
				if (performedChart == null)
				{
					LogError("Failed to create PerformedChart.", songArgs.FileInfo, songArgs.RelativePath, song, chart);
					continue;
				}

				// At this point we have succeeded, so add the chart index to remove if appropriate.
				if (currentChart != null)
					chartsIndicesToRemove.Add(currentChartIndex);

				// Create Events for the new Chart.
				var events = performedChart.CreateSMChartEvents();
				CopyNonPerformanceEvents(chart.Layers[0].Events, events);
				events.Sort(new SMEventComparer());
				CopyOriginalMeasurePositionExtras(chart.Layers[0].Events, events);

				// Warn when dropping steps.
				WarnOnDroppedSteps(chart.Layers[0].Events, events, song, songArgs, chart);

				// Create a new Chart for these Events.
				var newChart = new Chart
				{
					Artist = chart.Artist,
					ArtistTransliteration = chart.ArtistTransliteration,
					Genre = chart.Genre,
					GenreTransliteration = chart.GenreTransliteration,
					Author = FormatWithVersion(chart.Author),
					Description = FormatWithVersion(chart.Description),
					MusicFile = chart.MusicFile,
					ChartOffsetFromMusic = chart.ChartOffsetFromMusic,
					Tempo = chart.Tempo,
					DifficultyRating = chart.DifficultyRating,
					DifficultyType = chart.DifficultyType,
					Extras = new Extras(chart.Extras),
					Type = Config.Instance.OutputChartType,
					NumPlayers = 1,
					NumInputs = OutputStepGraph.NumArrows,
				};
				newChart.Layers.Add(new Layer { Events = events });
				newCharts.Add(newChart);

				// If the existing chart had a CHARTNAME set, update that with the version as well.
				// There is odd behavior in Stepmania where if no explicit VERSION is set or the VERSION is under 0.74,
				// and the DESCRIPTION does not match the CHARTNAME, then the steps will fail to load.
				// Because we alter the DESCRIPTION to indicate the chart has been auto-generated we need to update
				// the CHARTNAME as well to work around this issue.
				if (newChart.Extras.TryGetSourceExtra(TagChartName, out string originalChartName))
				{
					newChart.Extras.AddDestExtra(TagChartName, FormatWithVersion(originalChartName));
				}

				LogInfo(
					$"Generated new {newChart.Type} {newChart.DifficultyType} Chart from {chart.Type} {chart.DifficultyType} Chart"
					+ $" using ExpressedChartConfig \"{eccName}\" (BracketParsingMethod {expressedChart.GetBracketParsingMethod():G})"
					+ $" and PerformedChartConfig \"{pccName}\".",
					songArgs.FileInfo, songArgs.RelativePath, song, newChart);

				// Write a visualization.
				if (Config.Instance.OutputVisualizations && CanOutputVisualizations)
				{
					var visualizationDirectory = Fumen.Path.Combine(VisualizationDir, songArgs.RelativePath);
					Directory.CreateDirectory(visualizationDirectory);
					var saveFile = Fumen.Path.GetWin32FileSystemFullPath(
						Fumen.Path.Combine(visualizationDirectory,
							$"{fileNameNoExtension}-{chart.DifficultyType}-{extension}.html"));

					try
					{
						var visualizer = new Visualizer(
							songArgs.CurrentDir,
							saveFile,
							song,
							chart,
							expressedChart,
							eccName,
							performedChart,
							newChart
						);
						visualizer.Write();
					}
					catch (Exception e)
					{
						LogError($"Failed to write visualization to \"{saveFile}\". {e}",
							songArgs.FileInfo, songArgs.RelativePath, song, newChart);
					}
				}
			}
		}

		songArgs.AddConvertedChartCount(newCharts.Count);

		LogInfo(
			$"Generated {newCharts.Count} new {Config.Instance.OutputChartType} Charts (replaced {chartsIndicesToRemove.Count}).",
			songArgs.FileInfo, songArgs.RelativePath, song);

		// Remove overwritten charts.
		if (chartsIndicesToRemove.Count > 0)
		{
			// Ensure the indices are sorted descending so they don't shift when removing.
			chartsIndicesToRemove.Sort((a, b) => b.CompareTo(a));
			foreach (var i in chartsIndicesToRemove)
				song.Charts.RemoveAt(i);
		}

		// Add new charts.
		song.Charts.AddRange(newCharts);
	}

	/// <summary>
	/// Warns when steps were dropped if configured to do so.
	/// </summary>
	private static void WarnOnDroppedSteps(List<Event> sourceEvents, List<Event> destEvents, Song song, SongTaskData songArgs,
		Chart chart)
	{
		if (!Config.Instance.WarnOnDroppedSteps || destEvents.Count == sourceEvents.Count)
			return;

		var mineString = NoteChars[(int)NoteType.Mine].ToString();
		var positionsOfMissingSteps = new List<int>();
		var sourceEventIndex = 0;
		var destEventIndex = 0;
		var numDroppedMines = 0;
		while (true)
		{
			var newPosition = sourceEvents[sourceEventIndex].IntegerPosition;

			var numSourceSteps = 0;
			var numSourceMines = 0;
			while (sourceEventIndex < sourceEvents.Count && sourceEvents[sourceEventIndex].IntegerPosition == newPosition)
			{
				if (sourceEvents[sourceEventIndex].SourceType != mineString)
					numSourceSteps++;
				else
					numSourceMines++;
				sourceEventIndex++;
			}

			var numDestSteps = 0;
			var numDestMines = 0;
			while (destEventIndex < destEvents.Count && destEvents[destEventIndex].IntegerPosition == newPosition)
			{
				if (destEvents[destEventIndex].SourceType != mineString)
					numDestSteps++;
				else
					numDestMines++;
				destEventIndex++;
			}

			if (numDestSteps != numSourceSteps)
				positionsOfMissingSteps.Add(newPosition);
			numDroppedMines += numSourceMines - numDestMines;

			if (sourceEventIndex >= sourceEvents.Count)
				break;
		}

		if (positionsOfMissingSteps.Count > 0)
		{
			var sb = new StringBuilder();
			var first = true;
			foreach (var pos in positionsOfMissingSteps)
			{
				if (!first)
					sb.Append(", ");
				sb.Append(pos.ToString());
				first = false;
			}

			if (positionsOfMissingSteps.Count > 1)
			{
				LogWarn($"Dropped {positionsOfMissingSteps.Count} steps at positions: [{sb}].",
					songArgs.FileInfo, songArgs.RelativePath, song, chart);
			}
			else
			{
				LogWarn($"Dropped {positionsOfMissingSteps.Count} step at position {sb}.",
					songArgs.FileInfo, songArgs.RelativePath, song, chart);
			}
		}

		if (numDroppedMines > 0)
		{
			if (numDroppedMines > 1)
			{
				LogWarn($"Dropped {numDroppedMines} mines.",
					songArgs.FileInfo, songArgs.RelativePath, song, chart);
			}
			else
			{
				LogWarn($"Dropped {numDroppedMines} mine.",
					songArgs.FileInfo, songArgs.RelativePath, song, chart);
			}
		}
	}

	/// <summary>
	/// Copies the non-chart files from the given song directory into the given save directory.
	/// </summary>
	/// <remarks>
	/// Idempotent.
	/// Will not copy if already invoked with the same song directory.
	/// Will not copy if not appropriate based on Config.NonChartFileCopyBehavior.
	/// Expects that saveDir exists and is writable.
	/// Will log errors and warnings on failures.
	/// </remarks>
	/// <param name="songDir">
	/// Directory of the song to copy the non-chart files from.
	/// </param>
	/// <param name="saveDir">
	/// Directory to copy the non-chart files into.
	/// </param>
	private static void CopyNonChartFiles(string songDir, string saveDir)
	{
		if (Config.Instance.NonChartFileCopyBehavior == CopyBehavior.DoNotCopy
		    || Config.Instance.IsOutputDirectorySameAsInputDirectory())
			return;

		// Only copy the non-chart files once per song.
		lock (CopiedDirectories)
		{
			if (CopiedDirectories.Contains(songDir))
				return;
			CopiedDirectories.Add(songDir);
		}

		// Get the files in the song directory.
		string[] files;
		try
		{
			files = Directory.GetFiles(songDir);
		}
		catch (Exception e)
		{
			LogWarn($"Could not get files in \"{songDir}\". {e}");
			return;
		}

		// Check each file for copying
		foreach (var file in files)
		{
			// Get the FileInfo for this file so we can check its name.
			FileInfo fi;
			try
			{
				fi = new FileInfo(file);
			}
			catch (Exception e)
			{
				LogWarn($"Could not get file info for \"{file}\". {e}");
				continue;
			}

			// Skip this file if it is a chart.
			var fileFormat = FileFormat.GetFileFormatByExtension(fi.Extension);
			if (fileFormat != null && SupportedFileFormats.Contains(fileFormat.Type))
				continue;

			// Skip this file if it is not newer than the destination file and we
			// should only copy if newer.
			var destFilePath = saveDir + fi.Name;
			if (Config.Instance.NonChartFileCopyBehavior == CopyBehavior.IfNewer)
			{
				FileInfo dfi;
				try
				{
					dfi = new FileInfo(destFilePath);
				}
				catch (Exception e)
				{
					LogWarn($"Could not get file info for \"{destFilePath}\". {e}");
					continue;
				}

				if (dfi.Exists && fi.LastWriteTime <= dfi.LastWriteTime)
				{
					continue;
				}
			}

			// Copy the file.
			try
			{
				File.Copy(Fumen.Path.GetWin32FileSystemFullPath(fi.FullName),
					Fumen.Path.GetWin32FileSystemFullPath(destFilePath),
					true);
			}
			catch (Exception e)
			{
				LogWarn($"Failed to copy \"{fi.FullName}\" to \"{destFilePath}\". {e}");
			}
		}
	}

	/// <summary>
	/// Formats a string from a Chart by prepending the formatted version number.
	/// </summary>
	/// <returns>Formatted string with version.</returns>
	private static string FormatWithVersion(string originalStr)
	{
		if (string.IsNullOrEmpty(originalStr))
			return FormattedVersionString;
		return $"{FormattedVersionString} {originalStr}";
	}

	/// <summary>
	/// Parses the given Chart's description to see if it was generated by this application.
	/// Returns the version if present, via the out parameter.
	/// </summary>
	/// <param name="chart">Chart to check.</param>
	/// <param name="version">
	/// Out parameter to store the version of the application used to generate the chart.
	/// </param>
	/// <returns>Whether or not the given Chart was generated by this application.</returns>
	public static bool GetFumenGeneratedVersion(Chart chart, out SemanticVersion version)
	{
		version = new SemanticVersion();
		if (string.IsNullOrEmpty(chart.Description))
			return false;

		// Try the semantic version
		var match = Regex.Match(chart.Description, FumenGeneratedFormattedSemanticVersionRegexPattern,
			RegexOptions.IgnoreCase);
		if (match.Success && match.Groups.Count == 4
		                  && match.Groups[1].Captures.Count == 1
		                  && match.Groups[2].Captures.Count == 1
		                  && match.Groups[3].Captures.Count == 1)
		{
			if (int.TryParse(match.Groups[1].Captures[0].Value, out var major)
			    && int.TryParse(match.Groups[2].Captures[0].Value, out var minor)
			    && int.TryParse(match.Groups[3].Captures[0].Value, out var patch))
			{
				version = new SemanticVersion(major, minor, patch);
				return true;
			}
		}

		// Try the old deprecated version
		match = Regex.Match(chart.Description, FumenGeneratedFormattedDeprecatedVersionRegexPattern, RegexOptions.IgnoreCase);
		if (match.Success && match.Groups.Count == 3
		                  && match.Groups[1].Captures.Count == 1
		                  && match.Groups[2].Captures.Count == 1)
		{
			if (int.TryParse(match.Groups[1].Captures[0].Value, out var major)
			    && int.TryParse(match.Groups[2].Captures[0].Value, out var minor))
			{
				version = new SemanticVersion(major, minor, 0);
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Finds the Chart matching the given parameters in the given Song.
	/// </summary>
	/// <param name="song">Song to check.</param>
	/// <param name="chartType">Chart Type sting to match.</param>
	/// <param name="difficultyType">Chart DifficultyType string to match.</param>
	/// <param name="numArrows">Number of arrows / lanes to match.</param>
	/// <returns>
	/// Chart and the index of this Chart in the Song, if the Chart was found.
	/// Returns (null, 0) if not found.
	/// </returns>
	private static (Chart, int) FindChart(Song song, string chartType, string difficultyType, int numArrows)
	{
		var index = 0;
		foreach (var chart in song.Charts)
		{
			if (chart.Layers.Count == 1
			    && chart.Type == chartType
			    && chart.NumPlayers == 1
			    && chart.NumInputs == numArrows
			    && chart.DifficultyType == difficultyType)
			{
				return (chart, index);
			}

			index++;
		}

		return (null, 0);
	}

	/// <summary>
	/// Copies the TagFumenNoteOriginalMeasurePosition values from the SourceExtras on the given
	/// source Event List to the corresponding events in the dest list.
	/// Assumes both source and dest are sorted and the dest Events were generated from the
	/// source events and occur at the same IntegerPositions.
	/// </summary>
	/// <param name="source">Event List to copy from.</param>
	/// <param name="dest">Event List to copy to.</param>
	private static void CopyOriginalMeasurePositionExtras(List<Event> source, List<Event> dest)
	{
		var sourceIndex = 0;
		var destIndex = 0;
		var sourceCount = source.Count;
		var destCount = dest.Count;

		// It is possible for a generated chart to have fewer events than the source
		// chart due to edge cases with mine placement, so we need to keep track of
		// the indices separately.
		while (sourceIndex < sourceCount)
		{
			var row = source[sourceIndex].IntegerPosition;

			// Some events like TempoChanges do not have Extras for the original position.
			source[sourceIndex].Extras.TryGetSourceExtra(TagFumenNoteOriginalMeasurePosition, out Fraction f);
			while (sourceIndex < sourceCount && source[sourceIndex].IntegerPosition == row)
			{
				sourceIndex++;
				if (f == null && sourceIndex < sourceCount)
				{
					source[sourceIndex].Extras.TryGetSourceExtra(TagFumenNoteOriginalMeasurePosition, out f);
				}
			}

			while (destIndex < destCount && dest[destIndex].IntegerPosition == row)
			{
				if (f != null)
				{
					dest[destIndex].Extras.AddDestExtra(TagFumenNoteOriginalMeasurePosition, f);
				}

				destIndex++;
			}
		}
	}

	/// <summary>
	/// Generates a random seed to use for a PerformedChart based on the Song's file name.
	/// Creating a PerformedChart from the same inputs more than once should produce the same result.
	/// </summary>
	/// <param name="fileName">Name of the Song file to hash to generate the seed.</param>
	/// <returns>Random seed to use.</returns>
	private static int GeneratePerformedChartRandomSeed(string fileName)
	{
		var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(fileName));
		return BitConverter.ToInt32(hash, 0);
	}

	#region Logging

	private static string GetLogIdentifier(FileInfo fi, string relativePath, Song song = null, Chart chart = null)
	{
		if (chart != null && song != null)
			return $"[{relativePath}{fi.Name} \"{song.Title}\" {chart.Type} {chart.DifficultyType}]";
		if (song != null)
			return $"[{relativePath}{fi.Name} \"{song.Title}\"]";
		return $"[{relativePath}{fi.Name}]";
	}

	private static void LogError(string message)
	{
		Logger.Error($"[{LogTag}] {message}");
	}

	private static void LogWarn(string message)
	{
		Logger.Warn($"[{LogTag}] {message}");
	}

	private static void LogInfo(string message)
	{
		Logger.Info($"[{LogTag}] {message}");
	}

	private static void LogError(string message, FileInfo fi, string relativePath, Song song = null, Chart chart = null)
	{
		LogError($"{GetLogIdentifier(fi, relativePath, song, chart)} {message}");
	}

	private static void LogWarn(string message, FileInfo fi, string relativePath, Song song = null, Chart chart = null)
	{
		LogWarn($"{GetLogIdentifier(fi, relativePath, song, chart)} {message}");
	}

	private static void LogInfo(string message, FileInfo fi, string relativePath, Song song = null, Chart chart = null)
	{
		LogInfo($"{GetLogIdentifier(fi, relativePath, song, chart)} {message}");
	}

	#endregion Logging
}
