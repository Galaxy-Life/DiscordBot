cd ..
git pull
dotnet build src/AdvancedBot.Console/AdvancedBot.Console.csproj -c Release
start "Discord Bot" src/AdvancedBot.Console/bin/Release/net8.0/AdvancedBot.Console.exe
