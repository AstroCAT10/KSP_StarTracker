/* 
 * This mod adapts code from the Tarsier Space Technologies mod (TSTCameraModule.cs) and the HullCameraVDS mod (MuMechModuleHullCamera.cs). I have mainly
 * added onto the "MuMechModuleHullCamera.cs" file, so there is still a dependency on the other C# files made by Albert VDS. I did remove the EVA camera
 * code since "MuMechModuleHullCamera.cs" did not call any methods from that script file, so I deemed it as unnecessary to include. This mod
 * also uses assets from the HullCameraVDS mod (camera model and textures), and I do not take credit for those models and textures. That credit goes
 * to Albert VDS. LinuxGuruGamer is now mainaining HullCameraVDS as HullCameraVDSContinued, and I do not take credit for any additional code he wrote.
 * I have indicated where I added code as "Star Tracker" with comments and Tarsier Space Technologies with comments and regions.
 * 
 * 
 * Star Tracker modification:
 * Pertinent C# code, Python code that interacts with the Tetra3 star tracker algorithm, and the C++ binder between C# and Python
 * by Benjamin Pittelkau
 * License: GPLv3
 * 
 * Tetra3 Star Tracker Algorithm:
 * by the European Space Agency (ESA), and is publicly available here: https://github.com/esa/tetra3
 * License: Apache License 2.0
 * 
 * Original MuMechModuleHullCamera.cs, camera models, and textures:
 * by Albert VDS
 * Original HullCamVDS: https://forum.kerbalspaceprogram.com/index.php?/topic/42739-11hullcam-vds-mod-adopted-by-linuxgamer/
 * HullcamVDS Continued: https://forum.kerbalspaceprogram.com/index.php?/topic/145633-1111-hullcam-vds-continued/
 * License: GPLv3
 * 
 * TSTCameraModule.cs:
 * (C) Copyright 2015, Jamie Leighton
 * Tarsier Space Technologies
 * The original code and concept of TarsierSpaceTech rights go to Tobyb121 on the Kerbal Space Program Forums, which was covered by the MIT license.
 * Original License is here: https://github.com/JPLRepo/TarsierSpaceTechnology/blob/master/LICENSE
 * As such this code continues to be covered by MIT license.
 * Kerbal Space Program is Copyright (C) 2013 Squad.See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 * TST: https://forum.kerbalspaceprogram.com/index.php?/topic/154853-112x-tarsier-space-technology-with-galaxies-v713-12th-sep-2021/
 * 
 * 
 * ----- Star Tracker Info-----
 * The purpose of this mod is to find the attitude of a spacecraft by taking images of the stars. This attitude solution is expressed as a quaternion, which - along with 
 * gyroscope data - can be used in a control system (say in kOS) to control the attitude of the spacecraft. This is how satellites control their attitude in the real world.
 * 
 * 
 * I have only added code to MuMechModuleHullCamera.cs, including from TST and my own Star Tracker code. I most likely will not be continuing development
 * with this. But if someone wants to pick it up and do the things listed in the TODO section, or add other things, please go ahead!
 * 
 * The star tracker algorithm works with cameras that have a maximum Field of View (FOV) of 20 degrees. The camera in this mod has a 20 degree FOV. If you really wanted to, you 
 * could regenerate a different star catalogue to work with a different FOV using the Tetra3 code. The star catalogue I generated comes from the Tycho catalogue and only contains 
 * stars that have at most 6 magnitude brightness (star brightness is an inverse log scale, so the bigger the magnitude, the dimmer the star).
 * 
 * This mod requires Pood's Deep Star Map skybox mod to work. However, I believe there is some distortion in the star images due to the images being mapped onto a cube. Thus, sometimes 
 * you do not get a quaternion solution.
 * 
 * I have never made a mod for KSP, so I didn't really know what I was doing at first. I've learned a lot about Unity and how KSP works, but there's still a lot I don't 
 * know or understand. There may be better ways to do the things I implemented, so feel free to change my code.
 * 
 * 
 * *****DEPENDENCY*****: Pood's Deep Star Map skybox (to add in real stars) - https://forum.kerbalspaceprogram.com/index.php?/topic/169919-13-112-poods-skyboxes-v130-17th-jan-2019/
 * *****HOW TO USE*****: Once you right-click on the mounted camera and press "Activate", the star tracker algorithm will begin. The data can be seen in the console if you press "ALT-F12".
 * 
*/


/* ----- Star Tracker TODO section -----
 * Bugs:
 * - Camera and rendered galaxy texture are not aligned
 * - Crash if physics-less "High-speed" time warp is used while Star Tracker is active
 * - Fix possible skybox distortion (maybe map the background texture to part of a sphere?? --is that possible??)
 * 
 * TODO:
 * - Allow star tracker to sample at a specific rate (right now the code waits until the star tracker tries to get a solution, which happens at a non-constant rate)
 * - Add a separate button to activate star tracker
 * - Add a module to output quaternion solution to kerbal Operating System (kOS) mod
 * - Create a custom Star Camera model and texture
 * - Remove any dependency on HullCamVDS stuff (isolate Star Tracker code to make this mod only about the Star Tracker)
 *
*/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;
using KSP.IO;
using System.Runtime.InteropServices;

namespace HullcamVDS
{

    public class MuMechModuleHullCamera : PartModule
    {
        MovieTime mt = new MovieTime();

        //The following TODO list is from the HullCamVDS mod
        // TODO: Bugs:
        // - If the vessel is entirely destroyed with AllowMainCamera == false it's still possible to get stuck without a camera.
        //   When that happens the menus don't work and we're stuck.
        //   The part might be dead but not removed, so part management needs improving.

        // TODO: If we prevent cycling between different vessels, then it's a better experience to keep track of each vessels active camera.
        // TODO: Test action groups.
        // TODO: Look at describing what camera we're viewing from.

        // TODO: No-main-camera issues:
        // - Can't rename vessel
        // - Can't make crew reports
        // - Can't control from different places

        private const bool adjustMode = false;

        [KSPField]
        public float cameraFoVMax = 20;

        [KSPField]
        public float cameraFoVMin = 20;

        [KSPField]
        public float cameraZoomMult = 1.25f;

        [KSPAction("#autoLOC_HULL_EVT_007")] //Zoom In
        public void ZoomInAction(KSPActionParam ap)
        {
            sActionFlags.zoomIn = true;
        }

        [KSPAction("#autoLOC_HULL_EVT_008")] //Zoom Out
        public void ZoomOutAction(KSPActionParam ap)
        {
            sActionFlags.zoomOut = true;
        }

        [KSPField]
        public Vector3 cameraPosition = Vector3.zero;

        [KSPField]
        public Vector3 cameraForward = Vector3.forward;

        [KSPField]
        public Vector3 cameraFlip = Vector3.forward;

        [KSPField]
        public Vector3 cameraUp = Vector3.up;

        [KSPField]
        public string cameraTransformName = "";

        [KSPField]
        public bool allowFlip = false;

        [KSPField]
        public float cameraFoV = 20;

        [KSPField(isPersistant = false)]
        public float cameraClip = 0.05f;

        [KSPField]
        public bool camActive = false; // Saves when we're viewing from this camera.

        [KSPField]
        public bool camEnabled = true; // Lets us skip cycling through cameras.

        [KSPField(isPersistant = false)]
        public string cameraName = "Hull";

        [KSPField]
        public float cameraMode = 0;

        [KSPField(isPersistant = false)]
        public string mode = "Hull";

        public static List<MuMechModuleHullCamera> sCameras = new List<MuMechModuleHullCamera>();

