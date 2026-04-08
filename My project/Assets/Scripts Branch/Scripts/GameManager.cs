using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Stavy hry – řídí průběh kola.
/// </summary>
public enum GameState
{
    GameStart,
    PlayerDraw,
    PlayerPlace,
    PlayerActions,
    EnemyTurn
}

/// <summary>
/// Centrální řízení průběhu hry. Spravuje stavy a přechodí mezi nimi.
///
/// NASTAVENÍ:
/// 1. Vytvoř prázdný GO "GameManager" → přidej tento skript.
/// 2. Přetáhni DeckManager do "Player Deck".
/// 3. Přetáhni EnemyManager do "Enemy Manager".
/// 4. Přiřaď TMP_Text pro životy hráče a nepřítele.
/// </summary>

public class GameManager : MonoBehaviour
{

    // ...existing fields and methods...

    /// <summary>
    /// Aktualizuje zobrazení životů hráče a nepřítele v UI.
    /// </summary>
    void UpdateHealthUI()
    {
        if (playerHealthText != null)
            playerHealthText.text = PlayerHealth.ToString();
        if (enemyHealthText != null)
            enemyHealthText.text = EnemyHealth.ToString();
    }

    void SetState(GameState newState)
    {
        CurrentState = newState;
        UpdatePhaseUI();
    }

    void UpdatePhaseUI()
    {
        if (phaseText == null) return;

        string roundPrefix = currentRound > 0 ? $"Kolo {currentRound} – " : "";

        switch (CurrentState)
        {
            case GameState.GameStart:
                phaseText.text = "Hra začíná...";
                break;
            case GameState.PlayerDraw:
                phaseText.text = roundPrefix + "Lízni kartu z balíčku";
                break;
            case GameState.PlayerPlace:
                phaseText.text = roundPrefix + "Polož karty (Space = konec)";
                break;
            case GameState.PlayerActions:
                phaseText.text = roundPrefix + "Útok!";
                break;
            case GameState.EnemyTurn:
                phaseText.text = roundPrefix + "Tah nepřítele...";
                break;
        }
    }
    public static GameManager Instance { get; private set; }

    [Header("Reference")]
    [Tooltip("Hráčův DeckManager")]
    public DeckManager playerDeck;

    [Tooltip("EnemyManager")]
    public EnemyManager enemyManager;

    [Header("Šablony karet")]
    [Tooltip("CardData pro Raketku (1 DMG, 1 HP) – používá Překvápko affix")]
    public CardData raketkaTemplate;

    [Header("Životy")]
    [Tooltip("Životy hráče na začátku")]
    public int playerMaxHealth = 20;

    [Tooltip("Životy nepřítele na začátku")]
    public int enemyMaxHealth = 20;

    [Tooltip("TMP text pro zobrazení životů hráče")]
    public TMP_Text playerHealthText;

    [Tooltip("TMP text pro zobrazení životů nepřítele")]
    public TMP_Text enemyHealthText;

    [Header("Fáze / kolo")]
    [Tooltip("TMP text zobrazující aktuální fázi hry (co má hráč dělat)")]
    public TMP_Text phaseText;

    [Tooltip("Číslo aktuálního kola")]
    public int currentRound { get; private set; }

    [Header("Nastavení")]
    [Tooltip("Prodleva mezi útoky karet (v sekundách)")]
    public float actionDelay = 0.5f;

    public GameState CurrentState { get; private set; }
    public int PlayerHealth { get; private set; }
    public int EnemyHealth { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        // ═══ GAME START ═══
        SetState(GameState.GameStart);
        Debug.Log("[GameManager] ══ Hra začíná! ══");

        currentRound = 0;
        PlayerHealth = playerMaxHealth;
        EnemyHealth = enemyMaxHealth;
        UpdateHealthUI();

        playerDeck.InitializeDeck();
        enemyManager.Initialize();

        yield return playerDeck.DealStartingCardsRoutine();
        yield return enemyManager.DealStartingCardsRoutine();

        // ═══ HERNÍ LOOP ═══
        while (true)
        {
            // --- Hráč lízá kartu ---
            currentRound++;
            SetState(GameState.PlayerDraw);
            playerDeck.drawDone = false;
            Debug.Log("[GameManager] → Hráč táhne kartu z balíčku");

            yield return new WaitUntil(() => playerDeck.drawDone && !playerDeck.IsBusy);

            // --- Hráč pokládá karty (může položit více, ukončí mezerníkem) ---
            SetState(GameState.PlayerPlace);
            playerDeck.turnEnded = false;
            Debug.Log("[GameManager] → Hráč pokládá karty (Space = konec tahu)");

            yield return new WaitUntil(() => playerDeck.turnEnded && !playerDeck.IsBusy);

            // --- Akce hráče ---
            SetState(GameState.PlayerActions);
            Debug.Log("[GameManager] → Zpracování akcí hráče");
            yield return ProcessPlayerActions();

            // --- Tah nepřítele ---
            SetState(GameState.EnemyTurn);
            Debug.Log("[GameManager] → Nepřítelův tah");
            yield return enemyManager.DoFullTurn();

            Debug.Log("[GameManager] ══ Kolo dokončeno ══");
        }
    }

