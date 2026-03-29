using UnityEngine;

public enum ItemType
{
    ElectricShock,      // 1) Vypne enemy rakety na 1 kolo
    Shield,             // 2) Karta pohltí veškerý dmg na 1 kolo
    Asteroid,           // 3) Random 0-3 dmg na všech 10 polích
    ScoutRocket,        // 4) 1HP/1DMG Scout do ruky
    MoveLeft,           // 5) Posune kartu vlevo o 1
    MoveRight,          // 6) Posune kartu vpravo o 1
    Magnet,             // 7) Odstraní všechny efekty karet
    GlobalEffect,       // 8) Dá vybraným kartám stejný random efekt
    Hook,               // 9) Přetáhne enemy kartu na hráčovu stranu
    Scissors,           // 10) Zničí jakoukoliv kartu bez ohledu na HP/efekty
    SpaceDebris         // 11) Vyplní volná hráčova pole bordelem (0 dmg, 2 hp)
}

/// <summary>
/// ScriptableObject pro item/schopnost.
/// Vytvořit: Assets > Create > CardGame > Item Data
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "CardGame/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName = "Unnamed Item";
    public ItemType type;

    [TextArea(2, 4)]
    public string description = "";

    public Sprite icon;
}
