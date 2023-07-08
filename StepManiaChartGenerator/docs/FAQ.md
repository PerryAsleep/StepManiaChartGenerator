# Frequently Asked Questions

# General Usage

## When I run `StepManiaChartGenerator.exe` nothing happens. How do I fix this?

This is most likely because you don't have the required [.Net Runtime 7.0.8](https://dotnet.microsoft.com/en-us/download/dotnet/7.0). Try running the application through a command prompt. If text is output indicating you are missing a required .Net runtime, install the runtime and try again. 

## Can I run `StepManiaChartGenerator` on other operating systems besides Windows?

No.

## `StepManiaChartGenerator` didn't generate any charts. Why?

The default [Config](Config.md) values are sensible but won't work for every environment. Read through the documentation but most importantly make sure the following settings are correct for your environment:
- [InputDirectory](Config.md#inputdirectory)
- [OutputDirectory](Config.md#outputdirectory)
- [InputChartType](Config.md#inputcharttype)
- [OutputChartType](Config.md#outputcharttype)

## How do I convert from X kind of chart to Y kind of chart?

Set [InputChartType](Config.md#inputcharttype) and [OutputChartType](Config.md#outputcharttype) to the types you want. If the types you want [aren't supported](Config.md#supported-charttypes), see [Adding ChartTypes](Config.md#adding-charttypes).

# Performance

## How do I make `StepManiaChartGenerator` use less memory?

Set [ConcurrentSongCount](Config.md#concurrentsongcount) to an explicit value that is lower than the number of logical processors on your computer. This will also make `StepManiaChartGenerator` run more slowly.

## I'm seeing `RegexMatchTimeoutException` errors in the logs. How do I fix this?

This might happen if you have complex regular expressions or inputs for your [ExpressedChartConfigRules](Config.md#expressedchartconfigrules) or [PerformedChartConfigRules](Config.md#performedchartconfigrules). Try increasing the value for [RegexTimeoutSeconds](Config.md#regextimeoutseconds).

# Adjusting Generated Charts

## How do I generate specific kinds of charts like stamina charts or tech charts?

`StepManiaChartGenerator` doesn't distinguish between charts in this way. However it is designed to create natural charts of all types, including tech and stamina charts. See the question below for defining rules based on specific songs or packs.

## How do I define rules for only some songs or packs, and define different rules for others?

See [ExpressedChartConfigRules](Config.md#expressedchartconfigrules) for defining rules for to interpret charts differently depending on their song or pack.

See [PerformedChartConfigRules](Config.md#performedchartconfigrules) for defining rules for how to generate charts with different qualities based on the input song or pack.

## How do I generate charts that transition less frequently?

Enable [Transition Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/TransitionControls.md) and increase `StepsPerTransitionMin`.

## How do I prevent transitions during burst?

See [Lateral Tightening Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/LateralTighteningControls.md).

## Why does my generated chart still transition more than it is set to?

Transition Controls are evaluated after many other rules. See [PerformedChart Determination](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-determination) for the full order. When individual steps don't adhere to specific rules it is because the path with those steps is the lowest cost path when evaluating all rules in order. Loosening or disabling higher priority rules like [Step Tightening](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTighteningControls.md) will result in charts which follow specified transition limits more closely.

## Why do my generated doubles tech charts sometimes get stuck on one side of the pads?

Depending on the pad layout, complex technical patterns from the input [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ChartType.md) may not be able to be expressed in the output [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ChartType.md) with patterns that allow for transitions. Particularly for `dance-double`, the possibility space for transitions is relatively limited compared to other [ChartTypes](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ChartType.md) like `pump-double`.

## Why do my generated doubles stamina charts sometimes get stuck on one side of the pads?

If you see a generated stamina chart that is clearly stuck on one side of the pads for a very long period of time it is likely that [Lateral Tightening Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/LateralTighteningControls.md) are [enabled](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/LateralTighteningControls.md#enabled) and the song's notes-per-second is above the [Absolute Notes Per Second](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/LateralTighteningControls.md#absolutenps) for limiting lateral movement. Try disabling [Lateral Tightening Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/LateralTighteningControls.md) or setting the [AbsoluteNPS](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/LateralTighteningControls.md#absolutenps) value above the notes-per-second of your chart. The default `Stamina` [PerformedChart Configs](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-configuration) have [Lateral Tightening Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/LateralTighteningControls.md) enabled with a high [AbsoluteNPS](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/LateralTighteningControls.md#absolutenps). If you are already trying to disable [Lateral Tightening Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/LateralTighteningControls.md), ensure your [PerformedChartConfigRules](Config.md#performedchartconfigrules) are configured properly such that the expected [PerformedChart Config](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md#performedchart-configuration) is applied to your chart.

If you see a generated stamina chart where it is on one side of the pads longer than you would like but it still does transition, and the input chart has long sections where all steps are to new arrows and no steps step back on the same arrow that was stepped on previously, then [Step Tightening Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTighteningControls.md) are likely preventing pad transitions if the output [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ChartType.md) requires taking steps which move the feet longer distances in order to transition (like `dance-double`). In other words, for `dance-double`, if the current config rules prefer shortest distances, there is no way to transition without breaking that preference *if* all steps need to step on new arrows. The best way to mitigate this would be to increase the [SpeedTighteningMinDistance](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTighteningControls.md#speedtighteningmindistance) to allow larger fast steps that can allow for transitions.

## How do I generate stamina charts that use steps which individually move more or move less?

Adjust the [Step Tightening Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTighteningControls.md). Two sets of controls are most relevant:
 - [Distance Tightening](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTighteningControls.md#distance-tightening)
	- Increase [DistanceMin](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTighteningControls.md#distancemin) and [DistanceMax](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTighteningControls.md#distancemax) to generate steps which move more. Decrease them to generate steps which move less.
 - [Speed Tightening](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTighteningControls.md#speed-tightening)
	- Increase [SpeedMinTimeSeconds](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTighteningControls.md#speedmintimeseconds) and [SpeedMaxTimeSeconds](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTighteningControls.md#speedmaxtimeseconds) to generate steps which move less. Decrease them to generate steps which move more.

## How do I limit uncomfortable patterns?

Chart generation always applies the [StepTypes](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTypes.md) from the input chart to the output chart. So if for example the input chart has crossovers, the output chart will also have crossovers. The only controls available for tuning generated steps are:
 - Individual [Step Tightening Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTighteningControls.md)
 - [Facing Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/FacingControls.md)
 - [Transition Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/TransitionControls.md)
 - [Lateral Tightening Controls](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/LateralTighteningControls.md)

## Why does my generated chart seem like it has brackets when my original chart does not have brackets?

 It is possible the application interpreted a move as a bracket when the original author intended the pattern to be a jump. [ExpressedChart Configurations](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ExpressedChart.md#expressedchart-configuration) can be made to less aggressively interpret brackets, or to disable interpreting moves as brackets entirely.

## Why does my generated chart have jumps when my original chart has brackets?

[ExpressedChart Configurations](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ExpressedChart.md#expressedchart-configuration) can be adjusted to aggressively interpret brackets. However in rare cases brackets in extremely technical patterns (like bracketing in a crossed over orientation, or bracketing as part of a complex footswap pattern) may still be interpreted as jumps. This is a [known issue](KnownIssues.md).

## How does `StepManiaChartGenerator` define stretch?

Stretch is defined per [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ChartType.md) in that [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ChartType.md)'s [PadData](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PadData.md) and [StepGraph](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepGraphs.md). The [PadData](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PadData.md) and [StepGraph](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepGraphs.md) files provided with `StepManiaChartGenerator` all define stretch as moves which spread the legs by a distance of more than two panels in either X or Y.
