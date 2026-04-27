using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("вќ¤пёЏ Р—РґРѕСЂРѕРІСЊРµ")]
    public int maxHealth = 3;
    [HideInInspector] public int currentHealth;

    [Header("Р­С„С„РµРєС‚С‹")]
    public float invincibilityTime = 1f;
    private bool _isInvincible;
}