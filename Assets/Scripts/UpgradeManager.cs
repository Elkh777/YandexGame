using UnityEngine;

// Тип улучшения магазина. Значение enum используется как индекс в массивах конфигурации/уровней.
public enum UpgradeType
{
    WeaponDamage = 0,   // ⚔️ Сила оружия
    AttackSpeed = 1,    // ⚡ Скорость атаки
    Invincibility = 2,  // 🛡️ Неуязвимость после получения урона
    Freeze = 3,         // ❄️ Замораживающие пули (каждая 2-я пуля замедляет врага)
    Poison = 4          // ☠️ Ядовитые пули (урон во времени)
}

// Менеджер улучшений магазина.
// Хранит уровни прокачки, предоставляет эффекты игроку (множители урона/скорости атаки, бонус неуязвимости)
// и логику покупки. Реализован как статический класс: данные живут в рамках игровой сессии
// (статические поля переживают перезагрузку сцен при смене уровней, сбрасываются при выходе из игры).
// Не требует размещения в сцене.
public static class UpgradeManager
{
    // Конфигурация (индексируется по UpgradeType):
    public static readonly string[] DisplayNames = { "Сила оружия", "Скорость атаки", "Неуязвимость", "Заморозка пуль", "Ядовитые пули" };
    public static readonly int[] MaxLevels = { 5, 3, 3, 3, 3 };          // максимальные уровни
    public static readonly int[] CostPerLevel = { 60, 80, 120, 100, 100 }; // цена одного уровня в монетах

    // Текущие уровни прокачки. Длина = количеству улучшений.
    private static readonly int[] _levels = new int[5];

    // Событие об изменении любого улучшения — на него подписывается окно магазина для обновления кнопок.
    public static event System.Action OnUpgradesChanged;

    // ===== Доступ к состоянию =====

    public static int GetLevel(UpgradeType type) => _levels[(int)type];
    public static int GetMaxLevel(UpgradeType type) => MaxLevels[(int)type];
    public static string GetName(UpgradeType type) => DisplayNames[(int)type];
    public static bool IsMaxed(UpgradeType type) => _levels[(int)type] >= MaxLevels[(int)type];

    // Цена следующего уровня. У этих улучшений цена фиксированная за уровень.
    public static int GetNextCost(UpgradeType type) => CostPerLevel[(int)type];

    // Хватает ли монет на следующий уровень (без учёта максимума).
    public static bool CanAfford(UpgradeType type)
    {
        return CoinManager.Instance != null && CoinManager.Instance.CoinCount >= GetNextCost(type);
    }

    // ===== Покупка =====

    // Пытается купить 1 уровень улучшения:
    // 1) проверяет максимум, 2) списывает монеты (через CoinManager), 3) повышает уровень,
    // 4) оповещает подписчиков. Возвращает true при успешной покупке.
    public static bool TryBuy(UpgradeType type)
    {
        if (IsMaxed(type)) return false;
        if (CoinManager.Instance == null) return false;
        if (!CoinManager.Instance.SpendCoins(GetNextCost(type))) return false;

        _levels[(int)type]++;
        OnUpgradesChanged?.Invoke();
        return true;
    }

    // ===== Эффекты для игрока =====

    // Множитель урона оружия: +10% за уровень. Ур.0 -> 1.0, ур.5 -> 1.5.
    public static float WeaponDamageMultiplier => 1f + 0.1f * _levels[(int)UpgradeType.WeaponDamage];

    // Множитель скорости атаки: +10% за уровень. Ур.0 -> 1.0, ур.3 -> 1.3.
    // Используется как делитель интервала между атаками (меньше интервал = чаще атака).
    public static float AttackSpeedMultiplier => 1f + 0.1f * _levels[(int)UpgradeType.AttackSpeed];

    // Доп. время неуязвимости после получения урона: +0.5с за уровень. Ур.0 -> 0с, ур.3 -> 1.5с.
    public static float BonusInvincibilityTime => 0.5f * _levels[(int)UpgradeType.Invincibility];

    // ❄️ Заморозка: каждая 2-я пуля замедляет врага. Куплено, если уровень > 0.
    public static bool FreezeEnabled => _levels[(int)UpgradeType.Freeze] > 0;
    // Длительность заморозки растёт с уровнем: ур.1 -> 1.5с, ур.3 -> 2.5с.
    public static float FreezeDuration => 1f + 0.5f * _levels[(int)UpgradeType.Freeze];
    // Множитель скорости замороженного врага: чем выше уровень, тем сильнее замедление (но не до полной остановки).
    public static float FreezeSlowFactor => Mathf.Clamp(0.6f - 0.1f * _levels[(int)UpgradeType.Freeze], 0.25f, 0.6f);

    // ☠️ Яд: пули наносят урон во времени. Куплено, если уровень > 0.
    public static bool PoisonEnabled => _levels[(int)UpgradeType.Poison] > 0;
    // Длительность действия яда: ур.1 -> 2.5с, ур.3 -> 3.5с.
    public static float PoisonDuration => 2f + 0.5f * _levels[(int)UpgradeType.Poison];
    // Урон яда в секунду: +0.6 за уровень.
    public static float PoisonDps => 0.6f * _levels[(int)UpgradeType.Poison];
}
