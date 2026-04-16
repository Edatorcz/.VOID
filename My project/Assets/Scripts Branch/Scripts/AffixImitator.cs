using UnityEngine;

/// <summary>
/// Affix: Imitátor – Po položení karty na pole se vytvoří kopie dané karty a přidá se hráči do ruky (1x, nepřidá se do decku).
/// Logika se řeší v DeckManager.PlaceCardCoroutine().
/// </summary>
public class AffixImitator : CardAffix
{
    public override string AffixName => "Imitátor";
    public override string Description => "Po položení vytvoří kopii karty do ruky (1×).";

    [HideInInspector]
    public bool used = false;
}
