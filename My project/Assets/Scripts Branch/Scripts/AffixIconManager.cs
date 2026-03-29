using UnityEngine;

/// <summary>
/// Spravuje a poskytuje sprity pro affixy podle jejich jména/typu.
/// Nastav sprity v inspektoru v editoru Unity.
/// </summary>
[CreateAssetMenu(fileName = "AffixIconManager", menuName = "CardGame/AffixIconManager")]
public class AffixIconManager : ScriptableObject
{
    [System.Serializable]
    public struct AffixIconEntry
    {
        public string affixType;
        public Sprite icon;
    }

    public AffixIconEntry[] icons;

    public Sprite GetIcon(string affixType)
    {
        foreach (var entry in icons)
        {
            if (entry.affixType == affixType)
                return entry.icon;
        }
        Debug.LogWarning($"[AffixIconManager] Ikona pro affix '{affixType}' nebyla nalezena!");
        return null;
    }
}
