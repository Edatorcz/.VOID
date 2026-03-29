using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Jednoduchý UI panel pro výběr schopnosti karty.
/// Vytvoří se dynamicky, pokud není v hierarchii.
/// </summary>

public class AbilityUIPanel : MonoBehaviour
{
    private static AbilityUIPanel activePanel = null;
    public AffixIconManager iconManager; // Nastav v inspektoru nebo dynamicky
    private Card targetCard;

    /// <summary>
    /// Otevře affix panel pouze pokud žádný není otevřený.
    /// </summary>
    public static AbilityUIPanel Open(Card card, AffixIconManager iconManager, GameObject panelPrefab = null)
    {
        if (activePanel != null)
        {
            Debug.Log("[AbilityUIPanel] Nelze otevřít další affix okno, už je jedno otevřené.");
            return null;
        }
        GameObject panelObj;
        if (panelPrefab != null)
        {
            panelObj = GameObject.Instantiate(panelPrefab);
        }
        else
        {
            panelObj = new GameObject("AbilityUIPanel");
            panelObj.AddComponent<CanvasRenderer>();
        }
        var panel = panelObj.AddComponent<AbilityUIPanel>();
        panel.iconManager = iconManager;
        panel.Init(card);
        return panel;
    }

    // Otevři panel pro konkrétní kartu (používej pouze přes Open)
    public void Init(Card card)
    {
        activePanel = this;
        targetCard = card;
        // Debug: Zkontroluj přiřazení iconManageru
        if (iconManager == null)
        {
            Debug.LogWarning("[AbilityUIPanel][DEBUG] iconManager není přiřazen! Ikony affixů nebudou fungovat.");
        }
        else
        {
            Debug.Log($"[AbilityUIPanel][DEBUG] iconManager přiřazen: {iconManager.name}");
        }
        // Pokud karta už má affix, panel se neotevře
        if (targetCard != null && targetCard.affixes != null && targetCard.affixes.Count > 0)
        {
            Debug.Log("[AbilityUIPanel] Tato karta už má affix, další nelze přidat.");
            Destroy(gameObject);
            activePanel = null;
            return;
        }
    }
    private GameObject panel;
    private Text text;

