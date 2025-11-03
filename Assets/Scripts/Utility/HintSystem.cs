using UnityEngine;
using UnityEngine.Tilemaps;

public class HintSystem : MonoBehaviour
{
    [Header("Refs")]
    // TryFindAnySwapHint를 가진 보드
    [SerializeField] private TileBoardManager board;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Camera cam;

    [Header("Idle/Hints")]
    [SerializeField, Min(0f)] private float idleSeconds = 5f;
    [SerializeField] private Sprite anchorSprite;
    [SerializeField] private string sortingLayer = "Hints";
    [SerializeField] private int sortingOrder = 200;

    [Header("Nudge (moveFrom 젬 왕복)")]
    // 왕복 속도
    [SerializeField, Min(0f)]
    private float nudgeSpeed = 3.0f;
    // 이동 거리(월드)
    [SerializeField, Min(0f)]
    private float nudgeDistance = 0.08f;

    private float _idleTimer;
    private GameObject _anchorA, _anchorB;
    private bool _shown;

    // 왕복 대상
    private Transform _wobbleGem;
    private Vector3 _wobbleBasePos;
    // 정규화된 방향 (to - from)
    private Vector3 _wobbleDir;
    // 토글 대상
    [SerializeField] private bool masterEnabled = true;


    void Awake()
    {
        if (!cam) cam = Camera.main;
        _anchorA = MakeMarker("HintAnchorA");
        _anchorB = MakeMarker("HintAnchorB");
        HideAll();
    }

    void Update()
    {
        if (!masterEnabled)
        { _idleTimer = 0f; if (_shown) HideAll(); return; }

        // 입력/보드 동작 중엔 힌트 숨김 & 타이머 리셋
        if (HasAnyUserAction() || (board && !board.moveCheck))
        {
            _idleTimer = 0f;
            if (_shown) HideAll();
            return;
        }

        _idleTimer += Time.deltaTime;

        // 5초 경과 시 힌트 탐색
        if (!_shown && _idleTimer >= idleSeconds)
        {
            if (board != null && board.TryFindHintDetailed(
                    out var from, out var to,
                    out var anchorCellA, out var anchorCellB))
            {
                // 앵커 스프라이트 배치
                var aPos = WorldCenterOf(anchorCellA);
                var bPos = WorldCenterOf(anchorCellB);
                ShowAnchors(aPos, bPos);

                // 왕복할 젬 (from 칸의 젬)
                var gem = board.GetGemMap(from);
                if (gem != null)
                {
                    _wobbleGem = gem.transform;
                    _wobbleBasePos = _wobbleGem.position;
                    _wobbleDir = (WorldCenterOf(to) - WorldCenterOf(from)).normalized;
                }
                else
                {
                    _wobbleGem = null;
                }
            }
            else
            {
                _idleTimer = 0f; // 힌트가 없으면 다시 기다렸다가 재시도
            }
        }

        // 표시 중이면 왕복 적용
        if (_shown)
        {
            if (_wobbleGem != null)
            {
                float s = Mathf.Sin(Time.time * nudgeSpeed);
                _wobbleGem.position = _wobbleBasePos + _wobbleDir * (s * nudgeDistance);
            }
        }
    }
    public void SetMasterEnabled(bool on)
    {
        masterEnabled = on;
        if (!masterEnabled) HideAll();
    }
    private bool HasAnyUserAction()
    {
        if (Input.anyKeyDown) return true;
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
        return false;
    }

    private GameObject MakeMarker(string name)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = anchorSprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;
        sr.color = Color.white;
        go.transform.localScale = Vector3.one;
        go.SetActive(false);
        return go;
    }

    private void ShowAnchors(Vector3 a, Vector3 b)
    {
        _anchorA.transform.position = a;
        _anchorB.transform.position = b;
        _anchorA.SetActive(true);
        _anchorB.SetActive(true);
        _shown = true;
    }

    private void HideAll()
    {
        if (_anchorA) _anchorA.SetActive(false);
        if (_anchorB) _anchorB.SetActive(false);
        if (_wobbleGem) _wobbleGem.position = _wobbleBasePos; 
        _wobbleGem = null;
        _shown = false;
    }

    private Vector3 WorldCenterOf(Vector3Int cell)
    {
        var w = tilemap.CellToWorld(cell) + tilemap.tileAnchor;
        return new Vector3(w.x, w.y, tilemap.transform.position.z);
    }
}
