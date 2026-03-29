using UnityEngine;

/// <summary>
/// Affix: Želva – Vždy pohltí 1 damage z útoku.
/// </summary>
public class AffixZelva : CardAffix
{
    public override string AffixName => "Želva";
    public override string Description => "Vždy pohltí 1 damage z útoku.";

    public override int OnTakeDamage(Card card, int amount)
    {
        return Mathf.Max(0, amount - 1);
    }
}
