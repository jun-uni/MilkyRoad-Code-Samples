using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

// 외갑판 형태와 방향을 계산하는 순수 배치 계산기
public sealed class OuterHullPlacementCalculator
{
    private const uint StraightVariationPercent = 30;

    private static readonly CardinalDirection[] CardinalDirections =
    {
        CardinalDirection.Down,
        CardinalDirection.Left,
        CardinalDirection.Up,
        CardinalDirection.Right
    };

    private static readonly Vector2Int[] CardinalOffsets =
    {
        new(0, -1),
        new(-1, 0),
        new(0, 1),
        new(1, 0)
    };

    private static readonly Vector2Int[] CornerOffsets =
    {
        new(-1, -1),
        new(-1, 1),
        new(1, 1),
        new(1, -1)
    };

    private static readonly OuterCornerRule[] OuterCornerRules =
    {
        new(CornerDirection.DownLeft, CardinalDirection.Right, CardinalDirection.Up),
        new(CornerDirection.UpLeft, CardinalDirection.Right, CardinalDirection.Down),
        new(CornerDirection.UpRight, CardinalDirection.Left, CardinalDirection.Down),
        new(CornerDirection.DownRight, CardinalDirection.Left, CardinalDirection.Up)
    };

    private static readonly InnerCornerRule[] InnerCornerRules =
    {
        new(CardinalDirection.Down, CardinalDirection.Left, CornerDirection.UpLeft),
        new(CardinalDirection.Down, CardinalDirection.Right, CornerDirection.UpRight),
        new(CardinalDirection.Left, CardinalDirection.Down, CornerDirection.DownRight),
        new(CardinalDirection.Left, CardinalDirection.Up, CornerDirection.UpRight),
        new(CardinalDirection.Up, CardinalDirection.Left, CornerDirection.DownLeft),
        new(CardinalDirection.Up, CardinalDirection.Right, CornerDirection.DownRight),
        new(CardinalDirection.Right, CardinalDirection.Down, CornerDirection.DownLeft),
        new(CardinalDirection.Right, CardinalDirection.Up, CornerDirection.UpLeft)
    };

    public IReadOnlyDictionary<Vector2Int, IReadOnlyCollection<HullPlacement>> Calculate(
        IEnumerable<Vector2Int> occupiedTiles,
        Vector2Int gridSize,
        int variationSeed = 0)
    {
        if (occupiedTiles == null)
            throw new ArgumentNullException(nameof(occupiedTiles));

        if (gridSize.x <= 0 || gridSize.y <= 0)
            throw new ArgumentOutOfRangeException(nameof(gridSize), "그리드 크기는 0보다 커야 합니다.");

        HashSet<Vector2Int> occupied = ValidateAndCollectTiles(occupiedTiles, gridSize);
        Dictionary<Vector2Int, HashSet<HullPlacement>> placements = new();

        // 직선과 두 모서리 규칙의 순차 적용
        AddStraightPlacements(occupied, gridSize, variationSeed, placements);
        AddOuterCornerPlacements(occupied, gridSize, placements);
        AddInnerCornerPlacements(occupied, gridSize, placements);

        return CreateReadOnlyResult(placements);
    }

    private static HashSet<Vector2Int> ValidateAndCollectTiles(
        IEnumerable<Vector2Int> occupiedTiles,
        Vector2Int gridSize)
    {
        HashSet<Vector2Int> occupied = new();

        foreach (Vector2Int tile in occupiedTiles)
        {
            if (!IsInBounds(tile, gridSize))
                throw new ArgumentOutOfRangeException(nameof(occupiedTiles), $"그리드 범위를 벗어난 타일: {tile}");

            occupied.Add(tile);
        }

        return occupied;
    }

    private static void AddStraightPlacements(
        HashSet<Vector2Int> occupied,
        Vector2Int gridSize,
        int variationSeed,
        Dictionary<Vector2Int, HashSet<HullPlacement>> placements)
    {
        foreach (Vector2Int tile in occupied)
        {
            foreach (CardinalDirection direction in CardinalDirections)
            {
                Vector2Int candidate = tile + GetOffset(direction);

                if (!CanPlaceAt(candidate, occupied, gridSize))
                    continue;

                CardinalDirection facing = GetOpposite(direction);
                bool useVariation = ShouldUseVariation(candidate, facing, variationSeed);

                AddPlacement(placements, candidate, HullPlacement.CreateStraight(facing, useVariation));
            }
        }
    }

