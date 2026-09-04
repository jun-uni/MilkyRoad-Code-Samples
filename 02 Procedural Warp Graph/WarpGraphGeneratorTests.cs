#if UNITY_INCLUDE_TESTS

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class WarpGraphGeneratorTests
{
    private readonly WarpGraphGenerator generator = new(1, 4);

    [Test]
    public void Generate_CreatesSingleStartAndEndNodes()
    {
        WarpGraphData graph = generator.Generate(7, null, 17);

        Assert.That(graph.Layers, Has.Count.EqualTo(7));
        Assert.That(graph.Layers[0], Has.Count.EqualTo(1));
        Assert.That(graph.Layers[0][0].Type, Is.EqualTo(WarpNodeType.Start));
        Assert.That(graph.Layers[6], Has.Count.EqualTo(1));
        Assert.That(graph.Layers[6][0].Type, Is.EqualTo(WarpNodeType.End));
    }

    [Test]
    public void Generate_UsesDangerInformationPerLayer()
    {
        bool[] dangerInfo = { false, true, false, true };

        WarpGraphData graph = generator.Generate(4, dangerInfo, 17);

        for (int layerIndex = 0; layerIndex < graph.Layers.Count; layerIndex++)
            Assert.That(
                graph.Layers[layerIndex].All(node => node.IsDangerous == dangerInfo[layerIndex]),
                Is.True
            );
    }

    [Test]
    public void Generate_GivesEveryNodeAnAdjacentConnection()
    {
        WarpGraphData graph = generator.Generate(9, null, 31);

        for (int layerIndex = 0; layerIndex < graph.Layers.Count; layerIndex++)
        {
            for (int nodeIndex = 0; nodeIndex < graph.Layers[layerIndex].Count; nodeIndex++)
            {
                if (layerIndex > 0)
                    Assert.That(
                        graph.Edges.Any(edge =>
                            edge.ToLayer == layerIndex && edge.ToNode == nodeIndex),
                        Is.True
                    );

                if (layerIndex < graph.Layers.Count - 1)
                    Assert.That(
                        graph.Edges.Any(edge =>
                            edge.FromLayer == layerIndex && edge.FromNode == nodeIndex),
                        Is.True
                    );
            }
        }
    }

    [Test]
    public void Generate_DoesNotCreateCrossingEdgesBetweenAdjacentLayers()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            WarpGraphData graph = generator.Generate(10, null, seed);

            for (int layerIndex = 0; layerIndex < graph.Layers.Count - 1; layerIndex++)
            {
                List<WarpGraphEdge> layerEdges = graph.Edges
                    .Where(edge => edge.FromLayer == layerIndex)
                    .ToList();

                for (int first = 0; first < layerEdges.Count; first++)
                    for (int second = first + 1; second < layerEdges.Count; second++)
                    {
                        WarpGraphEdge left = layerEdges[first];
                        WarpGraphEdge right = layerEdges[second];

                        bool crosses = left.FromNode < right.FromNode && left.ToNode > right.ToNode ||
                                       left.FromNode > right.FromNode && left.ToNode < right.ToNode;

                        Assert.That(crosses, Is.False, $"seed={seed}, layer={layerIndex}");
                    }
            }
        }
    }

    [Test]
    public void Generate_WithSameSeedProducesSameGraph()
    {
        WarpGraphData first = generator.Generate(8, null, 53);
        WarpGraphData second = generator.Generate(8, null, 53);

        Assert.That(second.Edges, Is.EqualTo(first.Edges));
        Assert.That(
            second.Layers.Select(layer => layer.Count),
            Is.EqualTo(first.Layers.Select(layer => layer.Count))
        );
        Assert.That(
            second.Layers.SelectMany(layer => layer).Select(node => node.Type),
            Is.EqualTo(first.Layers.SelectMany(layer => layer).Select(node => node.Type))
        );
    }
}

#endif
