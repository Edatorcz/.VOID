using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Jednoduchý UI panel pro výběr schopnosti karty.
/// Vytvoří se dynamicky, pokud není v hierarchii.
/// </summary>

[System.Serializable]
public class AffixOverrideEntry
{
    public string key;
    public string name;
    public string desc;
}

[System.Serializable]
public class AffixOverrideData
{
    public AffixOverrideEntry[] overrides;
}

public class AbilityUIPanel : MonoBehaviour
{
    private static AbilityUIPanel activePanel = null;
    public AffixIconManager iconManager; // Nastav v inspektoru nebo dynamicky
    private Card targetCard;
    private static System.Collections.Generic.Dictionary<string, AffixOverrideEntry> overrideMap;

    private static void LoadOverrides()
    {
        if (overrideMap != null) return;
        overrideMap = new System.Collections.Generic.Dictionary<string, AffixOverrideEntry>();
        var asset = Resources.Load<TextAsset>("AffixDescriptions");
        if (asset == null) { Debug.LogWarning("[AbilityUIPanel] AffixDescriptions.json not found in Resources."); return; }
        var data = JsonUtility.FromJson<AffixOverrideData>(asset.text);
        if (data?.overrides == null) return;
        foreach (var entry in data.overrides)
        {
            if (!string.IsNullOrEmpty(entry.key))
                overrideMap[entry.key] = entry;
        }
    }

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
        CreatePanel();
    }
    private GameObject panel;
    private Text text;
    private GameObject tooltipObj;
    private Text tooltipTitle;
    private Text tooltipDesc;
    private bool tooltipVisible;
    private CanvasGroup panelCanvasGroup;
    private System.Collections.Generic.List<GameObject> buttonObjects = new System.Collections.Generic.List<GameObject>();

    void Update()
    {
        // Zavřít menu kolečkem myši
        if (Input.GetMouseButtonDown(2))
        {
            ClosePanel();
        }

        // Zavřít menu kliknutím mimo panel
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, mousePos))
            {
                ClosePanel();
            }
        }

        // Tooltip sleduje myš
        if (tooltipVisible && tooltipObj != null)
        {
            Vector2 pos = Input.mousePosition;
            // Offset doprava a nahoru od kurzoru
            pos += new Vector2(16f, 16f);
            // Zajisti, že tooltip nepřeteče přes pravý/horní okraj
            RectTransform ttRect = tooltipObj.GetComponent<RectTransform>();
            if (pos.x + ttRect.sizeDelta.x > Screen.width)
                pos.x = Input.mousePosition.x - ttRect.sizeDelta.x - 8f;
            if (pos.y + ttRect.sizeDelta.y > Screen.height)
                pos.y = Input.mousePosition.y - ttRect.sizeDelta.y - 8f;
            tooltipObj.transform.position = pos;
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

        // Panel – vesmírné pozadí
        panel = new GameObject("AbilityPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.02f, 0.08f, 0.97f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(480, 170);
        panelRect.anchoredPosition = Vector2.zero;

        // Vnější obrys – cyan glow
        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.0f, 0.85f, 0.95f, 0.7f);
        panelOutline.effectDistance = new Vector2(2, -2);
        // Druhý obrys pro silnější glow
        Outline panelOutline2 = panel.AddComponent<Outline>();
        panelOutline2.effectColor = new Color(0.0f, 0.6f, 1f, 0.35f);
        panelOutline2.effectDistance = new Vector2(4, -4);

        // CanvasGroup pro fade-in animaci
        panelCanvasGroup = panel.AddComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0f;

        if (panel == null)
        {
            Debug.LogError("[AbilityUIPanel] Panel nebyl vytvořen!");
            return;
        }

        // Dekorativní čára nahoře (tenká cyan linka)
        GameObject topLine = new GameObject("TopLine");
        topLine.transform.SetParent(panel.transform, false);
        Image lineImg = topLine.AddComponent<Image>();
        lineImg.color = new Color(0.0f, 0.85f, 0.95f, 0.6f);
        RectTransform lineRect = topLine.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.05f, 1f);
        lineRect.anchorMax = new Vector2(0.95f, 1f);
        lineRect.offsetMin = new Vector2(0, -2);
        lineRect.offsetMax = new Vector2(0, 0);

        // Text (nahoře) – vesmírný styl
        GameObject textObj = new GameObject("AbilityText");
        textObj.transform.SetParent(panel.transform, false);
        text = textObj.AddComponent<Text>();
        text.text = "✦  Vyber sigil  ✦";
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.color = new Color(0.0f, 0.9f, 1f, 1f);
        // Glow efekt na text
        Shadow textGlow = textObj.AddComponent<Shadow>();
        textGlow.effectColor = new Color(0.0f, 0.7f, 1f, 0.5f);
        textGlow.effectDistance = new Vector2(1, -1);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(440, 40);
        textRect.anchoredPosition = new Vector2(0, 55);

        // Tooltip (Minecraft styl) – skrytý na začátku
        CreateTooltip(canvasObj);

        // Získej všechny dostupné affixy (typy)
        var affixTypes = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(CardAffix)) && !t.IsAbstract)
            .ToList();

        // Vyber náhodné 3 různé affixy
        System.Random rng = new System.Random();
        var randomAffixes = affixTypes.OrderBy(x => rng.Next()).Take(3).ToList();

        // Rozložení: 3 vedle sebe
        float xStart = -145f;
        float y = -15f;
        float xStep = 145f;
        for (int i = 0; i < randomAffixes.Count; i++)
        {
            var affixType = randomAffixes[i];
            // Získej název a popis přes dočasný komponent
            string label = null;
            string desc = null;
            try {
                var temp = targetCard.gameObject.AddComponent(affixType) as CardAffix;
                if (temp != null)
                {
                    label = temp.AffixName;
                    desc = temp.Description;
                    DestroyImmediate(temp);
                }
            } catch { label = affixType.Name; desc = ""; }
            if (string.IsNullOrEmpty(label)) label = affixType.Name;
            if (string.IsNullOrEmpty(desc)) desc = "";
            // Odstraň prefix "affix" z labelu (case-insensitive)
            if (label.ToLower().StartsWith("affix"))
            {
                label = label.Substring(5).TrimStart('_', ' ');
            }
            // JSON override – vyšší priorita
            LoadOverrides();
            if (overrideMap != null && overrideMap.TryGetValue(label, out var ov))
            {
                if (!string.IsNullOrEmpty(ov.name)) label = ov.name;
                if (!string.IsNullOrEmpty(ov.desc)) desc = ov.desc;
            }
            float x = xStart + i * xStep;
            CreateAffixButton(label, affixType, x, y, desc, true, i);
        }

        // Spusť animaci otevření
        StartCoroutine(AnimateOpen());
    }

    // (Odstraněno: duplicitní a neplatná deklarace CreateAffixButton)
    // odstraněno: neplatná složená závorka
    // Pokud allowOnlyOne=true, po kliknutí na tlačítko se panel zavře a žádný další affix už nejde přidat v tomto kole
    private void CreateAffixButton(string label, System.Type affixType, float x, float y, string desc, bool allowOnlyOne = false, int index = 0)
    {
        GameObject btnObj = new GameObject(label + "Button");
        btnObj.transform.SetParent(panel.transform, false);
        Button btn = btnObj.AddComponent<Button>();
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.06f, 0.04f, 0.14f, 0.95f);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(135, 75);
        rect.anchoredPosition = new Vector2(x, y);

        // Cyan obrys na buttonu
        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.0f, 0.65f, 0.85f, 0.5f);
        btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Hover barvy – sci-fi
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.06f, 0.04f, 0.14f, 0.95f);
        cb.highlightedColor = new Color(0.05f, 0.15f, 0.3f, 1f);
        cb.pressedColor = new Color(0.0f, 0.3f, 0.5f, 1f);
        cb.selectedColor = new Color(0.05f, 0.15f, 0.3f, 1f);
        btn.colors = cb;

        // Ikona affixu (malá, nahoře v buttonu)
        if (iconManager != null)
        {
            Sprite icon = iconManager.GetIcon(label);
            if (icon != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(btnObj.transform, false);
                var iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.rectTransform.sizeDelta = new Vector2(32, 32);
                iconImg.rectTransform.anchoredPosition = new Vector2(0, 12);
            }
        }

        // Název affixu – vycentrovaný ve spodní části buttonu
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text btnText = txtObj.AddComponent<Text>();
        btnText.text = label;
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 18;
        btnText.color = new Color(0.7f, 0.85f, 1f, 1f);
        btnText.alignment = TextAnchor.MiddleCenter;
        RectTransform txtRect = btnText.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0, 0);
        txtRect.anchorMax = new Vector2(1, 0.45f);
        txtRect.offsetMin = new Vector2(4, 2);
        txtRect.offsetMax = new Vector2(-4, 0);
        // Glow na text
        Shadow txtGlow = txtObj.AddComponent<Shadow>();
        txtGlow.effectColor = new Color(0.0f, 0.6f, 1f, 0.4f);
        txtGlow.effectDistance = new Vector2(1, -1);

        // Uložit pro animaci
        btnObj.transform.localScale = Vector3.zero;
        buttonObjects.Add(btnObj);

        // Tooltip na hover (EventTrigger) + scale efekt
        EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => {
            ShowTooltip(label, desc);
            if (btnObj != null) StartCoroutine(AnimateButtonScale(btnObj, 1.08f, 0.1f));
        });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => {
            HideTooltip();
            if (btnObj != null) StartCoroutine(AnimateButtonScale(btnObj, 1f, 0.1f));
        });
        trigger.triggers.Add(exitEntry);

        btn.onClick.AddListener(() => {
            Debug.Log($"[AbilityUIPanel] Kliknuto na '{label}'");
            HideTooltip();
            StartCoroutine(AnimateSelection(btnObj, label, affixType, allowOnlyOne));
        });
    }

    private IEnumerator AnimateSelection(GameObject selectedBtn, string label, System.Type affixType, bool allowOnlyOne)
    {
        // Zablokuj všechny buttony
        foreach (var bo in buttonObjects)
        {
            if (bo == null) continue;
            var b = bo.GetComponent<Button>();
            if (b != null) b.interactable = false;
        }

        Image selectedImg = selectedBtn.GetComponent<Image>();
        Outline selectedOutline = selectedBtn.GetComponent<Outline>();

        // 1) Ostatní buttony fade-out + zmenšení
        foreach (var bo in buttonObjects)
        {
            if (bo == null || bo == selectedBtn) continue;
            StartCoroutine(FadeOutButton(bo, 0.2f));
        }

        // 2) Vybraný button – cyan flash + pulse
        float flashDur = 0.15f;
        float elapsed = 0f;
        Color origColor = selectedImg != null ? selectedImg.color : Color.black;
        Color flashColor = new Color(0.0f, 0.9f, 1f, 1f);
        // Flash in
        while (elapsed < flashDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flashDur);
            if (selectedImg != null) selectedImg.color = Color.Lerp(origColor, flashColor, t);
            if (selectedOutline != null) selectedOutline.effectColor = Color.Lerp(new Color(0f, 0.65f, 0.85f, 0.5f), new Color(0f, 1f, 1f, 1f), t);
            selectedBtn.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.15f, 1.15f, 1f), t);
            yield return null;
        }
        // Flash hold
        yield return new WaitForSecondsRealtime(0.1f);
        // Flash out + scale back
        elapsed = 0f;
        while (elapsed < flashDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flashDur);
            if (selectedImg != null) selectedImg.color = Color.Lerp(flashColor, new Color(0.0f, 0.2f, 0.35f, 0.95f), t);
            selectedBtn.transform.localScale = Vector3.Lerp(new Vector3(1.15f, 1.15f, 1f), Vector3.one, t);
            yield return null;
        }

        // 3) Aplikuj affix na kartu
        ApplyAffix(label, affixType);

        // 4) Flash na kartě (pokud existuje)
        if (targetCard != null)
        {
            yield return StartCoroutine(FlashCard(targetCard.gameObject));
        }

        yield return new WaitForSecondsRealtime(0.15f);

        // 5) Zavři panel
        if (allowOnlyOne)
        {
            ClosePanel();
        }
    }

    private IEnumerator FadeOutButton(GameObject btnObj, float duration)
    {
        CanvasGroup cg = btnObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = btnObj.AddComponent<CanvasGroup>();
        Vector3 startScale = btnObj.transform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (btnObj == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = 1f - t;
            btnObj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.85f, t);
            yield return null;
        }
        if (btnObj != null) btnObj.SetActive(false);
    }

    private IEnumerator FlashCard(GameObject cardObj)
    {
        // Najdi všechny renderery na kartě a flashni je bíle/cyan
        var renderers = cardObj.GetComponentsInChildren<Renderer>();
        var originalColors = new System.Collections.Generic.Dictionary<Renderer, Color>();
        foreach (var r in renderers)
        {
            if (r.material.HasProperty("_Color"))
                originalColors[r] = r.material.color;
        }

        Color flashColor = new Color(0.3f, 0.9f, 1f, 1f);
        // Flash in
        float dur = 0.12f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null && kvp.Key.material.HasProperty("_Color"))
                    kvp.Key.material.color = Color.Lerp(kvp.Value, flashColor, t);
            }
            yield return null;
        }
        // Flash out
        elapsed = 0f;
        while (elapsed < dur * 2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / (dur * 2f));
            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null && kvp.Key.material.HasProperty("_Color"))
                    kvp.Key.material.color = Color.Lerp(flashColor, kvp.Value, t);
            }
            yield return null;
        }
        // Obnov originální barvy
        foreach (var kvp in originalColors)
        {
            if (kvp.Key != null && kvp.Key.material.HasProperty("_Color"))
                kvp.Key.material.color = kvp.Value;
        }
    }

    private void ApplyAffix(string label, System.Type affixType)
    {
        if (targetCard != null && affixType != null)
        {
            if (targetCard.affixes != null && targetCard.affixes.Count > 0)
            {
                Debug.LogWarning($"[AbilityUIPanel] Karta '{targetCard.name}' už má affix, další nelze přidat.");
                return;
            }
            var affix = targetCard.gameObject.AddComponent(affixType) as CardAffix;
            if (affix != null)
            {
                targetCard.affixes.Add(affix);
                var affixData = targetCard.gameObject.AddComponent<AffixData>();
                affixData.affixType = label;
                Sprite icon = null;
                if (iconManager != null)
                {
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
                    foreach (var entry in iconManager.icons)
                    {
                        string entryNorm = Normalize(entry.affixType);
                        if (entryNorm == labelNorm || entryNorm == typeNorm)
                        {
                            icon = entry.icon;
                            break;
                        }
                    }
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
                }
                affixData.icon = icon;
                affixData.ShowIcon();
            }
            if (GameManager.Instance != null && GameManager.Instance.playerDeck != null)
                GameManager.Instance.playerDeck.UpdateDynamicAffixes();
        }
    }

    public void ClosePanel()
    {
        HideTooltip();
        StartCoroutine(AnimateClose());
    }

    private void DestroyPanel()
    {
        if (tooltipObj != null) Destroy(tooltipObj);
        activePanel = null;
        Destroy(panel);
        Destroy(gameObject);
    }

    private void CreateTooltip(GameObject canvasObj)
    {
        tooltipObj = new GameObject("AffixTooltip");
        tooltipObj.transform.SetParent(canvasObj.transform, false);
        // Pozadí – tmavé vesmírné
        Image bg = tooltipObj.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.01f, 0.06f, 0.96f);
        // Cyan obrys
        Outline outline = tooltipObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.0f, 0.75f, 0.95f, 0.65f);
        outline.effectDistance = new Vector2(2, -2);
        Outline outline2 = tooltipObj.AddComponent<Outline>();
        outline2.effectColor = new Color(0.0f, 0.5f, 0.8f, 0.25f);
        outline2.effectDistance = new Vector2(3, -3);

        RectTransform ttRect = tooltipObj.GetComponent<RectTransform>();
        ttRect.pivot = new Vector2(0, 0);
        ttRect.sizeDelta = new Vector2(280, 85);

        // Název affixu – cyan
        GameObject titleObj = new GameObject("TooltipTitle");
        titleObj.transform.SetParent(tooltipObj.transform, false);
        tooltipTitle = titleObj.AddComponent<Text>();
        tooltipTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipTitle.fontSize = 20;
        tooltipTitle.color = new Color(0.0f, 0.9f, 1f, 1f);
        tooltipTitle.alignment = TextAnchor.UpperLeft;
        Shadow titleGlow = titleObj.AddComponent<Shadow>();
        titleGlow.effectColor = new Color(0.0f, 0.6f, 1f, 0.4f);
        titleGlow.effectDistance = new Vector2(1, -1);
        RectTransform titleRect = tooltipTitle.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.55f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, -8);

        // Popis – světle modrošedý
        GameObject descObj = new GameObject("TooltipDesc");
        descObj.transform.SetParent(tooltipObj.transform, false);
        tooltipDesc = descObj.AddComponent<Text>();
        tooltipDesc.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipDesc.fontSize = 15;
        tooltipDesc.color = new Color(0.6f, 0.75f, 0.85f, 1f);
        tooltipDesc.alignment = TextAnchor.UpperLeft;
        RectTransform descRect = tooltipDesc.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 0.55f);
        descRect.offsetMin = new Vector2(10, 6);
        descRect.offsetMax = new Vector2(-10, 0);

        tooltipObj.SetActive(false);
        tooltipVisible = false;
    }

    private void ShowTooltip(string title, string desc)
    {
        if (tooltipObj == null) return;
        tooltipTitle.text = title;
        tooltipDesc.text = desc;
        tooltipObj.SetActive(true);
        tooltipVisible = true;
    }

    private void HideTooltip()
    {
        if (tooltipObj == null) return;
        tooltipObj.SetActive(false);
        tooltipVisible = false;
    }

    // === ANIMACE ===

    private IEnumerator AnimateOpen()
    {
        // Panel fade-in + scale
        panel.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
        float duration = 0.25f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Ease out back
            float overshoot = 1.4f;
            float t1 = t - 1f;
            float scale = 1f + t1 * t1 * ((overshoot + 1f) * t1 + overshoot);
            panel.transform.localScale = new Vector3(scale, scale, 1f);
            panelCanvasGroup.alpha = Mathf.Clamp01(t * 2f);
            yield return null;
        }
        panel.transform.localScale = Vector3.one;
        panelCanvasGroup.alpha = 1f;

        // Buttons pop-in jeden po druhém
        for (int i = 0; i < buttonObjects.Count; i++)
        {
            if (buttonObjects[i] == null) continue;
            StartCoroutine(AnimateButtonPopIn(buttonObjects[i], i * 0.08f));
        }
    }

    private IEnumerator AnimateButtonPopIn(GameObject btnObj, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (btnObj == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease out back
            float overshoot = 2f;
            float t1 = t - 1f;
            float s = 1f + t1 * t1 * ((overshoot + 1f) * t1 + overshoot);
            btnObj.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (btnObj != null) btnObj.transform.localScale = Vector3.one;
    }

    private IEnumerator AnimateButtonScale(GameObject btnObj, float targetScale, float duration)
    {
        if (btnObj == null) yield break;
        Vector3 start = btnObj.transform.localScale;
        Vector3 end = new Vector3(targetScale, targetScale, 1f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (btnObj == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Smooth step
            t = t * t * (3f - 2f * t);
            btnObj.transform.localScale = Vector3.Lerp(start, end, t);
            yield return null;
        }
        if (btnObj != null) btnObj.transform.localScale = end;
    }

    private IEnumerator AnimateClose()
    {
        float duration = 0.15f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f - t;
            if (panel != null) panel.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.9f, 0.9f, 1f), t);
            yield return null;
        }
        DestroyPanel();
    }
}
