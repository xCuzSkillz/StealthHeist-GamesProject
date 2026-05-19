using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class Level04Builder
{
    const string SCENE_PATH = "Assets/Scenes/Level04.unity";
    const float WALL_H = 5f;
    const float WALL_T = 1f;
    const float DOOR_W = 3f;
    const float DOOR_H = 4f;

    static Material matGrey;        // floors
    static Material matWall;        // painted white walls
    static Material matDarkGrey;    // pillars
    static Material matBeige;       // pedestal
    static Material matDarkGreen;   // lockers
    static Material matDarkBrown;   // benches, desks, door
    static Transform tWalls, tProps, tLights, tDoors;

    [MenuItem("StealthHeist/Build Level 04 - Ancient Gallery")]
    public static void Build()
    {
        EnsureScene();
        MakeMaterials();
        EnsureTags("Artifact", "LockedLocker", "LockedDoor", "HidingSpot");
        SetupHierarchy();
        BuildMainHall();
        BuildLeftWing();
        BuildRightWing();
        BuildBackRoom();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Level04Builder] Done. Press Ctrl+S to save.");
    }

    static void EnsureScene()
    {
        if (SceneManager.GetActiveScene().name == "Level04") return;
        string abs = System.IO.Path.Combine(
            System.IO.Directory.GetParent(Application.dataPath).FullName, SCENE_PATH);
        if (System.IO.File.Exists(abs))
            EditorSceneManager.OpenScene(SCENE_PATH);
        else
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
        }
    }

    static void SetupHierarchy()
    {
        var old = GameObject.Find("Level04_Map");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Level04_Map").transform;
        tWalls  = MakeGroup(root, "Walls");
        tProps  = MakeGroup(root, "Props");
        tLights = MakeGroup(root, "Lights");
        tDoors  = MakeGroup(root, "Doors");
    }

    static Transform MakeGroup(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.transform;
    }

    static void MakeMaterials()
    {
        matGrey      = Mat(new Color(0.60f, 0.60f, 0.60f));
        matWall      = Mat(new Color(0.95f, 0.95f, 0.92f));   // painted white
        matDarkGrey  = Mat(new Color(0.28f, 0.28f, 0.28f));
        matBeige     = Mat(new Color(0.85f, 0.76f, 0.60f));
        matDarkGreen = Mat(new Color(0.15f, 0.32f, 0.15f));
        matDarkBrown = Mat(new Color(0.28f, 0.16f, 0.07f));
    }

    static Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        if (m.shader == null || !m.shader.isSupported)
            m.shader = Shader.Find("Legacy Shaders/Diffuse");
        m.color = c;
        return m;
    }

    static void EnsureTags(params string[] tags)
    {
        var mgr = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
        var arr = mgr.FindProperty("tags");
        foreach (var tag in tags)
        {
            bool exists = false;
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
            if (!exists)
            {
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).stringValue = tag;
            }
        }
        mgr.ApplyModifiedProperties();
    }

    static void BuildMainHall()
    {
        Floor("MH_Floor", 0f, 0f, 30f, 20f);
        NSWallGap("MH_South", 0f, -10.5f, 32f, 0f);
        NSWallGap("MH_North", 0f,  10.5f, 32f, 0f);
        EWWallGap("MH_West", -15.5f, 0f, 20f, 0f);
        EWWallGap("MH_East",  15.5f, 0f, 20f, 0f);

        float[] pillarX = { -8f, 8f };
        float[] pillarZ = { -6f, 0f, 6f };
        int idx = 0;
        foreach (float px in pillarX)
            foreach (float pz in pillarZ)
            {
                var p = Cylinder("Pillar_" + (++idx), px, 2.5f, pz, 1f, 5f, matDarkGrey, tProps);
                Tag(p, "HidingSpot");
            }

        DisplayCase("MH_Display_1", -14.5f, -5f);
        DisplayCase("MH_Display_2", -14.5f,  5f);
        DisplayCase("MH_Display_3",  14.5f, -5f);
        DisplayCase("MH_Display_4",  14.5f,  5f);

        var pedestal = Cylinder("StatuePedestal", 0f, 0.5f, 0f, 1f, 1f, matBeige, tProps);
        Tag(pedestal, "Artifact");

        var b1 = Bench("MH_Bench_1",   0f, -7f);
        var b2 = Bench("MH_Bench_2", -10f,  0f);
        var b3 = Bench("MH_Bench_3",  10f,  0f);
        Tag(b1, "HidingSpot"); Tag(b2, "HidingSpot"); Tag(b3, "HidingSpot");

        AddLight("MH_Light", 0f, 4.5f, 0f, new Color(1f, 0.92f, 0.70f));
    }

    static void BuildLeftWing()
    {
        Floor("LW_Floor", -20f, 0f, 10f, 10f);
        EWWall("LW_West",  -25.5f,  0f, 12f);
        NSWall("LW_North", -20f,   5.5f, 10f);
        NSWall("LW_South", -20f,  -5.5f, 10f);

        var lk1 = Box("Locker_LW_1", -24.6f, 1f, -2f, 0.8f, 2f, 0.8f, matDarkGreen, tProps);
        var lk2 = Box("Locker_LW_2", -24.6f, 1f,  2f, 0.8f, 2f, 0.8f, matDarkGreen, tProps);
        Tag(lk1, "LockedLocker"); Tag(lk2, "LockedLocker");

        var b = Bench("LW_Bench", -20f, 0f);
        Tag(b, "HidingSpot");
        AddLight("LW_Light", -20f, 4.5f, 0f, new Color(1f, 0.92f, 0.70f));
    }

    static void BuildRightWing()
    {
        Floor("RW_Floor", 20f, 0f, 10f, 10f);
        EWWall("RW_East",  25.5f,  0f, 12f);
        NSWall("RW_North", 20f,   5.5f, 10f);
        NSWall("RW_South", 20f,  -5.5f, 10f);

        var lk = Box("Locker_RW", 24.6f, 1f, 0f, 0.8f, 2f, 0.8f, matDarkGreen, tProps);
        Tag(lk, "LockedLocker");

        DisplayCase("RW_Display_1", 24.5f, -3f);
        DisplayCase("RW_Display_2", 24.5f,  3f);
        AddLight("RW_Light", 20f, 4.5f, 0f, new Color(1f, 0.92f, 0.70f));
    }

    static void BuildBackRoom()
    {
        Floor("BR_Floor", 0f, 14f, 10f, 8f);
        NSWall("BR_North",  0f,   18.5f, 12f);
        EWWall("BR_East",   5.5f, 14f,    8f);
        EWWall("BR_West",  -5.5f, 14f,    8f);

        var door = Box("LockedDoor", 0f, 2f, 10.5f, 3f, 4f, 0.2f, matDarkBrown, tDoors);
        Tag(door, "LockedDoor");

        Box("BR_Desk",   0f, 0.4f, 14f,   1.5f, 0.8f, 0.8f, matDarkBrown, tProps);
        Box("BR_Locker", -3f, 1f,  17.6f, 0.8f, 2f,   0.8f, matDarkGreen, tProps);
        AddLight("BR_Light", 0f, 4.5f, 14f, new Color(1f, 0.92f, 0.70f));
    }

    static void NSWall(string name, float cx, float cz, float len)
    {
        Box(name, cx, WALL_H / 2f, cz, len, WALL_H, WALL_T, matWall, tWalls);
    }

    static void EWWall(string name, float cx, float cz, float len)
    {
        Box(name, cx, WALL_H / 2f, cz, WALL_T, WALL_H, len, matWall, tWalls);
    }

    static void NSWallGap(string name, float cx, float cz, float len, float doorOffsetX)
    {
        float half = len / 2f;
        float segA = (doorOffsetX - DOOR_W / 2f) + half;
        float segB = half - (doorOffsetX + DOOR_W / 2f);
        float topH = WALL_H - DOOR_H;
        if (segA > 0.01f)
            Box(name + "_L", cx + (-half + segA / 2f), WALL_H / 2f, cz, segA, WALL_H, WALL_T, matWall, tWalls);
        if (segB > 0.01f)
            Box(name + "_R", cx + (half - segB / 2f),  WALL_H / 2f, cz, segB, WALL_H, WALL_T, matWall, tWalls);
        if (topH > 0.01f)
            Box(name + "_Top", cx + doorOffsetX, DOOR_H + topH / 2f, cz, DOOR_W, topH, WALL_T, matWall, tWalls);
    }

    static void EWWallGap(string name, float cx, float cz, float len, float doorOffsetZ)
    {
        float half = len / 2f;
        float segA = (doorOffsetZ - DOOR_W / 2f) + half;
        float segB = half - (doorOffsetZ + DOOR_W / 2f);
        float topH = WALL_H - DOOR_H;
        if (segA > 0.01f)
            Box(name + "_L", cx, WALL_H / 2f, cz + (-half + segA / 2f), WALL_T, WALL_H, segA, matWall, tWalls);
        if (segB > 0.01f)
            Box(name + "_R", cx, WALL_H / 2f, cz + (half - segB / 2f),  WALL_T, WALL_H, segB, matWall, tWalls);
        if (topH > 0.01f)
            Box(name + "_Top", cx, DOOR_H + topH / 2f, cz + doorOffsetZ, WALL_T, topH, DOOR_W, matWall, tWalls);
    }

    static void Floor(string name, float cx, float cz, float w, float d)
    {
        Box(name, cx, -0.1f, cz, w, 0.2f, d, matGrey, tWalls);
    }

    static void DisplayCase(string name, float x, float z)
    {
        Box(name, x, 0.25f, z, 1f, 0.5f, 1f, matGrey, tProps);
    }

    static GameObject Bench(string name, float x, float z)
    {
        return Box(name, x, 0.25f, z, 2f, 0.5f, 0.8f, matDarkBrown, tProps);
    }

    static GameObject Box(string name, float x, float y, float z,
        float sx, float sy, float sz, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position   = new Vector3(x, y, z);
        go.transform.localScale = new Vector3(sx, sy, sz);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject Cylinder(string name, float x, float y, float z,
        float diameter, float height, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position   = new Vector3(x, y, z);
        go.transform.localScale = new Vector3(diameter, height / 2f, diameter);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void Tag(GameObject go, string tag) => go.tag = tag;

    static void AddLight(string name, float x, float y, float z, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(tLights);
        go.transform.position = new Vector3(x, y, z);
        var lt = go.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.color     = color;
        lt.intensity = 1.5f;
        lt.range     = 15f;
    }
}




