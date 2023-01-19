<div align="center">

# Team Resurgent Presents, Repackinator
**A Modern ISO Manager for Original Xbox**

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://github.com/Team-Resurgent/Repackinator/blob/main/LICENSE.md)
[![.NET](https://github.com/Team-Resurgent/Repackinator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Team-Resurgent/Repackinator/actions/workflows/dotnet.yml)
[![Discord](https://img.shields.io/badge/chat-on%20discord-7289da.svg?logo=discord)](https://discord.gg/VcdSfajQGK)

[![Patreon](https://img.shields.io/badge/Patreon-F96854?style=for-the-badge&logo=patreon&logoColor=white)](https://www.patreon.com/teamresurgent)

Repackinator was designed to be a modern all-in-one ISO management tool for the Original Xbox. 

It provides you the ability to convert your OG Xbox ISO dumps into full working split ISO images, as well as optionally replacing padding for even greater compression. Repackinator can also create reduced size ISO images by trimming the unused space, if desired. Additionally, the ability to create playable compressed ISO images was introduced to dovetail the newly released .CCI (Cerbios Compressed Image) compression method; this after being written from scratch, in collaboration with Team Cerbios. 

Programmed for the very specific task of compressing original Xbox ISO structures, all while removing unused and or wasted space, Cerbios Compressed Images are the gold standard of Xbox ISO compression. The smallest file sizes all while mantaining maximum playablity is the name of the game.
</div>

                        [ Program ..................................... Repackinator V1.2.9 ]
                        [ Type ................................................ Iso Manager ]
                        [ Patreon ....................https://www.patreon.com/teamresurgent ]
                                        
                        [                          Team Members:                            ]
                        [ EqUiNoX ......................................... Lead Programmer ]
                        [ HoRnEyDvL ............................... Tester/ Project Manager ]
                        [ Hazeno ................................................... Tester ]

## System Requirements
### Minimum
* OS: Windows 7+ x64, x86-64 Linux, or MacOS (verified on Big Sur, should run from High Sierra onwards, please report any findings). 32-bit is not supported.
    * Repackinator can be ran in a virtual machine with GPU passthrough. (Virtual GPU must be disabled)
* RAM: 8gb of RAM for proper operation.

## Prerequisites
  * [64-bit (x64) Visual C++ 2022 Redistributable](https://aka.ms/vs/17/release/vc_redist.x86.exe)

## Core Features & Functionality
Repackinator will extract Certs, Title ID & Title Image (.tbn) from the XBE located inside the ISO Dump. It will then generate a new `default.xbe`, which will be used to load the ISO on ISO Enabled Bioses, such as Cerbios (Native ISO Support) iND-BiOS (Patched), EvoX M8+ (Patched)

The generated `default.xbe` will use the XBE Title Column as the new Title Name. This is the name of the game which is displayed on your favorite dashboard.

Please note that the region shown in Repackinator is calculated based on the region that is extracted from the game's original XBE. These regions are:

  * GLO = (GLOBAL) USA,PAL,JAP
  * JAP = (JAPAN/ASIA) JAP
  * PAL = (Europe/Australia) PAL
  * USA = (USA) USA
  * USA-JAP = (USA,JAPAN/ASIA) USA,JAP
  * USA-PAL = (USA,Europe/Australia) USA,PAL

Current database contains 1044 games. The info shown has been compiled by extracting the Title Name, Region, Version & Title ID from the `default.xbe` of each game. This contains all USA Region Games, PAL Only Exclusives & JAP Only Exclusives. ***Full Xbox library support to come in a future release. JSON file can be edited to include missing titles if desired in the interim***.

Also included, is the ability to easily update legacy Attacher (default.xbe) created by tools like DVD2Xbox with new improved Cerbios Attacher (default.xbe).

## Install Notes
* Run Repackinator.exe first time as administrator. ***first run must be as administrator to enable context menu under Windows, CLI included***

## Known Issues
* XBMC based FTP programs are known to "trim" files that get too close to the FATX limit. Repackinator is designed to be aware of this limit and will never produce a file larger than 4,290,735,312 bytes. We have had issues reported from this scenario. ***This will manifest as a black screen when trying to launch your game. If you transfer to Xbox with FTP and experence this, verify your *.1.cci or *.1.iso file size matches what is on your computer.***
  * ***UNVERIFIED*** EvolutionX FTP has been reported to work properly for this use.

* We ***only*** recommend [FATXplorer by Eaton Works](https://fatxplorer.eaton-works.com/3-0-beta/) offically at this time. This is to ensure no problems transfering files to and from your Xbox hard drive. *Recommendation may be updated as more tools are tested in the future.*

## GUI Functionality
<div align="center">

![GUI](https://github.com/Team-Resurgent/Repackinator/blob/main/readmeStuff/gui.png?raw=true)</div>
* Select Grouping Type *creates grouped folders in the output directory. Default = no grouping*
* Set Input Folder. (Path to your Redump .ZIP/.7Z/.RAR/.CCI/.CSO or .ISO Files) ***SHOULD NOT INCLUDE REPACKINATOR'S ROOT, ANY SYSTEM FILES, OR BE A CHILD OF 'OUTPUT'***
* Set Output Path. (Path to where you want to save your processed games)
* **Process**: Must be selected for titles you desire to have prepaired
* **Scrub**: is selected by default. This will replace the padding with zeros, for greater compression. de-selecting will simply split ISO for Xbox FATX file system during processing.
* **Use Uppercase**: will output file/folder names with all uppercase characters.
* **Compress**: will add .cci compression to the output. *Note: .cci is currently only supported while using Cerbios.* 
* **Trim Scrub**: will remove all unused data at the end of data partition. *Similar to XISO*  
* **Traverse Input Subdir's**: will look for files to process inside any additional directories within your selected input folder.

## Command Line Use
* *Windows Only* ***Must run `Repackinator.exe -a=register` as admin to enable context menu use. Use `Repackinator.exe -a=unregister` as admin to remove context menu.***
* Run `Repackinator.exe -a=repack -h` to view possible  commands in CLI.
* Run `Repackinator.exe -a=repack` along with the following options, based on your intended results.
```
  -i, --input=VALUE          Input folder
  -o, --output=VALUE         Output folder
  -g, --grouping=VALUE       Grouping (None Default, Region, Letter,
 	                                   RegionLetter, LetterRegion)
  -u, --upperCase            Upper Case
  -r, --recurse              Recurse (Traverse Sub Dirs)
  -c, --compress             Compress (As .CCI)
  -t, --trimmedScrub         Trimmed Scrub
  -l, --log=VALUE            log file
  -h, --help                 show this message and exit
  -w, --wait                 Wait on exit
```
## Context Menu
<div align="center">

After Repackinator has been ran as admin the first time, the context menu will populate.

![contextMenu](https://github.com/Team-Resurgent/Repackinator/blob/main/readmeStuff/contextMenu.png?raw=true)

***CONTEXT MENU OPTIONS WILL NOT CREATE `default.xbe` OR `default.tbn` FILES!***
</div>

* .ISO files can be split as, well as .CCI files can be decompressed using the **Convert to ISO** functions.
* .ISO files can be compressed to various types of .CCI using the **Convert to CCI** functions.
* Compatible files can be cryptographically compaired by selecting **Compare Set First** on initial file, then **Compare First With** on the second file.
* Info will print the sector data.
* Extract will create a HDD ready file from *any* supported input type.


## Acknowledgements
* First, we would like to thank all of our Patreon supporters! You are the reason we can continue to advance our open source vision of the Xbox Scene!
* We can't thank Team Cerbios enough for their amazing Bios, as well as their continued contributions of features to a decades old gaming console. This program began as a collaboration with their team to modernize the Original Xbox. They also provided the modernized ISO Attach (default.xbe) with bug fixes and improvements. Thank you again!
* We want to thank all the Original Xbox devs for bringing us the awesome applications, dashboards and emulators we have grown to love and for kickstarting the scene back in the day.
* Thanks to the team at [Xbox-Scene Discord](https://discord.gg/VcdSfajQGK) - Haguero, AmyGrrl, CrunchBite, Derf, Risk, Sn34K, ngrst183
* Huge Shout-out to [Kekule](https://github.com/Kekule-OXC), [Ryzee119](https://github.com/Ryzee119), & [ChimericSystems](https://chimericsystems.com/) for all the time & effort they have put towards reverse engineering & creation of new hardware mods.
* To all the people behind projects such as [xemu](https://github.com/mborgerson/xemu) and [Insignia](https://insignia.live/). Keep up the amazing work! We can't wait for your final product releases.
* Greetz to the following scene people, in no particular order - Milenko, Iriez, Mattie, ODB718, ILTB, HoZy, IceKiller, Rowdy360, Lantus, Kl0wn, nghtshd, Redline99, The_Mad_M, Und3ad, HermesConrad, Rocky5, xbox7887, tuxuser, Masonly, manderson, InsaneNutter, IDC, Fyb3roptik, Bucko, Aut0botKilla, headph0ne,Xer0 449, hazardous774, rusjr1908, Octal450, Gunz4Hire, Dai, bluemeanie23, T3, ToniHC, Emaxx, Incursion64, empyreal96, Fredr1kh, Natetronn, braxtron
<!--* I'm sure there is someone else that belongs here too ;)-->