    IEnumerator ProcessPlayerActions()
    {
        Debug.Log("[GameManager] Hráčovy karty útočí...");

        int slotCount = Mathf.Min(playerDeck.SlotCount, enemyManager.enemyFieldSlots.Length);

        for (int i = 0; i < slotCount; i++)
        {
            Card playerCard = playerDeck.GetCardAtSlot(i);
            if (playerCard == null) continue;

            // Stunned karty neútočí
            if (playerCard.isStunned)
            {
                Debug.Log($"[GameManager] '{playerCard.data.cardName}' je omráčená – přeskakuje útok.");
                continue;
            }

            // Boxer: odstrčí pravou přilehlou enemy kartu na poslední volnou pozici vpravo
            if (playerCard.affixes.Exists(a => a is AffixBoxer))
            {
                int rightSlot = i + 1;
                if (rightSlot < enemyManager.enemyFieldSlots.Length && enemyManager.IsSlotOccupied(rightSlot))
                {
                    int lastFree = -1;
                    for (int s = enemyManager.enemyFieldSlots.Length - 1; s > rightSlot; s--)
                    {
                        if (!enemyManager.IsSlotOccupied(s)) { lastFree = s; break; }
                    }
                    if (lastFree >= 0)
                    {
                        Debug.Log($"[GameManager] '{playerCard.data.cardName}' (Boxer) odstrčil enemy kartu ze slotu {rightSlot} na {lastFree}");
                        yield return enemyManager.MoveCardBetweenSlots(rightSlot, lastFree);
                    }
                }
            }

            bool zpatecka = playerCard.affixes.Exists(a => a is AffixZpatecka);
            bool vidlicka = playerCard.affixes.Exists(a => a is AffixVidlicka);

            if (zpatecka)
            {
                int targetSlot = i + 1;
                Card rightEnemy = (targetSlot < enemyManager.enemyFieldSlots.Length) ? enemyManager.GetCardAtSlot(targetSlot) : null;
                if (rightEnemy != null)
                {
                    yield return PlayerAttackEnemyCard(playerCard, rightEnemy, targetSlot);
                    if (EnemyHealth <= 0) yield break;
                }
            }
            else if (vidlicka)
            {
                int leftSlot = i - 1;
                int rightSlot = i + 1;
                bool hit = false;
                if (leftSlot >= 0)
                {
                    Card leftEnemy = enemyManager.GetCardAtSlot(leftSlot);
                    if (leftEnemy != null)
                    {
                        yield return PlayerAttackEnemyCard(playerCard, leftEnemy, leftSlot);
                        if (EnemyHealth <= 0) yield break;
                        hit = true;
                    }
                }
                if (rightSlot < enemyManager.enemyFieldSlots.Length)
                {
                    Card rightEnemy = enemyManager.GetCardAtSlot(rightSlot);
                    if (rightEnemy != null)
                    {
                        yield return PlayerAttackEnemyCard(playerCard, rightEnemy, rightSlot);
                        if (EnemyHealth <= 0) yield break;
                        hit = true;
                    }
                }
                if (!hit)
                    Debug.Log($"[GameManager] '{playerCard.data.cardName}' (Vidlička) nemá koho zasáhnout.");
            }
            else
            {
                Card enemyCard = enemyManager.GetCardAtSlot(i);
                if (enemyCard != null)
                {
                    yield return PlayerAttackEnemyCard(playerCard, enemyCard, i);
                    if (EnemyHealth <= 0) yield break;
                }
                else
                {
                    // Žádná enemy karta → dmg nepříteli (násobený podle % ztracených HP)
                    float missingPercent = 1f - (float)PlayerHealth / playerMaxHealth;
                    float multiplier = 1f + missingPercent * 9f;
                    int totalDmg = Mathf.RoundToInt(playerCard.currentDamage * multiplier);
                    Debug.Log($"[GameManager] '{playerCard.data.cardName}' dává {playerCard.currentDamage}x{multiplier:F1} = {totalDmg} dmg nepříteli!");
                    yield return playerCard.AttackAnimation(enemyManager.enemyFieldSlots[i].position, 0.35f, 1.2f);
                    EnemyHealth -= totalDmg;
                    UpdateHealthUI();
                    if (EnemyHealth <= 0)
                        yield break;
                }
            }

            yield return new WaitForSeconds(actionDelay);
        }

        Debug.Log("[GameManager] Akce hráče zpracovány.");
        ClearTurnEffects(true);
    }

