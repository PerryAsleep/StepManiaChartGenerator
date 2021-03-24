# Configuration
ChartConverter's behavior can be configured via the `config.json` file in the application's install directory. Comments and trailing commas are supported.

## Example config.json
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
			"DesiredArrowWeights": {
				"dance-single": [25, 25, 25, 25],
				// This distribution was determined by parsing doubles charts from many packs.
				"dance-double": [6, 12, 10, 22, 22, 12, 10, 6]
			},
		
			// 16ths at 170
			"IndividualStepTighteningMinTimeSeconds": 0.176471,
			// 16ths at 125
			"IndividualStepTighteningMaxTimeSeconds": 0.24,
		
			"LateralTighteningPatternLength": 5,
			"LateralTighteningRelativeNPS": 1.65,
			"LateralTighteningAbsoluteNPS": 12.0,
			"LateralTighteningSpeed": 3.0,
		},

		// Stamina settings are the same as default settings with a lower threshold for
		// IndividualStepTighteningMinTimeSeconds. This lower threshold would tighten up
		// normal charts too aggressively but for stamina charts it is better to err on
		// tight steps, especially for doubles. Even bpms in the low 110s feel bad when
		// streaming 16ths and having to move more than a bracket distance.
		"Stamina":
		{
			"DesiredArrowWeights": {
				"dance-single": [25, 25, 25, 25],
				"dance-double": [6, 12, 10, 22, 22, 12, 10, 6]
			},
		
			// 16ths at 170
			"IndividualStepTighteningMinTimeSeconds": 0.176471,
			// 16ths at 100
			"IndividualStepTighteningMaxTimeSeconds": 0.3,
		
			"LateralTighteningPatternLength": 5,
			"LateralTighteningRelativeNPS": 1.65,
			"LateralTighteningAbsoluteNPS": 12.0,
			"LateralTighteningSpeed": 3.0,
		}
	},

	"StepTypeReplacements" :
	{
		"SameArrow": [ "SameArrow" ],
		"NewArrow": [ "NewArrow" ],
		"CrossoverFront": [ "CrossoverFront" ],
		"CrossoverBehind": [ "CrossoverBehind" ],
		"InvertFront": [ "InvertFront" ],
		"InvertBehind": [ "InvertBehind" ],
		"FootSwap": [ "FootSwap" ],
		"BracketHeelNewToeNew": [ "BracketHeelNewToeNew" ],
		"BracketHeelNewToeSame": [ "BracketHeelNewToeSame", "BracketHeelSameToeNew" ],
		"BracketHeelSameToeNew": [ "BracketHeelSameToeNew", "BracketHeelNewToeSame" ],
		"BracketHeelSameToeSame": [ "BracketHeelSameToeSame" ],
		"BracketHeelSameToeSwap": [ "BracketHeelSameToeSwap", "BracketHeelSwapToeSame" ],
		"BracketHeelNewToeSwap": [ "BracketHeelNewToeSwap", "BracketHeelSwapToeNew" ],
		"BracketHeelSwapToeSame": [ "BracketHeelSwapToeSame", "BracketHeelSameToeSwap" ],
		"BracketHeelSwapToeNew": [ "BracketHeelSwapToeNew", "BracketHeelNewToeSwap" ],
		"BracketOneArrowHeelSame": [ "BracketOneArrowHeelSame" ],
		"BracketOneArrowHeelNew": [ "BracketOneArrowHeelNew", "BracketOneArrowToeNew" ],
		"BracketOneArrowToeSame": [ "BracketOneArrowToeSame" ],
		"BracketOneArrowToeNew": [ "BracketOneArrowToeNew", "BracketOneArrowHeelNew" ],
	},
}
```

## Basic Configuration

### Input
- **InputDirectory**: String type. Directory to search for song files within. Typically the `Songs` directory for StepMania, e.g. `"C:\\Games\\StepMania 5\\Songs"`. The application will recursively search through all of this directory's subdirectories for song files.
- **InputNameRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching song file names. `".*\\.(sm|ssc)$"` will match all `sm` and `ssc` files.
- **InputChartType**: String type. StepMania [StepsType](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/GameManager.cpp#L47) for charts to use as input. A [PadData](PadData.md) file must exist for this StepsType in the application's install directory.
- **DifficultyRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching StepMania [DifficultyNames](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/Difficulty.cpp#L12). `"."` will match all difficulties.

### Output
- **OutputDirectory**: String type. Directory to export converted files to. Using the same directory set for `InputDirectory` will result in an in-place conversion. Using a different directory will result in the files matched from `InputDirectory` to be updated and written to the specified directory. The directory structure from within `InputDirectory` will be maintained.
- **OutputChartType**: String type. StepMania [StepsType](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/GameManager.cpp#L47) for charts to generate. A [PadData](PadData.md) file must exist for this StepsType in the application's install directory.
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

## Expressed Chart Behavior Configuration
[ExpressedChart](ExpressedChart.md) behavior is controlled through `ExpressedChartConfig` objects. A `DefaultExpressedChartConfig` must be specified. Multiple `ExpressedChartConfigs` may exist, and `ExpressedChartConfigRules` can be used to control which charts which use which `ExpressedChartConfig`.

- **DefaultExpressedChartConfig**: String type. Key in `ExpressedChartConfigs` for the default object to use when no `ExpressedChartConfigRules` entry matches the chart to convert.
- **ExpressedChartConfigRules**: Array type. Each object in the array specifies rules for matching a particular chart and mapping it to an `ExpressedChartConfig` object. If multiple matches exist, the match with the highest index in the array is used. Each object in the array has the following properties:
	- **FileRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching a chart's file name with full path.
	- **DifficultyRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching StepMania [DifficultyNames](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/Difficulty.cpp#L12) for a chart. `"."` will match all difficulties.
	- **Config**: String type. Identifier of the `ExpressedChartConfig` object within the `ExpressedChartConfigs` object to use.
- **ExpressedChartConfigs**: Object type. A Dictionary of string keys representing `ExpressedChartConfig` identifiers to `ExpressedChartConfig` objects. `ExpressedChartConfig` objects have the following properties:
	- **DefaultBracketParsingMethod**: String type. The default method to use for parsing steps which could be jumps or brackets. The default method is used when the `BracketParsingDetermination` is set to `"UseDefaultMethod"`. Valid `BracketParsingMethod` values are:
		- `"Aggressive"`: The application will try to interpret patterns aggressively as brackets instead of jumps. It is difficult to exhaust all the conditions under which brackets will be preferred as it relates to the costs of the surrounding patterns. For a more detailed description of how the `"Aggressive"` behavior works, please see [ExpressedChart.cs](https://github.com/PerryAsleep/Fumen/blob/master/StepManiaChartGenerator/ExpressedChart.cs).
		- `"Balanced"`: The application will attempt to take a balanced approach to interpreting brackets and jumps. Generally, this errs on choosing brackets in ambiguous situations. It is difficult to exhaust all the conditions under which brackets will be preferred as it relates to the costs of the surrounding patterns. For a more detailed description of how the `"Balanced"` behavior works, please see [ExpressedChart.cs](https://github.com/PerryAsleep/Fumen/blob/master/StepManiaChartGenerator/ExpressedChart.cs).
		- `"NoBrackets"`: Jumps will be preferred to brackets. Steps will still be expressed as brackets if there are more than two simultaneous notes.
	- **BracketParsingDetermination**: String type. How the application should determine which `BracketParsingMethod` to use. Valid `BracketParsingDetermination` values are:
		- `"ChooseMethodDynamically"`: The application will use the properties on this `ExpressedChartConfig` object to determine which `BracketParsingMethod` to use for the chart, falling back to the `"Balanced"` method.
		- `"UseDefaultMethod"`: The `DefaultBracketParsingMethod` will be used.
	- **MinLevelForBrackets**: Number (integer) type. When parsing using `"ChooseMethodDynamically"`, charts with a difficulty rating under this level will use the `"NoBrackets"` `BracketParsingMethod`.
	- **UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets**: Boolean type. When parsing using `"ChooseMethodDynamically"` and the chart is above the `MinLevelForBrackets`, and this value is `true`, then use the `"Aggressive"` `BracketParsingMethod` if there exist more simultaneous steps in the chart than can be covered without brackets. In other words, if there are patterns that cannot possibly be performed without bracketing, then use the `"Aggressive"` method for the chart.
	- **BalancedBracketsPerMinuteForAggressiveBrackets**: Number (double) type. When parsing using `"ChooseMethodDynamically"` and the above parameters have still not determined which `BracketParsingMethod` to use, then parse the chart using the `"Balanced"` `BracketParsingMethod` and determine the number of brackets per minute. If the brackets per minute is above this value, then switch to the `"Aggressive"` method for the chart.
	- **BalancedBracketsPerMinuteForNoBrackets**: Number (double) type. When parsing using `"ChooseMethodDynamically"` and the above parameters have still not determined which `BracketParsingMethod` to use, then parse the chart using the `"Balanced"` `BracketParsingMethod` and determine the number of brackets per minute. If the brackets per minute is below this value, then switch to the `"NoBrackets"` method for the chart.

## Performed Chart Behavior Configuration
[PerformedChart](PerformedChart.md) behavior is controlled through `PerformedChartConfig` objects. A `DefaultPerformedChartConfig` must be specified. Multiple `PerformedChartConfigs` may exist, and `PerformedChartConfigRules` can be used to control which charts which use which `PerformedChartConfig`.

- **DefaultPerformedChartConfig**: String type. Key in `PerformedChartConfigs` for the default object to use when no `PerformedChartConfigRules` entry matches the chart to convert.
- **PerformedChartConfigRules**: Array type. Each object in the array specifies rules for matching a particular chart and mapping it to an `PerformedChartConfig` object. If multiple matches exist, the match with the highest index in the array is used. Each object in the array has the following properties:
	- **FileRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching a chart's file name with full path.
	- **DifficultyRegex**: String type. [Regular Expression](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference) for matching StepMania [DifficultyNames](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/Difficulty.cpp#L12) for a chart. `"."` will match all difficulties.
	- **Config**: String type. Identifier of the `PerformedChartConfig` object within the `PerformedChartConfigs` object to use.
- **PerformedChartConfigs**: Object type. A Dictionary of string keys representing `PerformedChartConfig` identifiers to `PerformedChartConfig` objects.

### PerformedChartConfig Objects
The properties on `PerformedChartConfig` objects can be grouped into the following behaviors:

#### **Misleading and Ambiguous Steps**
While there are not configuration values to control this behavior, when generating steps, the application will first try to avoid steps which would mislead the player, or which would be ambiguous. A misleading step is a step which a reasonable player would interpret incorrectly, for example a jump with two feet on the same two arrows that were just intended to be hit by one foot with a bracket. Any reasonable player would bracket again. An ambiguous step is a step which could be performed with more than one equally valid choice. For example after a jump, stepping on a new arrow that is of equal distance between the two arrows that were jumped on.

#### **Individual Step Tightening**
When generating steps, after trying to avoid misleading and ambiguous steps the application will then consider costs associated with individual steps, preferring patterns with a lower individual step cost. The values below configure how the application should assign costs to individual step types.

Individual Step Tightening can be disabled by setting `IndividualStepTighteningMinTimeSeconds` to `0.0`.

`IndividualStepTighteningMinTimeSeconds` and `IndividualStepTighteningMaxTimeSeconds` represent a range of times in seconds between steps for one foot. If a step is faster (lower) than `IndividualStepTighteningMaxTimeSeconds` then it will have a cost assigned to it based on its speed. Speed is determined by both the time of the step, and the distance of the step. The speed of a step is weighted based on where its time falls within the range defined by these values. The same step movement taking place over a time closer to `IndividualStepTighteningMaxTimeSeconds` will have a lower cost than it would if it took place over a time closer to `IndividualStepTighteningMinTimeSeconds`. Steps which take longer than `IndividualStepTighteningMaxTimeSeconds` will have no individual step tightening cost. Steps which take shorter than `IndividualStepTighteningMinTimeSeconds` will all be considered equally fast, though larger movements at this speed will still be considered more costly than shorter movements.

For example, to configure the application to apply weights based on individual steps starting at 16th notes at 125bpm and peaking at 16th notes at 170bpm set `IndividualStepTighteningMaxTimeSeconds` to `0.24` and `IndividualStepTighteningMinTimeSeconds` to `0.176471`
```
(60 seconds per minute / (4 notes per beat x 125 beats per minute)) x 2 feet) = 0.24 seconds
(60 seconds per minute / (4 notes per beat x 170 beats per minute)) x 2 feet) = 0.176471 seconds
```

Note the extra multiplication by 2 feet in the above math since in a 16th note pattern each foot is actually moving at half that speed (each foot hits with eighth note frequency).

- **IndividualStepTighteningMinTimeSeconds**: Number (double) type. Time in seconds between steps for one foot. See above explanation. If set to `0.0` then the application will not apply any costs to individual steps based on their speed.
- **IndividualStepTighteningMaxTimeSeconds**: Number (double) type. Time in seconds between steps for one foot. See above explanation.

#### **Lateral Body Movement Tightening**
When generating steps, after considering Individual Step Tightening costs, the application will then consider costs for how quickly the body moves laterally, preferring patterns with lower lateral body movement costs during fast sections. The values below configure how the application should assign costs based on lateral body movement.

Lateral Body Movement Tightening can be disabled by setting `LateralTighteningPatternLength` to `0`.

For each step the application will consider the preceding `LateralTighteningPatternLength` steps. If all the steps in this set move the body in the same lateral direction and as a whole they move over `LateralTighteningSpeed` arrows per second, then the notes per second of this segment will be compared against `LateralTighteningRelativeNPS` and `LateralTighteningAbsoluteNPS`. If the notes per second is faster than `LateralTighteningAbsoluteNPS` or if the notes per second is faster than the average notes per second of the chart as a whole multiplied by `LateralTighteningAbsoluteNPS`, then the step at the end of the section will accrue a cost based on the lateral body speed of section.

Note that in this context "same lateral direction" means steps which do not change directions. For example, steps which move left and keep the body stationary are both valid for a section to be considered all moving in the left direction.

Note also that in this context "arrows per second" for `LateralTighteningSpeed` refers to the width of the arrow panels on the pad. For example in a `dance-double` chart if the body moved from centered over the player 1 arrows to centered over the player 2 arrows in 1 second then it would be moving at 3 arrows per second since the centers are 3 arrows apart.

- **LateralTighteningPatternLength**: Number (integer) type. The number of steps to look for uninterrupted movement in the same direction. If set to `0` then the application will not apply any costs based on lateral body movement.
- **LateralTighteningRelativeNPS**: Number (double) type. Multiplier. If the notes per second of a section of steps is over the chart's average notes per second multiplied by this value then the section is considered to be fast enough to apply a lateral body movement cost to.
- **LateralTighteningAbsoluteNPS**: Number (double) type. Absolute notes per section value. If the notes per second of a section of steps is over this value then the section is considered to be fast enough to apply a lateral body movement cost to.
- **LateralTighteningSpeed**: Number (double) type. Body speed in arrows per second over which fast sections will accrue costs.

#### **Arrow Distribution**
After considering Individual Step Tightening and Lateral Body Movement Tightening the application will then consider costs for how closely the arrow distribution of the chart up to the step under consideration matches the desired distribution, preferring patterns which result in a closer match. For doubles, this results in the application generating charts which move the body back and forth between the sides of the pads.

- **DesiredArrowWeights**: Object type. Each value in this object is a key value pair. The key is a string representing a StepMania [StepsType](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/GameManager.cpp#L47) and the value is an array of integers representing the desired weights per lane for that type. When generating charts for the specified `OutputChartType`, `DesiredArrowWeights` is used to control, for each lane, what percentage of the chart's steps should fall in that lane. For example, for doubles it is desirable to follow a bell-like curve with more steps on the middles than on the outer arrows. To specify this you could specify `"dance-double": [6, 12, 10, 22, 22, 12, 10, 6]`. The values do not need to sum to 100. They will be compared against each other and normalized when used to generate charts.

	Even with equal Individual Step Tightening and Lateral Body Movement Tightening costs, these weights may not be honored precisely depending on how much they deviate from allowed steps. For example doubles weights of `"dance-double": [50, 0, 0, 0, 0, 0, 0, 50]` will not result in a chart that only uses the outermost arrows as this level of stretch is not supported, and many step types cannot be represented with just those two arrows (for example, it would be impossible to perform a crossover).

## Miscellaneous Conversion Behavior Configuration

### StepType Replacements
- **StepTypeReplacements**: Object type. Each value in this object is a key value pair. The key is a string representing a [StepType](StepTypes.md) and the value is an array of StepTypes that the application can use instead of the key StepType when generating a chart. The main purpose of this structure is to allow the application to be more lenient with brackets, though it can also be used to replace certain StepTypes. For example, to remove crossovers you could set `"CrossoverFront": [ "NewArrow" ]` and `"CrossoverBehind": [ "NewArrow" ]`.