using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Places guards (GuardAI + NavMeshAgent) and security cameras
/// (CameraController + CameraDetection + VisionConeMesh) into Level04.
/// Menu: StealthHeist -> Add Guards and Cameras Level04
///
/// AFTER running this script:
///   1. Select every Guard root and assign the Player Transform
///      to the GuardAI "Player" field in the Inspector.
///   2. Open Window -> AI -> Navigation and bake a NavMesh
///      so guards can actually walk their patrol routes.
///   3. On each SecurityCamera DetectionZone, set the Obstacle Mask
///      and Player Mask layers that match your project setup.
/// </summary>
public static class Level04NPCPlacer
{
    const string SCENE_PATH = "Assets/Scenes/Level04.unity";

    // =========================================================================
    // Guard definitions
    // =========================================================================

    struct GuardDef
    {
        public string   Name;
        public Vector3  StartPos;
        public Vector3[] Waypoints;
    }

    static readonly GuardDef[] GuardDefs =
    {
        new GuardDef
        {
            Name     = "Guard_MainHall_1",
            StartPos = new Vector3(-8f, 0f, -6f),
            Waypoints = new Vector3[]
            {
                new Vector3(-8f, 0f, -6f),
                new Vector3( 8f, 0f, -6f),
                new Vector3( 8f, 0f,  6f),
                new Vector3(-8f, 0f,  6f),
            }
        },
        new GuardDef
        {
            Name     = "Guard_MainHall_2",
            StartPos = new Vector3(0f, 0f, -7f),
            Waypoints = new Vector3[]
            {
                new Vector3(-5f, 0f, -7f),
                new Vector3( 0f, 0f, -3f),
                new Vector3( 5f, 0f, -7f),
            }
        },
        new GuardDef
        {
            Name     = "Guard_BackRoom",
            StartPos = new Vector3(-3f, 0f, 13f),
            Waypoints = new Vector3[]
            {
                new Vector3(-3f, 0f, 13f),
                new Vector3( 3f, 0f, 13f),
                new Vector3( 3f, 0f, 17f),
                new Vector3(-3f, 0f, 17f),
            }
        },
    };

    // =========================================================================
    // Camera definitions
    // =========================================================================

    struct CamDef
    {
        public string  Name;
        public Vector3 Position;
        public Vector3 RootRotation;  // facing direction of the mount
        public float   MaxAngle;      // swing range for PingPong
    }

    static readonly CamDef[] CamDefs =
    {
        new CamDef { Name = "SecurityCamera_MainHall_N",  Position = new Vector3(  0f, 4f,  9.7f),  RootRotation = new Vector3(0, 180, 0), MaxAngle = 40f },
        new CamDef { Name = "SecurityCamera_MainHall_S",  Position = new Vector3(  0f, 4f, -9.7f),  RootRotation = new Vector3(0,   0, 0), MaxAngle = 40f },
        new CamDef { Name = "SecurityCamera_LeftWing",    Position = new Vector3(-24.7f, 4f,  0f),   RootRotation = new Vector3(0,  90, 0), MaxAngle = 35f },
        new CamDef { Name = "SecurityCamera_RightWing",   Position = new Vector3( 24.7f, 4f,  0f),   RootRotation = new Vector3(0, 270, 0), MaxAngle = 35f },
        new CamDef { Name = "SecurityCamera_BackRoom",    Position = new Vector3(  0f, 4f, 18.2f),  RootRotation = new Vector3(0, 180, 0), MaxAngle = 35f },
    };

    // =========================================================================
    // Entry point
    // =========================================================================

    [MenuItem("StealthHeist/Add Guards and Cameras Level04")]
    public static void Add()
    {
        try   { Execute(); }
        catch (Exception e)
        {
            Debug.LogError("[Level04NPCPlacer] " + e.Message + "\n" + e.StackTrace);
        }
    }

