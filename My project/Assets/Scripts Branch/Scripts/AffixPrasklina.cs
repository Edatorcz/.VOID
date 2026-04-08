using UnityEngine;

/// <summary>
/// Affix: Prasklina – Útok prochází skrz kartu s tímto sigilem a karta nedostává damage.
/// Když karta s Prasklinou je napadena, damage projde skrz na hráče/nepřítele.
/// </summary>
public class AffixPrasklina : CardAffix
{
    public override string AffixName => "Prasklina";
    public override string Description => "Útok prochází skrz tuto kartu (nedostává damage).";
}
