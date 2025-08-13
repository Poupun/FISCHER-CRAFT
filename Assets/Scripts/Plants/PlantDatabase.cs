using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "FISCHER-CRAFT/Plants/Plant Database", fileName = "PlantDatabase")]
public class PlantDatabase : ScriptableObject
{
    public PlantDefinition[] plants;

    public bool HasAny => plants != null && plants.Any(p => p != null && p.texture != null && p.weight > 0f);

    public PlantDefinition PickByWeight(System.Random rng)
    {
        if (!HasAny) return null;
        float total = 0f;
        foreach (var p in plants) if (p != null) total += Mathf.Max(0f, p.weight);
        if (total <= 0f) return null;
        float r = (float)rng.NextDouble() * total;
        foreach (var p in plants)
        {
            if (p == null) continue;
            float w = Mathf.Max(0f, p.weight);
            if (r < w) return p;
            r -= w;
        }
        return plants.FirstOrDefault(p => p != null);
    }
}
