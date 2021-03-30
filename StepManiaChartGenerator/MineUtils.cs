using System.Collections.Generic;
using Fumen;
using Fumen.ChartDefinition;
using static StepManiaChartGenerator.Constants;

namespace StepManiaChartGenerator
{
	/// <summary>
	/// Enumeration of ways to express a MineEvent.
	/// </summary>
	public enum MineType
	{
		/// <summary>
		/// Expressing a mine as occurring after a specific arrow is most preferable as
		/// this is typically done to inform future footing like a double step or a foot
		/// swap.
		/// </summary>
		AfterArrow,

		/// <summary>
		/// If a mine can't be expressed as occurring after an arrow because no arrow
		/// precedes it, then the next best way to express it is as occurring before a
		/// specific arrow.
		/// </summary>
		BeforeArrow,

		/// <summary>
		/// In the rare case that a mine is in a lane with no arrows then it is expressed
		/// as occurring with no arrow.
		/// </summary>
		NoArrow
	}

	/// <summary>
	/// Utilities to help with creating mine events in ExpressedCharts and PerformedCharts.
	/// This class isn't strictly necessary but it is helpful for a few reasons:
	///  1) Mines are associated with neighboring steps or releases. ExpressedCharts and
	///     PerformedCharts don't have a simple way to report what is a step and what is
	///     a release. This class keeps that logic in one spot so that these classes can
	///     stay in sync. See GetReleasesAndSteps.
	///  2) Similarly, logic for what constitutes the Nth most recent arrow (chiefly, that
	///     arrows at the same position share the same N) was duplicated in ExpressedChart
	///     and now PerformedChart. This class keeps that logic in one spot.
	///  3) Mine logic was causing legibility issues when integrated into ExpressedChart
	///     and PerformedChart. By moving that logic here those classes focus more on just
	///     steps.
	/// </summary>
	public class MineUtils
	{
		/// <summary>
		/// Mines are associated with neighboring arrows.
		/// This class is used to navigate over releases and steps easily to associate mines.
		/// </summary>
		public class FootActionEvent
		{
			public MetricPosition Position;
			public int Arrow;
			public int Foot;
		}

		/// <summary>
		/// To create FootActionEvents from an ExpressedChart or a PerformedChart we need
		/// to loop over the structures of those charts. Those charts both represent data
		/// with GraphNodes and GraphLinks. This interfaces allows for a common method
		/// of generating release and step lists of FootActionEvents for the charts.
		/// </summary>
		public interface IChartNode
		{
			GraphNode GetGraphNode();
			GraphLink GetGraphLinkToNode();
			MetricPosition GetPosition();
		}

		/// <summary>
		/// Creates lists of FootActionEvents for all steps and all releases for the given
		/// List of IChartNodes.
		/// This assumes the first event in the given events is the resting position.
		/// </summary>
		/// <typeparam name="T">IChartNode, for example ExpressedChart.ChartSearchNode or
		/// PerformedChart.StepPerformanceNode.</typeparam>
		/// <param name="events">List of IChartNodes representing the charts nodes.</param>
		/// <param name="numArrows">Number of arrows in the Chart or StepGraph.</param>
		/// <returns>List representing releases and List representing steps.</returns>
		public static (List<FootActionEvent>, List<FootActionEvent>) GetReleasesAndSteps<T>(List<T> events, int numArrows)
			where T : IChartNode
		{
			var releases = new List<FootActionEvent>();
			var steps = new List<FootActionEvent>();
			var numEvents = events.Count;
			if (numEvents == 0)
				return (releases, steps);

			var eventIndex = 0;
			var previousState = CreateState(events[0].GetGraphNode(), numArrows);

			// Skip first event representing the resting position.
			eventIndex++;

			while (eventIndex < numEvents)
			{
				var node = events[eventIndex];
				var graphNode = node.GetGraphNode();
				var linkToNode = node.GetGraphLinkToNode();

				// Compare current state and previous state for each foot.
				var currentState = CreateState(graphNode, numArrows);
				for (var f = 0; f < NumFeet; f++)
				{
					for (var arrow = 0; arrow < numArrows; arrow++)
					{
						var addStep = false;
						var addRelease = false;

						// Releasing a hold
						if ((currentState[f, arrow] == (int) GraphArrowState.Resting || currentState[f, arrow] == -1)
						    && previousState[f, arrow] == (int) GraphArrowState.Held)
						{
							addRelease = true;
						}
						// Tapping on a new arrow
						else if (currentState[f, arrow] == (int) GraphArrowState.Resting
						         && previousState[f, arrow] == -1)
						{
							addStep = true;
							addRelease = true;
						}
						// Starting a hold
						else if (currentState[f, arrow] == (int) GraphArrowState.Held
						         && (previousState[f, arrow] == (int) GraphArrowState.Resting || previousState[f, arrow] == -1))
						{
							addStep = true;
						}
						// Tapping on the same arrow
						else if (previousState[f, arrow] == (int) GraphArrowState.Resting &&
						         currentState[f, arrow] == (int) GraphArrowState.Resting)
						{
							for (var p = 0; p < NumFootPortions; p++)
							{
								if (linkToNode.Links[f, p].Valid && linkToNode.Links[f, p].Action == FootAction.Tap)
								{
									addStep = true;
									addRelease = true;
									break;
								}
							}
						}

						if (addStep)
						{
							steps.Add(new FootActionEvent
							{
								Position = node.GetPosition(),
								Foot = f,
								Arrow = arrow
							});
						}

						if (addRelease)
						{
							releases.Add(new FootActionEvent
							{
								Position = node.GetPosition(),
								Foot = f,
								Arrow = arrow
							});
						}
					}
				}

				previousState = currentState;
				eventIndex++;
			}

			return (releases, steps);
		}

