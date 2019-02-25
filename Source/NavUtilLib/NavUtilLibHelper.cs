﻿//NavUtilities by kujuman, © 2014. All Rights Reserved.

using System;
using UnityEngine;
using KSP;
using UnityEngine.UI;
using NavInstruments.NavUtilLib;
using var = NavInstruments.NavUtilLib.GlobalVariables;
//using ToolbarWrapper;


namespace NavInstruments.NavUtilLib
{
    [KSPAddon(KSPAddon.Startup.Flight, false)] //used to start up in flight, and be false
    public class NavUtilLibApp : MonoBehaviour
    {
        private void OnGUI()
        {
            //Log.detail("NavUtils: OnGUI()");


            if (NavUtilLib.GlobalVariables.Settings.isKSPGUIActive) // will hide GUI is F2 is pressed
            {
                if (NavUtilLib.GlobalVariables.Settings.hsiState) OnDraw();
                if (NavUtilLib.SettingsGUI.isActive) NavUtilLib.SettingsGUI.OnDraw();
                if (NavUtilLib.GlobalVariables.Settings.rwyEditorState) NavUtilGUI.RunwaysEditor.OnDraw();
				if (RunwayListGUI.isActive)	RunwayListGUI.OnDraw();
            }
        }


        //this class is to help load textures via GameDatabase since we cannot use static classes

        NavUtilLibApp app;

        KSP.UI.Screens.ApplicationLauncherButton appButton;

        public bool isHovering = false;

        //private bool visible = false;

        //RUIPanelTabGroup pTG;

        //public GameObject anchor;

        //public GameObject cascadeBody;
        //public GameObject cascadeFooter;
        //public GameObject cascadeHeader;

        //public RUICascadingList cascadingList;

        //public UIButton hoverComponent;

        //public int maxHeight = 200;
        //public int minHeight = 100;

        //public UIInteractivePanel panel;

        //private System.Collections.Generic.List<bool> bList;




		private IButton toolbarButton = null;
        private Rect windowPosition;
        private RenderTexture rt;

        private bool rwyHover = false;
        private bool gsHover = false;
        private bool closeHover = false;

		private Vector3 originalNavBallWaypointSize = Vector3.zero;
		private Vector3 originalIVANavBallWaypointSize = Vector3.zero;

        public void displayHSI()
        {
            Log.detail("NavUtils: NavUtilLibApp.displayHSI()");

            if (!NavUtilLib.GlobalVariables.Settings.hsiState)
            {
                Activate(true);
                NavUtilLib.GlobalVariables.Settings.hsiState = true;
                Log.detail("NavUtils: hsiState = " + NavUtilLib.GlobalVariables.Settings.hsiState);
            }
            else
            {
                Activate(false);

                NavUtilLib.GlobalVariables.Settings.hsiState = false;

                Log.detail("NavUtils: hsiState = " + NavUtilLib.GlobalVariables.Settings.hsiState);
            }
        }



		public void Activate(bool state)
		{
            Log.detail("NavUtils: NavUtilLibApp.Activate()");

            if (state)
            {
                rt = new RenderTexture(640, 640, 24, RenderTextureFormat.ARGB32);
                rt.Create();

                Log.detail("NavUtil: Starting systems...");
                if (!var.Settings.navAidsIsLoaded)
                    var.Settings.loadNavAids();

                if (!var.Materials.isLoaded)
                    var.Materials.loadMaterials();

                //load settings to config
                //ConfigLoader.LoadSettings(var.Settings.settingsFileURL);

                //ConfigureCamera();
                windowPosition.x = NavUtilLib.GlobalVariables.Settings.hsiPosition.x;
                windowPosition.y = NavUtilLib.GlobalVariables.Settings.hsiPosition.y;


                Log.detail("NavUtil: Systems started successfully!");
            }
            else
            {
                state = false;
                //RenderingManager.RemoveFromPostDrawQueue(3, OnDraw); //close the GUI
                NavUtilLib.GlobalVariables.Settings.hsiPosition.x = windowPosition.x;
                NavUtilLib.GlobalVariables.Settings.hsiPosition.y = windowPosition.y;

                ConfigLoader.SaveSettings();
            }
        }

