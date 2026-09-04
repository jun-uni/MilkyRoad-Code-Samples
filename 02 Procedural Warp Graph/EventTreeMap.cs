using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 생성된 워프 그래프를 Unity UI 노드와 연결선으로 변환하는 컨트롤러
public sealed class EventTreeMap : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform treeContainer;
    [SerializeField] private EventNode eventNodePrefab;
    [SerializeField] private RectTransform connectionLinePrefab;

    [Header("Map Settings")]
    [SerializeField, Min(1)] private int minNodesPerLayer = 1;
    [SerializeField, Min(1)] private int maxNodesPerLayer = 4;
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int fixedSeed = 17;

    [Header("Node Colors")]
    [SerializeField] private Color startNodeColor = Color.green;
    [SerializeField] private Color endNodeColor = Color.red;
    [SerializeField] private Color pirateNodeColor = new(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color spaceStationNodeColor = new(0.2f, 0.6f, 0.8f);
    [SerializeField] private Color randomEventNodeColor = new(0.8f, 0.6f, 0.8f);
    [SerializeField] private Color dangerNodeColor = new(0.6f, 0.2f, 0.8f);

    public event Action<EventNode> OnNodeSelected;

    private readonly List<EventNode> spawnedNodes = new();
    private readonly List<IReadOnlyList<EventNode>> nodeLayers = new();
    private readonly WarpGraphLineView lineView = new();
    private bool[] layerDangerInfo = Array.Empty<bool>();

    public void Initialize()
    {
        ClearCurrentTree();
    }

    public void GenerateTreeFromPath(List<Vector2> pathNodes, List<bool> dangerInfo = null)
    {
        if (pathNodes == null || pathNodes.Count < 2)
        {
            Debug.LogError("시작과 도착을 포함한 경로 정보가 필요합니다.");
            return;
        }

        layerDangerInfo = CopyDangerInfo(dangerInfo, pathNodes.Count);
        GenerateImprovedTree(pathNodes.Count);
    }

    public void GenerateImprovedTree(int layers)
    {
        if (layers < 2)
        {
            Debug.LogError("시작과 도착을 포함한 두 개 이상의 레이어가 필요합니다.");
            return;
        }

        if (!HasValidReferences())
            return;

        if (layerDangerInfo.Length != layers)
            layerDangerInfo = new bool[layers];

        int minimum = Mathf.Max(1, minNodesPerLayer);
        int maximum = Mathf.Max(minimum, maxNodesPerLayer);
        int seed = useFixedSeed ? fixedSeed : Environment.TickCount ^ GetInstanceID();
        WarpGraphGenerator generator = new(minimum, maximum);
        WarpGraphData graph = generator.Generate(layers, layerDangerInfo, seed);

        // 그래프 계산 결과를 노드와 연결선 뷰로 변환
        ClearCurrentTree();
        CreateNodeViews(graph);
        ApplyConnections(graph.Edges);
        lineView.Rebuild(treeContainer, connectionLinePrefab, nodeLayers, graph.Edges);
    }

    private void CreateNodeViews(WarpGraphData graph)
    {
        for (int layerIndex = 0; layerIndex < graph.Layers.Count; layerIndex++)
        {
            IReadOnlyList<WarpNodeData> layerData = graph.Layers[layerIndex];
            List<EventNode> layerViews = new(layerData.Count);

            foreach (WarpNodeData nodeData in layerData)
            {
                EventNode node = Instantiate(eventNodePrefab, treeContainer);
                RectTransform nodeTransform = (RectTransform)node.transform;

                nodeTransform.anchoredPosition = CalculateNodePosition(
                    nodeData.LayerIndex,
                    graph.Layers.Count,
                    nodeData.NodeIndex,
                    layerData.Count
                );
                node.SetNodeData(nodeData.LayerIndex, nodeData.NodeIndex, nodeData.IsDangerous);
                ApplyNodeStyle(node, nodeData.Type, nodeData.IsDangerous);
                BindSelection(node);

                layerViews.Add(node);
                spawnedNodes.Add(node);
            }

            nodeLayers.Add(layerViews);
        }
    }

    private Vector2 CalculateNodePosition(int layerIndex, int layerCount, int nodeIndex, int nodeCount)
    {
        float halfWidth = treeContainer.rect.width * 0.5f;
        float halfHeight = treeContainer.rect.height * 0.5f;

        // 시작과 도착은 양끝, 중간 노드는 높이에 균등 배치
        float layerRatio = (float)layerIndex / (layerCount - 1);
        float x = Mathf.Lerp(-halfWidth + 50f, halfWidth - 50f, layerRatio);

        if (layerIndex == 0)
            x = -halfWidth + 20f;
        else if (layerIndex == layerCount - 1)
            x = halfWidth - 20f;

        float y = nodeCount == 1
            ? 0f
            : Mathf.Lerp(
                -halfHeight * 0.8f + 20f,
                halfHeight * 0.8f - 20f,
                (float)nodeIndex / (nodeCount - 1)
            );

        return new Vector2(x, y);
    }

    private void ApplyConnections(IReadOnlyList<WarpGraphEdge> edges)
    {
        foreach (WarpGraphEdge edge in edges)
        {
            EventNode source = nodeLayers[edge.FromLayer][edge.FromNode];
            EventNode target = nodeLayers[edge.ToLayer][edge.ToNode];
            source.AddNextNode(target);
        }
    }

    private void ApplyNodeStyle(EventNode node, WarpNodeType type, bool isDangerous)
    {
        Image image = node.GetComponent<Image>();

        if (image != null)
            image.color = isDangerous ? dangerNodeColor : GetNodeColor(type);

        string dangerPrefix = isDangerous ? "Danger_" : string.Empty;
        node.name = $"{dangerPrefix}Node_{node.LevelIndex}_{node.NodeIndex}_{type}";
    }

    private Color GetNodeColor(WarpNodeType type)
    {
        switch (type)
        {
            case WarpNodeType.Start:
                return startNodeColor;
            case WarpNodeType.End:
                return endNodeColor;
            case WarpNodeType.Pirate:
                return pirateNodeColor;
            case WarpNodeType.SpaceStation:
                return spaceStationNodeColor;
            case WarpNodeType.RandomEvent:
                return randomEventNodeColor;
            default:
                return Color.white;
        }
    }

    private void BindSelection(EventNode node)
    {
        Button button = node.GetComponent<Button>();

        if (button == null)
        {
            Debug.LogWarning($"{node.name}에 Button이 없어 선택 입력을 연결하지 못했습니다.");
            return;
        }

        button.onClick.AddListener(() => OnNodeSelected?.Invoke(node));
    }

    private void ClearCurrentTree()
    {
        lineView.Clear();

        foreach (EventNode node in spawnedNodes)
            if (node != null)
                Destroy(node.gameObject);

        spawnedNodes.Clear();
        nodeLayers.Clear();
    }

    private bool HasValidReferences()
    {
        if (treeContainer != null &&
            eventNodePrefab != null &&
            eventNodePrefab.transform is RectTransform &&
            connectionLinePrefab != null)
            return true;

        Debug.LogError("트리 컨테이너, RectTransform 기반 노드 프리팹, 연결선 프리팹을 지정해야 합니다.");
        return false;
    }

    private static bool[] CopyDangerInfo(IReadOnlyList<bool> source, int layerCount)
    {
        bool[] result = new bool[layerCount];

        if (source == null)
            return result;

        int copyCount = Math.Min(source.Count, layerCount);
        for (int index = 0; index < copyCount; index++)
            result[index] = source[index];

        return result;
    }
}