        // Keep track of the camera we're viewing from.
        // A null value represents using the main camera.
        public static MuMechModuleHullCamera sCurrentCamera = null;

        // One camera module is the designated input handler, all others ignore it.
        // A camera's destroy function clears this and we have to set another in the update routine.
        public static MuMechModuleHullCamera sCurrentHandler = null;

        // Stores the current flight camera.
        protected static FlightCamera sCam = null;

        // Camera distance from active vessel
        public double sCameraDistance = double.NaN;

        // Takes a backup of the external camera.
        protected static Transform sOrigParent = null;
        protected static Quaternion sOrigRotation = Quaternion.identity;
        protected static Vector3 sOrigPosition = Vector3.zero;
        protected static float sOrigFov;
        protected static float sOrigClip;
        protected static Texture2D sOverlayTex = null;


        //from TST vvv --- second camera
        private int textureWidth = 256;
        private int textureHeight = 256;
        // Standard Cameras  
        public CameraHelper _galaxyCam;
        public CameraHelper _scaledSpaceCam;
        public CameraHelper _farCam;
        public CameraHelper _nearCam;
        // FullScreen Cameras  
        public CameraHelper _galaxyCamFS;
        public CameraHelper _scaledSpaceCamFS;
        public CameraHelper _farCamFS;
        public CameraHelper _nearCamFS;
        // Render Textures
        private RenderTexture _renderTexture;
        private RenderTexture _renderTextureFS;
        private Texture2D _texture2D;
        private Texture2D _texture2DFullSze;
        private Renderer[] skyboxRenderers;

        private AtmosphereFromGround[] atmospheres;
        private ScaledSpaceFader[] scaledSpaceFaders;

        //TempVars
        private float exposure;
        private Color origColor;
        private Renderer skyboxRenderer;
        private float tmpZoom;
        private float tmpfov;
        private bool zoomSkyBox = true;
        private float TanRadDfltFOV;
        private float TanRadFOV;
        private RenderTexture activeRT;
        private float staticPressure;
        //public TSTSpaceTelescope telescopeReference;

        //camera rendering info cache
        Dictionary<AtmosphereFromGround, Vector4> atmoInfo = new Dictionary<AtmosphereFromGround, Vector4>();

        //Const - but we don't use constants for Garbage collector
        private double KPtoAtms = 0.009869232;
        public float SkyboxExposure = 1;

        public Transform cameraTransform { get; private set; }
        private Transform _animationTransform;
        private Transform _baseTransform;
        private Transform _lookTransform;
        private Quaternion zeroRotation;


        private Thread _STthread;
        private bool solverDone = true;

        public byte[] star_img_color; //Star Tracker
        public double[] quaternion = new double[4] { 0, 0, 0, 0 }; //Star Tracker
        public IntPtr pSTClass; //Star Tracker
        public float prev_time = 0;
        private static float sample_rate = 5; //Hz --> samples / sec
        public float sample_time = 1 / sample_rate;
        public bool ST_sample = false;
        public bool first_ST_sample = true;
        private int len;
        private int w;
        private int h;


        //from TST ^^^ --- second camera

        /* --------- star tracker --------- */
        //place python.runtime, microsoft.csharp, and netstandard (C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Microsoft\Microsoft.NET.Build.Extensions\net461\lib) into "Managed" folder

        [DllImport(@"C:\Users\benlu\source\repos\TestExport1\x64\Release\TestExport1.dll")]
        static public extern IntPtr CreateStarTrackerClass();

        [DllImport(@"C:\Users\benlu\source\repos\TestExport1\x64\Release\TestExport1.dll")]
        static public extern void DisposeStarTrackerClass(IntPtr pTestClassObject);

        [DllImport(@"C:\Users\benlu\source\repos\TestExport1\x64\Release\TestExport1.dll", EntryPoint = "?SolveFromImage@StarTrackerClass@@QEAAXPEBDPEAN@Z", CallingConvention = CallingConvention.ThisCall)]
        static public extern IntPtr SolveFromImage(IntPtr pClassObject, string str, double[] quaternion);

        [DllImport(@"C:\Users\benlu\source\repos\TestExport1\x64\Release\TestExport1.dll", EntryPoint = "?SolveFromArray@StarTrackerClass@@QEAAXPEAEHHHHPEAN@Z", CallingConvention = CallingConvention.ThisCall)]
        static public extern IntPtr SolveFromArray(IntPtr pClassObject, byte[] img_arr, int len, int w, int h, int num_layers, double[] quaternion);

        [DllImport(@"C:\Users\benlu\source\repos\TestExport1\x64\Release\TestExport1.dll", EntryPoint = "?SolveFromImageThread@StarTrackerClass@@QEAAXPEBDPEAN@Z", CallingConvention = CallingConvention.ThisCall)]
        static public extern IntPtr SolveFromImageThread(IntPtr pClassObject, string str, double[] quaternion);

        [DllImport(@"C:\Users\benlu\source\repos\TestExport1\x64\Release\TestExport1.dll", EntryPoint = "?SolveFromArrayThread@StarTrackerClass@@QEAAXPEAEHHHHPEAN@Z", CallingConvention = CallingConvention.ThisCall)]
        static public extern IntPtr SolveFromArrayThread(IntPtr pClassObject, byte[] img_arr, int len, int w, int h, int num_layers, double[] quaternion);
        /* --------- star tracker --------- */


        // Stores the intended action to allow it to be passed to the update function.
        // Is there a reason for the action being deferred until Update, or can they just call the same function?
        protected struct ActionFlags
        {
            public bool deactivateCamera;
            public bool nextCamera;
            public bool prevCamera;
            public bool zoomIn;
            public bool zoomOut;
        }
        protected static ActionFlags sActionFlags;

        #region Localization

        private static string locActivateCamera; //Activate Camera
        private static string locDeactivateCamera; //Deactivate Camera
        //private static string locNextCam; //Next Camera
        //private static string locPrevCam; //Previous Camera
        private static string locDisableCam; //Disable Camera
        private static string locEnableCam; //Enable Camera
        //private static string locZoomIn; //Zoom In
        //private static string locZoomOut; //Zoom Out

        private static string locOriginalControlPoint; //Original Control Point
        private static string locControlPointRestored; //Control Point restored to
        private static string locControlPointChanged; //Control Point changed to
        private static string locSwitchCamera; //Switching to camera
        private static string locVessel; //on vessel

        private static void LocalizationStringInit()
        {
            locActivateCamera = Localizer.Format("#autoLOC_HULL_EVT_001");
            locDeactivateCamera = Localizer.Format("#autoLOC_HULL_EVT_002");
            locEnableCam = Localizer.Format("#autoLOC_HULL_EVT_003");
            locDisableCam = Localizer.Format("#autoLOC_HULL_EVT_004");
            //locNextCam = Localizer.Format("#autoLOC_HULL_EVT_005");
            //locPrevCam = Localizer.Format("#autoLOC_HULL_EVT_006");
            //locZoomIn = Localizer.Format("#autoLOC_HULL_EVT_007");
            //locZoomOut = Localizer.Format("#autoLOC_HULL_EVT_008");

            locOriginalControlPoint = Localizer.Format("#autoLOC_HULL_MSG_001");
            locControlPointRestored = Localizer.Format("#autoLOC_HULL_MSG_002");
            locControlPointChanged = Localizer.Format("#autoLOC_HULL_MSG_003");
            locSwitchCamera = Localizer.Format("#autoLOC_HULL_MSG_004");
            locVessel = Localizer.Format("#autoLOC_HULL_MSG_005");
        }               

        #endregion

        #region Configuration

