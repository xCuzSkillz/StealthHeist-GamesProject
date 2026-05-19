using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Places 9 paintings from Assets/Models/Level4/ onto the walls of the Level04 scene.
/// Menu: StealthHeist -> Place Paintings Level04
/// Run AFTER "Build Level 04 - Ancient Gallery" so the scene exists.
/// </summary>
public static class Level04PaintingPlacer
{
    // ── Painting descriptor ───────────────────────────────────────────────────

    struct Painting
    {
        public string  Id;        // "01" to "09"
        public Vector3 Position;
        public Vector3 Rotation;
        public string  WallNote;  // for log readability
    }

    static readonly Painting[] Paintings =
    {
        // ── Main Hall ──────────────────────────────────────────────────────────
        new Painting { Id = "01", Position = new Vector3( 0f,   2.5f,  9.5f), Rotation = new Vector3(0, 180, 0), WallNote = "Main Hall – North wall" },
        new Painting { Id = "02", Position = new Vector3(-5f,   2.5f, -9.5f), Rotation = new Vector3(0,   0, 0), WallNote = "Main Hall – South wall left" },
        new Painting { Id = "03", Position = new Vector3( 5f,   2.5f, -9.5f), Rotation = new Vector3(0,   0, 0), WallNote = "Main Hall – South wall right" },
        new Painting { Id = "04", Position = new Vector3(14.5f, 2.5f,  0f),   Rotation = new Vector3(0, 270, 0), WallNote = "Main Hall – East wall" },
        // ── Left Wing ──────────────────────────────────────────────────────────
        new Painting { Id = "05", Position = new Vector3(-14.5f, 2.5f,  5f),  Rotation = new Vector3(0,  90, 0), WallNote = "Left Wing – North wall" },
        new Painting { Id = "06", Position = new Vector3(-14.5f, 2.5f, -5f),  Rotation = new Vector3(0,  90, 0), WallNote = "Left Wing – South wall" },
        // ── Right Wing ─────────────────────────────────────────────────────────
        new Painting { Id = "07", Position = new Vector3(19.5f, 2.5f,  5f),   Rotation = new Vector3(0, 270, 0), WallNote = "Right Wing – North wall" },
        new Painting { Id = "08", Position = new Vector3(19.5f, 2.5f, -5f),   Rotation = new Vector3(0, 270, 0), WallNote = "Right Wing – South wall" },
        // ── Back Room ──────────────────────────────────────────────────────────
        new Painting { Id = "09", Position = new Vector3(0f,    2.5f, 14.5f), Rotation = new Vector3(0, 180, 0), WallNote = "Back Room – North wall" },
    };

    // ── Constants ─────────────────────────────────────────────────────────────

    const string MODELS_ROOT = "Assets/Models/Level4/";
    const string SCENE_PATH  = "Assets/Scenes/Level04.unity";

    static readonly Vector3 PAINTING_SCALE = new Vector3(1.5f, 1f, 0.05f);
    static readonly Color   FRAME_COLOR    = new Color(0.25f, 0.15f, 0.05f);

    // ── Entry point ───────────────────────────────────────────────────────────

    [MenuItem("StealthHeist/Place Paintings Level04")]
    public static void Place()
    {
        EnsureScene();
        EnsureTag("Painting");

        // Remove old Paintings group so re-running is safe
        var old = GameObject.Find("Paintings");
        if (old != null) Object.DestroyImmediate(old);

        var parent = new GameObject("Paintings").transform;

        // Build shared frame material from the pipeline-safe default shader
        var probe    = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var frameMat = new Material(probe.GetComponent<Renderer>().sharedMaterial.shader)
                       { color = FRAME_COLOR };
        Object.DestroyImmediate(probe);

        int placed = 0;
        foreach (var p in Paintings)
        {
            if (TryPlace(p, parent, frameMat))
                placed++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Level04PaintingPlacer] " + placed + " / 9 paintings placed. Press Ctrl+S to save.");
    }

    // ── Scene management ──────────────────────────────────────────────────────