    private static void AddOuterCornerPlacements(
        HashSet<Vector2Int> occupied,
        Vector2Int gridSize,
        Dictionary<Vector2Int, HashSet<HullPlacement>> placements)
    {
        foreach (Vector2Int tile in occupied)
        {
            foreach (OuterCornerRule rule in OuterCornerRules)
            {
                Vector2Int candidate = tile + GetOffset(rule.Corner);

                if (!CanPlaceAt(candidate, occupied, gridSize))
                    continue;

                // 후보 양옆이 비어 있을 때 외부 모서리 성립
                Vector2Int firstNeighbor = candidate + GetOffset(rule.FirstClearDirection);
                Vector2Int secondNeighbor = candidate + GetOffset(rule.SecondClearDirection);

                if (!CanPlaceAt(firstNeighbor, occupied, gridSize) ||
                    !CanPlaceAt(secondNeighbor, occupied, gridSize))
                    continue;

                CornerDirection facing = GetOpposite(rule.Corner);
                AddPlacement(placements, candidate, HullPlacement.CreateCorner(HullType.OuterCorner, facing));
            }
        }
    }

    private static void AddInnerCornerPlacements(
        HashSet<Vector2Int> occupied,
        Vector2Int gridSize,
        Dictionary<Vector2Int, HashSet<HullPlacement>> placements)
    {
        foreach (Vector2Int tile in occupied)
        {
            foreach (InnerCornerRule rule in InnerCornerRules)
            {
                Vector2Int candidate = tile + GetOffset(rule.EmptyDirection);

                if (!CanPlaceAt(candidate, occupied, gridSize))
                    continue;

                // 빈 후보의 직교 이웃이 방과 연결된 경우 내부 모서리 성립
                Vector2Int connectedTile = candidate + GetOffset(rule.ConnectedDirection);

                if (!IsInBounds(connectedTile, gridSize) || !occupied.Contains(connectedTile))
                    continue;

                AddPlacement(
                    placements,
                    candidate,
                    HullPlacement.CreateCorner(HullType.InnerCorner, rule.Corner)
                );
            }
        }
    }

    private static void AddPlacement(
        Dictionary<Vector2Int, HashSet<HullPlacement>> placements,
        Vector2Int position,
        HullPlacement placement)
    {
        if (!placements.TryGetValue(position, out HashSet<HullPlacement> tilePlacements))
        {
            tilePlacements = new HashSet<HullPlacement>();
            placements.Add(position, tilePlacements);
        }

        tilePlacements.Add(placement);
    }

    private static IReadOnlyDictionary<Vector2Int, IReadOnlyCollection<HullPlacement>> CreateReadOnlyResult(
        Dictionary<Vector2Int, HashSet<HullPlacement>> placements)
    {
        Dictionary<Vector2Int, IReadOnlyCollection<HullPlacement>> result = new(placements.Count);

        foreach (KeyValuePair<Vector2Int, HashSet<HullPlacement>> pair in placements)
        {
            HullPlacement[] tilePlacements = new HullPlacement[pair.Value.Count];
            pair.Value.CopyTo(tilePlacements);
            result.Add(pair.Key, Array.AsReadOnly(tilePlacements));
        }

        return new ReadOnlyDictionary<Vector2Int, IReadOnlyCollection<HullPlacement>>(result);
    }

    private static bool CanPlaceAt(Vector2Int position, HashSet<Vector2Int> occupied, Vector2Int gridSize)
    {
        return IsInBounds(position, gridSize) && !occupied.Contains(position);
    }

    private static bool IsInBounds(Vector2Int position, Vector2Int gridSize)
    {
        return position.x >= 0 && position.y >= 0 && position.x < gridSize.x && position.y < gridSize.y;
    }

    private static Vector2Int GetOffset(CardinalDirection direction)
    {
        return CardinalOffsets[(int)direction];
    }

    private static Vector2Int GetOffset(CornerDirection direction)
    {
        return CornerOffsets[(int)direction];
    }

