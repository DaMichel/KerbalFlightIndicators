using System;
using UnityEngine;

namespace KerbalFlightIndicators
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbalFlightIndicators : DaMichelToolbarSuperWrapper.PluginWithToolbarSupport
    {
        const int KFI_LAYER = 30;  // I claim this layer just for me!
        Texture2D marker_atlas = null;

        internal static string dataPath = "GameData/KerbalFlightIndicators/Plugins/PluginData/KerbalFlightIndicators/";

        //value for atlas_px needs to be set in Awake or in Start due to Unity changes.
        RectOffset[] atlas_px;
        Rect[] atlas_uv = null;
        float displayScaleFactor = 1.0f;
        string atlasTexture = "atlas.png";

        Color horizonColor     = Color.green;
        Color progradeColor    = Color.green;
        Color attitudeColor    = Color.green;
        bool  drawInFrontOfCockpit = true;

        MarkerScript[] markerScripts = null;
        CameraScript cameraScript  = null;
        GameObject markerParentObject = null;
        bool[] markerEnabling = null;

        protected override DaMichelToolbarSuperWrapper.ToolbarInfo GetToolbarInfo()
        {
            return new DaMichelToolbarSuperWrapper.ToolbarInfo {
                name = "KerbalFlightIndicators",
                tooltip = "KerbalFlightIndicators On/Off Switch",
                toolbarTexture = "KerbalFlightIndicators/textures/toolbarbutton",
                launcherTexture = "KerbalFlightIndicators/textures/icon",
                visibleInScenes = new GameScenes[] { GameScenes.FLIGHT }
            };
        }

        // see http://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html
        // Called when the script instance is being loaded.
        void Awake()
        {
            marker_atlas = Util.LoadTexture(atlasTexture);
            atlas_px = new RectOffset[] {
                new RectOffset(  0*2,  32*2, 32*2, 64*2), // heading
                new RectOffset( 32*2,  64*2, 32*2, 64*2), // prograde
                new RectOffset( 64*2,  96*2, 32*2, 64*2), // retrograde
                new RectOffset( 96*2, 128*2, 32*2, 64*2), // reverse heading
                new RectOffset(128*2, 192*2, 32*2, 64*2), // wings level guide
                new RectOffset((208-2)*2, (256-2)*2, (16-2)*2, (64-2)*2), // vertical
                new RectOffset(0*2, 256*2, 6*2, 10*2), // horizon
            };
            LoadSettings();
            atlas_uv = Util.ComputeTexCoords(marker_atlas.width, marker_atlas.height, atlas_px);
            InitializeToolbars();
        }


        // see http://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html
        // Maybe called once, on the first frame where the script is enabled. If the script is never enabled, Start won't be called.
        void Start()
        {
            AdjustPhysics();
            CreateGameObjects();
        }


        public void OnDestroy()
        {
            SaveSettings();
            // well we probably need this to not create a memory leak or so ...
            DestroyGameObjects();
            TearDownToolbars();
        }

        public void SaveSettings()
        {
            ConfigNode settings = new ConfigNode();
            settings.name = "SETTINGS";
            SaveMutableToolbarSettings(settings);
            settings.Save(dataPath + "state.cfg");
        }


        public void LoadSettings()
        {
            // load mutable configuration
            ConfigNode state = ConfigNode.Load(dataPath + "state.cfg");
            if (state != null)
            {
                LoadMutableToolbarSettings(state);
            }

            // load configuration that won't be changed in game
            ConfigNode settings = ConfigNode.Load(dataPath + "settings.cfg");
            if (settings != null)
            {
                string[] markerNames =
                {
                    "Heading",
                    "Prograde",
                    "Retrograde",
                    "Reverse",
                    "LevelGuide",
                    "Vertical",
                    "Horizon"
                };
                for (int i=0; i<(int)Markers.COUNT; ++i)
                {
                    if (settings.HasValue("rect"+markerNames[i])) 
                    {
                        atlas_px[i] = Util.RectOffsetFromString(settings.GetValue("rect"+markerNames[i]));
                    }
                }
                LoadImmutableToolbarSettings(settings);
                settings.TryGetValue("displayScaleFactor", ref displayScaleFactor);
                settings.TryGetValue("atlasTexture", ref atlasTexture);
                if (settings.HasValue("horizonColor")) horizonColor = Util.ColorFromString(settings.GetValue("horizonColor"));
                if (settings.HasValue("progradeColor")) progradeColor = Util.ColorFromString(settings.GetValue("progradeColor"));
                if (settings.HasValue("attitudeColor")) attitudeColor = Util.ColorFromString(settings.GetValue("attitudeColor"));
                if (settings.HasValue("drawInFrontOfCockpit")) drawInFrontOfCockpit = bool.Parse(settings.GetValue("drawInFrontOfCockpit"));
            }
        }


        protected override  void OnGuiVisibilityChange()
        {
            bool enabled_ = isGuiVisible;
            enabled = enabled_;
            if (cameraScript) cameraScript.gameObject.SetActive(enabled_);
            if (markerParentObject) markerParentObject.SetActive(enabled_);
        }


        private void AdjustPhysics()
        {
            // DON'T WANT OUR GUI ELEMENTS COLLIDING WITH STUFF!
            for (int i=0; i<30; ++i)
                Physics.IgnoreLayerCollision(KFI_LAYER, i, true); // ignore = true
            // Colliders are removed, too.
            // Is there anything else???
        }


        private void CreateGameObjects()
        {
            const float BLEND_HORIZON_C0 = 0.259f;
            const float BLEND_HORIZON_C1 = 0.342f;
            const float BLEND_VERT_C0 = 0.966f;
            const float BLEND_VERT_C1 = 0.94f;
            const float BLEND_LEVEL_GUIDE_C0 = 0.984f;
            const float BLEND_LEVEL_GUIDE_C1 = 0.996f;

            float viewportSizeX = Screen.width*0.5f;
            float viewportSizeY = Screen.height*0.5f;

            {
                GameObject o = new GameObject("KerbalFlightIndicators-CustomCamera");
                Camera cam = o.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.Nothing;
                cam.orthographicSize = viewportSizeY;
                cam.aspect           = viewportSizeX/viewportSizeY;
                cam.cullingMask  = (1<<KFI_LAYER);
                cam.depth = drawInFrontOfCockpit ? 10 : 1;
                cam.farClipPlane = 2.0f;
                cam.nearClipPlane = 0.5f;
                cam.allowHDR = false;
                o.transform.localPosition = new Vector3(0.5f, 0.5f, 0.0f);
                o.transform.localRotation = Quaternion.identity;
                CameraScript cs = cameraScript = o.AddComponent<CameraScript>();
            }

            {
                Shader shd = Shader.Find("KSP/Alpha/Unlit Transparent");

                GameObject o = markerParentObject = new GameObject("KerbalFlightIndicators-MarkerParent");

                markerEnabling = cameraScript.markerEnabling = new bool[(int)Markers.COUNT];
                markerScripts = cameraScript.markerScripts = new MarkerScript[(int)Markers.COUNT];
                for (int i=0; i<(int)Markers.COUNT; ++i)
                {
                    Material mat = new Material(shd);
                    mat.mainTexture = marker_atlas;
                    Rect uv = atlas_uv[i];
                    RectOffset px = atlas_px[i];
                    mat.mainTextureScale = new Vector2(uv.width, uv.height);
                    mat.mainTextureOffset = new Vector2(uv.xMin, uv.yMin);

                    o = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    o.name = "KerbalFlightIndicators-"+Enum.GetName(typeof(Markers), i);

                    Component[] colliders = o.GetComponents<Collider>();
                    foreach (var c in colliders)
                    {
                        Component.DestroyImmediate(c); // DON'T WANT OUR GUI ELEMENTS COLLIDING WITH STUFF!
                    }

                    MeshRenderer mr = o.GetComponent<MeshRenderer>();
                    mr.material = mat;
                    o.layer = KFI_LAYER;

                    float zlevel = 1.0f;
                    MarkerScript ms = markerScripts[i] = o.AddComponent<MarkerScript>();
                    if (i == (int)Markers.Heading || i == (int)Markers.LevelGuide || i == (int)Markers.Reverse)
                    {
                        ms.baseColor = attitudeColor;
                    }
                    else if (i == (int)Markers.Prograde || i == (int)Markers.Retrograde)
                    {
                        ms.baseColor = progradeColor;
                        zlevel = 1.1f;
                    }
                    else
                    {
                        ms.baseColor = horizonColor;
                        zlevel = 1.2f;
                    }
                    if (i == (int)Markers.Horizon)
                    {
                        ms.SetBlendConstants(BLEND_HORIZON_C0, BLEND_HORIZON_C1);
                    }
                    else if (i == (int)Markers.Vertical)
                    {
                        ms.SetBlendConstants(BLEND_VERT_C0, BLEND_VERT_C1);
                    }
                    else if (i == (int)Markers.LevelGuide)
                    {
                        ms.SetBlendConstants(BLEND_LEVEL_GUIDE_C0, BLEND_LEVEL_GUIDE_C1);
                    }

                    o.transform.parent = markerParentObject.transform;
                    o.transform.localPosition = new Vector3(0.0f, 0.0f, zlevel);
                    o.transform.localRotation = Quaternion.identity;
                    o.transform.localScale    = new Vector3((px.right - px.left)*displayScaleFactor, (px.bottom - px.top)*displayScaleFactor, 1.0f);

                    markerEnabling[i] = true;
                }
            }
            OnGuiVisibilityChange();
        }


        void DestroyGameObjects()
        {
            if (cameraScript != null && cameraScript.gameObject != null) // could it be that they are deleted somewhere outside this plugin? I suppose because i got a NRE here ...
                GameObject.Destroy(cameraScript.gameObject); 
            if (markerParentObject != null)
                GameObject.Destroy(markerParentObject); // destroys children, too
            cameraScript = null;
            markerScripts = null;
            markerParentObject = null;
        }
    }
}