    void Update()
    {
        // Zavřít menu kolečkem myši
        if (Input.GetMouseButtonDown(2))
        {
            ClosePanel();
        }

        // Zavřít menu kliknutím mimo panel
        if (Input.GetMouseButtonDown(0)) // levé tlačítko
        {
            // Zjisti pozici myši v canvasu
            Vector2 mousePos = Input.mousePosition;
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos))
            {
                ClosePanel();
            }
        }
    }

    void Awake()
    {
        // Zabrání duplicitnímu otevření panelu i při ručním vytvoření GameObjectu
        if (activePanel != null && activePanel != this)
        {
            Debug.Log("[AbilityUIPanel] Panel už existuje, tento bude zničen.");
            Destroy(gameObject);
            return;
        }
        activePanel = this;
        CreatePanel();
    }

    private void CreatePanel()
    {
        // Zajisti EventSystem ve scéně (pro funkční UI)
        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        // Canvas
        GameObject canvasObj = GameObject.Find("AbilityUICanvas");
        Canvas canvas;
        if (canvasObj == null)
        {
            canvasObj = new GameObject("AbilityUICanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
        }

        // Panel
        panel = new GameObject("AbilityPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 200);
        panelRect.anchoredPosition = Vector2.zero;

        if (panel == null)
        {
            Debug.LogError("[AbilityUIPanel] Panel nebyl vytvořen!");
            return;
        }

        // Text (nahoře)
        GameObject textObj = new GameObject("AbilityText");
        textObj.transform.SetParent(panel.transform, false);
        text = textObj.AddComponent<Text>();
        text.text = "Vyber schopnost (zatím placeholder)";
        text.alignment = TextAnchor.UpperCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.color = Color.white;
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(380, 60);
        textRect.anchoredPosition = new Vector2(0, 60);

        // Získej všechny dostupné affixy (typy)
        var affixTypes = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(CardAffix)) && !t.IsAbstract)
            .ToList();

        // Vyber náhodné 3 různé affixy
        System.Random rng = new System.Random();
        var randomAffixes = affixTypes.OrderBy(x => rng.Next()).Take(3).ToList();

        // Rozložení: 3 vedle sebe
        float xStart = -120f;
        float y = 10f;
        float xStep = 130f;
        for (int i = 0; i < randomAffixes.Count; i++)
        {
            var affixType = randomAffixes[i];
            // Získej název a popis přes reflexi (stejně jako dřív, fallback na typ)
            string label = null;
            string desc = null;
            var nameProp = affixType.GetProperty("AffixName", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            var descProp = affixType.GetProperty("Description", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            if (nameProp != null && descProp != null)
            {
                try {
                    var temp = targetCard.gameObject.AddComponent(affixType) as CardAffix;
                    label = temp.AffixName;
                    desc = temp.Description;
                    DestroyImmediate(temp);
                } catch { label = affixType.Name; desc = ""; }
            }
            if (string.IsNullOrEmpty(label)) label = affixType.Name;
            if (string.IsNullOrEmpty(desc)) desc = "";
            // Odstraň prefix "affix" z labelu (case-insensitive)
            if (label.ToLower().StartsWith("affix"))
            {
                label = label.Substring(5).TrimStart('_', ' ');
            }
            float x = xStart + i * xStep;
            CreateAffixButton(label, affixType, x, y, desc, true);
        }
    }

    // (Odstraněno: duplicitní a neplatná deklarace CreateAffixButton)
    // odstraněno: neplatná složená závorka
    // Pokud allowOnlyOne=true, po kliknutí na tlačítko se panel zavře a žádný další affix už nejde přidat v tomto kole
    private void CreateAffixButton(string label, System.Type affixType, float x, float y, string desc, bool allowOnlyOne = false)
    {
        GameObject btnObj = new GameObject(label + "Button");
        btnObj.transform.SetParent(panel.transform, false);
        Button btn = btnObj.AddComponent<Button>();
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.7f);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 60);
        rect.anchoredPosition = new Vector2(x, y);

        // Ikona affixu (pokud existuje)
        if (iconManager != null)
        {
            Sprite icon = iconManager.GetIcon(label);
            if (icon != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(btnObj.transform, false);
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.rectTransform.sizeDelta = new Vector2(40, 40);
                iconImg.rectTransform.anchoredPosition = new Vector2(-130, 0);
            }
        }

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text btnText = txtObj.AddComponent<Text>();
        btnText.text = label;
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 28;
        btnText.color = Color.black;
        btnText.alignment = TextAnchor.MiddleLeft;
        RectTransform txtRect = btnText.GetComponent<RectTransform>();
        txtRect.sizeDelta = new Vector2(200, 40);
        txtRect.anchoredPosition = new Vector2(-60, 0);

        // Popis affixu
        GameObject descObj = new GameObject("Desc");
        descObj.transform.SetParent(btnObj.transform, false);
        Text descText = descObj.AddComponent<Text>();
        descText.text = desc;
        descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 16;
        descText.color = new Color(0.2f,0.2f,0.2f,1f);
        descText.alignment = TextAnchor.MiddleLeft;
        RectTransform descRect = descText.GetComponent<RectTransform>();
        descRect.sizeDelta = new Vector2(200, 40);
        descRect.anchoredPosition = new Vector2(60, 0);

        btn.onClick.AddListener(() => {
            Debug.Log($"[AbilityUIPanel] Kliknuto na '{label}'");
            if (targetCard != null && affixType != null)
            {
                // Zabraň přidání dalšího affixu, pokud už karta nějaký má
                if (targetCard.affixes != null && targetCard.affixes.Count > 0)
                {
                    Debug.LogWarning($"[AbilityUIPanel] Karta '{targetCard.name}' už má affix, další nelze přidat.");
                    return;
                }
                // Přidej logický affix (CardAffix) na kartu
                var affix = targetCard.gameObject.AddComponent(affixType) as CardAffix;
                if (affix != null)
                {
                    targetCard.affixes.Add(affix);
                    // Přidej i vizuální AffixData (pro ikonu)
                    var affixData = targetCard.gameObject.AddComponent<AffixData>();
                    affixData.affixType = label;
                    Sprite icon = null;
                    if (iconManager != null)
                    {
                        // Normalize funkce pro porovnání
                        string Normalize(string s) {
                            var n = s.Normalize(System.Text.NormalizationForm.FormD);
                            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                            foreach (var c in n)
                                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                                    sb.Append(char.ToLowerInvariant(c));
                            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
                        }
                        string labelNorm = Normalize(label);
                        string typeNorm = Normalize(affixType.Name);
                        // Najdi první shodu v iconManager.icons
                        foreach (var entry in iconManager.icons)
                        {
                            string entryNorm = Normalize(entry.affixType);
                            if (entryNorm == labelNorm || entryNorm == typeNorm)
                            {
                                icon = entry.icon;
                                break;
                            }
                        }
                        // Fallback: zkus najít podle názvu sprite
                        if (icon == null)
                        {
                            foreach (var entry in iconManager.icons)
                            {
                                if (entry.icon != null && Normalize(entry.icon.name) == labelNorm)
                                {
                                    icon = entry.icon;
                                    break;
                                }
                            }
                        }
                        if (icon == null)
                        {
                            Debug.LogError($"[AbilityUIPanel] CHYBÍ ikona pro affix '{label}' ani '{affixType.Name}'! Zkontroluj asset AffixIconManager: musí být položka s affixType='{label}' nebo '{affixType.Name}' a přiřazený sprite.");
                        }
                        else if (icon.texture == null)
                        {
                            Debug.LogError($"[AbilityUIPanel] Ikona pro affix '{label}' je prázdná (sprite nemá texturu)! Zkontroluj asset AffixIconManager.");
                        }
                    }
                    else
                    {
                        Debug.LogError("[AbilityUIPanel] iconManager není přiřazen při hledání ikony! Přetáhni asset AffixIconManager do pole iconManager v inspektoru.");
                    }
                    affixData.icon = icon;
                    affixData.ShowIcon();
                }
                // Po přidání affixu ihned přepočítej dynamické efekty
                if (GameManager.Instance != null && GameManager.Instance.playerDeck != null)
                    GameManager.Instance.playerDeck.UpdateDynamicAffixes();
            }
            if (allowOnlyOne)
            {
                // Zabrání dalšímu výběru affixu v tomto kole
                ClosePanel();
            }
        });
    }

    public void ClosePanel()
    {
        activePanel = null;
        Destroy(panel);
        Destroy(gameObject);
    }
}