        public static KeyBinding CAMERA_NEXT = new KeyBinding(KeyCode.O);
        public static KeyBinding CAMERA_PREV = new KeyBinding(KeyCode.P);
        public static KeyBinding CAMERA_RESET = new KeyBinding(KeyCode.Escape);

        // Allows switching to the main camera.
        // The main camera will only be used if there aren't any camera parts to use.
        public static bool sAllowMainCamera = true;

        // If the main camera can be switched to, allows cycling to it via next/previous actions.
        public static bool sCycleToMainCamera = true;

        // Prevents cycling to cameras not on the active vessel.
        public static bool sCycleOnlyActiveVessel = false;

        // Whether to log things to the debug log.
        // This could be made into an integer that describes how many things to log.
        public static bool sDebugOutput = false;

        public static bool sDisplayCameraNameWhenSwitching = true;
        public static bool sDisplayVesselNameWhenSwitching = true;
        public static float sMessageDuration = 3.0f;

        #endregion

        #region Static Initialization

        protected static void DebugOutput(object o)
        {
   			if (true)
            {
                Debug.Log("HullCam: " + o.ToString());
            }
        }

        //protected static bool sInit = false;

        protected static void StaticInit()
        {
            // Commented out so that we can reload the config by reloading a save file rather than restarting KSP.
            /*
			if (sInit)
			{
				return;
			}
			sInit = true;
			*/

            try
            {
                foreach (ConfigNode cfg in GameDatabase.Instance.GetConfigNodes("HullCameraVDSConfig"))
                {
                    if (cfg.HasNode("CAMERA_NEXT"))
                    {
                        CAMERA_NEXT.Load(cfg.GetNode("CAMERA_NEXT"));
                    }
                    if (cfg.HasNode("CAMERA_PREV"))
                    {
                        CAMERA_PREV.Load(cfg.GetNode("CAMERA_PREV"));
                    }
                    if (cfg.HasNode("CAMERA_RESET"))
                    {
                        CAMERA_RESET.Load(cfg.GetNode("CAMERA_RESET"));
                    }
                    if (cfg.HasValue("CycleMainCamera"))
                    {
                        sCycleToMainCamera = Boolean.Parse(cfg.GetValue("CycleMainCamera"));
                    }
                    if (cfg.HasValue("AllowMainCamera"))
                    {
                        sAllowMainCamera = Boolean.Parse(cfg.GetValue("AllowMainCamera"));
                    }
                    if (cfg.HasValue("CycleOnlyActiveVessel"))
                    {
                        sCycleOnlyActiveVessel = Boolean.Parse(cfg.GetValue("CycleOnlyActiveVessel"));
                    }
                    if (cfg.HasValue("DebugOutput"))
                    {
                        sDebugOutput = Boolean.Parse(cfg.GetValue("DebugOutput"));
                    }

                    if (cfg.HasValue("DisplayCameraNameWhenSwitching"))
                    {
                        sDisplayCameraNameWhenSwitching = Boolean.Parse(cfg.GetValue("DisplayCameraNameWhenSwitching"));
                    }
                    if (cfg.HasValue("DisplayVesselNameWhenSwitching"))
                    {
                        sDisplayVesselNameWhenSwitching = Boolean.Parse(cfg.GetValue("DisplayVesselNameWhenSwitching"));
                    }
                    if (cfg.HasValue("MessageDuration"))
                    {
                        try
                        {
                            sMessageDuration = (float)Double.Parse(cfg.GetValue("MessageDuration"));
                        }
                        catch { }
                        if (sMessageDuration < 1 || sMessageDuration > 10)
                            sMessageDuration = 3;
                    }
                }
            }
            catch (Exception e)
            {
                print("Exception when loading HullCamera config: " + e.ToString());
            }

            Debug.Log(string.Format("CMC: {0} AMC: {1} COA: {2}", sCycleToMainCamera, sAllowMainCamera, sCycleOnlyActiveVessel));
        }

        #endregion

        static Part sOrigVesselTransformPart;

        protected static void SaveMainCamera()
        {
            DebugOutput("SaveMainCamera");

            sOrigParent = sCam.transform.parent;
            sOrigClip = Camera.main.nearClipPlane;
            sOrigFov = Camera.main.fieldOfView;
            sOrigPosition = sCam.transform.localPosition;
            sOrigRotation = sCam.transform.localRotation;
            if (sOrigVesselTransformPart == null)
            {
                sOrigVesselTransformPart = FlightGlobals.ActiveVessel.GetReferenceTransformPart();
                ScreenMessages.PostScreenMessage(locOriginalControlPoint + ": " + sOrigVesselTransformPart.partInfo.title);
            }
        }

        protected static void RestoreMainCamera()
        {
            DebugOutput("RestoreMainCamera");

            if (sCam != null)
            {
                sCam.transform.parent = sOrigParent;
                sCam.transform.localPosition = sOrigPosition;
                sCam.transform.localRotation = sOrigRotation;
                sCam.SetFoV(sOrigFov);
                sCam.ActivateUpdate();

                if (FlightGlobals.ActiveVessel != null && HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    //sCam.SetTarget(FlightGlobals.ActiveVessel.transform, FlightCamera.TargetMode.Transform);
                    sCam.SetTarget(FlightGlobals.ActiveVessel.transform, FlightCamera.TargetMode.Vessel);
                }

                sOrigParent = null;
            }
            if (sCurrentCamera != null)
            {
                sCurrentCamera.mt.SetCameraMode(CameraFilter.eCameraMode.Normal);
                sCurrentCamera.camActive = false;
            }
            sCurrentCamera = null;
            Camera.main.nearClipPlane = sOrigClip;

            /////////////////////////////////////

            if (sOrigVesselTransformPart != null)
            {
                if (GameSettings.MODIFIER_KEY.GetKey(false))
                {
                    FlightGlobals.ActiveVessel.SetReferenceTransform(sOrigVesselTransformPart, true);
                    ScreenMessages.PostScreenMessage(locControlPointRestored + " " + sOrigVesselTransformPart.partInfo.title);
                    sOrigVesselTransformPart = null;
                }
            }
            /////////////////////////////////////


        }

        public static void changeCameraMode()
        {
            DebugOutput("changeCameraMode");
            DebugOutput("yooo!");
            if (sCurrentCamera.cameraMode == 0)
            {
                sCurrentCamera.mt.SetCameraMode(CameraFilter.eCameraMode.Normal);
            }
            if (sCurrentCamera.cameraMode == 1)
            {
                sCurrentCamera.mt.SetCameraMode(CameraFilter.eCameraMode.DockingCam);
            }
            if (sCurrentCamera.cameraMode == 2)
            {
                sCurrentCamera.mt.SetCameraMode(CameraFilter.eCameraMode.BlackAndWhiteFilm);
            }
            if (sCurrentCamera.cameraMode == 3)
            {
                sCurrentCamera.mt.SetCameraMode(CameraFilter.eCameraMode.BlackAndWhiteLoResTV);
            }
            if (sCurrentCamera.cameraMode == 4)
            {
                sCurrentCamera.mt.SetCameraMode(CameraFilter.eCameraMode.BlackAndWhiteHiResTV);
            }
            if (sCurrentCamera.cameraMode == 5)
            {
                sCurrentCamera.mt.SetCameraMode(CameraFilter.eCameraMode.ColorFilm);
            }
            if (sCurrentCamera.cameraMode == 6)
            {
                sCurrentCamera.mt.SetCameraMode(CameraFilter.eCameraMode.ColorLoResTV);
            }
            if (sCurrentCamera.cameraMode == 7)
            {
                sCurrentCamera.mt.SetCameraMode(CameraFilter.eCameraMode.ColorHiResTV);
            }
            if (sCurrentCamera.cameraMode == 8)
            {
                sCurrentCamera.mt.SetCameraMode(CameraFilter.eCameraMode.NightVision);
            }
        }

