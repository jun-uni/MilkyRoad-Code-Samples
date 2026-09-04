using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class MyProjectile : MonoBehaviour
{
    private const float MinimumMovementSqrMagnitude = 0.000001f;

    private ProjectileData data;
    private Action<MyProjectile> returnToPool;
    private Action onHit;
    private TrailRenderer trailRenderer;

    private Vector3 targetPosition;
    private Vector3 parabolicStartPosition;
    private Vector3 middleControlPoint;

    private float lifetime;
    private float parabolicDistance;
    private float journeyProgress;

    private bool usesParabolicMovement;
    private bool hasStartedParabolicMovement;
    private bool isFlying;

    public int ProjectileId => data?.projectileId ?? -1;

    public void Initialize(ProjectileData projectileData, Action<MyProjectile> returnCallback)
    {
        data = projectileData;
        returnToPool = returnCallback;
        TryGetComponent(out trailRenderer);
    }

    public void Fire(Vector3 target, Action hitCallback)
    {
        if (data == null)
            return;

        targetPosition = target;
        onHit = hitCallback;
        lifetime = 0f;
        journeyProgress = 0f;
        isFlying = true;
        trailRenderer?.Clear();

        usesParabolicMovement =
            data.movementType == ProjectileMovementType.Parabolic && data.parabolicAngle > 0f;
        hasStartedParabolicMovement = false;

        RotateAlongMovement(targetPosition - transform.position);
    }

    public void UpdateProjectile(float deltaTime)
    {
        if (!isFlying || data == null)
            return;

        lifetime += deltaTime;
        if (lifetime >= data.maxLifetime)
        {
            ReturnToPool();
            return;
        }

        if (usesParabolicMovement &&
            !hasStartedParabolicMovement &&
            lifetime >= data.linearPhaseDuration)
        {
            // 설정된 직선 구간 이후 베지어 이동 전환
            BeginParabolicMovement();
        }

        bool reachedTarget = hasStartedParabolicMovement
            ? UpdateParabolicMovement(deltaTime)
            : UpdateLinearMovement(deltaTime);

        if (reachedTarget)
            HitTarget();
    }

    public void ResetForPool()
    {
        // 다음 대여를 위한 런타임 상태 초기화
        onHit = null;
        lifetime = 0f;
        journeyProgress = 0f;
        isFlying = false;
        hasStartedParabolicMovement = false;
        trailRenderer?.Clear();
    }

    private bool UpdateLinearMovement(float deltaTime)
    {
        Vector3 previousPosition = transform.position;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            data.speed * deltaTime
        );

        RotateAlongMovement(transform.position - previousPosition);
        return (transform.position - targetPosition).sqrMagnitude <= MinimumMovementSqrMagnitude;
    }

    private void BeginParabolicMovement()
    {
        parabolicStartPosition = transform.position;
        parabolicDistance = Vector3.Distance(parabolicStartPosition, targetPosition);
        journeyProgress = 0f;
        hasStartedParabolicMovement = true;

        Vector3 midpoint = (parabolicStartPosition + targetPosition) * 0.5f;
        // 이동 거리와 각도에 따른 베지어 제어점 높이 계산
        float height = parabolicDistance * Mathf.Tan(data.parabolicAngle * Mathf.Deg2Rad) * 0.5f;

        if (data.allowDownwardParabola && Random.Range(0, 2) == 0)
            height *= -1f;

        middleControlPoint = midpoint + Vector3.up * height;
    }

    private bool UpdateParabolicMovement(float deltaTime)
    {
        if (parabolicDistance <= Mathf.Epsilon)
            return true;

        journeyProgress = Mathf.Clamp01(
            journeyProgress + data.speed * deltaTime / parabolicDistance
        );

        Vector3 previousPosition = transform.position;
        transform.position = CalculateQuadraticBezierPoint(
            journeyProgress,
            parabolicStartPosition,
            middleControlPoint,
            targetPosition
        );

        RotateAlongMovement(transform.position - previousPosition);
        return journeyProgress >= 1f;
    }

    private void RotateAlongMovement(Vector3 movement)
    {
        if (movement.sqrMagnitude <= MinimumMovementSqrMagnitude)
            return;

        float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void HitTarget()
    {
        transform.position = targetPosition;
        isFlying = false;

        Action hitCallback = onHit;
        onHit = null;
        try
        {
            hitCallback?.Invoke();
        }
        finally
        {
            // 타격 처리 예외와 무관한 풀 반환 보장
            returnToPool?.Invoke(this);
        }
    }

    private void ReturnToPool()
    {
        isFlying = false;
        returnToPool?.Invoke(this);
    }

    private static Vector3 CalculateQuadraticBezierPoint(
        float t,
        Vector3 start,
        Vector3 control,
        Vector3 end)
    {
        float inverseT = 1f - t;
        return inverseT * inverseT * start +
               2f * inverseT * t * control +
               t * t * end;
    }
}
