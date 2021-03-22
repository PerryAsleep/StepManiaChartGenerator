using static StepManiaChartGenerator.Constants;
using System.Text.Json.Serialization;

namespace StepManiaChartGenerator
{
	/// <summary>
	/// Information about an arrow in an array of arrows representing the layout
	/// of one or more pads. This data informs how the other arrows are associated with
	/// this arrow. For example, per arrow, it is useful to know which other arrows
	/// are bracketable with it, are steppable to from it, form crossovers with it, etc.
	/// Deserialized from json.
	/// </summary>
	public class ArrowData
	{
		/// <summary>
		/// The lane / index of this arrow.
		/// Set after deserialization based on index of this ArrowData in the containing array.
		/// </summary>
		public int Lane;

		/// <summary>
		/// X position of the center of this arrow on the pads.
		/// </summary>
		[JsonInclude] public int X;

		/// <summary>
		/// Y position of the center of this arrow on the pads.
		/// </summary>
		[JsonInclude] public int Y;

		/// <summary>
		/// Which arrows are valid as a next step from this arrow for either foot.
		/// Index is arrow.
		/// </summary>
		[JsonInclude] public bool[] ValidNextArrows;

		/// <summary>
		/// Which arrows are bracketable with this arrow for the given foot when the
		/// toes are on this arrow and the heel is on the other arrow.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] BracketablePairingsOtherHeel = new bool[NumFeet][];

		/// <summary>
		/// Which arrows are bracketable with this arrow for the given foot when the
		/// heel is on this arrow and the toes are on the other arrow.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] BracketablePairingsOtherToe = new bool[NumFeet][];

		/// <summary>
		/// Which arrows are valid pairings for the other foot.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot without crossing over.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] OtherFootPairings = new bool[NumFeet][];

		/// <summary>
		/// Which arrows form a front crossover.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot such that Right is crossing over in front.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] OtherFootPairingsOtherFootCrossoverFront = new bool[NumFeet][];

		/// <summary>
		/// Which arrows form a back crossover.
		/// For example, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot such that Right is crossing over in back.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] OtherFootPairingsOtherFootCrossoverBehind = new bool[NumFeet][];

		/// <summary>
		/// Which arrows form an inverted position.
		/// An inverted position is one where if the player stood normally without
		/// twisting their body to face the screen they would be facing completely backwards.
		/// For example, left foot on right and right foot on left.
		/// For this data structure, if the first index is Left, the arrows listed are the valid
		/// positions for the Right foot such that the player is inverted.
		/// While there are two BodyOrientations for being inverted, every inverted position
		/// can be performed with right over left and left over right, so we only need one
		/// data structure.
		/// First index is foot, second is arrow.
		/// </summary>
		[JsonInclude] public bool[][] OtherFootPairingsInverted = new bool[NumFeet][];

		/// <summary>
		/// Approximate distance the foot needs to move from this arrow to the other arrows.
		/// This distance takes into account that Y distances require less movement due to the
		/// length of the foot.
		/// Index is arrow.
		/// </summary>
		public double[] TravelDistanceWithArrow;
	}
}