        private void OnDraw()
        {
            Log.detail("NavUtils: NavUtilLibApp.OnDraw()");

            Log.dbg("HSI: OnDraw()");
            if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Flight || ((CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal || CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA) && GlobalVariables.Settings.enableWindowsInIVA))
            {
                if ((windowPosition.xMin + windowPosition.width) < 20) windowPosition.xMin = 20 - windowPosition.width;
                if (windowPosition.yMin + windowPosition.height < 20) windowPosition.yMin = 20 - windowPosition.height;
                if (windowPosition.xMin > Screen.width - 20) windowPosition.xMin = Screen.width - 20;
                if (windowPosition.yMin > Screen.height - 20) windowPosition.yMin = Screen.height - 20;

                windowPosition = new Rect(windowPosition.x,
                     windowPosition.y,
                     (int)(NavUtilLib.GlobalVariables.Settings.hsiPosition.width * NavUtilLib.GlobalVariables.Settings.hsiGUIscale),
                     (int)(NavUtilLib.GlobalVariables.Settings.hsiPosition.height * NavUtilLib.GlobalVariables.Settings.hsiGUIscale)
                     );

                windowPosition = GUI.Window(-471466245, windowPosition, OnWindow, "Horizontal Situation Indicator");
            }
            Log.dbg("{0}", windowPosition);
        }

        private void DrawGauge(RenderTexture screen)
        {
            Log.detail("NavUtils: NavUtilLibApp.DrawGauge()");

            NavUtilLib.GlobalVariables.FlightData.updateNavigationData();

            RenderTexture pt = RenderTexture.active;
            RenderTexture.active = screen;

            NavUtilLib.DisplayData.DrawHSI(screen, 1);

            //write text to screen
            //write runway info

            string runwayText = (var.FlightData.isINSMode() ? "INS" : "Runway") + ": " + NavUtilLib.GlobalVariables.FlightData.selectedRwy.ident;
            string glideslopeText = var.FlightData.isINSMode() ? ""	: "Glideslope: " + string.Format("{0:F1}", NavUtilLib.GlobalVariables.FlightData.selectedGlideSlope) + "°  ";
            string elevationText = (var.FlightData.isINSMode() ? "Alt MSL" : "Elevation") + ": " + string.Format("{0:F0}", NavUtilLib.GlobalVariables.FlightData.selectedRwy.altMSL) + "m";
            
            runwayText = (rwyHover ? "→" : " ") + runwayText;
            glideslopeText = (gsHover ? "→" : " ") + glideslopeText;
            
	        NavUtilLib.TextWriter.addTextToRT(screen, runwayText, new Vector2(20, screen.height - 40), NavUtilLib.GlobalVariables.Materials.Instance.whiteFont, .64f);
	        NavUtilLib.TextWriter.addTextToRT(screen, glideslopeText + elevationText, new Vector2(20, screen.height - 64), NavUtilLib.GlobalVariables.Materials.Instance.whiteFont, .64f);

            NavUtilLib.TextWriter.addTextToRT(screen, NavUtilLib.Utils.numberFormatter((float)NavUtilLib.Utils.makeAngle0to360(FlightGlobals.ship_heading), true).ToString(), new Vector2(584, screen.height - 102), NavUtilLib.GlobalVariables.Materials.Instance.whiteFont, .64f);
            NavUtilLib.TextWriter.addTextToRT(screen, NavUtilLib.Utils.numberFormatter((float)NavUtilLib.Utils.makeAngle0to360(NavUtilLib.GlobalVariables.FlightData.bearing), true).ToString(), new Vector2(584, screen.height - 131), NavUtilLib.GlobalVariables.Materials.Instance.whiteFont, .64f);
            NavUtilLib.TextWriter.addTextToRT(screen, NavUtilLib.Utils.numberFormatter((float)NavUtilLib.Utils.makeAngle0to360(NavUtilLib.GlobalVariables.FlightData.selectedRwy.hdg), true).ToString(), new Vector2(35, screen.height - 124), NavUtilLib.GlobalVariables.Materials.Instance.whiteFont, .64f);
            NavUtilLib.TextWriter.addTextToRT(screen, NavUtilLib.Utils.numberFormatter((float)NavUtilLib.GlobalVariables.FlightData.dme / 1000, false).ToString(), new Vector2(45, screen.height - 563), NavUtilLib.GlobalVariables.Materials.Instance.whiteFont, .64f);

            if (closeHover)
                NavUtilLib.TextWriter.addTextToRT(screen, "    Close HSI", new Vector2(340, 15), NavUtilLib.GlobalVariables.Materials.Instance.whiteFont, .64f);

            RenderTexture.active = pt;
        }

