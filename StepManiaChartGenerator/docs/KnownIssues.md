# Known Issues

- [ExpressedChart](HowItWorks.md#-expressedCharts) searches will only search from the first tier of `StartingPositions` from the corresponding [PadData](PadData.md).
- Visualizations are only supported for `dance-single` and `dance-double` StepsTypes.
- There is no way to specify *tiers* of acceptable replacements for [StepTypes](StepTypes.md) when creating a [PerformedChart](HowItWorks.md#-performedCharts).
	- This hasn't been an issue when converting `dance-single` to `dance-double` charts as all patterns in `dance-single` can be performed in `dance-double`, but this is not true the other way around. For example, a run from 1-8 run from P1 L to P2 R has a momentary invert on the middles. There is no singles pattern which satisfies that expression, so we need a mechanism to replace `InvertFront` and `InvertBehind` with acceptable fallbacks when searches fail.