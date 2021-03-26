namespace StepManiaChartGenerator
{
	/// <summary>
	/// Commons constants and miscellaneous static helpers for constants. 
	/// </summary>
	public static class Constants
	{
		// Feet
		public const int InvalidFoot = -1;
		public const int NumFeet = 2;
		public const int L = 0;
		public const int R = 1;

		// Foot portions
		public const int InvalidFootPortion = -1;
		public const int NumFootPortions = 2;
		public const int DefaultFootPortion = 0;
		public const int Heel = 0;
		public const int Toe = 1;

		// Arrows
		public const int InvalidArrowIndex = -1;
		
		/// <summary>
		/// Gets the other Foot for the given Foot.
		/// </summary>
		/// <param name="foot">Foot. Assumed to be L or R.</param>
		/// <returns>The other Foot.</returns>
		public static int OtherFoot(int foot)
		{
			return foot == L ? R : L;
		}

		/// <summary>
		/// Gets the other foot portion for the given foot portion.
		/// </summary>
		/// <param name="footPortion">Foot portion. Assumed to be Heel or Toe.</param>
		/// <returns>The other foot portion.</returns>
		public static int OtherFootPortion(int footPortion)
		{
			return footPortion == Heel ? Toe : Heel;
		}
	}
}
