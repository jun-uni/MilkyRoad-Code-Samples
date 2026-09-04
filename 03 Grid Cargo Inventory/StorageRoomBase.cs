using System.Collections.Generic;
using UnityEngine;

// StorageRoomBase의 격자 점유 검사와 좌표 변환 로직 발췌
public abstract class StorageRoomBase : Room<StorageRoomBaseData, StorageRoomBaseData.StorageRoomBaseLevel>
{
    private TradingItem[,] itemGrid;

    public override void Initialize(int level)
    {
        base.Initialize(level);
        roomType = RoomType.Storage;
        itemGrid = new TradingItem[GetSize().y, GetSize().x];
    }

    public abstract bool CanStoreItemType(ItemCategory itemType);

    public bool CanPlaceItem(
        TradingItem item,
        Vector2Int position,
        Constants.Rotations.Rotation rotation)
    {
        if (item == null || itemGrid == null)
            return false;

        List<Vector2Int> occupiedTiles = GetOccupiedTiles(item, position, rotation);
        Vector2Int storageSize = GetSize();

        if (occupiedTiles.Count == 0)
            return false;

        foreach (Vector2Int tile in occupiedTiles)
        {
            // 창고 경계 검사
            if (tile.x < 0 || tile.x >= storageSize.x || tile.y < 0 || tile.y >= storageSize.y)
                return false;

            // 다른 아이템과의 점유 충돌 검사
            TradingItem occupant = itemGrid[tile.y, tile.x];
            if (occupant != null && occupant != item)
                return false;
        }

        return CanStoreItemType(item.GetItemData().type);
    }

    public List<Vector2Int> GetOccupiedTiles(
        TradingItem item,
        Vector2Int position,
        Constants.Rotations.Rotation rotation)
    {
        List<Vector2Int> occupiedTiles = new();
        IReadOnlyList<Vector2Int> shapeOffsets = ItemShape.Instance.GetOccupiedOffsets(
            item.GetItemData().shape,
            (int)rotation
        );

        if (shapeOffsets.Count == 0)
            return occupiedTiles;

        foreach (Vector2Int offset in shapeOffsets)
        {
            occupiedTiles.Add(position + offset);
        }

        return occupiedTiles;
    }

    public Vector2Int WorldToGridPosition(Vector2 worldPosition)
    {
        Vector2Int storageSize = GetSize();
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        Vector2 gridOrigin = new(
            -storageSize.x * Constants.Grids.CellSize * 0.5f,
            -storageSize.y * Constants.Grids.CellSize * 0.5f
        );

        int gridX = Mathf.FloorToInt((localPosition.x - gridOrigin.x) / Constants.Grids.CellSize);
        int gridY = Mathf.FloorToInt((localPosition.y - gridOrigin.y) / Constants.Grids.CellSize);
        return new Vector2Int(gridX, gridY);
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        Vector2Int storageSize = GetSize();
        Vector2 gridOrigin = new(
            -storageSize.x * Constants.Grids.CellSize * 0.5f,
            -storageSize.y * Constants.Grids.CellSize * 0.5f
        );
        Vector3 localPosition = new(
            gridOrigin.x + (gridPosition.x + 0.5f) * Constants.Grids.CellSize,
            gridOrigin.y + (gridPosition.y + 0.5f) * Constants.Grids.CellSize,
            0f
        );

        // 부동소수점 오차가 Transform 위치에 누적되지 않도록 보정
        localPosition.x = Mathf.Round(localPosition.x * 1000f) / 1000f;
        localPosition.y = Mathf.Round(localPosition.y * 1000f) / 1000f;

        return transform.TransformPoint(localPosition);
    }
}
