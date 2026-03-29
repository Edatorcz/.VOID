using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spravuje balíček karet, rozdání na začátku, tahání z balíčku kliknutím a umísťování na sloty.
///
/// NASTAVENÍ V EDITORU:
/// 1. Přidej tento skript na prázdný GameObject (např. "DeckManager").
/// 2. Vytvoř 5× CardData: Assets > Create > CardGame > Card Data → přetáhni do "Deck Cards".
/// 3. Vytvoř Card prefab (Sprite nebo Quad), přidej komponentu Card → přetáhni do "Card Prefab".
/// 4. Vytvoř prázdný GO "DeckPile" na levé/pravé straně scény → přetáhni do "Deck Pile Transform".
///    → DeckPile musí mít COLLIDER (BoxCollider / BoxCollider2D) pro detekci kliknutí!
/// 5. Vytvoř prázdné GO "FieldSlot0", "FieldSlot1" ... uprostřed/hrací plochy → přetáhni do "Field Slots".
///    → Každý slot musí mít COLLIDER pro detekci kliknutí!
/// 6. Vytvoř prázdný GO "CardPreview" před kamerou (blízko kamery, viditelný) → přetáhni do "Card Preview Transform".
/// </summary>
public class DeckManager : MonoBehaviour
{

    /// <summary>
    /// Projde všechny sloty a aplikuje dynamické efekty affixů (např. Medic, Bojovník) na sousední karty.
    /// </summary>
    public void UpdateDynamicAffixes()
    {

        // Nejprve resetuj bonusy (např. currentHealth, currentDamage) na základní hodnoty
        for (int i = 0; i < fieldSlots.Length; i++)
        {
            if (occupiedSlots.TryGetValue(i, out Card card) && card != null)
            {
                card.currentHealth = card.data.health;
                card.currentDamage = card.data.damage;
            }
        }

        // Nejprve spočítej pro každou kartu, kolik má sousedních Bojovníků
        int[] bojovnikBuffs = new int[fieldSlots.Length];
        for (int i = 0; i < fieldSlots.Length; i++)
        {
            if (!occupiedSlots.TryGetValue(i, out Card card) || card == null) continue;
            // Levý soused
            if (occupiedSlots.TryGetValue(i - 1, out Card left) && left != null)
            {
                foreach (var affix in left.affixes)
                {
                    if (affix is AffixBojovnik)
                        bojovnikBuffs[i]++;
                }
            }
            // Pravý soused
            if (occupiedSlots.TryGetValue(i + 1, out Card right) && right != null)
            {
                foreach (var affix in right.affixes)
                {
                    if (affix is AffixBojovnik)
                        bojovnikBuffs[i]++;
                }
            }
        }

        // Aplikuj buffy podle počtu sousedních Bojovníků
        for (int i = 0; i < fieldSlots.Length; i++)
        {
            if (occupiedSlots.TryGetValue(i, out Card card) && card != null)
            {
                for (int b = 0; b < bojovnikBuffs[i]; b++)
                    card.currentDamage *= 2;
            }
        }

        // Medic: přilehlým kartám +1 HP (původní logika)
        for (int i = 0; i < fieldSlots.Length; i++)
        {
            if (!occupiedSlots.TryGetValue(i, out Card card) || card == null) continue;
            foreach (var affix in card.affixes)
            {
                if (affix == null) continue;
                if (affix is AffixMedic)
                {
                    if (occupiedSlots.TryGetValue(i - 1, out Card left) && left != null)
                        left.currentHealth += 1;
                    if (occupiedSlots.TryGetValue(i + 1, out Card right) && right != null)
                        right.currentHealth += 1;
                }
            }
        }

        // Po změně statů aktualizuj zobrazení
        for (int i = 0; i < fieldSlots.Length; i++)
        {
            if (occupiedSlots.TryGetValue(i, out Card card) && card != null)
            {
                card.UpdateStatTexts();
            }
        }
    }
    [Header("Deck Setup")]
    [Tooltip("Všechny karty v balíčku (CardData ScriptableObjects)")]
    public List<CardData> deckCards = new List<CardData>();

