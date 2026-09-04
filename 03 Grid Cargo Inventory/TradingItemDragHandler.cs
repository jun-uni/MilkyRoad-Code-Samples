using System.Collections.Generic;
using UnityEngine;

// TradingItemDragHandler의 격자형 배치 프리뷰 계산 로직 발췌
public class TradingItemDragHandler : MonoBehaviour
{
    private GameObject previewObject;
    private TradingItem originalItem;
    private StorageRoomBase currentTargetStorage;
    private Vector2Int currentGridPosition;
    private Constants.Rotations.Rotation currentRotation;

    private void UpdatePreviewPosition(Vector2 mouseWorldPosition)
    {
        if (previewObject == null || originalItem == null)
            return;

        StorageRoomBase targetStorage = GetStorageUnderMouse(mouseWorldPosition);

        if (targetStorage == null)
        {
            previewObject.transform.position = new Vector3(mouseWorldPosition.x, mouseWorldPosition.y, 0f);
            currentTargetStorage = null;
            return;
        }

        currentTargetStorage = targetStorage;
        currentGridPosition = targetStorage.WorldToGridPosition(mouseWorldPosition);
        UpdatePreviewToGridPosition(targetStorage, currentGridPosition);
    }

    private void UpdatePreviewToGridPosition(StorageRoomBase storage, Vector2Int gridPosition)
    {
        List<Vector2Int> occupiedTiles = storage.GetOccupiedTiles(
            originalItem,
            gridPosition,
            currentRotation
        );

        if (occupiedTiles.Count == 0)
            return;

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (Vector2Int tile in occupiedTiles)
        {
            minX = Mathf.Min(minX, tile.x);
            maxX = Mathf.Max(maxX, tile.x);
            minY = Mathf.Min(minY, tile.y);
            maxY = Mathf.Max(maxY, tile.y);
        }

        // 불규칙 형태가 차지하는 영역의 중앙 계산
        Vector2 itemCenter = new(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f
        );
        Vector2Int storageSize = storage.GetSize();
        Vector2 gridOrigin = new(
            -storageSize.x * Constants.Grids.CellSize * 0.5f,
            -storageSize.y * Constants.Grids.CellSize * 0.5f
        );
        Vector3 localPosition = new(
            gridOrigin.x + (itemCenter.x + 0.5f) * Constants.Grids.CellSize,
            gridOrigin.y + (itemCenter.y + 0.5f) * Constants.Grids.CellSize,
            0f
        );

        previewObject.transform.position = storage.transform.TransformPoint(localPosition);
    }

    private static StorageRoomBase GetStorageUnderMouse(Vector2 mouseWorldPosition)
    {
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPosition);
        return hit != null
            ? hit.GetComponentInParent<StorageRoomBase>()
            : null;
    }
}
