using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Fumen;
using Fumen.Converters;
using StepManiaLibrary;

namespace StepManiaChartGenerator
{
	/// <summary>
	/// A rule for matching a file to a configuration data object to use.
	/// </summary>
	public class ConfigRule
	{
		/// <summary>
		/// Regular expression for matching full file names.
		/// </summary>
		[JsonInclude] public string FileRegex;

		/// <summary>
		/// Regular expression for matching chart difficulty types.
		/// </summary>
		[JsonInclude] public string DifficultyRegex;

		/// <summary>
		/// Configuration data identifier to use when matched.
		/// </summary>
		[JsonInclude] public string Config;
	}

	/// <summary>
	/// Configuration for StepManiaChartGenerator.
	/// Deserialized from json config file.
	/// Use Load to load Config.
	/// See Config.md for detailed descriptions and examples of Config values.
	/// </summary>
	public class Config
	{
		/// <summary>
		/// File to use for deserializing Config.
		/// </summary>
		private const string FileName = "StepManiaChartGeneratorConfig.json";

		/// <summary>
		/// Tag for logging messages.
		/// </summary>
		private const string LogTag = "Config";

		/// <summary>
		/// Static Config instance.
		/// </summary>
		public static Config Instance { get; private set; }

		/// <summary>
		/// Logger config.
		/// </summary>
		[JsonInclude] public LoggerConfig LoggerConfig;

		/// <summary>
		/// Timeout for regular expression matching.
		/// </summary>
		[JsonInclude] public double RegexTimeoutSeconds = 6.0;

		/// <summary>
		/// Whether the application should close automatically or wait for input when it completes.
		/// </summary>
		[JsonInclude] public bool CloseAutomaticallyWhenComplete = false;

		/// <summary>
		/// Directory to recursively search through for finding Charts to convert.
		/// </summary>
		[JsonInclude] public string InputDirectory;

		/// <summary>
		/// Regular expression for file names to match for Charts to convert.
		/// </summary>
		[JsonInclude] public string InputNameRegex;

		/// <summary>
		/// StepMania StepsType of Charts to match for conversion.
		/// </summary>
		[JsonInclude] public string InputChartType;

		/// <summary>
		/// Regular expression for matching StepMania DifficultyNames to match Charts for conversion.
		/// </summary>
		[JsonInclude] public string DifficultyRegex;

		/// <summary>
		/// Directory to write converted Charts to.
		/// </summary>
		[JsonInclude] public string OutputDirectory;

		/// <summary>
		/// StepMania StepsType to convert to.
		/// </summary>
		[JsonInclude] public string OutputChartType;

		/// <summary>
		/// OverwriteBehavior for controlling how to handle converting when the OutputChartType
		/// already exists in the input Song.
		/// </summary>
		[JsonInclude] public OverwriteBehavior OverwriteBehavior = OverwriteBehavior.DoNotOverwrite;

		/// <summary>
		/// CopyBehavior for controlling how to copy other files adjacent to the input Song
		/// when writing the output Song file.
		/// </summary>
		[JsonInclude] public CopyBehavior NonChartFileCopyBehavior = CopyBehavior.DoNotCopy;

		/// <summary>
		/// Whether or not to output visualization files.
		/// </summary>
		[JsonInclude] public bool OutputVisualizations = true;

		/// <summary>
		/// Directory to output visualization files to.
		/// </summary>
		[JsonInclude] public string VisualizationsDirectory;

		/// <summary>
		/// Identifier of the default ExpressedChartConfig to use.
		/// Expected that this identifier is a key in ExpressedChartConfigs.
		/// </summary>
		[JsonInclude] public string DefaultExpressedChartConfig;

		/// <summary>
		/// Identifier of the default PerformedChartConfig to use.
		/// Expected that this identifier is a key in PerformedChartConfigs.
		/// </summary>
		[JsonInclude] public string DefaultPerformedChartConfig;

		/// <summary>
		/// List of rules for matching files to ExpressedChartConfigs.
		/// Higher-indexed matching ConfigRules will be used over lower-indexed matching ConfigRules.
		/// </summary>
		[JsonInclude] public ConfigRule[] ExpressedChartConfigRules;

