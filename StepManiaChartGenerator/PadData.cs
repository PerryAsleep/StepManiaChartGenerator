using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fumen;
using static StepManiaChartGenerator.Constants;

namespace StepManiaChartGenerator
{
	/// <summary>
	/// Information about how a pad or set of pads is laid out.
	/// Deserialized from json.
	/// </summary>
	public class PadData
	{
		/// <summary>
		/// Tag for logging messages.
		/// </summary>
		private const string LogTag = "PadData";

		/// <summary>
		/// Stepmania StepsType that this PadData is used for.
		/// </summary>
		public string StepsType;

		/// <summary>
		/// Number of arrows / lanes for this PadData.
		/// Set after deserialization.
		/// </summary>
		public int NumArrows;

		/// <summary>
		/// Maximum difference in indices between arrows of a bracket in any ArrowData[] array.
		/// Used to improve scans over ArrowData for bracketable pairings.
		/// Set after deserialization.
		/// </summary>
		public int MaxBracketSeparation;

		/// <summary>
		/// Valid starting positions.
		/// First array is tier, with lower indices preferred to higher indices when choosing a start position.
		/// Second array is all equally preferred positions of the same tier.
		/// Third array is lane indices, one for each foot.
		/// </summary>
		[JsonInclude] public int[][][] StartingPositions;

		/// <summary>
		/// Data for each arrow on the pad.
		/// </summary>
		[JsonInclude] public ArrowData[] ArrowData;

		/// <summary>
		/// When the foot travels in Y it needs to travel less than when it travels in X
		/// since the foot is longer than it is wide. Movements in Y are substantially shorter
		/// as a result. This value is subtracted from Y deltas when computing TravelDistanceWithArrow.
		/// values.
		/// </summary>
		[JsonInclude] public double YTravelDistanceCompensation = 0.5;

		/// <summary>
		/// Loads the PadData from the given file.
		/// </summary>
		/// <param name="stepsType">Stepmania StepsType for the PadData.</param>
		/// <param name="fileName">Filename to load.</param>
		/// <returns>PadData Instance or null if it failed to load or did contained invalid data.</returns>
		public static async Task<PadData> LoadPadData(string stepsType, string fileName)
		{
			var options = new JsonSerializerOptions
			{
				Converters =
				{
					new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
				},
				ReadCommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true,
				IncludeFields = true,
			};

			PadData padData;
			try
			{
				var fileFileName = Fumen.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
				if (!File.Exists(fileFileName))
				{
					LogErrorStatic($"[{stepsType}] Could not find PadData file {fileName}. Please create a PadData file for {stepsType}.");
					return null;
				}

				using (FileStream openStream = File.OpenRead(fileFileName))
				{
					padData = await JsonSerializer.DeserializeAsync<PadData>(openStream, options);
					if (padData == null)
						throw new Exception("Null PadData.");
					padData.StepsType = stepsType;
					if (!padData.Validate())
						return null;
					padData.SetTravelDistances();
					padData.DetermineMaxBracketSeparation();
				}
			}
			catch (Exception e)
			{
				LogErrorStatic($"[{stepsType}] Failed to load {fileName}. {e}");
				return null;
			}

			return padData;
		}