    /// <summary>Hráčova karta útočí na konkrétní enemy kartu (obsahuje Prasklina + Překvápko logiku).</summary>
    IEnumerator PlayerAttackEnemyCard(Card attacker, Card defender, int defenderSlot)
    {
        // Prasklina: útok prochází skrz, dmg jde přímo nepříteli
        bool prasklina = defender.affixes.Exists(a => a is AffixPrasklina);
        if (prasklina)
        {
            Debug.Log($"[GameManager] '{attacker.data.cardName}' útočí skrz '{defender.data.cardName}' (Prasklina) – dmg jde nepříteli!");
            yield return attacker.AttackAnimation(defender.transform.position, 0.35f, 1.2f);
            EnemyHealth -= attacker.currentDamage;
            UpdateHealthUI();
            yield break;
        }

        Debug.Log($"[GameManager] '{attacker.data.cardName}' (dmg {attacker.currentDamage}) útočí na '{defender.data.cardName}' (hp {defender.currentHealth})");
        yield return attacker.AttackAnimation(defender.transform.position, 0.35f, 1.2f);
        bool died = defender.TakeDamage(attacker.currentDamage);
        int afterHP = defender.currentHealth;
        if (died)
        {
            bool hasPrekvapko = defender.affixes.Exists(a => a is AffixPrekvapko);
            Debug.Log($"[GameManager] '{defender.data.cardName}' zničena!");
            enemyManager.RemoveCardFromSlot(defenderSlot);
            int overflow = -(afterHP);
            if (overflow > 0)
            {
                Debug.Log($"[GameManager] Zbylý damage {overflow} projde nepříteli!");
                EnemyHealth -= overflow;
                UpdateHealthUI();
            }
            // Překvápko: spawn Raketka na místě zničené karty
            if (hasPrekvapko && raketkaTemplate != null && !enemyManager.IsSlotOccupied(defenderSlot))
            {
                Debug.Log($"[GameManager] Překvápko! Na slotu {defenderSlot} se objevuje Raketka!");
                yield return enemyManager.SpawnCardOnSlot(raketkaTemplate, defenderSlot);
            }
        }
    }