		/// <summary>
		/// List of rules for matching files to PerformedChartConfigs.
		/// Higher-indexed matching ConfigRules will be used over lower-indexed matching ConfigRules.
		/// </summary>
		[JsonInclude] public ConfigRule[] PerformedChartConfigRules;

		/// <summary>
		/// Dictionary of identifier to ExpressedChartConfig.
		/// </summary>
		[JsonInclude] public Dictionary<string, ExpressedChartConfig> ExpressedChartConfigs;

		/// <summary>
		/// Dictionary of identifier to PerformedChartConfig.
		/// </summary>
		[JsonInclude] public Dictionary<string, PerformedChartConfig> PerformedChartConfigs;

		/// <summary>
		/// Cached value for whether the output directory is the same as the input directory.
		/// </summary>
		private bool OutputDirectoryEqualsDirectory = false;

		/// <summary>
		/// Loads the Config from the config json file.
		/// </summary>
		/// <returns>Config Instance.</returns>
		public static async Task<Config> Load()
		{
			if (Instance != null)
				return Instance;

			var options = new JsonSerializerOptions
			{
				Converters =
				{
					new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
				},
				ReadCommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true,
				IncludeFields = true,
			};

			try
			{
				using (FileStream openStream = File.OpenRead(Fumen.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName)))
				{
					Instance = await JsonSerializer.DeserializeAsync<Config>(openStream, options);
					Instance?.Init();
				}
			}
			catch (Exception e)
			{
				LogError($"Failed to load {FileName}. {e}");
				Instance = null;
			}

			return Instance;
		}

		/// <summary>
		/// Post Load initialization.
		/// </summary>
		private void Init()
		{
			// Set the non-default configs to be overrides of the default.
			if (!string.IsNullOrEmpty(DefaultPerformedChartConfig)
				&& PerformedChartConfigs.ContainsKey(DefaultPerformedChartConfig))
			{
				var defaultConfig = PerformedChartConfigs[DefaultPerformedChartConfig];
				foreach (var kvp in PerformedChartConfigs)
				{
					if (kvp.Key != DefaultPerformedChartConfig)
						kvp.Value.SetAsOverrideOf(defaultConfig);
				}
			}

			// Initialize normalized weights.
			if (PerformedChartConfigs != null)
			{
				foreach (var kvp in PerformedChartConfigs)
				{
					kvp.Value.Init();
				}
			}

			// Convert paths to absolute paths.
			if (!string.IsNullOrEmpty(OutputDirectory))
				OutputDirectory = System.IO.Path.GetFullPath(OutputDirectory);
			if (!string.IsNullOrEmpty(InputDirectory))
				InputDirectory = System.IO.Path.GetFullPath(InputDirectory);

			// Cache whether the output and input directories are the same.
			OutputDirectoryEqualsDirectory =
				InputDirectory != null
				&& OutputDirectory != null
				&& InputDirectory.Equals(OutputDirectory);
		}

