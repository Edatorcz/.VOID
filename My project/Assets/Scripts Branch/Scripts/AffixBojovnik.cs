using UnityEngine;

/// <summary>
/// Affix: Bojovník – Přilehlé karty útočí 2x (dynamicky).
/// </summary>
public class AffixBojovnik : CardAffix
{
    public override string AffixName => "Bojovník";
    public override string Description => "Přilehlé karty útočí 2x (dynamicky)";

    // Tento affix sám neaplikuje bonus, ale DeckManager nebo Card by měl při změně pole volat metodu pro aktualizaci bonusů.
}
