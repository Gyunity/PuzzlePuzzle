using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static UnityEditor.Searcher.SearcherWindow;
public class TileBoardManager : MonoBehaviour, IBoardReadonly
{
    // t=[0,1]에서 이징 함수들
    private static float EaseInQuad(float t) => t * t;
    private static float EaseOutQuad(float t) => t * (2f - t);


    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private GemFactory gemFactory;
    [SerializeField]
    private TileMatchFinder tileMatchFinder;
    [SerializeField]
    private Transform gemsRoot;
    [SerializeField]
    private GameObject gemCrush;
    [SerializeField]
    private Material gemMaterial;

    private Dictionary<Vector3Int, Gem> gemMap = new Dictionary<Vector3Int, Gem>();


    private GemType[] allTypes;
    public bool moveCheck = true;

    private bool _isResolving;


    //삐에로 
    [SerializeField]
    private Transform jackRoot;
    [SerializeField]
    private JackFactory jackFactory;

    [SerializeField]
    private List<Vector3Int> blockerCells = new();
    private HashSet<Vector3Int> blockers;

    [SerializeField]
    private List<MonoBehaviour> destructionListenerRefs = new();
    private readonly List<IDestructionListener> _destructionListeners = new();
    private void Awake()
    {
        blockers = new HashSet<Vector3Int>(blockerCells);

        // 리스너 캐싱
        _destructionListeners.Clear();
        foreach (var mb in destructionListenerRefs)
            if (mb is IDestructionListener l) _destructionListeners.Add(l);
    }
    public bool IsBlocked(Vector3Int cell) => blockers != null && blockers.Contains(cell);

    private void Start()
    {
        allTypes = (GemType[])Enum.GetValues(typeof(GemType));
        tileMatchFinder.IsBlocked = IsBlocked;
        InitializeBoard();
    }

    //셀의 주변 한칸씩 탐색
    public IEnumerable<Vector3Int> Neighbor6(Vector3Int cell)
    {
        var a0 = HexDirections.GetAxisDeltas(cell, 0); // E/W
        var a1 = HexDirections.GetAxisDeltas(cell, 1); // NE/SW
        var a2 = HexDirections.GetAxisDeltas(cell, 2); // NW/SE
        return new[]
        {
            cell + a0.fwd, cell + a0.back,
            cell + a1.fwd, cell + a1.back,
            cell + a2.fwd, cell + a2.back
        };
    }

    public Gem GetGemMap(Vector3Int cell) => gemMap[cell];

