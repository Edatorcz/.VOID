using UnityEngine;

/// <summary>
/// Affix: Silák – Za každé dva sigily (affixy) na hráčově/vlastní straně pole +1 DMG.
/// Logika se počítá v DeckManager.UpdateDynamicAffixes().
/// </summary>
public class AffixSilak : CardAffix
{
    public override string AffixName => "Silák";
    public override string Description => "Za každé 2 sigily na vlastní straně pole +1 DMG.";
}
