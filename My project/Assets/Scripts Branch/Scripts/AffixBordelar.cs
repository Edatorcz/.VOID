using UnityEngine;

/// <summary>
/// Affix: Bordelář – Při položení vytvoří na přilehlých stranách "Bordel" karty (0 DMG, 2 HP).
/// </summary>
public class AffixBordelar : CardAffix
{
    public override string AffixName => "Bordelář";
    public override string Description => "Na přilehlých stranách se vytvoří Bordel (0 DMG, 2 HP).";
}
