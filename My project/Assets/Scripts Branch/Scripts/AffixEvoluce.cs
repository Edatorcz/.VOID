using UnityEngine;

/// <summary>
/// Affix: Evoluce – Po 2 kolech se kartě přidá +2 DMG a +1 HP (jednorázově).
/// </summary>
public class AffixEvoluce : CardAffix
{
    public override string AffixName => "Evoluce";
    public override string Description => "Po 2 kolech +2 DMG a +1 HP (jednorázově).";

    private int turnsLeft = 2;
    private bool evolved = false;

    public override void OnApply(Card card)
    {
        turnsLeft = 2;
        evolved = false;
    }

    public override void OnTurnStart(Card card)
    {
        if (evolved) return;
        turnsLeft--;
        if (turnsLeft <= 0)
        {
            evolved = true;
            card.data.damage += 2;
            card.data.health += 1;
            card.currentDamage += 2;
            card.currentHealth += 1;
            card.UpdateStatTexts();
            Debug.Log($"[AffixEvoluce] '{card.data.cardName}' evolvovala! +2 DMG, +1 HP");
            card.StartCoroutine(EvolveAnimation(card));
        }
    }

    /// <summary>Animace evoluce – karta se rozzáří a zvětší, pak se vrátí.</summary>
    private System.Collections.IEnumerator EvolveAnimation(Card card)
    {
        Transform t = card.transform;
        Vector3 origScale = t.localScale;
        Vector3 bigScale = origScale * 1.3f;
        Renderer rend = card.GetComponent<Renderer>();
        if (rend == null) rend = card.GetComponentInChildren<Renderer>();
        Color origColor = rend != null && rend.material != null ? rend.material.color : Color.white;
        Color glowColor = origColor + new Color(0.4f, 0.3f, 0f, 0f); // zlatý nádech

        float duration = 0.5f;
        float elapsed = 0f;

        // Zvětšení + glow
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            float wave = Mathf.Sin(p * Mathf.PI); // 0→1→0
            t.localScale = Vector3.Lerp(origScale, bigScale, wave);
            if (rend != null && rend.material != null)
                rend.material.color = Color.Lerp(origColor, glowColor, wave);
            yield return null;
        }

        t.localScale = origScale;
        if (rend != null && rend.material != null)
            rend.material.color = origColor;
    }
}
