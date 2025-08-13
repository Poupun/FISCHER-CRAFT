#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PlantAssetCreator
{
    [MenuItem("FISCHER-CRAFT/Create Plant Assets From Textures")] 
    public static void MenuCreatePlantAssets()
    {
        CreatePlantAssets("plant", 1f);
    }

    public static bool CreatePlantAssets(string searchName = "plant", float defaultWeight = 1f)
    {
        // Ensure output folder exists
        const string folder = "Assets/Plants";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Plants");
        }

        // Find textures matching name
        var guids = AssetDatabase.FindAssets($"{searchName} t:Texture2D");
        if (guids == null || guids.Length == 0)
        {
            Debug.LogError($"PlantAssetCreator: No textures found matching '{searchName}' (t:Texture2D). Place 'plant.png' under Assets and retry.");
            return false;
        }

        var created = new List<PlantDefinition>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;

            // Create PlantDefinition
            var def = ScriptableObject.CreateInstance<PlantDefinition>();
            def.name = $"Plant_{tex.name}";
            def.texture = tex;
            def.weight = Mathf.Max(0.01f, defaultWeight);
            def.width = 0.9f;
            def.heightRange = new Vector2(0.6f, 1.2f);
            def.yOffset = 0.02f;
            def.quadsPerInstance = 2;

            var defPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, $"{def.name}.asset"));
            AssetDatabase.CreateAsset(def, defPath);
            created.Add(def);
            Debug.Log($"PlantAssetCreator: Created {defPath}");
        }

        // Create or update PlantDatabase
        var dbPath = Path.Combine(folder, "PlantDatabase.asset");
        var db = AssetDatabase.LoadAssetAtPath<PlantDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<PlantDatabase>();
            db.plants = created.ToArray();
            AssetDatabase.CreateAsset(db, dbPath);
            Debug.Log($"PlantAssetCreator: Created database at {dbPath} with {created.Count} entries.");
        }
        else
        {
            var list = new List<PlantDefinition>();
            if (db.plants != null) list.AddRange(db.plants);
            foreach (var c in created)
            {
                if (c != null && !list.Contains(c)) list.Add(c);
            }
            db.plants = list.ToArray();
            EditorUtility.SetDirty(db);
            Debug.Log($"PlantAssetCreator: Updated database at {dbPath}; total {list.Count} entries.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return true;
    }
}
#endif