        protected static void CycleCamera(int direction)
        {
            DebugOutput(String.Format("CycleMainCamera({0})", direction));

            // Find the next camera to switch to, deactivate the current camera and activate the new one.
            MuMechModuleHullCamera newCam = sCurrentCamera;

            // Iterates the number of cameras and returns as soon as a camera is chosen.
            // Then if no camera is chosen, restore main camera as a last-ditch effort.
            for (int i = 0; i < sCameras.Count + 1; i++)
            {

                // Check if cycle direction is referse and if the current cam is the first hullZcam
                if (direction == -1 && sCameras.IndexOf(sCurrentCamera) == 0)
                {
                    sCurrentCamera.camActive = false;
                    RestoreMainCamera();
                    return;
                }
                int nextCam = sCameras.IndexOf(newCam) + direction;
                if (nextCam >= sCameras.Count)
                {
                    if (sAllowMainCamera && sCycleToMainCamera)
                    {
                        if (sCurrentCamera != null)
                        {
                            sCurrentCamera.camActive = false;
                            RestoreMainCamera();
                        }
                        return;
                    }
                    nextCam = (direction > 0) ? 0 : sCameras.Count - 1;
                }
                newCam = sCameras[nextCam];

#if true
            
                if (newCam.vessel == FlightGlobals.ActiveVessel)
                {
                    if (GameSettings.MODIFIER_KEY.GetKey(false))
                    {
                        ModuleDockingNode mdn = newCam.part.FindModuleImplementing<ModuleDockingNode>();
                        if (mdn != null)
                        {
                            if (sOrigVesselTransformPart == null)
                            {
                                sOrigVesselTransformPart = FlightGlobals.ActiveVessel.GetReferenceTransformPart();
                                ScreenMessages.PostScreenMessage(locOriginalControlPoint + ": " + sOrigVesselTransformPart.partInfo.title);
                            }

                            newCam.part.SetReferenceTransform(mdn.controlTransform);
                            FlightGlobals.ActiveVessel.SetReferenceTransform(newCam.part, true);

                            ScreenMessages.PostScreenMessage(locControlPointChanged + " " + newCam.part.partInfo.title);
                        }
                    }
                }

#endif

                if (sCycleOnlyActiveVessel && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel != newCam.vessel)
                {
                    continue;
                }


                if (newCam.camEnabled && newCam.part.State != PartStates.DEAD)
                {
                    if (sCurrentCamera != null)
                    {
                        sCurrentCamera.camActive = false;
                    }
                    sCurrentCamera = newCam;
                    changeCameraMode();
                    sCurrentCamera.camActive = true;
                    IdentifyCamera();

                    return;
                }
            }
            // Failed to find a camera including cycling back to the one we started from. Default to main as a last-ditch effort.
            if (sCurrentCamera != null)
            {
                sCurrentCamera.camActive = false;
                sCurrentCamera = null;
                RestoreMainCamera();
            }
        }

