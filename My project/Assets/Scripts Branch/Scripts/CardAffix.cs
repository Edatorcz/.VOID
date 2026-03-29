using UnityEngine;

/// <summary>
/// Základní třída pro affix (schopnost/efekt) karty.
/// Každý affix implementuje vlastní chování.
/// </summary>
public abstract class CardAffix : MonoBehaviour
{
    public abstract string AffixName { get; }
    public abstract string Description { get; }

    // Volá se při přidání affixu na kartu
    public virtual void OnApply(Card card) { }
    // Volá se při odebrání affixu z karty
    public virtual void OnRemove(Card card) { }
    // Volá se při začátku kola (volitelné)
    public virtual void OnTurnStart(Card card) { }
    // Volá se při útoku (volitelné)
    public virtual void OnAttack(Card card) { }
    // Volá se při přijímání damage (volitelné)
    public virtual int OnTakeDamage(Card card, int amount) { return amount; }
}
