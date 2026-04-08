using UnityEngine;

/// <summary>
/// Affix: Boxer – Při útoku odstrčí pravou přilehlou kartu nepřítele na poslední volnou pozici vpravo.
/// </summary>
public class AffixBoxer : CardAffix
{
    public override string AffixName => "Boxer";
    public override string Description => "Odstrčí pravou přilehlou enemy kartu na poslední volnou pozici vpravo.";
}
