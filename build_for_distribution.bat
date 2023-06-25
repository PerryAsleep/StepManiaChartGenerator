@REM Tools. Hard-coded and not included in repo. Brittle.
set ZIP=C:\Program Files\WinRAR\WinRAR.exe

@IF "%FUMEN_DEVENV%"=="" (
    echo FUMEN_DEVENV is not defined. Please set FUMEN_DEVENV in your environment variables to the path of your devenv.exe executable.
    exit /b
)

@REM Clean and build the solution.
"%FUMEN_DEVENV%" StepManiaChartGenerator.sln /Clean
"%FUMEN_DEVENV%" StepManiaChartGenerator.sln /Build Release

@REM Remove any existing package.
if exist Releases\StepManiaChartGenerator.zip (
    del Releases\StepManiaChartGenerator.zip
)

@REM Copy the config for shipping into the bin directory before packaging.
@REM TODO: Fix this up. Don't need ship config variant. All docs should be included and their paths should be local.
echo F|xcopy /Y /F /I .\StepManiaChartGenerator\StepManiaChartGeneratorConfig-ship.json .\StepManiaChartGenerator\bin\Release\StepManiaChartGeneratorConfig.json
echo F|xcopy /Y /F /I .\StepManiaChartGenerator\README-ship.md .\StepManiaChartGenerator\bin\Release\README.md

@REM Zip the contents of the bin directory into a new package.
chdir StepManiaChartGenerator\bin\Release
"%ZIP%" a -r ..\..\..\Releases\StepManiaChartGenerator.zip *
chdir ..\..\..