    [Tooltip("Prefab karty s komponentou Card a SpriteRenderer/MeshRenderer")]
    public GameObject cardPrefab;

    [Header("Pozice")]
    [Tooltip("Transform balíčku (na boku scény) – kliknutím se tahá karta")]
    public Transform deckPileTransform;

    [Tooltip("Sloty na hrací ploše, kam se karty umísťují")]
    public Transform[] fieldSlots;

    [Tooltip("Pozice před kamerou, kam se ukáže vytažená karta")]
    public Transform cardPreviewTransform;

    [Header("Kamera")]
    [Tooltip("GameCameraController na kameře (pro přesun kamery nad desku)")]
    public GameCameraController cameraController;

    [Header("Reference")]
    [Tooltip("Odkaz na EnemyManager (pro inspect nepřátelských karet)")]
    public EnemyManager enemyManager;

    [Header("Nastavení")]
    [Tooltip("Prodleva mezi rozdáním každé karty (v sekundách)")]
    public float dealDelay = 0.4f;

    [Tooltip("Jak dlouho trvá přesun karty (v sekundách)")]
    public float dealDuration = 0.5f;

    [Tooltip("Zničené karty se vrátí zpět do balíčku")]
    public bool recycleDestroyedCards = false;

    [Tooltip("Zdvojnásob všechny karty v balíčku")]
    public bool doubleDeck = false;

    [Header("Start")]
    [Tooltip("Kolik karet se položí na hrací pole na začátku")]
    public int startFieldCards = 2;

    [Header("Ruka")]
    [Tooltip("Střed ruky (prázdný GO před kamerou dole)")]
    public Transform handAnchor;

    [Tooltip("Kolik karet se lízne do ruky na začátku hry")]
    public int startingHandSize = 5;

    [Tooltip("Maximální počet karet v ruce")]
    public int maxHandSize = 7;

    [Tooltip("Rozestup karet v ruce")]
    public float handSpacing = 0.6f;

    [Tooltip("Výška oblouku ruky (vnější karty klesají)")]
    public float handArcHeight = 0.15f;

    [Tooltip("Maximální úhel naklonění vnějších karet (stupně)")]
    public float handMaxAngle = 15f;

    [Tooltip("Jak rychle se karty v ruce přeuspořádají")]
    public float handArrangeSpeed = 8f;

    private List<CardData> remainingDeck = new List<CardData>();
    private Dictionary<int, Card> occupiedSlots = new Dictionary<int, Card>();

    private List<Card> handCards = new List<Card>();
    private Card selectedCard;
    private Card inspectedCard;
    private bool inspectedFromField;
    private bool isDrawing;
    private bool isDealingStartCards;
    private bool isBusy;

    [HideInInspector] public bool cardPlacedThisTurn;
    [HideInInspector] public bool drawDone;
    [HideInInspector] public bool turnEnded;

    /// <summary>Karta vybraná z ruky (čeká na umístění).</summary>
    public Card SelectedCard => selectedCard;

    /// <summary>Probíhá animace / akce?</summary>
    public bool IsBusy => isBusy;

    void Start()
    {
        // Inicializaci a rozdávání řídí GameManager
    }

