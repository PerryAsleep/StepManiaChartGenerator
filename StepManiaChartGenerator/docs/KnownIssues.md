# Known Issues and Unsupported Features
In general this program has only been thoroughly exercised when converting `dance-single` charts to `dance-double` charts. While it is set up in such a way to support more [StepsTypes](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/GameManager.cpp#L47), the application may need updates to support them without issues.

- [ExpressedChart](HowItWorks.md#expressedCharts) generation is not perfect. Sometimes it will express a chart in a way the author did not intend.
- Hands (i.e. 5+ simultaneous notes or patterns which cannot be performed with bracketing by two feet) are not supported.
- Stetch moves (i.e. doubles patterns involving feet on both downs or a wider position) are not supported in the provided [PadData](PadData.md).
	- The PadData could be updated to allow them, though it may result in unintentionally energetic steps. The program does not understand stretch moves as a distinct [StepTypes](StepTypes.md) and will only limit their usage based on [Individual Step Tightening](Config.md/#individual-step-tightening).
- Patterns (i.e. the same group of steps repeated in the same chart) are not understood by the application and will not be preserved.
	- As such, charts which use negative stops gimmicks or other gimmicks which rely on patterns may not generate charts as expected.
- [ExpressedChart](HowItWorks.md#expressedCharts) searches will only search from the first tier of `StartingPositions` from the corresponding [PadData](PadData.md).
	- This hasn't been an issue when converting `dance-single` to `dance-double` charts since all `dance-single` patterns can be reached from the first tier of `StartingPositions`.
- [Visualizations](Visualizations.md) are only supported for `dance-single` and `dance-double` StepsTypes.
- Certain technical patterns like bracketing in a crossover position are not supported.
- When generating [PerformedCharts](HowItWorks.md#performedCharts) the application will always try to avoid misleading and ambiguous steps regardles of if the original chart had ambiguous or misleading steps.
- There is no way to specify *tiers* of acceptable replacements for [StepTypes](StepTypes.md) when creating a [PerformedChart](HowItWorks.md#performedCharts).
	- This hasn't been an issue when converting `dance-single` to `dance-double` charts as all patterns in `dance-single` can be performed in `dance-double`, but this is not true the other way around. For example, a 1-8 run from P1 L to P2 R has a momentary invert on the middles. There is no singles pattern which satisfies that expression, so a mechanism to replace `InvertFront` and `InvertBehind` with acceptable fallbacks when searches fail would be needed to convert such a chart.
- Small diffs to sm and ssc files beyond the expected diffs from chart generation may occur.
- When configured to [copy non-chart files](Config.md/#output) the application will only copy files in the same directory as a chart file. It will not, for example, copy pack assets.