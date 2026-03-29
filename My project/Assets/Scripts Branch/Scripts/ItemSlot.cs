using UnityEngine;

/// <summary>
/// Pedestal – přidej na prázdný GO ve scéně.
/// Při startu spawnne item prefab nad sebe, ten se vznáší a rotuje.
/// Kliknutím se item aktivuje.
///
/// NASTAVENÍ:
/// 1. Vytvoř prázdný GO "Pedestal" v scéně → přidej tento skript.
/// 2. Přiřaď "Item Prefab" (GO s Colliderem a MeshRenderer/SpriteRenderer).
/// 3. Přiřaď "Item" (ItemData ScriptableObject).
/// 4. ItemManager se najde automaticky.
/// </summary>
public class ItemSlot : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Data tohoto itemu")]
    public ItemData item;

    [Tooltip("Prefab itemu (spawnne se nad pedestal)")]
    public GameObject itemPrefab;

    [Header("Vznášení")]
    [Tooltip("O kolik nad pedestalem se item vznáší")]
    public float floatHeight = 1.5f;

    [Tooltip("Amplituda bobání nahoru/dolů")]
    public float bobAmplitude = 0.25f;

    [Tooltip("Rychlost bobání")]
    public float bobSpeed = 2f;

    [Tooltip("Rychlost rotace (stupně/s)")]
    public float rotateSpeed = 60f;

    [Header("Hover")]
    [Tooltip("Zvětšení při hoveru")]
    public float hoverScale = 1.3f;

    [Tooltip("Rychlost přechodu hoveru")]
    public float hoverLerpSpeed = 8f;

    [Tooltip("Jas zvýraznění při hoveru")]
    public float hoverBrightness = 0.15f;

    [Header("Použití")]
    [Tooltip("Zmizí item po použití?")]
    public bool consumeOnUse = true;

    private ItemManager manager;
    private GameObject spawnedItem;
    private Vector3 floatOrigin;
    private Vector3 baseScale;
    private Color baseColor;
    private Renderer rend;
    private bool isHovered;
    private bool isUsed;
    private float bobOffset;

    void Start()
    {
        manager = FindObjectOfType<ItemManager>();
        bobOffset = Random.Range(0f, Mathf.PI * 2f);

        if (itemPrefab == null) return;

        // Spawn prefab nad pedestal
        floatOrigin = transform.position + Vector3.up * floatHeight;
        spawnedItem = Instantiate(itemPrefab, floatOrigin, Quaternion.identity);
        spawnedItem.transform.SetParent(transform);

        baseScale = spawnedItem.transform.localScale;

        rend = spawnedItem.GetComponent<Renderer>();
        if (rend == null) rend = spawnedItem.GetComponentInChildren<Renderer>();
        if (rend != null && rend.material != null)
            baseColor = rend.material.color;
    }

    void Update()
    {
        if (isUsed || spawnedItem == null) return;

        // ─── Rotace ───
        spawnedItem.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // ─── Bobání ───
        float bob = Mathf.Sin((Time.time * bobSpeed) + bobOffset) * bobAmplitude;
        Vector3 targetPos = floatOrigin + Vector3.up * bob;
        spawnedItem.transform.position = Vector3.Lerp(spawnedItem.transform.position, targetPos, Time.deltaTime * 10f);

        // ─── Hover scale ───
        float targetScaleMul = isHovered ? hoverScale : 1f;
        spawnedItem.transform.localScale = Vector3.Lerp(spawnedItem.transform.localScale, baseScale * targetScaleMul, Time.deltaTime * hoverLerpSpeed);

        // ─── Hover brightness ───
        if (rend != null && rend.material != null)
        {
            Color target = isHovered
                ? baseColor + new Color(hoverBrightness, hoverBrightness, hoverBrightness, 0f)
                : baseColor;
            rend.material.color = Color.Lerp(rend.material.color, target, Time.deltaTime * hoverLerpSpeed);
        }

        // ─── Kliknutí ───
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == spawnedItem.transform || hit.transform.IsChildOf(spawnedItem.transform))
                    UseItem();
            }
        }

        isHovered = false;
    }

    void FixedUpdate()
    {
        // Hover detekce přes raycast každý frame
        if (isUsed || spawnedItem == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == spawnedItem.transform || hit.transform.IsChildOf(spawnedItem.transform))
                isHovered = true;
        }
    }

    void UseItem()
    {
        if (isUsed) return;
        if (manager == null || item == null) return;
        manager.ActivateItem(item);
        if (consumeOnUse)
        {
            isUsed = true;
            StartCoroutine(ConsumeAnimation());
        }
    }

    System.Collections.IEnumerator ConsumeAnimation()
    {
        if (spawnedItem == null) yield break;

        float duration = 0.5f;
        float elapsed = 0f;
        Transform t = spawnedItem.transform;
        Vector3 startScale = t.localScale;
        Vector3 startPos = t.position;
        Color startColor = baseColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;

            t.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
            t.position = startPos + Vector3.up * (p * 2f);
            t.Rotate(Vector3.up, (rotateSpeed + p * 720f) * Time.deltaTime, Space.World);

            if (rend != null && rend.material != null)
            {
                Color c = startColor;
                c.a = 1f - p;
                rend.material.color = c;
            }

            yield return null;
        }

        Destroy(spawnedItem);
    }
}
