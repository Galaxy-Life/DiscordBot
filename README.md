# Galaxy Life Discord Bot

This is the official Discord bot for Galaxy Life, based on [svr333](https://github.com/svr333) advanced discord bot template.<br/>
Check out players and alliances profiles, servers status, leaderboards and more!

We also use it internally for moderation purposes.<br/>
It uses our official .NET API Wrapper, available on [NuGet](https://www.nuget.org/packages/gl.net/), feel free to check it out.

## Prerequisites

- [.NET 7.0](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- [Visual Studio](https://visualstudio.microsoft.com/) (or [Visual Studio Code](https://code.visualstudio.com)).

## Running Locally

You must have created a discord application on discord [developer portal](https://discord.com/developers/applications).<br/>
Then, set up an environement variable named `Token`, and assign it your discord bot's token.<br />

#### Visual Studio

First set `AdvancedBot.Console` as your startup project.<br/>
Right-click the solution and select `Set as your Startup Project`, or use `CTRL + ALT + P` to open the project properties and then set your startup project here.
- Press `F5` to run the application in debug mode.

#### Visual Studio Code

The `.vscode/` folder contains all necessary configuration files for you to run the application easily.
- Press `F5` to start the application in debug mode.