		/// <summary>
		/// Helper method for GetReleasesAndSteps.
		/// Creates a multidimensional array representing the states of all arrows
		/// for each foot. The first index is the foot from [0, NumFeet). The second
		/// index is the arrow from [0, numArrows). The value in the arrow is the
		/// ordinal value of the GraphArrowState for the given node, or -1 if that
		/// arrow is free.
		/// </summary>
		/// <param name="node">GraphNode to generate state from.</param>
		/// <param name="numArrows">Number of arrows in the Chart or StepGraph.</param>
		/// <returns></returns>
		private static int[,] CreateState(GraphNode node, int numArrows)
		{
			var state = new int[NumFeet, numArrows];
			for (var f = 0; f < NumFeet; f++)
				for (var arrow = 0; arrow < numArrows; arrow++)
					state[f, arrow] = -1;
			for (var f = 0; f < NumFeet; f++)
				for (var p = 0; p < NumFootPortions; p++)
					if (node.State[f, p].Arrow != InvalidArrowIndex)
						state[f, node.State[f, p].Arrow] = (int)node.State[f, p].State;
			return state;
		}

		/// <summary>
		/// Creates an ExpressedChart MineEvent.
		/// If possible, this MineEvent will be an AfterArrow type.
		/// If no arrow precedes this mine then this MineEvent will be a BeforeArrow type.
		/// If no arrow exists in the same lane as the mine then this will be a NoArrow type.
		/// </summary>
		/// <param name="numArrows">
		/// Number of arrows in the chart being used to generate the ExpressedChart.
		/// This is used to ease the searches into neighboring events.
		/// </param>
		/// <param name="releases">
		/// List of all FootActionEvents representing release events for the ExpressedChart.
		/// See GetReleasesAndSteps for release FootActionEvent generation.
		/// </param>
		/// <param name="releaseIndex">
		/// The index into releases of the release event which precedes the given mine.
		/// This could be determined by this method, but requiring it as a parameter is a
		/// performance optimization as it allows the caller to call this method in loop
		/// and avoid another scan.
		/// </param>
		/// <param name="steps">
		/// List of all FootActionEvents representing step events for the ExpressedChart.
		/// See GetReleasesAndSteps for release FootActionEvent generation.
		/// </param>
		/// <param name="stepIndex">
		/// The index into steps of the step event which follows the given mine.
		/// This could be determined by this method, but requiring it as a parameter is a
		/// performance optimization as it allows the caller to call this method in loop
		/// and avoid another scan.
		/// </param>
		/// <param name="mine">The LaneNote representing a mine from the original chart.</param>
		/// <returns>New MineEvent.</returns>
		public static ExpressedChart.MineEvent CreateExpressedMineEvent(
			int numArrows,
			List<FootActionEvent> releases,
			int releaseIndex,
			List<FootActionEvent> steps,
			int stepIndex,
			LaneNote mine)
		{
			// Try first to create an AfterArrow type of mine by associating the mine
			// with a preceding arrow.
			var (n, f) = GetHowRecentIsNeighboringArrow(true, releaseIndex, numArrows, releases, mine.Lane);
			if (n >= 0)
			{
				return new ExpressedChart.MineEvent(mine.Position, mine.TimeMicros, mine.Lane)
				{
					Type = MineType.AfterArrow,
					ArrowIsNthClosest = n,
					FootAssociatedWithPairedNote = f
				};
			}

			// Next, try to create a BeforeArrow type of mine by associating the mine
			// with a following arrow.
			(n, f) = GetHowRecentIsNeighboringArrow(false, stepIndex, numArrows, steps, mine.Lane);
			if (n >= 0)
			{
				return new ExpressedChart.MineEvent(mine.Position, mine.TimeMicros, mine.Lane)
				{
					Type = MineType.BeforeArrow,
					ArrowIsNthClosest = n,
					FootAssociatedWithPairedNote = f
				};
			}

			// The mine could not be associated with an arrow, use the default NoArrow type.
			return new ExpressedChart.MineEvent(mine.Position, mine.TimeMicros, mine.Lane);
		}

