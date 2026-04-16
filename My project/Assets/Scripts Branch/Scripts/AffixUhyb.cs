using UnityEngine;

/// <summary>
/// Affix: Úhyb – Když je karta zasažena, posune se do volné pozice vpravo nebo vlevo.
/// Logika přesunu se řeší v GameManager (PlayerAttackEnemyCard / EnemyAttackPlayerCard).
/// </summary>
public class AffixUhyb : CardAffix
{
    public override string AffixName => "Úhyb";
    public override string Description => "Při zásahu se karta přesune na volnou pozici vedle.";
}
