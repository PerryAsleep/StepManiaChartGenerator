# How It Works

## PadData

The application understands how the dance pads are laid out based on [PadData](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PadData.md) files. It understands how to perform moves on a pad through [StepGraphs](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepGraphs.md) that define how each [StepType](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTypes.md) can move between every valid position on the pads.

The application comes with `PadData` and `StepGraph` files for the following [ChartTypes](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ChartType.md), though more can be added using the [PadDataGenerator](https://github.com/PerryAsleep/PadDataGenerator) application.

```C#
dance_single
dance_double
dance_solo
dance_threepanel
pump_single
pump_halfdouble
pump_double
smx_beginner
smx_single
smx_dual
smx_full
smx_team
```

## Config

The application's behavior can be controlled via a [Config](Config.md) file. The config file informs the application, among other things, where to read and write charts, what types of charts to use, and how to convert them. The application comes with a [StepManiaChartGeneratorConfig.json](../StepManiaChartGeneratorConfig.json) file with sensible defaults, but it should be updated before running to at least control where to read and write charts.

## ExpressedCharts

When converting a chart, the application will load an input chart and then parse it into an [ExpressedChart](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ExpressedChart.md), which represents how the body moves in order to satisfy the chart by defining movements in terms of their [StepTypes](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/StepTypes.md).

## PerformedCharts

Once the `ExpressedChart` has been determined, a [PerformedChart](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/PerformedChart.md) is generated for the output [ChartType](https://github.com/PerryAsleep/StepManiaLibrary/blob/main/StepManiaLibrary/docs/ChartType.md), which defines the best way to execute the `ExpressedChart` on the output `ChartType` using specific arrows. This is then converted to a StepMania `sm` or `ssc` file and written back to disk.

## Visualizations

The application can optionally output html [Visualizations](Visualizations.md) of the `ExpressedChart` and the `PerformedChart`. This is primarily meant for debugging.