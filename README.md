
                              # Team Resurgent   
                                                                             
                                                                          
      [ Program ..................................... Repackinator V1.0.1 ]
      [ Type ................................................. Iso Manager]
      [ Platform .................................... Windows, Linux, OSX ]
      [ OS Architecture ............................................. X64 ]
      [ By ............................................... Team Resurgent ]
      [ Homepage ..........https://github.com/Team-Resurgent/Repackinator ]
      [ Patreon ....................https://www.patreon.com/teamresurgent ]
	  [ Release date ......................................... 25.09.2022 ]
	  
	  [                          Team Members:                            ]
	  [ EqUiNoX ......................................... Lead Programmer ]
	  [ HoRnEyDvL ............................... Tester/ Project Manager ]
	  [ Hazeno ................................................... Tester ]
	  
```
/////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
///                                                                           ///
///                          Changes/Additions:                               ///
///                                                                           ///
///  -----------------------------------------------------------------------  ///
///                                                                           ///
///   Release: V1.0.1                                                         ///
///  -----------------------------------------------------------------------  ///
///  1.  Added native support for Linux & OSX                                 ///
///  2.  Fixed typos in GUI                                                   ///
///  3.  Github automation improvements                                       ///
///  4.  Added CLI version (For those that don't like GUI's)                  ///
///                                                                           ///
///  -----------------------------------------------------------------------  ///
///                                                                           ///
///                                                                           ///
///   Initial Release-                                                        ///
///  -----------------------------------------------------------------------  ///
///                                                                           ///
///  1.  ZIP/7Z & ISO Support                                                 ///
///  2.  Modern GUI                                                           ///
///  3.  Current DB contains 1044 Games, The info shown has been compiled     ///
///      by extracting Title Name, Region, Version & Title ID from the        ///
///      Default.XBE of each game.                                            ///
///  4.  CRC32 Info has been compiled by calculating the CRC32 of ISO and     ///
///      comparing it to that found on Redump.org                             ///
///  5.  Title Images extracted as Default.TBN                                ///
///  6.  Cerbios Modern ISO Attach (Default.XBE) backwards compatible with    ///
///      Iso Patched Bioses (IND,EVOX)                                        ///
///  7.  Splits Iso in 2 equal parts, 3.45 GB (3,709,681,664 bytes) each      ///
///  8.  DB contains all USA Region Games, Pal Only Exclusives & Jap Only     ///
///      Exclusives.                                                          ///
///  9.  Ability to edit Title Name(XBE Name), ISO & Folder Names             ///
///  10. Ability to group generated Isos by Region, Letter, Region + Letter,  ///
///      Letter + Region or Default(No Grouping)                              ///
///  11. Ability to easily update Attacher(Default.XBE) with New Version      ///
///                                                                           ///
///                                                                           ///
///  -----------------------------------------------------------------------  ///
///                                                                           ///
///                                                                           ///
////////////////                                                 ////////////////
////////////////                                                 ////////////////
///                                                                           ///
///  About & Release Notes                                                    ///
///  -----------------------------------------------------------------------  ///
///                                                                           ///
///                What is Repackinator?                                      ///
///                                                                           ///
///  Repackinator was designed to be a AIO ISO Management tool which will     ///
///  provide you the ability to convert your full OG Xbox Iso Dumps into      ///
///  Split ISO Images, Making them compatible with ISO loading bioses for     ///
///  the original Xbox console.                                               ///
///                                                                           ///
///  -----------------------------------------------------------------------  ///
///                                                                           ///
///                     Core Features & Functionality                         ///
///                                                                           ///
///  Repackinator will extract Certs, Title ID & Title Image from the XBE     ///
///  located inside the ISO Dump. It will then generate a Default.XBE which   ///
///  Will be used to load the ISO Games on ISO Enabled Bioses such as         ///
///  Cerbios (Native Iso Support) , IND-Bios (Patched) , EVOX (Patched)       ///
///                                                                           ///
///  The generated Default.XBE will use the XBE Title Column as the New       ///
///  Title Name. This is the name of the game which is displayed on your      ///
///  favorite dashboard.                                                      ///
///                                                                           ///
///  Please note that the region shown in Repackinator is calculated based    ///
///  on the Region that is extracted from the Games XBE. These regions are    ///
///  GLO = (GLOBAL) USA,PAL,JAP                                               ///
///  JAP = (JAPAN/ASIA) JAP                                                   ///
///  PAL = (Europe/Australia) PAL                                             ///
///  USA = (USA) USA                                                          ///
///  USA-JAP = (USA,JAPAN/ASIA) USA,JAP                                       ///
///  USA-PAL = (USA,Europe/Australia) USA,PAL                                 ///
///                                                                           ///
///                                                                           ///
////////////////                                                 ////////////////
////////////////                                                 ////////////////
///                                                                           ///
///  Install Notes                                                            ///
///  -----------------------------------------------------------------------  ///
///                                                                           ///
///  Run RepackinatorUI.exe.                                                  ///
///  Select Grouping Type.                                                    ///
///  Set Input Folder. (Path to your Redump .ZIP/.7Z or .ISO) Files           ///
///  Set Output Path. (Path to where you want to save your processed game)    ///
///  Click On Process, Sit back while Repackinator does the hard work for you ///
///                                                                           ///
///                                                                           ///
///                                                                           ///
////////////////                                                 ////////////////
////////////////                                                 ////////////////
///                                                                           ///
///  Acknowledgements                                                         ///
///  -----------------------------------------------------------------------  ///
///                                                                           ///
///  Firstly a shout-out to Team Cerbios for their amazing Bios. This         ///
///  App was a collaboration with Team Cerbios Who also provided              ///
///  A modern ISO Attach (Default.XBE) with bug fixes and improvements.       ///
///                                                                           ///
///  We want to thank all the Original Xbox devs for bringing us the awesome  ///
///  Applications, Dashboards and Emulators we have grown to love and for     ///
///  kickstarting the scene back in the day.                                  ///
///                                                                           ///
///  Thanks to the team on Xbox-scene Discord. https://discord.gg/VcdSfajQGK  ///
///  Haguero, AmyGrrl, CrunchBite, Derf, Risk, Sn34K, ngrst183                ///
///                                                                           ///
///  Huge Shout-out to Kekule & Ryzee119 for all the time & effort they have  ///
///  put towards the reverse engineering & creation of new hardware mods.     ///
///  Please show them some love by visiting their web sites located at.       ///
///  https://chimericsystems.com/                                             ///
///  https://github.com/Kekule-OXC                                            ///
///  https://github.com/Ryzee119                                              ///
///                                                                           ///
///                                                                           ///
///  To all the people behind projects such as Xemu and Insignia. Keep up     ///
///  the amazing work, cant wait to for your final product releases.          ///
///                                                                           ///
///  Greetz to the following scene people.                                    ///
///  Milenko, Iriez, Mattie, ODB718, ILTB, HoZy, IceKiller, Rowdy360, Lantus  ///
///  Kl0wn, nghtshd, Redline99, The_Mad_M, Und3ad, HermesConrad, Rocky5,      ///
///  xbox7887, tuxuser, Masonly, manderson, InsaneNutter, IDC, Fyb3roptik     ///
///  Bucko, Aut0botKilla, headph0ne,Xer0 449, hazardous774, rusjr1908,        ///
///  Octal450, Gunz4Hire, Dai, bluemeanie23, T3, ToniHC, Emaxx, Incursion64   ///
///  empyreal96, Fredr1kh, Natetronn, braxtron                                ///
///                                                                           ///
/////////////////////////////////////////////////////////////////////////////////
///////////////////////////// 2022 Team Resurgent ///////////////////////////////
///////////////////////////// nfo by Team Resurgent /////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
```