    /// <summary>Enemy karty útočí na hráče (voláno z EnemyManager).</summary>
    public IEnumerator ProcessEnemyAttacks()
    {
        Debug.Log("[GameManager] Enemy karty útočí...");

        int slotCount = Mathf.Min(playerDeck.SlotCount, enemyManager.enemyFieldSlots.Length);

        for (int i = 0; i < slotCount; i++)
        {
            Card enemyCard = enemyManager.GetCardAtSlot(i);
            if (enemyCard == null) continue;

            // Stunned karty neútočí
            if (enemyCard.isStunned)
            {
                Debug.Log($"[GameManager] Enemy '{enemyCard.data.cardName}' je omráčená – přeskakuje útok.");
                continue;
            }

            // Boxer: odstrčí pravou přilehlou player kartu na poslední volnou pozici vpravo
            if (enemyCard.affixes.Exists(a => a is AffixBoxer))
            {
                int rightSlot = i + 1;
                if (rightSlot < playerDeck.SlotCount && playerDeck.IsSlotOccupied(rightSlot))
                {
                    int lastFree = -1;
                    for (int s = playerDeck.SlotCount - 1; s > rightSlot; s--)
                    {
                        if (!playerDeck.IsSlotOccupied(s)) { lastFree = s; break; }
                    }
                    if (lastFree >= 0)
                    {
                        Debug.Log($"[GameManager] Enemy '{enemyCard.data.cardName}' (Boxer) odstrčil player kartu ze slotu {rightSlot} na {lastFree}");
                        yield return playerDeck.MoveCardBetweenSlots(rightSlot, lastFree);
                    }
                }
            }

            bool zpatecka = enemyCard.affixes.Exists(a => a is AffixZpatecka);
            bool vidlicka = enemyCard.affixes.Exists(a => a is AffixVidlicka);

            if (zpatecka)
            {
                int targetSlot = i + 1;
                Card rightPlayer = (targetSlot < playerDeck.SlotCount) ? playerDeck.GetCardAtSlot(targetSlot) : null;
                if (rightPlayer != null)
                {
                    yield return EnemyAttackPlayerCard(enemyCard, rightPlayer, targetSlot);
                    if (PlayerHealth <= 0) yield break;
                }
            }
            else if (vidlicka)
            {
                int leftSlot = i - 1;
                int rightSlot = i + 1;
                bool hit = false;
                if (leftSlot >= 0)
                {
                    Card leftPlayer = playerDeck.GetCardAtSlot(leftSlot);
                    if (leftPlayer != null)
                    {
                        yield return EnemyAttackPlayerCard(enemyCard, leftPlayer, leftSlot);
                        if (PlayerHealth <= 0) yield break;
                        hit = true;
                    }
                }
                if (rightSlot < playerDeck.SlotCount)
                {
                    Card rightPlayer = playerDeck.GetCardAtSlot(rightSlot);
                    if (rightPlayer != null)
                    {
                        yield return EnemyAttackPlayerCard(enemyCard, rightPlayer, rightSlot);
                        if (PlayerHealth <= 0) yield break;
                        hit = true;
                    }
                }
                if (!hit)
                    Debug.Log($"[GameManager] Enemy '{enemyCard.data.cardName}' (Vidlička) nemá koho zasáhnout.");
            }
            else
            {
                Card playerCard = playerDeck.GetCardAtSlot(i);
                if (playerCard != null)
                {
                    yield return EnemyAttackPlayerCard(enemyCard, playerCard, i);
                    if (PlayerHealth <= 0) yield break;
                }
                else
                {
                    Debug.Log($"[GameManager] Enemy '{enemyCard.data.cardName}' dává {enemyCard.currentDamage} dmg hráči!");
                    yield return enemyCard.AttackAnimation(playerDeck.fieldSlots[i].position, 0.35f, 1.2f);
                    PlayerHealth -= enemyCard.currentDamage;
                    UpdateHealthUI();
                    if (PlayerHealth <= 0)
                    {
                        Debug.Log("[GameManager] Hráč poražen! GAME OVER!");
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(actionDelay);
        }

        Debug.Log("[GameManager] Akce nepřítele zpracovány.");
        ClearTurnEffects(false);
    }

    /// <summary>Enemy karta útočí na konkrétní hráčovu kartu (obsahuje Prasklina + Překvápko logiku).</summary>
    IEnumerator EnemyAttackPlayerCard(Card attacker, Card defender, int defenderSlot)
    {
        // Prasklina: útok prochází skrz, dmg jde přímo hráči
        bool prasklina = defender.affixes.Exists(a => a is AffixPrasklina);
        if (prasklina)
        {
            Debug.Log($"[GameManager] Enemy '{attacker.data.cardName}' útočí skrz '{defender.data.cardName}' (Prasklina) – dmg jde hráči!");
            yield return attacker.AttackAnimation(defender.transform.position, 0.35f, 1.2f);
            PlayerHealth -= attacker.currentDamage;
            UpdateHealthUI();
            yield break;
        }

        Debug.Log($"[GameManager] Enemy '{attacker.data.cardName}' (dmg {attacker.currentDamage}) útočí na '{defender.data.cardName}' (hp {defender.currentHealth})");
        yield return attacker.AttackAnimation(defender.transform.position, 0.35f, 1.2f);
        bool died = defender.TakeDamage(attacker.currentDamage);
        int afterHP = defender.currentHealth;
        if (died)
        {
            bool hasPrekvapko = defender.affixes.Exists(a => a is AffixPrekvapko);
            Debug.Log($"[GameManager] '{defender.data.cardName}' zničena!");
            playerDeck.RemoveCardFromSlot(defenderSlot);
            int overflow = -(afterHP);
            if (overflow > 0)
            {
                Debug.Log($"[GameManager] Zbylý damage {overflow} projde hráči!");
                PlayerHealth -= overflow;
                UpdateHealthUI();
            }
            // Překvápko: spawn Raketka na místě zničené karty
            if (hasPrekvapko && raketkaTemplate != null && !playerDeck.IsSlotOccupied(defenderSlot))
            {
                Debug.Log($"[GameManager] Překvápko! Na slotu {defenderSlot} se objevuje Raketka!");
                yield return playerDeck.SpawnCardOnSlot(raketkaTemplate, defenderSlot);
            }
        }
    }

    /// <summary>Vyčistí stun a shield na kartách dané strany (po útočné fázi).</summary>
    void ClearTurnEffects(bool playerSide)
    {
        if (playerSide)
        {
            for (int i = 0; i < playerDeck.SlotCount; i++)
            {
                Card c = playerDeck.GetCardAtSlot(i);
                if (c != null) c.ClearEffects();
            }
        }
        else
        {
            for (int i = 0; i < enemyManager.enemyFieldSlots.Length; i++)
            {
                Card c = enemyManager.GetCardAtSlot(i);
                if (c != null) c.ClearEffects();
            }
        }
    }
// END OF CLASS
}

