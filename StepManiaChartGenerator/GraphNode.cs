using System;
using System.Collections.Generic;
using static StepManiaChartGenerator.Constants;

namespace StepManiaChartGenerator
{
	/// <summary>
	/// Node in a StepGraph.
	/// Connected to other GraphNodes by GraphLinks.
	/// One GraphLink may attach a GraphNode to multiple other GraphNodes.
	/// Represents the state of each foot. Each foot may be on one or two (NumFootPortions) arrows.
	/// In this representation, a foot is never considered to be lifted or in the air.
	/// The GraphNodes represent where the player's feet are after making a move.
	/// Mines aren't considered in this representation.
	/// Footswaps result in both feet resting on the same arrow.
	/// GraphNodes are considered equal if their state (but not necessarily GraphLinks) are equal.
	/// </summary>
	public class GraphNode : IEquatable<GraphNode>
	{
		/// <summary>
		/// The state of a foot and an arrow within a GraphNode.
		/// </summary>
		public readonly struct FootArrowState
		{
			/// <summary>
			/// The arrow / lane under the foot.
			/// </summary>
			public readonly int Arrow;
			/// <summary>
			/// The state of the foot on the arrow.
			/// </summary>
			public readonly GraphArrowState State;

			public FootArrowState(int arrow, GraphArrowState state)
			{
				Arrow = arrow;
				State = state;
			}

			public bool EqualsArrow(FootArrowState other)
			{
				return Arrow == other.Arrow;
			}

			#region IEquatable Implementation
			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				if (obj is FootArrowState f)
					return Arrow == f.Arrow && State == f.State;
				return false;
			}

			public override int GetHashCode()
			{
				var hash = 17;
				hash = unchecked(hash * 31 + Arrow);
				hash = unchecked(hash * 31 + (int)State);
				return hash;
			}
			#endregion
		}

		/// <summary>
		/// Static FootArrowState instance for ease of setting up an invalid state in lue of null.
		/// </summary>
		public static readonly FootArrowState InvalidFootArrowState;
		static GraphNode()
		{
			InvalidFootArrowState = new FootArrowState(InvalidArrowIndex, GraphArrowState.Resting);
		}

		/// <summary>
		/// The state of both feet.
		/// First index is the foot.
		/// Second index is the foot portion ([0-NumFootPortions)).
		/// For brackets, Heel (index 0) and Toe (index 1) are used for the second index.
		/// For normal steps, DefaultFootPortion (index 0) is used for the second index.
		/// </summary>
		public readonly FootArrowState[,] State;

		/// <summary>
		/// BodyOrientation at this GraphNode.
		/// Some GraphNodes can have identical State but different Orientations.
		/// </summary>
		public readonly BodyOrientation Orientation;

		/// <summary>
		/// GraphLinks to other GraphNodes.
		/// A GraphLink may connection more than one GraphNode.
		/// </summary>
		public Dictionary<GraphLink, List<GraphNode>> Links = new Dictionary<GraphLink, List<GraphNode>>();

		/// <summary>
		/// Constructor requiring the State for the GraphNode.
		/// </summary>
		/// <param name="state">State for the GraphNode.</param>
		/// <param name="orientation">BodyOrientation for the GraphNode.</param>
		public GraphNode(FootArrowState[,] state, BodyOrientation orientation)
		{
			State = state;
			Orientation = orientation;
		}

		/// <summary>
		/// Returns whether or not the given GraphNode matches this GraphNode's
		/// footing. Does not check GraphArrowState differences.
		/// </summary>
		/// <param name="other">Other GraphNode to compare against.</param>
		/// <returns>
		/// Whether or not the given GraphNode matches this GraphNode's footing.
		/// </returns>
		public bool EqualsFooting(GraphNode other)
		{
			if (other == null)
				return false;
			if (State.Length != other.State.Length)
				return false;
			for (var f = 0; f < NumFeet; f++)
				for (var p = 0; p < NumFootPortions; p++)
					if (!State[f, p].EqualsArrow(other.State[f, p]))
						return false;
			if (Orientation != other.Orientation)
				return false;
			return true;
		}

		#region IEquatable Implementation
		public bool Equals(GraphNode other)
		{
			if (other == null)
				return false;
			if (State.Length != other.State.Length)
				return false;
			for (var f = 0; f < NumFeet; f++)
				for (var p = 0; p < NumFootPortions; p++)
					if (!State[f, p].Equals(other.State[f, p]))
						return false;
			if (Orientation != other.Orientation)
				return false;
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (!(obj is GraphNode g))
				return false;
			return Equals(g);
		}

		public override int GetHashCode()
		{
			var hash = 17;
			for (var f = 0; f < NumFeet; f++)
				for (var p = 0; p < NumFootPortions; p++)
					hash = unchecked(hash * 31 + State[f, p].GetHashCode());
			hash = unchecked(hash * 31 + Orientation.GetHashCode());
			return hash;
		}
		#endregion
	}

	/// <summary>
	/// Class for attaching extra data to a GraphNode when it is used for ExpressedCharts
	/// and PerformedCharts. This allows GraphNode to remain slim, while supporting extra
	/// information at runtime, like what type of hold (hold or roll) is used.
	/// </summary>
	public class GraphNodeInstance
	{
		/// <summary>
		/// Underlying GraphNode.
		/// </summary>
		public GraphNode Node;

		/// <summary>
		/// Per foot and portion, any special instance types for the GraphNode.
		/// </summary>
		public InstanceStepType[,] InstanceTypes = new InstanceStepType[NumFeet, NumFootPortions];
	}
}