    static void Execute()
    {
        EnsureScene();

        // Level04_Map root
        GameObject mapRoot = GameObject.Find("Level04_Map");
        if (mapRoot == null)
            mapRoot = new GameObject("Level04_Map");

        Transform guardsParent = GetOrCreate(mapRoot.transform, "Guards");
        Transform camsParent   = GetOrCreate(mapRoot.transform, "SecurityCameras");

        // Shared materials (in-memory, not saved as assets)
        Material guardMat  = QuickMat(new Color(0.15f, 0.20f, 0.35f));  // navy blue
        Material camMat    = QuickMat(new Color(0.18f, 0.18f, 0.18f));  // dark grey

        // Try to use the existing CameraVision material for the cone
        Material visionMat = AssetDatabase.LoadAssetAtPath<Material>(
                                 "Assets/Materials/CameraVision.mat");
        if (visionMat == null)
            visionMat = QuickMat(new Color(1f, 0.85f, 0f, 0.25f));

        int guardsPlaced = PlaceGuards(guardsParent, guardMat);
        int camsPlaced   = PlaceCameras(camsParent,  camMat, visionMat);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[Level04NPCPlacer] Placed " + guardsPlaced + " guard(s) and "
                  + camsPlaced + " camera(s).\n"
                  + "Next steps:\n"
                  + "  1. Assign the Player Transform to each GuardAI in the Inspector.\n"
                  + "  2. Bake a NavMesh (Window > AI > Navigation > Bake).\n"
                  + "  3. Set Obstacle Mask / Player Mask on each DetectionZone.");
    }

    // =========================================================================
    // Guard placement
    // =========================================================================

    static int PlaceGuards(Transform parent, Material mat)
    {
        int count = 0;
        foreach (var def in GuardDefs)
        {
            if (parent.Find(def.Name) != null)
            {
                Debug.Log("[Level04NPCPlacer] " + def.Name + " already exists, skipping.");
                continue;
            }

            // ── Root object ───────────────────────────────────────────────────
            var root = new GameObject(def.Name);
            root.transform.SetParent(parent);
            root.transform.position = def.StartPos;

            // ── Visual (capsule) ──────────────────────────────────────────────
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "GuardBody";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale    = Vector3.one;
            body.GetComponent<Renderer>().sharedMaterial = mat;

            // ── NavMeshAgent ──────────────────────────────────────────────────
            var agent = root.AddComponent<NavMeshAgent>();
            agent.height           = 2f;
            agent.radius           = 0.4f;
            agent.speed            = 3.5f;
            agent.angularSpeed     = 120f;
            agent.stoppingDistance = 0.5f;

            // ── GuardAI ───────────────────────────────────────────────────────
            var ai = root.AddComponent<GuardAI>();
            ai.visionRange        = 15f;
            ai.visionAngle        = 60f;
            ai.waitTimeAtWaypoint = 2f;
            // ai.player is intentionally left null – assign in Inspector at runtime

            // ── Waypoints ─────────────────────────────────────────────────────
            var wpRoot = new GameObject("Waypoints");
            wpRoot.transform.SetParent(root.transform);

            Transform[] wpTransforms = new Transform[def.Waypoints.Length];
            for (int i = 0; i < def.Waypoints.Length; i++)
            {
                var wp = new GameObject("Waypoint_" + (i + 1));
                wp.transform.SetParent(wpRoot.transform);
                wp.transform.position = def.Waypoints[i];
                wpTransforms[i] = wp.transform;
            }
            ai.waypoints = wpTransforms;

            count++;
        }
        return count;
    }

    // =========================================================================
    // Camera placement
    // =========================================================================

    static int PlaceCameras(Transform parent, Material camMat, Material visionMat)
    {
        int count = 0;
        foreach (var def in CamDefs)
        {
            if (parent.Find(def.Name) != null)
            {
                Debug.Log("[Level04NPCPlacer] " + def.Name + " already exists, skipping.");
                continue;
            }

            // ── Root (wall anchor, sets facing direction) ──────────────────────
            var root = new GameObject(def.Name);
            root.transform.SetParent(parent);
            root.transform.position = def.Position;
            root.transform.rotation = Quaternion.Euler(def.RootRotation);

            // ── Mount (rotated by CameraController) ───────────────────────────
            var mount = new GameObject("CameraMount");
            mount.transform.SetParent(root.transform);
            mount.transform.localPosition = Vector3.zero;
            mount.transform.localRotation = Quaternion.identity;

            var ctrl = mount.AddComponent<CameraController>();
            ctrl.rotationMode  = CameraController.RotationMode.PingPong;
            ctrl.rotationSpeed = 25f;
            ctrl.maxAngle      = def.MaxAngle;
            ctrl.startAngle    = 0f;
            ctrl.verticalAngle = -20f;     // tilt downward
            ctrl.pauseTime     = 0.8f;
            ctrl.enableRotation  = true;
            ctrl.enableDetection = true;
            ctrl.showVisionCone  = true;

            // ── Camera housing (flat box) ──────────────────────────────────────
            var housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.name = "CameraHousing";
            housing.transform.SetParent(mount.transform);
            housing.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            housing.transform.localScale    = new Vector3(0.30f, 0.20f, 0.35f);
            housing.GetComponent<Renderer>().sharedMaterial = camMat;
            UnityEngine.Object.DestroyImmediate(housing.GetComponent<BoxCollider>());

            // ── Lens (small cylinder at front) ─────────────────────────────────
            var lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lens.name = "CameraLens";
            lens.transform.SetParent(housing.transform);
            lens.transform.localPosition = new Vector3(0f, 0f,  0.65f);
            lens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lens.transform.localScale    = new Vector3(0.35f, 0.08f, 0.35f);
            lens.GetComponent<Renderer>().sharedMaterial = camMat;
            UnityEngine.Object.DestroyImmediate(lens.GetComponent<CapsuleCollider>());

            // ── Camera Eye (raycast origin) ────────────────────────────────────
            var eye = new GameObject("CameraEye");
            eye.transform.SetParent(mount.transform);
            eye.transform.localPosition = new Vector3(0f, 0f, 0.4f);

            // ── Vision Cone (VisionConeMesh) ───────────────────────────────────
            var coneObj = new GameObject("VisionCone");
            coneObj.transform.SetParent(mount.transform);
            coneObj.transform.localPosition = Vector3.zero;
            coneObj.transform.localRotation = Quaternion.identity;

            coneObj.AddComponent<MeshFilter>();
            var coneMR = coneObj.AddComponent<MeshRenderer>();
            coneMR.sharedMaterial = visionMat;

            var coneScript = coneObj.AddComponent<VisionConeMesh>();
            coneScript.viewAngle    = 60f;
            coneScript.viewDistance = 10f;
            coneScript.rayCount     = 30;

            ctrl.visionCone = coneObj;

            // ── Detection Zone (SphereCollider trigger + CameraDetection) ──────
            var detZone = new GameObject("DetectionZone");
            detZone.transform.SetParent(mount.transform);
            detZone.transform.localPosition = Vector3.zero;

            var sphere = detZone.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius    = 10f;

            var detection = detZone.AddComponent<CameraDetection>();
            detection.cameraEye = eye.transform;
            // obstacleMask / playerMask: set in Inspector to match your layers

            count++;
        }
        return count;
    }

    // =========================================================================
    // Utilities
    // =========================================================================

    static Transform GetOrCreate(Transform parent, string name)
    {
        var found = parent.Find(name);
        if (found != null) return found;
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.transform;
    }

    static Material QuickMat(Color c)
    {
        Shader sh = Shader.Find("Standard") ?? GetDefaultShader();
        return new Material(sh) { color = c };
    }

    static Shader GetDefaultShader()
    {
        var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var sh    = probe.GetComponent<Renderer>().sharedMaterial.shader;
        UnityEngine.Object.DestroyImmediate(probe);
        return sh;
    }

    static void EnsureScene()
    {
        if (SceneManager.GetActiveScene().name == "Level04") return;
        string abs = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName, SCENE_PATH);
        if (File.Exists(abs))
            EditorSceneManager.OpenScene(SCENE_PATH);
        else
            throw new Exception(
                "Level04.unity not found. Run 'StealthHeist/Build Level 04 - Ancient Gallery' first.");
    }
}
