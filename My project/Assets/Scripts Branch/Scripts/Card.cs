using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Komponenta připojená na každý GameObject karty.
/// Funguje s 3D (MeshRenderer) i 2D (SpriteRenderer) prefaby.
/// </summary>
public class Card : MonoBehaviour
{
    [Header("Defaultní model pro všechny karty bez vlastního modelu.")]
    [Tooltip("Defaultní model pro všechny karty bez vlastního modelu.")]
    public static GameObject defaultModel;

    /// <summary>
    /// Vrátí model, který má být použit pro tuto kartu (nejdřív custom z CardData, jinak default).
    /// </summary>
    public GameObject GetModel()
    {
        // 1. Pokud má GameObject karty vlastní model jako child, použij ten
        foreach (Transform child in transform)
        {
            if (child.gameObject.CompareTag("CardModel")) // nastav tag na model prefabech
                return child.gameObject;
        }
        // 2. Pokud je v CardData customModel, použij ten
        if (data != null && data.customModel != null)
            return data.customModel;
        // 3. Jinak default
        return defaultModel;
    }

    // Affixy (schopnosti/efekty) připojené ke kartě
    public System.Collections.Generic.List<CardAffix> affixes = new System.Collections.Generic.List<CardAffix>();
    public CardData data;

    [Header("Anchor pro affix ikony")]
    public Transform affixAnchor;

    [Header("Hover Effect")]
    public float hoverLiftAmount = 0.3f;
    public float hoverBrightness = 0.2f;
    public float hoverSpeed = 8f;
    [Tooltip("Jak daleko před kameru se karta přiblíží při hoveru (0 = jen zvednutí)")]
    public float hoverPreviewDistance = 1.4f;
    [Tooltip("Jak dlouho zůstane highlight po sjetí kurzoru (v sekundách)")]
    public float hoverGracePeriod = 0.3f;
    [Tooltip("Jas zvýraznění vybrané karty")]
    public float selectedBrightness = 0.35f;
    [Tooltip("O kolik se karta v ruce zvedne když je vybraná")]
    public float selectedLift = 0.25f;

    [Header("Stat Display")]
    [Tooltip("TMP pro životy (vlevo dole na kartě)")]
    public TMP_Text healthText;
    [Tooltip("TMP pro damage (vpravo dole na kartě)")]
    public TMP_Text damageText;

    [Header("Efekty")]
    [Tooltip("GameObject štítu (vizuál nad kartou, aktivuje se při shieldu)")]
    public GameObject shieldVisual;

    [HideInInspector] public int currentHealth;
    [HideInInspector] public int currentDamage;
    [HideInInspector] public bool isStunned;
    [HideInInspector] public bool isShielded;
    [HideInInspector] public bool isDebris;

    private Renderer cardRenderer;
    private SpriteRenderer spriteRenderer;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;
    private Color baseColor;
    private bool isHovered;
    private bool basePositionSet;
    private bool isOnField;
    private bool isSelected;
    private float hoverTimer;

    void Awake()
    {
        cardRenderer = GetComponent<Renderer>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!basePositionSet) return;

        // Grace period – hover zůstane aktivní ještě chvíli po sjetí kurzoru
        if (isHovered)
        {
            hoverTimer = hoverGracePeriod;
        }
        else
        {
            hoverTimer -= Time.deltaTime;
        }

        bool showHover = hoverTimer > 0f;

