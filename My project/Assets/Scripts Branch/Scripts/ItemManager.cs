using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spravuje itemy/schopnosti hráče. Itemy jsou ve scéně jako GameObjecty –
/// hráč na ně klikne a aktivuje efekt.
///
/// NASTAVENÍ:
/// 1. Vytvoř prázdný GO "ItemManager" → přidej tento skript.
/// 2. Přiřaď reference na DeckManager, EnemyManager, GameManager.
/// 3. Vytvoř ScoutTemplate (CardData: Scout, 1 HP, 1 DMG) a DebrisTemplate (CardData: 0 DMG, 2 HP).
/// 4. Ve scéně vytvoř GO pro každý item (s Colliderem) → přidej komponentu ItemSlot.
/// </summary>
public class ItemManager : MonoBehaviour
{
    [Header("Reference")]
    public DeckManager playerDeck;
    public EnemyManager enemyManager;

    [Header("Šablony karet")]
    [Tooltip("CardData pro Scout raketu (1 HP, 1 DMG)")]
    public CardData scoutTemplate;

    [Tooltip("CardData pro vesmírný odpad (0 DMG, 2 HP)")]
    public CardData debrisTemplate;

    [Header("Nastavení")]
    public float effectDelay = 0.3f;

    private bool waitingForTarget;
    private ItemType pendingItem;
    private System.Action<int, bool> onSlotSelected;

    void Start()
    {
        if (playerDeck == null) playerDeck = FindObjectOfType<DeckManager>();
        if (enemyManager == null) enemyManager = FindObjectOfType<EnemyManager>();
    }

    /// <summary>Aktivuje item. Volá ItemSlot po kliknutí.</summary>
    public void ActivateItem(ItemData item)
    {
        if (waitingForTarget) return;
        GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.PlayerDraw;
        if (state != GameState.PlayerDraw && state != GameState.PlayerPlace)
            return;
        StartCoroutine(ExecuteItem(item));
    }

    IEnumerator ExecuteItem(ItemData item)
    {
        switch (item.type)
        {
            case ItemType.ElectricShock:
                yield return ElectricShock();
                break;
            case ItemType.Shield:
                yield return WaitForCardSelection(item.type);
                break;
            case ItemType.Asteroid:
                yield return AsteroidStrike();
                break;
            case ItemType.ScoutRocket:
                ScoutRocketToHand();
                break;
            case ItemType.MoveLeft:
                yield return WaitForPlayerSlotSelection(item.type);
                break;
            case ItemType.MoveRight:
                yield return WaitForPlayerSlotSelection(item.type);
                break;
            case ItemType.Magnet:
                MagnetClearEffects();
                break;
            case ItemType.GlobalEffect:
                break;
            case ItemType.Hook:
                yield return WaitForEnemySlotSelection(item.type);
                break;
            case ItemType.Scissors:
                yield return WaitForAnyCardSelection(item.type);
                break;
            case ItemType.SpaceDebris:
                yield return SpaceDebrisFill();
                break;
        }
    }

    // ═══════════════════════════════════════
    // 1) ELECTRIC SHOCK – stun všech enemy karet na 1 kolo
    // ═══════════════════════════════════════
    IEnumerator ElectricShock()
    {
        Debug.Log("[ItemManager] Elektrický výboj! Enemy karty omráčeny.");
        for (int i = 0; i < enemyManager.enemyFieldSlots.Length; i++)
        {
            Card card = enemyManager.GetCardAtSlot(i);
            if (card != null)
            {
                card.isStunned = true;
                yield return card.ShakeAnimation(0.15f);
            }
        }
    }

    // ═══════════════════════════════════════
    // 2) SHIELD – hráč klikne na kartu → shield na 1 kolo
    // ═══════════════════════════════════════
    IEnumerator WaitForCardSelection(ItemType type)
    {
        Debug.Log("[ItemManager] Klikni na kartu pro štít...");
        int selectedSlot = -1;
        bool isEnemy = false;
        waitingForTarget = true;

        onSlotSelected = (slot, enemy) => { selectedSlot = slot; isEnemy = enemy; };

        yield return new WaitUntil(() => selectedSlot >= 0 || Input.GetKeyDown(KeyCode.Escape));

        waitingForTarget = false;
        onSlotSelected = null;

        if (selectedSlot < 0) yield break;

        Card target = isEnemy
            ? enemyManager.GetCardAtSlot(selectedSlot)
            : playerDeck.GetCardAtSlot(selectedSlot);

        if (target != null)
        {
            target.SetShielded(true);
        }
    }

