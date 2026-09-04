using System;
using System.Collections.Generic;
using UnityEngine;

// 기본 화물 형태와 회전별 점유 좌표를 제공하는 카탈로그
public sealed class ItemShape
{
    public const int GridSize = 5;
    public const int CenterIndex = GridSize / 2;
    public const int RotationCount = 4;

    public static ItemShape Instance { get; } = new();

    private readonly IReadOnlyList<Vector2Int>[][] occupiedOffsets;

    private ItemShape()
    {
        Vector2Int[][] baseShapes =
        {
            new[] { Cell(0, 0) },
            new[] { Cell(-1, 0), Cell(0, 0) },
            new[] { Cell(-1, 0), Cell(0, 0), Cell(1, 0) },
            new[] { Cell(-2, 0), Cell(-1, 0), Cell(0, 0), Cell(0, -1) },
            new[] { Cell(0, 1), Cell(-2, 0), Cell(-1, 0), Cell(0, 0) },
            new[] { Cell(0, 1), Cell(-1, 0), Cell(0, 0), Cell(1, 0) },
            new[] { Cell(-2, 0), Cell(-1, 0), Cell(0, 0), Cell(1, 0) },
            new[] { Cell(-1, 1), Cell(0, 1), Cell(0, 0), Cell(1, 0) },
            new[] { Cell(0, 1), Cell(1, 1), Cell(-1, 0), Cell(0, 0) },
            new[] { Cell(-1, 1), Cell(0, 1), Cell(-1, 0), Cell(0, 0) },
            new[] { Cell(0, 1), Cell(-1, 0), Cell(0, 0), Cell(-1, -1), Cell(0, -1) },
            new[] { Cell(-1, 1), Cell(0, 1), Cell(1, 1), Cell(-1, 0), Cell(0, 0), Cell(1, 0) },
            new[] { Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(0, -1), Cell(2, -1) },
            new[]
            {
                Cell(-1, 1), Cell(0, 1), Cell(1, 1),
                Cell(-1, 0), Cell(0, 0), Cell(1, 0),
                Cell(-1, -1), Cell(0, -1), Cell(1, -1)
            }
        };

        occupiedOffsets = new IReadOnlyList<Vector2Int>[baseShapes.Length][];

        for (int shapeId = 0; shapeId < baseShapes.Length; shapeId++)
            occupiedOffsets[shapeId] = CreateRotations(baseShapes[shapeId]);
    }

    public IReadOnlyList<Vector2Int> GetOccupiedOffsets(int shapeId, int rotation)
    {
        if (shapeId < 0 || shapeId >= occupiedOffsets.Length)
            return Array.Empty<Vector2Int>();
        if (rotation < 0 || rotation >= RotationCount)
            return Array.Empty<Vector2Int>();

        return occupiedOffsets[shapeId][rotation];
    }

    private static IReadOnlyList<Vector2Int>[] CreateRotations(IReadOnlyList<Vector2Int> baseShape)
    {
        IReadOnlyList<Vector2Int>[] rotations = new IReadOnlyList<Vector2Int>[RotationCount];
        Vector2Int[] current = CopyAndValidate(baseShape);

        for (int rotation = 0; rotation < RotationCount; rotation++)
        {
            rotations[rotation] = Array.AsReadOnly(current);

            Vector2Int[] next = new Vector2Int[current.Length];
            for (int index = 0; index < current.Length; index++)
            {
                // 직전 형태를 시계 방향으로 90도 회전
                Vector2Int offset = current[index];
                next[index] = new Vector2Int(offset.y, -offset.x);
            }

            current = next;
        }

        return rotations;
    }

    private static Vector2Int[] CopyAndValidate(IReadOnlyList<Vector2Int> shape)
    {
        Vector2Int[] copy = new Vector2Int[shape.Count];

        for (int index = 0; index < shape.Count; index++)
        {
            Vector2Int offset = shape[index];
            if (Math.Abs(offset.x) > CenterIndex || Math.Abs(offset.y) > CenterIndex)
                throw new ArgumentOutOfRangeException(
                    nameof(shape),
                    $"화물 형태가 {GridSize}×{GridSize} 영역을 벗어났습니다."
                );

            copy[index] = offset;
        }

        return copy;
    }

    private static Vector2Int Cell(int x, int y)
    {
        return new Vector2Int(x, y);
    }
}
