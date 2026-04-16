using UnityEngine;

/// <summary>
/// Affix: Bomba – Po zničení udělí 5 DMG přilehlým kartám a protější kartě.
/// </summary>
public class AffixBomba : CardAffix
{
    public override string AffixName => "Bomba";
    public override string Description => "Po zničení udělí 5 DMG přilehlým a protější kartě.";
}
