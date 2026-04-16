using UnityEngine;

/// <summary>
/// Affix: Požár – Každé kolo karta získává +1 DMG a ztrácí 1 HP.
/// </summary>
public class AffixPozar : CardAffix
{
    public override string AffixName => "Požár";
    public override string Description => "Každé kolo +1 DMG, −1 HP.";

    public override void OnTurnStart(Card card)
    {
        card.data.damage += 1;
        card.currentDamage += 1;
        card.currentHealth -= 1;
        card.data.health -= 1;
        card.UpdateStatTexts();
        Debug.Log($"[AffixPozar] '{card.data.cardName}' hoří! +1 DMG, −1 HP (HP: {card.currentHealth})");
        card.StartCoroutine(BurnAnimation(card));

        if (card.currentHealth <= 0)
        {
            Debug.Log($"[AffixPozar] '{card.data.cardName}' shořela!");
            // Odstranění se provede přes GameManager/DeckManager
        }
    }

    /// <summary>Animace hoření – karta blikne oranžově/červeně.</summary>
    private System.Collections.IEnumerator BurnAnimation(Card card)
    {
        Renderer rend = card.GetComponent<Renderer>();
        if (rend == null) rend = card.GetComponentInChildren<Renderer>();
        if (rend == null || rend.material == null) yield break;

        Color origColor = rend.material.color;
        Color fireColor = new Color(1f, 0.3f, 0f, 1f); // oranžová

        float duration = 0.4f;
        int flashes = 3;
        float flashTime = duration / (flashes * 2f);

        for (int i = 0; i < flashes; i++)
        {
            rend.material.color = fireColor;
            float elapsed = 0f;
            while (elapsed < flashTime) { elapsed += Time.deltaTime; yield return null; }
            rend.material.color = origColor;
            elapsed = 0f;
            while (elapsed < flashTime) { elapsed += Time.deltaTime; yield return null; }
        }

        rend.material.color = origColor;
    }
}