    //타일맵에 있는 자리들을 바탕으로 Gem을 생성하고 Type를 랜덤으로 부여한다.
    //gemMap에 cell(Vector3Int)을 키로하여 Gem을 넣는다.
    private void InitializeBoard()
    {
        List<Vector3Int> positions = new List<Vector3Int>();
        var bounds = tilemap.cellBounds;
        foreach (var p in bounds.allPositionsWithin)
            if (tilemap.HasTile(p))
                positions.Add(p);

        // 아래→위(x 오름차순), 같은 줄은 좌→우(y 오름차순)

        positions.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));


        foreach (var cell in positions)
        {
            if (IsBlocked(cell))
            {
                gemMap[cell] = null;
                if (jackFactory)
                {
                    jackFactory.CreatJack(tilemap, cell, jackRoot);
                    continue;
                }

            }
            List<GemType> candidates = new List<GemType>(allTypes);
            candidates.RemoveAll(t => tileMatchFinder.WouldFormLineOf3At(cell, t, gemMap));

            GemType chosen = candidates.Count > 0 ?
                candidates[UnityEngine.Random.Range(0, candidates.Count)] : allTypes[UnityEngine.Random.Range(0, allTypes.Length)];

            gemMap[cell] = gemFactory.CreateGemOfType(chosen, tilemap, cell, gemsRoot);


        }
    }

    //cell 좌표를 월드 좌표로 교환
    private Vector3 WorldCenterOf(Vector3Int cell)
    {
        Vector3 w = tilemap.CellToWorld(cell) + tilemap.tileAnchor;
        return new Vector3(w.x, w.y, tilemap.transform.position.z);
    }

    //aGem과 bGem을 교체
    public void TrySwap(Vector3Int a, Vector3Int b)
    {
        //삐에로 블록
        if (IsBlocked(a) || IsBlocked(b))
            return;

        //보석 위치만 스왑
        if (!gemMap.ContainsKey(a) || !gemMap.ContainsKey(b) || a == b)
            return;

        StartCoroutine(SwapPosition(a, b));

    }

    //두개의 지점을 교체
    private IEnumerator SwapPosition(Vector3Int cellA, Vector3Int cellB, float duration = 0.25f)
    {

        if (!gemMap[cellA] || !gemMap[cellB])
        {
            yield break;
        }
        moveCheck = false;
        Vector3 startAPos = WorldCenterOf(cellA);
        Vector3 startBPos = WorldCenterOf(cellB);

        (gemMap[cellA], gemMap[cellB]) = (gemMap[cellB], gemMap[cellA]);


        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float du = Mathf.Clamp01(time / duration);

            gemMap[cellA].transform.position = Vector3.LerpUnclamped(startBPos, startAPos, du);
            gemMap[cellB].transform.position = Vector3.LerpUnclamped(startAPos, startBPos, du);
            yield return null;
        }
        gemMap[cellA].transform.position = startAPos;
        gemMap[cellB].transform.position = startBPos;

        yield return new WaitForSeconds(duration + 0.1f);

        List<Vector3Int> matches = tileMatchFinder.FindMatches(gemMap);
        if (matches.Count == 0)
        {
            (gemMap[cellA], gemMap[cellB]) = (gemMap[cellB], gemMap[cellA]);

            time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float du = Mathf.Clamp01(time / duration);

                gemMap[cellA].transform.position = Vector3.LerpUnclamped(startBPos, startAPos, du);
                gemMap[cellB].transform.position = Vector3.LerpUnclamped(startAPos, startBPos, du);
                yield return null;
            }
            gemMap[cellA].transform.position = startAPos;
            gemMap[cellB].transform.position = startBPos;

            if (gemsRoot)
            {
                gemMap[cellA].transform.SetParent(gemsRoot, worldPositionStays: true);
                gemMap[cellB].transform.SetParent(gemsRoot, worldPositionStays: true);

            }
            moveCheck = true;

            yield break;

        }
        // 매치 있으면 연쇄 코루틴 완료까지 대기
        yield return StartCoroutine(Co_ResolveCascades(matches));
        moveCheck = true;
    }


    //gemMap을 스캔하여 3개 이상의 Gem을 찾고 파괴한다음 채움
    private IEnumerator Co_ResolveCascades(List<Vector3Int> initialMatches = null)
    {
        if (_isResolving) yield break;
        _isResolving = true;

        var matches = initialMatches ?? tileMatchFinder.FindMatches(gemMap);
        if(matches.Count > 0)

        while (matches.Count > 0)
        {
            // 파괴
            foreach (var p in matches)
            {
                var g = gemMap[p];
                if (g != null)
                {
                    Destroy(g.gameObject);
                    //파괴된 자리에 GemCrush 생성
                    GemCrush gemC = Instantiate(gemCrush, g.transform.position, Quaternion.identity).GetComponent<GemCrush>();
                    gemC.Init(g.GemType);
                    Destroy(gemC, 0.5f);
                    gemMap[p] = null;
                }
            }

            // 여기서 한 번만 통지: 이번 프레임에 파괴된 셀들
            if (_destructionListeners.Count > 0)
            {
                // matches는 중복 없이 리스트로 유지되는 게 베스트
                _destructionListeners.ForEach(l => l.OnGemsDestroyed(matches));
            }

            // 0.25s 딜레이 후 낙하+보충 애니메이션
            yield return StartCoroutine(Co_ApplyGravityAndRefillAnimated(0.25f, 0.08f, true));


            //또 매치된게 있는지 봄
            matches = tileMatchFinder.FindMatches(gemMap);
        }

        _isResolving = false;
    }

    //Gem이 깨지고 아래로 내려오는 애니메이션
    private IEnumerator Co_ApplyGravityAndRefillAnimated(float delayBeforeFall, float perCellTime, bool alignEnd = true)
    {
        if (delayBeforeFall > 0f)
            yield return new WaitForSeconds(delayBeforeFall);

        // 유효 셀 수집
        List<Vector3Int> allCells = new List<Vector3Int>();
        foreach (var p in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.HasTile(p))
                allCells.Add(p);
        //y값이 같은 것끼리 그룹화 한 후 키값은 y값 Value는 그 값의 x값을 오름차순으로 정렬
        Dictionary<int, List<Vector3Int>> columns = allCells.GroupBy(c => c.y)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.x).ToList());

        // 이동, 스폰 계획
        var fallMoves = new List<(Gem gem, Vector3Int from, Vector3Int to)>();
        var spawnMoves = new List<(Gem gem, Vector3Int from, Vector3Int to)>();

        foreach (var kv in columns)
        {
            // 아래→위
            List<Vector3Int> col = kv.Value;

            // 이 열을 장애물을 기준으로 세그먼트로 분할
            // segment = 장애물 아닌 연속 구간
            List<List<Vector3Int>> segments = new List<List<Vector3Int>>();
            List<Vector3Int> cur = new List<Vector3Int>();
            foreach (var c in col)
            {
                if (IsBlocked(c))
                {
                    if (cur.Count > 0)
                    {
                        segments.Add(cur);
                        cur = new();
                    }
                    // 장애물 칸도 gemMap 키 확보(보장)
                    if (!gemMap.ContainsKey(c))
                        gemMap[c] = null;
                }
                else cur.Add(c);
            }
            if (cur.Count > 0)
                segments.Add(cur);

            // 각 세그먼트 내부에서만 상단 스폰 후 아래로
            foreach (var seg in segments)
            {
                // write 포인터
                int write = 0;
                for (int read = 0; read < seg.Count; read++)
                {
                    Vector3Int fromCell = seg[read];
                    if (!gemMap.TryGetValue(fromCell, out var g) || g == null)
                        continue;

                    var toCell = seg[write];
                    if (fromCell != toCell)
                    {
                        fallMoves.Add((g, fromCell, toCell));
                        gemMap[toCell] = g;
                        gemMap[fromCell] = null;
                    }
                    write++;
                }

                // 상단 빈칸들: 스폰 후보
                for (int i = write; i < seg.Count; i++)
                {
                    Vector3Int emptyCell = seg[i];
                    gemMap[emptyCell] = null;

                    // 즉시 3매치 방지
                    List<GemType> candidates = new List<GemType>(allTypes);
                    candidates.RemoveAll(t => tileMatchFinder.WouldFormLineOf3At(emptyCell, t, gemMap));
                    var chosen = (candidates.Count > 0)
                        ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
                        : allTypes[UnityEngine.Random.Range(0, allTypes.Length)];

                    // '세그먼트 위쪽'에서 스폰 (한 칸 위가 장애물이거나 보드 밖일 수 있음)
                    var up = HexDirections.GetAxisDeltas(emptyCell, 0).fwd; // (+1,0,0)
                    var spawnCell = emptyCell + up;

                    // 만약 spawnCell이 타일이 아니거나 장애물/보드 밖이면 월드 위치만 위로 올려서 시작
                    Vector3 fromPos = tilemap.HasTile(spawnCell) && !blockers.Contains(spawnCell)
                        ? WorldCenterOf(spawnCell)
                        : WorldCenterOf(emptyCell) + (WorldCenterOf(emptyCell) - WorldCenterOf(seg[0])).normalized * 0.6f; // 임시 위쪽

                    var gem = gemFactory.CreateGemOfType(chosen, tilemap, emptyCell, gemsRoot); // 로직 목적지는 emptyCell
                    gem.transform.position = fromPos; // 화면 시작 위치 보정
                    gemMap[emptyCell] = gem;
                    spawnMoves.Add((gem, emptyCell /*unused for dist*/, emptyCell));
                }
            }
        }

        // 2) 이동 애니메이션 (기존+신규 함께, 타이밍 정렬)
        int DistanceInCells(Vector3Int a, Vector3Int b) => Mathf.Abs(a.x - b.x); // x축이 중력

        int maxDist = 1;
        foreach (var m in fallMoves) maxDist = Mathf.Max(maxDist, DistanceInCells(m.from, m.to));
        foreach (var m in spawnMoves) maxDist = Mathf.Max(maxDist, 1); // 스폰은 최소 1칸 시간

        float T = maxDist * perCellTime;

        var batch = new List<(Transform tr, Vector3 start, Vector3 end, float delay, float duration)>();

        // 기존 낙하
        foreach (var m in fallMoves)
        {
            int d = Mathf.Max(1, DistanceInCells(m.from, m.to));
            float dur = d * perCellTime;
            float delay = (T - dur); // 끝을 맞추기
            batch.Add((m.gem.transform, m.gem.transform.position, WorldCenterOf(m.to), delay, dur));
        }

        // 신규 스폰(위치에서 → 목적 셀)
        foreach (var m in spawnMoves)
        {
            float dur = perCellTime; // 한 칸 시간(필요하면 더 키워도 됨)
            float delay = (T - dur);
            batch.Add((m.gem.transform, m.gem.transform.position, WorldCenterOf(m.to), delay, dur));
        }


        yield return StartCoroutine(AnimateMoveBatch(batch, EaseInQuad));
    }

    private IEnumerator AnimateMoveBatch(
    List<(Transform tr, Vector3 start, Vector3 end, float delay, float duration)> batch,
    Func<float, float> ease
)
    {
        if (batch.Count == 0) yield break;

        // 개별 타이머
        var timers = new Dictionary<Transform, float>(batch.Count);
        foreach (var b in batch) { timers[b.tr] = -b.delay; }

        bool allDone = false;
        while (!allDone)
        {
            allDone = true;
            foreach (var b in batch)
            {
                float t = timers[b.tr];
                t += Time.deltaTime;
                t += Time.deltaTime;
                timers[b.tr] = t;

                if (t < 0f) { allDone = false; continue; }                  // delay 대기
                if (t < b.duration)
                {
                    float u = Mathf.Clamp01(t / b.duration);
                    float e = ease(u);
                    b.tr.position = Vector3.LerpUnclamped(b.start, b.end, e);
                    allDone = false;
                }
                else
                {
                    b.tr.position = b.end; // 종료 스냅
                }
            }
            yield return null;
        }
    }
}