		/// <summary>
		/// Performs validation of Config options.
		/// Will log errors and warnings.
		/// Returns true if no errors were found.
		/// </summary>
		/// <returns>True of no errors were found and false otherwise.</returns>
		public bool Validate()
		{
			var errors = false;

			if (RegexTimeoutSeconds <= 0.0)
			{
				LogError($"Invalid RegexTimeoutSeconds {RegexTimeoutSeconds}. Must be greater than 0.0.");
				errors = true;
			}

			if (string.IsNullOrEmpty(InputDirectory))
			{
				LogError("No InputDirectory specified.");
				errors = true;
			}

			if (string.IsNullOrEmpty(InputNameRegex))
			{
				LogError("No InputNameRegex specified.");
				errors = true;
			}
			else if (!IsValidRegex(InputNameRegex))
			{
				LogError($"Invalid regular expression for InputNameRegex: \"{InputNameRegex}\".");
				errors = true;
			}

			if (string.IsNullOrEmpty(InputChartType))
			{
				LogError("No InputChartType specified.");
				errors = true;
			}
			else
			{
				var smChartTypeValid = SMCommon.TryGetChartType(InputChartType, out _);
				if (!smChartTypeValid)
				{
					LogError($"InputChartType \"{InputChartType}\" is not a valid stepmania chart type.");
					errors = true;
				}
			}

			if (OutputVisualizations && string.IsNullOrEmpty(VisualizationsDirectory))
			{
				LogError("OutputVisualizations is true, but no VisualizationsDirectory specified.");
				errors = true;
			}

			if (string.IsNullOrEmpty(OutputDirectory))
			{
				LogError("No OutputDirectory specified.");
				errors = true;
			}

			if (string.IsNullOrEmpty(DifficultyRegex))
			{
				LogError("No DifficultyRegex specified.");
				errors = true;
			}
			else if (!IsValidRegex(DifficultyRegex))
			{
				LogError($"Invalid regular expression for DifficultyRegex: \"{DifficultyRegex}\".");
				errors = true;
			}

			if (string.IsNullOrEmpty(OutputChartType))
			{
				LogError("No OutputChartType specified.");
				errors = true;
			}

			if (OutputChartType != null)
			{
				var smChartTypeValid = SMCommon.TryGetChartType(OutputChartType, out var smChartType);
				if (!smChartTypeValid)
				{
					LogError($"OutputChartType \"{OutputChartType}\" is not a valid stepmania chart type.");
					errors = true;
				}

				if (PerformedChartConfigs != null)
				{
					foreach (var kvp in PerformedChartConfigs)
					{
						if (!kvp.Value.ValidateArrowWeights(OutputChartType, smChartType, smChartTypeValid, kvp.Key))
							errors = true;
					}
				}
			}

			if (PerformedChartConfigs == null || PerformedChartConfigs.Count < 1)
			{
				LogError("No PerformedChartConfigs specified. Expected at least one.");
				errors = true;
			}

			if (string.IsNullOrEmpty(DefaultPerformedChartConfig))
			{
				LogError("No DefaultPerformedChartConfig specified.");
				errors = true;
			}

			if (PerformedChartConfigs != null
			    && !string.IsNullOrEmpty(DefaultPerformedChartConfig)
			    && !PerformedChartConfigs.ContainsKey(DefaultPerformedChartConfig))
			{
				LogError($"DefaultPerformedChartConfig \"{DefaultPerformedChartConfig}\" not found in PerformedChartConfigs.");
				errors = true;
			}

			if (PerformedChartConfigs != null)
			{
				foreach (var kvp in PerformedChartConfigs)
				{
					if (!kvp.Value.Validate(kvp.Key))
						errors = true;
				}
			}

			if (ExpressedChartConfigs == null || ExpressedChartConfigs.Count < 1)
			{
				LogError("No ExpressedChartConfigs specified. Expected at least one.");
				errors = true;
			}

			if (string.IsNullOrEmpty(DefaultExpressedChartConfig))
			{
				LogError("No DefaultExpressedChartConfig specified.");
				errors = true;
			}

			if (ExpressedChartConfigs != null
			    && !string.IsNullOrEmpty(DefaultExpressedChartConfig)
			    && !ExpressedChartConfigs.ContainsKey(DefaultExpressedChartConfig))
			{
				LogError($"DefaultExpressedChartConfig \"{DefaultExpressedChartConfig}\" not found in ExpressedChartConfigs.");
				errors = true;
			}

			if (ExpressedChartConfigs != null)
			{
				foreach (var kvp in ExpressedChartConfigs)
				{
					if (!kvp.Value.Validate(kvp.Key))
						errors = true;
				}
			}

			// Validate ExpressedChartConfigRules.
			if (ExpressedChartConfigRules != null)
			{
				for (var i = 0; i < ExpressedChartConfigRules.Length; i++)
				{
					var configRule = ExpressedChartConfigRules[i];
					if (string.IsNullOrEmpty(configRule.Config))
					{
						LogError($"ExpressedChartConfigRules[{i}] No Config specified.");
						errors = true;
					}

					if (ExpressedChartConfigs != null
					    && !string.IsNullOrEmpty(configRule.Config)
					    && !ExpressedChartConfigs.ContainsKey(configRule.Config))
					{
						LogError(
							$"ExpressedChartConfigRules[{i}] Config \"{configRule.Config}\" not found in ExpressedChartConfigs.");
						errors = true;
					}

					if (string.IsNullOrEmpty(configRule.FileRegex))
					{
						LogError($"ExpressedChartConfigRules[{i}] No FileRegex specified.");
						errors = true;
					}
					else if (!IsValidRegex(configRule.FileRegex))
					{
						LogError(
							$"ExpressedChartConfigRules[{i}] Invalid regular expression for FileRegex: \"{configRule.FileRegex}\".");
						errors = true;
					}

					if (string.IsNullOrEmpty(configRule.DifficultyRegex))
					{
						LogError($"ExpressedChartConfigRules[{i}] No DifficultyRegex specified.");
						errors = true;
					}
					else if (!IsValidRegex(configRule.DifficultyRegex))
					{
						LogError(
							$"ExpressedChartConfigRules[{i}] Invalid regular expression for DifficultyRegex: \"{configRule.DifficultyRegex}\".");
						errors = true;
					}
				}
			}

			// Validate PerformedChartConfigRules.
			if (PerformedChartConfigRules != null)
			{
				for (var i = 0; i < PerformedChartConfigRules.Length; i++)
				{
					var configRule = PerformedChartConfigRules[i];
					if (string.IsNullOrEmpty(configRule.Config))
					{
						LogError($"PerformedChartConfigRules[{i}] No Config specified.");
						errors = true;
					}

					if (PerformedChartConfigs != null
					    && !string.IsNullOrEmpty(configRule.Config)
					    && !PerformedChartConfigs.ContainsKey(configRule.Config))
					{
						LogError(
							$"PerformedChartConfigRules[{i}] Config \"{configRule.Config}\" not found in PerformedChartConfigs.");
						errors = true;
					}

					if (string.IsNullOrEmpty(configRule.FileRegex))
					{
						LogError($"PerformedChartConfigRules[{i}] No FileRegex specified.");
						errors = true;
					}
					else if (!IsValidRegex(configRule.FileRegex))
					{
						LogError(
							$"PerformedChartConfigRules[{i}] Invalid regular expression for FileRegex: \"{configRule.FileRegex}\".");
						errors = true;
					}

					if (string.IsNullOrEmpty(configRule.DifficultyRegex))
					{
						LogError($"PerformedChartConfigRules[{i}] No DifficultyRegex specified.");
						errors = true;
					}
					else if (!IsValidRegex(configRule.DifficultyRegex))
					{
						LogError(
							$"PerformedChartConfigRules[{i}] Invalid regular expression for DifficultyRegex: \"{configRule.DifficultyRegex}\".");
						errors = true;
					}
				}
			}

			if (!LoggerConfig.Validate())
				errors = true;

			return !errors;
		}

