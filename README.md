# tetr15

[![Made with C#](https://img.shields.io/badge/Made%20with-CS-blue)](https://en.wikipedia.org/wiki/C_Sharp_(programming_language))

Tetris on console, using pure C#!

Featuring ghost pieces, SRS and an incredibly ugly menu.

# Build and run

**1. Clone the repository**
```
git clone https://github.com/R-Bet/tetr15
```

Or without the history, which you don't need to run the program:
```
git clone https://github.com/R-Bet/tetr15 --depth 1
```

**2. Go into the project directory**
```
cd tetr15
```

**3. Build**
```
dotnet publish -c Release -o publish -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true
```

**4. Run**

The .exe will be generated at /publish/


_or just get the prebuilt from releases `¯\_(ツ)_/¯`_

# Controls

1. Use numbers and arrows to interact with the menu.

2. Use Left & Right / A and D keys to move horizontally.

3. Use the W key to rotate clockwise.

4. Use the Z key to rotate counterclockwise.

5. Use the space key to hard-drop.