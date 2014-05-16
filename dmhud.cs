using System;
using UnityEngine;
using System.Text;



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
}



public static class Util
{
    public static Vector3 x = Vector3.right;
    public static Vector3 y = Vector3.up;
    public static Vector3 z = Vector3.forward;

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
        return tex;
    }
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
    static Texture2D marker_prograde = null;
    static Texture2D marker_forward = null;
    static Texture2D marker_horizon = null;
    static Texture2D marker_retrograde = null;
    static Texture2D marker_backward = null;
    static Texture2D marker_dbg    = null;

    const  float param_speed_draw_threshold = 1.0e-1f;

    /* cached information about the vessel and the world.
     * Most of teh calculations are done in the draw pass
     * because i want to make sure the camera is up to date */
    Vector3 speed          = Vector3.zero;
    Quaternion qvessel    = Quaternion.identity;
    Vector3 up             = Util.z;
    Quaternion qhorizon    = Quaternion.identity;
    bool    data_valid     = false;
    //bool    active         = true;

    static Toolbar.IButton toolbarButton;

    void Awake()
    {
        toolbarButton = Toolbar.ToolbarManager.Instance.add("KerbalFlightIndicators", "damichelshud");
        toolbarButton.TexturePath = "KerbalFlightIndicators/toolbarbutton";
        toolbarButton.ToolTip = "KerbalFlightIndicators On/Off Switch";
        toolbarButton.Visibility = new Toolbar.GameScenesVisibility(GameScenes.FLIGHT);
        toolbarButton.Visible = true;
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
        settings.Save(AssemblyLoader.loadedAssemblies.GetPathByType(typeof(KerbalFlightIndicators)) + "/settings.cfg");
    }

    public void LoadSettings()
    {
        ConfigNode settings = new ConfigNode();
        settings = ConfigNode.Load(AssemblyLoader.loadedAssemblies.GetPathByType(typeof(KerbalFlightIndicators)) + @"\settings.cfg".Replace('/', '\\'));

        if (settings != null)
        {
            if (settings.HasValue("active")) enabled = bool.Parse(settings.GetValue("active"));
        }
    }


	void Start()
    {
        LoadSettings();
		RenderingManager.AddToPostDrawQueue(0, new Callback(OnDraw));

        if (marker_prograde == null)
        {
            marker_prograde = Util.LoadTexture("prograde.png",  32, 32);
        }

        if (marker_retrograde == null)
        {
            marker_retrograde = Util.LoadTexture("retrograde.png", 32, 32);
        }

        if (marker_forward == null)
        {
            marker_forward = Util.LoadTexture("heading.png", 32, 32);
        }

        if (marker_backward == null)
        {
            marker_backward = Util.LoadTexture("retroheading.png", 32, 32);
        }

        if (marker_horizon == null)
        {
            marker_horizon = Util.LoadTexture("horizon.png", 64, 4);
        }

        if (marker_dbg == null)
        {
            marker_dbg = new Texture2D(1, 1);
            marker_dbg.SetPixel(1, 1, Color.white);
            marker_dbg.Apply();
        }
	}


    public void OnDestroy()
    {
        SaveSettings();
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
        if (vessel.ReferenceTransform != null)
        {
            qvessel = vessel.ReferenceTransform.rotation;
        }
        else
        {
            qvessel = vessel.transform.rotation;
        }
        up = vessel.upAxis;
        Quaternion qfix = Quaternion.Euler(new Vector3(-90f, 0f, 0f));
        qvessel = qvessel * qfix;

        /* qhorizon is a frame where the y axis is parallel to vessel.upAxis and the other
         * axes are perpendicular to that. This tangent plane represents the horizon where
         * the pitch of the craft is 0.*/
        CelestialBody body = FlightGlobals.currentMainBody;
        if (body == null) return;

        Vector3 z = Vector3.Cross(body.angularVelocity, up);
        if (z.sqrMagnitude < 1.0e-9)
            z = Vector3.Cross(Util.y, up);
        if (z.sqrMagnitude < 1.0e-9)
            z = Vector3.Cross(Util.x, up);
        if (z.sqrMagnitude < 1.0e-9)
            return; // fail
        z.Normalize();
        qhorizon = Quaternion.LookRotation(z, up);

        data_valid = true;

#if DEBUG
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

	protected void OnDraw()
	{
        if (!data_valid || !enabled) return;
        
        FlightCamera cam = GetCamera();
        if (cam == null) return;


        Quaternion qcam     = cam.transform.rotation;
        /* qvessel points the z axis forward, i.e. when you look at your vessels from behind 
         * or from IVA, your camera z axis which is the direction you are looking to and
         * the z-axis of the qvessel frame are parallel */    
        Quaternion qroll = qhorizon.Inverse() * qvessel;
        float roll = qroll.eulerAngles.z;
                
        Quaternion qcamroll = qhorizon.Inverse() * qcam;
        qcamroll = qhorizon.Inverse() * qcam;
        float camroll = qcamroll.eulerAngles.z;

#if false
        Vector3 heading = qvessel * Util.z;
        Vector3 dh = heading + 0.001f * (qvessel * Util.x);
        Vector3 dx = Util.CameraToScreen(cam.mainCamera, cam.transform.InverseTransformDirection(dh)) - Util.CameraToScreen(cam.mainCamera, cam.transform.InverseTransformDirection(heading));
        float relative_roll = Util.PolarAngle(dx) * Mathf.Rad2Deg;
#endif

        Vector3 heading              = qvessel * Util.z;
        Vector3 up_in_cam_frame      = cam.transform.InverseTransformDirection(up);
        Vector3 speed_in_cam_frame   = cam.transform.InverseTransformDirection(speed);
        Vector3 heading_in_cam_frame = cam.transform.InverseTransformDirection(heading);
        /* hproj is the projection of the heading onto the trangent plane of upAxis */
        Vector3 hproj = heading_in_cam_frame - Vector3.Project(heading_in_cam_frame, up_in_cam_frame);
        /* here we have it on the screen */
        Vector3 horizon_marker_screen_position = Util.CameraToScreen(cam.mainCamera, hproj);

#if DEBUG
        if (Input.GetKeyDown(KeyCode.O))
        {
            StringBuilder sb = new StringBuilder(8);
            sb.AppendLine("roll = " + roll);
            sb.AppendLine("camroll = " + camroll);
            print(sb.ToString());
        }
#endif

        Color tmpColor = GUI.color;
        GUI.color = new Color(0f, 1f, 0f);
        int tmpDepth = GUI.depth;
        GUI.depth = 10;

        if (CheckScreenPosition(cam.mainCamera, horizon_marker_screen_position))
        {
            GUIUtility.RotateAroundPivot(camroll, horizon_marker_screen_position);
            GUIExt.DrawTextureCentered(horizon_marker_screen_position, marker_dbg, 256f, 1f);
            GUI.matrix = Matrix4x4.identity;
        }

        if (speed.magnitude > param_speed_draw_threshold)
        {
            Vector3 screen_pos = Util.CameraToScreen(cam.mainCamera, speed_in_cam_frame);
            if (CheckScreenPosition(cam.mainCamera, screen_pos))
            {
                /* I don't do relative_roll = (qvessel.Inverse() * qcam).eulerAngles.z on purpose. 
                 * It would make things dependend on the camera so that for some orientations the
                 * vessel roll relative to the horizon line as shown by the screen indicators
                 * would net represent their true orientations, i.e. showing parallel lines although
                 * actual roll is non-zero. */
                float relative_roll = camroll - Math.Sign(speed_in_cam_frame.z) * roll;

                GUIUtility.RotateAroundPivot(relative_roll, screen_pos);
                GUIExt.DrawTextureCentered(screen_pos, speed_in_cam_frame.z > 0 ? marker_prograde : marker_retrograde);
                GUI.matrix = Matrix4x4.identity;
            }
        }       

        {
            Vector3 screen_pos = Util.CameraToScreen(cam.mainCamera, heading_in_cam_frame);
            if (CheckScreenPosition(cam.mainCamera, screen_pos))
            {
                float relative_roll = camroll - Math.Sign(heading_in_cam_frame.z) * roll;

                GUIUtility.RotateAroundPivot(relative_roll, screen_pos);
                GUIExt.DrawTextureCentered(screen_pos, heading_in_cam_frame.z > 0 ? marker_forward : marker_backward);
                GUI.matrix = Matrix4x4.identity;

                GUIUtility.RotateAroundPivot(camroll, screen_pos);
                GUIExt.DrawTextureCentered(screen_pos, marker_horizon);
                GUI.matrix = Matrix4x4.identity;
            }
        }
        GUI.color = tmpColor;
        GUI.depth = tmpDepth;
	}
}