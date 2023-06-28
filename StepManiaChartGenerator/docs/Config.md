# Configuration

`StepManiaChartGenerator`'s behavior can be configured via the `StepManiaChartGeneratorConfig.json` file in the application's install directory. Comments and trailing commas are supported.

## Supported ChartTypes

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

### Adding ChartTypes

Additional [ChartTypes](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md) beyond the default types specified above can be added if they:
1) Are for one player only.
2) Are listed in the [full ChartType list](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md).

To add another [ChartTypes](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md), add a [PadData](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/PadData.md) and [StepGraph](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/StepGraphs.md) file to the application's install directory. These files can be generated from simple input with [PadDataGenerator](https://github.com/PerryAsleep/PadDataGenerator).

## Example Configuration

<details>
	<summary>Example</summary>

```json
{
	"LogLevel": "Info",
	"LogToFile": true,
	"LogDirectory": "C:\\Fumen\\Logs",
	"LogFlushIntervalSeconds": 20,
	"LogBufferSizeBytes": 10240,
	"LogToConsole": true,

	"RegexTimeoutSeconds": 10.0,
	"CloseAutomaticallyWhenComplete": false,

	"InputDirectory": "C:\\Games\\StepMania 5\\Songs",
	"InputNameRegex": ".*\\.(sm|ssc)$",
	"InputChartType": "dance-single",
	"DifficultyRegex": ".",

	"OutputDirectory": "C:\\Fumen\\Exports",
	"OutputChartType": "dance-double",
	"OverwriteBehavior": "IfFumenGenerated",
	"NonChartFileCopyBehavior": "DoNotCopy",
	"WarnOnDroppedSteps": true,

	"OutputVisualizations": true,
	"VisualizationsDirectory": "C:\\Fumen\\Visualizations",

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
			"MinLevelForBrackets": 9,
			"UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets": true,
			// Interpret charts with three brackets per minute or more as charts which should
			// aggressively interpret bracketable jumps as brackets.
			"BalancedBracketsPerMinuteForAggressiveBrackets": 3.0,
			// Interpret charts with one bracket every minute and 45 seconds or less as charts
			// which should not have brackets. Many non-technical charts have jumps which patterns
			// which can reasonably be done by bracketing. With balanced parsing, these charts
			// would produce brackets. This threshold helps keeps non-technical charts bracket-free.
			"BalancedBracketsPerMinuteForNoBrackets": 0.571,
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
				"TravelSpeedMinTimeSeconds": 0.176471,	// 16ths at 170
				"TravelSpeedMaxTimeSeconds": 0.24,		// 16ths at 125
				"TravelDistanceMin": 2.25,
				"TravelDistanceMax": 3.0,
				"StretchDistanceMin": 3.0,
				"StretchDistanceMax": 4.0,
				"DistanceCompensationX": 0.0,
				"DistanceCompensationY": 0.5,
			},

			"LateralTightening":
			{
				"RelativeNPS": 1.65,
				"AbsoluteNPS": 12.0,
				"Speed": 3.0,
			},

			"Facing":
			{
				"MaxInwardPercentage": 1.0,
				"MaxOutwardPercentage": 1.0,
			},
		},

		// Stamina settings are the same as default settings with a lower threshold for
		// IndividualStepTighteningMinTimeSeconds. This lower threshold would tighten up
		// normal charts too aggressively but for stamina charts it is better to err on
		// tight steps, especially for doubles. Even bpms in the low 110s feel bad when
		// streaming 16ths and having to move more than a bracket distance.
		"Stamina":
		{
			"StepTightening":
			{
				"TravelSpeedMaxTimeSeconds": 0.303,		// 16ths at 99
			},
		}
	},
}
```
</details>

## Basic Configuration

### Input

