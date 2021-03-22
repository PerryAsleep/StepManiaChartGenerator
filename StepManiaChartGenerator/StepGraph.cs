//#define DEBUG_STEPGRAPH

using System;
using System.Collections.Generic;
using System.Linq;
using static StepManiaChartGenerator.Constants;
using Fumen;

#if DEBUG_STEPGRAPH
using System.Diagnostics;
#endif // DEBUG_STEPGRAPH

namespace StepManiaChartGenerator
{
	/// <summary>
	/// A graph of GraphNodes connected by GraphLinks representing all the positions on a set
	/// of arrows and the ways in which one can move between those positions.
	/// </summary>
	public class StepGraph
	{
		/// <summary>
		/// Static cached StepType combinations that constitute valid jumps.
		/// First index is the index of the combination.
		/// Second index is foot for that combination.
		/// </summary>
		private static readonly StepType[][] JumpCombinations;
		/// <summary>
		/// Static cached Foot order to use when filling jumps.
		/// First index is the index of the order to use when looping over all orders.
		/// Second index is array of foot indexes (e.g. [L,R] or [R,L]).
		/// </summary>
		private static readonly int[][] JumpFootOrder;

		/// <summary>
		/// Cached array of all arrow indices.
		/// </summary>
		private readonly int[] AllArrows;
		/// <summary>
		/// Functions to call per StepType to fill steps.
		/// </summary>
		private readonly Func<GraphNode, int, int, int, FootAction[], List<GraphNode>>[] FillFuncs;

		/// <summary>
		/// Structure to keep track of visited nodes when filling the StepGraph.
		/// </summary>
		private HashSet<GraphNode> VisitedNodes;

		/// <summary>
		/// Number of arrows in this StepGraph.
		/// </summary>
		public readonly int NumArrows;

		/// <summary>
		/// Identifier for logs.
		/// </summary>
		private readonly string LogIdentifier;

		/// <summary>
		/// The root GraphNode for this StepGraph.
		/// </summary>
		public readonly GraphNode Root;

		/// <summary>
		/// PadData associated with this StepGraph.
		/// </summary>
		public readonly PadData PadData;

		/// <summary>
		/// Static initializer.
		/// </summary>
		static StepGraph()
		{
			// Initialize JumpCombinations.
			var jumpSingleSteps = new List<StepType>();
			for (var stepType = 0; stepType < StepData.Steps.Length; stepType++)
			{
				if (StepData.Steps[stepType].CanBeUsedInJump)
					jumpSingleSteps.Add((StepType)stepType);
			}
			JumpCombinations = Combinations.CreateCombinations(jumpSingleSteps, NumFeet).ToArray();

			// Initialize JumpFootOrder.
			JumpFootOrder = new[]
			{
				new []{L, R},
				new []{R, L}
			};
		}

		/// <summary>
		/// Private constructor.
		/// StepGraphs are publicly created using CreateStepGraph.
		/// </summary>
		/// <param name="padData">PadData this StepGraph is for.</param>
		/// <param name="root">Root GraphNode.</param>
		private StepGraph(PadData padData, GraphNode root)
		{
			Root = root;
			PadData = padData;
			NumArrows = PadData.NumArrows;
			LogIdentifier = PadData.StepsType;

			AllArrows = new int[NumArrows];
			for (var i = 0; i < NumArrows; i++)
				AllArrows[i] = i;

			// Configure the fill Functions.
			FillFuncs = new Func<GraphNode, int, int, int, FootAction[], List<GraphNode>>[]
			{
				FillSameArrow,
				FillNewArrow,
				FillCrossoverFront,
				FillCrossoverBack,
				FillInvertFront,
				FillInvertBack,
				FillFootSwap,
				FillBracketHeelNewToeNew,
				FillBracketHeelNewToeSame,
				FillBracketHeelSameToeNew,
				FillBracketHeelSameToeSame,
				FillBracketHeelSameToeSwap,
				FillBracketHeelNewToeSwap,
				FillBracketHeelSwapToeSame,
				FillBracketHeelSwapToeNew,
				FillBracketOneArrowHeelSame,
				FillBracketOneArrowHeelNew,
				FillBracketOneArrowToeSame,
				FillBracketOneArrowToeNew
			};
		}

		/// <summary>
		/// Creates a new StepGraph satisfying all movements for the given ArrowData with
		/// a root position at the given starting arrows.
		/// </summary>
		/// <param name="padData">PadData for the set of arrows to create a StepGraph for.</param>
		/// <param name="leftStartingArrow">Starting arrow for the left foot.</param>
		/// <param name="rightStartingArrow">Starting arrow for the right foot.</param>
		/// <returns>
		/// StepGraph satisfying all movements for the given ArrowData with a root position
		/// at the given starting arrows.
		/// </returns>
		public static StepGraph CreateStepGraph(PadData padData, int leftStartingArrow, int rightStartingArrow)
		{
			// Set up state for root node.
			var state = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (p == DefaultFootPortion)
				{
					state[L, p] = new GraphNode.FootArrowState(leftStartingArrow, GraphArrowState.Resting);
					state[R, p] = new GraphNode.FootArrowState(rightStartingArrow, GraphArrowState.Resting);
				}
				else
				{
					state[L, p] = GraphNode.InvalidFootArrowState;
					state[R, p] = GraphNode.InvalidFootArrowState;
				}
			}

			var root = new GraphNode(state, BodyOrientation.Normal);
			var stepGraph = new StepGraph(padData, root);
			stepGraph.Fill();
			return stepGraph;
		}

		#region Public Search Methods
		/// <summary>
		/// Searches the StepGraph for a GraphNode matching the given left and right
		/// foot states using DefaultFootPortions.
		/// </summary>
		/// <param name="leftArrow">Arrow the left foot should be on.</param>
		/// <param name="leftState">GraphArrowState for the left foot.</param>
		/// <param name="rightArrow">Arrow the right foot should be on.</param>
		/// <param name="rightState">GraphArrowState for the right foot.</param>
		/// <returns>GraphNode matching parameters or null if none was found.</returns>
		public GraphNode FindGraphNode(
			int leftArrow, GraphArrowState leftState,
			int rightArrow, GraphArrowState rightState)
		{
			var trackedNodes = new HashSet<GraphNode>();
			var nodes = new HashSet<GraphNode> {Root};
			trackedNodes.Add(Root);
			while (true)
			{
				var newNodes = new HashSet<GraphNode>();

				foreach (var node in nodes)
				{
					if (StateMatches(node, leftArrow, leftState, rightArrow, rightState))
						return node;

					foreach (var l in node.Links)
					{
						foreach (var g in l.Value)
						{
							if (!trackedNodes.Contains(g))
							{
								trackedNodes.Add(g);
								newNodes.Add(g);
							}
						}
					}
				}

				nodes = newNodes;
				if (nodes.Count == 0)
					break;
			}

			return null;
		}

		/// <summary>
		/// Checks if the given GraphNode matches the state represented by the given
		/// arrows and GraphArrowStates for DefaultFootPortions for the left and right foot.
		/// Helper for FindGraphNode.
		/// </summary>
		/// <param name="node">GraphNode to check.</param>
		/// <param name="leftArrow">Arrow the left foot should be on.</param>
		/// <param name="leftState">GraphArrowState for the left foot.</param>
		/// <param name="rightArrow">Arrow the right foot should be on.</param>
		/// <param name="rightState">GraphArrowState for the right foot.</param>
		/// <returns>True if the state matches and false otherwise.</returns>
		private static bool StateMatches(GraphNode node,
			int leftArrow, GraphArrowState leftState,
			int rightArrow, GraphArrowState rightState)
		{
			var state = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			for (var f = 0; f < NumFeet; f++)
				for (var p = 0; p < NumFootPortions; p++)
					state[f, p] = new GraphNode.FootArrowState(InvalidArrowIndex, GraphArrowState.Resting);

			state[L, DefaultFootPortion] = new GraphNode.FootArrowState(leftArrow, leftState);
			state[R, DefaultFootPortion] = new GraphNode.FootArrowState(rightArrow, rightState);
			var newNode = new GraphNode(state, BodyOrientation.Normal);
			return node.Equals(newNode);
		}

