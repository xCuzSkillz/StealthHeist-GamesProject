using UnityEngine;
using UnityEditor;

public static class Level1WhiteboxBuilder
{
    private const string RootName = "Level1_Offices";
    private const string MaterialsFolder = "Assets/Materials";
    private const string DefaultMatPath = "Assets/Materials/Whitebox_Default.mat";
    private const string ExitMatPath = "Assets/Materials/Whitebox_Exit.mat";

    private struct HorizontalWall { public int id; public float z, x0, x1; }
    private struct VerticalWall { public int id; public float x, z0, z1; }

    private static readonly HorizontalWall[] Horizontals = new HorizontalWall[]
    {
        new HorizontalWall { id = 1,  z = -2.5f, x0 = 13.25f, x1 = 20.25f },
        new HorizontalWall { id = 2,  z = 0f,    x0 = 0f,     x1 = 8f     },
        new HorizontalWall { id = 3,  z = 4.5f,  x0 = 13.25f, x1 = 15.75f },
        new HorizontalWall { id = 4,  z = 4.5f,  x0 = 17.75f, x1 = 20.25f },
        new HorizontalWall { id = 5,  z = 6f,    x0 = 0f,     x1 = 3f     },
        new HorizontalWall { id = 6,  z = 6f,    x0 = 5f,     x1 = 8f     },
        new HorizontalWall { id = 7,  z = 6f,    x0 = 8f,     x1 = 15f    },
        new HorizontalWall { id = 8,  z = 18f,   x0 = 0f,     x1 = 15f    },
        new HorizontalWall { id = 9,  z = 19.5f, x0 = 13.25f, x1 = 15.75f },
        new HorizontalWall { id = 10, z = 19.5f, x0 = 17.75f, x1 = 20.25f },
        new HorizontalWall { id = 11, z = 26.5f, x0 = 13.25f, x1 = 20.25f },
        new HorizontalWall { id = 12, z = 9.5f,  x0 = 18.5f,  x1 = 24.5f  },
        new HorizontalWall { id = 13, z = 14.5f, x0 = 18.5f,  x1 = 24.5f  },
    };

    private static readonly VerticalWall[] Verticals = new VerticalWall[]
    {
        new VerticalWall { id = 14, x = 0f,     z0 = 0f,    z1 = 18f   },
        new VerticalWall { id = 15, x = 8f,     z0 = 0f,    z1 = 6f    },
        new VerticalWall { id = 16, x = 13.25f, z0 = -2.5f, z1 = 4.5f  },
        new VerticalWall { id = 17, x = 13.25f, z0 = 19.5f, z1 = 26.5f },
        new VerticalWall { id = 18, x = 15f,    z0 = 4.5f,  z1 = 11f   },
        new VerticalWall { id = 19, x = 15f,    z0 = 13f,   z1 = 19.5f },
        new VerticalWall { id = 20, x = 18.5f,  z0 = 4.5f,  z1 = 11f   },
        new VerticalWall { id = 21, x = 18.5f,  z0 = 13f,   z1 = 19.5f },
        new VerticalWall { id = 22, x = 20.25f, z0 = -2.5f, z1 = 4.5f  },
        new VerticalWall { id = 23, x = 20.25f, z0 = 19.5f, z1 = 26.5f },
        new VerticalWall { id = 24, x = 24.5f,  z0 = 9.5f,  z1 = 14.5f },
    };

    [MenuItem("StealthHeist/Build Level 1 Whitebox")]
    public static void BuildWhitebox()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        Material defaultMat = EnsureMaterial(DefaultMatPath, new Color(0.85f, 0.85f, 0.85f));
        Material exitMat = EnsureMaterial(ExitMatPath, Color.red);

        GameObject root = new GameObject(RootName);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.position = new Vector3(12.25f, -0.05f, 12f);
        floor.transform.localScale = new Vector3(26f, 0.1f, 31f);
        floor.GetComponent<Renderer>().sharedMaterial = defaultMat;

        GameObject walls = new GameObject("Walls");
        walls.transform.SetParent(root.transform, false);

        foreach (var w in Horizontals)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Wall_{w.id:D2}";
            go.transform.SetParent(walls.transform, false);
            go.transform.position = new Vector3((w.x0 + w.x1) * 0.5f, 1.5f, w.z);
            go.transform.localScale = new Vector3(w.x1 - w.x0, 3f, 0.2f);
            go.GetComponent<Renderer>().sharedMaterial = defaultMat;
        }

        foreach (var w in Verticals)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Wall_{w.id:D2}";
            go.transform.SetParent(walls.transform, false);
            go.transform.position = new Vector3(w.x, 1.5f, (w.z0 + w.z1) * 0.5f);
            go.transform.localScale = new Vector3(0.2f, 3f, w.z1 - w.z0);
            go.GetComponent<Renderer>().sharedMaterial = defaultMat;
        }

        GameObject markers = new GameObject("Markers");
        markers.transform.SetParent(root.transform, false);

        GameObject spawn = new GameObject("Spawn");
        spawn.transform.SetParent(markers.transform, false);
        spawn.transform.position = new Vector3(4f, 0f, 3f);

        GameObject exit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exit.name = "Exit";
        exit.transform.SetParent(markers.transform, false);
        exit.transform.position = new Vector3(21.5f, 0.5f, 12f);
        exit.transform.localScale = Vector3.one;
        exit.GetComponent<Renderer>().sharedMaterial = exitMat;

        GameObject lightGo = new GameObject("Directional Light");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Directional;
        l.shadows = LightShadows.Soft;

        Debug.Log($"[Level1WhiteboxBuilder] Built '{RootName}' whitebox: {Horizontals.Length + Verticals.Length} walls + floor + markers + light.");
    }

    private static Material EnsureMaterial(string path, Color color)
    {
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