        private void OnWindow(int WindowID)
        {
            Log.detail("NavUtils: NavUtilLibApp.OnWindow()");

            Log.dbg("HSI: OnWindow()");



            Rect rwyBtn = new Rect(20 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale,
                13 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale,
                200 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale,
                20 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale);

            Rect gsBtn = new Rect(20 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale,
        38 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale,
        200 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale,
        20 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale);

            Rect closeBtn = new Rect(330 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale,
                580 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale,
                300 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale,
                50 * NavUtilLib.GlobalVariables.Settings.hsiGUIscale);

            if (GUI.Button(closeBtn, new GUIContent("CloseBtn", "closeOn")))
            {
                //displayHSI();
                Log.dbg("CloseHSI");
                appButton.SetFalse(true);
                //goto CloseWindow;
            }

            if (GUI.tooltip == "closeOn")
                closeHover = true;
            else
                closeHover = false;


            if (GUI.Button(rwyBtn, new GUIContent("Next Runway", "rwyOn")) && !var.FlightData.isINSMode()) //doesn't let runway to be switched in INS mode
            {
				if (Event.current.alt) {
					RunwayListGUI.show(windowPosition);
				} else {

					if (Event.current.button == 0) {
						NavUtilLib.GlobalVariables.FlightData.rwyIdx++;
					} else {
						NavUtilLib.GlobalVariables.FlightData.rwyIdx--;
					}

					NavUtilLib.GlobalVariables.FlightData.rwyIdx = NavUtilLib.Utils.indexChecker(NavUtilLib.GlobalVariables.FlightData.rwyIdx, NavUtilLib.GlobalVariables.FlightData.currentBodyRunways.Count - 1, 0);
				}
            }

            if (GUI.tooltip == "rwyOn")
                rwyHover = true;
            else
                rwyHover = false;


            if (GUI.Button(gsBtn, new GUIContent("Next G/S", "gsOn")))
            {
                if (Event.current.button == 0)
                {
                    NavUtilLib.GlobalVariables.FlightData.gsIdx++;
                }
                else
                {
                    NavUtilLib.GlobalVariables.FlightData.gsIdx--;
                }

                NavUtilLib.GlobalVariables.FlightData.gsIdx = NavUtilLib.Utils.indexChecker(NavUtilLib.GlobalVariables.FlightData.gsIdx, NavUtilLib.GlobalVariables.FlightData.gsList.Count - 1, 0);
            }

            if (GUI.tooltip == "gsOn")
                gsHover = true;
            else
                gsHover = false;

            rt.Create();

            DrawGauge(rt);
            GUI.DrawTexture(new Rect(0, 0, windowPosition.width, windowPosition.height), rt, ScaleMode.ScaleToFit);

            //GUI.DrawTexture(new Rect(0, 0, windowPosition.width, windowPosition.height), var.Materials.Instance.overlay.mainTexture);

            GUI.DragWindow();
        }



        void AddButton()
        {
			if (KSP.UI.Screens.ApplicationLauncher.Ready) {
				appButton = KSP.UI.Screens.ApplicationLauncher.Instance.AddModApplication(
					onAppLaunchToggleOn,
					onAppLaunchToggleOff,
					onAppLaunchHoverOn,
					onAppLaunchHoverOff,
					onAppLaunchEnable,
					onAppLaunchDisable,
					KSP.UI.Screens.ApplicationLauncher.AppScenes.FLIGHT,
					NavUtilGraphics.loadTexture(KSPe.IO.File<KSPeHack>.Asset.Solve("Toolbar/toolbarButton3838.png"), 0, 0)
				);
				;
				app = this;
			}
        }



        void Awake()
		{
            Log.detail("NavUtils: NavUtilLibApp.Awake()");

            //load settings to config
            ConfigLoader.LoadSettings();

            Log.detail("NavUtil: useBlizzy? " + NavUtilLib.GlobalVariables.Settings.useBlizzy78ToolBar);

			if (NavUtilLib.GlobalVariables.Settings.useBlizzy78ToolBar && ToolbarManager.ToolbarAvailable) {
				IToolbarManager toolbar = ToolbarManager.Instance;
				toolbarButton = toolbar.add("NavUtilities", "NavUtilButton");
				toolbarButton.TexturePath = KSPe.IO.File<NavUtilLibApp>.Asset.Solve("Toolbar/toolbarButton.png");
				toolbarButton.OnClick += (clickEvent => {
					isHovering = true;
					onAppLaunchToggleOn();
				});
				toolbarButton.Visible = true;
				toolbarButton.ToolTip = "NavUtilities HSI / Hold Alt to open settings";
			} else {
                //GameEvents.onGUIApplicationLauncherReady.Add(OnGUIReady);

                if (appButton == null)
                GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
                GameEvents.onGUIApplicationLauncherUnreadifying.Add(dEstroy);
                GameEvents.onGameSceneLoadRequested.Add(dEstroy);

                GameEvents.onShowUI.Add(ShowGUI);
                GameEvents.onHideUI.Add(HideGUI);
            }



            NavUtilLib.GlobalVariables.Settings.appInstance = this.GetInstanceID();
            NavUtilLib.GlobalVariables.Settings.appReference = this;

        }

