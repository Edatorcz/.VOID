using UnityEngine;

/// <summary>
/// Affix: Dav – Za každou kartu s tímto sigilem na poli je zvýšen DMG +1 u všech karet s tímto sigilem.
/// Dynamické: přidání/odebrání karty s Davem přepočítá bonus všem.
/// </summary>
public class AffixDav : CardAffix
{
    public override string AffixName => "Dav";
    public override string Description => "+1 DMG za každou kartu s tímto sigilem na poli.";
}
