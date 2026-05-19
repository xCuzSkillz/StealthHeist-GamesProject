using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CeilingBuilder
{
    const string SCENE_PATH = "Assets/Scenes/Level04.unity";
    const string MAT_PATH   = "Assets/Materials/CeilingMaterial.mat";

    [MenuItem("StealthHeist/Add Ceilings Level04")]
    public static void Add()
    {
        try
        {
            Execute();
        }
        catch (Exception e)
        {
            Debug.LogError("[CeilingBuilder] " + e.Message);
        }
    }

    static void Execute()
    {
        // Open Level04 scene if not already active
        if (SceneManager.GetActiveScene().name != "Level04")
        {
            string abs = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                SCENE_PATH);
            if (File.Exists(abs))
                EditorSceneManager.OpenScene(SCENE_PATH);
            else
            {
                Debug.LogError("[CeilingBuilder] Level04.unity not found.");
                return;
            }
        }

        // Ensure Ceiling tag exists
        AddTag("Ceiling");

        // Find or create Level04_Map
        GameObject mapRoot = GameObject.Find("Level04_Map");
        if (mapRoot == null)
        {
            Debug.LogWarning("[CeilingBuilder] Level04_Map not found, creating it.");
            mapRoot = new GameObject("Level04_Map");
        }

        // Find or create Ceilings group
        Transform ceilParent = mapRoot.transform.Find("Ceilings");
        if (ceilParent == null)
        {
            GameObject ceilGroup = new GameObject("Ceilings");
            ceilGroup.transform.SetParent(mapRoot.transform);
            ceilParent = ceilGroup.transform;
        }

        // Create ceiling material
        Material mat = MakeMaterial();

        // Ceiling data: name, x, y, z, scaleX, scaleY, scaleZ
        float[,] data = new float[,]
        {
            //  x      y   z    sX    sY   sZ
            {   0f,   5f,  0f,  30f,  0.3f, 20f },   // Main Hall
            { -15f,   5f,  0f,  10f,  0.3f, 10f },   // Left Wing
            {  15f,   5f,  0f,  10f,  0.3f, 10f },   // Right Wing
            {   0f,   5f, 12f,  10f,  0.3f,  8f },   // Back Room
        };

        string[] names = {
            "Ceiling_MainHall",
            "Ceiling_LeftWing",
            "Ceiling_RightWing",
            "Ceiling_BackRoom"
        };

        int added = 0;
        for (int i = 0; i < names.Length; i++)
        {
            if (ceilParent.Find(names[i]) != null)
            {
                Debug.Log("[CeilingBuilder] Ceiling " + names[i] + " already exists, skipping");
                continue;
            }

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = names[i];
            slab.transform.SetParent(ceilParent);
            slab.transform.position   = new Vector3(data[i, 0], data[i, 1], data[i, 2]);
            slab.transform.localScale = new Vector3(data[i, 3], data[i, 4], data[i, 5]);
            slab.tag = "Ceiling";

            MeshRenderer mr = slab.GetComponent<MeshRenderer>();
            mr.sharedMaterial    = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = true;

            added++;
        }

        // Light fixtures above each point light
        int fixtures = AddLightFixtures(ceilParent);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[CeilingBuilder] Success: Added " + added + " ceilings and "
                  + fixtures + " light fixtures to Level04");
    }

    static Material MakeMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
        if (existing != null)
            return existing;

        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shader = probe.GetComponent<Renderer>().sharedMaterial.shader;
            UnityEngine.Object.DestroyImmediate(probe);
        }

        Material mat = new Material(shader);
        mat.color = new Color(0.92f, 0.90f, 0.88f);
        mat.SetFloat("_Glossiness", 0.2f);
        mat.SetFloat("_Metallic", 0f);

        string absFolder = Application.dataPath + "/Materials/";
        if (!Directory.Exists(absFolder))
        {
            Directory.CreateDirectory(absFolder);
            AssetDatabase.Refresh();
        }

        AssetDatabase.CreateAsset(mat, MAT_PATH);
        return mat;
    }

    static int AddLightFixtures(Transform parent)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shader = probe.GetComponent<Renderer>().sharedMaterial.shader;
            UnityEngine.Object.DestroyImmediate(probe);
        }

        Material fixMat = new Material(shader);
        fixMat.color = new Color(0.85f, 0.85f, 0.80f);

        Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>();
        int count = 0;
        int idx   = 0;

        foreach (Light lt in lights)
        {
            if (lt.type != LightType.Point) continue;
            idx++;

            string fixName = "CeilingLight_Fixture_" + idx;
            if (parent.Find(fixName) != null) continue;

            GameObject fix = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fix.name = fixName;
            fix.transform.SetParent(parent);
            fix.transform.position   = new Vector3(
                lt.transform.position.x, 5f, lt.transform.position.z);
            fix.transform.localScale = new Vector3(1f, 0.1f, 1f);

            MeshRenderer mr = fix.GetComponent<MeshRenderer>();
            mr.sharedMaterial    = fixMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;

            Collider col = fix.GetComponent<Collider>();
            if (col != null)
                UnityEngine.Object.DestroyImmediate(col);

            count++;
        }

        return count;
    }

    static void AddTag(string tag)
    {
        UnityEngine.Object asset =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                "ProjectSettings/TagManager.asset");
        SerializedObject so  = new SerializedObject(asset);
        SerializedProperty arr = so.FindProperty("tags");

        for (int i = 0; i < arr.arraySize; i++)
            if (arr.GetArrayElementAtIndex(i).stringValue == tag) return;

        arr.InsertArrayElementAtIndex(arr.arraySize);
        arr.GetArrayElementAtIndex(arr.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }
}
