using UnityEngine;

// Прогресс уровней в постоянном хранилище (PlayerPrefs).
// UnlockedLevel — максимальный разблокированный индекс уровня (0-based).
// StartLevel — с какого уровня запустить игру (выставляется из меню выбора уровней).
public static class LevelProgress
{
    private const string KeyUnlocked = "level_unlocked";
    private const string KeyStart = "level_start";

    // Индекс самого старшего разблокированного уровня (0 = только первый).
    public static int UnlockedLevel => PlayerPrefs.GetInt(KeyUnlocked, 0);

    public static bool IsUnlocked(int levelIndex) => levelIndex <= UnlockedLevel;

    // Отметить уровень пройденным — разблокирует следующий.
    public static void MarkCompleted(int levelIndex)
    {
        int next = levelIndex + 1;
        if (next > UnlockedLevel)
        {
            PlayerPrefs.SetInt(KeyUnlocked, next);
            PlayerPrefs.Save();
        }
    }

    // С какого уровня стартовать (по умолчанию 0). После прочтения сбрасывается на 0.
    public static int ConsumeStartLevel()
    {
        int level = PlayerPrefs.GetInt(KeyStart, 0);
        PlayerPrefs.SetInt(KeyStart, 0);
        PlayerPrefs.Save();
        return level;
    }

    public static void SetStartLevel(int levelIndex)
    {
        PlayerPrefs.SetInt(KeyStart, levelIndex);
        PlayerPrefs.Save();
    }

    // Полный сброс прогресса (на случай отладки/новой игры).
    public static void ResetAll()
    {
        PlayerPrefs.SetInt(KeyUnlocked, 0);
        PlayerPrefs.SetInt(KeyStart, 0);
        PlayerPrefs.Save();
    }
}