    void Update()
    {
        if (isDealingStartCards) return;

        GameState state = GameManager.Instance != null
            ? GameManager.Instance.CurrentState
            : GameState.PlayerDraw;

        bool isPlayerPhase = state == GameState.PlayerDraw || state == GameState.PlayerPlace;

        // Hover – povoleno během tahu hráče
        if (!isBusy && isPlayerPhase)
            CheckHover();

        if (isBusy) return;

        if (state == GameState.PlayerDraw)
        {
            if (Input.GetMouseButtonDown(0))
                HandleDrawPhaseClick();
            // Pravé tlačítko – prohlédnout kartu v ruce
            if (Input.GetMouseButtonDown(1))
                HandleInspectClick();
        }
        else if (state == GameState.PlayerPlace)
        {
            if (Input.GetMouseButtonDown(0))
                HandlePlacePhaseClick();
            // Pravé tlačítko – prohlédnout kartu v ruce
            if (Input.GetMouseButtonDown(1))
                HandleInspectClick();
            // Mezer – ukončit tah
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (selectedCard != null) CancelSelection();
                turnEnded = true;
                Debug.Log("[DeckManager] Hráč ukončil tah.");
            }
        }
    }

    void CheckHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 3D raycast
        if (Physics.Raycast(ray, out RaycastHit hit3D))
        {
            Card card = hit3D.collider.GetComponent<Card>();
            if (card == null) card = hit3D.collider.GetComponentInParent<Card>();
            if (card != null) card.SetHovered();
        }

        // 2D raycast
        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
        if (hit2D.collider != null)
        {
            Card card = hit2D.collider.GetComponent<Card>();
            if (card == null) card = hit2D.collider.GetComponentInParent<Card>();
            if (card != null) card.SetHovered();
        }
    }

    void HandleDrawPhaseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        GameObject clickedObj = null;

        if (Physics.Raycast(ray, out RaycastHit hit3D))
            clickedObj = hit3D.collider.gameObject;
        else
        {
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
            if (hit2D.collider != null)
                clickedObj = hit2D.collider.gameObject;
        }

        if (clickedObj == null) return;

        // Klik na balíček → lízni kartu
        if (clickedObj.transform == deckPileTransform)
        {
            DrawCard();
            return;
        }

        // Klik na kartu v ruce → vyber ji
        Card clickedCard = clickedObj.GetComponent<Card>();
        if (clickedCard == null) clickedCard = clickedObj.GetComponentInParent<Card>();
        if (clickedCard != null && IsCardInHand(clickedCard))
        {
            SelectFromHand(clickedCard);
            return;
        }
    }

    void HandlePlacePhaseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        GameObject clickedObj = null;

        if (Physics.Raycast(ray, out RaycastHit hit3D))
            clickedObj = hit3D.collider.gameObject;
        else
        {
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
            if (hit2D.collider != null)
                clickedObj = hit2D.collider.gameObject;
        }

        if (clickedObj == null) return;

        // Levý klik na kartu v ruce → vyber ji pro položení
        Card clickedCard = clickedObj.GetComponent<Card>();
        if (clickedCard == null) clickedCard = clickedObj.GetComponentInParent<Card>();
        if (clickedCard != null && IsCardInHand(clickedCard))
        {
            SelectFromHand(clickedCard);
            return;
        }

        // Levý klik na kartu na poli (s vybranou kartou) → zkus ji zničit
        if (selectedCard != null && clickedCard != null && !IsCardInHand(clickedCard))
        {
            // Najdi slot, na kterém karta sedí
            foreach (var kvp in occupiedSlots)
            {
                if (kvp.Value == clickedCard)
                {
                    TryPlaceCard(kvp.Key);
                    return;
                }
            }
        }

        // Levý klik na slot (s vybranou kartou) → polož ji
        if (selectedCard != null)
        {
            for (int i = 0; i < fieldSlots.Length; i++)
            {
                if (clickedObj.transform == fieldSlots[i])
                {
                    TryPlaceCard(i);
                    return;
                }
            }
        }
    }

    /// <summary>Pravý klik – highlight/inspect kartu z ruky (přiletí na preview, další pravý klik ji vrátí).</summary>
    void HandleInspectClick()
    {
        // Pokud už něco inspektuji, vrať to zpět
        if (inspectedCard != null)
        {
            ReturnInspectedCard();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        GameObject clickedObj = null;

        if (Physics.Raycast(ray, out RaycastHit hit3D))
            clickedObj = hit3D.collider.gameObject;
        else
        {
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
            if (hit2D.collider != null)
                clickedObj = hit2D.collider.gameObject;
        }

        if (clickedObj == null) return;

        Card clickedCard = clickedObj.GetComponent<Card>();
        if (clickedCard == null) clickedCard = clickedObj.GetComponentInParent<Card>();

        // Pokud raycast trefil slot (ne kartu), najdi kartu na tom slotu
        if (clickedCard == null)
        {
            for (int i = 0; i < fieldSlots.Length; i++)
            {
                if (clickedObj.transform == fieldSlots[i] && occupiedSlots.ContainsKey(i))
                {
                    clickedCard = occupiedSlots[i];
                    break;
                }
            }
        }

        // Zkus i enemy sloty
        if (clickedCard == null && enemyManager != null)
        {
            for (int i = 0; i < enemyManager.enemyFieldSlots.Length; i++)
            {
                if (clickedObj.transform == enemyManager.enemyFieldSlots[i] && enemyManager.IsSlotOccupied(i))
                {
                    clickedCard = enemyManager.GetCardAtSlot(i);
                    break;
                }
            }
        }

        if (clickedCard != null)
        {
            InspectCard(clickedCard, !IsCardInHand(clickedCard));
        }
    }

    void InspectCard(Card card, bool fromField = false)
    {
        if (card == selectedCard) return; // vybraná karta se neinspektuje
        inspectedCard = card;
        inspectedFromField = fromField;
        card.ClearHover();
        StartCoroutine(InspectCoroutine(card));
        Debug.Log($"[DeckManager] Prohlížení karty '{card.data.cardName}'.");
    }

    IEnumerator InspectCoroutine(Card card)
    {
        isBusy = true;
        yield return card.MoveToPosition(cardPreviewTransform.position, dealDuration, 1f);
        // Nastaví rotaci přesně podle cardPreviewTransform, ale zachová Z z prefabu
        Vector3 previewEuler = cardPreviewTransform.rotation.eulerAngles;
        previewEuler.z = cardPrefab.transform.rotation.eulerAngles.z;
        Quaternion previewRot = Quaternion.Euler(previewEuler);
        card.SetBasePositionAndRotation(cardPreviewTransform.position, previewRot);
        isBusy = false;
    }

    void ReturnInspectedCard()
    {
        if (inspectedFromField)
        {
            bool found = false;

            // Zkus hráčovy sloty
            foreach (var kvp in occupiedSlots)
            {
                if (kvp.Value == inspectedCard)
                {
                    Quaternion slotRot = fieldSlots[kvp.Key].rotation * Quaternion.Euler(0f, 0f, cardPrefab.transform.rotation.eulerAngles.z);
                    inspectedCard.SetBasePositionAndRotation(
                        fieldSlots[kvp.Key].position,
                        slotRot,
                        true);
                    found = true;
                    break;
                }
            }

            // Zkus enemy sloty
            if (!found && enemyManager != null)
            {
                for (int i = 0; i < enemyManager.enemyFieldSlots.Length; i++)
                {
                    if (enemyManager.GetCardAtSlot(i) == inspectedCard)
                    {
                        Quaternion slotRot = enemyManager.enemyFieldSlots[i].rotation * Quaternion.Euler(0f, 0f, cardPrefab.transform.rotation.eulerAngles.z);
                        inspectedCard.SetBasePositionAndRotation(
                            enemyManager.enemyFieldSlots[i].position,
                            slotRot,
                            true);
                        break;
                    }
                }
            }
        }

        inspectedCard.ClearHover();
        inspectedCard = null;
        inspectedFromField = false;
        ArrangeHand();
        Debug.Log("[DeckManager] Karta vrácena.");
    }

    bool IsCardInHand(Card card)
    {
        return handCards.Contains(card);
    }

    void SelectFromHand(Card card)
    {
        // Vrať inspektovanou kartu, pokud existuje
        if (inspectedCard != null)
            ReturnInspectedCard();

        // Klik na již vybranou kartu → odznač
        if (selectedCard == card)
        {
            CancelSelection();
            return;
        }

        // Pokud je jiná karta vybraná, odznač ji
        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
        }

        if (handCards.Contains(card))
        {
            selectedCard = card;
            card.SetSelected(true);
            Debug.Log($"[DeckManager] Vybrána karta '{card.data.cardName}' z ruky. Klikni na slot!");
        }
    }

    IEnumerator SelectFromHandCoroutine(Card card)
    {
        isBusy = true;
        yield return card.MoveToPosition(cardPreviewTransform.position, dealDuration, 1f);
        isBusy = false;
    }

    void ReturnCardToHand(Card card)
    {
        card.ClearHover();
        ArrangeHand();
    }

    // --- Dynamický fan layout ruky ---

    void CalculateHandLayout(int index, int total, out Vector3 pos, out Quaternion rot)
    {
        float centerOffset = index - (total - 1) * 0.5f;
        float maxOffset = (total - 1) * 0.5f;
        float normalizedOffset = maxOffset > 0f ? centerOffset / maxOffset : 0f;

        Vector3 right = handAnchor.right;
        Vector3 up = handAnchor.up;

        Vector3 offset = right * (centerOffset * handSpacing);

        float arc = -handArcHeight * (normalizedOffset * normalizedOffset);
        offset += up * arc;

        pos = handAnchor.position + offset;

        // Základní rotace prefabu + fan naklonění
        float angle = -handMaxAngle * normalizedOffset;
        rot = handAnchor.rotation * Quaternion.Euler(0f, 0f, cardPrefab.transform.rotation.eulerAngles.z + angle);
    }

    void ArrangeHand()
    {
        int count = 0;
        foreach (var c in handCards)
            if (c != null && c != selectedCard && c != inspectedCard) count++;

        int idx = 0;
        for (int i = 0; i < handCards.Count; i++)
        {
            Card card = handCards[i];
            if (card == null || card == selectedCard || card == inspectedCard) continue;

            CalculateHandLayout(idx, count, out Vector3 pos, out Quaternion rot);
            card.SetBasePositionAndRotation(pos, rot, false);
            idx++;
        }
    }

    public void InitializeDeck()
    {
        remainingDeck = new List<CardData>(deckCards);
        if (doubleDeck)
            remainingDeck.AddRange(deckCards);
        handCards.Clear();
        ShuffleDeck();
        Debug.Log($"[DeckManager] Balíček připraven: {remainingDeck.Count} karet.");
    }

    /// <summary>Volá GameManager – spustí rozdání počátečních karet.</summary>
    public Coroutine DealStartingCardsRoutine()
    {
        return StartCoroutine(DealStartingCards());
    }

    /// <summary>Fisher-Yates shuffle.</summary>
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
        isDealingStartCards = true;

        // 1. Rozdělej karty na hrací pole (random sloty)
        int fieldCount = Mathf.Min(startFieldCards, Mathf.Min(remainingDeck.Count, fieldSlots.Length));

        List<int> availableSlots = new List<int>();
        for (int i = 0; i < fieldSlots.Length; i++)
            availableSlots.Add(i);
        for (int i = availableSlots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (availableSlots[i], availableSlots[j]) = (availableSlots[j], availableSlots[i]);
        }

        for (int i = 0; i < fieldCount; i++)
        {
            yield return DealCardToFieldSlot(availableSlots[i]);
            yield return new WaitForSeconds(dealDelay);
        }

        // 2. Lízni karty do ruky
        int handCount = Mathf.Min(startingHandSize, Mathf.Min(remainingDeck.Count, maxHandSize));

        for (int i = 0; i < handCount; i++)
        {
            yield return DealCardToHandRoutine();
            if (i < handCount - 1)
                yield return new WaitForSeconds(dealDelay);
        }

        isDealingStartCards = false;
    }

    IEnumerator DealCardToFieldSlot(int slotIndex)
    {
        if (remainingDeck.Count == 0) yield break;

        CardData cardData = remainingDeck[0];
        remainingDeck.RemoveAt(0);

        GameObject cardObj = Instantiate(cardPrefab, deckPileTransform.position, cardPrefab.transform.rotation);
        Card card = cardObj.GetComponent<Card>();
        if (card == null)
            card = cardObj.AddComponent<Card>();

        card.Setup(cardData);
        occupiedSlots[slotIndex] = card;

        yield return card.MoveToPosition(fieldSlots[slotIndex].position, dealDuration);
        Quaternion slotRot = fieldSlots[slotIndex].rotation * Quaternion.Euler(0f, 0f, cardPrefab.transform.rotation.eulerAngles.z);
        card.SetBasePositionAndRotation(fieldSlots[slotIndex].position, slotRot, true);

        UpdateDynamicAffixes();
        Debug.Log($"[DeckManager] Karta '{cardData.cardName}' rozdána do slotu {slotIndex}.");
    }

    IEnumerator DealCardToHandRoutine()
    {
        if (remainingDeck.Count == 0) yield break;
        if (cardPrefab == null || deckPileTransform == null)
        {
            Debug.LogError("[DeckManager] Chybí Card Prefab nebo Deck Pile Transform!");
            yield break;
        }

        CardData cardData = remainingDeck[0];
        remainingDeck.RemoveAt(0);

        GameObject cardObj = Instantiate(cardPrefab, deckPileTransform.position, cardPrefab.transform.rotation);
        Card card = cardObj.GetComponent<Card>();
        if (card == null)
            card = cardObj.AddComponent<Card>();

        card.Setup(cardData);
        handCards.Add(card);

        // Spočti cílovou pozici pro novou kartu
        int idx = handCards.Count - 1;
        CalculateHandLayout(idx, handCards.Count, out Vector3 targetPos, out Quaternion targetRot);

        // Přeuspořádej zbytek ruky (ostatní se posunou plynule)
        ArrangeHand();

        yield return card.MoveToPosition(targetPos, dealDuration, 1f);
        card.SetBasePositionAndRotation(targetPos, targetRot, false);

        Debug.Log($"[DeckManager] Karta '{cardData.cardName}' líznutá do ruky.");
    }

    // --- Tahání karet z balíčku ---

    void DrawCard()
    {
        if (isDrawing) return;
        if (remainingDeck.Count == 0)
        {
            Debug.LogWarning("[DeckManager] Balíček je prázdný!");
            return;
        }
        if (handCards.Count >= maxHandSize)
        {
            Debug.LogWarning("[DeckManager] Ruka je plná!");
            return;
        }

        isDrawing = true;

        CardData cardData = remainingDeck[0];
        remainingDeck.RemoveAt(0);

        GameObject cardObj = Instantiate(cardPrefab, deckPileTransform.position, cardPrefab.transform.rotation);
        Card card = cardObj.GetComponent<Card>();

        if (card == null)
            card = cardObj.AddComponent<Card>();

        card.Setup(cardData);
        handCards.Add(card);

        StartCoroutine(DrawToHandCoroutine(card));
        Debug.Log($"[DeckManager] Líznutá karta '{cardData.cardName}' do ruky.");
    }

    IEnumerator DrawToHandCoroutine(Card card)
    {
        isBusy = true;

        int idx = handCards.IndexOf(card);
        CalculateHandLayout(idx, handCards.Count, out Vector3 targetPos, out Quaternion targetRot);

        // Přeuspořádej zbytek ruky
        ArrangeHand();

        yield return card.MoveToPosition(targetPos, dealDuration, 1f);
        card.SetBasePositionAndRotation(targetPos, targetRot, false);

        isDrawing = false;
        isBusy = false;
        drawDone = true;
    }

    void TryPlaceCard(int slotIndex)
    {
        if (selectedCard == null) return;

        // Slot je obsazený – zkus bojový mechanismus
        if (occupiedSlots.ContainsKey(slotIndex))
        {
            Card targetCard = occupiedSlots[slotIndex];

            if (CanDestroy(selectedCard.data.group, targetCard.data.group))
            {
                Card attacker = selectedCard;
                RemoveFromHand();
                cardPlacedThisTurn = true;
                StartCoroutine(DestroyCardOnSlot(attacker, targetCard, slotIndex));
            }
            else
            {
                Debug.LogWarning($"[DeckManager] {selectedCard.data.group} nemůže zničit {targetCard.data.group}!");
                StartCoroutine(selectedCard.ShakeAnimation());
            }
            return;
        }

        // Slot je volný – normální umístění
        Card card = selectedCard;
        RemoveFromHand();
        cardPlacedThisTurn = true;

        occupiedSlots[slotIndex] = card;
        StartCoroutine(PlaceCardCoroutine(card, slotIndex));
        Debug.Log($"[DeckManager] Karta '{card.data.cardName}' umístěna do slotu {slotIndex}.");
    }

    void RemoveFromHand()
    {
        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
            handCards.Remove(selectedCard);
        }
        selectedCard = null;
        ArrangeHand();
    }

    /// <summary>Může útočník (attacker) zničit oběť (target)?</summary>
    bool CanDestroy(CardGroup attacker, CardGroup target)
    {
        // Specialist může ničit specialistu
        if (attacker == CardGroup.Specialist && target == CardGroup.Specialist)
            return true;

        // Support a Sentinel ničí Scout a Striker
        bool isAttacker = attacker == CardGroup.Support || attacker == CardGroup.Sentinel;
        bool isTarget = target == CardGroup.Scout || target == CardGroup.Striker;
        return isAttacker && isTarget;
    }

    IEnumerator DestroyCardOnSlot(Card attacker, Card target, int slotIndex)
    {
        isBusy = true;
        // Ulož data zničené karty před zničením
        CardData destroyedData = target.data;

        // Vyčisti hover cílové karty
        target.ClearHover();

        // Útočník přiletí na slot
        yield return attacker.MoveToPosition(fieldSlots[slotIndex].position, dealDuration);

        Debug.Log($"[DeckManager] '{attacker.data.cardName}' ničí '{destroyedData.cardName}'!");

        // Zničí jen cílovou kartu – útočník zůstane na slotu
        StartCoroutine(target.DestroyAnimation());

        // Vrať zničenou kartu do balíčku
        if (recycleDestroyedCards && destroyedData != null)
        {
            remainingDeck.Add(destroyedData);
            Debug.Log($"[DeckManager] '{destroyedData.cardName}' vrácena do balíčku.");
        }

        // Útočník obsadí slot
        occupiedSlots[slotIndex] = attacker;
        Quaternion slotRot = fieldSlots[slotIndex].rotation * Quaternion.Euler(0f, 0f, cardPrefab.transform.rotation.eulerAngles.z);
        attacker.SetBasePositionAndRotation(fieldSlots[slotIndex].position, slotRot, true);
        isBusy = false;
    }

    IEnumerator PlaceCardCoroutine(Card card, int slotIndex)
    {
        isBusy = true;
        card.ClearHover();
        yield return card.MoveToPosition(fieldSlots[slotIndex].position, dealDuration);
        Quaternion slotRot = fieldSlots[slotIndex].rotation * Quaternion.Euler(0f, 0f, cardPrefab.transform.rotation.eulerAngles.z);
        card.SetBasePositionAndRotation(fieldSlots[slotIndex].position, slotRot, true);
        isBusy = false;
    }

    void CancelSelection()
    {
        if (selectedCard == null) return;
        selectedCard.SetSelected(false);
        selectedCard = null;
        Debug.Log("[DeckManager] Výběr karty zrušen.");
    }

    // --- Veřejné API ---

    /// <summary>Počet zbývajících karet v balíčku.</summary>
    public int RemainingCardCount => remainingDeck.Count;

    /// <summary>Je slot obsazený?</summary>
    public bool IsSlotOccupied(int slotIndex) => occupiedSlots.ContainsKey(slotIndex);

    /// <summary>Vrátí kartu na daném slotu (nebo null).</summary>
    public Card GetCardAtSlot(int slotIndex)
    {
        occupiedSlots.TryGetValue(slotIndex, out Card card);
        return card;
    }

    /// <summary>Počet hráčových slotů.</summary>
    public int SlotCount => fieldSlots.Length;

    /// <summary>Odstraní kartu ze slotu (např. když zemře).</summary>
    public void RemoveCardFromSlot(int slotIndex)
    {
        if (occupiedSlots.ContainsKey(slotIndex))
        {
            Card card = occupiedSlots[slotIndex];
            occupiedSlots.Remove(slotIndex);
            StartCoroutine(card.DestroyAnimation());
        }
    }

    /// <summary>Vloží existující kartu na konkrétní slot (pro itemy).</summary>
    public Coroutine PlaceCardDirectly(Card card, int slotIndex)
    {
        occupiedSlots[slotIndex] = card;
        return StartCoroutine(PlaceCardDirectlyCoroutine(card, slotIndex));
    }

    IEnumerator PlaceCardDirectlyCoroutine(Card card, int slotIndex)
    {
        yield return card.MoveToPosition(fieldSlots[slotIndex].position, dealDuration);
        Quaternion slotRot = fieldSlots[slotIndex].rotation * Quaternion.Euler(0f, 0f, cardPrefab.transform.rotation.eulerAngles.z);
        card.SetBasePositionAndRotation(fieldSlots[slotIndex].position, slotRot, true);
    }

    /// <summary>Přesune kartu z jednoho slotu na druhý.</summary>
    public Coroutine MoveCardBetweenSlots(int fromSlot, int toSlot)
    {
        if (!occupiedSlots.ContainsKey(fromSlot) || occupiedSlots.ContainsKey(toSlot)) return null;
        Card card = occupiedSlots[fromSlot];
        occupiedSlots.Remove(fromSlot);
        occupiedSlots[toSlot] = card;
        return StartCoroutine(PlaceCardDirectlyCoroutine(card, toSlot));
    }

    /// <summary>Odebere kartu ze slotu BEZ destroy animace (pro přesuny).</summary>
    public Card TakeCardFromSlot(int slotIndex)
    {
        if (!occupiedSlots.ContainsKey(slotIndex)) return null;
        Card card = occupiedSlots[slotIndex];
        occupiedSlots.Remove(slotIndex);
        return card;
    }

    /// <summary>Vytvoří kartu a přidá ji do ruky (pro itemy jako ScoutRocket).</summary>
    public void AddCardToHand(CardData cardData)
    {
        if (handCards.Count >= maxHandSize) return;
        GameObject cardObj = Instantiate(cardPrefab, deckPileTransform.position, cardPrefab.transform.rotation);
        Card card = cardObj.GetComponent<Card>();
        if (card == null) card = cardObj.AddComponent<Card>();
        card.Setup(cardData);
        handCards.Add(card);
        StartCoroutine(AddCardToHandCoroutine(card));
    }

    IEnumerator AddCardToHandCoroutine(Card card)
    {
        int idx = handCards.IndexOf(card);
        CalculateHandLayout(idx, handCards.Count, out Vector3 targetPos, out Quaternion targetRot);
        ArrangeHand();
        yield return card.MoveToPosition(targetPos, dealDuration, 1f);
        card.SetBasePositionAndRotation(targetPos, targetRot, false);
    }

    /// <summary>Vytvoří kartu přímo na slot (pro debris apod.).</summary>
    public Coroutine SpawnCardOnSlot(CardData cardData, int slotIndex)
    {
        GameObject cardObj = Instantiate(cardPrefab, fieldSlots[slotIndex].position + Vector3.up * 2f, cardPrefab.transform.rotation);
        Card card = cardObj.GetComponent<Card>();
        if (card == null) card = cardObj.AddComponent<Card>();
        card.Setup(cardData);
        return PlaceCardDirectly(card, slotIndex);
    }
}
