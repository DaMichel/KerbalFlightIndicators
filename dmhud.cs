using System;
using UnityEngine;
using System.Text;

namespace KerbalFlightIndicators
{

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

    public static Vector3 CameraToViewport(Camera cam, Vector3 v)
    {
        Vector3 q = cam.projectionMatrix.MultiplyPoint(v);
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

    //public static Vector3d EulerAngles(QuaternionD q)
    //{
    //    double yaw   = Math.Atan2(2.0*(q.w*q.z + q.x*q.y), 1.0 - 2.0*(q.y*q.y + q.z*q.z));
    //    double pitch = Math.Asin (2.0*(q.w*q.y - q.z*q.x));
    //    double roll  = Math.Atan2(2.0*(q.w*q.x + q.y*q.z), 1.0 - 2.0*(q.x*q.x + q.y*q.y));
    //    return new Vector3d(RAD2DEG*pitch, RAD2DEG*yaw, RAD2DEG*roll);
    //}
}


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
    public enum AtlasItem 
    {
        Horizon = 0,
        Heading,
        Prograde,
        Retrograde,
        Reverse,
        LevelGuide,
        Vertical
    };

    const  float param_speed_draw_threshold = 1.0e-1f;

    /* cached information about the vessel and the world.
     * Most of teh calculations are done in the draw pass
     * because i want to make sure the camera is up to date */
    Quaternion qfix = Quaternion.Euler(new Vector3(-90f, 0f, 0f));
    Vector3 speed          = Vector3.zero;
    Vector3 normalized_speed = Vector3.zero;
    bool is_moving         = true;
    Quaternion qvessel     = Quaternion.identity;
    Vector3 up             = Util.z;
    Quaternion qhorizon    = Quaternion.identity;
    Quaternion qhorizoninv = Quaternion.identity;
    bool    data_valid     = false;
    Color horizonColor     = Color.green;
    Color progradeColor    = Color.green;
    Color attitudeColor    = Color.green;
    Vector3 vessel_euler   = Vector3.zero;

    static Toolbar.IButton toolbarButton;

    void Awake()
    {
        toolbarButton = Toolbar.ToolbarManager.Instance.add("KerbalFlightIndicators", "damichelshud");
        toolbarButton.TexturePath = "KerbalFlightIndicators/toolbarbutton";
        toolbarButton.ToolTip = "KerbalFlightIndicators On/Off Switch";
        toolbarButton.Visibility = new Toolbar.GameScenesVisibility(GameScenes.FLIGHT);
        //toolbarButton.Visible = true;
        toolbarButton.Enabled = true;
        toolbarButton.OnClick += (e) =>
        {
            enabled = !enabled;
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
            #if false
            Debug.Log("DMHUD parsed horizonColor = " + horizonColor + " Read " + settings.GetValue("horizonColor"));
            Debug.Log("DMHUD parsed progradeColor = " + progradeColor + " Read " + settings.GetValue("progradeColor"));
            Debug.Log("DMHUD parsed attitudeColor = " + attitudeColor + " Read " + settings.GetValue("attitudeColor"));
            #endif
        }
    }


	void Start()
    {
        LoadSettings();
        
        RenderingManager.AddToPostDrawQueue(0, new Callback(OnDraw));

        if (marker_atlas == null) // initialize static members
        {
            marker_atlas = Util.LoadTexture("atlas.png",  256, 64);
            atlas_uv = Util.ComputeTexCoords(256, 64, atlas_px);
        }

        //if (marker_dbg == null)
        //{
        //    marker_dbg = new Texture2D(1, 2);
        //    marker_dbg.SetPixel(1, 1, Color.white);
        //    marker_dbg.SetPixel(1, 2, Color.black);
        //    marker_dbg.Apply();
        //}
	}


    public void OnDestroy()
    {
        SaveSettings();
        // well we probably need this to not create a memory leak or so ...
        RenderingManager.RemoveFromPostDrawQueue(0, new Callback(OnDraw));
    }


