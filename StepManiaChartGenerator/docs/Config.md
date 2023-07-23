# Configuration

`StepManiaChartGenerator`'s behavior can be configured via the `StepManiaChartGeneratorConfig.json` file in the application's install directory. Comments and trailing commas are supported.

# Supported ChartTypes

The following [ChartTypes](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md) are supported for both input and output. When specified in the configuration file `-` is expected instead of `_`.
- `dance-single`
- `dance-double`
- `dance-solo`
- `dance-threepanel`
- `pump-single`
- `pump-halfdouble`
- `pump-double`
- `smx-beginner`
- `smx-single`
- `smx-dual`
- `smx-full`

## Adding ChartTypes

Additional [ChartTypes](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md) beyond the default types specified above can be added if they:
1) Are for one player only.
2) Are listed in the [full ChartType list](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md).

To add another [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md), add a [PadData](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/PadData.md) file and a [StepGraph](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/StepGraphs.md) file for the [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md) to the application's install directory. These files can be generated from simple input with [PadDataGenerator](https://github.com/PerryAsleep/PadDataGenerator).

# Example Configuration

<details>
	<summary>Example</summary>

```json5
{
	// Logger configuration.
	"LoggerConfig":
	{
		"LogLevel": "Info",
		"LogToFile": true,
		"LogDirectory": "C:\\Fumen\\Logs",
		"LogFlushIntervalSeconds": 20,
		"LogBufferSizeBytes": 10240,
		"LogToConsole": true,
	},

	"InputDirectory": "C:\\Games\\StepMania 5\\Songs",
	"InputNameRegex": ".*\\.(sm|ssc)$",
	"InputChartType": "dance-single",
	"DifficultyRegex": ".",

	"OutputDirectory": "C:\\Fumen\\Exports",
	"OutputChartType": "dance-double",
	"OverwriteBehavior": "IfFumenGenerated",
	"NonChartFileCopyBehavior": "DoNotCopy",

	"OutputVisualizations": true,
	"VisualizationsDirectory": "C:\\Fumen\\Visualizations",

	// Derive the concurrent song limit from the number of logical processors.
	"ConcurrentSongCount": -1,
	"RegexTimeoutSeconds": 20.0,

	"WarnOnDroppedSteps": true,
	"CloseAutomaticallyWhenComplete": false,

	"DefaultExpressedChartConfig": "BalancedDynamic",
	"DefaultPerformedChartConfig": "Default",

	"ExpressedChartConfigRules":
	[
		{"FileRegex": ".*(\\\\|/)MyPackWithNoBrackets.*(\\\\|/).*", "DifficultyRegex": ".", "Config": "NoBrackets"},
	],

	"PerformedChartConfigRules":
	[
		{"FileRegex": ".*(\\\\|/)MyStaminaPack.*(\\\\|/).*", "DifficultyRegex": ".", "Config": "Stamina"},
	],

	"ExpressedChartConfigs":
	{
		"BalancedDynamic":
		{
			"DefaultBracketParsingMethod": "Balanced",
			"BracketParsingDetermination": "ChooseMethodDynamically",
			"MinLevelForBrackets": 7,
			"UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets": true,
			// Interpret charts with three brackets per minute or more as charts which should aggressively interpret bracketable jumps as brackets.
			"BalancedBracketsPerMinuteForAggressiveBrackets": 3.0,
			// Interpret charts with one bracket every minute or less as charts which should not have brackets.
			// Many non-technical charts have jumps with patterns which can reasonably be done by bracketing. With balanced
			// parsing, these charts would produce brackets. This threshold helps keeps non-technical charts bracket-free.
			"BalancedBracketsPerMinuteForNoBrackets": 1.0,
		},
		"NoBrackets":
		{
			"DefaultBracketParsingMethod": "NoBrackets",
			"BracketParsingDetermination": "UseDefaultMethod",
		},
		"AggressiveBrackets":
		{
			"DefaultBracketParsingMethod": "Aggressive",
			"BracketParsingDetermination": "UseDefaultMethod",
		}
	},

	"PerformedChartConfigs":
	{
		// Balanced default settings.
		"Default":
		{
			"ArrowWeights":
			{
				"dance-single": [25, 25, 25, 25],
				"dance-double": [6, 12, 10, 22, 22, 12, 10, 6],
				"dance-solo": [13, 12, 25, 25, 12, 13],
				"dance-threepanel": [25, 50, 25],

				"pump-single": [17, 16, 34, 16, 17],
				"pump-halfdouble": [25, 12, 13, 13, 12, 25],
				"pump-double": [4, 4, 17, 12, 13, 13, 12, 17, 4, 4],

				"smx-beginner": [25, 50, 25],
				"smx-single": [25, 21, 8, 21, 25],
				"smx-dual": [8, 17, 25, 25, 17, 8],
				"smx-full": [6, 8, 7, 8, 22, 22, 8, 7, 8, 6],
			},

			"StepTightening":
			{
				// Laterally, consider a foot moving 1/6 into a panel as the minimum distance to trigger it.
				"LateralMinPanelDistance": 0.166667,
				// Longitudinally, consider a foot moving 1/8 outside of a panel as the minimum distance to trigger it.
				"LongitudinalMinPanelDistance": -0.125,
				
				// Enable distance tightening.
				"DistanceTighteningEnabled": true,
				// With the above min panel distance values, 1.4 will:
				// - Allow a 2X1Y move.
				// - Penalize a 2X2Y move.
				// - Penalize a 3X move.
				// - Penalize a bracket move moving an average of 2 panels.
				"DistanceMin": 1.4,
				// 2 1/3 is the cutoff for 3 panel stretch in X.
				"DistanceMax": 2.333333,

				// Enable speed tightening.
				"SpeedTighteningEnabled": true,
				// Stop increasing costs at 16th notes at 170bpm.
				"SpeedMinTimeSeconds": 0.176471,
				// Start limiting at 16th notes at 125bpm.
				"SpeedMaxTimeSeconds": 0.24,
				// Do not use a distance cutoff for speed tightening.
				"SpeedTighteningMinDistance": 0.0,

				// Enable stretch tightening.
				"StretchTighteningEnabled": true,
				// Start limiting stretch moves at 2 1/3, which is a 3 panel move in X.
				"StretchDistanceMin": 2.333333,
				// Stop increasing costs for stretch moves at 3 1/3 which is a 4 panel move in X.
				"StretchDistanceMax": 3.333333,
			},

			"LateralTightening":
			{
				// Enable lateral tightening.
				"Enabled": true,
				// Penalize lateral movement steps that are 1.65 times as dense as the chart average.
				"RelativeNPS": 1.65,
				// Penalize lateral movement steps that are over 12 notes per second.
				"AbsoluteNPS": 12.0,
				// The body must be moving at least 3 arrow widths per second for lateral tightening to penalize steps.
				"Speed": 3.0,
			},

			"Facing":
			{
				// Do not penalize inward facing steps.
				"MaxInwardPercentage": 1.0,
				"InwardPercentageCutoff": 0.5,
				// Do not penalize outward facing steps.
				"MaxOutwardPercentage": 1.0,
				"OutwardPercentageCutoff": 0.5,
			},

			"Transitions":
			{
				// Do not enable transition limits.
				"Enabled": false,
			},
		},

		// Default stamina settings.
		"Stamina":
		{
			"StepTightening":
			{
				// This higher threshold would tighten up
				// normal charts too aggressively but for stamina charts it is better to err on
				// tight steps, especially for doubles. Even bpms in the low 110s feel bad when
				// streaming 16ths and having to move more than a bracket distance.
				"SpeedMaxTimeSeconds": 0.303,		// 16ths at 99
			},

			"LateralTightening":
			{
				// Disable Lateral Tightening.
				// We do not want to tighten very fast stamina songs.
				// We also do not want to try to tighten burst as it is not uncommon for stamina songs to
				// only have stream for a relatively small percentage of the song. This has a side-effect
				// of not laterally tightening actual burst (e.g. Night of the Flesh Eaters in SlowStreamz),
				// but this is the better tradeoff as this kind of stamina chart is much is less common.
				"Enabled": false,
			},

			"Transitions":
			{
				// Limit transitions for stamina charts.
				"Enabled": true,
				// Prefer steps which do not transition more than once every two measures.
				"StepsPerTransitionMin": 32,
				// Do not penalize slow transitions.
				"StepsPerTransitionMax": 1024,
				// Don't limit transitions for narrow ChartTypes.
				"MinimumPadWidth": 5,
				// Consider a transition to be moving from one half of the pads to the other half.
				"TransitionCutoffPercentage": 0.5,
			},
		},
	},
}
```
</details>