		void Update() {

			NavWaypoint navWaypoint = NavWaypoint.fetch;

			//TODO optimize these searches
			if (navWaypoint.IsActive && CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA) {
				InternalNavBall navBall = null;
				foreach (InternalProp prop in CameraManager.Instance.IVACameraActiveKerbal.InPart.internalModel.props) {
					navBall = (InternalNavBall)prop.internalModules.Find(module => module.GetType().Equals(typeof(InternalNavBall))); 
					if (navBall != null) {
						break;
					}
				}
				if (navBall != null) {
					if (originalIVANavBallWaypointSize.Equals(Vector3.zero)) {
						originalIVANavBallWaypointSize = navBall.navWaypointVector.localScale;
					}
					navBall.navWaypointVector.localScale = GlobalVariables.Settings.hideNavBallWaypoint ? Vector3.zero : originalIVANavBallWaypointSize;
				}
			}

			if (originalNavBallWaypointSize.Equals(Vector3.zero)) {
				originalNavBallWaypointSize = navWaypoint.Visual.transform.localScale;
			}
			navWaypoint.Visual.transform.localScale = GlobalVariables.Settings.hideNavBallWaypoint ? Vector3.zero : originalNavBallWaypointSize;
		}

		/*private Transform findWaypointVisual() {
			Transform navBall = FlightUIModeController.Instance.navBall.transform.FindChild("IVAEVACollapseGroup");
			if (navBall == null) {
				return null;
			}
			Transform navBallVectors = navBall.FindChild("NavBallVectorsPivot");
			if (navBallVectors == null) {
				return null;
			}
			return navBallVectors.FindChild("NavWaypointVisual");
		}*/

        void ShowGUI()
        {
            NavUtilLib.GlobalVariables.Settings.isKSPGUIActive = true;
        }

        void HideGUI()
        {
            NavUtilLib.GlobalVariables.Settings.isKSPGUIActive = false;
        }




        public void dEstroy(GameScenes g)
        {
            //Log.dbg("NavUtils: Destorying App 1");

            GameEvents.onGUIApplicationLauncherReady.Remove(AddButton);

            if (appButton != null)
            {
                //Log.dbg("NavUtils: Destorying App 2");


                //save settings to config
                ConfigLoader.SaveSettings();

                NavUtilLib.GlobalVariables.Settings.hsiState = false;

                KSP.UI.Screens.ApplicationLauncher.Instance.RemoveModApplication(appButton);
            }
        }



        //void OnGUIReady()
        //{
        //    Log.dbg("NavUtils: NavUtilLibApp.OnGUIReady()");

        //    if (KSP.UI.Screens.ApplicationLauncher.Ready && !NavUtilLib.GlobalVariables.Settings.useBlizzy78ToolBar)
        //    {
        //        appButton = KSP.UI.Screens.ApplicationLauncher.Instance.AddModApplication(
        //            onAppLaunchToggleOn,
        //            onAppLaunchToggleOff,
        //            onAppLaunchHoverOn,
        //            onAppLaunchHoverOff,
        //            onAppLaunchEnable,
        //            onAppLaunchDisable,
        //            KSP.UI.Screens.ApplicationLauncher.AppScenes.FLIGHT,
        //            (Texture)GameDatabase.Instance.GetTexture("KerbalScienceFoundation/NavInstruments/CommonTextures/toolbarButton3838", false)
        //          );
        //        ;
        //    }

        //    app = this;

        //    //panel = new UIInteractivePanel();
        //    //panel.draggable = true;
        //    //panel.index = 1;



        //}

        void onAppLaunchToggleOn()
        {
            Log.detail("NavUtils: onAppLaunchToggleOn");
            if(isHovering)
            {
                if (Event.current.alt)
                {
                    NavUtilLib.SettingsGUI.startSettingsGUI();
                    goto Finish;
                }
            }
            displayHSI();

        Finish:
            ;
            ////Log.dbg("onAppLaunchToggleOn");

            //Log.dbg("{0}", appButton.GetAnchor());
            //Log.dbg("State: {0}", appButton.State);
            //Log.dbg("{0}", appButton.transform);
            //Log.dbg("{0}", appButton.transform.position);

            Log.detail("NavUtils: onAppLaunchToggleOn End");
        }




        void onAppLaunchToggleOff()
        {
            Log.detail("NavUtils: onAppLaunchToggleOff");
            if (isHovering)
            {
                if (Event.current.alt)
                {
                    NavUtilLib.SettingsGUI.startSettingsGUI();
                    goto Finish;
                }
            }
            displayHSI();

        Finish:
            ;
            //bug.Log("onAppLaunchToggleOff");
            ;
        }
        void onAppLaunchHoverOn()
        {
            Log.detail("onHover");

            isHovering = true;
        }
        void onAppLaunchHoverOff()
        {
            Log.detail("offHover");
            isHovering = false;
        }
        void onAppLaunchEnable()
        {
            Log.detail("NavUtils: onAppLaunchEnable");
        }
        void onAppLaunchDisable()
        {
            Log.detail("NavUtils: onAppLaunchDisable");
        }

        bool isApplicationTrue()
        {
            return false;
        }

    }
}
