using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fumen;
using Fumen.ChartDefinition;
using Fumen.Converters;
using StepManiaLibrary;
using static StepManiaLibrary.Constants;

namespace StepManiaChartGenerator
{
	// TODO: Major cleanup. This class is quick and dirty.
	public class Visualizer
	{
		private const int BannerW = 418;
		private const int BannerH = 164;
		private const int ArrowW = 128;
		private const int HoldCapH = 64;
		private const int TableBorderW = 1;
		private const int ChartColW = 50;
		private const int ChartHeaderH = 20;
		private const int MinesColW = 256;
		private const int CostColW = 64;
		private const int ExpressionColW = 256;

		private const int BeatYSeparation = ArrowW * 2;
		private const int MeasureMarkerH = 10;
		private const int BeatMarkerH = 6;
		private const int ChartTextH = 20;

		private static string VisualizationDir;

		private enum ChartColumns
		{
			TimeSignature,
			BPM,
			Stop,
			Measure
		}

		private enum ExpressionColumns
		{
			Mines,
			Cost,
			LeftFoot,
			RightFoot
		}

		private class ColumnInfo
		{
			public string Name;
			public int X;
			public int Width;
		}

		private static readonly ColumnInfo[] ChartColumnInfo;
		private static readonly ColumnInfo[] ExpressionColumnInfo;
		private static string[] ArrowNames = {"L", "D", "U", "R"};
		private static string[] ArrowClassStrings = {"left", null, "up", "right"};

		private static string[] StepTypeStrings =
		{
			"Same Arrow",
			"New Arrow",
			"Crossover In Front",
			"Crossover In Back",
			"Invert In Front",
			"Invert In Back",
			"Foot Swap",
			"Br H New T New",
			"Br H New T Same",
			"Br H Same T New",
			"Br H Same T Same",
			"Br H Same T Swap",
			"Br H New T Swap",
			"Br H Swap T Same",
			"Br H Swap T New",
			"Br 1 H Same",
			"Br 1 H New",
			"Br 1 T Same",
			"Br 1 T New",
		};

		private StreamWriter StreamWriter;
		private readonly Song Song;
		private readonly Chart OriginalChart;
		private readonly ExpressedChart ExpressedChart;
		private readonly string ExpressedChartConfigName;
		private readonly Chart GeneratedChart;
		private readonly PerformedChart PerformedChart;
		private readonly string PerformedChartConfigName;
		private readonly string SongPath;
		private readonly string SaveFile;
		private readonly string SrcPath;

		private readonly int OriginalChartX = 0;
		private readonly int ExpressedChartX;
		private readonly int GeneratedChartX;

		private double[] LastExpressionPosition = new double[NumFeet];

		static Visualizer()
		{
			var x = 0;
			ChartColumnInfo = new ColumnInfo[Enum.GetNames(typeof(ChartColumns)).Length];
			ChartColumnInfo[(int) ChartColumns.TimeSignature] = new ColumnInfo {Name = "Time", Width = ChartColW, X = x};
			x += ChartColumnInfo[(int) ChartColumns.TimeSignature].Width;
			ChartColumnInfo[(int) ChartColumns.BPM] = new ColumnInfo {Name = "BPM", Width = ChartColW, X = x};
			x += ChartColumnInfo[(int) ChartColumns.BPM].Width;
			ChartColumnInfo[(int) ChartColumns.Stop] = new ColumnInfo {Name = "Stop", Width = ChartColW, X = x};
			x += ChartColumnInfo[(int) ChartColumns.Stop].Width;
			ChartColumnInfo[(int) ChartColumns.Measure] = new ColumnInfo {Name = "Meas", Width = ChartColW, X = x};

			x = 0;
			ExpressionColumnInfo = new ColumnInfo[Enum.GetNames(typeof(ExpressionColumns)).Length];
			ExpressionColumnInfo[(int) ExpressionColumns.Mines] = new ColumnInfo {Name = "Mines", Width = MinesColW, X = x};
			x += ExpressionColumnInfo[(int) ExpressionColumns.Mines].Width;
			ExpressionColumnInfo[(int) ExpressionColumns.Cost] = new ColumnInfo {Name = "Cost", Width = CostColW, X = x};
			x += ExpressionColumnInfo[(int) ExpressionColumns.Cost].Width;
			ExpressionColumnInfo[(int) ExpressionColumns.LeftFoot] =
				new ColumnInfo {Name = "Left Foot", Width = ExpressionColW, X = x};
			x += ExpressionColumnInfo[(int) ExpressionColumns.LeftFoot].Width;
			ExpressionColumnInfo[(int) ExpressionColumns.RightFoot] =
				new ColumnInfo {Name = "Right Foot", Width = ExpressionColW, X = x};
		}

