# Pad Data

`StepManiaChartConverter` uses json files named after the StepMania [StepsType](https://github.com/stepmania/stepmania/blob/6a645b4710dd6a89a5f22a2d849e86a98af5c9a3/src/GameManager.cpp#L47) to understand how the dance pads are laid out. The application comes with a [dance-single.json](../dance-single.json) file and a [dance-double.json](../dance-double.json) file. More can be added in the application's install directory. Once a pad data file is present in the install directory, it can be referenced in [Config](Config.md) for chart generation. Comments and trailing commas are supported.

## Example: dance-single.json
```json
{
	"YTravelDistanceCompensation": 0.5,

	"StartingPositions": [
		[
			[0, 3]
		]
	],

	"ArrowData":
	[
		// P1L
		{
			"X": 0,
			"Y": 1,
			// A foot on P1L can move next to P1D, P1U, or P1R.
			"ValidNextArrows": [false, true, true, true],
			"BracketablePairingsOtherHeel": [
				// Left foot on P1L is bracketable with the heel on other arrows on P1D.
				[false, true, false, false],
				// Right foot on P1L is a crossover and not bracketable.
				[false, false, false, false],
			],
			"BracketablePairingsOtherToe": [
				// Left foot on P1L is bracketable with the toes on other arrows on P1U.
				[false, false, true, false],
				// Right foot on P1L is a crossover and not bracketable.
				[false, false, false, false],
			],
			"OtherFootPairings": [
				// Left foot on P1L supports right foot on P1D, P1U, and P1R without crossovers.
				[false, true, true, true],
				// Right foot on P1L is a crossover with no normal left foot pairing.
				[false, false, false, false],
			],
			"OtherFootPairingsOtherFootCrossoverFront": [
				// Left foot on P1L is never a crossover position.
				[false, false, false, false],
				// Right foot on P1L is a crossover with left in front when left is on P1U.
				[false, false, true, false],
			],
			"OtherFootPairingsOtherFootCrossoverBehind": [
				// Left foot on P1L is never a crossover position.
				[false, false, false, false],
				// Right foot on P1L is a crossover with left in back when left is on P1D.
				[false, true, false, false],
			],
			"OtherFootPairingsInverted": [
				// Left foot on P1L is never inverted.
				[false, false, false, false],
				// Right foot on P1L is inverted with left when left is on P1R.
				[false, false, false, true],
			],
		},

		// P1D
		{
			"X": 1,
			"Y": 2,
			// A foot on P1L can move next to P1D, P1U, or P1R.
			"ValidNextArrows": [true, false, true, true],
			"BracketablePairingsOtherHeel": [
				// Left foot on P1D uses the heel.
				[false, false, false, false],
				// Right foot on P1D uses the heel.
				[false, false, false, false],
			],
			"BracketablePairingsOtherToe": [
				// Left foot on P1D is bracketable with the toes on other arrows on P1L.
				[true, false, false, false],
				// Right foot on P1D is bracketable with the toes on other arrows on P1R.
				[false, false, false, true],
			],
			"OtherFootPairings": [
				// Left foot on P1D supports right foot on P1U and P1R without crossovers.
				[false, false, true, true],
				// Right foot on P1D supports Left foot on P1L and P1U without crossovers.
				[true, false, true, false],
			],
			"OtherFootPairingsOtherFootCrossoverFront": [
				// Left foot on P1D is a crossover with right in front with right is on P1L.
				[true, false, false, false],
				// Right foot on P1D is a crossover with left in front when left is on P1R.
				[false, false, false, true],
			],
			"OtherFootPairingsOtherFootCrossoverBehind": [
				// None.
				[false, false, false, false],
				// Right foot on P1D is not a crossover with left in back.
				[false, false, false, false],
			],
			"OtherFootPairingsInverted": [
				// Left foot on P1D is never inverted.
				[false, false, false, false],
				// Right foot on P1D is never inverted.
				[false, false, false, false],
			],
		},

		// P1U
		{
			"X": 1,
			"Y": 0,
			// A foot on P1U can move next to P1L, P1D, or P1R.
			"ValidNextArrows": [true, true, false, true],
			"BracketablePairingsOtherHeel": [
				// Left foot on P1U is bracketable with the heel on other arrows on P1L.
				[true, false, false, false],
				// Right foot on P1U is bracketable with the heel on other arrows on P1R.
				[false, false, false, true],
			],
			"BracketablePairingsOtherToe": [
				// Left foot on P1U uses the toes.
				[false, false, false, false],
				// Right foot on P1U uses the toes.
				[false, false, false, false],
			],
			"OtherFootPairings": [
				// Left foot on P1U supports right foot on P1D and P1R without crossovers.
				[false, true, false, true],
				// Right foot on P1U supports left foot on P1L and P1D without crossovers.
				[true, true, false, false],
			],
			"OtherFootPairingsOtherFootCrossoverFront": [
				// None.
				[false, false, false, false],
				// None.
				[false, false, false, false],
			],
			"OtherFootPairingsOtherFootCrossoverBehind": [
				// Left foot on P1U is a crossover with right in back when right is on P1L.
				[true, false, false, false],
				// Right foot on P1U is a crossover with left in back when left is on P1R.
				[false, false, false, true],
			],
			"OtherFootPairingsInverted": [
				// Left foot on P1U is never inverted.
				[false, false, false, false],
				// Right foot on P1U is never inverted.
				[false, false, false, false],
			],
		},

		// P1R
		{
			"X": 2,
			"Y": 1,
			// A foot on P1R can move next to P1L, P1D, or P1U.
			"ValidNextArrows": [true, true, true, false],
			"BracketablePairingsOtherHeel": [
				// Left foot on P1R is a crossover and not bracketable.
				[false, false, false, false],
				// Right foot on P1R is bracketable with the heel on other arrows on P1D.
				[false, true, false, false],
			],
			"BracketablePairingsOtherToe": [
				// Left foot on P1R is a crossover and not bracketable.
				[false, false, false, false],
				// Right foot on P1R is bracketable with the toes on other arrows on P1U.
				[false, false, true, false],
			],
			"OtherFootPairings": [
				// Left foot on P1R is a crossover with no normal right foot pairing.
				[false, false, false, false],
				// Right foot on P1R supports left foot on P1L, P1D, and P1U without crossovers.
				[true, true, true, false],
			],
			"OtherFootPairingsOtherFootCrossoverFront": [
				// Left foot on P1R is a crossover with right in front when right is on P1U.
				[false, false, true, false],
				// Right foot on P1R is not a crossover position.
				[false, false, false, false],
			],
			"OtherFootPairingsOtherFootCrossoverBehind": [
				// Left foot on P1R is a crossover with right in back when right is on P1D.
				[false, true, false, false],
				// Right foot on P1R is not a crossover position.
				[false, false, false, false],
			],
			"OtherFootPairingsInverted": [
				// Left foot on P1R is inverted with right when right is on P1L.
				[true, false, false, false],
				// Right foot on P1R is never inverted.
				[false, false, false, false],
			],
		}
	]
}
```

## Configuration
The application considers X to mean left and right movement and Y to mean forward and backward movement. The provided Pad Data uses right as positive X and backwards as positive Y, but the application makes no assumptions about which directions are positive and which are negative.

### Pad Data
- **YTravelDistanceCompensation**: Number (double) type. A value to help take into account that movements between equally spaced and equally sized panels in Y take less foot movement to travel between than movements for panels separated by the same distance in X due to the length of a foot being significantly greater than the width of a foot. This value is used when computing individual step speeds for [Individual Step Tightening](Config.md/###-individual-step-tightening). A value of 1.0 for this parameter corresponds to the length of one panel.

- **StartingPositions**: Array type. Each value in this array is a tier of positions with lower index tiers being preferred to higher index tiers. The value at each tier is an array of equally preferred starting positions on the pad. A position is an array of two number (integer) values with the first index corresponding the left foot and the second index corresponding to the right foot. The value at each index is in the index of the arrow for the foot to start on. When creating a [PerformedChart](PerformedChart.md) searches begin using the starting position at the first tier, and if no path could be found, the application will try using the starting positions at the next tier, and so on. When multiple positions exist at the same tier, they are considered equally preferred and will be chosen in a random order. It is required that there be at least one tier and it is required that the first tier have exactly one position in it.

- **ArrowData**: Array type. Each value is an object describing each panel on the pad. See [Arrow Data](###-arrow-data) for more details.

### Arrow Data

- **X**: Number (integer) type. X Position of the panel relative to other panels.
- **Y**: Number (integer) type. Y Position of the panel relative to other panels.
- **ValidNextArrows**: Array type. The index of this array is the arrow within the ArrowData array. Each value in this array is a boolean representing whether the arrow at that index is a valid next step from this arrow for either foot. Expected to be `false` for this ArrowData's index.
- **BracketablePairingsOtherHeel**: Array type. The index of this array is the foot under consideration. It is expected this is of length 2, with the first index being the left foot and the second index being the right foot. For each foot, the value is another array. The index of this array is the arrow within the ArrowData array. Each value in this array is a boolean representing whether the arrow at that index is a valid pairing with this arrow for the given foot when the toes are on this arrow and the heel is on the other arrow.
- **BracketablePairingsOtherToe**: Array type. The index of this array is the foot under consideration. It is expected this is of length 2, with the first index being the left foot and the second index being the right foot. For each foot, the value is another array. The index of this array is the arrow within the ArrowData array. Each value in this array is a boolean representing whether the arrow at that index is a valid pairing with this arrow for the given foot when the heel is on this arrow and the toes are on the other arrow.
- **OtherFootPairings**: Array type. The index of this array is the foot under consideration. It is expected this is of length 2, with the first index being the left foot and the second index being the right foot. For each foot, the value is another array. The index of this array is the arrow within the ArrowData array. Each value in this array is a boolean representing whether the arrow at that index is a valid pairing for the other foot without crossing over or inverting. For example, if the first index is Left (0), the arrows listed are the valid positions for the Right foot without crossing over or inverting.
- **OtherFootPairingsOtherFootCrossoverFront**: Array type. The index of this array is the foot under consideration. It is expected this is of length 2, with the first index being the left foot and the second index being the right foot. For each foot, the value is another array. The index of this array is the arrow within the ArrowData array. Each value in this array is a boolean representing whether the arrow at that index forms a crossover in front for the given foot. For example, if the first index is Left (0), the arrows listed are the valid positions for the Right foot such that Right is crossing over the Left foot in front.
- **OtherFootPairingsOtherFootCrossoverBehind**: Array type. The index of this array is the foot under consideration. It is expected this is of length 2, with the first index being the left foot and the second index being the right foot. For each foot, the value is another array. The index of this array is the arrow within the ArrowData array. Each value in this array is a boolean representing whether the arrow at that index forms a crossover behind for the given foot. For example, if the first index is Left (0), the arrows listed are the valid positions for the Right foot such that Right is crossing over the Left foot in back.
- **OtherFootPairingsInverted**: Array type. The index of this array is the foot under consideration. It is expected this is of length 2, with the first index being the left foot and the second index being the right foot. For each foot, the value is another array. The index of this array is the arrow within the ArrowData array. Each value in this array is a boolean representing whether the arrow at that index forms an inverted body orientation for the given foot. An inverted position is one where if the player stood normally without twisting their body to face the screen they would be facing completely backwards. For example, if the first index is Left (0), the arrows listed are the valid positions for the Right foot such that the player is inverted. While there are two orientations for being inverted, every inverted position can be performed with both Right over Left and Left over Right, so we only need one data structure.