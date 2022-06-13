# KSP Star Tracker Mod
## Summary
Star Tracker mod for Kerbal Space Program. It is a direct modification of the HullCamVDS mod with some functions from the Tarsier Space Technologies mod. It uses the European Space Agency Tetra3 algorithm and CPython.

## Star Tracker Info
The purpose of this mod is to find the attitude of a spacecraft by taking images of the stars. This attitude solution is expressed as a quaternion, which - along with gyroscope data - can be used in a control system (say in kOS) to control the attitude of the spacecraft. This is how satellites control their attitude in the real world.

I have only added code to "MuMechModuleHullCamera.cs", including from TST and my own Star Tracker code. I most likely will not be continuing development with this. But if someone wants to pick it up and do the things listed in the TODO section, or add other things, please go ahead!

The star tracker algorithm works with cameras that have a maximum Field of View (FOV) of 20 degrees. The camera in this mod has a 20 degree FOV. If you really wanted to, you could regenerate a different star catalogue to work with a different FOV using the Tetra3 code. The star catalogue I generated comes from the Tycho catalogue and only contains  stars that have at most 6 magnitude brightness (star brightness is an inverse log scale, so the bigger the magnitude, the dimmer the star).

This mod requires Pood's Deep Star Map skybox mod to work. However, I believe there is some distortion in the stars due to the images being mapped onto a cube. Thus, sometimes you do not get a quaternion solution.

I have never made a mod for KSP, so I didn't really know what I was doing at first. I've learned a lot about Unity and how KSP works, but there's still a lot I don't  know or understand. There may be better ways to do the things I implemented, so feel free to change my code.


**DEPENDENCY**: Pood's Deep Star Map skybox (to add in real stars) - https://forum.kerbalspaceprogram.com/index.php?/topic/169919-13-112-poods-skyboxes-v130-17th-jan-2019/
**HOW TO USE**: Once you right-click on the mounted camera and press "Activate", the star tracker algorithm will begin. The data can be seen in the console if you press "ALT-F12".


## About The Code
This mod adapts code from the Tarsier Space Technologies mod (TSTCameraModule.cs) and the HullCameraVDS mod (MuMechModuleHullCamera.cs). I have mainly added onto the "MuMechModuleHullCamera.cs" file, so there is still a dependency on the other C# files made by Albert VDS. I did remove the EVA camera code since "MuMechModuleHullCamera.cs" did not call any methods from that script file, so I deemed it as unnecessary to include. This mod also uses assets from the HullCameraVDS mod (camera model and textures), and I do not take credit for those models and textures. That credit goes to Albert VDS. LinuxGuruGamer is now mainaining HullCameraVDS as HullCameraVDSContinued, and I do not take credit for any additional code he wrote. I have indicated where I added code as "Star Tracker" with comments and Tarsier Space Technologies with comments and regions.
  
  
### Star Tracker modification
Pertinent C# code, Python code that interacts with the Tetra3 star tracker algorithm, and the C++ binder between C# and Python<br>
by Benjamin Pittelkau<br>
License: GPLv3
  
### Tetra3 Star Tracker Algorithm
by the European Space Agency (ESA), and is publicly available here: https://github.com/esa/tetra3<br>
License: Apache License 2.0
  
### Original MuMechModuleHullCamera.cs, camera models, and textures
by Albert VDS<br>
Original HullCamVDS: https://forum.kerbalspaceprogram.com/index.php?/topic/42739-11hullcam-vds-mod-adopted-by-linuxgamer/<br>
HullcamVDS Continued: https://forum.kerbalspaceprogram.com/index.php?/topic/145633-1111-hullcam-vds-continued/<br>
License: GPLv3
  
### TSTCameraModule.cs
(C) Copyright 2015, Jamie Leighton<br>
Tarsier Space Technologies<br>
The original code and concept of TarsierSpaceTech rights go to Tobyb121 on the Kerbal Space Program Forums, which was covered by the MIT license. Original License is here: https://github.com/JPLRepo/TarsierSpaceTechnology/blob/master/LICENSE. As such this code continues to be covered by MIT license. Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This project is in no way associated with nor endorsed by Squad. TST mod page: https://forum.kerbalspaceprogram.com/index.php?/topic/154853-112x-tarsier-space-technology-with-galaxies-v713-12th-sep-2021/
  

## Star Tracker TODO section
Bugs:
- Camera and rendered galaxy texture are not aligned
- Crash if physics-less "High-speed" time warp is used while Star Tracker is active
- Fix possible skybox distortion (maybe map the background texture to part of a sphere?? --is that possible??)

TODO:
- Allow star tracker to sample at a specific rate (right now the code waits until the star tracker tries to get a solution, which happens at a non-constant rate)
- Add a separate button to activate star tracker
- Add a module to output quaternion solution to kerbal Operating System (kOS) mod
- Create a custom Star Camera model and texture
- Remove any dependency on HullCamVDS stuff (isolate Star Tracker code to make this mod only about the Star Tracker)