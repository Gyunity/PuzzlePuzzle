using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Linq;

public class TileGemInputHandler : MonoBehaviour
{
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private TileBoardManager boardManager;
    [SerializeField]
    private LayerMask gemLayerMaske;

    private Vector3Int? selectedCell;
    private Vector3 dragStartWorld;
    private const float dragMinPixels = 10f;
    
    
    private void Update()
    {
        // 터치
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId)) return;

            switch (t.phase)
            {
                case TouchPhase.Began:
                    if (TryPickGem(t.position, out var cell, out dragStartWorld))
                        selectedCell = cell;
                    else
                        selectedCell = null;
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (!selectedCell.HasValue) return;

                    var endWorld = ScreenToWorldOnTilePlane(t.position);
                    var endCell = tilemap.WorldToCell(endWorld);

                    // 드래그 거리로 분기
                    if ((t.position - (Vector2)cam.WorldToScreenPoint(dragStartWorld)).sqrMagnitude < dragMinPixels * dragMinPixels)
                    {
                        // 탭 스왑: 손 뗀 셀로
                        if (endCell != selectedCell.Value &&
                            boardManager.TryGetGemAtCell(selectedCell.Value, out _) &&
                            boardManager.TryGetGemAtCell(endCell, out _))
                        {
                            boardManager.TrySwap(selectedCell.Value, endCell);
                        }
                    }
                    else
                    {
                        // 드래그 스왑: 시작 셀의 6이웃 중 드래그 방향에 가장 가까운 셀
                        var neighbor = GetDirectionalNeighborCell(selectedCell.Value, dragStartWorld, endWorld);
                        if (neighbor.HasValue &&
                            boardManager.TryGetGemAtCell(selectedCell.Value, out _) &&
                            boardManager.TryGetGemAtCell(neighbor.Value, out _))
                        {
                            boardManager.TrySwap(selectedCell.Value, neighbor.Value);
                        }
                    }

                    selectedCell = null;
                    break;
            }
            return;
        }

        // 마우스(에디터 편의)
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
            if (TryPickGem(Input.mousePosition, out var cell, out dragStartWorld))
                selectedCell = cell;
            else
                selectedCell = null;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) { selectedCell = null; return; }
            if (!selectedCell.HasValue) return;

            var endWorld = ScreenToWorldOnTilePlane(Input.mousePosition);
            var endCell = tilemap.WorldToCell(endWorld);

            if ((Input.mousePosition - cam.WorldToScreenPoint(dragStartWorld)).sqrMagnitude < dragMinPixels * dragMinPixels)
            {
                if (endCell != selectedCell.Value &&
                    boardManager.TryGetGemAtCell(selectedCell.Value, out _) &&
                    boardManager.TryGetGemAtCell(endCell, out _))
                {
                    boardManager.TrySwap(selectedCell.Value, endCell);
                }
            }
            else
            {
                var neighbor = GetDirectionalNeighborCell(selectedCell.Value, dragStartWorld, endWorld);
                if (neighbor.HasValue &&
                    boardManager.TryGetGemAtCell(selectedCell.Value, out _) &&
                    boardManager.TryGetGemAtCell(neighbor.Value, out _))
                {
                    boardManager.TrySwap(selectedCell.Value, neighbor.Value);
                }
            }

            selectedCell = null;
        }
    }
 

    private Vector3 ScreenToWorldOnTilePlane(Vector3 screenPos)
    {
        float planeZ = tilemap.transform.position.z;
        Vector3 sp = new(
            screenPos.x, screenPos.y,
            cam.orthographic ? cam.nearClipPlane : Mathf.Abs(planeZ - cam.transform.position.z)
        );
        return cam.ScreenToWorldPoint(sp);
    }

    private bool TryPickGem(Vector3 screenPos, out Vector3Int cellOut, out Vector3 worldOut)
    {
        worldOut = ScreenToWorldOnTilePlane(screenPos);

        var col = Physics2D.OverlapPoint(worldOut, gemLayerMaske);
        if (col)
        {
            Debug.Log(col.transform.name);
            var gem = col.GetComponentInParent<Gem>();
            if (gem != null)
            {
                cellOut = tilemap.WorldToCell(gem.transform.position);
                return true;
            }
        }
        cellOut = default;
        return false;
    }

    // 드래그 방향과 가장 일치하는 1칸 이웃 (헥스)
    private Vector3Int? GetDirectionalNeighborCell(Vector3Int startCell, Vector3 startWorld, Vector3 endWorld)
    {
        var dirs = HexDirections.GetNeighbor6(startCell); // parity-aware 버전 권장
        var startCenter = tilemap.CellToWorld(startCell) + tilemap.tileAnchor;

        Vector2 drag = (endWorld - startWorld);
        if (drag.sqrMagnitude < 0.0001f) return null;
        drag.Normalize();

        float bestDot = -1f;
        Vector3Int? best = null;

        foreach (var d in dirs)
        {
            var c = startCell + d;
            if (!tilemap.HasTile(c)) continue;

            var cWorld = tilemap.CellToWorld(c) + tilemap.tileAnchor;
            Vector2 dir = (Vector2)(cWorld - startCenter);
            if (dir.sqrMagnitude < 0.0001f) continue;
            dir.Normalize();

            float dot = Vector2.Dot(drag, dir);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = c;
            }
        }

        // 드래그 방향과 충분히 유사할 때만 승인
        return (bestDot > 0.5f) ? best : null;
    }
    
}
