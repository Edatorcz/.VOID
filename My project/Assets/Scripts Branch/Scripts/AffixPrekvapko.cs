using UnityEngine;

/// <summary>
/// Affix: Překvápko – Po zničení karty s tímto sigilem se na jeho místě objeví "Raketka" (1 DMG, 1 HP).
/// Zpracování probíhá v GameManageru po smrti karty.
/// </summary>
public class AffixPrekvapko : CardAffix
{
    public override string AffixName => "Překvápko";
    public override string Description => "Po zničení se na místě objeví Raketka (1 DMG, 1 HP).";
}