        // Cílová pozice – zvednutá pokud je vybraná
        Vector3 targetPos = isSelected ? basePosition + Vector3.up * selectedLift : basePosition;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * hoverSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, baseRotation, Time.deltaTime * hoverSpeed);

        if (cardRenderer != null && cardRenderer.material != null)
        {
            Color targetColor;
            if (isSelected)
                targetColor = baseColor + new Color(selectedBrightness, selectedBrightness, selectedBrightness, 0f);
            else if (showHover)
                targetColor = baseColor + new Color(hoverBrightness, hoverBrightness, hoverBrightness, 0f);
            else
                targetColor = baseColor;

            cardRenderer.material.color = Color.Lerp(cardRenderer.material.color, targetColor, Time.deltaTime * hoverSpeed);
        }

        isHovered = false;

        // Detekce kliknutí kolečkem myši na kartu
        if (Input.GetMouseButtonDown(2)) // 2 = middle mouse button
        {
            // Zabrání opakovanému otevírání affix menu
            if (GameObject.FindObjectOfType<AbilityUIPanel>() == null)
            {
                // Raycast z kamery na pozici myši
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform == this.transform)
                    {
                        Debug.Log($"Kliknuto kolečkem na kartu: {gameObject.name}");
                        ShowAbilityUI();
                    }
                }
            }
        }
    }

    /// <summary>Volej každý frame když myš najíždí na kartu.</summary>
    public void SetHovered()
    {
        isHovered = true;
    }

    /// <summary>Ukonč hover okamžitě (bez grace period).</summary>
    public void ClearHover()
    {
        isHovered = false;
        hoverTimer = 0f;
    }

    /// <summary>Označ/odznač kartu jako vybranou (zvedne se a zvýrazní).</summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
    }

    /// <summary>Aktualizuje base pozici a rotaci (volat po dokončení pohybu).</summary>
    public void SetBasePosition(Vector3 pos, bool onField = false)
    {
        basePosition = pos;
        baseRotation = transform.rotation;
        basePositionSet = true;
        isOnField = onField;
    }

    /// <summary>Nastaví base pozici + rotaci explicitně (pro dynamický fan v ruce).</summary>
    public void SetBasePositionAndRotation(Vector3 pos, Quaternion rot, bool onField = false)
    {
        basePosition = pos;
        baseRotation = rot;
        basePositionSet = true;
        isOnField = onField;
    }

    /// <summary>
    /// Nastaví vizuál karty podle CardData.
    /// </summary>
    public void Setup(CardData cardData)
    {
        data = cardData;
        if (data == null) return;

        gameObject.name = data.cardName;

        currentHealth = data.health;
        currentDamage = data.damage;

        // Barva podle skupiny
        if (cardRenderer != null && cardRenderer.material != null)
        {
            cardRenderer.material.color = data.GroupColor;
            baseColor = data.GroupColor;
        }

        // Artwork – jen pro 2D SpriteRenderer
        if (spriteRenderer != null && data.artwork != null)
            spriteRenderer.sprite = data.artwork;

        UpdateStatTexts();
    }

    public void UpdateStatTexts()

    {
        if (healthText != null)
            healthText.text = currentHealth.ToString();
        if (damageText != null)
            damageText.text = currentDamage.ToString();
    }

    // Placeholder metoda pro zobrazení UI schopností
    private void ShowAbilityUI()
    {
        // Zabrání otevření menu, pokud už karta má affix
        if (affixes != null && affixes.Count > 0)
        {
            Debug.Log("[Card] Tato karta už má affix, menu se neotevře.");
            return;
        }
        var iconManager = Resources.Load<AffixIconManager>("AffixIcons/AffixIconManager");
        if (iconManager == null)
        {
            Debug.LogWarning("[Card] Asset AffixIconManager nebyl nalezen v Resources! Přetáhni ho do složky Resources nebo přiřaď ručně v kódu.");
        }
        // Oprava volání statické metody Open, fallback na ruční vytvoření pokud by nebyla dostupná
        #if UNITY_EDITOR || UNITY_STANDALONE
        if (typeof(AbilityUIPanel).GetMethod("Open") != null)
        {
            AbilityUIPanel.Open(this, iconManager);
        }
        else
        {
            // Fallback: ruční vytvoření panelu (starý způsob)
            GameObject go = new GameObject("AbilityUIPanel");
            var panel = go.AddComponent<AbilityUIPanel>();
            panel.iconManager = iconManager;
            panel.Init(this);
        }
        #else
        AbilityUIPanel.Open(this, iconManager);
        #endif
    }

    /// <summary>Sníží životy karty. Vrátí true pokud karta zemřela. Shield blokuje všechen dmg.</summary>
    public bool TakeDamage(int amount)
    {
        if (isShielded) return false;

        // Uprav damage podle všech affixů
        int modifiedAmount = amount;
        foreach (var affix in affixes)
        {
            if (affix != null)
                modifiedAmount = affix.OnTakeDamage(this, modifiedAmount);
        }

        currentHealth -= modifiedAmount;
        UpdateStatTexts();
        return currentHealth <= 0;
    }

    /// <summary>Nastav/sundej štít.</summary>
    public void SetShielded(bool shielded)
    {
        isShielded = shielded;
        if (shieldVisual != null)
            shieldVisual.SetActive(shielded);
    }

    /// <summary>Odstraň všechny efekty (stun, shield).</summary>
    public void ClearEffects()
    {
        isStunned = false;
        SetShielded(false);
    }

    /// <summary>
    /// Animovaný pohyb karty po oblouku na cílovou pozici.
    /// arcHeight = výška oblouku (kladná = nahoru, záporná = dolů, 0 = rovně).
    /// </summary>
    public IEnumerator MoveToPosition(Vector3 targetPosition, float duration, float arcHeight = 2f)
    {
        // Během pohybu vypni hover
        basePositionSet = false;

        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        // Náhodná variace oblouku ± 30 %
        float randomArc = arcHeight * Random.Range(0.7f, 1.3f);
        // Náhodný mírný horizontální offset na vrcholu
        float sideOffset = Random.Range(-0.3f, 0.3f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            Vector3 pos = Vector3.Lerp(startPosition, targetPosition, t);

            // Parabola: 4*t*(1-t) má vrchol 1 při t=0.5
            float arc = randomArc * 4f * t * (1f - t);
            pos.y += arc;

            // Mírný boční posun na vrcholu
            pos.x += sideOffset * 4f * t * (1f - t);

            transform.position = pos;
            yield return null;
        }

        transform.position = targetPosition;
        SetBasePosition(targetPosition);
    }

    /// <summary>
    /// Animace útoku: karta se zvedne, přesune nad cíl a vrátí zpět.
    /// </summary>
    public IEnumerator AttackAnimation(Vector3 targetPosition, float duration = 1.5f, float lift = 1.2f)
    {
        Vector3 start = transform.position;
        Vector3 aboveStart = start + Vector3.up * lift;
        Vector3 aboveTarget = targetPosition + Vector3.up * lift;
        float half = duration * 0.5f;

        // Zvednutí a přesun nad cíl
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            // Interpolace mezi start -> aboveStart -> aboveTarget
            Vector3 pos = Vector3.Lerp(aboveStart, aboveTarget, t);
            // Zvedání z původní pozice
            if (t < 0.5f)
                pos = Vector3.Lerp(start, aboveStart, t * 2f);
            transform.position = pos;
            yield return null;
        }

        // Krátká pauza nad cílem
        yield return new WaitForSeconds(0.07f);

        // Návrat zpět
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            Vector3 pos = Vector3.Lerp(aboveTarget, start, t);
            transform.position = pos;
            yield return null;
        }

        transform.position = start;
    }

    /// <summary>
    /// Animace zničení karty – zmenší se, zatočí se a zmizí.
    /// </summary>
    public IEnumerator DestroyAnimation(float duration = 0.6f)
    {
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        // Materiál pro fade-out
        Renderer rend = GetComponent<Renderer>();
        Color startColor = Color.white;
        if (rend != null && rend.material != null)
            startColor = rend.material.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Zmenšování
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // Rotace (spin)
            transform.rotation = startRot * Quaternion.Euler(0f, 0f, t * 360f);

            // Vyletí trochu nahoru
            transform.position = startPos + Vector3.up * (t * 1.5f);

            // Průhlednost
            if (rend != null && rend.material != null)
            {
                Color c = startColor;
                c.a = 1f - t;
                rend.material.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Krátká vibrace karty (shake) – signalizuje, že akce nejde provést.
    /// </summary>
    public IEnumerator ShakeAnimation(float duration = 0.4f, float intensity = 0.15f)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Útlum – vibrace slábne ke konci
            float fade = 1f - t;
            float offsetX = Mathf.Sin(t * Mathf.PI * 8f) * intensity * fade;

            transform.position = originalPos + new Vector3(offsetX, 0f, 0f);
            yield return null;
        }

        transform.position = originalPos;
    }
}
