using UnityEngine;

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

    private void OnEnable()
    {
        Debug.Log($"[AffixIconManager] Asset '{name}' byl načten a je aktivní.");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void LogAllAffixIconsOnStart()
    {
        var managers = Resources.FindObjectsOfTypeAll<AffixIconManager>();
        foreach (var manager in managers)
        {
            Debug.Log($"[AffixIconManager] --- Ikony dostupné v assetu '{manager.name}' ---");
            if (manager.icons != null)
            {
                foreach (var entry in manager.icons)
                {
                    Debug.Log($"[AffixIconManager] AffixType: '{entry.affixType}', Sprite: '{(entry.icon != null ? entry.icon.name : "NULL")}'");
                }
            }
        }
    }

    public Sprite GetIcon(string affixType)
    {
        // Pomocná funkce pro odstranění diakritiky a převedení na lowercase
        string NormalizeKey(string text)
        {
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        string affixTypeNorm = NormalizeKey(affixType);

        // 1. Přesná shoda (case-insensitive, bez diakritiky)
        foreach (var entry in icons)
        {
            if (NormalizeKey(entry.affixType) == affixTypeNorm)
                return entry.icon;
        }
        // 2. Pokud affixType začíná na 'affix', zkus bez prefixu a s _icon (vše normalized)
        if (affixTypeNorm.StartsWith("affix"))
        {
            string shortName = affixTypeNorm.Substring(5); // např. affixrychlik -> rychlik
            string iconName = shortName + "_icon";
            foreach (var entry in icons)
            {
                if (NormalizeKey(entry.affixType) == iconName)
                    return entry.icon;
            }
        }
        Debug.LogWarning($"[AffixIconManager] Ikona pro affix '{affixType}' nebyla nalezena! Zkus přidat položku s affixType='{affixType}' nebo bez diakritiky/malými písmeny.");
        return null;
    }
}