		/// <summary>
		/// Finds all GraphLinks used by this StepGraph.
		/// </summary>
		/// <returns>HashSet of all GraphLinks in this StepGraph.</returns>
		public HashSet<GraphLink> FindAllGraphLinks()
		{
			var allLinks = new HashSet<GraphLink>();
			var trackedNodes = new HashSet<GraphNode>();
			var nodes = new HashSet<GraphNode> { Root };
			trackedNodes.Add(Root);
			while (true)
			{
				var newNodes = new HashSet<GraphNode>();
				foreach (var node in nodes)
				{
					foreach (var l in node.Links)
					{
						allLinks.Add(l.Key);
						foreach (var g in l.Value)
						{
							if (!trackedNodes.Contains(g))
							{
								trackedNodes.Add(g);
								newNodes.Add(g);
							}
						}
					}
				}

				nodes = newNodes;
				if (nodes.Count == 0)
					break;
			}
			return allLinks;
		}
		#endregion Public Search Methods

		#region Fill
		private void Fill()
		{
			LogInfo("Generating StepGraph...");

			VisitedNodes = new HashSet<GraphNode>();
			var completeNodes = new HashSet<GraphNode>();
			var currentNodes = new List<GraphNode> { Root };
			var level = 0;

			var allArrows = new int[NumArrows];
			for (var i = 0; i < allArrows.Length; i++)
				allArrows[i] = i;

			while (currentNodes.Count > 0)
			{
				LogInfo($"Level {level + 1}: Searching {currentNodes.Count} nodes...");

				var allChildren = new HashSet<GraphNode>();
				foreach (var currentNode in currentNodes)
				{
					VisitedNodes.Add(currentNode);

					// Fill node
					foreach (var stepType in Enum.GetValues(typeof(StepType)).Cast<StepType>())
						FillSingleFootStep(currentNode, stepType);
					foreach (var jump in JumpCombinations)
						FillJump(currentNode, jump);

					// Collect children
					foreach (var linkEntry in currentNode.Links)
						foreach (var childNode in linkEntry.Value)
							allChildren.Add(childNode);

					// Mark node complete
					completeNodes.Add(currentNode);
				}

				// Remove all complete nodes
				var previousCount = allChildren.Count;
				allChildren.RemoveWhere(n => completeNodes.Contains(n));

				LogInfo($"Level {level + 1}: Found {allChildren.Count} children (pruned from {previousCount}).");

				// Search one level deeper
				currentNodes = allChildren.ToList();
				level++;
			}

			LogInfo($"StepGraph generation complete. {completeNodes.Count} Nodes.");
		}

		private void AddNode(GraphNode currentNode, GraphNode newNode, GraphLink link)
		{
			if (VisitedNodes.TryGetValue(newNode, out var visitedNode))
				newNode = visitedNode;
			else
				VisitedNodes.Add(newNode);

			if (!currentNode.Links.ContainsKey(link))
				currentNode.Links[link] = new List<GraphNode>();
			if (!currentNode.Links[link].Contains(newNode))
				currentNode.Links[link].Add(newNode);
		}

		private void FillSingleFootStep(GraphNode currentNode, StepType stepType)
		{
			// Get the foot portions needed for this step.
			// For brackets this is two portions and for other steps it is one.
			// We do not need to exhaust combinations here since we will do it for
			// ActionCombinations.
			var footPortions = StepData.Steps[(int)stepType].FootPortionsForStep;
			// Get the appropriate function to use for filling this StepType.
			var fillFunc = FillFuncs[(int)stepType];
			// Get the list of combinations of FootActions for the number arrows used by this StepType.
			// If this StepType uses two arrows then this actionSets array will include
			// arrays for every combination of FootActions for two feet.
			// This includes, for example, Tap then Hold and Hold then Tap.
			var actionSets = StepData.Steps[(int)stepType].ActionSets;
			var onlyConsiderCurrent = StepData.Steps[(int)stepType].OnlyConsiderCurrentArrowsWhenFilling;

			var arrows = onlyConsiderCurrent ? new int[1] : AllArrows;

			for (var foot = 0; foot < NumFeet; foot++)
			{
				for (var startingFootPortion = 0; startingFootPortion < NumFootPortions; startingFootPortion++)
				{
					if (currentNode.State[foot, startingFootPortion].Arrow == InvalidArrowIndex)
						continue;
					for (var setIndex = 0; setIndex < actionSets.Length; setIndex++)
					{
						var actions = actionSets[setIndex];
						if (onlyConsiderCurrent)
							arrows[0] = currentNode.State[foot, startingFootPortion].Arrow;
						foreach (var newArrow in arrows)
						{
							// Call the fill function to get the List of GraphNodes that can be reached
							// from the current Node for the given StepType and FootActions.
							var newNodes = fillFunc(currentNode, foot, startingFootPortion, newArrow, actions);
							if (newNodes == null || newNodes.Count == 0)
								continue;
							// Add each new GraphNode with a new GraphLink
							foreach (var newNode in newNodes)
							{
								// Fill the GraphLink Links at the correct foot portions.
								// For example, if this is BracketHeelSameToeNew StepType we want to only
								// fill the Link at the Toe foot portion index.
								// We do not want to index the actions by the foot portion.
								// For example, if this is BracketHeelSameToeNew StepType the foot portion
								// will be Toe (index 1) with only one FootAction in the actions
								// array (at index 0).
								var link = new GraphLink();
								var actionIndex = 0;
								foreach (var footPortion in footPortions)
								{
									link.Links[foot, footPortion] = new GraphLink.FootArrowState(stepType, actions[actionIndex++]);
								}

								AddNode(currentNode, newNode, link);
							}
						}
					}
				}
			}
		}

		private void FillJump(GraphNode currentNode, StepType[] stepTypes)
		{
			// When filling a jump we fill one foot at a time, then pass the state that has been altered by the first foot
			// to the next foot to complete the jump.
			// We need to loop over the feet in both orders to ensure all valid states are hit.
			// For example, if we only process left then right we will miss a jump where both feet us NewArrow and the jump
			// goes from LU to UR since the right foot would not have moved yet to make room for the left.
			foreach (var footOrder in JumpFootOrder)
			{
				var f1 = footOrder[0];
				var f2 = footOrder[1];

				// Get the foot portions needed for this step.
				// For brackets this is two portions and for other steps it is one.
				// We do not need to exhaust combinations here since we will do it for
				// ActionCombinations.
				var footPortionsF1 = StepData.Steps[(int)stepTypes[f1]].FootPortionsForStep;
				var footPortionsF2 = StepData.Steps[(int)stepTypes[f2]].FootPortionsForStep;

				// Find all the states from moving the first foot.
				var actionsToNodesF1 = FillJumpStep(currentNode, stepTypes[f1], f1, null);
				foreach (var actionNodeF1 in actionsToNodesF1)
				{
					foreach (var newNodeF1 in actionNodeF1.Value)
					{
						// Optimization - do not need to check every element in the action set since if one is
						// a release they are all releases.
						var firstFootIsReleasing = actionNodeF1.Key[0] == FootAction.Release;

						// Using the state from the first foot, find all the states from moving the second foot.
						var actionsToNodesF2 = FillJumpStep(newNodeF1, stepTypes[f2], f2, firstFootIsReleasing);
						foreach (var actionNodeF2 in actionsToNodesF2)
						{
							foreach (var newNodeF2 in actionNodeF2.Value)
							{
								// Fill the GraphLink Links at the correct foot portions for each foot.
								// For example, if this is BracketHeelSameToeNew StepType we want to only
								// fill the Link at the Toe foot portion index.
								// We do not want to index the actions by the foot portion.
								// For example, if this is BracketHeelSameToeNew StepType the foot portion
								// will be Toe (index 1) with only one FootAction in the actions
								// array (at index 0).
								var link = new GraphLink();
								var actionIndex = 0;
								foreach (var footPortion in footPortionsF1)
								{
									link.Links[f1, footPortion] = new GraphLink.FootArrowState(stepTypes[f1], actionNodeF1.Key[actionIndex++]);
								}
								actionIndex = 0;
								foreach (var footPortion in footPortionsF2)
								{
									link.Links[f2, footPortion] = new GraphLink.FootArrowState(stepTypes[f2], actionNodeF2.Key[actionIndex++]);
								}

								AddNode(currentNode, newNodeF2, link);
							}
						}
					}
				}
			}
		}

