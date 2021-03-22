using System;
using System.Linq;
using Fumen;
using static StepManiaChartGenerator.Constants;

namespace StepManiaChartGenerator
{
	/// <summary>
	/// Types of steps which can be performed with one foot.
	/// Jumps are combinations of StepTypes for each foot.
	/// </summary>
	public enum StepType
	{
		// Simple step types.
		SameArrow,
		NewArrow,
		CrossoverFront,
		CrossoverBehind,
		InvertFront,
		InvertBehind,
		FootSwap,

		// Bracket two arrows with one foot.
		BracketHeelNewToeNew,
		BracketHeelNewToeSame,
		BracketHeelSameToeNew,
		BracketHeelSameToeSame,
		BracketHeelSameToeSwap,
		BracketHeelNewToeSwap,
		BracketHeelSwapToeSame,
		BracketHeelSwapToeNew,

		// Bracket one arrow (e.g. due to holding on one arrow).
		BracketOneArrowHeelSame,
		BracketOneArrowHeelNew,
		BracketOneArrowToeSame,
		BracketOneArrowToeNew
	}

	/// <summary>
	/// Actions that can be performed when stepping with a foot.
	/// Releases can only occur on StepTypes that keep the foot on the same arrow.
	/// Not considering rolls as a unique action.
	/// </summary>
	public enum FootAction
	{
		Tap,
		Hold,
		Release
	}

	/// <summary>
	/// The orientation the body can be in based on certain steps.
	/// InvertFront and InvertBack will result in inverted orientations.
	/// </summary>
	public enum BodyOrientation
	{
		Normal,
		InvertedRightOverLeft,
		InvertedLeftOverRight
	}

	/// <summary>
	/// The state a foot on an arrow in StepGraph can be in.
	/// There is no none / lifted state.
	/// Each foot is on one or more arrows each in one of these states.
	/// Rolls are considered no different than holds in the StepGraph.
	/// </summary>
	public enum GraphArrowState
	{
		Resting,
		Held
	}

	/// <summary>
	/// Data for each StepType.
	/// </summary>
	public class StepData
	{
		/// <summary>
		/// Cached FootAction combinations for a step.
		/// Steps that involve one arrow will have an array of length 1 arrays.
		/// Steps that involve multiple arrows (brackets) will have an array of length 2 arrays.
		/// First index is the set of actions.
		///  For example, for length 2 FootAction combinations we would have roughly 9 entries
		///  since it is combining the 3 FootActions with 3 more.
		/// Second index is the arrow for the foot in question.
		///  For most StepTypes this will be a length 1 arrow. For brackets this will be a
		///  length 2 array.
		/// </summary>
		public readonly FootAction[][] ActionSets;

		/// <summary>
		/// Which foot portions to use when filling the graph.
		/// Most StepTypes use just one foot portion, DefaultFootPortion.
		/// Bracket individual steps use one foot portion, Heel or Toe.
		/// Brackets use two foot portions, Heel then Toe.
		/// We do not need to include other combinations (Toe then Heel) since the
		/// ActionCombinations will apply all FootAction combinations to Heel then Toe.
		/// For example it will apply Hold, Tap to Heel, Toe, and also Tap, Hold to Heel Toe.
		/// </summary>
		public readonly int[] FootPortionsForStep;

		/// <summary>
		/// Whether or not this StepType can be used in a jump.
		/// </summary>
		public readonly bool CanBeUsedInJump;

		/// <summary>
		/// Whether or not this StepType involves a foot swap.
		/// Index is the FootPortion.
		/// </summary>
		public readonly bool[] IsFootSwap;

		/// <summary>
		/// Cached value for if any portion of the foot involves a crossover.
		/// </summary>
		public readonly bool IsFootSwapWithAnyPortion;

		/// <summary>
		/// Whether or not this StepType is a bracket on more than one arrow with one foot.
		/// </summary>
		public readonly bool IsBracket;

		/// <summary>
		/// If true then when filling this StepType only consider filling from currently
		/// valid State entries in the parent GraphNode.
		/// If false, then loop over all arrows to try and fill for each.
		/// </summary>
		public readonly bool OnlyConsiderCurrentArrowsWhenFilling;

