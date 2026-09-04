using System;
using System.Collections.Generic;
using UnityEngine;

// ID별 투사체 풀과 활성 수명주기 관리
public class ProjectileManager : MonoBehaviour
{
    private sealed class PoolEntry
    {
        public ProjectileData Data { get; }
        public Transform Parent { get; }
        public Queue<MyProjectile> Available { get; } = new();

        public PoolEntry(ProjectileData data, Transform parent)
        {
            Data = data;
            Parent = parent;
        }
    }

    public static ProjectileManager Instance { get; private set; }

    [SerializeField] private List<ProjectileData> projectileData = new();
    [SerializeField, Min(1)] private int initialPoolSize = 100;
    [SerializeField, Min(1)] private int expansionSize = 10;

    private readonly Dictionary<int, PoolEntry> pools = new();
    private readonly List<MyProjectile> activeProjectiles = new();

    private Transform pooledProjectilesParent;
    private Transform activeProjectilesParent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateHierarchy();
        InitializePools();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        for (int index = activeProjectiles.Count - 1; index >= 0; index--)
        {
            MyProjectile projectile = activeProjectiles[index];
            if (projectile == null)
            {
                activeProjectiles.RemoveAt(index);
                continue;
            }

            if (!projectile.gameObject.activeInHierarchy)
            {
                ReturnProjectileToPool(projectile);
                continue;
            }

            projectile.UpdateProjectile(Time.deltaTime);
        }
    }

    public MyProjectile FireProjectile(
        Vector3 startPosition,
        Vector3 targetPosition,
        int projectileId,
        Action onHit)
    {
        if (!TryGetProjectile(projectileId, out MyProjectile projectile))
        {
            Debug.LogWarning($"사용 가능한 투사체가 없습니다. ID: {projectileId}");
            return null;
        }

        projectile.transform.SetParent(activeProjectilesParent, true);
        projectile.transform.position = startPosition;
        projectile.gameObject.SetActive(true);
        projectile.Fire(targetPosition, onHit);

        activeProjectiles.Add(projectile);
        return projectile;
    }

    public void ReturnProjectileToPool(MyProjectile projectile)
    {
        if (projectile == null || !activeProjectiles.Remove(projectile))
            return;

        if (!pools.TryGetValue(projectile.ProjectileId, out PoolEntry pool))
        {
            Debug.LogWarning($"반환할 투사체 풀이 없습니다. ID: {projectile.ProjectileId}");
            Destroy(projectile.gameObject);
            return;
        }

        projectile.ResetForPool();
        projectile.gameObject.SetActive(false);
        projectile.transform.SetParent(pool.Parent, false);
        pool.Available.Enqueue(projectile);
    }

    private void InitializePools()
    {
        pools.Clear();
        activeProjectiles.Clear();

        foreach (ProjectileData data in projectileData)
        {
            if (!TryCreatePool(data, out PoolEntry pool))
                continue;

            pools.Add(data.projectileId, pool);
            // ID별 초기 인스턴스 사전 생성
            CreateProjectiles(pool, initialPoolSize);
        }
    }

    private bool TryCreatePool(ProjectileData data, out PoolEntry pool)
    {
        pool = null;

        if (data == null || data.projectilePrefab == null)
        {
            Debug.LogWarning("투사체 데이터 또는 프리팹이 없습니다.");
            return false;
        }

        if (pools.ContainsKey(data.projectileId))
        {
            Debug.LogWarning($"중복된 투사체 ID: {data.projectileId}");
            return false;
        }

        if (data.projectilePrefab.GetComponent<MyProjectile>() == null)
        {
            Debug.LogWarning($"프리팹에 MyProjectile이 없습니다. ID: {data.projectileId}");
            return false;
        }

        if (data.speed <= 0f || data.maxLifetime <= 0f)
        {
            Debug.LogWarning($"투사체 속도와 수명은 0보다 커야 합니다. ID: {data.projectileId}");
            return false;
        }

        string poolName = string.IsNullOrWhiteSpace(data.projectileName)
            ? $"Projectile {data.projectileId}"
            : data.projectileName;

        Transform poolParent = new GameObject(poolName).transform;
        poolParent.SetParent(pooledProjectilesParent, false);
        pool = new PoolEntry(data, poolParent);
        return true;
    }

    private bool TryGetProjectile(int projectileId, out MyProjectile projectile)
    {
        projectile = null;

        if (!pools.TryGetValue(projectileId, out PoolEntry pool))
            return false;

        while (pool.Available.Count > 0)
        {
            projectile = pool.Available.Dequeue();
            if (projectile != null)
                return true;
        }

        // 고갈된 종류만 지정된 크기로 확장
        CreateProjectiles(pool, expansionSize);
        if (pool.Available.Count == 0)
            return false;

        projectile = pool.Available.Dequeue();
        return projectile != null;
    }

    private void CreateProjectiles(PoolEntry pool, int count)
    {
        for (int index = 0; index < count; index++)
        {
            GameObject instance = Instantiate(pool.Data.projectilePrefab, pool.Parent);
            MyProjectile projectile = instance.GetComponent<MyProjectile>();

            projectile.Initialize(pool.Data, ReturnProjectileToPool);
            projectile.ResetForPool();
            projectile.gameObject.SetActive(false);
            pool.Available.Enqueue(projectile);
        }
    }

    private void CreateHierarchy()
    {
        pooledProjectilesParent = new GameObject("Pooled Projectiles").transform;
        pooledProjectilesParent.SetParent(transform, false);

        activeProjectilesParent = new GameObject("Active Projectiles").transform;
        activeProjectilesParent.SetParent(transform, false);
    }
}
