# tetr15

[![Made with C#](https://img.shields.io/badge/Made%20with-CS-blue)](https://en.wikipedia.org/wiki/C_Sharp_(programming_language)) [![LatestRelease](https://img.shields.io/badge/Version-1.1.0-orange)](https://github.com/R-Bet/tetr15/releases/tag/1.1.0)

Tetris on console, made using pure C#!

Features include ghost pieces, SRS and an incredibly ugly menu.

# Build and run

**1. Clone the repository**
```
git clone https://github.com/R-Bet/tetr15
```

Or clone without the history, which you don't need to run the program:
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

The `.exe` will be generated at `/publish/`


_or just get the prebuilt from the releases `¯\_(ツ)_/¯`_

# Controls

1. Use numbers and arrows to interact with the menu.

2. Use Left & Right arrow keys or A and D keys to move horizontally.

3. Press the W key to rotate clockwise.

4. Press the Z key to rotate counterclockwise.

5. Press the C key to hold a piece or switch the held piece.

6. Press the Space key to hard-drop.

7. Press the ESC key to quit to menu.

8. Press the R key to restart current game with the same starting level.