- **InputDirectory**: String type. Directory to search for song files within. Typically the `Songs` directory for StepMania, e.g. `"C:\\Games\\StepMania 5\\Songs"`. The application will recursively search through all of this directory's subdirectories for song files.
- **InputNameRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching song file names. `".*\\.(sm|ssc)$"` will match all `sm` and `ssc` files.
- **InputChartType**: String type. [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md) for charts to use as input.
- **DifficultyRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching StepMania [DifficultyNames](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/Difficulty.cpp#L12). `"."` will match all difficulties.

### Output

- **OutputDirectory**: String type. Directory to export converted files to. Using the same directory set for `InputDirectory` will result in an in-place conversion. Using a different directory will result in the files matched from `InputDirectory` to be updated and written to the specified directory. The directory structure from within `InputDirectory` will be maintained.
- **OutputChartType**: String type. [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ChartType.md) for charts to generate.
- **OverwriteBehavior**: String type. Behavior for overwriting existing charts. Valid values are:
	- `"DoNotOverwrite"`: Existing charts will not be updated.
	- `"IfFumenGenerated"`: Existing charts will be updated if they were generated by this application using any version.
	- `"IfFumenGeneratedAndNewerVersion"`: Existing charts will be updated if they were generated by this application using an older version.
	- `"Always"`: Existing charts will always be updated.
- **NonChartFileCopyBehavior**: String type. Behavior for copying non-chart files from within a song's folder when `OutputDirectory` is different from `InputDirectory`. Valid values are:
	- `"DoNotCopy"`: Do not copy non-chart files.
	- `"IfNewer"`: Copy non-chart files if they do not exist in the destination directory, or if they do exist then only copy them if they are newer than the destination file.
	- `"Always"`: Always copy the non-chart files. 

### Logging

- **LogLevel**: String type. The log level for the application. Valid values are `"Info"`, `"Warn"`, `"Error"`, or `"None"`.
- **LogToFile**: Boolean type. If `true` then the application will log to the directory specified by `LogDirectory`. If `false` then the application will not log to a file.
- **LogDirectory**: String type. Path to directory to log to.
- **LogFlushIntervalSeconds**: Number (integer) type. Interval in seconds to flush the log to disk. If set to `0` then the log will not flush on a timer.
- **LogBufferSizeBytes**: Number (integer) type. Must be non-negative. If the log buffer accumulates this many bytes of data it will flush to disk.
- **LogToConsole**: Boolean type. If `true` then the application will log to the console. If `false` then the application will not log to the console.

### Visualizations

- **OutputVisualizations**: Boolean type. If `true` then the application will generate [Visualizations](Visualizations.md) in the `VisualizationsDirectory` for each chart generated.
- **VisualizationsDirectory**: String type. Path to directory to generate [Visualizations](Visualizations.md) to.

### Miscellaneous

- **RegexTimeoutSeconds**: Number (double) type. Number of seconds to timeout after when testing [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) matches. Must be non-negative.
- **CloseAutomaticallyWhenComplete**: Boolean type. If `true` then the application will close automatically when it has completed. If `false` then the application will wait for user input to exit.
- **WarnOnDroppedSteps**: Boolean type. If `true` then log warning messages when steps are omitted due to [StepType Fallbacks](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/StepTypeFallbacks.md).

## Expressed Chart Behavior Configuration

See [ExpressedChart Configuration](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ExpressedChart.md#expressedchart-configuration).

[ExpressedChart](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ExpressedChart.md) behavior is controlled through [ExpressedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/ExpressedChart.md#expressedchart-configuration) objects. A `DefaultExpressedChartConfig` must be specified. Multiple `ExpressedChartConfigs` may exist, and `ExpressedChartConfigRules` can be used to control which charts use which `ExpressedChartConfig`.

- **DefaultExpressedChartConfig**: String type. Key in `ExpressedChartConfigs` for the default object to use when no `ExpressedChartConfigRules` entry matches the chart to convert.
- **ExpressedChartConfigRules**: Array type. Each object in the array specifies rules for matching a particular chart and mapping it to an `ExpressedChartConfig` object. If multiple matches exist, the match with the highest index in the array is used. Each object in the array has the following properties:
	- **FileRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching a chart's file name with full path.
	- **DifficultyRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching StepMania [DifficultyNames](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/Difficulty.cpp#L12) for a chart. `"."` will match all difficulties.
	- **Config**: String type. Identifier of the `ExpressedChartConfig` object within the `ExpressedChartConfigs` object to use.
- **ExpressedChartConfigs**: Object type. A Dictionary of string keys representing `ExpressedChartConfig` identifiers to `ExpressedChartConfig` objects.

## Performed Chart Behavior Configuration

See [PerformedChart Configuration](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-configuration).

[PerformedChart](https://github.com/PerryAsleep/StepManiaLibrary/tree/main/StepManiaLibrary/docs/PerformedChart.md) behavior is controlled through [PerformedChartConfig](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-configuration) objects. A `DefaultPerformedChartConfig` must be specified. Multiple `PerformedChartConfigs` may exist, and `PerformedChartConfigRules` can be used to control which charts use which `PerformedChartConfig`.

All `PerformedChartConfigs` other than the `DefaultPerformedChartConfig` may omit parameters. Omitted parameters will fallback to those specified in the `DefaultPerformedChartConfig`.

- **DefaultPerformedChartConfig**: String type. Key in `PerformedChartConfigs` for the default object to use when no `PerformedChartConfigRules` entry matches the chart to convert.
- **PerformedChartConfigRules**: Array type. Each object in the array specifies rules for matching a particular chart and mapping it to an `PerformedChartConfig` object. If multiple matches exist, the match with the highest index in the array is used. Each object in the array has the following properties:
	- **FileRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching a chart's file name with full path.
	- **DifficultyRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching StepMania [DifficultyNames](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/Difficulty.cpp#L12) for a chart. `"."` will match all difficulties.
	- **Config**: String type. Identifier of the `PerformedChartConfig` object within the `PerformedChartConfigs` object to use.
- **PerformedChartConfigs**: Object type. A Dictionary of string keys representing `PerformedChartConfig` identifiers to `PerformedChartConfig` objects.