    /*  from Steam Gauges */
    protected Vector3 GetSpeedVector()
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

    FlightCamera GetCamera()
    {
        FlightCamera cam = null;
        switch (CameraManager.Instance.currentCameraMode)
        {
            case CameraManager.CameraMode.Flight:
            case CameraManager.CameraMode.Internal:
            case CameraManager.CameraMode.IVA: 
                {
                    cam = FlightCamera.fetch;
                }
                break;
        }
        if (cam == null || cam.transform == null || cam.mainCamera == null) return null;
        return cam;
    }


    void Update()
    {   
        data_valid = false;
        if (!enabled) return;

        Vessel vessel = FlightGlobals.ActiveVessel;
        if (vessel == null) return;
        /* collect data on the current vessel */
        speed = GetSpeedVector();
        normalized_speed = speed.normalized;
        is_moving = speed.sqrMagnitude > param_speed_draw_threshold*param_speed_draw_threshold;
        if (vessel.ReferenceTransform != null)
        {
            qvessel = vessel.ReferenceTransform.rotation;
        }
        else
        {
            qvessel = vessel.transform.rotation;
        }
        up = vessel.upAxis;
        // make z be the forward pointing axis
        qvessel = qvessel * qfix;
        
        CelestialBody body = FlightGlobals.currentMainBody;
        if (body == null) return;

        //Vector3 z = Vector3.Cross(body.angularVelocity, up);
        //if (z.sqrMagnitude < 1.0e-9)
        //    z = Vector3.Cross(Util.y, up);
        //if (z.sqrMagnitude < 1.0e-9)
        //    z = Vector3.Cross(Util.x, up);
        //if (z.sqrMagnitude < 1.0e-9)
        //    return; // fail
        //z.Normalize();
        //qhorizon = Quaternion.LookRotation(z, up);

        NavBall ball =  ball = FlightUIController.fetch.GetComponentInChildren<NavBall>();
        Quaternion vesselRot = Quaternion.Inverse(ball.relativeGymbal);
        vessel_euler = vesselRot.eulerAngles;
        /* qhorizon describes the orientation of the planetary surface underneath the vessel */
        // ball = horizon -> vessel
        // vessel = vessel -> world
        // => (vessel * ball) = horizon -> world
        qhorizon = qvessel * ball.relativeGymbal;
        Quaternion qhorizoninv = qhorizon.Inverse();

        data_valid = true;

#if false
        if (Input.GetKeyDown(KeyCode.O))
        {
            FlightCamera cam = GetCamera();
            StringBuilder sb = new StringBuilder(64);
            sb.AppendLine("--------- Active Vessels -> Up ----------");
            DMDebug.PrintTransform(vessel.transform, 0, sb);
            //DMDebug.PrintTransformHierarchyUp(FlightGlobals.ActiveVessel.transform, 0, sb);
            //sb.AppendLine("--------- Reference Transform -> Up ----------");
            //DMDebug.PrintTransformHierarchyUp(FlightGlobals.ActiveVessel.ReferenceTransform, 0, sb);
            sb.AppendLine("--------- Camera -> Up ----------");
            DMDebug.PrintTransform(cam.transform, 0, sb);
            sb.AppendLine("---------- upAxis Frame-------------");
            Matrix4x4  m = Matrix4x4.TRS(new Vector3(), qhorizon, Vector3.one);
            sb.AppendLine("m = \n"+m.ToString("F3"));
            sb.AppendLine("------------ body ----------------");
            sb.AppendLine("body fwd = " + body.GetFwdVector().ToString("F3"));
            sb.AppendLine("body rotaxis = " + body.angularVelocity.ToString());
            sb.AppendLine("body zUpAngularVel = " + body.zUpAngularVelocity.ToString());
            DMDebug.PrintTransform(body.GetTransform(), 0, sb);
            sb.AppendLine("----------------------------------");
            NavBall ball = FlightUIController.fetch.GetComponentInChildren<NavBall>();
            sb.AppendLine("rel. gimbal = " + ball.relativeGymbal.eulerAngles.ToString("F3"));
            sb.AppendLine("upAxis = " + vessel.upAxis.ToString());
            sb.AppendLine("world CoM = " + vessel.findWorldCenterOfMass().ToString("F3"));
            sb.AppendLine("heading = " + (qvessel * Util.z).ToString("F3"));
            print(sb.ToString());
        }
#endif
    }

