using System;
using UnityEngine;

public enum ProjectileMovementType
{
    Linear,
    Parabolic
}

// 투사체 식별 정보와 이동 방식을 묶는 직렬화 데이터
[Serializable]
public class ProjectileData
{
    [Header("식별 정보")]
    public int projectileId;
    public string projectileName;
    public GameObject projectilePrefab;

    [Header("이동 설정")]
    [Min(0f)] public float speed;
    [Min(0f)] public float maxLifetime;
    public ProjectileMovementType movementType = ProjectileMovementType.Linear;
    [Min(0f)] public float linearPhaseDuration = 2f;

    [Range(0f, 89f)]
    public float parabolicAngle;

    public bool allowDownwardParabola = true;
}
