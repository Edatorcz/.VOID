using UnityEngine;

/// <summary>
/// Komponenta, která reprezentuje jeden affix na kartě – obsahuje sprite a typ affixu.
/// Po přidání na kartu sama zobrazí ikonu v rohu a může obsahovat logiku affixu.
/// </summary>
public class AffixData : MonoBehaviour
{
    public Sprite icon; // Ikona affixu
    public string affixType; // Typ/ID affixu (pro logiku, např. "Medic")
    private GameObject iconObj;

    void Start()
    {
        ShowIcon();
    }

    public void ShowIcon()
    {
        if (icon == null)
        {
            Debug.LogWarning($"[AffixData] Ikona není nastavena pro affix '{affixType}' na {gameObject.name}");
            return;
        }
        if (iconObj != null) Destroy(iconObj);

        // Najdi Card komponentu a případný anchor
        Card card = GetComponent<Card>() ?? GetComponentInParent<Card>();
        Transform anchor = (card != null && card.affixAnchor != null) ? card.affixAnchor : this.transform;

        bool isUI = GetComponentInParent<Canvas>() != null;

        iconObj = new GameObject("AffixIcon");
        iconObj.transform.SetParent(anchor, false);
        iconObj.transform.localScale = new Vector3(0.01f, 0.005f, 0.01f);
        iconObj.transform.localRotation = Quaternion.Euler(0, 90, 90);

        if (isUI)
        {
            // UI varianta (Image)
            var img = iconObj.AddComponent<UnityEngine.UI.Image>();
            img.sprite = icon;
            img.raycastTarget = false;
            var rect = iconObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(32, 32);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            Debug.Log($"[AffixData] UI ikona '{affixType}' přidána na {gameObject.name} (anchor: {anchor.name})");
        }
        if (isUI)
        {
            // UI varianta (Image)
            var img = iconObj.AddComponent<UnityEngine.UI.Image>();
            img.sprite = icon;
            img.raycastTarget = false;
            var rect = iconObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(32, 32);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            Debug.Log($"[AffixData] UI ikona '{affixType}' přidána na {gameObject.name} (anchor: {anchor.name})");
        }
        else
        {
            // 2D/3D varianta (SpriteRenderer)
            var sr = iconObj.AddComponent<SpriteRenderer>();
            sr.sprite = icon;
            sr.sortingOrder = 10;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.006f, 0.003f);
            iconObj.transform.localPosition = Vector3.zero;
            iconObj.transform.localScale = Vector3.one;
            iconObj.transform.localRotation = Quaternion.Euler(0, 90, 90);
            Debug.Log($"[AffixData] Sprite ikona '{affixType}' přidána na {gameObject.name} (anchor: {anchor.name}), size: {sr.size}");
        }
    }
}
