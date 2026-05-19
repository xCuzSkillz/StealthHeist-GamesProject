using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Downloads PBR textures from AmbientCG and applies them to the Level04 scene.
/// Menu: StealthHeist -> Build Level04 Textured
/// Run AFTER "Build Level 04 - Ancient Gallery" so the scene objects exist.
/// </summary>
public static class Level04TextureBuilder
{
    // ── Texture descriptor ────────────────────────────────────────────────────

    class TexSet
    {
        public string  Url;
        public Vector2 Tiling;
        public Color   Fallback;
        public Material Mat;   // populated at runtime
    }

    // ── State ─────────────────────────────────────────────────────────────────

    static string  absTemp;                          // absolute path  Assets/TempTextures/
    static string  relTemp = "Assets/TempTextures/"; // asset-relative path
    static Shader  defaultShader;                    // pipeline-safe shader

    static TexSet sFloor, sWall, sPillar, sWood, sMetal, sPedestal;
    static Material matGlass;

    // ── Entry point ───────────────────────────────────────────────────────────

    [MenuItem("StealthHeist/Build Level04 Textured")]
    public static void Build()
    {
        try   { Run(); }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[Level04TextureBuilder] " + e.Message + "\n" + e.StackTrace);
        }
    }

    static void Run()
    {
        // 1 ── Ensure Level04 scene is open ───────────────────────────────────
        EnsureScene();

        // 2 ── Temp folder ─────────────────────────────────────────────────────
        absTemp = Application.dataPath + "/TempTextures/";
        Directory.CreateDirectory(absTemp);

        // 3 ── Detect pipeline-safe shader from a throwaway primitive ─────────
        var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        defaultShader = probe.GetComponent<Renderer>().sharedMaterial.shader;
        UnityEngine.Object.DestroyImmediate(probe);

        // 4 ── Define texture sets ─────────────────────────────────────────────
        sFloor    = new TexSet { Url = "https://ambientcg.com/get?file=Marble021_1K-PNG.zip",
                                 Tiling = new Vector2(4,4),
                                 Fallback = new Color(0.76f, 0.70f, 0.50f) };

        sWall     = new TexSet { Url = "https://ambientcg.com/get?file=Rock022_1K-PNG.zip",
                                 Tiling = new Vector2(2,2),
                                 Fallback = new Color(0.55f, 0.52f, 0.48f) };

        sPillar   = new TexSet { Url = "https://ambientcg.com/get?file=Concrete034_1K-PNG.zip",
                                 Tiling = new Vector2(1,3),
                                 Fallback = new Color(0.40f, 0.38f, 0.35f) };

        sWood     = new TexSet { Url = "https://ambientcg.com/get?file=Wood049_1K-PNG.zip",
                                 Tiling = new Vector2(1,1),
                                 Fallback = new Color(0.35f, 0.22f, 0.12f) };

        sMetal    = new TexSet { Url = "https://ambientcg.com/get?file=Metal032_1K-PNG.zip",
                                 Tiling = new Vector2(1,1),
                                 Fallback = new Color(0.45f, 0.45f, 0.45f) };

        sPedestal = new TexSet { Url = "https://ambientcg.com/get?file=Marble017_1K-PNG.zip",
                                 Tiling = new Vector2(1,1),
                                 Fallback = new Color(0.85f, 0.76f, 0.60f) };

        var sets = new (string Label, TexSet Set)[]
        {
            ("Floor – Marble",     sFloor),
            ("Walls – Rock",       sWall),
            ("Pillars – Concrete", sPillar),
            ("Wood Props",         sWood),
            ("Metal Props",        sMetal),
            ("Pedestal – Marble",  sPedestal),
        };

        // 5 ── Download + build materials ─────────────────────────────────────
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        for (int i = 0; i < sets.Length; i++)
        {
            EditorUtility.DisplayProgressBar(
                "Level04 – Downloading Textures",
                sets[i].Label + "  (" + (i + 1) + "/" + sets.Length + ")",
                (float)i / sets.Length);

            sets[i].Set.Mat = DownloadAndBuild(sets[i].Set);
        }

        // 6 ── Glass (no download) ─────────────────────────────────────────────
        matGlass = BuildGlass();

        // 7 ── Apply to scene objects ──────────────────────────────────────────
        EditorUtility.DisplayProgressBar("Level04 – Textures", "Applying materials to scene...", 0.97f);
        ApplyToScene();

        // 8 ── Finalise ────────────────────────────────────────────────────────
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.ClearProgressBar();
        Debug.Log("[Level04TextureBuilder] Level04 textures applied successfully!");
    }

    // ── Scene helper ──────────────────────────────────────────────────────────

    static void EnsureScene()
    {
        if (SceneManager.GetActiveScene().name == "Level04") return;

        string rel = "Assets/Scenes/Level04.unity";
        string abs = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName, rel);

        if (File.Exists(abs))
            EditorSceneManager.OpenScene(rel);
        else
            throw new Exception(
                "Level04.unity not found. Run 'StealthHeist/Build Level 04 - Ancient Gallery' first.");
    }

    // ── Download, extract, import, build material ──────────────────────────

    static Material DownloadAndBuild(TexSet s)
    {
        try
        {
            // ── Download ZIP ──────────────────────────────────────────────────
            string zipName = s.Url.Substring(s.Url.IndexOf("file=", StringComparison.Ordinal) + 5);
            string zipPath = Path.Combine(absTemp, zipName);
            string extractDir = Path.Combine(absTemp, Path.GetFileNameWithoutExtension(zipName));

            using (var client = new WebClient())
                client.DownloadFile(s.Url, zipPath);

            // ── Extract ZIP (flat – no sub-folder created) ────────────────────
            Directory.CreateDirectory(extractDir);
            using (var fs  = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;   // skip dir entries
                    string dest = Path.Combine(extractDir, entry.Name);
                    using (var src = entry.Open())
                    using (var dst = new FileStream(dest, FileMode.Create, FileAccess.Write))
                        src.CopyTo(dst);
                }
            }

            // ── Locate _Color.png ──────────────────────────────────────────────
            string[] colorFiles = Directory.GetFiles(extractDir, "*_Color.png");
            if (colorFiles.Length == 0)
            {
                Debug.LogWarning("[Level04TextureBuilder] No _Color.png in " + zipName + ". Fallback used.");
                return FallbackMat(s.Fallback);
            }

            // ── Copy + import colour texture ───────────────────────────────────
            string colorAbs = absTemp + Path.GetFileName(colorFiles[0]);
            string colorRel = relTemp + Path.GetFileName(colorFiles[0]);
            File.Copy(colorFiles[0], colorAbs, overwrite: true);
            AssetDatabase.ImportAsset(colorRel, ImportAssetOptions.ForceUpdate);
            Texture2D colorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(colorRel);

            // ── Build material ─────────────────────────────────────────────────
            var mat = new Material(defaultShader);
            if (colorTex != null)
            {
                mat.mainTexture      = colorTex;
                mat.mainTextureScale = s.Tiling;
            }
            else
            {
                mat.color = s.Fallback;
            }

            // ── Normal map ────────────────────────────────────────────────────
            string[] normalFiles = Directory.GetFiles(extractDir, "*_NormalGL.png");
            if (normalFiles.Length > 0)
            {
                string normalAbs = absTemp + Path.GetFileName(normalFiles[0]);
                string normalRel = relTemp + Path.GetFileName(normalFiles[0]);
                File.Copy(normalFiles[0], normalAbs, overwrite: true);

                AssetDatabase.ImportAsset(normalRel, ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(normalRel) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    AssetDatabase.ImportAsset(normalRel, ImportAssetOptions.ForceUpdate);
                }

                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalRel);
                if (normalTex != null)
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            return mat;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Level04TextureBuilder] Download failed for " +
                             s.Url + "\n" + e.Message + "\nUsing fallback colour.");
            return FallbackMat(s.Fallback);
        }
    }

    // ── Fallback flat-colour material ──────────────────────────────────────

    static Material FallbackMat(Color c)
    {
        return new Material(defaultShader) { color = c };
    }

    // ── Glass / transparent material ───────────────────────────────────────

    static Material BuildGlass()
    {
        var color = new Color(0.7f, 0.85f, 0.95f, 0.4f);

        // Standard pipeline (built-in)
        var std = Shader.Find("Standard");
        if (std != null)
        {
            var m = new Material(std);
            m.SetFloat("_Mode", 3);   // Transparent
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            m.color = color;
            return m;
        }

        // URP
        var urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp != null)
        {
            var m = new Material(urp);
            m.SetFloat("_Surface", 1f);   // 0=Opaque 1=Transparent
            m.SetFloat("_Blend",   0f);   // Alpha
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.color = color;
            return m;
        }

        // Ultimate fallback – opaque tinted
        var f = new Material(defaultShader);
        f.color = new Color(color.r, color.g, color.b, 1f);
        return f;
    }

    // ── Apply materials to scene objects ───────────────────────────────────
    //
    // Object naming from Level04Builder:
    //   Floors   : MH_Floor, LW_Floor, RW_Floor, BR_Floor
    //   Walls    : MH_South_L/R/Top, MH_North_*, MH_West_*, MH_East_*,
    //              LW_West, LW_North, LW_South, RW_East, RW_North, RW_South,
    //              BR_North, BR_East, BR_West
    //   Pillars  : Pillar_1 … Pillar_6
    //   Pedestal : StatuePedestal   (tag: Artifact)
    //   Benches  : MH_Bench_*, LW_Bench
    //   Desk     : BR_Desk
    //   Door     : LockedDoor       (tag: LockedDoor)
    //   Lockers  : Locker_LW_*, Locker_RW, BR_Locker  (tag: LockedLocker)
    //   Displays : MH_Display_*, RW_Display_*
    // ──────────────────────────────────────────────────────────────────────

    static void ApplyToScene()
    {
        var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        int applied = 0;

        foreach (var go in allObjects)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) continue;

            string n   = go.name.ToLowerInvariant();
            string tag = go.tag;

            Material mat = null;

            // Order matters: check more specific names first
            if (n.Contains("floor"))
                mat = sFloor.Mat;
            else if (n.Contains("pillar"))
                mat = sPillar.Mat;
            else if (n.Contains("display"))
                mat = matGlass;
            else if (n.Contains("pedestal") || tag == "Artifact")
                mat = sPedestal.Mat;
            else if (n.Contains("bench") || n.Contains("desk"))
                mat = sWood.Mat;
            else if (tag == "LockedDoor" || n.Contains("door"))
                mat = sWood.Mat;
            else if (tag == "LockedLocker" || n.Contains("locker"))
                mat = sMetal.Mat;
            else if (n.Contains("north") || n.Contains("south") ||
                     n.Contains("east")  || n.Contains("west")  ||
                     n.Contains("wall"))
                mat = sWall.Mat;

            if (mat != null)
            {
                r.sharedMaterial = mat;
                applied++;
            }
        }

        Debug.Log("[Level04TextureBuilder] Materials applied to " + applied + " objects.");
    }
}