        static void IdentifyCamera()
        {
            if (sCurrentCamera != null && sDisplayCameraNameWhenSwitching)
            {
                if (sDisplayVesselNameWhenSwitching)
                    ScreenMessages.PostScreenMessage(locSwitchCamera + ": " + sCurrentCamera.cameraName + " " + locVessel + " " + sCurrentCamera.vessel.GetDisplayName(), sMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                else
                    ScreenMessages.PostScreenMessage(locSwitchCamera + ": " + sCurrentCamera.cameraName, sMessageDuration, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        protected static void LeaveCamera()
        {
            DebugOutput("LeaveCamera");

            if (sCurrentCamera == null)
            {
                return;
            }
            if (sAllowMainCamera)
            {
                RestoreMainCamera();
            }
            else
            {
                DebugOutput(" 5");
                CycleCamera(1);
            }
        }

        protected void Activate()
        {
            DebugOutput("Activate");

            if (part.State == PartStates.DEAD)
            {
                return;
            }
            if (camActive)
            {
                if (sAllowMainCamera)
                {
                    RestoreMainCamera();
                }
                else
                {
                    DebugOutput(" 6");
                    CycleCamera(1);
                }
                return;
            }

            sCurrentCamera = this;
            camActive = true;
            changeCameraMode();


            Enabled = true; //from TST
            /* --------- star tracker --------- */
            //tutorial and code for importing c++ class to c#: https://www.codeproject.com/Articles/18032/How-to-Marshal-a-C-Class
            //DUMPBIN (to find mangled class function names) command only available in "Native Tools Command Prompt for VS 2019"
            //command: DUMPBIN /EXPORTS "C:\path\to\dll" => i.e. DUMPBIN /EXPORTS "C:\Users\benlu\source\repos\TestExport1\x64\Release\TestExport1.dll"

            pSTClass = CreateStarTrackerClass();
 
            //quaternion = new double[4] { 0, 0, 0, 0 };

            drawFS();
            //star_img_color = _texture2DFullSze.EncodeToPNG();
            star_img_color = _texture2DFullSze.EncodeToJPG();
            //Screen.width, Screen.height

            //byte[] star_img_color = null;
            //var rawDataPtr = _texture2DFullSze.GetRawTextureData<byte>();

            //star_img_color = rawDataPtr.ToArray();

            //byte[] star_img_color = _texture2DFullSze.GetRawTextureData();
            //Color[] star_img = star_img_color.grayscale;

            DebugOutput(star_img_color[0]);
            DebugOutput(star_img_color[10000]);
            DebugOutput(star_img_color[60000]);

            DebugOutput(star_img_color.Length);
            DebugOutput(_texture2DFullSze.width);
            DebugOutput(_texture2DFullSze.height);


            len = star_img_color.Length;
            w = _texture2DFullSze.width;
            h = _texture2DFullSze.height;
            //var STtask = StarTrackerSolve(pSTClass, star_img_color, len, w, h, quaternion);
            //StarTrackerSolve();

            //Task solver_task = new Task(() => SolveFromArray(pSTClass, star_img_color, len, w, h, 3, quaternion));
            //solver_task.Start();
            //solver_task.Wait();
            //await solver;

            /* crashes game b/c memory access violation from python :(
            var tcs = new TaskCompletionSource<double[]>();
            Task.Run(async () =>
            {
                DebugOutput("Task started");
                tcs.SetResult(await StarTrackerSolver(pSTClass, star_img_color, len, w, h, quaternion));
                DebugOutput("Task stopped");
            });

            tcs.Task.ConfigureAwait(true).GetAwaiter().OnCompleted(() =>
            {
                DebugOutput("Task completed");
                quaternion = tcs.Task.Result;
                VecShow(quaternion, 8, 12);
            });
            */

            //_STthread = new Thread(StarTrackerSolver);
            //if (!_STthread.IsAlive)
            //    _STthread.Start();

            SolveFromArray(pSTClass, star_img_color, star_img_color.Length, _texture2DFullSze.width, _texture2DFullSze.height, 3, quaternion);
            VecShow(quaternion, 8, 12);

            SolveFromImage(pSTClass, "C:\\Users\\benlu\\source\\repos\\c_sharp_py_test\\test_images\\ksp_star_img1.png", quaternion);
            VecShow(quaternion, 8, 12);

            SolveFromImage(pSTClass, "C:\\Users\\benlu\\source\\repos\\c_sharp_py_test\\test_images\\ksp_star_img2.png", quaternion);
            VecShow(quaternion, 8, 12);

            SolveFromArrayThread(pSTClass, star_img_color, star_img_color.Length, _texture2DFullSze.width, _texture2DFullSze.height, 3, quaternion);
            //SolveFromImageThread(pSTClass, "C:\\Users\\benlu\\source\\repos\\c_sharp_py_test\\test_images\\ksp_star_img1.png", quaternion);
            VecShow(quaternion, 8, 12);

            DebugOutput("saving image...");
            saveToFile("star_cam_img");
            DebugOutput("Done.");
            //Enabled = false;

            /* --------- star tracker --------- */
        }

        protected void DirtyWindow()
        {
            foreach (UIPartActionWindow w in GameObject.FindObjectsOfType(typeof(UIPartActionWindow)).Where(w => ((UIPartActionWindow)w).part == part))
            {
                w.displayDirty = true;
            }
        }

#region Events

        // Note: Events show in the part menu in flight.

        [KSPEvent(guiActive = true, guiName = "#autoLOC_HULL_EVT_001")] //Activate Camera
        public void ActivateCamera()
        {
            Activate();
            Events["ActivateCamera"].guiName = camActive ? locDeactivateCamera : locActivateCamera;
        }

        [KSPEvent(guiActive = true, guiName = "#autoLOC_HULL_EVT_004")] //Disable Camera
        public void EnableCamera()
        {
            if (part.State == PartStates.DEAD)
            {
                return;
            }

            if (camActive)
            {
                LeaveCamera();
                camActive = false;
                Enabled = false;
                DisposeStarTrackerClass(pSTClass);
                pSTClass = IntPtr.Zero;
            }

            camEnabled = !camEnabled;
            Events["EnableCamera"].guiName = camEnabled ? locDisableCam : locEnableCam;

            DirtyWindow();
        }

#endregion

#region Actions

        // Note: Actions are available to action groups.

        [KSPAction("#autoLOC_HULL_EVT_001")] //Activate Camera
        public void ActivateCameraAction(KSPActionParam ap)
        {
            Activate();
        }

        [KSPAction("#autoLOC_HULL_EVT_002")] //Deactivate Camera
        public void DeactivateCameraAction(KSPActionParam ap)
        {
            if (part.State == PartStates.DEAD)
            {
                return;
            }

            if (camActive)
            {
                LeaveCamera();
                camActive = false;
                Enabled = false;
                DisposeStarTrackerClass(pSTClass);
                pSTClass = IntPtr.Zero;
            }

            camEnabled = !camEnabled;
            Events["EnableCamera"].guiName = camEnabled ? locDeactivateCamera : locActivateCamera;
            DirtyWindow();
        }

        [KSPAction("#autoLOC_HULL_EVT_005")] //Next Camera
        public void NextCameraAction(KSPActionParam ap)
        {
            sActionFlags.nextCamera = true;
        }

        [KSPAction("#autoLOC_HULL_EVT_006")] //Previous Camera
        public void PreviousCameraAction(KSPActionParam ap)
        {
            sActionFlags.prevCamera = true;
        }

#endregion

#region Callbacks

        void DebugList()
        {

            foreach (PartModule s in sCameras)
            {
                Debug.Log(s);
            }

        }

        public void LateUpdate()
        {
            // In the VAB
            if (vessel == null)
            {
                return;
            }

            if (sCurrentHandler == null)
            {
                sCurrentHandler = this;
            }


            if (sCurrentCamera != null)
            {
                if (sCurrentCamera.vessel != FlightGlobals.ActiveVessel)
                {
                    Vector3d activeVesselPos = FlightGlobals.ActiveVessel.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()) + FlightGlobals.ActiveVessel.orbit.referenceBody.position;
                    Vector3d targetVesselPos = sCurrentCamera.vessel.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()) + sCurrentCamera.vessel.orbit.referenceBody.position;

                    sCameraDistance = (activeVesselPos - targetVesselPos).magnitude;

                    if (sCameraDistance >= 2480)
                    {
                        LeaveCamera();
                    }
                }
            }

            if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.Flight)
            {
                return;
            }

#if true
            if (sActionFlags.deactivateCamera || CAMERA_RESET.GetKeyDown() || GameSettings.CAMERA_NEXT.GetKeyDown())
            {
                LeaveCamera();
                sActionFlags.deactivateCamera = false;
            }
#endif
            if (sActionFlags.nextCamera || (sCurrentHandler == this && CAMERA_NEXT.GetKeyDown()))
            {
                CycleCamera(1);
                sActionFlags.nextCamera = false;
            }
#if true
            if (sActionFlags.prevCamera || (sCurrentHandler == this && CAMERA_PREV.GetKeyDown()))
            {
                if (sCameras.IndexOf(sCurrentCamera) != -1)
                {
                    CycleCamera(-1);
                    sActionFlags.prevCamera = false;
                }
                else
                {
                    DebugOutput(" 2");
                    CycleCamera(sCameras.Count);
                }
            }
#endif
            //_galaxyCamFS.reset();
            //_scaledSpaceCamFS.reset();
            //_nearCamFS.reset();

            //Transform pTransform = (cameraTransformName.Length > 0) ? part.FindModelTransform(cameraTransformName) : part.transform;

            //_galaxyCamFS.set_pos_rot(true, true, true, -90, 0, 0, cameraForward, cameraUp, cameraPosition, pTransform); //good rot, slight translation issue
            //_scaledSpaceCamFS.set_pos_rot(true, false, false, -90, 0, 0, cameraForward, cameraUp, cameraPosition, pTransform); //good
            //_nearCamFS.set_pos_rot(true, false, false, -90, 0, 0, cameraForward, cameraUp, cameraPosition, pTransform); //idk


            /*
	        if (sCurrentCamera == this)
			{
	            if (Input.GetKeyUp(KeyCode.Keypad8))
	            {
	                cameraPosition += cameraUp * 0.1f;
	            }
	            if (Input.GetKeyUp(KeyCode.Keypad2))
	            {
	                cameraPosition -= cameraUp * 0.1f;
	            }
	            if (Input.GetKeyUp(KeyCode.Keypad6))
	            {
	                cameraPosition += cameraForward * 0.1f;
	            }
	            if (Input.GetKeyUp(KeyCode.Keypad4))
	            {
	                cameraPosition -= cameraForward * 0.1f;
	            }
	            if (Input.GetKeyUp(KeyCode.Keypad7))
	            {
	                cameraClip += 0.05f;
	            }
	            if (Input.GetKeyUp(KeyCode.Keypad1))
	            {
	                cameraClip -= 0.05f;
	            }
	            if (Input.GetKeyUp(KeyCode.Keypad9))
	            {
	                cameraFoV += 5;
	            }
	            if (Input.GetKeyUp(KeyCode.Keypad3))
	            {
	                cameraFoV -= 5;
	            }
	            if (Input.GetKeyUp(KeyCode.KeypadMinus))
	            {
	                print("Position: " + cameraPosition + " - Clip = " + cameraClip + " - FoV = " + cameraFoV);
	            }
	        } */
            doOnUpdate();
        }

        //public override void OnUpdate()
        void doOnUpdate()
        {
            //base.OnUpdate();

            if (vessel == null)
            {
                return;
            }


            if (!camActive || CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.Flight)
                return;

            if (sActionFlags.zoomIn || GameSettings.ZOOM_IN.GetKeyDown() || (Input.GetAxis("Mouse ScrollWheel") > 0))
            {
                cameraFoV = Mathf.Clamp(cameraFoV / cameraZoomMult, cameraFoVMin, cameraFoVMax);
                sActionFlags.zoomIn = false;
            }
            if (sActionFlags.zoomOut || GameSettings.ZOOM_OUT.GetKeyDown() || (Input.GetAxis("Mouse ScrollWheel") < 0))
            {
                cameraFoV = Mathf.Clamp(cameraFoV * cameraZoomMult, cameraFoVMin, cameraFoVMax);
                sActionFlags.zoomOut = false;
            }
            if (MapView.MapIsEnabled)
            {
                cameraFoV = Mathf.Clamp(cameraFoV / cameraZoomMult, cameraFoVMin, cameraFoVMax);
            }


        }

        /* This function: 
         * - Calls the C++ function that uses CPython to communicate to Python
         * - The Python function gives the image to the Tetra3 solver
         *      - The Python function is also threaded, but there is a join() that waits until the thread is complete
         *      - This ensures that a quaternion solution was calculated
         * - A quaternion is returned by the Python function
         * - The C++ function copies the quaternion from Python into the quaternion parameter passed below
         * 
         * No Unity API is used here, so it's safe to thread this function.
         */
        private void StarTrackerSolver() //IntPtr pClass, byte[] star_img_color, int length, int width, int height, double[] q
        {
            //SolveFromImageThread(pSTClass, "C:\\Users\\benlu\\source\\repos\\c_sharp_py_test\\test_images\\ksp_star_img1.png", quaternion);
            SolveFromArrayThread(pSTClass, star_img_color, len, w, h, 3, quaternion);

            solverDone = true;
        }

        public void FixedUpdate()
        {
            // In the VAB
            if (vessel == null)
            {
                return;
            }
            // following "if" added by jbb
            if (MapView.MapIsEnabled)
            {
                return;
            }

            if (part.State == PartStates.DEAD)
            {
                if (camActive)
                {
                    LeaveCamera();
                }
                Events["ActivateCamera"].guiActive = false;
                Events["EnableCamera"].guiActive = false;
                camEnabled = false;
                camActive = false;
                Enabled = false;
                DirtyWindow();
                CleanUp();
                DisposeStarTrackerClass(pSTClass);
                pSTClass = IntPtr.Zero;
                return;
            }

            if (!sAllowMainCamera && sCurrentCamera == null && !vessel.isEVA)
            {
                camActive = true;
                sCurrentCamera = this;
            }

            if (!camActive)
            {
                return;
            }

            if (!camEnabled)
            {
                DebugOutput(" 3");
                CycleCamera(1);
                return;
            }

            if (sCam == null)
            {
                sCam = FlightCamera.fetch;
                // No idea if fetch returns null in normal operation (i.e. when there isn't a game breaking bug going on already)
                // but the original code had similar logic.
                if (sCam == null)
                {
                    return;
                }
            }

            // Either we haven't set sOriginParent, or we've nulled it when restoring the main camera, so we save it again here.
            if (sOrigParent == null)
            {
                SaveMainCamera();
            }


            //sCam.SetTarget(null);
            sCam.SetTargetNone();
            sCam.transform.parent = (cameraTransformName.Length > 0) ? part.FindModelTransform(cameraTransformName) : part.transform;
            sCam.DeactivateUpdate();
            sCam.transform.localPosition = cameraPosition;



            sCam.transform.localRotation = Quaternion.LookRotation(cameraForward, cameraUp);
            sCam.SetFoV(cameraFoV);
            Camera.main.nearClipPlane = cameraClip;


            /* --------- star tracker --------- */
            /*
            if (!ST_sample)
            {
                ST_sample = true;
                prev_time = Time.time;
                //if(_STthread.IsAlive) 
            }

            if ((Time.time - prev_time) >= sample_time && ST_sample == true)
            {
                ST_sample = false;

                //star tracker
                prev_time = Time.time;
                //DebugOutput(prev_time);
                //pSTClass = CreateStarTrackerClass();
                //DebugOutput((Time.time - prev_time));

                //quaternion = new double[4] { 0, 0, 0, 0 };

                //float dt;

                //prev_time = Time.time;
                //drawFS();
                //dt = Time.time - prev_time;
                //DebugOutput(dt);

                //prev_time = Time.time;
                //star_img_color = _texture2DFullSze.EncodeToJPG();
                //dt = Time.time - prev_time;
                //DebugOutput(dt);

                //prev_time = Time.time;
                //SolveFromArray(pSTClass, star_img_color, star_img_color.Length, _texture2DFullSze.width, _texture2DFullSze.height, 3, quaternion);
                //SolveFromImageThread(pSTClass, "C:\\Users\\benlu\\source\\repos\\c_sharp_py_test\\test_images\\ksp_star_img2.png", quaternion);
                //dt = Time.time - prev_time;
                //DebugOutput(dt);

                //VecShow(quaternion, 8, 12);

                //prev_time = Time.time;
                //DisposeStarTrackerClass(pSTClass);
                //dt = Time.time - prev_time;
                //DebugOutput(dt);
                //pSTClass = IntPtr.Zero;

                
                //drawFS();
                //star_img_color = _texture2DFullSze.EncodeToJPG();
                //SolveFromArray(pSTClass, star_img_color, star_img_color.Length, _texture2DFullSze.width, _texture2DFullSze.height, 3, quaternion);
                //VecShow(quaternion, 8, 12);
                
            }
            */

            //once a solution is done, show the quaternion solution and begin the next solution 
            if (solverDone)
            {
                solverDone = false;
                VecShow(quaternion, 8, 12);

                drawFS(); //render the fullscreen texture
                star_img_color = _texture2DFullSze.EncodeToJPG(); //JPG encoding is faster than PNG
                _STthread = new Thread(new ThreadStart(StarTrackerSolver)); //begin a thread to solve the image to prevent KSP from lagging
                _STthread.Start(); //start the thread
            }
            /* --------- star tracker --------- */


            // If we're only allowed to cycle the active vessel and viewing through a camera that's not the active vessel any more, then cycle to one that is.
            if (sCycleOnlyActiveVessel && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel != vessel)
            {
                DebugOutput(" 4");
                CycleCamera(1);
            }

            base.OnFixedUpdate();
        }

        private void onGameSceneLoadRequested(GameScenes gameScene)
        {
            if (sCurrentCamera != null)
            {
                print("Clearing camera list");
                sCameras.Clear();
                sCurrentCamera.mt.SetCameraMode(CameraFilter.eCameraMode.Normal);
                sCurrentCamera = null;
            }
        }

        public override void OnStart(StartState state)
        {
            LocalizationStringInit();
            StaticInit();

            GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoadRequested);


            // Reading camEnabled right away, so is something setting this value?
            // KSPFields are saving game state.
            // So this must also be called when we load the game too.
            if ((state != StartState.None) && (state != StartState.Editor))
            {
                if (!sCameras.Contains(this))
                {
                    sCameras.Add(this);
                    this.cameraFoV = this.cameraFoVMax;
                    DebugOutput(String.Format("Added, now {0}", sCameras.Count));
                }
                vessel.OnJustAboutToBeDestroyed += CleanUp;
            }
            part.OnJustAboutToBeDestroyed += CleanUp;
            part.OnEditorDestroy += CleanUp;

            if (part.State == PartStates.DEAD)
            {
                Events["ActivateCamera"].guiActive = false;
                Events["EnableCamera"].guiActive = false;
            }
            else
            {
                Events["EnableCamera"].guiName = camEnabled ? locDisableCam : locEnableCam;
            }

            /*
            part.CoMOffset = part.attachNodes[0].position;

            _baseTransform = FindChildRecursive(transform, baseTransformName);
            cameraTransform = FindChildRecursive(transform, cameraTransformName);
            _lookTransform = FindChildRecursive(transform, lookTransformName);
            _animationTransform = FindChildRecursive(transform, animationTransformName);
            zeroRotation = cameraTransform.localRotation;
            if (state != StartState.Editor)
            {
                _camera = cameraTransform.gameObject.AddComponent<TSTCameraModule>();
                _camera.telescopeReference = this;
            }
            */

                base.OnStart(state);
        }

