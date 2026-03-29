using UnityEngine;

/// <summary>
/// Affix: Vidlička – při útoku zasáhne pouze karty vlevo a vpravo od protější karty (ne hlavní cíl).
/// </summary>
public class AffixVidlicka : CardAffix
{
    public override string AffixName => "Vidlička";
    public override string Description => "Při útoku zasáhne pouze karty vlevo a vpravo od protější karty.";
}

/// <summary>
/// Affix: Rychlík – po 2 kolech se karta sama odstraní z pole s animací.
/// </summary>
public class AffixRychlik : CardAffix
{
    public override string AffixName => "Rychlík";
    public override string Description => "Po 2 kolech se karta sama odstraní z pole.";
    private int turnsLeft = 2;
    private Card hostCard;

    public override void OnApply(Card card)
    {
        hostCard = card;
        turnsLeft = 2;
    }

    public override void OnTurnStart(Card card)
    {
        turnsLeft--;
        if (turnsLeft <= 0 && hostCard != null)
        {
            hostCard.StartCoroutine(RemoveWithAnimation());
        }
    }

    private System.Collections.IEnumerator RemoveWithAnimation()
    {
        // Animace: zmenšení, fade out, rotace
        float duration = 0.7f;
        Vector3 startScale = hostCard.transform.localScale;
        Quaternion startRot = hostCard.transform.rotation;
        float elapsed = 0f;
        var rend = hostCard.GetComponent<Renderer>();
        Color startColor = rend != null && rend.material != null ? rend.material.color : Color.white;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            hostCard.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            hostCard.transform.rotation = startRot * Quaternion.Euler(0f, 0f, t * 360f);
            if (rend != null && rend.material != null)
            {
                Color c = startColor;
                c.a = 1f - t;
                rend.material.color = c;
            }
            yield return null;
        }
        // Odstranění karty ze slotu
        if (GameManager.Instance != null && GameManager.Instance.playerDeck != null)
        {
            var deck = GameManager.Instance.playerDeck;
            for (int i = 0; i < deck.SlotCount; i++)
            {
                if (deck.GetCardAtSlot(i) == hostCard)
                {
                    deck.RemoveCardFromSlot(i);
                    break;
                }
            }
        }
        Destroy(hostCard.gameObject);
    }
}

/// <summary>
/// Affix: Zpátečka – útočí na pravou hráčovu kartu místo naproti.
/// </summary>
public class AffixZpatecka : CardAffix
{
    public override string AffixName => "Zpátečka";
    public override string Description => "Útočí na pravou hráčovu kartu místo naproti.";
}
