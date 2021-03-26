namespace StepManiaChartGenerator
{
	/// <summary>
	/// Costs for ExpressedChart searches.
	/// </summary>
	class ExpressedChartCosts
	{
		public const int CostUnknown = 1000;
		public const int CostRelease = 0;

		// This needs to be great enough such that we prefer one foot holding and
		// the other foot rocking back and forth. If this were low the the held
		// foot does an alternating pattern then the other foot is only going to
		// be hitting the same arrow which has a very low cost.
		public const int CostSameArrow_OtherHoldingNone_ThisHeld_OtherCanStep = 7;
		public const int CostSameArrow = 0;

		public const int CostNewArrow_AllOtherHeld_ThisFootCanBracketToNewArrow = 1;
		public const int CostNewArrow_AllOtherHeld_ThisFootCannotBracketToNewArrow = 2;

		public const int CostNewArrow_BothFeetHolding_OtherCanBracket_DoubleStep = 6;
		public const int CostNewArrow_BothFeetHolding_OtherCanBracket_AlternatingStep = 5;
		public const int CostNewArrow_BothFeetHolding_OtherCannotBracket = 4;

		public const int CostNewArrow_OtherHoldingOne = 3;

		public const int CostNewArrow_OtherHoldingNone_ThisHeld_ThisCannotBracket = 160;
		public const int CostNewArrow_OtherHoldingNone_ThisHeld_OtherCanStep_DoubleStep = 51;   // worse than a mine indicated double step
		public const int CostNewArrow_OtherHoldingNone_ThisHeld_OtherCanStep = 31;  // worse than crossover / inversion
		public const int CostNewArrow_OtherHoldingNone_ThisHeld_OtherCannotStep_DoubleStep = 10;
		public const int CostNewArrow_OtherHoldingNone_ThisHeld_OtherCannotStep = 9;

		public const int CostNewArrow_TripleStep = 200;
		public const int CostNewArrow_DoubleStep = 100;
		public const int CostNewArrow_DoubleStepMineIndicated = 50;
		public const int CostNewArrow_DoubleStepOtherFootReleasedLater = 5;

		public const int CostNewArrow_FirstStep_OtherIsBracketable_ThisIsNotBracketable = 3;
		public const int CostNewArrow_FirstStep_OtherIsNotBracketable_ThisIsBracketable = 1;
		public const int CostNewArrow_FirstStep_OtherCannotStep = 0;
		public const int CostNewArrow_FirstStep_Ambiguous = 2;

		public const int CostNewArrow_Alternating = 0;

		public const int CostNewArrow_StepFromJump_OtherCannotStep = 1;
		public const int CostNewArrow_StepFromJump_OtherCrossover = 1;
		public const int CostNewArrow_StepFromJump_ThisCrossover = 10;
		public const int CostNewArrow_StepFromJump_OtherMineIndicated_ThisNotMineIndicated = 9;
		public const int CostNewArrow_StepFromJump_BothMineIndicated_OtherSooner = 8;
		public const int CostNewArrow_StepFromJump_BothMineIndicated_ThisSooner = 4;
		public const int CostNewArrow_StepFromJump_OtherNotMineIndicated_ThisMineIndicated = 2;
		public const int CostNewArrow_StepFromJump_OtherFootReleasedLater = 2;
		public const int CostNewArrow_StepFromJump_ThisFootReleasedLater = 8;
		public const int CostNewArrow_StepFromJump_OtherFootBracketable_ThisFootNotBracketable = 5;
		public const int CostNewArrow_StepFromJump_OtherFootNotBracketable_ThisFootBracketable = 3;
		public const int CostNewArrow_StepFromJump_Ambiguous = 2;

		public const int CostNewArrow_Crossover_AfterJump = 20;
		public const int CostNewArrow_Crossover_OtherHeld = 3;
		public const int CostNewArrow_Crossover_OtherFree_DoubleStep_MineIndicated = 100;
		public const int CostNewArrow_Crossover_OtherFree_DoubleStep_NoIndication = 200;
		public const int CostNewArrow_Crossover = 4;

		public const int CostNewArrow_FootSwap_DoubleStep_NoMineIndication = 150;
		public const int CostNewArrow_FootSwap_DoubleStep_MineIndication = 75;
		public const int CostNewArrow_FootSwap_OtherHolding = 13;
		public const int CostNewArrow_FootSwap_OtherInBracketPosture = 13;
		public const int CostNewArrow_FootSwap_MineIndicationOnThisFootsArrow = 1;
		public const int CostNewArrow_FootSwap_MineIndicationOnFreeLaneArrow = 2;
		public const int CostNewArrow_FootSwap_SubsequentSwap = 6;
		public const int CostNewArrow_FootSwap_NoIndication_Bracketable = 7;
		public const int CostNewArrow_FootSwap_NoIndication_NotBracketable = 8;

		public const int CostNewArrow_Invert_FromSwap = 200;
		public const int CostNewArrow_Invert_OtherHeld = 5;
		public const int CostNewArrow_Invert_OtherFree_DoubleStep_MineIndicated = 300;
		public const int CostNewArrow_Invert_OtherFree_DoubleStep_NoIndication = 400;
		public const int CostNewArrow_Invert = 6;

		public const int NoBrackets_CostBracket = 1000;

		public const int CostTwoArrows_Bracket_OtherFootHoldingBoth = 1;
		public const int CostTwoArrows_Bracket_DoubleStep = 100;
		public const int CostTwoArrows_Bracket_PreferredDueToMovement = 5;
		public const int CostTwoArrows_Bracket_PreferredDueToMovement_Swap = 8;
		public const int CostTwoArrows_Bracket = 10;
		public const int CostTwoArrows_Bracket_BothSame = 2;
		public const int CostTwoArrows_Bracket_Swap = 11;

		public const int AggressiveBrackets_CostJump_BracketPreferredDueToMovement = 1000;
		public const int AggressiveBrackets_CostJump_OtherFootHoldingOne_ThisFootCouldBracket = 1000;

		public const int CostTwoArrows_Jump_OtherFootHoldingOne_ThisFootCouldBracket = 20;
		public const int CostTwoArrows_Jump_OtherFootHoldingOne_NotBracketable = 15;
		public const int CostTwoArrows_Jump_OneFootPrefersBracketToDueMovement = 7;
		public const int CostTwoArrows_Jump_BothNewAndNeitherBracketable = 12;
		public const int CostTwoArrows_Jump_BothNewAndOneBracketable = 10;
		public const int CostTwoArrows_Jump_Inverted = 8;
		public const int CostTwoArrows_Jump_CrossedOver = 7;
		public const int CostTwoArrows_Jump_BothSame = 2;
		public const int CostTwoArrows_Jump_BothNew = 6;
		public const int CostTwoArrows_Jump_OneNew = 5;

		public const int CostThreeArrows = 0;
		public const int CostFourArrows = 0;
	}
}