		/// <summary>
		/// For a mine positioned at the given arrow, determines how far away the neighboring
		/// arrow in the same lane is relative to arrows in other lanes. This is a number from
		/// [0, numArrows). Arrows sharing the same position (jumps and brackets) are considered
		/// at the same relative location.
		/// Example:
		/// X--- (This arrow is the 1st most recent relative to the mine.)
		/// -XX- (Both arrows here are the 0th most recent relative to the mine.)
		/// O--X (Mine. Arrow at this position is disqualified.)
		/// </summary>
		/// <param name="searchBackwards">
		/// If true then search backwards through the events.
		/// If false then search forwards through the events.
		/// </param>
		/// <param name="searchIndex">Search index into events to start the searching at.</param>
		/// <param name="numArrows">Number of arrows in the chart.</param>
		/// <param name="events">The List of FootActionEvent to search.</param>
		/// <param name="arrow">Arrow of the mine. Between [0, numArrows).</param>
		/// <returns>
		/// A tuple where the first value is the relative position of the neighboring arrow and
		/// the second value is the foot that was used for this arrow. If no arrow could be found
		/// then (InvalidArrowIndex, InvalidFoot) is returned.
		/// </returns>
		private static (int, int) GetHowRecentIsNeighboringArrow(
			bool searchBackwards,
			int searchIndex,
			int numArrows,
			List<FootActionEvent> events,
			int arrow)
		{
			var currentN = 0;
			var consideredArrows = new bool[numArrows];
			MetricPosition currentNPosition = null;
			while (searchBackwards ? searchIndex >= 0 : searchIndex < events.Count)
			{
				if (!consideredArrows[events[searchIndex].Arrow])
				{
					var newN = !(currentNPosition == null || currentNPosition == events[searchIndex].Position);
					if (newN)
						currentN++;
					currentNPosition = events[searchIndex].Position;

					if (events[searchIndex].Arrow == arrow)
						return (currentN, events[searchIndex].Foot);

					consideredArrows[events[searchIndex].Arrow] = true;
				}

				searchIndex += searchBackwards ? -1 : 1;
			}

			// Could not find an arrow
			return (InvalidArrowIndex, InvalidFoot);
		}

