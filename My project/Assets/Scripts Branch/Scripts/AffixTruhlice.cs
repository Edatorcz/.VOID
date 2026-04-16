using UnityEngine;

/// <summary>
/// Affix: Truhlice – Po zničení této karty dostane hráč 3 karty do ruky z kategorie Striker/Support (neukládá se do decku).
/// </summary>
public class AffixTruhlice : CardAffix
{
    public override string AffixName => "Truhlice";
    public override string Description => "Po zničení dostane hráč 3 karty (Striker/Support) do ruky.";
}