    // ═══════════════════════════════════════
    // 3) ASTEROID – 0-3 dmg na všech 10 polích
    // ═══════════════════════════════════════
    IEnumerator AsteroidStrike()
    {
        Debug.Log("[ItemManager] Asteroid dopadá!");
        int totalSlots = Mathf.Max(playerDeck.SlotCount, enemyManager.enemyFieldSlots.Length);

        for (int i = 0; i < totalSlots; i++)
        {
            int dmg = Random.Range(0, 4);

            // Hráčova strana
            if (i < playerDeck.SlotCount)
            {
                Card pCard = playerDeck.GetCardAtSlot(i);
                if (pCard != null && dmg > 0)
                {
                    yield return pCard.ShakeAnimation(0.15f);
                    if (pCard.TakeDamage(dmg))
                        playerDeck.RemoveCardFromSlot(i);
                }
            }

            dmg = Random.Range(0, 4);

            // Enemy strana
            if (i < enemyManager.enemyFieldSlots.Length)
            {
                Card eCard = enemyManager.GetCardAtSlot(i);
                if (eCard != null && dmg > 0)
                {
                    yield return eCard.ShakeAnimation(0.15f);
                    if (eCard.TakeDamage(dmg))
                        enemyManager.RemoveCardFromSlot(i);
                }
            }

            yield return new WaitForSeconds(effectDelay * 0.5f);
        }
    }

    // ═══════════════════════════════════════
    // 4) SCOUT ROCKET – 1HP/1DMG Scout do ruky
    // ═══════════════════════════════════════
    void ScoutRocketToHand()
    {
        if (scoutTemplate == null)
        {
            Debug.LogError("[ItemManager] Chybí ScoutTemplate!");
            return;
        }
        playerDeck.AddCardToHand(scoutTemplate);
    }

    // ═══════════════════════════════════════
    // 5+6) MOVE LEFT / RIGHT – hráč vybere svou kartu a posune
    // ═══════════════════════════════════════
    IEnumerator WaitForPlayerSlotSelection(ItemType type)
    {
        bool moveLeft = type == ItemType.MoveLeft;
        Debug.Log($"[ItemManager] Klikni na svou kartu (posun {(moveLeft ? "vlevo" : "vpravo")})...");

        int selectedSlot = -1;
        waitingForTarget = true;
        onSlotSelected = (slot, enemy) =>
        {
            if (!enemy) selectedSlot = slot;
        };

        yield return new WaitUntil(() => selectedSlot >= 0 || Input.GetKeyDown(KeyCode.Escape));

        waitingForTarget = false;
        onSlotSelected = null;

        if (selectedSlot < 0) yield break;

        int targetSlot = moveLeft ? selectedSlot - 1 : selectedSlot + 1;

        if (targetSlot < 0 || targetSlot >= playerDeck.SlotCount)
        {
            Debug.LogWarning("[ItemManager] Nelze posunout – mimo pole!");
            yield break;
        }

        if (playerDeck.IsSlotOccupied(targetSlot))
        {
            Debug.LogWarning("[ItemManager] Cílový slot je obsazený!");
            yield break;
        }

        yield return playerDeck.MoveCardBetweenSlots(selectedSlot, targetSlot);
    }

    // ═══════════════════════════════════════
    // 7) MAGNET – odstraní efekty všech karet
    // ═══════════════════════════════════════
    void MagnetClearEffects()
    {
        Debug.Log("[ItemManager] Magnet! Všechny efekty odstraněny.");
        for (int i = 0; i < playerDeck.SlotCount; i++)
        {
            Card c = playerDeck.GetCardAtSlot(i);
            if (c != null) c.ClearEffects();
        }
        for (int i = 0; i < enemyManager.enemyFieldSlots.Length; i++)
        {
            Card c = enemyManager.GetCardAtSlot(i);
            if (c != null) c.ClearEffects();
        }
    }

