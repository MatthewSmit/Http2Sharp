dotnet D:\Applications\SonarQube-Scanner-Core\SonarScanner.MSBuild.dll begin /k:"Http2Sharp" /d:sonar.cs.dotcover.reportsPaths="%CD%\dotCover.html"
dotnet build
D:\Applications\SonarQube-Scanner-Core\DotCover\dotCover.exe analyse coverage.xml
dotnet D:\Applications\SonarQube-Scanner-Core\SonarScanner.MSBuild.dll end