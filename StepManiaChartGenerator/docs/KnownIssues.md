# Known Issues and Unsupported Features

- [ExpressedChart](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ExpressedChart.md) generation is not perfect. Sometimes it will express a chart in a way the author did not intend.
- Hands (i.e. 5+ simultaneous notes or patterns which cannot be performed with bracketing by two feet) are not supported.
- Repeated patterns (i.e. the same group of steps repeated in the same chart) are not understood by the application and will not be preserved.
	- As such, charts which use negative stops gimmicks or other gimmicks which rely on repeated patterns may not generate charts as expected.
- [Visualizations](Visualizations.md) are only supported for `dance-single` and `dance-double` [ChartTypes](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ChartType.md).
- When generating [PerformedCharts](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md) the application will always try to avoid misleading and ambiguous steps regardles of if the original chart had ambiguous or misleading steps.
- Small diffs to `sm` and `ssc` files beyond the expected diffs from chart generation may occur.
- When configured to [copy non-chart files](Config.md/#nonchartfilecopybehavior) the application will only copy files in the same directory as a chart file. It will not, for example, copy pack assets.