    private static CardinalDirection GetOpposite(CardinalDirection direction)
    {
        return direction switch
        {
            CardinalDirection.Down => CardinalDirection.Up,
            CardinalDirection.Left => CardinalDirection.Right,
            CardinalDirection.Up => CardinalDirection.Down,
            CardinalDirection.Right => CardinalDirection.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    private static CornerDirection GetOpposite(CornerDirection direction)
    {
        return direction switch
        {
            CornerDirection.DownLeft => CornerDirection.UpRight,
            CornerDirection.UpLeft => CornerDirection.DownRight,
            CornerDirection.UpRight => CornerDirection.DownLeft,
            CornerDirection.DownRight => CornerDirection.UpLeft,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    private static bool ShouldUseVariation(
        Vector2Int position,
        CardinalDirection direction,
        int variationSeed)
    {
        unchecked
        {
            // 위치·방향·seed가 같으면 동일한 변형 선택
            uint hash = (uint)variationSeed;
            hash ^= (uint)position.x * 0x9E3779B9u;
            hash ^= (uint)position.y * 0x85EBCA6Bu;
            hash ^= (uint)direction * 0xC2B2AE35u;
            hash ^= hash >> 16;

            return hash % 100u < StraightVariationPercent;
        }
    }

    public enum HullType
    {
        Straight,
        OuterCorner,
        InnerCorner
    }

    public enum CardinalDirection
    {
        Down,
        Left,
        Up,
        Right
    }

    public enum CornerDirection
    {
        DownLeft,
        UpLeft,
        UpRight,
        DownRight
    }

    public readonly struct HullPlacement : IEquatable<HullPlacement>
    {
        private const int StraightVariationOffset = 4;
        private const int OuterCornerOffset = 8;
        private const int InnerCornerOffset = 12;

        public HullType Type { get; }
        public int DirectionIndex { get; }
        public bool UseVariation { get; }

        public int SpriteIndex => Type switch
        {
            HullType.Straight => DirectionIndex + (UseVariation ? StraightVariationOffset : 0),
            HullType.OuterCorner => DirectionIndex + OuterCornerOffset,
            HullType.InnerCorner => DirectionIndex + InnerCornerOffset,
            _ => throw new ArgumentOutOfRangeException()
        };

        private HullPlacement(HullType type, int directionIndex, bool useVariation)
        {
            Type = type;
            DirectionIndex = directionIndex;
            UseVariation = useVariation;
        }

        public static HullPlacement CreateStraight(CardinalDirection direction, bool useVariation)
        {
            return new HullPlacement(HullType.Straight, (int)direction, useVariation);
        }

        public static HullPlacement CreateCorner(HullType type, CornerDirection direction)
        {
            if (type != HullType.OuterCorner && type != HullType.InnerCorner)
                throw new ArgumentOutOfRangeException(nameof(type), type, "모서리 형태만 사용할 수 있습니다.");

            return new HullPlacement(type, (int)direction, false);
        }

        public bool Equals(HullPlacement other)
        {
            return Type == other.Type &&
                DirectionIndex == other.DirectionIndex &&
                UseVariation == other.UseVariation;
        }

        public override bool Equals(object obj)
        {
            return obj is HullPlacement other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Type;
                hashCode = (hashCode * 397) ^ DirectionIndex;
                hashCode = (hashCode * 397) ^ UseVariation.GetHashCode();
                return hashCode;
            }
        }
    }

    private readonly struct OuterCornerRule
    {
        public CornerDirection Corner { get; }
        public CardinalDirection FirstClearDirection { get; }
        public CardinalDirection SecondClearDirection { get; }

        public OuterCornerRule(
            CornerDirection corner,
            CardinalDirection firstClearDirection,
            CardinalDirection secondClearDirection)
        {
            Corner = corner;
            FirstClearDirection = firstClearDirection;
            SecondClearDirection = secondClearDirection;
        }
    }

    private readonly struct InnerCornerRule
    {
        public CardinalDirection EmptyDirection { get; }
        public CardinalDirection ConnectedDirection { get; }
        public CornerDirection Corner { get; }

        public InnerCornerRule(
            CardinalDirection emptyDirection,
            CardinalDirection connectedDirection,
            CornerDirection corner)
        {
            EmptyDirection = emptyDirection;
            ConnectedDirection = connectedDirection;
            Corner = corner;
        }
    }
}