    static void EnsureScene()
    {
        if (SceneManager.GetActiveScene().name == "Level04") return;

        string abs = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            SCENE_PATH);

        if (File.Exists(abs))
            EditorSceneManager.OpenScene(SCENE_PATH);
        else
            Debug.LogError("[Level04PaintingPlacer] Level04.unity not found. " +
                           "Run 'StealthHeist/Build Level 04 - Ancient Gallery' first.");
    }

    // ── Tag helper ────────────────────────────────────────────────────────────

    static void EnsureTag(string tag)
    {
        var mgr = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
        var arr = mgr.FindProperty("tags");

        for (int i = 0; i < arr.arraySize; i++)
            if (arr.GetArrayElementAtIndex(i).stringValue == tag) return;

        arr.InsertArrayElementAtIndex(arr.arraySize);
        arr.GetArrayElementAtIndex(arr.arraySize - 1).stringValue = tag;
        mgr.ApplyModifiedProperties();
    }

    // ── Place one painting ────────────────────────────────────────────────────

    static bool TryPlace(Painting data, Transform parent, Material frameMat)
    {
        // ── Validate folder ───────────────────────────────────────────────────
        string relFolder = MODELS_ROOT + data.Id + "/";
        string absFolder = Path.Combine(Application.dataPath, "Models", "Level4", data.Id);

        if (!Directory.Exists(absFolder))
        {
            Debug.LogWarning("[Level04PaintingPlacer] Warning: Painting folder " +
                             data.Id + " not found, skipping  (" + data.WallNote + ")");
            return false;
        }

        // ── Find FBX ──────────────────────────────────────────────────────────
        string[] fbxFiles = Directory.GetFiles(absFolder, "*.fbx", SearchOption.TopDirectoryOnly);

        if (fbxFiles.Length == 0)
        {
            Debug.LogWarning("[Level04PaintingPlacer] Warning: No FBX found in folder " +
                             data.Id + ", skipping  (" + data.WallNote + ")");
            return false;
        }

        string fbxRelPath = relFolder + Path.GetFileName(fbxFiles[0]);
        var    prefab     = AssetDatabase.LoadAssetAtPath<GameObject>(fbxRelPath);

        if (prefab == null)
        {
            Debug.LogWarning("[Level04PaintingPlacer] Warning: Could not load FBX at " +
                             fbxRelPath + ", skipping");
            return false;
        }

        // ── Create painting group ─────────────────────────────────────────────
        var group = new GameObject("Painting_" + data.Id);
        group.transform.SetParent(parent);
        group.transform.position = data.Position;
        group.transform.rotation = Quaternion.Euler(data.Rotation);
        group.tag = "Painting";

        // ── Instantiate FBX mesh ──────────────────────────────────────────────
        GameObject mesh = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (mesh == null)
            mesh = Object.Instantiate(prefab);   // fallback: plain copy

        mesh.name = "Mesh_" + data.Id;
        mesh.transform.SetParent(group.transform);
        mesh.transform.localPosition = Vector3.zero;
        mesh.transform.localRotation = Quaternion.identity;
        mesh.transform.localScale    = PAINTING_SCALE;
        mesh.tag = "Painting";

        // Tag any child renderers too
        foreach (Transform child in mesh.transform)
            if (child.GetComponent<Renderer>() != null)
                child.gameObject.tag = "Painting";

        // ── Create frame (dark-brown cube behind the mesh) ────────────────────
        var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "Frame_" + data.Id;
        frame.transform.SetParent(group.transform);
        frame.transform.localPosition = new Vector3(0f, 0f, -0.05f);   // slightly behind
        frame.transform.localRotation = Quaternion.identity;
        frame.transform.localScale    = new Vector3(
            PAINTING_SCALE.x + 0.2f,   // 1.7
            PAINTING_SCALE.y + 0.2f,   // 1.2
            0.05f);
        frame.GetComponent<Renderer>().sharedMaterial = frameMat;

        Debug.Log("[Level04PaintingPlacer] Placed Painting_" + data.Id + " – " + data.WallNote);
        return true;
    }
}