    bool CheckScreenPosition(Camera cam, Vector3 q)
    {
        float h = Screen.height;
        float w = Screen.width;
        if (float.IsInfinity(q.x) || float.IsNaN(q.x) || q.x < -w || q.x > 2*w) return false;
        if (float.IsInfinity(q.y) || float.IsNaN(q.y) || q.y < -h || q.y > 2*h) return false;
        return true;
    }

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

    protected void DrawMarker(AtlasItem id, Vector3 position)
    {
        float w = atlas_px[(int)id].right - atlas_px[(int)id].left;
        float h = atlas_px[(int)id].bottom - atlas_px[(int)id].top;
        GUIExt.DrawTextureCentered(position, marker_atlas, w, h, atlas_uv[(int)id]);
    }


	protected void OnDraw()
	{
        if (!data_valid || !enabled) return;
        
        FlightCamera cam = GetCamera();
        if (cam == null) return;


        Quaternion qcam     = cam.transform.rotation;
        /* When you look at your vessels from behind 
         * or from IVA, your camera z axis, which is the direction you are looking to, and
         * the z-axis of the qvessel frame are parallel */    
                
        Quaternion qcaminv = qcam.Inverse();
        Quaternion horizon_to_cam = qcaminv * qhorizon;

        float camroll = -Util.RAD2DEGf*Util.PolarAngle(horizon_to_cam * Util.y)+90f;

        Quaternion qroll = qcaminv * qvessel;
        float roll = -Util.RAD2DEGf*Util.PolarAngle(qroll * Util.y)+90f;

        Vector3 heading              = qvessel * Util.z;
        Vector3 up_in_cam_frame      = cam.transform.InverseTransformDirection(up);
        Vector3 speed_in_cam_frame   = cam.transform.InverseTransformDirection(normalized_speed);
        Vector3 heading_in_cam_frame = cam.transform.InverseTransformDirection(heading);

        float vertical_roll = -Util.RAD2DEGf*Util.PolarAngle(horizon_to_cam * Util.z) + 90f;

        // Pretend we were at zero roll and get the alignment of the resulting lateral axis in screen coords
        // relative_gimbal^-1 gives euler angles of the craft in the planetary surface frame
        // so, for roll == 0 we have:
        // qhorizon * relative_gimbal^-1 = qvessel * relative_gimbal * relative_gimbal^-1 = qvessel 
        // meaning that what is computed here will be identical to the vessel orientation if roll=0
        Quaternion qroll_level = horizon_to_cam * Quaternion.Euler(new Vector3(vessel_euler.x, vessel_euler.y, 0));
        float level_roll = -Util.RAD2DEGf*Util.PolarAngle(qroll_level * Util.y)+90f;

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
        const float BLEND_HORIZON_C0 = 0.259f;
        const float BLEND_HORIZON_C1 = 0.342f;
        const float BLEND_VERT_C0 = 0.94f;
        const float BLEND_VERT_C1 = 0.966f;
        const float BLEND_LEVEL_GUIDE_C0 = 0.984f;
        const float BLEND_LEVEL_GUIDE_C1 = 0.996f;

#if DEBUG
        if (Input.GetKeyDown(KeyCode.O))
        {
            StringBuilder sb = new StringBuilder(8);
            sb.AppendLine("       qvessel = " + (qhorizoninv*qvessel).ToString("F3"));
            sb.AppendLine("       qlevel  = " + (qhorizoninv*qroll_level).ToString("F3"));
            sb.AppendLine("       qhorizon = " + qhorizon.ToString("F3"));
            sb.AppendLine("       euler    = " + vessel_euler.ToString("F3"));
            sb.AppendLine("       heading  = " + (qhorizoninv * heading).ToString("F2"));
            sb.AppendLine("       horizon_roll_z = " + (qcaminv * qhorizon * Util.z).ToString("F2"));
            Debug.Log(sb.ToString());
        }
#endif

        Color tmpColor = GUI.color;

        if (blend_horizon < BLEND_HORIZON_C1)
        {
            /* hproj is the projection of the heading onto the trangent plane of upAxis */
            Vector3 hproj = heading_in_cam_frame - Vector3.Project(heading_in_cam_frame, up_in_cam_frame);
            Vector3 horizon_marker_screen_position = Util.CameraToScreen(cam.mainCamera, hproj);
            if (CheckScreenPosition(cam.mainCamera, horizon_marker_screen_position)) // possible optimization: i think this check can be made before screen space projection
            {
                Color c = horizonColor;
                c.a *= blend_horizon > BLEND_HORIZON_C0 ? ((blend_horizon-BLEND_HORIZON_C1)/(BLEND_HORIZON_C0-BLEND_HORIZON_C1)) : 1f;
                GUI.color = c;

                GUIUtility.RotateAroundPivot(camroll, horizon_marker_screen_position); // big optimization opportunity here: construct matrix directly from position and direction vector!
                DrawMarker(AtlasItem.Horizon, horizon_marker_screen_position);
                GUI.matrix = Matrix4x4.identity;
            }
        }

        if (blend_vertical > BLEND_VERT_C0)
        {
            Vector3 vertical_screen_position = Util.CameraToScreen(cam.mainCamera, up_in_cam_frame);
            if (CheckScreenPosition(cam.mainCamera, vertical_screen_position))
            {
                Color c = horizonColor;
                c.a *= blend_vertical < BLEND_VERT_C1 ? ((blend_vertical-BLEND_VERT_C0)/(BLEND_VERT_C1-BLEND_VERT_C0)) : 1f;
                GUI.color = c;
                
                GUIUtility.RotateAroundPivot(vertical_roll, vertical_screen_position);
                DrawMarker(AtlasItem.Vertical, vertical_screen_position);
                GUI.matrix = Matrix4x4.identity;
            }
        }

        if (is_moving)
        {
            Vector3 screen_pos = Util.CameraToScreen(cam.mainCamera, speed_in_cam_frame);
            if (CheckScreenPosition(cam.mainCamera, screen_pos))
            {
                GUI.color = progradeColor;
                DrawMarker(speed_in_cam_frame.z > 0 ? AtlasItem.Prograde :AtlasItem.Retrograde, screen_pos);
            }
        }       

        {
            Vector3 screen_pos = Util.CameraToScreen(cam.mainCamera, heading_in_cam_frame);
            if (CheckScreenPosition(cam.mainCamera, screen_pos))
            {
                GUI.color = attitudeColor;
                GUIUtility.RotateAroundPivot(roll, screen_pos);
                DrawMarker(heading_in_cam_frame.z > 0 ? AtlasItem.Heading : AtlasItem.Reverse, screen_pos);
                GUI.matrix = Matrix4x4.identity;

                if (blend_level_guide < BLEND_LEVEL_GUIDE_C1)
                {
                    Color c = attitudeColor;
                    c.a *= blend_level_guide > BLEND_LEVEL_GUIDE_C0 ? ((blend_level_guide-BLEND_LEVEL_GUIDE_C1)/(BLEND_LEVEL_GUIDE_C0-BLEND_LEVEL_GUIDE_C1)) : 1f;
                    GUI.color = c;

                    GUIUtility.RotateAroundPivot(level_roll, screen_pos);
                    DrawMarker(AtlasItem.LevelGuide, screen_pos);
                    GUI.matrix = Matrix4x4.identity;
                }
            }
        }
        GUI.color = tmpColor;
	}
}

}