#if UNITY_INCLUDE_TESTS

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class OuterHullPlacementCalculatorTests
{
    private readonly OuterHullPlacementCalculator calculator = new();

    [Test]
    public void SingleTile_CreatesFourStraightsAndFourOuterCorners()
    {
        IReadOnlyDictionary<Vector2Int, IReadOnlyCollection<OuterHullPlacementCalculator.HullPlacement>> result =
            calculator.Calculate(new[] { new Vector2Int(2, 2) }, new Vector2Int(5, 5));

        List<OuterHullPlacementCalculator.HullPlacement> placements = result.Values.SelectMany(value => value).ToList();

        Assert.That(
            placements.Count(placement => placement.Type == OuterHullPlacementCalculator.HullType.Straight),
            Is.EqualTo(4)
        );
        Assert.That(
            placements.Count(placement => placement.Type == OuterHullPlacementCalculator.HullType.OuterCorner),
            Is.EqualTo(4)
        );
    }

    [Test]
    public void SharedEdge_DoesNotCreateHullOnOccupiedTile()
    {
        Vector2Int firstRoom = new(1, 1);
        Vector2Int secondRoom = new(2, 1);

        IReadOnlyDictionary<Vector2Int, IReadOnlyCollection<OuterHullPlacementCalculator.HullPlacement>> result =
            calculator.Calculate(new[] { firstRoom, secondRoom }, new Vector2Int(4, 3));

        Assert.That(result.ContainsKey(firstRoom), Is.False);
        Assert.That(result.ContainsKey(secondRoom), Is.False);
    }

    [Test]
    public void LShape_CreatesOneInnerCornerAtConcaveTile()
    {
        Vector2Int concaveTile = new(1, 1);
        Vector2Int[] occupied =
        {
            new(0, 0),
            new(0, 1),
            new(1, 0)
        };

        IReadOnlyDictionary<Vector2Int, IReadOnlyCollection<OuterHullPlacementCalculator.HullPlacement>> result =
            calculator.Calculate(occupied, new Vector2Int(3, 3));

        OuterHullPlacementCalculator.HullPlacement expected =
            OuterHullPlacementCalculator.HullPlacement.CreateCorner(
                OuterHullPlacementCalculator.HullType.InnerCorner,
                OuterHullPlacementCalculator.CornerDirection.DownLeft
            );

        Assert.That(result[concaveTile], Does.Contain(expected));
        Assert.That(
            result[concaveTile].Count(placement =>
                placement.Type == OuterHullPlacementCalculator.HullType.InnerCorner),
            Is.EqualTo(1)
        );
    }

    [Test]
    public void GridBoundary_DoesNotCreatePlacementOutsideGrid()
    {
        Vector2Int gridSize = new(2, 2);

        IReadOnlyDictionary<Vector2Int, IReadOnlyCollection<OuterHullPlacementCalculator.HullPlacement>> result =
            calculator.Calculate(new[] { Vector2Int.zero }, gridSize);

        Assert.That(
            result.Keys.All(position =>
                position.x >= 0 && position.y >= 0 && position.x < gridSize.x && position.y < gridSize.y),
            Is.True
        );
    }

    [Test]
    public void SpriteIndex_UsesDocumentedRanges()
    {
        OuterHullPlacementCalculator.HullPlacement straight =
            OuterHullPlacementCalculator.HullPlacement.CreateStraight(
                OuterHullPlacementCalculator.CardinalDirection.Right,
                false
            );
        OuterHullPlacementCalculator.HullPlacement straightVariation =
            OuterHullPlacementCalculator.HullPlacement.CreateStraight(
                OuterHullPlacementCalculator.CardinalDirection.Right,
                true
            );
        OuterHullPlacementCalculator.HullPlacement outerCorner =
            OuterHullPlacementCalculator.HullPlacement.CreateCorner(
                OuterHullPlacementCalculator.HullType.OuterCorner,
                OuterHullPlacementCalculator.CornerDirection.DownRight
            );
        OuterHullPlacementCalculator.HullPlacement innerCorner =
            OuterHullPlacementCalculator.HullPlacement.CreateCorner(
                OuterHullPlacementCalculator.HullType.InnerCorner,
                OuterHullPlacementCalculator.CornerDirection.DownRight
            );

        Assert.That(straight.SpriteIndex, Is.EqualTo(3));
        Assert.That(straightVariation.SpriteIndex, Is.EqualTo(7));
        Assert.That(outerCorner.SpriteIndex, Is.EqualTo(11));
        Assert.That(innerCorner.SpriteIndex, Is.EqualTo(15));
    }

    [Test]
    public void SameSeed_ProducesSameStraightVariations()
    {
        Vector2Int[] occupied =
        {
            new(2, 2),
            new(3, 2),
            new(2, 3)
        };

        IReadOnlyDictionary<Vector2Int, IReadOnlyCollection<OuterHullPlacementCalculator.HullPlacement>> first =
            calculator.Calculate(occupied, new Vector2Int(6, 6), 17);
        IReadOnlyDictionary<Vector2Int, IReadOnlyCollection<OuterHullPlacementCalculator.HullPlacement>> second =
            calculator.Calculate(occupied, new Vector2Int(6, 6), 17);

        Assert.That(second.Keys, Is.EquivalentTo(first.Keys));

        foreach (Vector2Int position in first.Keys)
            Assert.That(second[position], Is.EquivalentTo(first[position]));
    }
}

#endif
