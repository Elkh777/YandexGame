using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public int maxAliveEnemies = 5;
    public float spawnIntervalMin = 5f;
    public float spawnIntervalMax = 8f;
    public float spawnAheadMin = 10f;
    public float spawnAheadMax = 24f;
    public float minSpawnX = 8f;
    public float maxSpawnX = 105f;
    public float groundY = -3f;

    // Пути к спрайтам видов врагов (в папке Resources/Sprites). При спавне выбирается случайный.
    public string[] enemySpritePaths =
    {
        "Sprites/enemy1",
        "Sprites/enemy2",
        "Sprites/enemy3",
        "Sprites/enemy4",
        "Sprites/enemy5",
        "Sprites/enemy6",
    };

    // Параллельный массив: true — спрайт нарисован лицом вправо, false — лицом влево.
    // Все шесть спрайтов (включая призрака enemy6) нарисованы лицом вправо.
    public bool[] enemySpriteFacesRight =
    {
        true, true, true, true, true, true,
    };

    private Sprite[] _enemySprites;
    private Transform player;
    private float nextSpawnTime;

    void Start()
    {
        FindPlayer();
        ScheduleNextSpawn();
    }

    void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        if (Time.time < nextSpawnTime || CountAliveEnemies() >= maxAliveEnemies)
        {
            return;
        }

        SpawnEnemy();
        ScheduleNextSpawn();
    }

    private void SpawnEnemy()
    {
        GameObject prefab = Resources.Load<GameObject>("Sprites/Prefabs/Enemy");
        if (prefab == null)
        {
            Debug.LogError("Enemy prefab not found in Resources/Sprites/Prefabs/Enemy");
            return;
        }

        float direction = player.localScale.x >= 0f ? 1f : -1f;
        float spawnDistance = Random.Range(spawnAheadMin, spawnAheadMax) * direction;
        float spawnX = Mathf.Clamp(player.position.x + spawnDistance, minSpawnX, maxSpawnX);

        GameObject enemyObject = Instantiate(prefab, new Vector3(spawnX, groundY, 0f), Quaternion.identity);
        enemyObject.tag = "Enemy";

        Enemy enemy = enemyObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.maxHealth = 3;
            enemy.detectionRange = 8f;
            enemy.checkLineOfSight = false;
            enemy.patrolSpeed = 1.2f;
            enemy.chaseSpeed = 2.4f;
            enemy.attackRange = 1.5f;
            enemy.rangedAttackRange = 6f;
            enemy.scoreReward = 10;

            // Случайно выбираем один из видов врага. Внешность отличается, характеристики — одинаковые.
            AssignRandomAppearance(enemy);
        }

        SpriteRenderer sr = enemyObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 1;
        }
    }

    private void AssignRandomAppearance(Enemy enemy)
    {
        EnsureSpritesLoaded();

        // Собираем индексы успешно загруженных спрайтов и выбираем случайный.
        List<int> available = new List<int>();
        for (int i = 0; i < _enemySprites.Length; i++)
        {
            if (_enemySprites[i] != null)
            {
                available.Add(i);
            }
        }

        if (available.Count == 0)
        {
            return;
        }

        int index = available[Random.Range(0, available.Count)];
        enemy.bodySprite = _enemySprites[index];
        enemy.spriteFacesRight = index < enemySpriteFacesRight.Length ? enemySpriteFacesRight[index] : true;
    }

    private void EnsureSpritesLoaded()
    {
        // Загружаем спрайты один раз и кэшируем. LoadAll корректно работает и для режима Multiple.
        if (_enemySprites != null)
        {
            return;
        }

        _enemySprites = new Sprite[enemySpritePaths.Length];
        for (int i = 0; i < enemySpritePaths.Length; i++)
        {
            Sprite[] loaded = Resources.LoadAll<Sprite>(enemySpritePaths[i]);
            if (loaded != null && loaded.Length > 0)
            {
                _enemySprites[i] = PickLargestSprite(loaded);
            }
            else
            {
                Debug.LogWarning($"Не удалось загрузить спрайт врага: {enemySpritePaths[i]}");
            }
        }
    }

    // Среди нарезанных спрайтов берём самый большой по площади (на случай лишних мелких слайсов).
    private Sprite PickLargestSprite(Sprite[] sprites)
    {
        Sprite best = null;
        float bestArea = -1f;
        foreach (Sprite sprite in sprites)
        {
            if (sprite == null) continue;
            float area = sprite.rect.width * sprite.rect.height;
            if (area > bestArea)
            {
                bestArea = area;
                best = sprite;
            }
        }

        return best;
    }

    private void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(spawnIntervalMin, spawnIntervalMax);
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private int CountAliveEnemies()
    {
        return GameObject.FindGameObjectsWithTag("Enemy").Length;
    }
}