		/// <summary>
		/// Returns whether the output directory is the same as the input directory.
		/// </summary>
		/// <returns>Whether the output directory is the same as the input directory</returns>
		public bool IsOutputDirectorySameAsInputDirectory()
		{
			return OutputDirectoryEqualsDirectory;
		}

		/// <summary>
		/// Determines whether the difficulty represented by the given string
		/// matches the Config DifficultyRegex.
		/// </summary>
		/// <param name="difficulty">String representing the difficulty to check.</param>
		/// <returns>True if this difficulty matches and false otherwise.</returns>
		public bool DifficultyMatches(string difficulty)
		{
			return Matches(DifficultyRegex, difficulty);
		}

		/// <summary>
		/// Determines whether the file name represented by the given string
		/// matches the Config InputNameRegex.
		/// </summary>
		/// <param name="inputFileName">String representing the input file name to check.</param>
		/// <returns>True if this file name matches and false otherwise.</returns>
		public bool InputNameMatches(string inputFileName)
		{
			return Matches(InputNameRegex, inputFileName);
		}

		/// <summary>
		/// Gets the ExpressedChartConfig to use for the Chart represented by the given FileInfo
		/// and difficulty.
		/// </summary>
		/// <param name="file">FileInfo for the file containing the Chart.</param>
		/// <param name="difficulty">Difficulty string of the Chart.</param>
		/// <returns>ExpressedChartConfig to use and its identifier.</returns>
		public (ExpressedChartConfig, string) GetExpressedChartConfig(FileInfo file, string difficulty)
		{
			// Check for rules specific to this file and difficulty.
			if (ExpressedChartConfigRules != null && ExpressedChartConfigRules.Length > 0)
			{
				// Loop in reverse order to prioritize later matches.
				for (var i = ExpressedChartConfigRules.Length - 1; i >= 0; i--)
				{
					if (Matches(ExpressedChartConfigRules[i].FileRegex, file.FullName)
					    && Matches(ExpressedChartConfigRules[i].DifficultyRegex, difficulty))
					{
						return (ExpressedChartConfigs[ExpressedChartConfigRules[i].Config],
							ExpressedChartConfigRules[i].Config);
					}
				}
			}

			// Fallback to the default config.
			return (ExpressedChartConfigs[DefaultExpressedChartConfig], DefaultExpressedChartConfig);
		}

