using System;
using UnityEngine;

// Базовая переиспользуемая система здоровья (float). Используется игроком и врагами.
// Сообщает об изменениях (OnChanged) и смерти (OnDeath); UI подписывается на эти события.
public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public event Action<float, float> OnChanged; // (current, max)
    public event Action OnDeath;

    public bool IsDead { get; private set; }
    public float Normalized => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    // Задаёт максимум (например, при настройке типа врага/уровня). refill — восстановить до полного.
    public void SetMax(float newMax, bool refill = true)
    {
        maxHealth = Mathf.Max(1f, newMax);
        currentHealth = refill ? maxHealth : Mathf.Min(currentHealth, maxHealth);
        IsDead = currentHealth <= 0f;
        OnChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float dmg)
    {
        if (IsDead || dmg <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        OnChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0f)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnChanged?.Invoke(currentHealth, maxHealth);
    }
}
