using System;
using System.Collections.Generic;

public enum WarpNodeType
{
    Start,
    Pirate,
    SpaceStation,
    RandomEvent,
    End
}

public readonly struct WarpNodeData
{
    public int LayerIndex { get; }
    public int NodeIndex { get; }
    public WarpNodeType Type { get; }
    public bool IsDangerous { get; }

    public WarpNodeData(int layerIndex, int nodeIndex, WarpNodeType type, bool isDangerous)
    {
        LayerIndex = layerIndex;
        NodeIndex = nodeIndex;
        Type = type;
        IsDangerous = isDangerous;
    }
}

public readonly struct WarpGraphEdge : IEquatable<WarpGraphEdge>
{
    public int FromLayer { get; }
    public int FromNode { get; }
    public int ToLayer { get; }
    public int ToNode { get; }

    public WarpGraphEdge(int fromLayer, int fromNode, int toLayer, int toNode)
    {
        FromLayer = fromLayer;
        FromNode = fromNode;
        ToLayer = toLayer;
        ToNode = toNode;
    }

    public bool Equals(WarpGraphEdge other)
    {
        return FromLayer == other.FromLayer &&
               FromNode == other.FromNode &&
               ToLayer == other.ToLayer &&
               ToNode == other.ToNode;
    }

    public override bool Equals(object obj)
    {
        return obj is WarpGraphEdge other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = FromLayer;
            hash = (hash * 397) ^ FromNode;
            hash = (hash * 397) ^ ToLayer;
            return (hash * 397) ^ ToNode;
        }
    }
}

public sealed class WarpGraphData
{
    public IReadOnlyList<IReadOnlyList<WarpNodeData>> Layers { get; }
    public IReadOnlyList<WarpGraphEdge> Edges { get; }

    public WarpGraphData(
        IReadOnlyList<IReadOnlyList<WarpNodeData>> layers,
        IReadOnlyList<WarpGraphEdge> edges)
    {
        Layers = layers;
        Edges = edges;
    }
}

// 레이어별 노드와 인접 레이어 간 경로를 생성하는 계산기
public sealed class WarpGraphGenerator
{
    private const double AdditionalConnectionChance = 0.35;

    private readonly int minNodesPerLayer;
    private readonly int maxNodesPerLayer;

    public WarpGraphGenerator(int minNodesPerLayer, int maxNodesPerLayer)
    {
        if (minNodesPerLayer < 1)
            throw new ArgumentOutOfRangeException(nameof(minNodesPerLayer));
        if (maxNodesPerLayer < minNodesPerLayer)
            throw new ArgumentOutOfRangeException(nameof(maxNodesPerLayer));

        this.minNodesPerLayer = minNodesPerLayer;
        this.maxNodesPerLayer = maxNodesPerLayer;
    }

    public WarpGraphData Generate(int layerCount, IReadOnlyList<bool> dangerInfo, int seed)
    {
        if (layerCount < 2)
            throw new ArgumentOutOfRangeException(nameof(layerCount), "시작과 도착을 포함한 두 개 이상의 레이어가 필요합니다.");

        Random random = new(seed);
        List<IReadOnlyList<WarpNodeData>> layers = CreateLayers(layerCount, dangerInfo, random);
        List<WarpGraphEdge> edges = CreateEdges(layers, random);

        return new WarpGraphData(layers, edges);
    }

    private List<IReadOnlyList<WarpNodeData>> CreateLayers(
        int layerCount,
        IReadOnlyList<bool> dangerInfo,
        Random random)
    {
        List<IReadOnlyList<WarpNodeData>> layers = new(layerCount);

        for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
        {
            int nodeCount = DetermineNodeCount(layerIndex, layerCount, random);
            bool isDangerous = dangerInfo != null &&
                               layerIndex < dangerInfo.Count &&
                               dangerInfo[layerIndex];
            List<WarpNodeData> nodes = new(nodeCount);

            for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
            {
                nodes.Add(new WarpNodeData(
                    layerIndex,
                    nodeIndex,
                    DetermineNodeType(layerIndex, layerCount, random),
                    isDangerous
                ));
            }

            layers.Add(nodes);
        }

        return layers;
    }

    private int DetermineNodeCount(int layerIndex, int layerCount, Random random)
    {
        if (layerIndex == 0 || layerIndex == layerCount - 1)
            return 1;

        int distanceFromEdge = Math.Min(layerIndex, layerCount - 1 - layerIndex);
        int layerMaximum = distanceFromEdge == 1
            ? Math.Min(maxNodesPerLayer, 3)
            : maxNodesPerLayer;
        int layerMinimum = Math.Min(minNodesPerLayer, layerMaximum);

        return random.Next(layerMinimum, layerMaximum + 1);
    }

