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
    public static GameManager Instance { get; private set; }

    [Header("Reference")]
    [Tooltip("Hráčův DeckManager")]
    public DeckManager playerDeck;

    [Tooltip("EnemyManager")]
    public EnemyManager enemyManager;

    [Header("Životy")]
    [Tooltip("Životy hráče na začátku")]
    public int playerMaxHealth = 20;

    [Tooltip("Životy nepřítele na začátku")]
    public int enemyMaxHealth = 20;

    [Tooltip("TMP text pro zobrazení životů hráče")]
    public TMP_Text playerHealthText;

    [Tooltip("TMP text pro zobrazení životů nepřítele")]
    public TMP_Text enemyHealthText;

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
        CurrentState = GameState.GameStart;
        Debug.Log("[GameManager] ══ Hra začíná! ══");

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
            CurrentState = GameState.PlayerDraw;
            playerDeck.drawDone = false;
            Debug.Log("[GameManager] → Hráč táhne kartu z balíčku");

            yield return new WaitUntil(() => playerDeck.drawDone && !playerDeck.IsBusy);

            // --- Hráč pokládá karty (může položit více, ukončí mezerníkem) ---
            CurrentState = GameState.PlayerPlace;
            playerDeck.turnEnded = false;
            Debug.Log("[GameManager] → Hráč pokládá karty (Space = konec tahu)");

            yield return new WaitUntil(() => playerDeck.turnEnded && !playerDeck.IsBusy);

            // --- Akce hráče ---
            CurrentState = GameState.PlayerActions;
            Debug.Log("[GameManager] → Zpracování akcí hráče");
            yield return ProcessPlayerActions();

            // --- Tah nepřítele ---
            CurrentState = GameState.EnemyTurn;
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

            bool zpatecka = playerCard.affixes.Exists(a => a is AffixZpatecka);
            bool vidlicka = playerCard.affixes.Exists(a => a is AffixVidlicka);

            if (zpatecka)
            {
                // Útočí na pravou enemy kartu (slot i+1)
                int targetSlot = i + 1;
                Card rightEnemy = (targetSlot < enemyManager.enemyFieldSlots.Length) ? enemyManager.GetCardAtSlot(targetSlot) : null;
                if (rightEnemy != null)
                {
                    Debug.Log($"[GameManager] '{playerCard.data.cardName}' (Zpátečka) útočí na pravou kartu '{rightEnemy.data.cardName}'");
                    yield return playerCard.AttackAnimation(enemyManager.enemyFieldSlots[targetSlot].position, 0.35f, 1.2f);
                    int beforeHP = rightEnemy.currentHealth;
                    bool died = rightEnemy.TakeDamage(playerCard.currentDamage);
                    int afterHP = rightEnemy.currentHealth;
                    if (died)
                    {
                        enemyManager.RemoveCardFromSlot(targetSlot);
                        int overflow = -(afterHP);
                        if (overflow > 0)
                        {
                            EnemyHealth -= overflow;
                            UpdateHealthUI();
                            if (EnemyHealth <= 0)
                                yield break;
                        }
                    }
                }
            }
            else if (vidlicka)
            {
                // Útočí pouze na karty vlevo a vpravo od protější karty (slot i-1, i+1)
                int leftSlot = i - 1;
                int rightSlot = i + 1;
                bool hit = false;
                if (leftSlot >= 0)
                {
                    Card leftEnemy = enemyManager.GetCardAtSlot(leftSlot);
                    if (leftEnemy != null)
                    {
                        Debug.Log($"[GameManager] '{playerCard.data.cardName}' (Vidlička) útočí na levou kartu '{leftEnemy.data.cardName}'");
                        yield return playerCard.AttackAnimation(enemyManager.enemyFieldSlots[leftSlot].position, 0.35f, 1.2f);
                        int beforeHP = leftEnemy.currentHealth;
                        bool died = leftEnemy.TakeDamage(playerCard.currentDamage);
                        int afterHP = leftEnemy.currentHealth;
                        if (died)
                        {
                            enemyManager.RemoveCardFromSlot(leftSlot);
                            int overflow = -(afterHP);
                            if (overflow > 0)
                            {
                                EnemyHealth -= overflow;
                                UpdateHealthUI();
                                if (EnemyHealth <= 0)
                                    yield break;
                            }
                        }
                        hit = true;
                    }
                }
                if (rightSlot < enemyManager.enemyFieldSlots.Length)
                {
                    Card rightEnemy = enemyManager.GetCardAtSlot(rightSlot);
                    if (rightEnemy != null)
                    {
                        Debug.Log($"[GameManager] '{playerCard.data.cardName}' (Vidlička) útočí na pravou kartu '{rightEnemy.data.cardName}'");
                        yield return playerCard.AttackAnimation(enemyManager.enemyFieldSlots[rightSlot].position, 0.35f, 1.2f);
                        int beforeHP = rightEnemy.currentHealth;
                        bool died = rightEnemy.TakeDamage(playerCard.currentDamage);
                        int afterHP = rightEnemy.currentHealth;
                        if (died)
                        {
                            enemyManager.RemoveCardFromSlot(rightSlot);
                            int overflow = -(afterHP);
                            if (overflow > 0)
                            {
                                EnemyHealth -= overflow;
                                UpdateHealthUI();
                                if (EnemyHealth <= 0)
                                    yield break;
                            }
                        }
                        hit = true;
                    }
                }
                if (!hit)
                {
                    Debug.Log($"[GameManager] '{playerCard.data.cardName}' (Vidlička) nemá koho zasáhnout.");
                }
            }
            else
            {
                // Standardní útok na protější kartu
                Card enemyCard = enemyManager.GetCardAtSlot(i);
                if (enemyCard != null)
                {
                    Debug.Log($"[GameManager] '{playerCard.data.cardName}' (dmg {playerCard.currentDamage}) útočí na '{enemyCard.data.cardName}' (hp {enemyCard.currentHealth})");
                    yield return playerCard.AttackAnimation(enemyCard.transform.position, 0.35f, 1.2f);
                    int beforeHP = enemyCard.currentHealth;
                    bool died = enemyCard.TakeDamage(playerCard.currentDamage);
                    int afterHP = enemyCard.currentHealth;
                    if (died)
                    {
                        Debug.Log($"[GameManager] '{enemyCard.data.cardName}' zničena!");
                        enemyManager.RemoveCardFromSlot(i);
                        int overflow = -(afterHP); // afterHP je záporné nebo nula
                        if (overflow > 0)
                        {
                            Debug.Log($"[GameManager] Zbylý damage {overflow} projde nepříteli!");
                            EnemyHealth -= overflow;
                            UpdateHealthUI();
                            if (EnemyHealth <= 0)
                                yield break;
                        }
                    }
                }
                else
                {
                    // Žádná enemy karta → dmg nepříteli (násobený podle % ztracených HP)
                    float missingPercent = 1f - (float)PlayerHealth / playerMaxHealth; // 0 na full, 1 na smrti
                    float multiplier = 1f + missingPercent * 9f; // 1x na full HP, 10x na 0 HP
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

        // Vyčisti jednorázové efekty na hráčových kartách (stun, shield)
        ClearTurnEffects(true);
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

            bool zpatecka = enemyCard.affixes.Exists(a => a is AffixZpatecka);
            bool vidlicka = enemyCard.affixes.Exists(a => a is AffixVidlicka);

            if (zpatecka)
            {
                // Útočí na pravou hráčovu kartu (slot i+1)
                int targetSlot = i + 1;
                Card rightPlayer = (targetSlot < playerDeck.SlotCount) ? playerDeck.GetCardAtSlot(targetSlot) : null;
                if (rightPlayer != null)
                {
                    Debug.Log($"[GameManager] Enemy '{enemyCard.data.cardName}' (Zpátečka) útočí na pravou kartu '{rightPlayer.data.cardName}'");
                    yield return enemyCard.AttackAnimation(playerDeck.fieldSlots[targetSlot].position, 0.35f, 1.2f);
                    int beforeHP = rightPlayer.currentHealth;
                    bool died = rightPlayer.TakeDamage(enemyCard.currentDamage);
                    int afterHP = rightPlayer.currentHealth;
                    if (died)
                    {
                        playerDeck.RemoveCardFromSlot(targetSlot);
                        int overflow = -(afterHP);
                        if (overflow > 0)
                        {
                            PlayerHealth -= overflow;
                            UpdateHealthUI();
                            if (PlayerHealth <= 0)
                            {
                                Debug.Log("[GameManager] Hráč poražen! GAME OVER!");
                                yield break;
                            }
                        }
                    }
                }
            }
            else if (vidlicka)
            {
                // Útočí pouze na karty vlevo a vpravo od protější karty (slot i-1, i+1)
                int leftSlot = i - 1;
                int rightSlot = i + 1;
                bool hit = false;
                if (leftSlot >= 0)
                {
                    Card leftPlayer = playerDeck.GetCardAtSlot(leftSlot);
                    if (leftPlayer != null)
                    {
                        Debug.Log($"[GameManager] Enemy '{enemyCard.data.cardName}' (Vidlička) útočí na levou kartu '{leftPlayer.data.cardName}'");
                        yield return enemyCard.AttackAnimation(playerDeck.fieldSlots[leftSlot].position, 0.35f, 1.2f);
                        int beforeHP = leftPlayer.currentHealth;
                        bool died = leftPlayer.TakeDamage(enemyCard.currentDamage);
                        int afterHP = leftPlayer.currentHealth;
                        if (died)
                        {
                            playerDeck.RemoveCardFromSlot(leftSlot);
                            int overflow = -(afterHP);
                            if (overflow > 0)
                            {
                                PlayerHealth -= overflow;
                                UpdateHealthUI();
                                if (PlayerHealth <= 0)
                                {
                                    Debug.Log("[GameManager] Hráč poražen! GAME OVER!");
                                    yield break;
                                }
                            }
                        }
                        hit = true;
                    }
                }
                if (rightSlot < playerDeck.SlotCount)
                {
                    Card rightPlayer = playerDeck.GetCardAtSlot(rightSlot);
                    if (rightPlayer != null)
                    {
                        Debug.Log($"[GameManager] Enemy '{enemyCard.data.cardName}' (Vidlička) útočí na pravou kartu '{rightPlayer.data.cardName}'");
                        yield return enemyCard.AttackAnimation(playerDeck.fieldSlots[rightSlot].position, 0.35f, 1.2f);
                        int beforeHP = rightPlayer.currentHealth;
                        bool died = rightPlayer.TakeDamage(enemyCard.currentDamage);
                        int afterHP = rightPlayer.currentHealth;
                        if (died)
                        {
                            playerDeck.RemoveCardFromSlot(rightSlot);
                            int overflow = -(afterHP);
                            if (overflow > 0)
                            {
                                PlayerHealth -= overflow;
                                UpdateHealthUI();
                                if (PlayerHealth <= 0)
                                {
                                    Debug.Log("[GameManager] Hráč poražen! GAME OVER!");
                                    yield break;
                                }
                            }
                        }
                        hit = true;
                    }
                }
                if (!hit)
                {
                    Debug.Log($"[GameManager] Enemy '{enemyCard.data.cardName}' (Vidlička) nemá koho zasáhnout.");
                }
            }
            else
            {
                Card playerCard = playerDeck.GetCardAtSlot(i);
                if (playerCard != null)
                {
                    // Útok na hráčovu kartu
                    Debug.Log($"[GameManager] Enemy '{enemyCard.data.cardName}' (dmg {enemyCard.currentDamage}) útočí na '{playerCard.data.cardName}' (hp {playerCard.currentHealth})");
                    yield return enemyCard.AttackAnimation(playerCard.transform.position, 0.35f, 1.2f);
                    int beforeHP = playerCard.currentHealth;
                    bool died = playerCard.TakeDamage(enemyCard.currentDamage);
                    int afterHP = playerCard.currentHealth;
                    if (died)
                    {
                        Debug.Log($"[GameManager] '{playerCard.data.cardName}' zničena!");
                        playerDeck.RemoveCardFromSlot(i);
                        int overflow = -(afterHP); // afterHP je záporné nebo nula
                        if (overflow > 0)
                        {
                            Debug.Log($"[GameManager] Zbylý damage {overflow} projde hráči!");
                            PlayerHealth -= overflow;
                            UpdateHealthUI();
                            if (PlayerHealth <= 0)
                            {
                                Debug.Log("[GameManager] Hráč poražen! GAME OVER!");
                                yield break;
                            }
                        }
                    }
                }
                else
                {
                    // Žádná player karta → dmg hráči
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

        // Vyčisti jednorázové efekty na enemy kartách (stun, shield)
        ClearTurnEffects(false);
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