		// TODO: Support more StepsTypes for visualizations.
		public static bool IsStepsTypeSupported(string stepsType)
		{
			return !string.IsNullOrEmpty(stepsType) && stepsType == "dance-single" || stepsType == "dance-double";
		}

		/// <summary>
		/// Creates the directory for writing visualizations and copies source assets to it.
		/// </summary>
		/// <param name="visualizationDir">The directory to use for writing visualizations.</param>
		public static void InitializeVisualizationDir(string visualizationDir)
		{
			VisualizationDir = visualizationDir;

			// Create the directory for writing visualization files.
			Directory.CreateDirectory(VisualizationDir);

			// Copy the src assets to the new directory.
			var sourceDir = Fumen.Path.Combine(new[] {AppDomain.CurrentDomain.BaseDirectory, "html", "src"});
			var targetDir = GetSrcDir();
			Directory.CreateDirectory(targetDir);
			foreach (var file in Directory.GetFiles(sourceDir))
			{
				File.Copy(file, Fumen.Path.Combine(targetDir, System.IO.Path.GetFileName(file)));
			}
		}

		private static string GetSrcDir()
		{
			return Fumen.Path.Combine(VisualizationDir, "src");
		}

		public Visualizer(
			string songPath,
			string saveFile,
			Song song,
			Chart originalChart,
			ExpressedChart expressedChart,
			string expressedChartConfigName,
			PerformedChart performedChart,
			string performedChartConfigName,
			Chart generatedChart)
		{
			SongPath = songPath;
			SaveFile = saveFile;
			Song = song;
			OriginalChart = originalChart;
			ExpressedChart = expressedChart;
			ExpressedChartConfigName = expressedChartConfigName;
			PerformedChart = performedChart;
			PerformedChartConfigName = performedChartConfigName;
			GeneratedChart = generatedChart;

			ExpressedChartX = 0;
			foreach (var chartColInfo in ChartColumnInfo)
				ExpressedChartX += chartColInfo.Width;
			ExpressedChartX += (originalChart.NumInputs * ArrowW);

			GeneratedChartX = ExpressedChartX;
			foreach (var expressedColInfo in ExpressionColumnInfo)
				GeneratedChartX += expressedColInfo.Width;

			if (!SongPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
				SongPath += System.IO.Path.DirectorySeparatorChar.ToString();

			// Determine the relative src path.
			if (string.IsNullOrEmpty(VisualizationDir))
				throw new Exception("VisualizationDir is not set. Set with InitializeVisualizationDir.");
			SrcPath = Fumen.Path.GetRelativePath(SaveFile, GetSrcDir());
			SrcPath = SrcPath.Replace('\\', '/');
			if (!SrcPath.EndsWith("/"))
				SrcPath += "/";
		}

		public void Write()
		{
			// TODO: Async
			using (StreamWriter = new StreamWriter(SaveFile))
			{
				StreamWriter.Write("<!DOCTYPE html>\r\n");
				WriteHead();
				StreamWriter.Write("<html>\r\n\t<body>\r\n");
				WriteTitle();
				WriteChartHeaders();
				StreamWriter.Write($"		<div style=\"position:absolute; top:{BannerH + ChartHeaderH * 2}px;\">\r\n");
				WriteChart(OriginalChart, OriginalChartX, true);
				WriteChart(GeneratedChart, GeneratedChartX, false);
				StreamWriter.Write("		</div>\r\n");
				WriteScript();
				StreamWriter.Write("\t</body>\r\n</html>\r\n");
			}

			StreamWriter = null;
		}

		private void WriteHead()
		{
			StreamWriter.Write(
$@"<head>
	<style>
body {{
  font-family: Arial;
  background: #f3f3f3;
  border: none;
  padding: none;
  margin: 0px;
  margin-block-start: 0.0em;
  margin-block-end: 0.0em;
}}
p {{
	margin-block-start: 0.0em;
	margin-block-end: 0.0em;
	border: none;
	text-align: center;
}}
.left {{
	-webkit-transform:rotate(90deg);
	-moz-transform: rotate(90deg);
	-ms-transform: rotate(90deg);
	-o-transform: rotate(90deg);
	transform: rotate(90deg);
}}
.up {{
	-webkit-transform:rotate(180deg);
	-moz-transform: rotate(180deg);
	-ms-transform: rotate(180deg);
	-o-transform: rotate(180deg);
	transform: rotate(180deg);
}}
.right {{
	-webkit-transform:rotate(270deg);
	-moz-transform: rotate(270deg);
	-ms-transform: rotate(270deg);
	-o-transform: rotate(270deg);
	transform: rotate(270deg);
}}
.leftfoot {{
	content:url(""{SrcPath}l.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{ArrowW}px;
	border:none;
}}
.rightfoot {{
	content:url(""{SrcPath}r.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{ArrowW}px;
	border:none;
}}
.quarter {{
	content:url(""{SrcPath}1_4.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{ArrowW}px;
	border:none;
}}
.eighth {{
	content:url(""{SrcPath}1_8.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{ArrowW}px;
	border:none;
}}
.twelfth {{
	content:url(""{SrcPath}1_12.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{ArrowW}px;
	border:none;
}}
.sixteenth {{
	content:url(""{SrcPath}1_16.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{ArrowW}px;
	border:none;
}}
.twentyfourth {{
	content:url(""{SrcPath}1_24.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{ArrowW}px;
	border:none;
}}
.thirtysecond {{
	content:url(""{SrcPath}1_32.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{ArrowW}px;
	border:none;
}}
.fourtyeighth {{
	content:url(""{SrcPath}1_48.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{ArrowW}px;
	border:none;
}}
.sixtyfourth {{
	content:url(""{SrcPath}1_64.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{ArrowW}px;
	border:none;
}}
.holdcap {{
	content:url(""{SrcPath}hold_cap.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{HoldCapH}px;
	border:none;
}}
.rollcap {{
	content:url(""{SrcPath}roll_cap.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{HoldCapH}px;
	border:none;
}}
.mine {{
	content:url(""{SrcPath}mine.png"");
	position:absolute;
	width:{ArrowW}px;
	height:{ArrowW}px;
	border:none;
}}
.exp_text {{
	position:absolute;
	height:{ChartTextH}px;
	border:none;
	z-index:10
}}
.m_mark {{
	position:absolute;
	height:10px;
	border:none;
	background: #929292;
}}
.b_mark {{
	position:absolute;
	height:6px;
	border:none;
	background: #929292;
}}
#holdbody {{
	border: none;
	position:absolute;
	width:{ArrowW}px;
	padding: none;
	background: url(""{SrcPath}hold.png"");
	background-repeat: repeat;
}}
#rollbody {{
	border: none;
	position:absolute;
	width:{ArrowW}px;
	padding: none;
	background: url(""{SrcPath}roll.png"");
	background-repeat: repeat;
}}
	</style>
</head>
");
		}

		private void WriteTitle()
		{
			var title = Song.Title ?? "";
			var img = "";
			if (!string.IsNullOrEmpty(Song.SongSelectImage))
				img = SongPath + Song.SongSelectImage;
			var subtitle = "";
			if (!string.IsNullOrEmpty(Song.SubTitle))
				subtitle = $@"<span style=""color:#6b6b6b""> {Song.SubTitle}</span>";
			var artist = Song.Artist ?? "";

			StreamWriter.Write(
$@"		<div style=""height: 164px; margin-block-start: 0.0em; margin-block-end: 0.0em;"">
			<img style=""float: left; width: {BannerW}x; height: {BannerH}px; margin-block-start: 0.0em; margin-block-end: 0.0em;"" src=""{img}""/>
			<h1 style=""text-align: left; font-size: 50px; margin-block-start: 0.0em; margin-block-end: 0.0em;"">{title}{subtitle}</h1>
			<h3 style=""text-align: left; font-size: 30px; margin-block-start: 0.0em; margin-block-end: 0.0em;"">{artist}</h3>
		</div>
");
		}

		private void WriteChartHeaders()
		{
			var originalChartWidth = 0;
			var originalChartCols = 0;
			var expressedChartWidth = 0;
			var expressedChartCols = 0;
			var generatedChartWidth = 0;
			var generatedChartCols = 0;

			foreach (var chartCol in ChartColumnInfo)
			{
				originalChartWidth += chartCol.Width;
				originalChartCols++;
				generatedChartWidth += chartCol.Width;
				generatedChartCols++;
			}

			originalChartWidth += (OriginalChart.NumInputs * ArrowW);
			originalChartCols += OriginalChart.NumInputs;
			generatedChartWidth += (GeneratedChart.NumInputs * ArrowW);
			generatedChartCols += GeneratedChart.NumInputs;

			var originalChartNotePercentages = new int[OriginalChart.NumInputs];
			var originalChartTotalSteps = 0;
			foreach (var chartEvent in OriginalChart.Layers[0].Events)
			{
				if (chartEvent is LaneHoldStartNote lhsn)
				{
					originalChartTotalSteps++;
					originalChartNotePercentages[lhsn.Lane]++;
				}
				else if (chartEvent is LaneTapNote ltn)
				{
					originalChartTotalSteps++;
					originalChartNotePercentages[ltn.Lane]++;
				}
			}

			var generatedChartNotePercentages = new int[GeneratedChart.NumInputs];
			var generatedChartTotalSteps = 0;
			foreach (var chartEvent in GeneratedChart.Layers[0].Events)
			{
				if (chartEvent is LaneHoldStartNote lhsn)
				{
					generatedChartTotalSteps++;
					generatedChartNotePercentages[lhsn.Lane]++;
				}
				else if (chartEvent is LaneTapNote ltn)
				{
					generatedChartTotalSteps++;
					generatedChartNotePercentages[ltn.Lane]++;
				}
			}

			foreach (var expressionCol in ExpressionColumnInfo)
			{
				expressedChartWidth += expressionCol.Width;
				expressedChartCols++;
			}

			// PKL - I do not understand why adding one here is necessary for everything to line up.
			var fullWidth = originalChartWidth + expressedChartWidth + generatedChartWidth + 1;

			originalChartWidth -= TableBorderW;
			expressedChartWidth -= TableBorderW;
			generatedChartWidth -= TableBorderW;

			var colH = ChartHeaderH - TableBorderW;

			var originalChartTitle =
				$"{OriginalChart.Type} {OriginalChart.DifficultyType}: Level {OriginalChart.DifficultyRating}";
			if (!string.IsNullOrEmpty(OriginalChart.Description))
				originalChartTitle += $", {OriginalChart.Description} ({originalChartTotalSteps} steps)";

			var generatedChartTitle =
				$"{GeneratedChart.Type} {GeneratedChart.DifficultyType}: Level {GeneratedChart.DifficultyRating}";
			if (!string.IsNullOrEmpty(GeneratedChart.Description))
				generatedChartTitle += $", {GeneratedChart.Description} ({generatedChartTotalSteps} steps)";

			StreamWriter.Write(
$@"		<div id=""chartHeaders"" style=""z-index:10000000; border:none; margin-block-start: 0.0em; margin-block-end: 0.0em;"">
			<table style=""border-collapse: collapse; background: #dbdbdb; width: {fullWidth}px;"">
				<tr>
					<th colspan=""{originalChartCols}"" style=""table-layout: fixed; width: {originalChartWidth}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{originalChartTitle}</th>
					<th colspan=""{expressedChartCols}"" style=""table-layout: fixed; width: {expressedChartWidth}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">Expression [{ExpressedChartConfigName}] (BracketParsingMethod: {ExpressedChart.GetBracketParsingMethod():G})</th>
					<th colspan=""{generatedChartCols}"" style=""table-layout: fixed; width: {generatedChartWidth}x; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{generatedChartTitle}</th>
				</tr>
				<tr>
");

			foreach (var chartCol in ChartColumnInfo)
			{
				StreamWriter.Write(
$@"					<th style=""table-layout: fixed; width: {chartCol.Width - TableBorderW}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{chartCol.Name}</th>
");
			}

			for (var a = 0; a < OriginalChart.NumInputs; a++)
			{
				var percentage = originalChartTotalSteps == 0
					? 0
					: Math.Round(originalChartNotePercentages[a] / (double) originalChartTotalSteps * 100);
				StreamWriter.Write(
$@"					<th style=""table-layout: fixed; width: {ArrowW - TableBorderW}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{ArrowNames[a % 4]} ({percentage}%)</th>
");
			}

			foreach (var expressionCol in ExpressionColumnInfo)
			{
				StreamWriter.Write(
$@"					<th style=""table-layout: fixed; width: {expressionCol.Width - TableBorderW}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{expressionCol.Name}</th>
");
			}

			foreach (var chartCol in ChartColumnInfo)
			{
				StreamWriter.Write(
$@"					<th style=""table-layout: fixed; width: {chartCol.Width - TableBorderW}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{chartCol.Name}</th>
");
			}

			for (var a = 0; a < GeneratedChart.NumInputs; a++)
			{
				var percentage = generatedChartTotalSteps == 0
					? 0
					: Math.Round(generatedChartNotePercentages[a] / (double) generatedChartTotalSteps * 100);
				StreamWriter.Write(
$@"					<th style=""table-layout: fixed; width: {ArrowW - TableBorderW}px; height: {colH}px; padding: 0px; border: {TableBorderW}px solid black"">{ArrowNames[a % 4]} ({percentage}%)</th>
");
			}

			StreamWriter.Write(
@"				</tr>
			</table>
		</div>
");
		}

		private void WriteChart(Chart chart, int chartXPosition, bool originalChart)
		{
			for (var f = 0; f < NumFeet; f++)
				LastExpressionPosition[f] = -1.0;

			var firstLaneX = chartXPosition;
			foreach (var chartCol in ChartColumnInfo)
				firstLaneX += chartCol.Width;

			var previousTimeSignatureIntegerPosition = 0;
			var previousTimeSignatureY = ArrowW * 0.5;
			var previousTimeSignatureMeasure = 0;
			var currentTimeSignature = new Fraction(SMCommon.NumBeatsPerMeasure, SMCommon.NumBeatsPerMeasure);
			var yPerBeat = (double) BeatYSeparation;

			var lastHoldStarts = new int[chart.NumInputs];
			var lastHoldWasRoll = new bool[chart.NumInputs];

			var currentExpressedChartSearchNode = ExpressedChart.GetRootSearchNode();
			var currentPerformedChartNode = PerformedChart.GetRootNodes()[0];
			var currentExpressedMineIndex = 0;

			foreach (var chartEvent in chart.Layers[0].Events)
			{
				double eventY = ArrowW * 0.5 + ((double)chartEvent.IntegerPosition / SMCommon.MaxValidDenominator) * BeatYSeparation;

				while (currentExpressedChartSearchNode != null && currentExpressedChartSearchNode.Position < chartEvent.IntegerPosition)
					currentExpressedChartSearchNode = currentExpressedChartSearchNode.GetNextNode();
				while (currentPerformedChartNode != null && currentPerformedChartNode.Position < chartEvent.IntegerPosition)
					currentPerformedChartNode = currentPerformedChartNode.Next;
				while (currentExpressedMineIndex < ExpressedChart.MineEvents.Count
				       && ExpressedChart.MineEvents[currentExpressedMineIndex].Position < chartEvent.IntegerPosition)
					currentExpressedMineIndex++;

				if (chartEvent is TimeSignature ts)
				{
					// Write measure markers up until this time signature change

					var numMeasures = (ts.IntegerPosition - previousTimeSignatureIntegerPosition)
						/ ((currentTimeSignature.Numerator * SMCommon.MaxValidDenominator * SMCommon.NumBeatsPerMeasure) / currentTimeSignature.Denominator);

					WriteMeasures(
						chartXPosition,
						previousTimeSignatureY,
						yPerBeat,
						currentTimeSignature,
						previousTimeSignatureMeasure,
						numMeasures,
						chart.NumInputs);

					// Update time signature tracking
					previousTimeSignatureY = eventY;
					currentTimeSignature = ts.Signature;
					yPerBeat = BeatYSeparation * ((double)SMCommon.NumBeatsPerMeasure / currentTimeSignature.Denominator);
					previousTimeSignatureMeasure += numMeasures;
					previousTimeSignatureIntegerPosition = ts.IntegerPosition;
				}

				// Chart column values. Excluding measures which are handled in WriteMeasures.
				var colX = chartXPosition;
				var colW = ChartColumnInfo[(int) ChartColumns.TimeSignature].Width;
				var colY = (int) (eventY - ChartTextH * .5);
				string colVal = null;
				if (chartEvent is TimeSignature tse)
				{
					colVal = $"{tse.Signature.Numerator}/{tse.Signature.Denominator}";
					colX += ChartColumnInfo[(int) ChartColumns.TimeSignature].X;
				}
				else if (chartEvent is Stop stop)
				{
					colVal = $"{stop.LengthSeconds}";
					colX += ChartColumnInfo[(int) ChartColumns.Stop].X;
				}
				else if (chartEvent is Tempo tc)
				{
					colVal = $"{tc.TempoBPM}";
					colX += ChartColumnInfo[(int) ChartColumns.BPM].X;
				}

				if (colVal != null)
				{
					StreamWriter.Write(
$@"			<p class=""exp_text"" style=""top:{colY}px; left:{colX}px; width:{colW}px;"">{colVal}</p>
");
				}

				// Arrows
				if (chartEvent is LaneTapNote ltn)
				{
					if (originalChart)
						WriteExpression(currentExpressedChartSearchNode, eventY);

					var foot = InvalidFoot;
					if (originalChart)
						foot = GetFootForArrow(ltn.Lane, ltn.IntegerPosition, currentExpressedChartSearchNode);
					else
						foot = GetFootForArrow(ltn.Lane, ltn.IntegerPosition, currentPerformedChartNode);
					WriteArrow(ltn.Lane, foot, firstLaneX, eventY, ltn.IntegerPosition);
				}
				else if (chartEvent is LaneHoldStartNote lhsn)
				{
					if (originalChart)
						WriteExpression(currentExpressedChartSearchNode, eventY);

					var foot = InvalidFoot;
					if (originalChart)
						foot = GetFootForArrow(lhsn.Lane, lhsn.IntegerPosition, currentExpressedChartSearchNode);
					else
						foot = GetFootForArrow(lhsn.Lane, lhsn.IntegerPosition, currentPerformedChartNode);
					WriteArrow(lhsn.Lane, foot, firstLaneX, eventY, lhsn.IntegerPosition);

					lastHoldStarts[lhsn.Lane] = (int) eventY;
					lastHoldWasRoll[lhsn.Lane] =
						lhsn.SourceType == SMCommon.NoteChars[(int) SMCommon.NoteType.RollStart].ToString();
				}
				else if (chartEvent is LaneHoldEndNote lhen)
				{
					if (originalChart)
						WriteExpression(currentExpressedChartSearchNode, eventY);

					WriteHold(lhen.Lane, firstLaneX, lastHoldStarts[lhen.Lane], eventY, lastHoldWasRoll[lhen.Lane]);
				}
				else if (chartEvent is LaneNote ln)
				{
					if (ln.SourceType == SMCommon.NoteChars[(int) SMCommon.NoteType.Mine].ToString())
					{
						// Write any expressed mine events for mines at this position.
						if (originalChart)
						{
							var mineEvents = new List<ExpressedChart.MineEvent>();
							while (currentExpressedMineIndex < ExpressedChart.MineEvents.Count
							       && ExpressedChart.MineEvents[currentExpressedMineIndex].Position <= chartEvent.IntegerPosition)
							{
								mineEvents.Add(ExpressedChart.MineEvents[currentExpressedMineIndex]);
								currentExpressedMineIndex++;
							}

							if (mineEvents.Count > 0)
							{
								mineEvents = mineEvents.OrderBy(m => m.OriginalArrow).ToList();
							}

							WriteExpressedMines(mineEvents, eventY);
						}

						// Write the mine.
						WriteMine(ln.Lane, firstLaneX, eventY);
					}
				}
			}

			// Write the final measure markers
			var numMeasuresToWrite =
				(chart.Layers[0].Events[chart.Layers[0].Events.Count - 1].IntegerPosition - previousTimeSignatureIntegerPosition) 
				/ (currentTimeSignature.Numerator * (SMCommon.MaxValidDenominator * (SMCommon.NumBeatsPerMeasure / currentTimeSignature.Denominator)))
				+ 1;
			WriteMeasures(
				chartXPosition,
				previousTimeSignatureY,
				yPerBeat,
				currentTimeSignature,
				previousTimeSignatureMeasure,
				numMeasuresToWrite,
				chart.NumInputs);
		}

		private void WriteExpressedMines(List<ExpressedChart.MineEvent> mines, double y)
		{
			if (mines == null || mines.Count == 0)
				return;

			var mineSB = new StringBuilder();
			var first = true;
			foreach (var mine in mines)
			{
				if (!first)
					mineSB.Append("<br>");

				mineSB.Append($"{ArrowNames[mine.OriginalArrow]}: ");
				switch (mine.Type)
				{
					case MineType.NoArrow:
						mineSB.Append("No Association");
						break;
					case MineType.AfterArrow:
					{
						mineSB.Append($"After {FormatSequential(mine.ArrowIsNthClosest + 1)} Most Recent ");
						break;
					}
					case MineType.BeforeArrow:
					{
						mineSB.Append($"Before {FormatSequential(mine.ArrowIsNthClosest + 1)} Next ");
						break;
					}
				}

				if (mine.FootAssociatedWithPairedNote == L)
					mineSB.Append("(Foot: L)");
				else if (mine.FootAssociatedWithPairedNote == R)
					mineSB.Append("(Foot: R)");
				first = false;
			}

			var minesStr = mineSB.ToString();

			var x = ExpressedChartX + ExpressionColumnInfo[(int) ExpressionColumns.Mines].X;
			var h = ChartTextH * mines.Count;
			StreamWriter.Write(
$@"			<p class=""exp_text"" style=""top:{(int)(y - h * .5)}px; left:{x}px; width:{MinesColW}px; height:{h}px;"">{minesStr}</p>
");
		}

		private string FormatSequential(int n)
		{
			string suffix;
			switch (n % 100)
			{
				case 11: suffix = "th"; break;
				case 12: suffix = "th"; break;
				case 13: suffix = "th"; break;
				default:
				{
					switch (n % 10)
					{
						case 1: suffix = "st"; break;
						case 2: suffix = "nd"; break;
						case 3: suffix = "rd"; break;
						default: suffix = "th"; break;
					}

					break;
				}
			}

			return n + suffix;
		}

		private void WriteExpression(ExpressedChart.ChartSearchNode node, double y)
		{
			if (node == null)
				return;
			var position = node.Position;
			while (node != null && node.Position == position)
			{
				if (node.PreviousLink != null && !node.PreviousLink.GraphLink.IsRelease())
				{
					var writeCost = false;
					for (var p = 0; p < NumFootPortions; p++)
					{
						// Left Foot
						if (LastExpressionPosition[L] < y && node.PreviousLink.GraphLink.Links[L, p].Valid)
						{
							var leftX = ExpressedChartX + ExpressionColumnInfo[(int) ExpressionColumns.LeftFoot].X;
							var stepStr = StepTypeStrings[(int) node.PreviousLink.GraphLink.Links[L, p].Step];
							if (node.PreviousLink.GraphLink.IsJump())
								stepStr = "[Jump] " + stepStr;
							StreamWriter.Write(
$@"			<p class=""exp_text"" style=""top:{(int)(y - ChartTextH * .5)}px; left:{leftX}px; width:{ExpressionColW}px;"">{stepStr}</p>
");
							writeCost = true;
							LastExpressionPosition[L] = y;
						}

						// Right Foot
						if (LastExpressionPosition[R] < y && node.PreviousLink.GraphLink.Links[R, p].Valid)
						{
							var rightX = ExpressedChartX + ExpressionColumnInfo[(int) ExpressionColumns.RightFoot].X;
							var stepStr = StepTypeStrings[(int) node.PreviousLink.GraphLink.Links[R, p].Step];
							if (node.PreviousLink.GraphLink.IsJump())
								stepStr = "[Jump] " + stepStr;
							StreamWriter.Write(
$@"			<p class=""exp_text"" style=""top:{(int)(y - ChartTextH * .5)}px; left:{rightX}px; width:{ExpressionColW}px;"">{stepStr}</p>
");
							writeCost = true;
							LastExpressionPosition[R] = y;
						}
					}

					// Cost
					if (writeCost)
					{
						var costX = ExpressedChartX + ExpressionColumnInfo[(int) ExpressionColumns.Cost].X;
						StreamWriter.Write(
$@"			<p class=""exp_text"" style=""top:{(int) (y - ChartTextH * .5)}px; left:{costX}px; width:{CostColW}px;"">{node.Cost}</p>
");
					}
				}

				node = node.GetNextNode();
			}
		}

		private void WriteMine(int arrow, int firstLaneX, double y)
		{
			var x = firstLaneX + (arrow * ArrowW);

			StreamWriter.Write(
$@"			<img class=""mine"" style=""top:{(int)(y - ArrowW * 0.5)}px; left:{x}px; z-index:{(int)y};""/>
");
		}

		private int GetFootForArrow(int arrow, int position, ExpressedChart.ChartSearchNode node)
		{
			while (node != null && node.Position == position)
			{
				if (node.PreviousLink != null && !node.PreviousLink.GraphLink.IsRelease())
				{
					// If this step is a footswap we need to ignore the other foot, which may still be resting on this arrow.
					var previousStepLink = node.GetPreviousStepLink();
					var footSwapFoot = InvalidFoot;
					var footSwapPortion = DefaultFootPortion;
					var footSwap = previousStepLink?.GraphLink.IsFootSwap(out footSwapFoot, out footSwapPortion) ?? false;
					if (footSwap && node.GraphNode.State[footSwapFoot, footSwapPortion].Arrow == arrow)
						return footSwapFoot;

					// No footswap on the given arrow.
					for (var f = 0; f < NumFeet; f++)
					{
						for (var p = 0; p < NumFootPortions; p++)
						{
							if (node.GraphNode.State[f, p].Arrow == arrow)
							{
								return f;
							}
						}
					}
				}

				node = node.GetNextNode();
			}

			return InvalidFoot;
		}

		private int GetFootForArrow(int arrow, int position, PerformedChart.PerformanceNode node)
		{
			while (node != null && node.Position == position)
			{
				if (node is PerformedChart.StepPerformanceNode spn)
				{
					var previousStepLink = spn.GraphLinkInstance;
					if (previousStepLink != null && !previousStepLink.GraphLink.IsRelease())
					{
						// If this step is a footswap we need to ignore the other foot, which may still be resting on this arrow.
						var footSwap = previousStepLink.GraphLink.IsFootSwap(out var footSwapFoot, out var footSwapPortion);
						if (footSwap && spn.GraphNodeInstance.Node.State[footSwapFoot, footSwapPortion].Arrow == arrow)
							return footSwapFoot;

						// No footswap on the given arrow.
						for (var f = 0; f < NumFeet; f++)
						{
							for (var p = 0; p < NumFootPortions; p++)
							{
								if (spn.GraphNodeInstance.Node.State[f, p].Arrow == arrow)
								{
									return f;
								}
							}
						}
					}
				}

				node = node.Next;
			}

			return InvalidFoot;
		}

		private void WriteArrow(int arrow, int foot, int firstLaneX, double y, int integerPosition)
		{
			var rotClass = ArrowClassStrings[arrow % 4];
			var x = firstLaneX + arrow * ArrowW;
			var fraction = new Fraction(integerPosition % SMCommon.MaxValidDenominator, SMCommon.MaxValidDenominator).Reduce();

			string imgClass;
			switch (fraction.Denominator)
			{
				case 0:
				case 1: imgClass = "quarter"; break;
				case 2: imgClass = "eighth"; break;
				case 3: imgClass = "twelfth"; break;
				case 4: imgClass = "sixteenth"; break;
				case 6: imgClass = "twentyfourth"; break;
				case 8: imgClass = "thirtysecond"; break;
				case 12: imgClass = "fourtyeighth"; break;
				default: imgClass = "sixtyfourth"; break;
			}

			string classStr;
			if (rotClass != null)
				classStr = $"{imgClass} {rotClass}";
			else
				classStr = $"{imgClass}";

			// Arrow
			StreamWriter.Write(
$@"			<img class=""{classStr}"" style=""top:{(int)(y - ArrowW * 0.5)}px; left:{x}px; z-index:{(int)y};""/>
");
			// Foot indicator
			if (foot != InvalidFoot)
			{
				var footClass = foot == L ? "leftfoot" : "rightfoot";
				StreamWriter.Write(
$@"			<img class=""{footClass}"" style=""top:{(int)(y - ArrowW * 0.5)}px; left:{x}px; z-index:{(int)y};""/>
");
			}
		}

		private void WriteHold(int arrow, int firstLaneX, double startY, double endY, bool roll)
		{
			var id = roll ? "rollbody" : "holdbody";
			var cap = roll ? "rollcap" : "holdcap";
			var x = firstLaneX + arrow * ArrowW;

			StreamWriter.Write(
$@"			<div id=""{id}"" style=""top:{(int)startY}px; left:{x}px; height:{(int)(endY - startY)}px; z-index:{(int)startY - 1};""></div>
");

			StreamWriter.Write(
$@"			<img class=""{cap}"" style=""top:{(int)endY}px; left:{x}px; z-index:{(int)startY - 1};""/>
");
		}

		private void WriteMeasures(int x, double startY, double yPerBeat, Fraction timeSignature, int currentMeasure, int numMeasures, int numArrows)
		{
			var barX = x;
			foreach (var chartCol in ChartColumnInfo)
				barX += chartCol.Width;
			var barW = numArrows * ArrowW;

			var mmX = x + ChartColumnInfo[(int) ChartColumns.Measure].X;
			var mmW = ChartColumnInfo[(int) ChartColumns.Measure].Width;
			var mmH = ChartTextH;

			for (var m = 0; m < numMeasures; m++)
			{
				for (var b = 0; b < timeSignature.Numerator; b++)
				{
					var y = startY + (m * timeSignature.Numerator + b) * yPerBeat;
					string classStr;
					if (b == 0)
					{
						// Write measure number
						StreamWriter.Write(
$@"			<p class=""exp_text"" style=""top:{(int)(y - mmH * .5)}px; left:{mmX}px; width:{mmW}px;"">{currentMeasure + m}</p>
");
						y -= MeasureMarkerH * 0.5;
						classStr = "m_mark";
					}
					else
					{
						y -= BeatMarkerH * 0.5;
						classStr = "b_mark";
					}

					// Write measure / beat marker
					StreamWriter.Write(
$@"			<div class=""{classStr}"" style=""top:{(int)y}px; left:{barX}px; width:{barW}px;""></div>
");
				}
			}
		}

		private void WriteScript()
		{
			StreamWriter.Write(
$@"		<script>
			window.onscroll = function() {{ updateSticky() }};
			var chartHeaders = document.getElementById(""chartHeaders"");
			function updateSticky()
			{{
				if (window.pageYOffset >= {BannerH})
				{{
					chartHeaders.style.position = ""fixed"";
				}}
				else
				{{
					chartHeaders.style.position = ""static"";
				}}
				chartHeaders.style.top = ""0px"";
				chartHeaders.style.left = `${{-window.pageXOffset}}px`;
			}}
		</script>
");
		}
	}
}
