using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using StepManiaChartGenerator;
using System.Collections.Generic;
using Fumen.ChartDefinition;

namespace StepManiaChartGeneratorTests
{
	[TestClass]
	public class TestProgram
	{
		/// <summary>
		/// Test the program version parsing behavior from Chart descriptions.
		/// </summary>
		[TestMethod]
		public void TestVersionParsing()
		{
			var cases = new List<Tuple<string, Fumen.SemanticVersion, bool>>
			{
				new Tuple<string, Fumen.SemanticVersion, bool>(null, new Fumen.SemanticVersion(), false),
				new Tuple<string, Fumen.SemanticVersion, bool>("", new Fumen.SemanticVersion(), false),
				new Tuple<string, Fumen.SemanticVersion, bool>("test_no_version", new Fumen.SemanticVersion(), false),
				new Tuple<string, Fumen.SemanticVersion, bool>("test_version_short_[FG v1]", new Fumen.SemanticVersion(), false),
				new Tuple<string, Fumen.SemanticVersion, bool>("test_version_short_[FG v1.]", new Fumen.SemanticVersion(), false),
				new Tuple<string, Fumen.SemanticVersion, bool>("test_version_long_[FG v1.0.0.1]", new Fumen.SemanticVersion(),
					false),
				new Tuple<string, Fumen.SemanticVersion, bool>("test_deprecated_end[FG v1.2]", new Fumen.SemanticVersion(),
					false),
				new Tuple<string, Fumen.SemanticVersion, bool>("test_semantic_end[FG v1.2.3]", new Fumen.SemanticVersion(),
					false),
				new Tuple<string, Fumen.SemanticVersion, bool>("[FG v1.2]test_valid_deprecated_start",
					new Fumen.SemanticVersion(1, 2, 0), true),
				new Tuple<string, Fumen.SemanticVersion, bool>("[FG v1.2.3]test_valid_semantic_start",
					new Fumen.SemanticVersion(1, 2, 3), true),
				new Tuple<string, Fumen.SemanticVersion, bool>("[FG v001.002]test_zeros_deprecated",
					new Fumen.SemanticVersion(1, 2, 0), true),
				new Tuple<string, Fumen.SemanticVersion, bool>("[FG v001.002.003]test_zeros_semantic",
					new Fumen.SemanticVersion(1, 2, 3), true),
				new Tuple<string, Fumen.SemanticVersion, bool>("[FG v1.2]", new Fumen.SemanticVersion(1, 2, 0), true),
				new Tuple<string, Fumen.SemanticVersion, bool>("[FG v1.2.3]", new Fumen.SemanticVersion(1, 2, 3), true),
				new Tuple<string, Fumen.SemanticVersion, bool>("[FG v1.2] [FG v9.8]", new Fumen.SemanticVersion(1, 2, 0), true),
				new Tuple<string, Fumen.SemanticVersion, bool>("[FG v1.2.3] [FG v9.8.7]", new Fumen.SemanticVersion(1, 2, 3),
					true),
			};

			foreach (var testCase in cases)
			{
				var c = new Chart
				{
					Description = testCase.Item1,
				};
				var validVersion = Program.GetFumenGeneratedVersion(c, out var actualVersion);
				Assert.AreEqual(testCase.Item2, actualVersion);
				Assert.AreEqual(testCase.Item3, validVersion);
			}
		}
	}
}
