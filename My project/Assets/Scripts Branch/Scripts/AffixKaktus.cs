using UnityEngine;

/// <summary>
/// Affix: Kaktus – Po zasažení je útočníkovi uděleno 1 poškození.
/// </summary>
public class AffixKaktus : CardAffix
{
    public override string AffixName => "Kaktus";
    public override string Description => "Po zasažení útočník obdrží 1 poškození.";

    public override int OnTakeDamage(Card card, int amount)
    {
        // Damage reflection se řeší v GameManager po útoku
        return amount;
    }
}