		private Dictionary<FootAction[], List<GraphNode>> FillJumpStep(
			GraphNode currentNode,
			StepType stepType,
			int foot,
			bool? otherFootReleasing)
		{
			var result = new Dictionary<FootAction[], List<GraphNode>>();

			// Get the appropriate function to use for filling this StepType.
			var fillFunc = FillFuncs[(int)stepType];
			// Get the list of combinations of FootActions for the number arrows used by this StepType.
			// If this StepType uses two arrows then this actionSets array will include
			// arrays for every combination of FootActions for two feet.
			// This includes, for example, Tap then Hold and Hold then Tap.
			var actionSets = StepData.Steps[(int)stepType].ActionSets;
			var onlyConsiderCurrent = StepData.Steps[(int)stepType].OnlyConsiderCurrentArrowsWhenFilling;

			var arrows = onlyConsiderCurrent ? new int[1] : AllArrows;

			for (var startingFootPortion = 0; startingFootPortion < NumFootPortions; startingFootPortion++)
			{
				if (currentNode.State[foot, startingFootPortion].Arrow == InvalidArrowIndex)
					continue;
				for (var setIndex = 0; setIndex < actionSets.Length; setIndex++)
				{
					var actions = actionSets[setIndex];

					// Make sure the jump is either all releases or all steps.
					// Optimization - do not need to check every element in the action set since if one is
					// a release they are all releases.
					if (otherFootReleasing != null && otherFootReleasing != (actions[0] == FootAction.Release))
						continue;

					if (onlyConsiderCurrent)
						arrows[0] = currentNode.State[foot, startingFootPortion].Arrow;
					foreach (var newArrow in arrows)
					{
						// Call the fill function to get the List of GraphNodes that can be reached
						// from the current Node for the given StepType and FootActions.
						var newNodes = fillFunc(currentNode, foot, startingFootPortion, newArrow, actions);
						if (newNodes == null || newNodes.Count == 0)
							continue;

						if (!result.TryGetValue(actions, out var nodeLists))
							nodeLists = newNodes;
						else
							nodeLists.AddRange(newNodes);
						result[actions] = nodeLists;
					}
				}
			}

			return result;
		}

		#region Fill Single Step
		private List<GraphNode> FillNewArrow(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillNewArrowInternal(
				currentNode,
				foot,
				currentFootPortion,
				newArrow,
				DefaultFootPortion,
				footActions,
				false);
		}

		private List<GraphNode> FillBracketOneArrowHeelNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillNewArrowInternal(currentNode, foot, currentFootPortion, newArrow, Heel, footActions, true);
		}

		private List<GraphNode> FillBracketOneArrowToeNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillNewArrowInternal(currentNode, foot, currentFootPortion, newArrow, Toe, footActions, true);
		}

		private List<GraphNode> FillNewArrowInternal(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			int destinationFootPortion,
			FootAction[] footActions,
			bool bracket)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;
			var currentArrow = currentState[foot, currentFootPortion].Arrow;

