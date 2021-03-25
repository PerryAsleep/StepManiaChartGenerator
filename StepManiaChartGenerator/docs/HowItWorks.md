# How It Works

## PadData
The application understands how the dance pads are laid out based on [PadData](PadData.md) files. These files define how the panels are positioned, and how a player can move on the panels. For example, this data defines for each panel, which other panels can be stepped to from it, can be bracketed with it, form a crossover with it, etc. The application comes with a [dance-single.json](../dance-single.json) file and a [dance-double.json](../dance-double.json) file.

## Config
The application's behavior can be controlled via a [Config](Config.md) file. The config file informs the application, among other things, where to read and write charts, what types of charts to use, and how to convert them. The application comes with a [config.json](../config.json) file with sensible defaults, but it should be updated before running to at least control where to read and write charts.

## StepGraphs
After the application starts it loads the [Config](Config.md) file, and the [PadData](PadData.md) files it will need, it then creates `StepGraphs` for each set of `PadData`. A `StepGraph` represents all possible positions the feet can be in on the pads, and all possible ways to move between those positions based on the [StepTypes](StepTypes.md) that the application understands. For implementation details, see [StepGraph.cs](../StepGraph.cs), [GraphNode.cs](../GraphNode.cs), and [GraphLink.cs](../GraphLink.cs).

## ExpressedCharts
When converting a chart, the application will load the input chart and then attempt to parse it into an `ExpressedChart`. An `ExpressedChart` represents how the body moves in order to satisfy a chart, rather than which arrows are being hit. For example, on a `dance-single` pad, if the player is standing with their left foot on L and their right foot on R, and the next arrows in the chart are L, then D, then R, the ExpressedChart representation would be roughly "With your left foot, step on the same arrow it is already on, then with your right foot step on a new arrow, then with your left foot, crossover your right foot in front".

In order to convert a chart into an `ExpressedChart` a search is performed through the corresponding `StepGraph` in order to find nodes which satisfy the steps. Many nodes will satisfy a step in the chart. In order to determine the most accurate expression, costs are assigned to nodes based on how likely or unlikely it would be for the chart author to have intended the [StepType](StepTypes.md) required to make the move. For example, with all else being equal, a pattern which can be alternated has a lower cost than expressing that pattern with double stepping as alternating steps is more natural. All possible paths are searched and the path with the lowest cost is chosen as the best `ExpressedChart` to represent the input chart. Some control over `ExpressedChart` parsing logic is outlined in the [Expressed Chart Behavior Configuration](Config.md#expressed-chart-behavior-configuration) section of the [Config](Config.md) guide. For implementation details, see [ExpressedChart.cs](../ExpressedChart.cs).

## PerformedCharts
Once the `ExpressedChart` has been determined, it can be applied to the `StepGraph` for the output chart type in order to form a `PerformedChart`. The `PerformedChart` represents the specific arrows which should be used. A search is performed through the output `StepGraph` for all valid paths which satisfy the `ExpressedChart`. The best path is chosen based on many [Config](Config.md) criteria outlined in the [Performed Chart Behavior Configuration](Config.md#performed-chart-behavior-configuration) section. When the best `PerformedChart` is found, it is converted back into a StepMania chart. For implementation details, see [PerformedChart.cs](../PerformedChart.cs).

## Visualizations
The application can optionally output html [Visualizations](Visualizations.md) of the `ExpressedChart` and the `PerformedChart`. This is primarily meant for debugging.