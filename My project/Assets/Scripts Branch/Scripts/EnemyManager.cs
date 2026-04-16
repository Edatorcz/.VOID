using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI nepřítel s virtuálně generovaným balíčkem karet.
///
/// NASTAVENÍ:
/// 1. Vytvoř prázdný GO "EnemyManager" → přidej tento skript.
/// 2. Vytvoř 1 CardData pro každý typ (Support, Scout, Specialist, Striker, Sentinel)
///    → přetáhni do "Card Templates" (5 položek, 1 per group).
/// 3. Přiřaď Card Prefab.
/// 4. Vytvoř prázdné GO "EnemySlot0", "EnemySlot1" ... → přetáhni do "Enemy Field Slots".
/// 5. Přiřaď odkaz na hráčův DeckManager do "Player Deck Manager".
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Header("Card Templates")]
    [Tooltip("1 CardData za každý typ – z nich se generuje virtuální balíček")]
    public CardData supportTemplate;
    public CardData scoutTemplate;
    public CardData specialistTemplate;
    public CardData strikerTemplate;
    public CardData sentinelTemplate;

    [Tooltip("Prefab karty")]
    public GameObject cardPrefab;

    [Header("Pozice")]
    [Tooltip("Transform nepřátelského balíčku (odkud se karty dávají)")]
    public Transform enemyDeckPileTransform;

    [Tooltip("Sloty nepřítele na hrací ploše")]
    public Transform[] enemyFieldSlots;

    [Header("Reference")]
    [Tooltip("Odkaz na hráčův DeckManager")]
    public DeckManager playerDeckManager;

    [Header("Generování balíčku")]
    [Tooltip("Minimální počet karet od každého typu")]
    public int minPerGroup = 1;

    [Tooltip("Maximální počet karet od každého typu")]
    public int maxPerGroup = 4;

    [Header("Start hry")]
    [Tooltip("Minimální počet karet na začátku")]
    public int startCardsMin = 2;

    [Tooltip("Maximální počet karet na začátku")]
    public int startCardsMax = 3;

    [Header("Nastavení")]
    [Tooltip("Prodleva mezi nepřátelskými akcemi (v sekundách)")]
    public float actionDelay = 1f;

    [Tooltip("Prodleva mezi rozdáním karet na začátku")]
    public float dealDelay = 0.4f;

    [Tooltip("Jak dlouho trvá přesun karty (v sekundách)")]
    public float dealDuration = 0.5f;

    [Header("Škálování")]
    [Tooltip("Kolik karet navíc za každý level/kolo (přidá se k min/max per group)")]
    public int scalingPerRound = 0;

    [Header("Affixy")]
    [Tooltip("Šance (0–1), že enemy karta dostane náhodný affix při položení")]
    [Range(0f, 1f)]
    public float affixChance = 0.15f;

    /// <summary>Typy affixů, které mohou dostat enemy karty.</summary>
    private static readonly System.Type[] possibleAffixes = new System.Type[]
    {
        typeof(AffixBoxer),
        typeof(AffixBojovnik),
        typeof(AffixMedic),
        typeof(AffixDav),
        typeof(AffixZelva),
        typeof(AffixPrasklina),
        typeof(AffixPrekvapko),
        typeof(AffixVidlicka),
        typeof(AffixZpatecka),
        typeof(AffixTruhlice),
        typeof(AffixBordelar),
        typeof(AffixKaktus),
        typeof(AffixBomba),
        typeof(AffixEvoluce),
        typeof(AffixUhyb),
        typeof(AffixPozar),
        typeof(AffixSilak),
    };

    private List<CardData> remainingDeck = new List<CardData>();
    private Dictionary<int, Card> occupiedSlots = new Dictionary<int, Card>();
    private bool isTakingTurn;
    private int currentRound = 0;

    void Start()
    {
        // Inicializaci a rozdávání řídí GameManager
    }

    /// <summary>Inicializuje (vygeneruje) nepřátelský balíček.</summary>
    public void Initialize()
    {
        GenerateDeck();
    }

    /// <summary>Volá GameManager – spustí rozdání počátečních karet.</summary>
    public Coroutine DealStartingCardsRoutine()
    {
        return StartCoroutine(DealStartingCards());
    }

    /// <summary>Vygeneruje virtuální balíček s random počtem karet od každého typu.</summary>
    void GenerateDeck()
    {
        remainingDeck.Clear();

        int bonus = currentRound * scalingPerRound;
        int min = minPerGroup + bonus;
        int max = maxPerGroup + bonus;

        AddCardsOfType(supportTemplate, Random.Range(min, max + 1));
        AddCardsOfType(scoutTemplate, Random.Range(min, max + 1));
        AddCardsOfType(specialistTemplate, Random.Range(min, max + 1));
        AddCardsOfType(strikerTemplate, Random.Range(min, max + 1));
        AddCardsOfType(sentinelTemplate, Random.Range(min, max + 1));

        ShuffleDeck();
        Debug.Log($"[EnemyManager] Balíček vygenerován (kolo {currentRound}): {remainingDeck.Count} karet.");
    }

    void AddCardsOfType(CardData template, int count)
    {
        if (template == null) return;
        for (int i = 0; i < count; i++)
            remainingDeck.Add(template);
    }

    void ShuffleDeck()
    {
        for (int i = remainingDeck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (remainingDeck[i], remainingDeck[j]) = (remainingDeck[j], remainingDeck[i]);
        }
    }

    IEnumerator DealStartingCards()
    {
        int count = Random.Range(startCardsMin, startCardsMax + 1);
        count = Mathf.Min(count, Mathf.Min(remainingDeck.Count, enemyFieldSlots.Length));

        // Náhodné sloty
        List<int> slots = new List<int>();
        for (int i = 0; i < enemyFieldSlots.Length; i++)
            slots.Add(i);

        for (int i = slots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }

        for (int i = 0; i < count; i++)
        {
            yield return PlaceCardOnSlot(slots[i]);
            yield return new WaitForSeconds(dealDelay);
        }
    }

    // --- Veřejné API ---

    /// <summary>Spusť nepřátelský tah (fire-and-forget).</summary>
    public void TakeTurn()
    {
        if (!isTakingTurn)
            StartCoroutine(DoFullTurn());
    }

    /// <summary>Zvyš kolo (pro škálování balíčku) a přegeneruj balíček.</summary>
    public void NextRound()
    {
        currentRound++;
        GenerateDeck();
    }

    /// <summary>Je nepřítel uprostřed tahu?</summary>
    public bool IsTakingTurn => isTakingTurn;

    /// <summary>Počet zbývajících karet.</summary>
    public int RemainingCardCount => remainingDeck.Count;

    /// <summary>Je slot obsazený?</summary>
    public bool IsSlotOccupied(int slotIndex) => occupiedSlots.ContainsKey(slotIndex);

    /// <summary>Vrátí kartu na daném slotu (nebo null).</summary>
    public Card GetCardAtSlot(int slotIndex)
    {
        occupiedSlots.TryGetValue(slotIndex, out Card card);
        return card;
    }

    // --- AI logika ---

    /// <summary>Celý nepřátelský tah: zpracuj akce → polož 1-2 karty. Volá GameManager.</summary>
    public IEnumerator DoFullTurn()
    {
        isTakingTurn = true;
        Debug.Log("[EnemyManager] Nepřítel začíná tah...");

        // 1. Zpracuj akce nepřítele (útoky, speciální schopnosti)
        yield return ProcessEnemyActions();

        // 2. Polož 1-2 karty na hrací pole
        int cardsToPlace = Random.Range(1, 3);
        for (int i = 0; i < cardsToPlace; i++)
        {
            if (remainingDeck.Count == 0) break;
            int slotIndex = GetRandomFreeSlot();
            if (slotIndex < 0) break;

            yield return new WaitForSeconds(actionDelay);
            yield return PlaceCardOnSlot(slotIndex);
        }

        yield return new WaitForSeconds(actionDelay);
        isTakingTurn = false;
        Debug.Log("[EnemyManager] Nepřítel ukončil tah.");
        // Spusť OnTurnStart pro všechny affixy hráčových karet (pro Rychlík a další efekty)
        for (int i = 0; i < GameManager.Instance.playerDeck.SlotCount; i++)
        {
            Card card = GameManager.Instance.playerDeck.GetCardAtSlot(i);
            if (card != null)
            {
                foreach (var affix in card.affixes)
                {
                    affix.OnTurnStart(card);
                }
                // Požár – pokud karta umřela na hoření, odstraň ji
                if (card.currentHealth <= 0)
                {
                    Debug.Log($"[EnemyManager] Hráčova '{card.data.cardName}' shořela (Požár)!");
                    GameManager.Instance.playerDeck.RemoveCardFromSlot(i);
                }
            }
        }

        // Spusť OnTurnStart pro všechny affixy enemy karet (Evoluce, Požár apod.)
        for (int i = 0; i < enemyFieldSlots.Length; i++)
        {
            Card eCard = GetCardAtSlot(i);
            if (eCard != null)
            {
                foreach (var affix in eCard.affixes)
                {
                    affix.OnTurnStart(eCard);
                }
                // Požár – pokud karta umřela na hoření, odstraň ji
                if (eCard.currentHealth <= 0)
                {
                    Debug.Log($"[EnemyManager] '{eCard.data.cardName}' shořela (Požár)!");
                    RemoveCardFromSlot(i);
                }
            }
        }
    }

    /// <summary>Zpracování nepřátelských akcí (útoky karet na hráče).</summary>
    IEnumerator ProcessEnemyActions()
    {
        yield return GameManager.Instance.ProcessEnemyAttacks();
    }

    int GetRandomFreeSlot()
    {
        List<int> freeSlots = new List<int>();
        for (int i = 0; i < enemyFieldSlots.Length; i++)
        {
            if (!occupiedSlots.ContainsKey(i))
                freeSlots.Add(i);
        }

        if (freeSlots.Count == 0) return -1;
        return freeSlots[Random.Range(0, freeSlots.Count)];
    }

    IEnumerator PlaceCardOnSlot(int slotIndex)
    {
        if (remainingDeck.Count == 0) yield break;

        CardData cardData = remainingDeck[0];
        remainingDeck.RemoveAt(0);

        Vector3 spawnPos = enemyDeckPileTransform != null
            ? enemyDeckPileTransform.position
            : enemyFieldSlots[slotIndex].position + Vector3.up * 2f;

        GameObject cardObj = Instantiate(cardPrefab, spawnPos, cardPrefab.transform.rotation);
        Card card = cardObj.GetComponent<Card>();

        if (card == null)
            card = cardObj.AddComponent<Card>();

        card.Setup(cardData);

        // Náhodná šance na affix
        if (Random.value < affixChance)
        {
            System.Type affixType = possibleAffixes[Random.Range(0, possibleAffixes.Length)];
            var affix = cardObj.AddComponent(affixType) as CardAffix;
            if (affix != null)
            {
                card.affixes.Add(affix);
                var affixData = cardObj.AddComponent<AffixData>();
                affixData.affixType = affix.AffixName;
                Debug.Log($"[EnemyManager] Enemy karta '{cardData.cardName}' dostala affix: {affix.AffixName}");
            }
        }

        occupiedSlots[slotIndex] = card;

        yield return card.MoveToPosition(enemyFieldSlots[slotIndex].position, dealDuration);
        Quaternion slotRot = enemyFieldSlots[slotIndex].rotation * Quaternion.Euler(0f, 0f, cardPrefab.transform.rotation.eulerAngles.z);
        card.SetBasePositionAndRotation(enemyFieldSlots[slotIndex].position, slotRot, true);

        // Bordelář: spawn Bordel na přilehlých volných slotech
        if (card.affixes.Exists(a => a is AffixBordelar))
        {
            CardData bordelData = GameManager.Instance != null ? GameManager.Instance.bordelTemplate : null;
            if (bordelData != null)
            {
                int[] adj = new int[] { slotIndex - 1, slotIndex + 1 };
                foreach (int a in adj)
                {
                    if (a >= 0 && a < enemyFieldSlots.Length && !occupiedSlots.ContainsKey(a))
                    {
                        Debug.Log($"[EnemyManager] Bordelář: spawn Bordel na enemy slotu {a}.");
                        yield return SpawnCardOnSlot(GameManager.Instance.bordelTemplate, a);
                    }
                }
            }
        }

        Debug.Log($"[EnemyManager] Nepřítel položil '{cardData.cardName}' na slot {slotIndex}.");
    }

    /// <summary>Odstraní kartu ze slotu (např. když ji hráč zničí).</summary>
    public void RemoveCardFromSlot(int slotIndex)
    {
        if (occupiedSlots.ContainsKey(slotIndex))
        {
            Card card = occupiedSlots[slotIndex];
            occupiedSlots.Remove(slotIndex);
            StartCoroutine(card.DestroyAnimation());
        }
    }

    /// <summary>Odebere kartu ze slotu BEZ destroy animace (pro přesuny / Hook).</summary>
    public Card TakeCardFromSlot(int slotIndex)
    {
        if (!occupiedSlots.ContainsKey(slotIndex)) return null;
        Card card = occupiedSlots[slotIndex];
        occupiedSlots.Remove(slotIndex);
        return card;
    }

    /// <summary>Vytvoří kartu přímo na slot (pro Překvápko apod.).</summary>
    public Coroutine SpawnCardOnSlot(CardData cardData, int slotIndex)
    {
        GameObject cardObj = Instantiate(cardPrefab, enemyFieldSlots[slotIndex].position + Vector3.up * 2f, cardPrefab.transform.rotation);
        Card card = cardObj.GetComponent<Card>();
        if (card == null) card = cardObj.AddComponent<Card>();
        card.Setup(cardData);
        occupiedSlots[slotIndex] = card;
        return StartCoroutine(SpawnCardOnSlotCoroutine(card, slotIndex));
    }

    IEnumerator SpawnCardOnSlotCoroutine(Card card, int slotIndex)
    {
        yield return card.MoveToPosition(enemyFieldSlots[slotIndex].position, dealDuration);
        Quaternion slotRot = enemyFieldSlots[slotIndex].rotation * Quaternion.Euler(0f, 0f, cardPrefab.transform.rotation.eulerAngles.z);
        card.SetBasePositionAndRotation(enemyFieldSlots[slotIndex].position, slotRot, true);
    }

    /// <summary>Přesune kartu z jednoho slotu na druhý.</summary>
    public Coroutine MoveCardBetweenSlots(int fromSlot, int toSlot)
    {
        if (!occupiedSlots.ContainsKey(fromSlot) || occupiedSlots.ContainsKey(toSlot)) return null;
        Card card = occupiedSlots[fromSlot];
        occupiedSlots.Remove(fromSlot);
        occupiedSlots[toSlot] = card;
        return StartCoroutine(SpawnCardOnSlotCoroutine(card, toSlot));
    }
}
