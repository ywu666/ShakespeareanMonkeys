set prompt=$G

dotnet run -p Client.csproj < nul
pause

dotnet run -p Client.csproj < tt-test.txt
pause

dotnet run -p Client.csproj -- 8102 < nul
pause
