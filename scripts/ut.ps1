Set-Location ..

# Install the ReportGenerator tool
Write-Output "Installing ReportGenerator tool..."
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run unit tests and generate coverage report
Write-Output "Running unit tests and generating coverage report..."
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./TestResults/coverage.opencover.xml

# Generate HTML report
$coverageFiles = Get-ChildItem -Path . -Recurse -Filter "coverage.cobertura.xml"
$coverageFiles | ForEach-Object { Write-Output $_.FullName }
$reportPaths = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
reportgenerator "-reports:$reportPaths" "-targetdir:coveragereport" -reporttypes:Html

# Check that the coverage report files exist
Write-Output "Coverage report generated successfully."
Start-Process "coveragereport/index.htm"