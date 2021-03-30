using System;
using static StepManiaChartGenerator.Constants;

namespace StepManiaChartGenerator
{
	/// <summary>
	/// Step types that can be applied to a GraphLinkInstance or a GraphNodeInstance
	/// to capture specific types of steps in a chart that aren't relevant in the
	/// StepGraph.
	/// </summary>
	public enum InstanceStepType
	{
		/// <summary>
		/// No special instance step type.
		/// </summary>
		Default,

		Roll,
		Fake,
		Lift
	}

	/// <summary>
	/// Link between GraphNodes in a StepGraph.
	/// Represents what each foot does to move from one GraphNode to a set of other GraphNodes.
	/// </summary>
	public class GraphLink : IEquatable<GraphLink>
	{
		/// <summary>
		/// The state for one FootPortion within a GraphLink.
		/// </summary>
		public readonly struct FootArrowState
		{
			/// <summary>
			/// StepType performed with the FootPortion.
			/// </summary>
			public readonly StepType Step;

			/// <summary>
			/// FootAction performed with the FootPortion.
			/// </summary>
			public readonly FootAction Action;

			/// <summary>
			/// Whether this is a valid FootArrowState within the GraphLink's Links.
			/// </summary>
			public readonly bool Valid;

			public FootArrowState(StepType step, FootAction action)
			{
				Step = step;
				Action = action;
				Valid = true;
			}

			#region IEquatable Implementation

			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				if (obj is FootArrowState f)
					return Step == f.Step && Action == f.Action && Valid == f.Valid;
				return false;
			}

			public override int GetHashCode()
			{
				var hash = 17;
				hash = unchecked(hash * 31 + (int) Step);
				hash = unchecked(hash * 31 + (int) Action);
				hash = unchecked(hash * 31 + (Valid ? 1 : 0));
				return hash;
			}

			#endregion
		}

		/// <summary>
		/// The actions of both feet.
		/// First index is the foot.
		/// Second index is the foot portion ([0-NumFootPortions)).
		/// For brackets, Heel (index 0) and Toe (index 1) are used for the second index.
		/// For normal steps, DefaultFootPortion (index 0) is used for the second index.
		/// </summary>
		public readonly FootArrowState[,] Links = new FootArrowState[NumFeet, NumFootPortions];

		/// <summary>
		/// Whether or not this link represents a jump with both feet.
		/// Includes bracket jumps
		/// </summary>
		/// <returns>True if this link is a jump and false otherwise.</returns>
		public bool IsJump()
		{
			for (var f = 0; f < NumFeet; f++)
			{
				var footHasStep = false;
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (Links[f, p].Valid && Links[f, p].Action != FootAction.Release)
					{
						footHasStep = true;
						break;
					}
				}

				if (!footHasStep)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Whether or not this link represents a release with any foot.
		/// </summary>
		/// <returns>True if this link is a release and false otherwise.</returns>
		public bool IsRelease()
		{
			for (var f = 0; f < NumFeet; f++)
			{
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (Links[f, p].Valid && Links[f, p].Action == FootAction.Release)
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Whether or not this link represents a step with one foot, regardless of if
		/// that is a bracket step.
		/// </summary>
		/// <param name="foot">The foot in question.</param>
		/// <returns>True if this link is a step with the given foot and false otherwise.</returns>
		public bool IsStepWithFoot(int foot)
		{
			// If this foot does not step then this is not a step with this foot.
			var thisFootSteps = false;
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (Links[foot, p].Valid && Links[foot, p].Action != FootAction.Release)
				{
					thisFootSteps = true;
					break;
				}
			}

			if (!thisFootSteps)
				return false;

			// If the other foot performs an action this is not a step with this foot.
			var otherFoot = OtherFoot(foot);
			for (var p = 0; p < NumFootPortions; p++)
			{
				if (Links[otherFoot, p].Valid)
				{
					return false;
				}
			}

			// This foot steps and the other foot does perform an action.
			return true;
		}

		/// <summary>
		/// Whether or not this link represents a bracket step with exactly one foot.
		/// </summary>
		/// <returns>
		/// True if this link is a bracket step with exactly one foot and false otherwise.
		/// </returns>
		public bool IsBracketStep()
		{
			var numFeetWithSteps = 0;
			var bracketFound = false;
			for (var f = 0; f < NumFeet; f++)
			{
				var allPortionsStep = true;
				var anyValid = false;
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (Links[f, p].Valid)
						anyValid = true;
					if (!Links[f, p].Valid || Links[f, p].Action == FootAction.Release)
						allPortionsStep = false;
				}

				if (allPortionsStep)
					bracketFound = true;
				if (anyValid)
					numFeetWithSteps++;
			}

			return numFeetWithSteps == 1 && bracketFound;
		}

		/// <summary>
		/// Whether or not this link represents a footswap.
		/// This includes a bracket that is also a footswap.
		/// </summary>
		/// <param name="foot">
		/// Out param to store the foot which performed the swap, if this is a foot swap.
		/// </param>
		/// <param name="portion">
		/// Out param to store the portion of the foot which performed the swap, if this is a foot swap.
		/// </param>
		/// <returns>True if this link is a footswap and false otherwise.</returns>
		public bool IsFootSwap(out int foot, out int portion)
		{
			foot = InvalidFoot;
			portion = DefaultFootPortion;
			for (var f = 0; f < NumFeet; f++)
			{
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (Links[f, p].Valid && StepData.Steps[(int) Links[f, p].Step].IsFootSwap[p])
					{
						foot = f;
						portion = p;
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Gets wither this link involves any brackets.
		/// Includes both single arrow and multiple arrow steps.
		/// </summary>
		/// <returns>True if this link involves any brackets and false otherwise.</returns>
		public bool InvolvesBracket()
		{
			for (var f = 0; f < NumFeet; f++)
			{
				for (var p = 0; p < NumFootPortions; p++)
				{
					if (Links[f, p].Valid && StepData.Steps[(int) Links[f, p].Step].IsBracket)
						return true;
				}
			}

			return false;
		}

		#region IEquatable Implementation

		public bool Equals(GraphLink other)
		{
			if (other == null)
				return false;
			for (var f = 0; f < NumFeet; f++)
				for (var p = 0; p < NumFootPortions; p++)
					if (!Links[f, p].Equals(other.Links[f, p]))
						return false;
			return true;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (!(obj is GraphLink g))
				return false;
			return Equals(g);
		}

		public override int GetHashCode()
		{
			var hash = 17;
			for (var f = 0; f < NumFeet; f++)
				for (var p = 0; p < NumFootPortions; p++)
					hash = unchecked(hash * 31 + Links[f, p].GetHashCode());
			return hash;
		}

		#endregion
	}

	/// <summary>
	/// Class for attaching extra data to a GraphLink when it is used for ExpressedCharts
	/// and PerformedCharts. This allows GraphLink to remain slim, while supporting extra
	/// information at runtime, like what type of hold (hold or roll) is used.
	/// </summary>
	public class GraphLinkInstance
	{
		/// <summary>
		/// Underlying GraphLink.
		/// </summary>
		public GraphLink GraphLink;

		/// <summary>
		/// Per foot and portion, any special instance types for the GraphLink.
		/// </summary>
		public InstanceStepType[,] InstanceTypes = new InstanceStepType[NumFeet, NumFootPortions];
	}
}