#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footAction != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Must be new arrow.
			if (currentArrow == newArrow)
				return null;

			// BracketOneArrowHeelNew and BracketOneArrowToeNew checks.
			if (bracket)
			{
				// Skip if the new arrow is not a valid bracketable pairing.
				if (destinationFootPortion == Heel && !PadData.ArrowData[currentArrow].BracketablePairingsOtherHeel[foot][newArrow])
					return null;
				if (destinationFootPortion == Toe && !PadData.ArrowData[currentArrow].BracketablePairingsOtherToe[foot][newArrow])
					return null;

				var numHeld = NumHeld(currentState, foot);

				// Must be holding on an arrow to bracket another.
				if (numHeld < 1)
					return null;
				// Must have one free foot.
				if (numHeld == NumFootPortions)
					return null;
			}
			// NewArrow checks.
			else
			{
				// Skip if the new arrow isn't a valid next arrow for the current placement.
				if (!PadData.ArrowData[currentArrow].ValidNextArrows[newArrow])
					return null;

				// Cannot step on a new arrow if already holding.
				if (AnyHeld(currentState, foot))
					return null;
			}
			// Skip if the new arrow is occupied.
			if (!IsFree(currentState, newArrow))
				return null;
			// Skip if the new arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, newArrow))
				return null;
			// Skip if the new arrow is not a valid pairing for any other foot arrows.
			if (!IsValidPairingWithAnyOtherFoot(currentState, foot, newArrow))
				return null;

			// Set up the state for a new node.
			// Copy the previous state, but lift from any resting arrows for the given foot.
			// Leave holds.
			var otherFoot = OtherFoot(foot);
			var newState = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			for (var p = 0; p < NumFootPortions; p++)
			{
				newState[otherFoot, p] = currentState[otherFoot, p];

				if (IsStateRestingAtIndex(currentState, p, foot))
					newState[foot, p] = GraphNode.InvalidFootArrowState;
				else
					newState[foot, p] = currentState[foot, p];
			}

			// If stepping on a new bracket with the Heel (0) from a state where only one foot portion is
			// resting (DefaultFootPortion, also 0), then we need to make the existing held arrow held with
			// the Toe.
			if (bracket
			    && destinationFootPortion == DefaultFootPortion
			    && currentState[foot, destinationFootPortion].Arrow != InvalidArrowIndex)
			{
				newState[foot, OtherFootPortion(destinationFootPortion)] = currentState[foot, destinationFootPortion];
			}

			newState[foot, destinationFootPortion] = new GraphNode.FootArrowState(newArrow, StepData.StateAfterAction[(int)footAction]);

			// Stepping on a new arrow implies a normal orientation.
			return new List<GraphNode> { new GraphNode(newState, BodyOrientation.Normal) };
		}

		private List<GraphNode> FillSameArrow(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillSameArrowInternal(
				currentNode,
				foot,
				currentFootPortion,
				newArrow,
				DefaultFootPortion,
				footActions,
				false);
		}

		private List<GraphNode> FillBracketOneArrowHeelSame(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillSameArrowInternal(currentNode, foot, currentFootPortion, newArrow, Heel, footActions, true);
		}

		private List<GraphNode> FillBracketOneArrowToeSame(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillSameArrowInternal(currentNode, foot, currentFootPortion, newArrow, Toe, footActions, true);
		}

		private List<GraphNode> FillSameArrowInternal(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			int destinationFootPortion,
			FootAction[] footActions,
			bool bracket)
		{
#if DEBUG_STEPGRAPH
			Debug.Assert(currentNode.State[foot, currentFootPortion].Arrow == newArrow);
#endif // DEBUG_STEPGRAPH

			// Only for brackets ensure that the foot portions match.
			// We do not want to do this for non-bracket steps because you could be resting
			// with both foot portions and we want to allow SameArrow steps from either portion
			// to the DefaultFootPortion.
			if (bracket && currentFootPortion != destinationFootPortion)
				return null;

			var footAction = footActions[0];
			var release = footAction == FootAction.Release;
			var currentState = currentNode.State;

			// Release logic. Lift portion from hold.
			if (release && currentState[foot, destinationFootPortion].State != GraphArrowState.Held)
				return null;
			// Step logic. Placement action on an arrow resting under the heel or toe.
			if (!release && currentState[foot, destinationFootPortion].State != GraphArrowState.Resting)
				return null;

			// Count number held and held or resting so we can determine if this step is appropriate
			// for the given type of step (release or not and also bracket or not).
			var numHeld = 0;
			var numHeldOrResting = 0;
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (currentState[foot, p].Arrow != InvalidArrowIndex)
				{
					numHeldOrResting++;
					if (currentState[foot, p].State == GraphArrowState.Held)
						numHeld++;
				}
			}

			// Bracket (BracketOneArrowHeelSame and BracketOneArrowToeSame) logic.
			if (bracket)
			{
				// For both releasing and stepping there needs to be at least one hold.
				// For releasing this is because the hold needs to be released.
				// For stepping this is because if none were held this would not be a bracket step.
				if (numHeld < 1)
					return null;
				// For both releasing and stepping both foot portions need to be on arrows.
				// For releasing this is because if only one were held and the other was not resting or held then
				// the step would not be a bracket release.
				// For stepping this is because to step on the same arrow as a bracket, the foot needs to be resting
				// on the arrow while the other portion is held.
				if (numHeldOrResting < NumFootPortions)
					return null;
			}
			// SameArrow logic.
			else
			{
				// Releasing. Different logic than stepping for a non-bracket step.
				if (release)
				{
					// Releasing on the SameArrow when not bracketing requires exactly one
					// arrow to be held.
					if (numHeld != 1)
						return null;
					// When releasing only one foot portion can be holding. If the other foot
					// portion were resting then this release would be a bracket release.
					if (numHeldOrResting == NumFootPortions)
						return null;
				}
				// Stepping. Different logic than releasing for a non-bracket step.
				else
				{
					// When stepping no arrow can be held with this foot. If an arrow was
					// held then this step should be a bracket.
					if (numHeld != 0)
						return null;
				}
			}

			var otherFoot = OtherFoot(foot);

			// If this follows a footswap then you cannot perform this action.
			// Ideally we should only prevent a same arrow step after a foot swap with the other foot since
			// there is nothing wrong with tapping twice after a swap, but we are unable to that when
			// making the StepGraph as we cannot know how the currentState was entered. The only options
			// are to forbid any same arrow step if it follows a swap with either feet, or allow it
			// all the time and then when making an ExpressedChart give a same arrow step movement following
			// a swap with the other foot a high cost. I can't think of any scenario where it would
			// actually be beneficial to do some kind of paradiddle pattern on one arrow so for now I am
			// going to prevent it always by not including it in the StepGraph.
			if (!release && IsOn(currentState, newArrow, otherFoot))
				return null;

			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			for (var p = 0; p < NumFootPortions; p++)
			{
				newState[otherFoot, p] = currentState[otherFoot, p];

				if (p == destinationFootPortion)
					newState[foot, p] = new GraphNode.FootArrowState(newArrow, StepData.StateAfterAction[(int)footAction]);
				// When stepping is is necessary to lift all resting portions.
				else if (!release && IsStateRestingAtIndex(currentState, p, foot))
					newState[foot, p] = GraphNode.InvalidFootArrowState;
				else
					newState[foot, p] = currentState[foot, p];
			}

			// Stepping on the same arrow maintains the previous state's orientation.
			return new List<GraphNode> { new GraphNode(newState, currentNode.Orientation) };
		}

		private List<GraphNode> FillFootSwap(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

			// Cannot release on a new arrow.
			if (footAction == FootAction.Release)
				return null;
			// Must be new arrow.
			if (currentArrow == newArrow)
				return null;

			var otherFoot = OtherFoot(foot);

			// Only consider moving to a new arrow when the currentIndex is resting.
			if (currentNode.State[foot, currentFootPortion].State != GraphArrowState.Resting)
				return null;
			// The new arrow must have the other foot resting so it can be swapped to.
			if (!IsResting(currentState, newArrow, otherFoot))
				return null;
			// Disallow foot swap if this foot is holding or rolling.
			if (AnyHeld(currentState, foot))
				return null;
			// Disallow foot swap if the other foot is holding or rolling.
			if (AnyHeld(currentState, otherFoot))
				return null;

			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			// Update other foot.
			UpdateOtherFootNewStateAfterSwapToArrow(currentState, newState, otherFoot, newArrow);
			// Update this foot.
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (p == DefaultFootPortion)
					newState[foot, p] = new GraphNode.FootArrowState(newArrow, StepData.StateAfterAction[(int)footAction]);
				else
					newState[foot, p] = GraphNode.InvalidFootArrowState;
			}

			// Footswaps correct inverted orientation.
			return new List<GraphNode> { new GraphNode(newState, BodyOrientation.Normal) };
		}
		#endregion Fill Single Step

		#region Fill Crossover
		private List<GraphNode> FillCrossoverFront(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentNode, foot, currentFootPortion, newArrow, footActions, true);
		}

		private List<GraphNode> FillCrossoverBack(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillCrossoverInternal(currentNode, foot, currentFootPortion, newArrow, footActions, false);
		}

		private List<GraphNode> FillCrossoverInternal(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions,
			bool front)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footAction != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Must be new arrow.
			if (currentArrow == newArrow)
				return null;
			// Cannot crossover if any arrows are held by this foot.
			if (AnyHeld(currentState, foot))
				return null;
			// Skip if this isn't a valid next arrow for the current placement.
			if (!PadData.ArrowData[currentArrow].ValidNextArrows[newArrow])
				return null;
			// Skip if the new arrow is occupied.
			if (!IsFree(currentState, newArrow))
				return null;
			// Skip if the new arrow is not a crossover.
			if (front && !FootCrossesOverInFrontWithAnyOtherFoot(currentState, foot, newArrow))
				return null;
			if (!front && !FootCrossesOverInBackWithAnyOtherFoot(currentState, foot, newArrow))
				return null;

			var otherFoot = OtherFoot(foot);

			// If the current state is already a crossover of the same type (front or back)
			// with the other foot then we cannot create another crossover of that type
			// with this foot as the legs would cross through each other (or you would be spun).
			if (front && FootCrossedOverInFront(currentState, otherFoot))
				return null;
			if (!front && FootCrossedOverInBack(currentState, otherFoot))
				return null;

			// Skip if the current state is inverted and this crossover faces the player in
			// the other direction since this would cross their legs through each other.
			if (currentNode.Orientation == BodyOrientation.InvertedRightOverLeft
			    && ((front && foot == L) || (!front && foot == R)))
				return null;
			if (currentNode.Orientation == BodyOrientation.InvertedLeftOverRight
				&& ((front && foot == R) || (!front && foot == L)))
				return null;

			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			for (var p = 0; p < NumFootPortions; p++)
			{
				// Copy previous state for other foot
				newState[otherFoot, p] = currentState[otherFoot, p];

				if (p == DefaultFootPortion)
					newState[foot, p] = new GraphNode.FootArrowState(newArrow, StepData.StateAfterAction[(int)footAction]);
				// Lift any resting arrows for the given foot.
				else if (IsStateRestingAtIndex(currentState, p, foot))
					newState[foot, p] = GraphNode.InvalidFootArrowState;
				else
					newState[foot, p] = currentState[foot, p];
			}

			// Crossovers are not inverted.
			return new List<GraphNode> { new GraphNode(newState, BodyOrientation.Normal) };
		}
		#endregion Fill Crossover

		#region Fill Invert
		private List<GraphNode> FillInvertFront(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillInvertInternal(currentNode, foot, currentFootPortion, newArrow, footActions, true);
		}

		private List<GraphNode> FillInvertBack(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			return FillInvertInternal(currentNode, foot, currentFootPortion, newArrow, footActions, false);
		}

		private List<GraphNode> FillInvertInternal(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions,
			bool front)
		{
			var footAction = footActions[0];
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footAction != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Must be new arrow.
			if (currentArrow == newArrow)
				return null;
			// Cannot invert if any arrows are held by this foot.
			if (AnyHeld(currentState, foot))
				return null;
			// Skip if this isn't a valid next arrow for the current placement.
			if (!PadData.ArrowData[currentArrow].ValidNextArrows[newArrow])
				return null;
			// Skip if the new arrow is occupied.
			if (!IsFree(currentState, newArrow))
				return null;
			// Skip if the new arrow is not an inversion.
			if (!FootInvertsWithAnyOtherFoot(currentState, foot, newArrow))
				return null;

			// Determine the orientation this inversion will result in.
			var otherFoot = OtherFoot(foot);
			var orientation = ((front && foot == R) || (!front && foot == L)) ?
				BodyOrientation.InvertedRightOverLeft : BodyOrientation.InvertedLeftOverRight;

			// If the current state is already inverted in the opposite orientation, skip.
			// The player needs to right themselves first.
			if (currentNode.Orientation != BodyOrientation.Normal && currentNode.Orientation != orientation)
				return null;

			// If the current state is crossed over with the same type (front or back)
			// with the other foot then we cannot invert with the same type with
			// this foot as the legs would cross through each other.
			if (front && FootCrossedOverInFront(currentState, otherFoot))
				return null;
			if (!front && FootCrossedOverInBack(currentState, otherFoot))
				return null;

			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			for (var p = 0; p < NumFootPortions; p++)
			{
				// Copy previous state for other foot.
				newState[otherFoot, p] = currentState[otherFoot, p];

				if (p == DefaultFootPortion)
					newState[foot, p] = new GraphNode.FootArrowState(newArrow, StepData.StateAfterAction[(int)footAction]);
				// Lift any resting arrows for the given foot.
				else if (IsStateRestingAtIndex(currentState, p, foot))
					newState[foot, p] = GraphNode.InvalidFootArrowState;
				else
					newState[foot, p] = currentState[foot, p];
			}

			return new List<GraphNode> { new GraphNode(newState, orientation) };
		}
		#endregion Fill Invert

		#region Bracket Fill Functions
		private List<GraphNode> FillBracketHeelNewToeNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;

