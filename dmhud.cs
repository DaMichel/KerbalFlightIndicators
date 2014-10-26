using System;
using UnityEngine;
using System.Text;

namespace KerbalFlightIndicators
{

#region Utilities
#if DEBUG
public static class DMDebug
{
    public static void PrintTransformHierarchy(Transform transf, int depth, StringBuilder sb)
    {
        PrintTransform(transf, depth, sb);
        for (int i=0; i<transf.childCount; ++i)
        {
            PrintTransformHierarchy(transf.GetChild(i), depth + 1, sb);
        }
    }

    public static void PrintTransform(Transform transf, int depth, StringBuilder sb)
    {
        String vfmt = "F3";
        sb.AppendLine(new String(' ', depth) + transf);
        sb.AppendLine("P = " + transf.localPosition.ToString(vfmt));
        sb.AppendLine("R = " + transf.localEulerAngles.ToString(vfmt));
        sb.AppendLine("S = " + transf.localScale.ToString(vfmt));
        sb.AppendLine("MW = \n" + transf.localToWorldMatrix.ToString(vfmt));
        if (transf.parent != null)
        {
            Matrix4x4 ml = transf.parent.localToWorldMatrix.inverse * transf.localToWorldMatrix;
            sb.AppendLine("ML = \n" + ml.ToString(vfmt));
        }
    }

    public static void PrintTransformHierarchyUp(Transform transf, int depth, StringBuilder sb)
    {
        PrintTransform(transf, depth, sb);
        if (transf.parent)
            PrintTransformHierarchyUp(transf.parent, depth+1, sb);
    }
}
#endif


public static class GUIExt
{
    public static void DrawTextureCentered(Vector3 position, Texture2D tex, float width, float height)
    {
        GUI.DrawTexture(new Rect(position.x - width * 0.5f,
                                 position.y - height * 0.5f,
                                 width, height),
                                 tex);
    }

    public static void DrawTextureCentered(Vector3 position, Texture2D tex)
    {
        DrawTextureCentered(position, tex, tex.width, tex.height);
    }

    public static void DrawTextureCentered(Vector3 position, Texture2D tex, float width, float height, Rect texCoords)
    {
        GUI.DrawTextureWithTexCoords(new Rect(position.x - width * 0.5f,
                                              position.y - height * 0.5f,
                                              width, height),
                                     tex, texCoords);
    }
}



public static class Util
{
    public static Vector3 x = Vector3.right;
    public static Vector3 y = Vector3.up;
    public static Vector3 z = Vector3.forward;

    public static float RAD2DEGf = 180f/Mathf.PI;
    public static double RAD2DEG = 180.0/Math.PI;

    // angle need to rotate the x-axis so that it is parallel with (x,y); in Radians
    public static float PolarAngle(Vector2 v)
    {
        float n = v.magnitude;
        float a = Mathf.Acos(v.x/n);
        if (v.y < 0f) a = Mathf.PI*2f - a;
        return a;
    }

    public static Vector3 PerspectiveProjection(Camera cam, Vector3 p)
    {
        return cam.projectionMatrix.MultiplyPoint(p);
    }

    public static Vector3 CameraToViewport(Camera cam, Vector3 v)
    {
        Vector3 q = cam.projectionMatrix.MultiplyPoint(v);
        // After projection, coordinates go from -1 to 1.
        // We need to transform to proper viewport space which goes from 0 to 1.
        // The bottom left is viewport-(0,0), according to the Unity 3d reference.
        // For some odd reason the x axis in camera space seems reversed. 
        q.y = (q.y + 1.0f) * 0.5f;
        q.x = (1.0f - q.x) * 0.5f;
        return q;
    }

    public static Vector3 CameraToScreen(Camera cam, Vector3 v)
    {
        return cam.ViewportToScreenPoint(CameraToViewport(cam, v));
    }