    // ═══════════════════════════════════════
    // 9) HOOK – přetáhne enemy kartu na hráčovu stranu (stejný index)
    // ═══════════════════════════════════════
    IEnumerator WaitForEnemySlotSelection(ItemType type)
    {
        Debug.Log("[ItemManager] Klikni na enemy kartu pro hák...");
        int selectedSlot = -1;
        waitingForTarget = true;
        onSlotSelected = (slot, enemy) =>
        {
            if (enemy) selectedSlot = slot;
        };

        yield return new WaitUntil(() => selectedSlot >= 0 || Input.GetKeyDown(KeyCode.Escape));

        waitingForTarget = false;
        onSlotSelected = null;

        if (selectedSlot < 0) yield break;

        // Kontrola – hráčův slot musí být volný
        if (selectedSlot >= playerDeck.SlotCount || playerDeck.IsSlotOccupied(selectedSlot))
        {
            Debug.LogWarning("[ItemManager] Hráčův slot je obsazený – nelze přetáhnout!");
            yield break;
        }

        // Odeber enemy kartu a přesuň na hráčovu stranu
        Card card = enemyManager.TakeCardFromSlot(selectedSlot);
        if (card == null) yield break;

        yield return playerDeck.PlaceCardDirectly(card, selectedSlot);
    }

    // ═══════════════════════════════════════
    // 10) SCISSORS – zničí jakoukoliv kartu
    // ═══════════════════════════════════════
    IEnumerator WaitForAnyCardSelection(ItemType type)
    {
        Debug.Log("[ItemManager] Klikni na kartu ke zničení (nůžky)...");
        int selectedSlot = -1;
        bool isEnemy = false;
        waitingForTarget = true;
        onSlotSelected = (slot, enemy) => { selectedSlot = slot; isEnemy = enemy; };

        yield return new WaitUntil(() => selectedSlot >= 0 || Input.GetKeyDown(KeyCode.Escape));

        waitingForTarget = false;
        onSlotSelected = null;

        if (selectedSlot < 0) yield break;

        if (isEnemy)
        {
            Card c = enemyManager.GetCardAtSlot(selectedSlot);
            if (c != null)
            {
                Debug.Log($"[ItemManager] Nůžky ničí '{c.data.cardName}'!");
                enemyManager.RemoveCardFromSlot(selectedSlot);
            }
        }
        else
        {
            Card c = playerDeck.GetCardAtSlot(selectedSlot);
            if (c != null)
            {
                Debug.Log($"[ItemManager] Nůžky ničí '{c.data.cardName}'!");
                playerDeck.RemoveCardFromSlot(selectedSlot);
            }
        }
    }

    // ═══════════════════════════════════════
    // 11) SPACE DEBRIS – zaplní volná hráčova pole bordelem
    // ═══════════════════════════════════════
    IEnumerator SpaceDebrisFill()
    {
        if (debrisTemplate == null)
        {
            Debug.LogError("[ItemManager] Chybí DebrisTemplate!");
            yield break;
        }

        Debug.Log("[ItemManager] Vesmírný odpad zaplňuje pole!");

        for (int i = 0; i < playerDeck.SlotCount; i++)
        {
            if (!playerDeck.IsSlotOccupied(i))
            {
                yield return playerDeck.SpawnCardOnSlot(debrisTemplate, i);
                // Označ jako debris
                Card c = playerDeck.GetCardAtSlot(i);
                if (c != null) c.isDebris = true;
                yield return new WaitForSeconds(effectDelay);
            }
        }
    }

    // ═══════════════════════════════════════
    // Targeting systém – volá Update pro kliknutí na pole
    // ═══════════════════════════════════════

    void Update()
    {
        if (!waitingForTarget) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        // Zkus najít kartu
        Card card = hit.collider.GetComponent<Card>();
        if (card == null) card = hit.collider.GetComponentInParent<Card>();

        if (card != null)
        {
            // Najdi v kterém slotu je
            for (int i = 0; i < playerDeck.SlotCount; i++)
            {
                if (playerDeck.GetCardAtSlot(i) == card)
                {
                    onSlotSelected?.Invoke(i, false);
                    return;
                }
            }
            for (int i = 0; i < enemyManager.enemyFieldSlots.Length; i++)
            {
                if (enemyManager.GetCardAtSlot(i) == card)
                {
                    onSlotSelected?.Invoke(i, true);
                    return;
                }
            }
        }

        // Zkus sloty přímo
        Transform hitT = hit.collider.transform;
        for (int i = 0; i < playerDeck.SlotCount; i++)
        {
            if (hitT == playerDeck.fieldSlots[i])
            {
                onSlotSelected?.Invoke(i, false);
                return;
            }
        }
        for (int i = 0; i < enemyManager.enemyFieldSlots.Length; i++)
        {
            if (hitT == enemyManager.enemyFieldSlots[i])
            {
                onSlotSelected?.Invoke(i, true);
                return;
            }
        }
    }
}