		/// <summary>
		/// Finds the lane of the Nth most recent arrow relative to the given indices
		/// of the given releases and steps. If all lanes at the desired N are occupied,
		/// then the search will continue to the next N until either a free location is
		/// found, or the search can no longer continue.
		/// If no arrow can be found, InvalidArrowIndex is returned.
		/// Used by PerformedChart in order to find which arrow should be used for a mine
		/// when we know the layout of the arrows and the N value from the
		/// ExpressedChart's MineEvent (ArrowIsNthClosest/desiredN).
		/// For determining N, arrows sharing the same position (jumps and brackets)
		/// are considered at the same relative location.
		/// Example:
		/// X--- (This arrow is the 1st most recent relative to the mine.)
		/// -XX- (Both arrows here are the 0th most recent relative to the mine.)
		/// O--X (Mine. Arrow at this position is disqualified.)
		/// </summary>
		/// <param name="searchBackwards">
		/// If true then search backwards through the releases.
		/// If false then search forwards through the steps.
		/// </param>
		/// <param name="desiredN">
		/// The desired N value (Nth most recent).
		/// </param>
		/// <param name="desiredFoot">
		/// The desired foot for the associated arrow.
		/// If multiple arrows are found that share the desired position then preference
		/// will be given to one that uses this foot.
		/// </param>
		/// <param name="numArrows"> Number of arrows in the chart.</param>
		/// <param name="releases">
		/// List of all FootActionEvents representing release events for the PerformedChart.
		/// See GetReleasesAndSteps for release FootActionEvent generation.
		/// </param>
		/// <param name="releaseIndex">
		/// The index into releases of the release event which precedes the given mine.
		/// This could be determined by this method, but requiring it as a parameter is a
		/// performance optimization as it allows the caller to call this method in loop
		/// and avoid another scan.
		/// </param>
		/// <param name="steps">
		/// List of all FootActionEvents representing step events for the PerformedChart.
		/// See GetReleasesAndSteps for release FootActionEvent generation.
		/// </param>
		/// <param name="stepIndex">
		/// The index into steps of the step event which follows the given mine.
		/// This could be determined by this method, but requiring it as a parameter is a
		/// performance optimization as it allows the caller to call this method in loop
		/// and avoid another scan.
		/// </param>
		/// <param name="arrowsOccupiedByMines">
		/// Array of booleans representing if the lane at that index is occupied by a mine
		/// at the position of the mine in question. Tracked externally.
		/// </param>
		/// <param name="minePosition">Position of the mine in question.</param>
		/// <param name="randomLaneOrder">Randomly ordered lane indices for fallback random lane choices.</param>
		/// <returns>The Nth most recent arrow or InvalidArrowIndex if none could be found.</returns>
		public static int FindBestNthMostRecentArrow(
			bool searchBackwards,
			int desiredN,
			int desiredFoot,
			int numArrows,
			List<FootActionEvent> releases,
			int releaseIndex,
			List<FootActionEvent> steps,
			int stepIndex,
			bool[] arrowsOccupiedByMines,
			MetricPosition minePosition,
			int[] randomLaneOrder)
		{
			var events = searchBackwards ? releases : steps;
			var searchIndex = searchBackwards ? releaseIndex : stepIndex;
			var currentN = 0;
			var bestArrow = InvalidArrowIndex;
			var consideredArrows = new bool[numArrows];
			var numArrowsConsidered = 0;
			MetricPosition currentNPosition = null;

			// Search
			while (searchBackwards ? searchIndex >= 0 : searchIndex < events.Count)
			{
				var arrow = events[searchIndex].Arrow;
				var pos = events[searchIndex].Position;

				// This event is for an arrow we haven't yet considered as the Nth most recent
				if (!consideredArrows[arrow])
				{
					// Check to see if we have already considered an arrow at this depth before.
					var newN = !(currentNPosition == null || currentNPosition == pos);

					// If we have advanced positions but we found a possible arrow a the last position
					// then use that and stop.
					if (newN && bestArrow != InvalidArrowIndex)
						break;

					// If we have advanced positions update the currentN tracker.
					if (newN)
					{
						currentN++;
						if (currentN == numArrows)
							break;
					}

					// Update the currentNPosition tracker.
					currentNPosition = pos;

					// This arrow is at least the Nth most recent.
					// This is a greater than or equal check in case no arrow is free at the desiredN.
					// In that case we continue searching further away.
					if (currentN >= desiredN)
					{
						// If this arrow free then we can use it.
						if (IsArrowFreeAtPosition(arrow, minePosition, releases, releaseIndex, steps, stepIndex,
							arrowsOccupiedByMines))
						{
							// Record this arrow as the best arrow.
							bestArrow = arrow;
							// If this arrow uses the desired foot, then we are done. If it doesn't keep
							// searching to see if another arrow exists at this depth that would be better.
							// If searching results in advancing currentN again we will stop since we have
							// recorded a bestArrow.
							if (events[searchIndex].Foot == desiredFoot)
								break;
						}
					}

					// Mark this arrow considered.
					consideredArrows[arrow] = true;
					numArrowsConsidered++;
					// If we have considered every possible arrow then stop searching.
					if (numArrowsConsidered == numArrows)
						break;
				}

				// Advance search
				searchIndex += searchBackwards ? -1 : 1;
			}

			// It is possible for the search to fail in some edge cases. For example if we are trying to place
			// a mine after the 4th most recent arrow and the Performed chart has only filled 3 lanes at this point
			// then we will not find a match. In this case, see if we can choose a random free arrow.
			if (bestArrow == InvalidArrowIndex)
			{
				foreach (var a in randomLaneOrder)
				{
					if (!consideredArrows[a]
					    && IsArrowFreeAtPosition(a, minePosition, releases, releaseIndex, steps, stepIndex,
						    arrowsOccupiedByMines))
					{
						bestArrow = a;
						break;
					}
				}
			}

			return bestArrow;
		}

