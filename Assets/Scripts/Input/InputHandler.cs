using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.Tilemaps;

public class InputHandler : MonoBehaviour
{
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private LayerMask gemLayer;
    [SerializeField]
    private TileBoardManager tileBoardManager;

    private Vector3Int selectedCell;
    private Vector3Int swapCell;

    private Vector3Int resetVec = new Vector3Int(99999, 9999, 9999);
    // 현재 프레임에 처리 중인 터치/마우스가 UI 위인지
    bool IsPointerOverUI(int touchId = -1)
    {
        if (EventSystem.current == null) return false;
#if UNITY_EDITOR || UNITY_STANDALONE
        // 마우스
        return EventSystem.current.IsPointerOverGameObject();
#else
        // 터치: 반드시 fingerId로 검사
        return EventSystem.current.IsPointerOverGameObject(touchId);
#endif
    }

    void Awake()
    {
        if (!cam) cam = Camera.main;
        selectedCell = resetVec;
        swapCell = resetVec;
    }

    void Update()
    {
        // 보드가 바쁜 동안 입력 무시
        if (!tileBoardManager.moveCheck) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    // ===== Mouse path (에디터/PC) =====
    void HandleMouse()
    {
        // UI 위면 입력 무시
        if (IsPointerOverUI()) return;

        if (Input.GetMouseButtonDown(0))
        {
            TryPickGemCell(Input.mousePosition, out selectedCell);
        }
        else if (Input.GetMouseButton(0) && selectedCell != resetVec)
        {
            if (TryPickGemCell(Input.mousePosition, out swapCell))
            {
                // 인접하면 스왑 시도 후 선택 해제(중복 스왑 방지)
                if (IsNeighbor(selectedCell, swapCell))
                {
                    tileBoardManager.TrySwap(selectedCell, swapCell);
                    selectedCell = resetVec;
                    swapCell = resetVec;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            selectedCell = resetVec;
            swapCell = resetVec;
        }
    }

    // ===== Touch path (모바일) =====
    void HandleTouch()
    {
        if (Input.touchCount == 0) return;

        // 하나만 처리: 첫 번째 터치
        Touch t = Input.GetTouch(0);

        // UI 위면 무시
        if (IsPointerOverUI(t.fingerId)) return;

        switch (t.phase)
        {
            case TouchPhase.Began:
                TryPickGemCell(t.position, out selectedCell);
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (selectedCell == resetVec) break;
                if (TryPickGemCell(t.position, out swapCell))
                {
                    if (IsNeighbor(selectedCell, swapCell))
                    {
                        tileBoardManager.TrySwap(selectedCell, swapCell);
                        selectedCell = resetVec;
                        swapCell = resetVec;
                    }
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                selectedCell = resetVec;
                swapCell = resetVec;
                break;
        }
    }

    // 두 셀이 6방향 이웃인지 검사
    bool IsNeighbor(Vector3Int a, Vector3Int b)
    {
        foreach (var n in tileBoardManager.Neighbor6(a))
            if (n == b) return true;
        return false;
    }

    // 화면 좌표 → 월드에서 젬 콜라이더 히트 → 해당 셀 반환
    bool TryPickGemCell(Vector3 screenPos, out Vector3Int cell)
    {
        cell = resetVec;

        // 카메라 z 설정: 2D(Ortho)인 경우 z=0으로 충분하지만, 안전하게 마우스/터치 z를 카메라-평면 거리로 보정
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cam.nearClipPlane));
        // 2D OverlapPoint는 Z를 무시하므로 XY만 정확히 맞으면 됨
        var hit = Physics2D.OverlapPoint(wp, gemLayer);
        if (!hit) return false;

        var gem = hit.GetComponentInParent<Gem>();
        if (!gem) return false;

        var c = tilemap.WorldToCell(gem.transform.position);
        // 같은 셀 재선택 방지
        if (c == selectedCell) return false; 
        cell = c;
        return true;
    }
}