        public void CleanUp()
        {
            DebugOutput("Cleanup");
            if (sCurrentHandler == this)
            {
                DebugOutput("sCurrentHandler is this");
                sCurrentHandler = null;
            }
            if (sCurrentCamera == this)
            {
                DebugOutput("If it's the current camera");
                // On destruction, revert to main camera so we're not left hanging.
                LeaveCamera();

            }
            if (sCameras.Contains(this))
            {
                sCameras.Remove(this);
                DebugOutput(String.Format("Removed, now {0}", sCameras.Count));
                // This happens when we're saving and reloading.

                if (sCameras.Count < 1 && sOrigParent != null && !this.camActive)
                {
                    DebugOutput("Set sCurrentCamera to null");
                    sCurrentCamera = null;
                    DebugOutput("RestoreMainCamera");
                    RestoreMainCamera();
                    DebugOutput("RestoringMainCamera");
                }
            }
        }

        public void onVesselDestroy()
        {
            if (sCameras.Contains(this))
            {
                DebugOutput("onVesselDestroy");
                sCameras.Remove(this);
                DebugOutput(String.Format("Removed, now {0}", sCameras.Count));
                LeaveCamera();
            }
        }

        public void OnDestroy()
        {
            DebugOutput("OnDestroy");
            CleanUp();
        }

