using System.Collections.Generic;
using UnityEngine;

// 워프 노드 사이의 연결 정보를 UI 선으로 표시하는 뷰
public sealed class WarpGraphLineView
{
    private readonly List<RectTransform> spawnedLines = new();

    public void Rebuild(
        RectTransform container,
        RectTransform linePrefab,
        IReadOnlyList<IReadOnlyList<EventNode>> layers,
        IReadOnlyList<WarpGraphEdge> edges)
    {
        Clear();

        foreach (WarpGraphEdge edge in edges)
        {
            RectTransform start = layers[edge.FromLayer][edge.FromNode].transform as RectTransform;
            RectTransform end = layers[edge.ToLayer][edge.ToNode].transform as RectTransform;

            if (start == null || end == null)
                continue;

            RectTransform line = Object.Instantiate(linePrefab, container);
            SetLineTransform(line, start, end);
            line.SetAsFirstSibling();
            spawnedLines.Add(line);
        }
    }

    public void Clear()
    {
        foreach (RectTransform line in spawnedLines)
            if (line != null)
                Object.Destroy(line.gameObject);

        spawnedLines.Clear();
    }

    private static void SetLineTransform(RectTransform line, RectTransform start, RectTransform end)
    {
        Vector2 startCenter = start.anchoredPosition;
        Vector2 endCenter = end.anchoredPosition;
        Vector2 direction = endCenter - startCenter;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            line.anchoredPosition = startCenter;
            line.sizeDelta = new Vector2(0f, line.sizeDelta.y);
            return;
        }

        direction.Normalize();
        // 노드 중심 대신 사각 경계 사이를 연결
        Vector2 startEdge = CalculateRectangleEdge(start, direction);
        Vector2 endEdge = CalculateRectangleEdge(end, -direction);

        line.anchoredPosition = (startEdge + endEdge) * 0.5f;
        line.sizeDelta = new Vector2(Vector2.Distance(startEdge, endEdge), line.sizeDelta.y);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private static Vector2 CalculateRectangleEdge(RectTransform node, Vector2 direction)
    {
        Vector2 halfSize = node.rect.size * 0.5f;
        float horizontalScale = Mathf.Abs(direction.x) > Mathf.Epsilon
            ? halfSize.x / Mathf.Abs(direction.x)
            : float.PositiveInfinity;
        float verticalScale = Mathf.Abs(direction.y) > Mathf.Epsilon
            ? halfSize.y / Mathf.Abs(direction.y)
            : float.PositiveInfinity;

        return node.anchoredPosition + direction * Mathf.Min(horizontalScale, verticalScale);
    }
}