		/// <summary>
		/// Helper method for FindBestNthMostRecentArrow to determine if the given
		/// arrow is free at the given position.
		/// </summary>
		/// <param name="arrow">The arrow to check.</param>
		/// <param name="position">The position to check.</param>
		/// <param name="releases">
		/// The List of release FootActionEvents from the PerformedChart.
		/// Needed to check hold/roll overlap.
		/// </param>
		/// <param name="startingReleaseIndex">
		/// The index into releases of the release event which precedes the given mine.
		/// Used to check hold/roll overlap.
		/// </param>
		/// <param name="steps">
		/// The List of step FootActionEvents from the PerformedChart.
		/// Needed to check hold/roll overlap.
		/// </param>
		/// <param name="startingStepIndex">
		/// The index into steps of the step event which follows the given mine.
		/// Used to check hold/roll overlap.
		/// </param>
		/// <param name="arrowsOccupiedByMines">
		/// Array of booleans representing if the lane at that index is occupied by a mine
		/// at the position of the mine in question. Tracked externally.
		/// </param>
		/// <returns>True if the arrow is free and false otherwise.</returns>
		private static bool IsArrowFreeAtPosition(
			int arrow,
			MetricPosition position,
			List<FootActionEvent> releases,
			int startingReleaseIndex,
			List<FootActionEvent> steps,
			int startingStepIndex,
			bool[] arrowsOccupiedByMines)
		{
			// If there is a mine at this location it is not free.
			if (arrowsOccupiedByMines[arrow])
				return false;

			// Find the step for this arrow at or before this position.
			var precedingStepIndex = System.Math.Min(startingStepIndex, steps.Count - 1);
			MetricPosition precedingStepPosition = null;
			while (precedingStepIndex >= 0)
			{
				if (steps[precedingStepIndex].Position <= position
				    && steps[precedingStepIndex].Arrow == arrow)
				{
					precedingStepPosition = steps[precedingStepIndex].Position;
					break;
				}

				precedingStepIndex--;
			}

			// If there is no step at or before the desired position, it is free.
			if (precedingStepPosition == null)
				return true;
			// If the preceding step is at the same position then it is not free.
			if (precedingStepPosition == position)
				return false;

			// Find the release for this arrow at or after this position.

			// First, back the release index up to preceding step.
			var followingReleaseIndex = startingReleaseIndex;
			while (followingReleaseIndex - 1 >= 0)
			{
				if (releases[followingReleaseIndex].Arrow == arrow
				    && releases[followingReleaseIndex - 1].Position < precedingStepPosition)
					break;
				followingReleaseIndex--;
			}

			// Now advance the release index to the following release.
			MetricPosition followingReleasePosition = null;
			while (followingReleaseIndex < releases.Count)
			{
				if (releases[followingReleaseIndex].Position >= precedingStepPosition
				    && releases[followingReleaseIndex].Arrow == arrow)
				{
					followingReleasePosition = releases[followingReleaseIndex].Position;
					break;
				}

				followingReleaseIndex++;
			}

			// If there is no release at or after the desired position, it is free.
			if (followingReleasePosition == null)
				return true;
			// If the following release is at the same position then it is not free.
			if (followingReleasePosition == position)
				return false;

			// If a hold or roll is active over this position then it is not free.
			if (precedingStepPosition < position && followingReleasePosition > position)
				return false;

			// It is free.
			return true;
		}
	}
}