    public static Texture2D LoadTexture(String filename, int width, int height)
    {
        Byte[] data = KSP.IO.File.ReadAllBytes<KerbalFlightIndicators>(filename);
        Texture2D tex = new Texture2D(width, height);
        tex.LoadImage(data);
        tex.anisoLevel = 0;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    public static Color ColorFromString(String s)
    {
        Color result = new Color();
        var strvalues = s.Split(',');
        for (int i = 0; i < strvalues.Length; ++i)
            result[i] = float.Parse(strvalues[i].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        return result;
    }

    public static String ColorToString(Color c)
    {
        String[] s = new String[4];
        for (int i = 0; i < 4; ++i) 
            s[i] = c[i].ToString(System.Globalization.CultureInfo.InvariantCulture);
        return String.Join(",", s);
    }


    public static Rect[] ComputeTexCoords(int tex_width, int tex_height, RectOffset[] atlas)
    {
        Rect[] result = new Rect[atlas.Length];
        float sx = 1f/tex_width;
        float sy = 1f/tex_height;
        for (int i=0; i<atlas.Length; ++i)
        {
            result[i] = new Rect(
                atlas[i].left * sx,
                1f - (atlas[i].bottom  * sy),
                (atlas[i].right-atlas[i].left) * sx,
                (atlas[i].bottom-atlas[i].top) * sy
            );
        }
        return result;
    }
}


/*
These are KSPs layers
 mask 0 = Default
 mask 1 = TransparentFX
 mask 2 = Ignore Raycast
 mask 3 = 
 mask 4 = Water
 mask 5 = UI
 mask 6 = 
 mask 7 = 
 mask 8 = PartsList_Icons
 mask 9 = Atmosphere
 mask 10 = Scaled Scenery
 mask 11 = UI_Culled
 mask 12 = UI_Main
 mask 13 = UI_Mask
 mask 14 = Screens
 mask 15 = Local Scenery
 mask 16 = kerbals
 mask 17 = Editor_UI
 mask 18 = SkySphere
 mask 19 = Disconnected Parts
 mask 20 = Internal Space
 mask 21 = Part Triggers
 mask 22 = KerbalInstructors
 mask 23 = ScaledSpaceSun
 mask 24 = MapFX
 mask 25 = EzGUI_UI
 mask 26 = WheelCollidersIgnore
 mask 27 = WheelColliders
 mask 28 = TerrainColliders
 mask 29 = 
 mask 30 = 
 */


#if false
    Quaternion BuildUpFrame(FlightCamera cam, Vector3 up)
    {
        Vector3 z;
        if (Mathf.Abs(Vector3.Dot(up, cam.transform.right)) < 0.9999)
        {
            z = Vector3.Cross(cam.transform.right, up); // z is in the cameras y-z plane
            z.Normalize();
        }
        else // up_in_cam_frame is parallel to x
        {
            z = cam.transform.forward;
        }
        return Quaternion.LookRotation(z, up);
    }
#endif
#endregion


public enum Markers
{
    Horizon = 0,
    Heading,
    Prograde,
    Retrograde,
    Reverse,
    LevelGuide,
    Vertical,
    COUNT
};


public class MarkerScript : MonoBehaviour
{
    public  Color baseColor;
    private float blendC0; // fully visible
    private float blendC1; // invisible
    private float blendCM;
    private bool  doBlending = false;
    public  float blendValue;
    
    public void SetBlendConstants(float blendC0_, float blendC1_)
    {
        blendC0    = blendC0_;
        blendC1    = blendC1_;
        blendCM    = 1.0f/(blendC1 - blendC0);
        blendValue = blendC1;
        doBlending = true;
    }

    public void Start()
    {
        renderer.material.color = baseColor;
    }

    public void OnWillRenderObject()
    {
        if (doBlending)
        {
            float alpha = 1.0f - Mathf.Clamp01((blendValue-blendC0)*blendCM);
            Color c = baseColor;
            c.a *= alpha;
            renderer.material.color = c;
        }
    }
};


public class CameraScript : MonoBehaviour
{
    const  float speed_draw_threshold = 1.0e-1f;
    /* cached information about the vessel and the world.
     * Most of teh calculations are done in the draw pass
     * because i want to make sure the camera is up to date */
    Quaternion qfix = Quaternion.Euler(new Vector3(-90f, 0f, 0f));
    Quaternion qvessel     = Quaternion.identity;
    Vector3 vessel_euler   = Vector3.zero;
    Vector3 up             = Util.z;
    Vector3 speed          = Vector3.zero;
    NavBall ball           = null;
    Quaternion qhorizon    = Quaternion.identity;
    Camera cam             = null;

    public bool[] markerEnabling = null;
    public MarkerScript[] markerScripts = null;

    void OnPreCull()  // this is only called if the MonoBehaviour component is attached to a GameObject which also has a Camera!
    {   
        for (int i=0; i<(int)Markers.COUNT; ++i)
            markerEnabling[i] = false;

        if (UpdateInternalData()) 
        {
            UpdateAllMarkers(cam);
        }

        for (int i=0; i<(int)Markers.COUNT; ++i)
        {
            markerScripts[i].gameObject.SetActive(markerEnabling[i]);
        }
    }


    bool UpdateInternalData()
    {
        Vessel vessel = FlightGlobals.ActiveVessel;
        if (vessel == null) return false;
        CelestialBody body = FlightGlobals.currentMainBody;
        if (body == null) return false;
        cam = FlightGlobals.fetch.mainCameraRef;
        if (cam == null) return false;
        if (ball == null)
            ball = FlightUIController.fetch.GetComponentInChildren<NavBall>();
        if (ball == null)
            return false;
        if (!IsInAdmissibleCameraMode())
            return false;

        speed = GetSpeedVector();
        if (vessel.ReferenceTransform != null)
        {
            qvessel = vessel.ReferenceTransform.rotation;
        }
        else
        {
            qvessel = vessel.transform.rotation;
        }
        qvessel = qvessel * qfix;  // make z be the forward pointing axis

        up = vessel.upAxis;

        qhorizon = qvessel * ball.relativeGymbal;
        /* qhorizon describes the orientation of the planetary surface underneath the vessel. 
         * Here is how to get the planetary to world frame transform:
                ball = horizon -> vessel
                vessel = vessel -> world
                    => (vessel * ball) = horizon -> world
         */
        Quaternion vesselRot = Quaternion.Inverse(ball.relativeGymbal);
        vessel_euler = vesselRot.eulerAngles;
        return true;
    }


    void UpdateMarker(Markers id, Vector3 position, Vector3 up_vector, float blend_value)
    {
        // alpha = 0 is transparent
        // position is in camera space
        MarkerScript ms = markerScripts[(int)id];
        position.x *= -camera.aspect * camera.orthographicSize;
        position.y *= -camera.orthographicSize;
        ms.transform.localPosition = position; 
        ms.blendValue = blend_value;
        float nrm = Mathf.Sqrt(up_vector.x * up_vector.x + up_vector.y * up_vector.y);
        float cs =  up_vector.y / nrm;
        float sn = -up_vector.x / nrm;
        Quaternion q; 
        q.x = q.y = 0;
        if (cs < 0.9999999999)
        {
            q.z = Mathf.Sqrt((1 - cs)*0.5f);
            q.w = sn / q.z * 0.5f;
        }
        else // identity
        {   
            q.z = 0.0f;
            q.w = 1.0f;
        }
        ms.transform.localRotation = q;
        markerEnabling[(int)id] = true;
    }


    void UpdateAllMarkers(Camera cam)
    {
        Vector3 normalized_speed = speed.normalized;
        bool is_moving = speed.sqrMagnitude > speed_draw_threshold*speed_draw_threshold;
        
        Quaternion qhorizoninv = qhorizon.Inverse();

        Quaternion qcam     = cam.transform.rotation;
        /* When you look at your vessels from behind 
         * or from IVA, your camera z axis, which is the direction you are looking to, and
         * the z-axis of the qvessel frame are parallel */    
                
        Quaternion qcaminv = qcam.Inverse();
        Quaternion horizon_to_cam = qcaminv * qhorizon;

        Vector3 upvector_horizon = horizon_to_cam * Util.y;

        Quaternion qroll = qcaminv * qvessel;
        Vector3 upvector_roll = qroll * Util.y;

        Vector3 heading              = qvessel * Util.z;
        Vector3 up_in_cam_frame      = cam.transform.InverseTransformDirection(up);
        Vector3 speed_in_cam_frame   = cam.transform.InverseTransformDirection(normalized_speed);
        Vector3 heading_in_cam_frame = cam.transform.InverseTransformDirection(heading);
        Vector3 hproj = heading_in_cam_frame - Vector3.Project(heading_in_cam_frame, up_in_cam_frame); // the projection of the heading onto the trangent plane of upAxis

#if false
        {
            StringBuilder sb = new StringBuilder(8);
            sb.AppendLine("up      = "+up.ToString("F3"));
            sb.AppendLine("heading = "+heading.ToString("F3"));
            sb.AppendLine("up_in_cam_frame = "+up_in_cam_frame.ToString("F3"));
            sb.AppendLine("heading_in_cam_frame = "+heading_in_cam_frame.ToString("F3"));
            sb.AppendLine("hproj = "+hproj.ToString("F3"));
            sb.AppendLine("cam   = "+cam.transform.rotation.eulerAngles.ToString("F3"));
            Debug.Log(sb.ToString());
        }
#endif

#if DEBUG
        if (Input.GetKeyDown(KeyCode.O))
        {
            StringBuilder sb = new StringBuilder(8);
            sb.AppendLine("DMHUD:");
            sb.AppendLine("       up_in_cam_frame = " + up_in_cam_frame.ToString("F3"));
            sb.AppendLine("       speed_in_cam_frame = " + speed_in_cam_frame.ToString("F3"));
            sb.AppendLine("       heading_in_cam_frame = " + heading_in_cam_frame.ToString("F3"));
            sb.AppendLine("       qvessel = " + (qhorizoninv*qvessel).ToString("F3"));
            sb.AppendLine("       qhorizon = " + qhorizon.ToString("F3"));
            sb.AppendLine("       euler    = " + vessel_euler.ToString("F3"));
            sb.AppendLine("       heading  = " + (qhorizoninv * heading).ToString("F2"));
            sb.AppendLine("       horizon_roll_z = " + (qcaminv * qhorizon * Util.z).ToString("F2"));
            Debug.Log(sb.ToString());
        }
#endif

        Vector3 upvector_vertical = horizon_to_cam * Util.z;

        // Pretend we were at zero roll and get the alignment of the resulting lateral axis in screen coords
        // relative_gimbal^-1 gives euler angles of the craft in the planetary surface frame
        // so, for roll == 0 we have:
        // qhorizon * relative_gimbal^-1 = qvessel * relative_gimbal * relative_gimbal^-1 = qvessel 
        // meaning that what is computed here will be identical to the vessel orientation if roll=0
        Quaternion qroll_level = horizon_to_cam * Quaternion.Euler(new Vector3(vessel_euler.x, vessel_euler.y, 0));
        Vector3 upvector_levelguide = qroll_level * Util.y;

        /* This doesn't work very well when pointing straight up/down due to gimbal lock issues in the Quaternion->euler angles conversion! */
        //Vector3 tmp = qhorizoninv * normalized_speed;
        //Vector3 speed_hpb = Quaternion.LookRotation(tmp, Util.y).eulerAngles;
        //Quaternion qroll_prograde = qcaminv * qhorizon * Quaternion.Euler(new Vector3(speed_hpb.x, speed_hpb.y, vessel_euler.z));
        //float prograde_roll = -Util.RAD2DEGf*Util.PolarAngle(qroll_prograde * Util.y)+90f;

        float heading_dot_up = Vector3.Dot(up_in_cam_frame, heading_in_cam_frame);
        float speed_dot_up   = Vector3.Dot(up_in_cam_frame, speed_in_cam_frame);
        
        float blend_vertical = Mathf.Max(Mathf.Abs(heading_dot_up), is_moving ? Mathf.Abs(speed_dot_up) : 0f);
        float blend_horizon = Mathf.Min(Mathf.Abs(heading_dot_up), is_moving ? Mathf.Abs(speed_dot_up) : 0f);
        float blend_level_guide = Mathf.Abs(heading_dot_up);

        Vector3 horizon_marker_screen_position = Util.PerspectiveProjection(cam, hproj);
        if (CheckPosition(cam, horizon_marker_screen_position)) // possible optimization: i think this check can be made before screen space projection
        {
            UpdateMarker(Markers.Horizon, horizon_marker_screen_position, upvector_horizon, blend_horizon);
        }

        {
            Vector3 screen_position = Util.PerspectiveProjection(cam, up_in_cam_frame);
            if (CheckPosition(cam, screen_position))
            {
                UpdateMarker(Markers.Vertical, screen_position, upvector_vertical, blend_horizon);
            }
        }

        if (is_moving)
        {
            Vector3 screen_pos = Util.PerspectiveProjection(cam, speed_in_cam_frame);
            if (CheckPosition(cam, screen_pos))
            {
                if (speed_in_cam_frame.z >= 0)
                    UpdateMarker(Markers.Prograde, screen_pos, Util.y, 1.0f);
                else
                    UpdateMarker(Markers.Retrograde, screen_pos, Util.y, 1.0f);
            }
        }       

        {
            Vector3 screen_pos = Util.PerspectiveProjection(cam, heading_in_cam_frame);
            if (CheckPosition(cam, screen_pos))
            {
                if (heading_in_cam_frame.z >= 0)
                    UpdateMarker(Markers.Heading, screen_pos, upvector_roll, 1.0f);
                else
                    UpdateMarker(Markers.Reverse, screen_pos, upvector_roll, 1.0f);
                UpdateMarker(Markers.LevelGuide, screen_pos, upvector_levelguide,  blend_level_guide);
            }
        }
    }

    /*  from Steam Gauges */
    public static Vector3 GetSpeedVector()
    {
        Vessel vessel = FlightGlobals.ActiveVessel; // must not be null. Check before calling.
        // Target velocity correction
        ITargetable tar = FlightGlobals.fetch.VesselTarget;
        Vector3 tgt_velocity = FlightGlobals.ship_tgtVelocity;
        if (tar != null && tar.GetVessel() != null)
        {
            // Otherwise it seems to be equal to orbital velocity when the target
            // vessel isn't loaded (i.e. more than 2km away), which makes no sense.
            // I consider this as a bug in the stock nav-ball functionality.
            Vessel target_vessel = tar.GetVessel();
            if (target_vessel.LandedOrSplashed)
            {
                tgt_velocity = vessel.GetSrfVelocity();
                if (target_vessel.loaded)
                    tgt_velocity -= tar.GetSrfVelocity();
            }
        }
        Vector3 speed = Vector3.zero;
        switch (FlightUIController.speedDisplayMode)
        {
            case FlightUIController.SpeedDisplayModes.Orbit:
                speed = FlightGlobals.ship_obtVelocity;
                break;

            case FlightUIController.SpeedDisplayModes.Target:
                speed = tgt_velocity;
                break;

            default:
                speed = vessel.GetSrfVelocity();
                break;
        }
        return speed;
    }
    /* end of Steam Gauges code */

    bool CheckPosition(Camera cam, Vector3 q)
    {
        if (float.IsInfinity(q.x) || float.IsNaN(q.x) || q.x < -1.5f || q.x > 1.5f) return false;
        if (float.IsInfinity(q.y) || float.IsNaN(q.y) || q.y < -1.5f || q.y > 1.5f) return false;
        return true;
    }

    bool IsInAdmissibleCameraMode()
    {
        switch (CameraManager.Instance.currentCameraMode)
        {
            case CameraManager.CameraMode.Flight:
            case CameraManager.CameraMode.Internal:
            case CameraManager.CameraMode.IVA: 
                return true;
        }
        return false;
    }
}


[KSPAddon(KSPAddon.Startup.Flight, false)]
public class KerbalFlightIndicators : MonoBehaviour
{
    private static Texture2D marker_atlas = null;
    private static RectOffset[] atlas_px = {
        new RectOffset(0, 256, 6, 10), // horizon
        new RectOffset(  0,  32, 32, 64), // heading
        new RectOffset( 32,  64, 32, 64), // prograde
        new RectOffset( 64,  96, 32, 64), // retrograde
        new RectOffset( 96, 128, 32, 64), // reverse heading
        new RectOffset(128, 192, 32, 64), // wings level guide
        new RectOffset(208-2, 256-2, 16-2, 64-2), // vertical
    };
    private static Rect[] atlas_uv = null;

    Color horizonColor     = Color.green;
    Color progradeColor    = Color.green;
    Color attitudeColor    = Color.green;
    MarkerScript[] markerScripts = null;
    CameraScript cameraScript  = null;
    GameObject markerParentObject = null;
    bool[] markerEnabling = null;

    static Toolbar.IButton toolbarButton;

    void Awake()
    {
        toolbarButton = Toolbar.ToolbarManager.Instance.add("KerbalFlightIndicators", "damichelshud");
        toolbarButton.TexturePath = "KerbalFlightIndicators/toolbarbutton";
        toolbarButton.ToolTip = "KerbalFlightIndicators On/Off Switch";
        toolbarButton.Visibility = new Toolbar.GameScenesVisibility(GameScenes.FLIGHT);
        toolbarButton.Enabled = true;
        toolbarButton.OnClick += (e) =>
        {
            SetEnabled(!enabled);
        };
    }

    public void SaveSettings()
    {
        ConfigNode settings = new ConfigNode();
        settings.name = "SETTINGS";
        settings.AddValue("active", enabled);
        settings.AddValue("horizonColor", Util.ColorToString(horizonColor));
        settings.AddValue("progradeColor", Util.ColorToString(progradeColor));
        settings.AddValue("attitudeColor", Util.ColorToString(attitudeColor));
        settings.Save(AssemblyLoader.loadedAssemblies.GetPathByType(typeof(KerbalFlightIndicators)) + "/settings.cfg");
    }

    public void LoadSettings()
    {
        ConfigNode settings = new ConfigNode();
        settings = ConfigNode.Load(AssemblyLoader.loadedAssemblies.GetPathByType(typeof(KerbalFlightIndicators)) + @"\settings.cfg".Replace('/', '\\'));
        if (settings != null)
        {
            if (settings.HasValue("active")) enabled = bool.Parse(settings.GetValue("active"));
            if (settings.HasValue("horizonColor")) horizonColor = Util.ColorFromString(settings.GetValue("horizonColor"));
            if (settings.HasValue("progradeColor")) progradeColor = Util.ColorFromString(settings.GetValue("progradeColor"));
            if (settings.HasValue("attitudeColor")) attitudeColor = Util.ColorFromString(settings.GetValue("attitudeColor"));
        }
    }

    private void SetEnabled(bool enabled_)
    {
        enabled = enabled_;
        if (cameraScript) cameraScript.gameObject.SetActive(enabled_);
        if (markerParentObject) markerParentObject.SetActive(enabled_);
    }


    void CreateGameObjects()
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
            cam.cullingMask  = (1<<31);
            cam.depth = 1;
            cam.farClipPlane = 2.0f;
            cam.nearClipPlane = 0.5f;
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
            MeshRenderer mr = o.GetComponent<MeshRenderer>();
            mr.material = mat;
            o.layer = 31;
            o.transform.localPosition = new Vector3(0.0f, 0.0f, 1.0f);
            o.transform.localRotation = Quaternion.identity;
            o.transform.localScale    = new Vector3((px.right - px.left), (px.bottom - px.top), 1);

            MarkerScript ms = markerScripts[i] = o.AddComponent<MarkerScript>();
            if (i == (int)Markers.Heading || i == (int)Markers.LevelGuide || i == (int)Markers.Reverse)
            {
                ms.baseColor = attitudeColor;
            }
            else if (i == (int)Markers.Prograde || i == (int)Markers.Retrograde)
            {
                ms.baseColor = progradeColor;
            }
            else
            {
                ms.baseColor = horizonColor;
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

            markerEnabling[i] = true;
        }
        }
        SetEnabled(enabled);
    }

    void DestroyGameObjects()
    {
        GameObject.Destroy(cameraScript.gameObject); 
        GameObject.Destroy(markerParentObject); // destroys children, too
        cameraScript = null;
        markerScripts = null;
        markerParentObject = null;
    }

	void Start()
    {
        LoadSettings();
        if (marker_atlas == null) // initialize static members
        {
            marker_atlas = Util.LoadTexture("atlas.png",  256, 64);
            atlas_uv = Util.ComputeTexCoords(256, 64, atlas_px);
        }
        CreateGameObjects();
	}

    public void OnDestroy()
    {
        SaveSettings();
        // well we probably need this to not create a memory leak or so ...
        DestroyGameObjects();
    }
}

}