		/// <summary>
		/// Gets the PerformedChartConfig to use for the Chart represented by the given FileInfo
		/// and difficulty.
		/// </summary>
		/// <param name="file">FileInfo for the file containing the Chart.</param>
		/// <param name="difficulty">Difficulty string of the Chart.</param>
		/// <returns>PerformedChartConfig to use and its identifier.</returns>
		public (PerformedChartConfig, string) GetPerformedChartConfig(FileInfo file, string difficulty)
		{
			// Check for rules specific to this file and difficulty.
			if (PerformedChartConfigRules != null && PerformedChartConfigRules.Length > 0)
			{
				// Loop in reverse order to prioritize later matches.
				for (var i = PerformedChartConfigRules.Length - 1; i >= 0; i--)
				{
					if (Matches(PerformedChartConfigRules[i].FileRegex, file.FullName)
					    && Matches(PerformedChartConfigRules[i].DifficultyRegex, difficulty))
					{
						return (PerformedChartConfigs[PerformedChartConfigRules[i].Config],
							PerformedChartConfigRules[i].Config);
					}
				}
			}

			// Fallback to the default config.
			return (PerformedChartConfigs[DefaultPerformedChartConfig], DefaultPerformedChartConfig);
		}

		/// <summary>
		/// Returns whether or not the given regular expression string is a valid regular expression.
		/// </summary>
		/// <param name="regex">Regular expression string to check.</param>
		/// <returns>True if the given regular expression string is valid and false otherwise.</returns>
		private bool IsValidRegex(string regex)
		{
			if (string.IsNullOrEmpty(regex))
				return false;
			try
			{
				// Using a hard-coded min timeout since this is used during validation and a low 
				// RegexTimeoutSeconds shouldn't cause validation failures here.
				var timeout = Math.Min(6.0, RegexTimeoutSeconds);
				var _ = Regex.IsMatch("", regex, RegexOptions.Singleline, TimeSpan.FromSeconds(timeout));
			}
			catch (ArgumentException)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Returns whether the given input string matches the given regular expression.
		/// </summary>
		/// <param name="regex">Regular expression string to check.</param>
		/// <param name="input">Input string to check against the regular expression.</param>
		/// <returns>True if the given regular expression string is valid and false otherwise.</returns>
		private bool Matches(string regex, string input)
		{
			var matches = false;
			try
			{
				matches = Regex.IsMatch(input, regex, RegexOptions.Singleline, TimeSpan.FromSeconds(RegexTimeoutSeconds));
			}
			catch (Exception e)
			{
				LogError($"Failed to determine if \"{input}\" matches \"{regex}\". {e}");
			}

			return matches;
		}

		#region Logging

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

		#endregion Logging
	}
}
