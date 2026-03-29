using UnityEngine;

/// <summary>
/// Affix: Medic – Přilehlým kartám dává +1 HP (dynamicky).
/// </summary>
public class AffixMedic : CardAffix
{
    public override string AffixName => "Medic";
    public override string Description => "Přilehlým kartám dává +1 HP (dynamicky)";

    // Při zničení affixu (např. karta umře) přepočítej dynamické buffy sousedů
    void OnDestroy()
    {
        // Najdi DeckManager přes GameManager.Instance.playerDeck
        if (GameManager.Instance != null && GameManager.Instance.playerDeck != null)
        {
            GameManager.Instance.playerDeck.UpdateDynamicAffixes();
        }
    }
}
