# osu-rx
osu!standard relax hack

## Status
osu!rx is still under development. Bug reports, feature requests, pull requests and any other help or feedback are very much appreciated!

Hit Timing randomization and HitScan need improvements and your feedback. Take a look at the code to get started!
- [Hit Timings randomization](https://github.com/mrflashstudio/osu-rx/blob/master/osu!rx/Core/Relax.cs#L254)
- [HitScan](https://github.com/mrflashstudio/osu-rx/blob/master/osu!rx/Core/Relax.cs#L209)

## Features
- **Automatic beatmap detection:** you don't need to alt-tab from osu! to select beatmap manually, osu!rx will do all the dirty work for you.

- **Playstyles:** osu!rx has 4 most(?) popular built-in playstyles that you can select!
  - Singletap
  - Alternate
  - Mouse only
  - TapX
  
- **Hit Timing randomization:** osu!rx automatically randomizes click timings depending on whether you're currently alternating or not.

- **HitWindow100 Key:** if this key is pressed, osu!rx will widen current hit timings so you can get more 100s and slightly increase your Unstable Rate.

- **HitScan:** it will scan for current cursor position and determine whether relax should hit right now, earlier or later.

- **osu!rx won't ever be outdated:** osu!rx has IPC Fallback mode which will turn on if memory scanning fails. While this mode is on, osu!rx will communicate with osu! through IPC to get needed variables. So if there will be any breaking-updates to osu!, most of osu!rx's functions will continue to work.

## Running osu!rx
**Download latest build from one of the sources below:**  
| [Latest GitHub release](https://github.com/mrflashstudio/osu-rx/releases/latest) | [MPGH (every build is approved by forum admin)](https://www.mpgh.net/forum/showthread.php?t=1488076) |
|-----------------------|-----------------------------------------------|  

*Paranoids can compile source code themselves ;)*

- Extract downloaded archive into any folder.
- Launch osu!, then run osu!rx.exe
- Change any setting you want.
- Select "Start relax" option in the main menu.
- Go back to osu! and select beatmap you want to play.
- Start playing!

### Requirements
- .net framework 4.7.2 is required to run osu!rx. You can download it [here](https://dotnet.microsoft.com/download/thank-you/net472).  

### Important notes
- If you plan on changing executable's name, then change the name of *"osu!rx.exe.config"* too or else it will crash.  

- If you see something like *"Unhandled exception: System.IO.FileNotFoundException: ..."* follow these steps:
  - Right click on downloaded archive.
  - Click "Properties".
  - Click "Unblock".
  - Click "Apply" and repeat all steps described in **Running osu!rx** section.
   ![s](https://i.ibb.co/jZY8fk0/image.png)

### Demonstation video
***osu!rx does not affect performance. In this case lags were caused by obs and cute girls in the background.***
[![Video](https://i.ibb.co/grQSzMP/screenshot065.png)](https://www.youtube.com/watch?v=1FUxnGqjASQ)
