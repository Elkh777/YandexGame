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
        float direction = player.localScale.x >= 0f ? 1f : -1f;
        float spawnDistance = Random.Range(spawnAheadMin, spawnAheadMax) * direction;
        float spawnX = Mathf.Clamp(player.position.x + spawnDistance, minSpawnX, maxSpawnX);

        GameObject enemyObject = new GameObject("Enemy");
        enemyObject.tag = "Enemy";
        enemyObject.transform.position = new Vector3(spawnX, groundY + 1f, 0f);

        SpriteRenderer renderer = enemyObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 1;

        BoxCollider2D collider = enemyObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 1.1f);

        Rigidbody2D body = enemyObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        Enemy enemy = enemyObject.AddComponent<Enemy>();
        enemy.maxHealth = 3;
        enemy.detectionRange = 8f;
        enemy.checkLineOfSight = false;
        enemy.patrolSpeed = 1.2f;
        enemy.chaseSpeed = 2.4f;
        enemy.attackRange = 1f;
        enemy.rangedAttackRange = 6f;
        enemy.scoreReward = 100;
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