		/// <summary>
		/// Private Constructor
		/// </summary>
		private StepData(
			FootAction[][] actionSets,
			int[] footPortionsForStep,
			bool canBeUsedInJump,
			bool[] isFootSwap,
			bool isBracket,
			bool onlyConsiderCurrentArrowsWhenFilling)
		{
			ActionSets = actionSets;
			FootPortionsForStep = footPortionsForStep;
			CanBeUsedInJump = canBeUsedInJump;
			IsFootSwap = isFootSwap;
			IsBracket = isBracket;
			OnlyConsiderCurrentArrowsWhenFilling = onlyConsiderCurrentArrowsWhenFilling;

			foreach (var swap in isFootSwap)
				IsFootSwapWithAnyPortion |= swap;
		}

		/// <summary>
		/// Static cached StepData for each StepType.
		/// </summary>
		public static readonly StepData[] Steps;

		/// <summary>
		/// Static cached mapping of FootAction to the GraphArrowState that results from
		/// performing that action;
		/// </summary>
		public static readonly GraphArrowState[] StateAfterAction;

		/// <summary>
		/// Static initializer.
		/// </summary>
		static StepData()
		{
			// Initialize StateAfterAction.
			var footActions = Enum.GetValues(typeof(FootAction)).Cast<FootAction>().ToList();
			StateAfterAction = new GraphArrowState[footActions.Count];
			StateAfterAction[(int)FootAction.Tap] = GraphArrowState.Resting;
			StateAfterAction[(int)FootAction.Hold] = GraphArrowState.Held;
			StateAfterAction[(int)FootAction.Release] = GraphArrowState.Resting;

			// Create lists of combinations of actions for each number of foot portions.
			// Create one list with releases and one without.
			var actionCombinations = new FootAction[NumFootPortions][][];
			var actionCombinationsWithoutReleases = new FootAction[NumFootPortions][][];
			for (var i = 0; i < NumFootPortions; i++)
			{
				var combinations = Combinations.CreateCombinations<FootAction>(i + 1);

				// For brackets you can never release and place at the same time
				// This would be split into two events, a release first, and a step after.
				// Prune those combinations out now so we don't loop over them and need to check for them
				// when searching.
				if (i >= 1)
				{
					combinations.RemoveAll(actions =>
					{
						var hasAtLeastOneRelease = false;
						var isAllReleases = true;
						foreach (var action in actions)
						{
							if (action == FootAction.Release)
								hasAtLeastOneRelease = true;
							else
								isAllReleases = false;
						}
						return hasAtLeastOneRelease != isAllReleases;
					});
				}
				actionCombinations[i] = combinations.ToArray();

				// Update the combinations with no releases.
				combinations.RemoveAll(actions =>
				{
					foreach (var action in actions)
					{
						if (action == FootAction.Release)
							return true;
					}

					return false;
				});
				actionCombinationsWithoutReleases[i] = combinations.ToArray();
			}

			var steps = Enum.GetValues(typeof(StepType)).Cast<StepType>().ToList();

			// Set up swap data arrays for legibility below.
			var noFootSwap = new bool[NumFootPortions];
			var defaultFootSwap = new bool[NumFootPortions];
			var heelFootSwap = new bool[NumFootPortions];
			var toeFootSwap = new bool[NumFootPortions];
			for (var p = 0; p < NumFootPortions; p++)
			{
				noFootSwap[p] = false;
				defaultFootSwap[p] = p == DefaultFootPortion;
				heelFootSwap[p] = p == Heel;
				toeFootSwap[p] = p == Toe;
			}

			// Configure the Steps.
			Steps = new StepData[steps.Count];
			Steps[(int) StepType.SameArrow] = new StepData(
				actionSets: actionCombinations[0],
				footPortionsForStep: new[] { DefaultFootPortion },
				canBeUsedInJump: true,
				isFootSwap: noFootSwap,
				isBracket: false,
				onlyConsiderCurrentArrowsWhenFilling: true
			);
			Steps[(int)StepType.NewArrow] = new StepData(
				actionSets: actionCombinationsWithoutReleases[0],
				footPortionsForStep: new[] { DefaultFootPortion },
				canBeUsedInJump: true,
				isFootSwap: noFootSwap,
				isBracket: false,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.CrossoverFront] = new StepData(
				actionSets: actionCombinationsWithoutReleases[0],
				footPortionsForStep: new[] { DefaultFootPortion },
				canBeUsedInJump: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.CrossoverBehind] = new StepData(
				actionSets: actionCombinationsWithoutReleases[0],
				footPortionsForStep: new[] { DefaultFootPortion },
				canBeUsedInJump: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.InvertFront] = new StepData(
				actionSets: actionCombinationsWithoutReleases[0],
				footPortionsForStep: new[] { DefaultFootPortion },
				canBeUsedInJump: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.InvertBehind] = new StepData(
				actionSets: actionCombinationsWithoutReleases[0],
				footPortionsForStep: new[] { DefaultFootPortion },
				canBeUsedInJump: false,
				isFootSwap: noFootSwap,
				isBracket: false,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.FootSwap] = new StepData(
				actionSets: actionCombinationsWithoutReleases[0],
				footPortionsForStep: new[] { DefaultFootPortion },
				canBeUsedInJump: false,
				isFootSwap: defaultFootSwap,
				isBracket: false,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.BracketHeelNewToeNew] = new StepData(
				actionSets: actionCombinationsWithoutReleases[1],
				footPortionsForStep: new[] { Heel, Toe },
				canBeUsedInJump: true,
				isFootSwap: noFootSwap,
				isBracket: true,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.BracketHeelNewToeSame] = new StepData(
				actionSets: actionCombinationsWithoutReleases[1],
				footPortionsForStep: new[] { Heel, Toe },
				canBeUsedInJump: true,
				isFootSwap: noFootSwap,
				isBracket: true,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.BracketHeelSameToeNew] = new StepData(
				actionSets: actionCombinationsWithoutReleases[1],
				footPortionsForStep: new[] { Heel, Toe },
				canBeUsedInJump: true,
				isFootSwap: noFootSwap,
				isBracket: true,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.BracketHeelSameToeSame] = new StepData(
				actionSets: actionCombinations[1],
				footPortionsForStep: new[] { Heel, Toe },
				canBeUsedInJump: true,
				isFootSwap: noFootSwap,
				isBracket: true,
				onlyConsiderCurrentArrowsWhenFilling: true
			);
			Steps[(int)StepType.BracketHeelSameToeSwap] = new StepData(
				actionSets: actionCombinationsWithoutReleases[1],
				footPortionsForStep: new[] { Heel, Toe },
				canBeUsedInJump: false,
				isFootSwap: toeFootSwap,
				isBracket: true,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.BracketHeelNewToeSwap] = new StepData(
				actionSets: actionCombinationsWithoutReleases[1],
				footPortionsForStep: new[] { Heel, Toe },
				canBeUsedInJump: false,
				isFootSwap: toeFootSwap,
				isBracket: true,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.BracketHeelSwapToeSame] = new StepData(
				actionSets: actionCombinationsWithoutReleases[1],
				footPortionsForStep: new[] { Heel, Toe },
				canBeUsedInJump: false,
				isFootSwap: heelFootSwap,
				isBracket: true,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.BracketHeelSwapToeNew] = new StepData(
				actionSets: actionCombinationsWithoutReleases[1],
				footPortionsForStep: new[] { Heel, Toe },
				canBeUsedInJump: false,
				isFootSwap: heelFootSwap,
				isBracket: true,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.BracketOneArrowHeelSame] = new StepData(
				actionSets: actionCombinations[0],
				footPortionsForStep: new[] { Heel },
				canBeUsedInJump: true,
				isFootSwap: noFootSwap,
				isBracket: false,
				onlyConsiderCurrentArrowsWhenFilling: true
			);
			Steps[(int)StepType.BracketOneArrowHeelNew] = new StepData(
				actionSets: actionCombinationsWithoutReleases[0],
				footPortionsForStep: new[] { Heel },
				canBeUsedInJump: true,
				isFootSwap: noFootSwap,
				isBracket: false,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
			Steps[(int)StepType.BracketOneArrowToeSame] = new StepData(
				actionSets: actionCombinations[0],
				footPortionsForStep: new[] { Toe },
				canBeUsedInJump: true,
				isFootSwap: noFootSwap,
				isBracket: false,
				onlyConsiderCurrentArrowsWhenFilling: true
			);
			Steps[(int)StepType.BracketOneArrowToeNew] = new StepData(
				actionSets: actionCombinationsWithoutReleases[0],
				footPortionsForStep: new[] { Toe },
				canBeUsedInJump: true,
				isFootSwap: noFootSwap,
				isBracket: false,
				onlyConsiderCurrentArrowsWhenFilling: false
			);
		}
	}
}
