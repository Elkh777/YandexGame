using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public int maxAliveEnemies = 5;
    public float spawnIntervalMin = 2.5f;
    public float spawnIntervalMax = 4.5f;
    public float spawnAheadMin = 10f;
    public float spawnAheadMax = 24f;
    public float minSpawnX = 8f;
    public float maxSpawnX = 105f;
    public float groundY = -3f;

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
            enemy.attackRange = 1f;
            enemy.rangedAttackRange = 6f;
            enemy.scoreReward = 100;
        }

        SpriteRenderer sr = enemyObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 1;
        }
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