#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footActions[0] != FootAction.Release && footActions[1] != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Cannot step on a new bracket if already holding on an arrow
			if (AnyHeld(currentState, foot))
				return null;

			var heelAction = footActions[Heel];
			var toeAction = footActions[Toe];
			var allResults = new List<GraphNode>();

			// Avoid calling this method twice since almost all of it is independent of the foot portion.
			// Perform the foot portion check separately below as performance optimization.
			if (!FillBracketInternalFirstArrowNew(currentNode, foot, currentFootPortion, newArrow, InvalidFootPortion,
				out var firstArrowOtherFootValidPairings,
				out var firstNewArrowIsValidPlacement))
				return null;

			if (IsValidNewArrowForBracket(currentState, foot, Heel, newArrow))
			{
				// Fill steps where the heel arrow precedes the toe arrow.
				var results = FillBracketInternalSecondArrowNew(
					currentNode,
					foot,
					currentFootPortion,
					newArrow,
					new[] { Heel, Toe },
					new[] { heelAction, toeAction },
					firstNewArrowIsValidPlacement,
					firstArrowOtherFootValidPairings);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			if (IsValidNewArrowForBracket(currentState, foot, Toe, newArrow))
			{

				// Fill steps where the toe arrow precedes the heel arrow.
				var results = FillBracketInternalSecondArrowNew(
					currentNode,
					foot,
					currentFootPortion,
					newArrow,
					new[] {Toe, Heel},
					new[] {toeAction, heelAction},
					firstNewArrowIsValidPlacement,
					firstArrowOtherFootValidPairings);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);

			}

			return allResults;
		}

		private List<GraphNode> FillBracketHeelNewToeSame(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footActions[0] != FootAction.Release && footActions[1] != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Cannot step on a new bracket if already holding on an arrow
			if (AnyHeld(currentNode.State, foot))
				return null;

			var allResults = new List<GraphNode>();

			// Heel first index, toe second index
			if (FillBracketInternalFirstArrowNew(currentNode, foot, currentFootPortion, newArrow, Heel,
				out var firstArrowOtherFootValidPairings,
				out var firstNewArrowIsValidPlacement))
			{
				var results = FillBracketInternalSecondArrowSame(currentNode, foot, currentFootPortion, newArrow,
					new[] { Heel, Toe },
					footActions,
					firstNewArrowIsValidPlacement,
					firstArrowOtherFootValidPairings);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			// Toe first index, heel second index
			if (FillBracketInternalFirstArrowSame(currentNode, foot, currentFootPortion, newArrow,
				out firstArrowOtherFootValidPairings,
				out firstNewArrowIsValidPlacement))
			{
				var results = FillBracketInternalSecondArrowNew(currentNode, foot, currentFootPortion, newArrow,
					new[] { Toe, Heel },
					new[] { footActions[Toe], footActions[Heel] },
					firstNewArrowIsValidPlacement,
					firstArrowOtherFootValidPairings);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			return allResults;
		}

		private List<GraphNode> FillBracketHeelSameToeNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footActions[0] != FootAction.Release && footActions[1] != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Cannot step on a new bracket if already holding on an arrow
			if (AnyHeld(currentNode.State, foot))
				return null;

			var allResults = new List<GraphNode>();

			// Toe first index, heel second index
			if (FillBracketInternalFirstArrowNew(currentNode, foot, currentFootPortion, newArrow, Toe,
				out var firstArrowOtherFootValidPairings,
				out var firstNewArrowIsValidPlacement))
			{
				var results = FillBracketInternalSecondArrowSame(currentNode, foot, currentFootPortion, newArrow,
					new[] { Toe, Heel },
					new[] { footActions[Toe], footActions[Heel] },
					firstNewArrowIsValidPlacement,
					firstArrowOtherFootValidPairings);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			// Heel first index, toe second index
			if (FillBracketInternalFirstArrowSame(currentNode, foot, currentFootPortion, newArrow,
				out firstArrowOtherFootValidPairings,
				out firstNewArrowIsValidPlacement))
			{
				var results = FillBracketInternalSecondArrowNew(currentNode, foot, currentFootPortion, newArrow,
					new[] { Heel, Toe },
					footActions,
					firstNewArrowIsValidPlacement,
					firstArrowOtherFootValidPairings);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			return allResults;
		}

		private List<GraphNode> FillBracketHeelSameToeSame(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
			var heelAction = footActions[Heel];
			var toeAction = footActions[Toe];

			var allResults = new List<GraphNode>();

			var results = FillBracketInternalHeelSameToeSame(
				currentNode,
				foot,
				currentFootPortion,
				newArrow,
				new[] { Heel, Toe },
				new[] { heelAction, toeAction });
			if (results != null && results.Count > 0)
				allResults.AddRange(results);

			results = FillBracketInternalHeelSameToeSame(
				currentNode,
				foot,
				currentFootPortion,
				newArrow,
				new[] { Toe, Heel },
				new[] { toeAction, heelAction });
			if (results != null && results.Count > 0)
				allResults.AddRange(results);

			return allResults;
		}

		private List<GraphNode> FillBracketHeelSameToeSwap(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footActions[0] != FootAction.Release && footActions[1] != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Cannot step on a new bracket if already holding on an arrow
			if (AnyHeld(currentNode.State, foot))
				return null;

			var allResults = new List<GraphNode>();

			// Heel first index, toe second index
			if (FillBracketInternalFirstArrowSame(currentNode, foot, currentFootPortion, newArrow, out _, out _))
			{
				var results = FillBracketInternalSecondArrowSwap(currentNode, foot, currentFootPortion, newArrow,
					new[] { Heel, Toe },
					footActions);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			// Toe first index, heel second index
			if (FillBracketInternalFirstArrowSwap(currentNode, foot, currentFootPortion, newArrow))
			{
				var results = FillBracketInternalSecondArrowSame(currentNode, foot, currentFootPortion, newArrow,
					new[] { Toe, Heel },
					new[] { footActions[Toe], footActions[Heel] },
					true, null, true);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			return allResults;
		}

		private List<GraphNode> FillBracketHeelNewToeSwap(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footActions[0] != FootAction.Release && footActions[1] != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Cannot step on a new bracket if already holding on an arrow
			if (AnyHeld(currentNode.State, foot))
				return null;

			var allResults = new List<GraphNode>();

			// Heel first index, toe second index
			if (FillBracketInternalFirstArrowNew(currentNode, foot, currentFootPortion, newArrow, Heel, out _, out _))
			{
				var results = FillBracketInternalSecondArrowSwap(currentNode, foot, currentFootPortion, newArrow,
					new[] { Heel, Toe },
					footActions);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			// Toe first index, heel second index
			if (FillBracketInternalFirstArrowSwap(currentNode, foot, currentFootPortion, newArrow))
			{
				var results = FillBracketInternalSecondArrowNew(currentNode, foot, currentFootPortion, newArrow,
					new[] { Toe, Heel },
					new[] { footActions[Toe], footActions[Heel] },
					true, null, true);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			return allResults;
		}

		private List<GraphNode> FillBracketHeelSwapToeSame(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footActions[0] != FootAction.Release && footActions[1] != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Cannot step on a new bracket if already holding on an arrow
			if (AnyHeld(currentNode.State, foot))
				return null;

			var allResults = new List<GraphNode>();

			// Heel first index, toe second index
			if (FillBracketInternalFirstArrowSwap(currentNode, foot, currentFootPortion, newArrow))
			{
				var results = FillBracketInternalSecondArrowSame(currentNode, foot, currentFootPortion, newArrow,
					new[] { Heel, Toe },
					footActions,
					true, null, true);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			// Heel first index, toe second index
			if (FillBracketInternalFirstArrowSame(currentNode, foot, currentFootPortion, newArrow, out _, out _))
			{
				var results = FillBracketInternalSecondArrowSwap(currentNode, foot, currentFootPortion, newArrow,
					new[] { Toe, Heel },
					new[] { footActions[Toe], footActions[Heel] });
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			return allResults;
		}

		private List<GraphNode> FillBracketHeelSwapToeNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int newArrow,
			FootAction[] footActions)
		{
#if DEBUG_STEPGRAPH
			// Cannot release on a new arrow.
			Debug.Assert(footActions[0] != FootAction.Release && footActions[1] != FootAction.Release);
#endif // DEBUG_STEPGRAPH

			// Cannot step on a new bracket if already holding on an arrow
			if (AnyHeld(currentNode.State, foot))
				return null;

			var allResults = new List<GraphNode>();

			// Heel first index, toe second index
			if (FillBracketInternalFirstArrowSwap(currentNode, foot, currentFootPortion, newArrow))
			{
				var results = FillBracketInternalSecondArrowNew(currentNode, foot, currentFootPortion, newArrow,
					new[] { Heel, Toe },
					footActions,
					true, null, true);
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			// Heel first index, toe second index
			if (FillBracketInternalFirstArrowNew(currentNode, foot, currentFootPortion, newArrow, Toe, out _, out _))
			{
				var results = FillBracketInternalSecondArrowSwap(currentNode, foot, currentFootPortion, newArrow,
					new[] { Toe, Heel },
					new[] { footActions[Toe], footActions[Heel] });
				if (results != null && results.Count > 0)
					allResults.AddRange(results);
			}

			return allResults;
		}
		#endregion Bracket Fill Functions

		#region Internal Bracket Fill Functions
		private bool FillBracketInternalFirstArrowNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int firstNewArrow,
			int firstNewFootPortion,
			out List<int> otherFootValidPairings,
			out bool validNextArrow)
		{
			var currentState = currentNode.State;
			validNextArrow = false;
			otherFootValidPairings = null;

			// Skip this check as a performance optimization if no foot portion is provided.
			if (firstNewFootPortion != InvalidFootPortion
				&& !IsValidNewArrowForBracket(currentState, foot, firstNewFootPortion, firstNewArrow))
				return false;

			// Skip if the first new arrow is a crossover with any other foot pairing.
			if (FootCrossesOverWithAnyOtherFoot(currentState, foot, firstNewArrow))
				return false;

			// Skip if the first new arrow is not a valid pairing for any other foot arrows.
			otherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, firstNewArrow);
			if (otherFootValidPairings.Count == 0)
				return false;

			validNextArrow = PadData.ArrowData[currentNode.State[foot, currentFootPortion].Arrow].ValidNextArrows[firstNewArrow];
			return true;
		}

		private List<GraphNode> FillBracketInternalSecondArrowNew(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int firstNewArrow,
			int[] newFootPortions,
			FootAction[] footActions,
			bool firstNewArrowIsValidPlacement,
			List<int> firstArrowOtherFootValidPairings,
			bool swap = false)
		{
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

			var results = new List<GraphNode>();
			var lastSecondArrowIndexToCheck = Math.Min(NumArrows - 1, firstNewArrow + PadData.MaxBracketSeparation);
			for (var secondNewArrow = firstNewArrow + 1; secondNewArrow <= lastSecondArrowIndexToCheck; secondNewArrow++)
			{
				// Skip if this is not a valid bracketable pairing.
				if (newFootPortions[1] == Heel && !PadData.ArrowData[firstNewArrow].BracketablePairingsOtherHeel[foot][secondNewArrow])
					continue;
				if (newFootPortions[1] == Toe && !PadData.ArrowData[firstNewArrow].BracketablePairingsOtherToe[foot][secondNewArrow])
					continue;
				// Skip if this next arrow is not a valid new arrow for this step.
				if (!IsValidNewArrowForBracket(currentState, foot, newFootPortions[1], secondNewArrow))
					continue;
				// Skip if this second arrow is a crossover with any other foot pairing.
				if (FootCrossesOverWithAnyOtherFoot(currentState, foot, secondNewArrow))
					continue;
				// Skip if the second arrow is not a valid pairing for any other foot arrows.
				var secondOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, secondNewArrow);
				if (secondOtherFootValidPairings.Count == 0)
					continue;

				// One of the pair must be a valid next placement
				var secondNewArrowIsValidPlacement = PadData.ArrowData[currentArrow].ValidNextArrows[secondNewArrow];
				if (!firstNewArrowIsValidPlacement && !secondNewArrowIsValidPlacement)
					continue;

				// Both feet on the bracket must be reachable from at least one of the other foot's arrows.
				// If the first arrow is a swap then the bracket is considered reachable and this list will be null.
				if (firstArrowOtherFootValidPairings != null
				    && !firstArrowOtherFootValidPairings.Intersect(secondOtherFootValidPairings).Any())
					continue;

				// Set up the state for a new node.
				results.Add(CreateNewBracketNode(
					currentState,
					foot,
					firstNewArrow,
					secondNewArrow,
					newFootPortions,
					footActions,
					swap ? firstNewArrow : InvalidArrowIndex));
			}

			return results;
		}

		/// <summary>
		/// Helper for determining if an arrow should be considered a valid new arrow
		/// for a bracket step.
		/// </summary>
		private bool IsValidNewArrowForBracket(GraphNode.FootArrowState[,] currentState, int foot, int footPortion, int newArrow)
		{
			var otherFoot = OtherFoot(foot);
			var numResting = 0;
			var onFootPortion = InvalidFootPortion;
			for (var p = 0; p < NumFootPortions; p++)
			{
				// Cannot step to an arrow occupied by the other foot.
				if (currentState[otherFoot, p].Arrow == newArrow)
				{
					return false;
				}
				if (currentState[foot, p].Arrow == newArrow)
				{
					numResting++;
					onFootPortion = p;
				}
				else if (currentState[foot, p].Arrow != InvalidArrowIndex)
				{
					numResting++;
				}
			}

			// New arrow from a bracketing position.
			// In this scenario the move can be to a free arrow or to an arrow occupied
			// by the other portion of this foot. We allow this move in order to support
			// a bracket like DL to LU where the heel moves to the old toe position.
			if (numResting == NumFootPortions)
			{
				if (onFootPortion == footPortion)
					return false;
			}

			// New arrow from a normal position of resting on one arrow.
			// In this scenario the move must be to a free arrow.
			else
			{
				if (onFootPortion != InvalidFootPortion)
					return false;
			}

			return true;
		}

		/// <summary>
		/// This is different enough from filling a same arrow on a bracket with a non-same arrow portion
		/// that it makes sense to keep it as its own function. In here we care about the portion used on
		/// the same arrow and when filling SameArrow and anything else we don't care. We also don't need
		/// to perform any checks that the arrows are bracketable and reachable when they are both the same
		/// since those checks were performed previously.
		/// </summary>
		private List<GraphNode> FillBracketInternalHeelSameToeSame(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int firstNewArrow,
			int[] newFootPortions,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

			// Must be all releases or all placements.
			// This check is performed when creating ActionCombinations so we do not
			// need to perform it again here.
			var releasing = footActions[0] == FootAction.Release;

			// Check to make sure we are acting on the same arrow.
			// This is to ensure we do not process the bracket twice from the caller.
			if (currentArrow != firstNewArrow)
				return null;

			// Check for state at the first new arrow matching expected state for whether releasing or placing.
			if (!releasing && !IsRestingWithFootPortion(currentState, firstNewArrow, foot, newFootPortions[0]))
				return null;
			if (releasing && !IsHeldWithFootPortion(currentState, firstNewArrow, foot, newFootPortions[0]))
				return null;

			// If this follows a footswap then you cannot perform this action.
			// See FillSameArrowInternal.
			var otherFoot = OtherFoot(foot);
			if (!releasing && IsResting(currentState, firstNewArrow, otherFoot))
				return null;

			var lastSecondArrowIndexToCheck = Math.Min(NumArrows - 1, firstNewArrow + PadData.MaxBracketSeparation);
			for (var secondNewArrow = firstNewArrow + 1; secondNewArrow <= lastSecondArrowIndexToCheck; secondNewArrow++)
			{
				// Check for state at the second new arrow matching expected state for whether releasing or placing.
				if (!releasing && !IsRestingWithFootPortion(currentState, secondNewArrow, foot, newFootPortions[1]))
					continue;
				if (releasing && !IsHeldWithFootPortion(currentState, secondNewArrow, foot, newFootPortions[1]))
					continue;

				// If this follows a footswap then you cannot perform this action.
				// See FillSameArrowInternal.
				if (!releasing && IsResting(currentState, secondNewArrow, otherFoot))
					return null;

				// Set up the state for a new node.
				return new List<GraphNode>
					{CreateNewBracketNode(currentState, foot, firstNewArrow, secondNewArrow, newFootPortions, footActions)};
			}

			return null;
		}

		private bool FillBracketInternalFirstArrowSame(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int firstNewArrow,
			out List<int> otherFootValidPairings,
			out bool validNextArrow)
		{
			var currentState = currentNode.State;

			otherFootValidPairings = null;
			validNextArrow = false;

			// Must be a step on the same arrow.
			if (!IsResting(currentState, firstNewArrow, foot))
				return false;

			// Skip if the first new arrow is not a valid pairing for any other foot arrows.
			otherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, firstNewArrow);
			if (otherFootValidPairings.Count == 0)
				return false;

			// If this follows a footswap then you cannot perform this action.
			// See FillSameArrowInternal.
			var otherFoot = OtherFoot(foot);
			if (IsOn(currentState, firstNewArrow, otherFoot))
				return false;

			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;
			validNextArrow = PadData.ArrowData[currentArrow].ValidNextArrows[firstNewArrow];
			return true;
		}

		private List<GraphNode> FillBracketInternalSecondArrowSame(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int firstNewArrow,
			int[] newFootPortions,
			FootAction[] footActions,
			bool firstNewArrowIsValidPlacement,
			List<int> firstArrowOtherFootValidPairings,
			bool swap = false)
		{
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

			var results = new List<GraphNode>();
			var lastSecondArrowIndexToCheck = Math.Min(NumArrows - 1, firstNewArrow + PadData.MaxBracketSeparation);
			for (var secondNewArrow = firstNewArrow + 1; secondNewArrow <= lastSecondArrowIndexToCheck; secondNewArrow++)
			{
				// Skip if this is not a valid bracketable pairing.
				if (newFootPortions[1] == Heel && !PadData.ArrowData[firstNewArrow].BracketablePairingsOtherHeel[foot][secondNewArrow])
					continue;
				if (newFootPortions[1] == Toe && !PadData.ArrowData[firstNewArrow].BracketablePairingsOtherToe[foot][secondNewArrow])
					continue;
				// The second new arrow must be a step on the same arrow.
				if (!IsResting(currentState, secondNewArrow, foot))
					continue;
				// Skip if this second arrow is a crossover with any other foot pairing.
				if (FootCrossesOverWithAnyOtherFoot(currentState, foot, secondNewArrow))
					continue;
				// Skip if the second arrow is not a valid pairing for any other foot arrows.
				var secondOtherFootValidPairings = GetValidPairingsWithOtherFoot(currentState, foot, secondNewArrow);
				if (secondOtherFootValidPairings.Count == 0)
					continue;
				// One of the pair must be a valid next placement.
				if (!firstNewArrowIsValidPlacement && !PadData.ArrowData[currentArrow].ValidNextArrows[secondNewArrow])
					continue;
				// Both feet on the bracket must be reachable from at least one of the other foot's arrows.
				// If the first arrow is a swap then the bracket is considered reachable and this list will be null.
				if (firstArrowOtherFootValidPairings != null
				    && !firstArrowOtherFootValidPairings.Intersect(secondOtherFootValidPairings).Any())
					continue;

				// If this follows a footswap then you cannot perform this action.
				// See FillSameArrowInternal.
				var otherFoot = OtherFoot(foot);
				if (IsOn(currentState, secondNewArrow, otherFoot))
					return null;

				// Set up the state for a new node.
				results.Add(CreateNewBracketNode(
					currentState,
					foot,
					firstNewArrow,
					secondNewArrow,
					newFootPortions,
					footActions,
					swap ? firstNewArrow : InvalidArrowIndex));
			}

			return results;
		}

		private bool FillBracketInternalFirstArrowSwap(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int firstNewArrow)
		{
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;

			// Must be new arrow.
			if (currentArrow == firstNewArrow)
				return false;

			var otherFoot = OtherFoot(foot);

			// The new arrow must have the other foot resting so it can be swapped to.
			if (!IsResting(currentState, firstNewArrow, otherFoot))
				return false;
			// Disallow foot swap if the other foot is holding.
			if (AnyHeld(currentState, otherFoot))
				return false;

			return true;
		}

		private List<GraphNode> FillBracketInternalSecondArrowSwap(
			GraphNode currentNode,
			int foot,
			int currentFootPortion,
			int firstNewArrow,
			int[] newFootPortions,
			FootAction[] footActions)
		{
			var currentState = currentNode.State;
			var currentArrow = currentNode.State[foot, currentFootPortion].Arrow;
			var otherFoot = OtherFoot(foot);

			// Disallow foot swap if the other foot is holding.
			if (AnyHeld(currentState, otherFoot))
				return null;

			var results = new List<GraphNode>();
			var lastSecondArrowIndexToCheck = Math.Min(NumArrows - 1, firstNewArrow + PadData.MaxBracketSeparation);
			for (var secondNewArrow = firstNewArrow + 1; secondNewArrow <= lastSecondArrowIndexToCheck; secondNewArrow++)
			{
				// Skip if this is not a valid bracketable pairing.
				if (newFootPortions[1] == Heel && !PadData.ArrowData[firstNewArrow].BracketablePairingsOtherHeel[foot][secondNewArrow])
					continue;
				if (newFootPortions[1] == Toe && !PadData.ArrowData[firstNewArrow].BracketablePairingsOtherToe[foot][secondNewArrow])
					continue;

				// Must be new arrow.
				if (currentArrow == secondNewArrow)
					continue;
				// The new arrow must have the other foot resting so it can be swapped to.
				if (!IsResting(currentState, secondNewArrow, otherFoot))
					continue;

				// Do not need to check if either arrow is a valid next placement since we know this one is.
				// Do not need to check for both feet on the bracket being reachable from at least one of the
				// other foot's arrows since a swap is always valid.

				// Set up the state for a new node.
				results.Add(CreateNewBracketNode(
					currentState,foot, firstNewArrow, secondNewArrow, newFootPortions, footActions, secondNewArrow));
			}

			return results;
		}

		private static GraphNode CreateNewBracketNode(
			GraphNode.FootArrowState[,] currentState,
			int foot,
			int firstArrow,
			int secondArrow,
			int[] footPortions,
			FootAction[] footActions,
			int swapArrow = InvalidArrowIndex)
		{
			// Set up the state for a new node.
			var newState = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			var otherFoot = OtherFoot(foot);

			// Update the other foot state after a swap.
			if (swapArrow != InvalidArrowIndex)
			{
				UpdateOtherFootNewStateAfterSwapToArrow(currentState, newState, otherFoot, swapArrow);
			}
			// If this is not a swap then the other foot doesn't change.
			else
			{
				for (var p = 0; p < NumFootPortions; p++)
					newState[otherFoot, p] = currentState[otherFoot, p];
			}
			
			// The given foot brackets the two new arrows.
			newState[foot, footPortions[0]] = new GraphNode.FootArrowState(firstArrow, StepData.StateAfterAction[(int)footActions[0]]);
			newState[foot, footPortions[1]] = new GraphNode.FootArrowState(secondArrow, StepData.StateAfterAction[(int)footActions[1]]);

			// Brackets are not inverted.
			return new GraphNode(newState, BodyOrientation.Normal);
		}
		#endregion Internal Bracket Fill Functions

		#region Fill Helpers
		private static void UpdateOtherFootNewStateAfterSwapToArrow(
			GraphNode.FootArrowState[,] currentState,
			GraphNode.FootArrowState[,] newState,
			int otherFoot,
			int arrow)
		{
			// Check the current state to see if the other foot is resting on another arrow besides the one
			// being swapped.
			var otherArrowUnderOtherFoot = InvalidArrowIndex;
			var otherArrowUnderFootState = GraphArrowState.Resting;
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (currentState[otherFoot, p].Arrow != InvalidArrowIndex && currentState[otherFoot, p].Arrow != arrow)
				{
					otherArrowUnderOtherFoot = currentState[otherFoot, p].Arrow;
					otherArrowUnderFootState = currentState[otherFoot, p].State;
					break;
				}
			}

			// Update the other foot in the new state.
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (p == DefaultFootPortion)
				{
					// If we swapped but the other foot is on another arrow, then in the new state, it should
					// have its default foot portion still on that arrow in whatever state it was, and it should
					// be invalid on the swapped arrow.
					if (otherArrowUnderOtherFoot != InvalidArrowIndex)
						newState[otherFoot, p] = new GraphNode.FootArrowState(otherArrowUnderOtherFoot, otherArrowUnderFootState);
					// If the other arrow is only on the arrow being swapped to then we should keep it on that
					// arrow as resting so we know where it is for next foot placement.
					else
						newState[otherFoot, p] = new GraphNode.FootArrowState(arrow, GraphArrowState.Resting);
				}
				else
					newState[otherFoot, p] = GraphNode.InvalidFootArrowState;
			}
		}

		private bool IsValidPairingWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && PadData.ArrowData[otherFootArrowIndex].OtherFootPairings[otherFoot][arrow])
					return true;
			}
			return false;
		}

		private List<int> GetValidPairingsWithOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var result = new List<int>();
			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && PadData.ArrowData[otherFootArrowIndex].OtherFootPairings[otherFoot][arrow])
					result.Add(otherFootArrowIndex);
			}
			return result;
		}

		private bool FootCrossedOverInFront(GraphNode.FootArrowState[,] state, int foot)
		{
			for (var p = 0; p < NumFootPortions; p++)
			{
				var footArrowIndex = state[foot, p].Arrow;
				if (footArrowIndex != InvalidArrowIndex
				    && FootCrossesOverInFrontWithAnyOtherFoot(state, foot, footArrowIndex))
					return true;
			}
			return false;
		}

		private bool FootCrossedOverInBack(GraphNode.FootArrowState[,] state, int foot)
		{
			for (var p = 0; p < NumFootPortions; p++)
			{
				var footArrowIndex = state[foot, p].Arrow;
				if (footArrowIndex != InvalidArrowIndex
				    && FootCrossesOverInBackWithAnyOtherFoot(state, foot, footArrowIndex))
					return true;
			}
			return false;
		}

		private bool FootCrossesOverWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && (PadData.ArrowData[otherFootArrowIndex].OtherFootPairingsOtherFootCrossoverFront[otherFoot][arrow]
				        || PadData.ArrowData[otherFootArrowIndex].OtherFootPairingsOtherFootCrossoverBehind[otherFoot][arrow]))
					return true;
			}

			return false;
		}

		private bool FootCrossesOverInFrontWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && PadData.ArrowData[otherFootArrowIndex].OtherFootPairingsOtherFootCrossoverFront[otherFoot][arrow])
					return true;
			}

			return false;
		}

		private bool FootCrossesOverInBackWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && PadData.ArrowData[otherFootArrowIndex].OtherFootPairingsOtherFootCrossoverBehind[otherFoot][arrow])
					return true;
			}

			return false;
		}

		private bool FootInvertsWithAnyOtherFoot(GraphNode.FootArrowState[,] state, int foot, int arrow)
		{
			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				var otherFootArrowIndex = state[otherFoot, p].Arrow;
				if (otherFootArrowIndex != InvalidArrowIndex
				    && PadData.ArrowData[otherFootArrowIndex].OtherFootPairingsInverted[otherFoot][arrow])
					return true;
			}

			return false;
		}

		private static bool IsFree(GraphNode.FootArrowState[,] state, int arrow)
		{
			for (var f = 0; f < NumFeet; f++)
				for (var p = 0; p < NumFootPortions; p++)
					if (state[f, p].Arrow == arrow)
						return false;
			return true;
		}

		private static int NumHeld(GraphNode.FootArrowState[,] state, int foot)
		{
			var num = 0;
			for (var p = 0; p < NumFootPortions; p++)
				if (state[foot, p].Arrow != InvalidArrowIndex && state[foot, p].State == GraphArrowState.Held)
					num++;
			return num;
		}

		private static bool AnyHeld(GraphNode.FootArrowState[,] state, int foot)
		{
			for (var p = 0; p < NumFootPortions; p++)
				if (state[foot, p].Arrow != InvalidArrowIndex && state[foot, p].State == GraphArrowState.Held)
					return true;
			return false;
		}

		private static bool IsHeldWithFootPortion(GraphNode.FootArrowState[,] state, int arrow, int foot, int footPortion)
		{
			if (state[foot, footPortion].Arrow == arrow && state[foot, footPortion].State == GraphArrowState.Held)
				return true;
			return false;
		}

		private static bool IsResting(GraphNode.FootArrowState[,] state, int arrow, int foot)
		{
			for (var p = 0; p < NumFootPortions; p++)
				if (state[foot, p].Arrow == arrow && state[foot, p].State == GraphArrowState.Resting)
					return true;
			return false;
		}

		private static bool IsOn(GraphNode.FootArrowState[,] state, int arrow, int foot)
		{
			for (var p = 0; p < NumFootPortions; p++)
				if (state[foot, p].Arrow == arrow)
					return true;
			return false;
		}

		private static bool IsRestingWithFootPortion(GraphNode.FootArrowState[,] state, int arrow, int foot, int footPortion)
		{
			if (state[foot, footPortion].Arrow == arrow && state[foot, footPortion].State == GraphArrowState.Resting)
				return true;
			return false;
		}

		private static bool IsStateRestingAtIndex(GraphNode.FootArrowState[,] state, int a, int foot)
		{
			if (state[foot, a].Arrow != InvalidArrowIndex && state[foot, a].State == GraphArrowState.Resting)
				return true;
			return false;
		}
		#endregion Fill Helpers
		#endregion Fill

		#region Logging
		private void LogInfo(string message)
		{
			Logger.Info($"[StepGraph] [{LogIdentifier} ({NumArrows})] {message}");
		}
		#endregion Logging
	}
}