		/// <summary>
		/// Validates the PadData, logging errors on unexpected data.
		/// Side-effects of setting NumArrows on PadData and Lane on each ArrowData.
		/// </summary>
		/// <returns>True of no errors were found and false otherwise.</returns>
		private bool Validate()
		{
			var errors = false;
			if (ArrowData == null)
			{
				LogError("Null ArrowData. Expected an array.");
				return true;
			}

			NumArrows = ArrowData.Length;
			if (NumArrows < 1)
			{
				LogError("Empty ArrowData. Expected an array.");
				errors = true;
			}

			var lane = 0;
			foreach (var arrowData in ArrowData)
			{
				arrowData.Lane = lane;
				lane++;

				if (arrowData.ValidNextArrows == null || arrowData.ValidNextArrows.Length != NumArrows)
				{
					LogError($"Lane {lane}: {arrowData.ValidNextArrows?.Length ?? 0} ValidNextArrows entries found. Expected {NumArrows}.");
					errors = true;
				}

				errors |= !ValidateArrowDataArrays(arrowData.BracketablePairingsOtherHeel, lane, "BracketablePairingsOtherHeel");
				errors |= !ValidateArrowDataArrays(arrowData.BracketablePairingsOtherToe, lane, "BracketablePairingsOtherToe");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairings, lane, "OtherFootPairings");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairingsOtherFootCrossoverFront, lane, "OtherFootPairingsOtherFootCrossoverFront");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairingsOtherFootCrossoverBehind, lane, "OtherFootPairingsOtherFootCrossoverBehind");
				errors |= !ValidateArrowDataArrays(arrowData.OtherFootPairingsInverted, lane, "OtherFootPairingsInverted");
			}

			if (StartingPositions == null || StartingPositions.Length < 1)
			{
				LogError("Empty StartingPositions. At least one valid starting position is required.");
				errors = true;
			}
			else
			{
				var tierIndex = 0;
				foreach (var tier in StartingPositions)
				{
					if (tier == null || tier.Length < 1)
					{
						LogError($"Empty array at StartingPositions[{tierIndex}].");
						errors = true;
					}
					else
					{
						if (tierIndex == 0 && tier.Length != 1)
						{
							LogError($"The first tier of StartingPositions has {tier.Length} entries. It should only have 1.");
							errors = true;
						}

						var positionIndex = 0;
						foreach (var position in tier)
						{
							if (position == null || position.Length != NumFeet)
							{
								LogError($"StartingPositions[{tierIndex}][{positionIndex}] {position?.Length ?? 0} entries found. Expected {NumFeet}, one for each foot.");
								errors = true;
							}
							else
							{
								var laneIndex = 0;
								foreach (var spLane in position)
								{
									if (spLane < 0 || spLane >= NumArrows)
									{
										LogError($"StartingPositions[{tierIndex}][{positionIndex}][{laneIndex}] lane {spLane} out of bounds Must be within [0, {NumArrows-1}].");
										errors = true;
									}
									laneIndex++;
								}
							}
							positionIndex++;
						}
					}
					tierIndex++;
				}
			}

			return !errors;
		}

		/// <summary>
		/// Helper method for Validate to validate one of the many bool[][] arrays on ArrowData.
		/// </summary>
		/// <param name="arrowDataArray">Array from ArrowData. First index is foot, second is lane.</param>
		/// <param name="lane">Lane of the ArrowData. Used for logging.</param>
		/// <param name="name">Name of the bool[][] property. Used for logging.</param>
		/// <returns>True of no errors were found and false otherwise.</returns>
		private bool ValidateArrowDataArrays(bool[][] arrowDataArray, int lane, string name)
		{
			var errors = false;
			if (arrowDataArray == null || arrowDataArray.Length != NumFeet)
			{
				LogError($"Lane {lane}: {arrowDataArray?.Length ?? 0} {name} entries found. Expected {NumFeet} arrays, one for each foot.");
				errors = true;
			}
			else
			{
				for (var f = 0; f < NumFeet; f++)
				{
					if (arrowDataArray[f] == null || arrowDataArray[f].Length != NumArrows)
					{
						LogError($"Lane {lane}: {name}[{f}] {arrowDataArray[f]?.Length ?? 0} entries found. Expected {NumArrows}.");
						errors = true;
					}
				}
			}
			return !errors;
		}

		/// <summary>
		/// Sets the TravelDistanceWithArrow values for each ArrowData in the given array
		/// based off of the X and Y positions of the other ArrowData entries.
		/// </summary>
		private void SetTravelDistances()
		{
			foreach (var data in ArrowData)
			{
				data.TravelDistanceWithArrow = new double[ArrowData.Length];
				foreach (var otherData in ArrowData)
				{
					var dx = data.X - otherData.X;
					// Subtract YTravelDistanceCompensation from y since the heel and toe make forward
					// and backward movements shorter.
					var dy = Math.Max(0.0, Math.Abs(data.Y - otherData.Y) - YTravelDistanceCompensation);
					var d = Math.Sqrt(dx * dx + dy * dy);
					data.TravelDistanceWithArrow[otherData.Lane] = d;
				}
			}
		}

		/// <summary>
		/// Determines and caches the maximum number arrow indices which separate
		/// arrows in a bracket on this PadData. Used for improving performance when generating
		/// a StepGraph with this PadData.
		/// </summary>
		private void DetermineMaxBracketSeparation()
		{
			var numArrows = ArrowData.Length;
			for (var a = 0; a < numArrows; a++)
			{
				for (var a2 = 0; a2 < numArrows; a2++)
				{
					for (var f = 0; f < NumFeet; f++)
					{
						if (ArrowData[a].BracketablePairingsOtherHeel[f][a2]
							|| ArrowData[a].BracketablePairingsOtherToe[f][a2])
							MaxBracketSeparation = Math.Max(MaxBracketSeparation, Math.Abs(a2 - a));
					}
				}
			}
		}

		#region Logging
		private void LogError(string message)
		{
			LogErrorStatic($"[{LogTag}] [{StepsType}] {message}");
		}

		private void LogWarn(string message)
		{
			LogWarnStatic($"[{LogTag}] [{StepsType}] {message}");
		}

		private void LogInfo(string message)
		{
			LogInfoStatic($"[{LogTag}] [{StepsType}] {message}");
		}

		private static void LogErrorStatic(string message)
		{
			Logger.Error($"[{LogTag}] {message}");
		}

		private static void LogWarnStatic(string message)
		{
			Logger.Warn($"[{LogTag}] {message}");
		}

		private static void LogInfoStatic(string message)
		{
			Logger.Info($"[{LogTag}] {message}");
		}
		#endregion Logging
	}
}
