using StepManiaLibrary;
using static StepManiaLibrary.Constants;

namespace StepManiaChartGenerator
{
	/// <summary>
	/// Methods to aid in debugging.
	/// </summary>
	class DebugUtils
	{
		public static bool StateMatches(GraphNode node,
			int leftArrow, GraphArrowState leftState,
			int rightArrow, GraphArrowState rightState)
		{
			var state = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			for (var f = 0; f < NumFeet; f++)
				for (var p = 0; p < NumFootPortions; p++)
					state[f, p] = new GraphNode.FootArrowState(InvalidArrowIndex, GraphArrowState.Resting);

			state[0, 0] = new GraphNode.FootArrowState(leftArrow, leftState);
			state[1, 0] = new GraphNode.FootArrowState(rightArrow, rightState);
			GraphNode newNode = new GraphNode(state, BodyOrientation.Normal);
			return node.Equals(newNode);
		}

		public static bool StateMatches(GraphNode node,
			int leftArrow, GraphArrowState leftState,
			int leftArrow2, GraphArrowState leftState2,
			int rightArrow, GraphArrowState rightState,
			int rightArrow2, GraphArrowState rightState2)
		{
			var state = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			state[0, 0] = new GraphNode.FootArrowState(leftArrow, leftState);
			state[0, 1] = new GraphNode.FootArrowState(leftArrow2, leftState2);
			state[1, 0] = new GraphNode.FootArrowState(rightArrow, rightState);
			state[1, 1] = new GraphNode.FootArrowState(rightArrow2, rightState2);
			GraphNode newNode = new GraphNode(state, BodyOrientation.Normal);
			return node.Equals(newNode);
		}

		public static bool StateMatches(GraphNode.FootArrowState[,] state,
			int leftArrow, GraphArrowState leftState,
			int rightArrow, GraphArrowState rightState)
		{
			GraphNode node = new GraphNode(state, BodyOrientation.Normal);
			var newState = new GraphNode.FootArrowState[NumFeet, NumFootPortions];
			for (var f = 0; f < NumFeet; f++)
				for (var p = 0; p < NumFootPortions; p++)
					newState[f, p] = new GraphNode.FootArrowState(InvalidArrowIndex, GraphArrowState.Resting);

			newState[0, 0] = new GraphNode.FootArrowState(leftArrow, leftState);
			newState[1, 0] = new GraphNode.FootArrowState(rightArrow, rightState);
			GraphNode newNode = new GraphNode(newState, BodyOrientation.Normal);
			return node.Equals(newNode);
		}
	}
}
