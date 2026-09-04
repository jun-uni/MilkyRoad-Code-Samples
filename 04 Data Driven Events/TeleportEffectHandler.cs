using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TeleportEffectHandler : ISpecialEffectHandler
{
    public void HandleEffect(EventOutcome outcome)
    {
        List<PlanetData> planets = GameManager.Instance.PlanetDataList;
        if (planets == null || planets.Count == 0)
        {
            Debug.LogWarning("이동할 행성 데이터가 없습니다.");
            return;
        }

        PlanetData targetPlanet = planets[Random.Range(0, planets.Count)];

        // 목적 행성 전환에 맞춰 이전 월드 탐색 상태 정리
        GameManager.Instance.ClearCurrentWarpMap();
        GameManager.Instance.SetCurrentWarpTargetPlanetId(targetPlanet.planetId);
        EventManager.Instance.ClearPendingEvents();
        GameManager.Instance.WorldNodeDataList.Clear();

        // 월드 맵 범위에 맞춘 함선 위치 갱신
        Vector2 planetPosition = targetPlanet.normalizedPosition;
        GameManager.Instance.normalizedPlayerPosition = new Vector2(
            Mathf.Clamp01(planetPosition.x),
            Mathf.Clamp01(planetPosition.y)
        );

        GameManager.Instance.LandOnPlanet();
    }
}