    private static WarpNodeType DetermineNodeType(int layerIndex, int layerCount, Random random)
    {
        if (layerIndex == 0)
            return WarpNodeType.Start;
        if (layerIndex == layerCount - 1)
            return WarpNodeType.End;

        double value = random.NextDouble();

        if (layerIndex == layerCount - 2)
        {
            if (value < 0.7)
                return WarpNodeType.SpaceStation;
            if (value < 0.85)
                return WarpNodeType.RandomEvent;
            return WarpNodeType.Pirate;
        }

        if (value < 0.5)
            return WarpNodeType.Pirate;
        if (value < 0.8)
            return WarpNodeType.SpaceStation;
        return WarpNodeType.RandomEvent;
    }

    private static List<WarpGraphEdge> CreateEdges(
        IReadOnlyList<IReadOnlyList<WarpNodeData>> layers,
        Random random)
    {
        HashSet<WarpGraphEdge> edges = new();

        for (int layerIndex = 0; layerIndex < layers.Count - 1; layerIndex++)
        {
            int currentCount = layers[layerIndex].Count;
            int nextCount = layers[layerIndex + 1].Count;
            bool[] hasIncomingConnection = new bool[nextCount];

            // 진출 경로 생성 후 누락된 진입 경로 보완
            AddOutgoingConnections(
                layerIndex,
                currentCount,
                nextCount,
                random,
                edges,
                hasIncomingConnection
            );
            AddMissingIncomingConnections(
                layerIndex,
                currentCount,
                hasIncomingConnection,
                edges
            );
        }

        List<WarpGraphEdge> orderedEdges = new(edges);
        orderedEdges.Sort(CompareEdges);
        return orderedEdges;
    }

    private static void AddOutgoingConnections(
        int layerIndex,
        int currentCount,
        int nextCount,
        Random random,
        ISet<WarpGraphEdge> edges,
        IList<bool> hasIncomingConnection)
    {
        for (int sourceIndex = 0; sourceIndex < currentCount; sourceIndex++)
        {
            GetConnectionRange(sourceIndex, currentCount, nextCount, out int start, out int end);

            // 각 노드의 최소 진출 경로
            int primaryTarget = random.Next(start, end + 1);
            edges.Add(new WarpGraphEdge(layerIndex, sourceIndex, layerIndex + 1, primaryTarget));
            hasIncomingConnection[primaryTarget] = true;

            // 같은 연결 구간 안의 추가 분기
            for (int targetIndex = start; targetIndex <= end; targetIndex++)
                if (targetIndex != primaryTarget && random.NextDouble() < AdditionalConnectionChance)
                {
                    edges.Add(new WarpGraphEdge(layerIndex, sourceIndex, layerIndex + 1, targetIndex));
                    hasIncomingConnection[targetIndex] = true;
                }
        }
    }

    private static void AddMissingIncomingConnections(
        int layerIndex,
        int currentCount,
        IReadOnlyList<bool> hasIncomingConnection,
        ISet<WarpGraphEdge> edges)
    {
        int nextCount = hasIncomingConnection.Count;
        for (int targetIndex = 0; targetIndex < nextCount; targetIndex++)
        {
            if (hasIncomingConnection[targetIndex])
                continue;

            int sourceIndex = MapIndex(targetIndex, nextCount, currentCount);
            edges.Add(new WarpGraphEdge(layerIndex, sourceIndex, layerIndex + 1, targetIndex));
        }
    }

    private static void GetConnectionRange(
        int sourceIndex,
        int sourceCount,
        int targetCount,
        out int start,
        out int end)
    {
        // source 위치에 대응하는 target 인덱스 구간 계산
        start = sourceIndex * targetCount / sourceCount;
        end = ((sourceIndex + 1) * targetCount + sourceCount - 1) / sourceCount - 1;

        start = Math.Min(start, targetCount - 1);
        end = Math.Max(start, Math.Min(end, targetCount - 1));
    }

    private static int MapIndex(int index, int fromCount, int toCount)
    {
        if (fromCount <= 1 || toCount <= 1)
            return 0;

        int numerator = index * (toCount - 1) * 2 + fromCount - 1;
        int denominator = (fromCount - 1) * 2;
        return numerator / denominator;
    }

    private static int CompareEdges(WarpGraphEdge left, WarpGraphEdge right)
    {
        int comparison = left.FromLayer.CompareTo(right.FromLayer);
        if (comparison != 0)
            return comparison;

        comparison = left.FromNode.CompareTo(right.FromNode);
        if (comparison != 0)
            return comparison;

        return left.ToNode.CompareTo(right.ToNode);
    }
}
