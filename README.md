# StepManiaChartGenerator

`StepManiaChartGenerator` is an application for converting [StepMania](https://www.stepmania.com/) charts from one type to another. It generates charts that maintain the qualities of their original charts and feel natural.

It understands technical moves including crossovers, inverted steps (a.k.a Afronova walks), footswaps, brackets, stretch, and double-stepping and it offers many controls for fine-tuning generated charts.

## Supported Layouts

`StepManiaChartGenerator` supports all major dance game single-player layouts.

| ITG/DDR | PIU | SMX |
| ----------- | ----------- | ----------- |
| `dance_single` | `pump_single` | `smx_beginner` |
| `dance_double` | `pump_halfdouble` | `smx_single` |
| `dance_solo` | `pump_double` | `smx_dual` |
| `dance_threepanel` | | `smx_full` |

## Examples

[<img src="StepManiaChartGenerator/docs/Images/conversion-example.png" width="100%"/>](StepManiaChartGenerator/docs/Images/conversion-example.png)
*Screenshots from [GrooveAuthor](https://github.com/PerryAsleep/GrooveAuthor) of a dance-double chart converted to a variety of other chart types.*

[<img src="StepManiaChartGenerator/docs/visualization-example.png" width="100%"/>](https://perryasleep.github.io/StepManiaChartGenerator/StepManiaChartGenerator/docs/Visualizations/(NG%20-%2011)%20Zora/zora-Challenge-sm.html)
*Visualization of a dance-single chart converted to a dance-double chart.*

See the [Examples](StepManiaChartGenerator/docs/Examples.md) page for [Visualizations](StepManiaChartGenerator/docs/Visualizations.md) of how charts are converted and some videos of generated doubles charts being played.

## Installation

`StepManiaChartGenerator` is available for Windows. Download the latest version of `StepManiaChartGenerator.zip` from the [Releases](https://github.com/PerryAsleep/StepManiaChartGenerator/releases) page and extract it to a desired location.

`StepManiaChartGenerator` requires [.Net Runtime 7.0.8](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).

## Configuration

See the [Configuration](StepManiaChartGenerator/docs/Config.md) guide.

## Usage

Double-click `StepManiaChartGenerator.exe`.

## How It Works

See the [How It Works](StepManiaChartGenerator/docs/HowItWorks.md) page.

## Frequently Asked Questions.

See the [Frequently Asked Questions](StepManiaChartGenerator/docs/FAQ.md) page.

## Known Issues and Unsupported Features

See the [Known Issues and Unsupported Features](StepManiaChartGenerator/docs/KnownIssues.md) page.

## Building From Source

Building from source requires Windows 10 or greater and Microsoft Visual Studio Community 2022.

1. Clone the repository and init submodules.
	```
	git clone https://github.com/PerryAsleep/StepManiaChartGenerator.git
	git submodule update --init --recursive
	```
2. If you want to run the `PackageBuild` project, set the following environment variables:
	- Set `FUMEN_DEVENV` to the path of your Visual Studio `devenv.exe` (e.g. `C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe`).
	- Set `FUMEN_7Z` to the path of a [7-Zip](https://www.7-zip.org/) executable (e.g. `C:\Program Files\7-Zip\7z.exe`).
3. Open `StepManiaChartGenerator.sln` and build through Visual Studio.
