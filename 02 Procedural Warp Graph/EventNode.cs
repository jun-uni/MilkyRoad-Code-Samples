using System.Collections.Generic;
using UnityEngine;

public class EventNode : MonoBehaviour
{
    [SerializeField] private int levelIndex;
    [SerializeField] private int nodeIndex;
    [SerializeField] private bool isDangerous;

    // 다음 레이어로 이어지는 노드 목록
    private readonly List<EventNode> nextNodes = new();

    public int LevelIndex => levelIndex;
    public int NodeIndex => nodeIndex;
    public IReadOnlyList<EventNode> NextNodes => nextNodes;
    public bool IsDangerous => isDangerous;

    public void SetNodeData(int level, int index, bool dangerous = false)
    {
        levelIndex = level;
        nodeIndex = index;
        isDangerous = dangerous;
    }

    public void AddNextNode(EventNode node)
    {
        if (node != null && !nextNodes.Contains(node)) nextNodes.Add(node);
    }

    public void ClearConnections()
    {
        nextNodes.Clear();
    }
}
