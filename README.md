<div align="center">

# Team Resurgent Presents, Repackinator
**A Modern ISO Manager for Original Xbox**

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://github.com/Team-Resurgent/Repackinator/blob/main/LICENSE.md)
[![.NET](https://github.com/Team-Resurgent/Repackinator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Team-Resurgent/Repackinator/actions/workflows/dotnet.yml)
[![Discord](https://img.shields.io/badge/chat-on%20discord-7289da.svg?logo=discord)](https://discord.gg/VcdSfajQGK)

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/J3J7L5UMN)
[![Patreon](https://img.shields.io/badge/Patreon-F96854?style=for-the-badge&logo=patreon&logoColor=white)](https://www.patreon.com/teamresurgent)

[![Download](https://img.shields.io/badge/download-latest-brightgreen.svg?style=for-the-badge&logo=github)](https://github.com/Team-Resurgent/Repackinator/releases/latest)

Repackinator was designed to be a modern all-in-one ISO management tool for the Original Xbox.

It provides you the ability to convert your OG Xbox ISO dumps into full working split ISO images, as well as optionally replacing padding for even greater compression. Repackinator can also create reduced size ISO images by trimming the unused space, if desired. Additionally, the ability to create playable compressed ISO images was introduced to dovetail the newly released CCI (Cerbios Compressed Image) compression method; this after being written from scratch, in collaboration with Team Cerbios.

Programmed for the very specific task of compressing original Xbox ISO structures, all while removing unused and or wasted space, Cerbios Compressed Images are the gold standard of Xbox ISO compression. The smallest file sizes all while maintaining maximum playability is the name of the game.
</div>

                        [ Program ..................................... Repackinator V2.0.1 ]
                        [ Type ................................................ Iso Manager ]
                        [ Patreon ....................https://www.patreon.com/teamresurgent ]

                        [                           Team Members:                           ]
                        [ EqUiNoX ......................................... Lead Programmer ]
                        [ HoRnEyDvL ............................... Tester/ Project Manager ]
                        [ Hazeno ................................................... Tester ]

## System Requirements
### Minimum
* OS: Windows 7+ x64, x86-64 Linux, or MacOS (verified on Big Sur, should run from High Sierra onwards, please report any findings). 32-bit is not supported.
    * Repackinator can be ran in a virtual machine with GPU passthrough. (Virtual GPU must be disabled)
* RAM: 8GiB of RAM for proper operation.

## Prerequisites
  * [32-bit (x86) Visual C++ 2022 Redistributable](https://aka.ms/vs/17/release/vc_redist.x86.exe)
  * [64-bit (x64) Visual C++ 2022 Redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe)

## Core Features & Functionality
Repackinator will extract Certs, Title ID & Title Image (.tbn) from the XBE located inside the ISO Dump. It will then generate a new `default.xbe`, which will be used to load the ISO on ISO Enabled softmods and BIOSs, such as Cerbios (Native ISO Support), iND-BiOS (Patched), EvoX M8+ (Patched).

The generated `default.xbe` will use the XBE Title Column as the new Title Name. This is the name of the game which is displayed on your favorite dashboard.

Please note that the region shown in Repackinator is calculated based on the region that is extracted from the game's original XBE. These regions are:

  * GLO = (Global) USA,PAL,JPN
  * JPN = (Japan/Asia) JPN
  * PAL = (Europe/Australia) PAL
  * USA = (USA) USA
  * USA-JPN = (USA,Japan/Asia) USA,JPN
  * USA-PAL = (USA,Europe/Australia) USA,PAL

Current database contains 1044 games. The info shown has been compiled by extracting the Title Name, Region, Version & Title ID from the `default.xbe` of each game. This contains all USA Region Games, PAL Only Exclusives & JPN Only Exclusives.
***Remaining Xbox library available in the alternative database. JSON file can be edited to include missing titles if desired.***

Also included, is the ability to easily update legacy Attacher (default.xbe) created by tools like DVD2Xbox with new improved Cerbios Attacher (default.xbe).

## Install Notes
* Run Repackinator.exe first time as administrator. ***First run must be as administrator to enable context menu under Windows, CLI included.***

## Known Issues
* XBMC based FTP programs are known to "trim" files that get too close to the FATX limit. Repackinator is designed to be aware of this limit and will never produce a file larger than 4,290,735,312 bytes. We have had issues reported from this scenario. ***This will manifest as a black screen when trying to launch your game. If you transfer to Xbox with FTP and experience this, verify your *.1.cci or *.1.iso file size matches what is on your computer.***
  * ***UNVERIFIED*** EvolutionX FTP has been reported to work properly for this use.

* We ***only*** recommend [FATXplorer by Eaton Works](https://fatxplorer.eaton-works.com/3-0-beta/) officially at this time. This is to ensure no problems transferring files to and from your Xbox hard drive. *Recommendation may be updated as more tools are tested in the future.*

## GUI Functionality
<div align="center">

![GUI](https://github.com/Team-Resurgent/Repackinator/blob/main/readmeStuff/gui.png?raw=true)</div>
Per title:
* **Process**: must be selected for titles you desire to be processed. *Deselects after title have been processed*
* **Scrub**: replace the padding with zeros, for greater compression.

Options:
* **Grouping Selection**: creates grouped folders in the output directory. *Default = no grouping*
* **Use Uppercase**: output file/folder names with all *UPPERCASE* characters.
* **Compress**: apply CCI compression to the output. *CCI is currently only supported by Cerbios*
* **Trim Scrub**: remove all unused data at the end of data partition. *Similar to XISO*
* **Traverse Input Subdir's**: look for files to process inside any additional directories within your selected input folder.
* **Do not split ISO**: no spliting of ISO output. *≥4GiB titles will not fit FATX size limit*
* **Input Folder**: (Path to your Redump .ZIP/.7Z/.RAR/.CCI/.CSO or .ISO Files) ***SHOULD NOT INCLUDE REPACKINATOR'S ROOT, ANY SYSTEM FILES, OR BE A CHILD OF 'OUTPUT'***
* **Output Folder**: (Path to where you want to save your processed games)
* **Unpack Folder**: (Optional path to where to temporarily extract .ZIP/.7Z/.RAR archives)

Actions:
* **Save Game Data**: save changes to title information in database.
* **Save Selected**: export data from selected titles.
* **Scan Output**: scan titles in output folder.
* **Attach Update**: update attach files in output folder.
* **Process**: start processing.

## Command Line Use
* *Windows Only* ***Must run `Repackinator.exe -a=register` as admin to enable context menu use. Use `Repackinator.exe -a=unregister` as admin to remove context menu.***
* Run `Repackinator.exe -a=repack -h` to view possible commands in CLI.
* Run `Repackinator.exe -a=repack` along with the following options, based on your intended results.
```
  -i, --input=VALUE          Input folder
  -o, --output=VALUE         Output folder
  -p, --unpack=VALUE         Unpack folder (optional)
  -g, --grouping=VALUE       Grouping (None *default*, Region, Letter,
 	                                   RegionLetter, LetterRegion)
  -u, --upperCase            Upper Case
  -r, --recurse              Recurse (Traverse Sub Dirs)
  -c, --compress=VALUE       Compress (None *default*, CCI)
  -t, --trimmedScrub         Trimmed Scrub
  -l, --log=VALUE            log file
  -h, --help                 show help
  -w, --wait                 Wait on exit
```
## Context Menu
<div align="center">

After Repackinator has been ran as admin the first time, the context menu will populate.

![contextMenu](https://github.com/Team-Resurgent/Repackinator/blob/main/readmeStuff/contextMenu.png?raw=true)

***CONTEXT MENU OPTIONS WILL NOT CREATE `default.xbe` OR `default.tbn` FILES!***
</div>

* .ISO files can be split, as well as .CCI/.CSO files can be decompressed using the **Convert To ISO** functions.
* .ISO files can be compressed to various types of .CCI using the **Convert To CCI** functions.
* Compatible files can be cryptographically compared by selecting **Compare Set First** on initial file, then **Compare First With** on the second file.
* **Info** will print the sector data.
* **Extract** will create a HDD ready file from *any* supported input type.


## Acknowledgments
* First, we would like to thank all of our Patreon supporters! You are the reason we can continue to advance our open source vision of the Xbox Scene!
* We can't thank Team Cerbios enough for their amazing BIOS, as well as their continued contributions of features to a decades old gaming console. This program began as a collaboration with their team to modernize the Original Xbox. They also provided the modernized ISO Attach (default.xbe) with bug fixes and improvements. Thank you again!
* We want to thank all the Original Xbox devs for bringing us the awesome applications, dashboards and emulators we have grown to love and for kickstarting the scene back in the day.
* Thanks to the team at [Xbox-Scene Discord](https://discord.gg/VcdSfajQGK) - Haguero, AmyGrrl, CrunchBite, Derf, Risk, Sn34K, ngrst183
* Huge Shout-out to [Kekule](https://github.com/Kekule-OXC), [Ryzee119](https://github.com/Ryzee119), & [ChimericSystems](https://chimericsystems.com/) for all the time & effort they have put towards reverse engineering & creation of new hardware mods.
* To all the people behind projects such as [xemu](https://github.com/mborgerson/xemu) and [Insignia](https://insignia.live/). Keep up the amazing work! We can't wait for your final product releases.
* Greetz to the following scene people, in no particular order - Milenko, Iriez, Mattie, ODB718, ILTB, HoZy, IceKiller, Rowdy360, Lantus, Kl0wn, nghtshd, Redline99, The_Mad_M, Und3ad, HermesConrad, Rocky5, xbox7887, tuxuser, Masonly, manderson, InsaneNutter, IDC, Fyb3roptik, Bucko, Aut0botKilla, headph0ne,Xer0 449, hazardous774, rusjr1908, Octal450, Gunz4Hire, Dai, bluemeanie23, T3, ToniHC, Emaxx, Incursion64, empyreal96, Fredr1kh, Natetronn, braxtron
<!--* I'm sure there is someone else that belongs here too ;)-->

<div align="center">

![GitHub contributors](https://img.shields.io/github/contributors/Team-Resurgent/Repackinator?style=flat-square)
![GitHub repo file count](https://img.shields.io/github/directory-file-count/Team-Resurgent/Repackinator?style=flat-square)
![Lines of code](https://img.shields.io/tokei/lines/github/Team-Resurgent/Repackinator?style=flat-square)
![GitHub repo size](https://img.shields.io/github/repo-size/Team-Resurgent/Repackinator?style=flat-square)
![GitHub all releases](https://img.shields.io/github/downloads/Team-Resurgent/Repackinator/total?style=flat-square)

</div>
