using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Downloads Plaster001 from AmbientCG and applies it to every wall
/// (and optionally ceiling) object in the Level04 Ancient Gallery scene.
/// Menu: StealthHeist -> Fix Wall Material Level04
/// </summary>
public static class WallMaterialFixer
{
    // ── Constants ─────────────────────────────────────────────────────────────

    const string SCENE_PATH = "Assets/Scenes/Level04.unity";
    const string ZIP_URL    = "https://ambientcg.com/get?file=Plaster001_1K-PNG.zip";
    const string TEX_REL    = "Assets/Textures/Walls/";
    const string MAT_PATH   = "Assets/Materials/WallPlaster.mat";
    const string CEIL_PATH  = "Assets/Materials/WallPlaster_Ceiling.mat";

    // ── Runtime paths (set in Execute) ────────────────────────────────────────

    static string absTexDir;

    // ── Entry point ───────────────────────────────────────────────────────────

    [MenuItem("StealthHeist/Fix Wall Material Level04")]
    public static void Run()
    {
        try   { Execute(); }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[WallMaterialFixer] Fatal error: " + e.Message + "\n" + e.StackTrace);
        }
    }

    // ── Main flow ─────────────────────────────────────────────────────────────

    static void Execute()
    {
        EnsureScene();

        absTexDir = Application.dataPath + "/Textures/Walls/";
        Directory.CreateDirectory(absTexDir);

        // ── Build or fall back to flat material ───────────────────────────────
        Material wallMat;
        try
        {
            wallMat = DownloadAndBuild();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[WallMaterialFixer] Texture download failed, using fallback color.\n" + e.Message);
            wallMat = FallbackMat();
        }

        // ── Save as persistent asset ───────────────────────────────────────────
        EditorUtility.DisplayProgressBar("Fix Wall Material", "Saving material asset...", 0.86f);
        wallMat = PersistMaterial(wallMat, MAT_PATH, new Vector2(3f, 3f));

        // ── Ceiling variant (same textures, 2x2 tiling) ───────────────────────
        Material ceilMat = new Material(wallMat);
        ceilMat.mainTextureScale = new Vector2(2f, 2f);
        ceilMat = PersistMaterial(ceilMat, CEIL_PATH, new Vector2(2f, 2f));

        // ── Apply to scene objects ─────────────────────────────────────────────
        EditorUtility.DisplayProgressBar("Fix Wall Material", "Applying to scene walls...", 0.93f);
        int wallCount = ApplyMaterials(wallMat, ceilMat);

        // ── Finalise ──────────────────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.ClearProgressBar();

        if (wallCount == 0)
            Debug.LogWarning("[WallMaterialFixer] Warning: No wall objects found in Level04");
        else
            Debug.Log("[WallMaterialFixer] Success: Updated material on " + wallCount + " wall objects");
    }

    // ── Download, extract, import, build material ─────────────────────────────

    static Material DownloadAndBuild()
    {
        string zipPath   = absTexDir + "Plaster001_1K-PNG.zip";
        string extractDir = absTexDir + "Plaster001_Extract/";

        // Download ZIP
        EditorUtility.DisplayProgressBar("Fix Wall Material", "Downloading Plaster001 texture pack...", 0.10f);
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        using (var client = new WebClient())
            client.DownloadFile(ZIP_URL, zipPath);

        // Extract
        EditorUtility.DisplayProgressBar("Fix Wall Material", "Extracting ZIP...", 0.30f);
        Directory.CreateDirectory(extractDir);
        using (var fs  = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
        {
            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                string dest = extractDir + entry.Name;
                using (var src = entry.Open())
                using (var dst = new FileStream(dest, FileMode.Create, FileAccess.Write))
                    src.CopyTo(dst);
            }
        }

        // Import textures
        EditorUtility.DisplayProgressBar("Fix Wall Material", "Importing albedo...", 0.48f);
        Texture2D colorTex = ImportTex(
            FindFile(extractDir, "*_Color.png"),
            TextureImporterType.Default, readable: false);

        EditorUtility.DisplayProgressBar("Fix Wall Material", "Importing normal map...", 0.60f);
        Texture2D normalTex = ImportTex(
            FindFile(extractDir, "*_NormalGL.png"),
            TextureImporterType.NormalMap, readable: false);

        EditorUtility.DisplayProgressBar("Fix Wall Material", "Importing roughness...", 0.70f);
        Texture2D roughTex = ImportTex(
            FindFile(extractDir, "*_Roughness.png"),
            TextureImporterType.Default, readable: true);   // readable needed for pixel processing

        // Build
        EditorUtility.DisplayProgressBar("Fix Wall Material", "Building PBR material...", 0.80f);
        return BuildMaterial(colorTex, normalTex, roughTex);
    }

    // ── PBR material assembly ──────────────────────────────────────────────────

    static Material BuildMaterial(Texture2D colorTex, Texture2D normalTex, Texture2D roughTex)
    {
        // Prefer Standard for full PBR support; fall back to pipeline default
        Shader shader = Shader.Find("Standard") ?? GetDefaultShader();
        var mat = new Material(shader);

        // Base properties
        mat.SetFloat("_Metallic",    0f);
        mat.SetFloat("_Glossiness",  0.25f);   // used if no smoothness map

        // Albedo (colour + tiling)
        if (colorTex != null)
        {
            mat.mainTexture      = colorTex;
            mat.mainTextureScale = new Vector2(3f, 3f);
        }
        else
        {
            mat.color = new Color(0.95f, 0.93f, 0.90f);
        }

        // Normal map
        if (normalTex != null)
        {
            mat.SetTexture("_BumpMap", normalTex);
            mat.EnableKeyword("_NORMALMAP");
        }

        // Inverted roughness → smoothness packed into metallic-gloss map alpha
        if (roughTex != null)
        {
            try
            {
                Color[] rough = roughTex.GetPixels();
                var msMap = new Texture2D(roughTex.width, roughTex.height, TextureFormat.RGBA32, true);
                Color[] ms = new Color[rough.Length];
                for (int i = 0; i < rough.Length; i++)
                    ms[i] = new Color(0f, 0f, 0f, 1f - rough[i].r);  // A = 1 - roughness

                msMap.SetPixels(ms);
                msMap.Apply();

                string msAbs = absTexDir + "Plaster001_SmoothnessMap.png";
                string msRel = TEX_REL   + "Plaster001_SmoothnessMap.png";
                File.WriteAllBytes(msAbs, msMap.EncodeToPNG());
                AssetDatabase.ImportAsset(msRel, ImportAssetOptions.ForceUpdate);

                var msTex = AssetDatabase.LoadAssetAtPath<Texture2D>(msRel);
                if (msTex != null)
                {
                    mat.SetTexture("_MetallicGlossMap", msTex);
                    mat.EnableKeyword("_METALLICGLOSSMAP");
                    mat.SetFloat("_GlossMapScale", 0.25f);
                }
            }
            catch (Exception e)
            {
                // Non-fatal: _Glossiness = 0.25 already set above
                Debug.LogWarning("[WallMaterialFixer] Roughness inversion skipped: " + e.Message);
            }
        }

        return mat;
    }

    // ── Fallback flat-colour material ──────────────────────────────────────────

    static Material FallbackMat()
    {
        var m = new Material(GetDefaultShader());
        m.color = new Color(0.95f, 0.93f, 0.90f);
        return m;
    }

    // ── Save material as a project asset (overwrite if it already exists) ──────

    static Material PersistMaterial(Material mat, string assetPath, Vector2 tiling)
    {
        mat.mainTextureScale = tiling;

        var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (existing != null)
        {
            existing.CopyPropertiesFromMaterial(mat);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        // Ensure folder exists
        string folder = System.IO.Path.GetDirectoryName(assetPath);
        if (!AssetDatabase.IsValidFolder(folder))
            Directory.CreateDirectory(Application.dataPath.Replace("Assets", "") + folder);

        AssetDatabase.CreateAsset(mat, assetPath);
        return mat;
    }

    // ── Apply to scene wall & ceiling objects ──────────────────────────────────

    static int ApplyMaterials(Material wallMat, Material ceilMat)
    {
        int count = 0;
        var wallsGroup = GameObject.Find("Level04_Map/Walls");

        // ── Exclusion keywords (never paint these) ────────────────────────────
        string[] exclude = {
            "floor", "pillar", "bench", "desk", "door",
            "locker", "display", "pedestal", "painting",
            "frame", "light", "camera"
        };

        foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) continue;

            string n = go.name.ToLowerInvariant();

            // Skip excluded objects
            bool skip = false;
            foreach (var kw in exclude)
                if (n.Contains(kw)) { skip = true; break; }
            if (skip) continue;

            // ── Ceiling check ─────────────────────────────────────────────────
            bool isCeiling = false;
            try { isCeiling = go.tag == "Ceiling"; } catch {}
            if (!isCeiling) isCeiling = n.Contains("ceiling");

            if (isCeiling)
            {
                r.sharedMaterial = ceilMat;
                count++;
                continue;
            }

            // ── Wall check ────────────────────────────────────────────────────
            bool isWall = false;
            try { isWall = go.tag == "Wall"; } catch {}

            if (!isWall)
                isWall = n.Contains("wall")  || n.Contains("north") ||
                         n.Contains("south") || n.Contains("east")  ||
                         n.Contains("west");

            // Children of the Level04_Map/Walls group also count
            if (!isWall && wallsGroup != null)
            {
                for (var t = go.transform.parent; t != null; t = t.parent)
                {
                    if (t.gameObject == wallsGroup) { isWall = true; break; }
                }
            }

            if (!isWall) continue;

            r.sharedMaterial = wallMat;
            count++;
        }

        return count;
    }

    // ── Texture import helper ──────────────────────────────────────────────────

    static Texture2D ImportTex(string srcPath, TextureImporterType type, bool readable)
    {
        if (srcPath == null) return null;

        string filename = Path.GetFileName(srcPath);
        string absDst   = absTexDir + filename;
        string relDst   = TEX_REL   + filename;

        File.Copy(srcPath, absDst, overwrite: true);
        AssetDatabase.ImportAsset(relDst, ImportAssetOptions.ForceUpdate);

        var imp = AssetImporter.GetAtPath(relDst) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = type;
            imp.isReadable  = readable;
            AssetDatabase.ImportAsset(relDst, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(relDst);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    static string FindFile(string dir, string pattern)
    {
        var hits = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
        return hits.Length > 0 ? hits[0] : null;
    }

    static Shader GetDefaultShader()
    {
        var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var sh = probe.GetComponent<Renderer>().sharedMaterial.shader;
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
