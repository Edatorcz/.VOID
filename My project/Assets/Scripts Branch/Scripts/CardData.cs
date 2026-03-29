using UnityEngine;

public enum CardGroup
{
    Support,
    Scout,
    Specialist,
    Striker,
    Sentinel
}

public static class CardGroupColors
{
    public static Color GetColor(CardGroup group)
    {
        switch (group)
        {
            case CardGroup.Support:    return new Color(0.3f, 0.8f, 0.3f);   // zelená
            case CardGroup.Scout:      return new Color(0.3f, 0.6f, 1.0f);   // modrá
            case CardGroup.Specialist: return new Color(0.9f, 0.6f, 0.1f);   // oranžová
            case CardGroup.Striker:    return new Color(0.9f, 0.2f, 0.2f);   // červená
            case CardGroup.Sentinel:   return new Color(0.6f, 0.4f, 0.8f);   // fialová
            default:                   return Color.white;
        }
    }
}

/// <summary>
/// ScriptableObject definující data jedné karty.
/// Vytvořit: Assets > klik pravý > Create > CardGame > Card Data
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "CardGame/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName = "Unnamed Card";

    public CardGroup group = CardGroup.Support;

    [TextArea(2, 4)]
    public string description = "";

    public int cost = 1;

    [Header("Stats")]
    public int health = 3;
    public int damage = 1;

    public Sprite artwork;

    /// <summary>Barva podle skupiny.</summary>
    public Color GroupColor => CardGroupColors.GetColor(group);
}