        private void Remove()
        {
            DebugOutput("On Unload");
            LeaveCamera();
        }
    #endregion

//-----------------------------------------------------
#region TarsierSpaceTelescope Code

        public static Camera findCameraByName(string camera)
        {
            return Camera.allCameras.FirstOrDefault(cam => cam.name == camera);
        }

        private void setupRenderTexture()
        {
            DebugOutput("Setting Up Render Texture");
            if (_renderTexture)
            {
                _galaxyCam.renderTarget.Release();
                _scaledSpaceCam.renderTarget.Release();
                _nearCam.renderTarget.Release();
                _renderTexture.Release();
            }

            _renderTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _renderTexture.Create();
            if (_renderTextureFS)
            {
                DebugOutput("setting up fullscreen texture render");
                _galaxyCamFS.renderTarget.Release();
                _scaledSpaceCamFS.renderTarget.Release();
                _nearCamFS.renderTarget.Release();
                _renderTextureFS.Release();
            }
            _renderTextureFS = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _renderTextureFS.antiAliasing = GameSettings.ANTI_ALIASING;
            _renderTextureFS.Create();
            _texture2D = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false, false);
            _texture2DFullSze = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false, false);
            _galaxyCam.renderTarget = _renderTexture;
            _nearCam.renderTarget = _renderTexture;
            _galaxyCamFS.renderTarget = _renderTextureFS;
            _scaledSpaceCamFS.renderTarget = _renderTextureFS;
            _nearCamFS.renderTarget = _renderTextureFS;
            DebugOutput("Finish Setting Up Render Texture");
        }


        public void Start()
        {
            //in VAB
            if (vessel == null)
                return;

            DebugOutput("Setup Cameras");
            _galaxyCam = new CameraHelper(gameObject, findCameraByName("GalaxyCamera"), _renderTexture, 17, false);
            _scaledSpaceCam = new CameraHelper(gameObject, findCameraByName("Camera ScaledSpace"), _renderTexture, 18, false);
            _nearCam = new CameraHelper(gameObject, findCameraByName("Camera 00"), _renderTexture, 20, true);

            _galaxyCamFS = new CameraHelper(gameObject, findCameraByName("GalaxyCamera"), _renderTextureFS, 21, false);
            _scaledSpaceCamFS = new CameraHelper(gameObject, findCameraByName("Camera ScaledSpace"), _renderTextureFS, 22, false);
            _nearCamFS = new CameraHelper(gameObject, findCameraByName("Camera 00"), _renderTextureFS, 24, true);

            setupRenderTexture();

            _galaxyCam.reset();
            _scaledSpaceCam.reset();
            _nearCam.reset();

            _galaxyCamFS.reset();
            _scaledSpaceCamFS.reset();
            _nearCamFS.reset();

            TanRadDfltFOV = Mathf.Tan(Mathf.Deg2Rad * CameraHelper.DEFAULT_FOV);

            DebugOutput($"skyBoxCam CullingMask = {_scaledSpaceCam.camera.cullingMask}, camera.nearClipPlane = {_scaledSpaceCam.camera.nearClipPlane}, camera.farClipPlane = {_scaledSpaceCam.camera.farClipPlane}");
            DebugOutput($"nearCam CullingMask = {_nearCam.camera.cullingMask}, camera.farClipPlane = {_nearCam.camera.farClipPlane}");

            DebugOutput("Camera setup complete");


        }

        internal Texture2D drawFS() // Same as Draw() but for fullscreencameras
        {
            DebugOutput("drawFS()");
            activeRT = RenderTexture.active;
            RenderTexture.active = _renderTextureFS;
            _galaxyCamFS.reset();
            _scaledSpaceCamFS.reset();
            _nearCamFS.reset();

            Transform pTransform = (cameraTransformName.Length > 0) ? part.FindModelTransform(cameraTransformName) : part.transform;

            _galaxyCamFS.set_pos_rot(true, true, true, -90, 0, 0, cameraForward, cameraUp, cameraPosition, pTransform); //good rot, slight translation issue
            _scaledSpaceCamFS.set_pos_rot(true, false, false, -90, 0, 0, cameraForward, cameraUp, cameraPosition, pTransform); //good
            _nearCamFS.set_pos_rot(true, false, false, -90, 0,0, cameraForward, cameraUp, cameraPosition, pTransform); //idk


            //Render the Skybox
            //Calculate exposure setting for skybox
            exposure = CalculateExposure(ScaledSpace.ScaledToLocalSpace(_scaledSpaceCam.position));
            origColor = skyboxRenderers[0].sharedMaterial.GetColor(PropertyIDs._Color);
            for (int i = 0; i < skyboxRenderers.Length; i++)
            {
                skyboxRenderer = skyboxRenderers[i];
                Color color = Color.Lerp(GalaxyCubeControl.Instance.minGalaxyColor, GalaxyCubeControl.Instance.maxGalaxyColor, exposure);
                skyboxRenderer.material.SetColor(PropertyIDs._Color, color);
            }

            _galaxyCamFS.camera.Render();

            //set exposure back to what it was
            for (int i = 0; i < skyboxRenderers.Length; i++)
            {
                Renderer sr = skyboxRenderers[i];
                sr.material.SetColor(PropertyIDs._Color, origColor);
            }

            //Render ScaledSpace
            //Render Atmospheres
            foreach (AtmosphereFromGround afg in atmospheres)
            {
                //cache the atmosphere info
                if (!atmoInfo.ContainsKey(afg))
                    atmoInfo.Add(afg, new Vector4(afg.cameraPos.x, afg.cameraPos.y, afg.cameraPos.z, afg.cameraHeight));
                else
                    atmoInfo[afg] = new Vector4(afg.cameraPos.x, afg.cameraPos.y, afg.cameraPos.z, afg.cameraHeight);

                //set the atmosphere's camera to the scaled camera
                afg.cameraPos = _scaledSpaceCam.position;
                afg.cameraHeight = (float)_scaledSpaceCam.position.magnitude;
                afg.cameraHeight2 = afg.cameraHeight * afg.cameraHeight;
                afg.SetMaterial(false);
            }

            // KSP/Scaled Space/Planet Fader turn on the renderers for the planet faders
            foreach (ScaledSpaceFader s in scaledSpaceFaders)
                s.r.enabled = true;
            _scaledSpaceCamFS.camera.clearFlags = CameraClearFlags.Depth; //clear only the depth buffer
            _scaledSpaceCamFS.camera.farClipPlane = 3e15f; //set clipping plane distance            
            _scaledSpaceCamFS.camera.Render(); // render the ScaledSpaceCam

            //reset atmoInfo
            foreach (AtmosphereFromGround afg in atmospheres)
            {
                //reset the atmosphere info from the cache
                if (atmoInfo.ContainsKey(afg))
                {
                    Vector4 info = atmoInfo[afg];
                    afg.cameraPos = new Vector3(info.x, info.y, info.z);
                    afg.cameraHeight = info.z;
                    afg.cameraHeight2 = info.z * info.z;
                    afg.SetMaterial(false);
                }
            }

            _nearCamFS.camera.Render(); // render camera 00

            _texture2DFullSze.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            _texture2DFullSze.Apply();
            RenderTexture.active = activeRT;

            DebugOutput("end drawFS()");

            return _texture2DFullSze;
        }
        public static Texture2D texture;
        public void saveToFile(string fileName) // Save Image to filesystem
        {
            string lgefileName = fileName + "Large.png";
            //fileName += ".png";
            //byte[] data = _texture2D.EncodeToPNG();
            drawFS();
            byte[] dataFS = _texture2DFullSze.EncodeToPNG();
            DebugOutput($"FS texture PNG data: {dataFS}");

            if (File.Exists<MuMechModuleHullCamera>(lgefileName))  
                File.Delete<MuMechModuleHullCamera>(lgefileName);

            using (FileStream file = File.Open<MuMechModuleHullCamera>(lgefileName, FileMode.CreateNew))
            {
                file.Write(dataFS, 0, dataFS.Length);
            }
        }

        private float CalculateExposure(Vector3d cameraWorldPos)
        {
            DebugOutput("CalculateExposure()");
            staticPressure = (float)(FlightGlobals.getStaticPressure(cameraWorldPos) * KPtoAtms);
            return Mathf.Lerp(SkyboxExposure, 0f, staticPressure);
        }

        private bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                _galaxyCam.enabled = value;
                _scaledSpaceCam.enabled = value;
                _nearCam.enabled = value;
                skyboxRenderers = (from Renderer r in (FindObjectsOfType(typeof(Renderer)) as IEnumerable<Renderer>) where (r.name == "XP" || r.name == "XN" || r.name == "YP" || r.name == "YN" || r.name == "ZP" || r.name == "ZN") select r).ToArray();
                scaledSpaceFaders = FindObjectsOfType<ScaledSpaceFader>();
                atmospheres = FindObjectsOfType<AtmosphereFromGround>();
            }
        }

        public class CameraHelper
        {
            public CameraHelper(GameObject parent, Camera copyFrom, RenderTexture renderTarget, float depth, bool attachToParent) //possibility of adding component position as constructor args to get rid of -90deg rotation
            {
                _copyFrom = copyFrom;
                _renderTarget = renderTarget;
                _parent = parent;
                _go = new GameObject();
                _go.name = "HC_" + copyFrom.name;
                _camera = _go.AddComponent<Camera>();
                _depth = depth;
                _attachToParent = attachToParent;
                _camera.enabled = false;
                _camera.targetTexture = _renderTarget;
            }

            public const float DEFAULT_FOV = 20f;
            private Camera _camera;
            public Camera camera
            {
                get { return _camera; }
            }

            private Camera _copyFrom;
            private float _depth;
            private GameObject _go;
            private GameObject _parent;
            private bool _attachToParent;

            private RenderTexture _renderTarget;
            public RenderTexture renderTarget
            {
                get { return _renderTarget; }
                set
                {
                    _renderTarget = value;
                    _camera.targetTexture = _renderTarget;
                }
            }

            private float _fov = DEFAULT_FOV;
            public float fov
            {
                get { return _fov; }
                set
                {
                    _fov = value;
                    _camera.fieldOfView = _fov;
                }
            }

            public bool enabled
            {
                get { return _camera.enabled; }
                set { _camera.enabled = value; }
            }

            public Vector3d position
            {
                get { return _go.transform.position; }
                set { _go.transform.position = position; }
            }

            public void set_pos_rot(bool parent_override, bool change_pos, bool part_transform, int x, int y, int z, Vector3 cameraForward, Vector3 cameraUp, Vector3 cameraPosition, Transform pTransform)
            {
                _camera.CopyFrom(_copyFrom);
                if (part_transform)
                {
                    _go.transform.parent = pTransform;
                }
                else
                {
                    _go.transform.parent = _parent.transform;
                }
                if (change_pos)
                {
                    _go.transform.localPosition = cameraPosition;
                }
                else
                {
                    _go.transform.localPosition = Vector3.zero;
                }

                if (parent_override)
                {
                    _go.transform.localEulerAngles = new Vector3(x, y, z);
                    //_go.transform.localRotation = Quaternion.LookRotation(cameraForward, cameraUp);
                }
                else if (_attachToParent)
                {
                    //_go.transform.localEulerAngles = new Vector3(x, y, z);
                    _go.transform.localRotation = Quaternion.LookRotation(cameraForward, cameraUp);
                    
                }
                else
                {
                    _go.transform.rotation = _parent.transform.rotation;
                }

                _camera.rect = new Rect(0, 0, 1, 1);
                _camera.depth = _depth;
                _camera.fieldOfView = _fov;
                _camera.targetTexture = _renderTarget;
                _camera.enabled = enabled;
            }


            public void reset() //add rotate method to fix camera being 90degs apart from rendered texture
            {
                _camera.CopyFrom(_copyFrom);
                if (_attachToParent)
                {
                    
                    _go.transform.parent = _parent.transform;
                    _go.transform.localPosition = Vector3.zero;
                    _go.transform.localEulerAngles = new Vector3(0, 0, 0); //Vector3.zero; -90

                    /*
                    sCam.transform.parent = (cameraTransformName.Length > 0) ? part.FindModelTransform(cameraTransformName) : part.transform;
                    sCam.DeactivateUpdate();
                    sCam.transform.localPosition = cameraPosition;

                    sCam.transform.localRotation = Quaternion.LookRotation(cameraForward, cameraUp);
                    */
                }
                else
                {
                    _go.transform.rotation = _parent.transform.rotation;
                }
                _camera.rect = new Rect(0, 0, 1, 1);
                _camera.depth = _depth;
                _camera.fieldOfView = _fov;
                _camera.targetTexture = _renderTarget;
                _camera.enabled = enabled;
            }
        }

        public static Transform FindChildRecursive(Transform parent, string name)
        {
            return parent.gameObject.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == name);
        }

        #endregion

        //Star Tracker - Helper function that prints a vector of any size
        public static void VecShow(double[] vec, int dec, int wid)
        {
            string msg;
            string left_bracket = "[";
            msg = left_bracket.PadRight(3);
            for (int i = 0; i < vec.Length; ++i)
                msg += vec[i].ToString("F" + dec).PadRight(wid);
            msg += "]";
            Debug.Log("HullCam: " + msg);
        }
    }
    
}