# Input

```json5
"InputDirectory": "C:\\Games\\StepMania 5\\Songs",
"InputNameRegex": ".*\\.(sm|ssc)$",
"InputChartType": "dance-single",
"DifficultyRegex": ".",
```

## `InputDirectory`

String type. Directory to search for song files within. Typically the `Songs` directory for StepMania, e.g. `"C:\\Games\\StepMania 5\\Songs"`. The application will recursively search through all of this directory's subdirectories for song files.

## `InputNameRegex`

String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching song file names. `".*\\.(sm|ssc)$"` will match all `sm` and `ssc` files.

## `InputChartType`

String type. [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md) for charts to use as input.

## `DifficultyRegex`

String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching StepMania [DifficultyNames](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/Difficulty.cpp#L12). `"."` will match all difficulties.

# Output

```json5
"OutputDirectory": "C:\\Fumen\\Exports",
"OutputChartType": "dance-double",
"OverwriteBehavior": "IfFumenGenerated",
"NonChartFileCopyBehavior": "DoNotCopy",
```

## `OutputDirectory`

String type. Directory to export converted files to. Using the same directory set for `InputDirectory` will result in an in-place conversion. Using a different directory will result in the files matched from `InputDirectory` to be updated and written to the specified directory. The directory structure from within `InputDirectory` will be maintained.

## `OutputChartType`

String type. [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md) for charts to generate.

## `OverwriteBehavior`

String type. Behavior for overwriting existing charts. Valid values are:

- `"DoNotOverwrite"`: Existing charts will not be updated.
- `"IfFumenGenerated"`: Existing charts will be updated if they were generated by this application using any version.
- `"IfFumenGeneratedAndNewerVersion"`: Existing charts will be updated if they were generated by this application using an older version.
- `"Always"`: Existing charts will always be updated.

## `NonChartFileCopyBehavior`

String type. Behavior for copying non-chart files from within a song's folder when `OutputDirectory` is different from `InputDirectory`. Valid values are:

- `"DoNotCopy"`: Do not copy non-chart files.
- `"IfNewer"`: Copy non-chart files if they do not exist in the destination directory, or if they do exist then only copy them if they are newer than the destination file.
- `"Always"`: Always copy the non-chart files. 

# Logging

```json5
"LoggerConfig":
{
	"LogLevel": "Info",
	"LogToFile": true,
	"LogDirectory": "C:\\Fumen\\Logs",
	"LogFlushIntervalSeconds": 20,
	"LogBufferSizeBytes": 10240,
	"LogToConsole": true,
},
```

## `LogLevel`

String type. The log level for the application. Valid values are `"Info"`, `"Warn"`, `"Error"`, or `"None"`.

## `LogToFile`

Boolean type. If `true` then the application will log to the directory specified by `LogDirectory`. If `false` then the application will not log to a file.

## `LogDirectory`

String type. Path to directory to log to.

## `LogFlushIntervalSeconds`

Number (integer) type. Interval in seconds to flush the log to disk. If set to `0` then the log will not flush on a timer.

## `LogBufferSizeBytes`

Number (integer) type. Must be non-negative. If the log buffer accumulates this many bytes of data it will flush to disk.

## `LogToConsole`

Boolean type. If `true` then the application will log to the console. If `false` then the application will not log to the console.

# Performance

```json5
"ConcurrentSongCount": -1,
"RegexTimeoutSeconds": 20.0,
```

## `ConcurrentSongCount`

Number (integer) type. Maximum number of songs to process concurrently. If omitted or less than 1 the number will be derived from the number of logical processors available. Increasing this value beyond the number of logical processors will have adverse performance effects as more memory will be used and more time will be devoted to asynchronous task management without any concurrency gains. Lowering this value below the number of logical processors will reduce the memory usage of the application but will make the program run more slowly as less work can be parallelized.

## `RegexTimeoutSeconds`

Number (double) type. Number of seconds to timeout after when testing [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) matches. Must be non-negative.

# Visualizations

```json5
"OutputVisualizations": true,
"VisualizationsDirectory": "C:\\Fumen\\Visualizations",
```

## `OutputVisualizations`

Boolean type. If `true` then the application will generate [Visualizations](Visualizations.md) in the `VisualizationsDirectory` for each chart generated.

## `VisualizationsDirectory`

String type. Path to directory to generate [Visualizations](Visualizations.md) to.

# Miscellaneous

```json5
"WarnOnDroppedSteps": true,
"CloseAutomaticallyWhenComplete": false,
```

## `WarnOnDroppedSteps`

Boolean type. If `true` then log warning messages when steps are omitted due to [StepType Fallbacks](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/StepTypeFallbacks.md).

## `CloseAutomaticallyWhenComplete`

Boolean type. If `true` then the application will close automatically when it has completed. If `false` then the application will wait for user input to exit.

# ExpressedChart Behavior Configuration

See also [ExpressedChart Configuration](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ExpressedChart.md#expressedchart-configuration).

[ExpressedChart](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ExpressedChart.md) behavior is controlled through [ExpressedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ExpressedChart.md#expressedchart-configuration) objects. A `DefaultExpressedChartConfig` must be specified. Multiple `ExpressedChartConfigs` may exist, and `ExpressedChartConfigRules` can be used to control which charts use which [ExpressedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ExpressedChart.md#expressedchart-configuration).

```json5
"DefaultExpressedChartConfig": "BalancedDynamic",
"ExpressedChartConfigRules":
[
	{"FileRegex": ".*(\\\\|/)MyPackWithNoBrackets.*(\\\\|/).*", "DifficultyRegex": ".", "Config": "NoBrackets"},
],
"ExpressedChartConfigs":
{
	"BalancedDynamic":
	{
		//...
	},
	//...
},
```

## `DefaultExpressedChartConfig`

String type. Key in `ExpressedChartConfigs` for the default object to use when no `ExpressedChartConfigRules` entry matches the chart to convert.

## `ExpressedChartConfigRules`

Array type. Each object in the array specifies rules for matching a particular chart and mapping it to an [ExpressedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ExpressedChart.md#expressedchart-configuration) object. If multiple matches exist, the match with the highest index in the array is used. Each object in the array has the following properties:

```json5
{"FileRegex": ".*(\\\\|/)MyPackWithNoBrackets.*(\\\\|/).*", "DifficultyRegex": ".", "Config": "NoBrackets"},
```

- ### `FileRegex`
	- String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching a chart's file name with full path.

- ### `DifficultyRegex`
	- String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching StepMania [DifficultyNames](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/Difficulty.cpp#L12) for a chart. `"."` will match all difficulties.

- ### `Config`
	- String type. Identifier of the [ExpressedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ExpressedChart.md#expressedchart-configuration) object within the `ExpressedChartConfigs` object to use.

## `ExpressedChartConfigs`

Object type. A Dictionary of string keys representing [ExpressedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ExpressedChart.md#expressedchart-configuration) identifiers to [ExpressedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ExpressedChart.md#expressedchart-configuration) objects.

# PerformedChart Behavior Configuration

See also [PerformedChart Configuration](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-configuration).

[PerformedChart](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/PerformedChart.md) behavior is controlled through [PerformedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-configuration) objects. A `DefaultPerformedChartConfig` must be specified. Multiple `PerformedChartConfigs` may exist, and `PerformedChartConfigRules` can be used to control which charts use which [PerformedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-configuration).

All `PerformedChartConfigs` other than the `DefaultPerformedChartConfig` may omit parameters. Omitted parameters will fallback to those specified in the `DefaultPerformedChartConfig`.

```json5
"DefaultPerformedChartConfig": "Default",
"PerformedChartConfigRules":
[
	{"FileRegex": ".*(\\\\|/)MyStaminaPack.*(\\\\|/).*", "DifficultyRegex": ".", "Config": "Stamina"},
],
"PerformedChartConfigs":
{
	"Default":
	{
		//...
	},
	//...
},
```

## `DefaultPerformedChartConfig`

String type. Key in `PerformedChartConfigs` for the default object to use when no `PerformedChartConfigRules` entry matches the chart to convert.

## `PerformedChartConfigRules`

Array type. Each object in the array specifies rules for matching a particular chart and mapping it to a [PerformedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-configuration) object. If multiple matches exist, the match with the highest index in the array is used. Each object in the array has the following properties:

```json5
{"FileRegex": ".*(\\\\|/)MyStaminaPack.*(\\\\|/).*", "DifficultyRegex": ".", "Config": "Stamina"},
```

- ### `FileRegex`
	- String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching a chart's file name with full path.

- ### `DifficultyRegex`
	- String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching StepMania [DifficultyNames](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/Difficulty.cpp#L12) for a chart. `"."` will match all difficulties.

- ### `Config`
	- String type. Identifier of the [PerformedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-configuration) object within the `PerformedChartConfigs` object to use.

## `PerformedChartConfigs`

Object type. A Dictionary of string keys representing [PerformedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-configuration) identifiers to [PerformedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-configuration) objects.
