using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
public class TileBoardManager : MonoBehaviour, IBoardReadonly
{
    private static float EaseInQuad(float t) => t * t;


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

    //특수 잼
    private Vector3Int _lastSwapFrom, _lastSwapTo;
    private int _lastSwapAxis = -1;

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
        SoundManager.I.PlayBgm(BgmId.Stage, 1.0f);
        SoundManager.I.PlaySfx(SfxId.Start);
    }

    //셀의 주변 한칸씩 탐색
    public IEnumerable<Vector3Int> Neighbor6(Vector3Int cell)
    {
        var a0 = HexDirections.GetAxisDeltas(cell, 0);
        var a1 = HexDirections.GetAxisDeltas(cell, 1);
        var a2 = HexDirections.GetAxisDeltas(cell, 2);
        yield return cell + a0.fwd; yield return cell + a0.back;
        yield return cell + a1.fwd; yield return cell + a1.back;
        yield return cell + a2.fwd; yield return cell + a2.back;
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
            if (ScaleTweener.Instance) ScaleTweener.Instance.PopIn(gemMap[cell].transform, 0.30f, unscaled: false, ease: EaseType.EaseOutBack);

        }
    }

    //cell 좌표를 월드 좌표로 교환
    public Vector3 WorldCenterOf(Vector3Int cell)
    {
        Vector3 w = tilemap.CellToWorld(cell) + tilemap.tileAnchor;
        return new Vector3(w.x, w.y, tilemap.transform.position.z);
    }

    //aGem과 bGem을 교체
    public void TrySwap(Vector3Int a, Vector3Int b)
    {
        if (IsBlocked(a) || IsBlocked(b))
            return;

        if (!gemMap.ContainsKey(a) || !gemMap.ContainsKey(b) || a == b)
            return;

        // 스왑 축 미리 계산
        _lastSwapFrom = a;
        _lastSwapTo = b;
        _lastSwapAxis = AxisFromDelta(b - a);

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

        SoundManager.I.PlaySfx(SfxId.Swap);
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
            SoundManager.I.PlaySfx(SfxId.Swap);
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

        while (matches.Count > 0)
        {
            SoundManager.I.PlaySfx(SfxId.Match3);
            //스페셜 생성 (이번에 만든 셀 받기)
            Vector3Int createdSpecialCell;
            bool created = TryCreateSpecialAtLastSwapTo(matches, out createdSpecialCell);

            // 이번 턴 파괴 집합(기본: 매치된 것들)
            var kill = new HashSet<Vector3Int>(matches);

            // 방금 만든 스페셜은 이번 턴 보호(살려둔다)
            if (created) kill.Remove(createdSpecialCell);

            // 스페셜 발동(라인 확장) — 단, 막 만든 것은 발동 제외
            ExpandMatchesBySpecialsForActivation(kill, matches, created ? createdSpecialCell : (Vector3Int?)null);

            // 파괴
            foreach (var p in kill)
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
            yield return StartCoroutine(Co_ApplyGravityAndRefillAnimated(0.25f, 0.5f, true));


            //또 매치된게 있는지 봄
            matches = tileMatchFinder.FindMatches(gemMap);
        }
        _lastSwapAxis = -1; // 끝난 뒤 초기화
        _isResolving = false;
    }

    //Gem이 깨지고 아래로 내려오는 애니메이션
   
        private IEnumerator Co_ApplyGravityAndRefillAnimated(float delayBeforeFall, float perCellTime, bool alignEnd = true)
    {
        if (delayBeforeFall > 0f)
            yield return new WaitForSeconds(delayBeforeFall);

        // 유효 셀 수집
        var allCells = new List<Vector3Int>();
        foreach (var p in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.HasTile(p)) allCells.Add(p);

        // y(열)별로 아래→위(x 오름차순)
        var columns = allCells
            .GroupBy(c => c.y)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.x).ToList());

        //  기존 젬 낙하 계획
        var fallMoves = new List<(Gem gem, Vector3Int from, Vector3Int to)>();

        // 세그먼트별 스폰 타깃 묶음
        var segmentsForSpawn = new List<List<(Vector3Int cell, GemType type, int lift, int rank)>>();

        foreach (var kv in columns)
        {
            var col = kv.Value; 

            // 장애물 기준 세그먼트 분할
            var segments = new List<List<Vector3Int>>();
            var cur = new List<Vector3Int>();
            foreach (var c in col)
            {
                if (IsBlocked(c))
                {
                    if (cur.Count > 0) { segments.Add(cur); cur = new(); }
                    if (!gemMap.ContainsKey(c)) gemMap[c] = null; 
                }
                else cur.Add(c);
            }
            if (cur.Count > 0) segments.Add(cur);

            foreach (var seg in segments)
            {
                // 아래로 압축
                int write = 0;
                for (int read = 0; read < seg.Count; read++)
                {
                    var fromCell = seg[read];
                    if (!gemMap.TryGetValue(fromCell, out var g) || g == null) continue;

                    var toCell = seg[write];
                    if (fromCell != toCell)
                    {
                        fallMoves.Add((g, fromCell, toCell));
                        gemMap[toCell] = g;
                        gemMap[fromCell] = null;
                    }
                    write++;
                }

                // 스폰 타깃 수집
                var spawnList = new List<(Vector3Int, GemType, int, int)>();
                int topIndex = seg.Count - 1;
                int rank = 0; // 아래칸부터 0
                for (int i = write; i < seg.Count; i++, rank++)
                {
                    var emptyCell = seg[i];

                    // 즉시 3매치 방지 타입 고르기
                    var candidates = new List<GemType>(allTypes);
                    candidates.RemoveAll(t => tileMatchFinder.WouldFormLineOf3At(emptyCell, t, gemMap));
                    var chosen = (candidates.Count > 0)
                        ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
                        : allTypes[UnityEngine.Random.Range(0, allTypes.Length)];

                    // 가장 위쪽일수록 더 위에서 시작
                    int lift = (topIndex - i + 1);
                    spawnList.Add((emptyCell, chosen, lift, rank));
                    // 논리 채움은 실제 생성 시점에 수행
                }
                if (spawnList.Count > 0) segmentsForSpawn.Add(spawnList);
            }
        }

        // 기존 젬만 먼저 내려오게
        if (fallMoves.Count > 0)
        {
            int DistanceInCells(Vector3Int a, Vector3Int b) => Mathf.Abs(a.x - b.x); 
            int maxDist = 1;
            foreach (var m in fallMoves) maxDist = Mathf.Max(maxDist, DistanceInCells(m.from, m.to));
            float T = maxDist * perCellTime;

            var batch1 = new List<(Transform tr, Vector3 start, Vector3 end, float delay, float duration)>();
            foreach (var m in fallMoves)
            {
                int d = Mathf.Max(1, Math.Abs(m.from.x - m.to.x));
                float dur = d * perCellTime;
                float delay = alignEnd ? (T - dur) : 0f;
                batch1.Add((m.gem.transform, m.gem.transform.position, WorldCenterOf(m.to), delay, dur));
            }
            yield return StartCoroutine(AnimateMoveBatch(batch1, EaseInQuad));
        }

        // 새 젬을 ‘세그먼트 위’에서 생성 → 아래로
        if (segmentsForSpawn.Count > 0)
        {
            var spawnBatch = new List<(Transform tr, Vector3 start, Vector3 end, float delay, float duration)>();
            const float rankStagger = 0.06f; // 아래칸→위칸 순으로 살짝 지연(도착 순서 보장)

            foreach (var segSpawn in segmentsForSpawn)
            {
                // 세그먼트 안에서 worldStep은 각 타깃별로 안전하게 계산(셀 간 월드 오프셋)
                // 각 스폰의 duration을 먼저 구해 maxDur을 얻음
                float maxDur = 0f;
                var temp = new List<(Gem gem, Vector3 start, Vector3 end, float dur, int rank)>();

                foreach (var (cell, type, lift, rank) in segSpawn)
                {
                    // 실제 젬 생성 (목적지: cell)
                    var gem = gemFactory.CreateGemOfType(type, tilemap, cell, gemsRoot);
                    gemMap[cell] = gem;

                    // 월드 한 칸 위 오프셋(해당 셀 기준으로 계산 → 타일맵 스케일/앵커 자동 대응)
                    var upDelta = HexDirections.GetAxisDeltas(cell, 0).fwd; // (+1,0,0)
                    Vector3 endPos = WorldCenterOf(cell);
                    Vector3 step = WorldCenterOf(cell + upDelta) - endPos;
                    if (step.sqrMagnitude < 1e-6f) step = Vector3.right * 0.6f;

                    Vector3 startPos = endPos + step * Mathf.Max(1, lift);

                    if (ScaleTweener.Instance)
                        ScaleTweener.Instance.PopIn(gem.transform, 0.18f, unscaled: false, ease: EaseType.EaseOutBack);
                    gem.transform.position = startPos;

                    float dur = perCellTime * Mathf.Max(1, lift);
                    temp.Add((gem, startPos, endPos, dur, rank));
                    if (dur > maxDur) maxDur = dur;
                }

                // 도착 타이밍: (maxDur - dur)로 짧은 이동일수록 더 ‘늦게’ 시작해
                // → 아래칸(보통 dur 길다)이 먼저 도착. 그리고 rank로 ‘아래→위’ 추가 간격 부여.
                foreach (var t in temp)
                {
                    float delay = (maxDur - t.dur) + (t.rank * rankStagger);
                    spawnBatch.Add((t.gem.transform, t.start, t.end, delay, t.dur));
                }
            }

            yield return StartCoroutine(AnimateMoveBatch(spawnBatch, EaseInQuad));
        }
    
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
                // delay 대기
                if (t < 0f) { allDone = false; continue; }                 
                if (t < b.duration)
                {
                    float u = Mathf.Clamp01(t / b.duration);
                    float e = ease(u);
                    b.tr.position = Vector3.LerpUnclamped(b.start, b.end, e);
                    allDone = false;
                }
                else
                {
                    // 종료 스냅
                    b.tr.position = b.end;
                }
            }
            yield return null;
        }
    }


    // 헥사 좌표 델타 → 축(0/1/2) 판별
    private int AxisFromDelta(Vector3Int d)
    {
        // 이동하려는 ‘앞 방향’이 어느 축인지 판단
        // 축0(E/W), 축1(NE/SW), 축2(NW/SE) 기준으로 매칭
        // Cell 기준 delta를 각 축의 fwd/back와 대조
        for (int axis = 0; axis < 3; axis++)
        {
            var a = HexDirections.GetAxisDeltas(_lastSwapFrom, axis);
            if (d == a.fwd || d == a.back) return axis;
        }
        return 0;
    }

    // TileBoardManager.Co_ResolveCascades 내의 while(matches.Count>0) 루프에서,
    // 실제 파괴하기 전에 아래 로직을 끼워 넣어준다.

    private void ExpandMatchesBySpecialsForActivation(HashSet<Vector3Int> kill, List<Vector3Int> matches, Vector3Int? createdThisStep
)
    {
        if (matches == null || matches.Count == 0) return;

        var specials = new List<(Vector3Int cell, int axis)>();
        foreach (var p in matches)
        {
            if (!gemMap.TryGetValue(p, out var g) || g == null) continue;
            if (!g.IsLineBlaster()) continue;

            // 이번에 막 만든 스페셜은 발동시키지 않는다
            if (createdThisStep.HasValue && p == createdThisStep.Value) continue;

            specials.Add((p, g.BlastAxis));
        }
        if (specials.Count == 0) return;

        foreach (var (origin, axis) in specials)
        {
            var (fwd, back) = HexDirections.GetAxisDeltas(origin, axis);

            // +방향 끝까지
            var cur = origin;
            while (true)
            {
                cur += fwd;
                if (!tilemap.HasTile(cur)) break;
                if (IsBlocked(cur)) break;   
                kill.Add(cur);
            }

            // -방향 끝까지
            cur = origin;
            while (true)
            {
                cur += back;
                if (!tilemap.HasTile(cur)) break;
                if (IsBlocked(cur)) break;
                kill.Add(cur);
            }

            // 발동한 스페셜은 이번에 함께 파괴되어야 한다 
            kill.Add(origin);
        }
    }
    // 4개 이상 매치면 _lastSwapTo 칸에 스페셜 생성
    // 생성했으면 그 셀을 out으로 돌려준다
    private bool TryCreateSpecialAtLastSwapTo(List<Vector3Int> matches, out Vector3Int createdCell)
    {
        createdCell = default;
        if (matches == null || matches.Count < 4) return false;
        if (_lastSwapAxis < 0) return false;

        var to = _lastSwapTo;
        if (!tilemap.HasTile(to) || IsBlocked(to)) return false;
        if (!gemMap.TryGetValue(to, out var g) || g == null) return false;

        // 이미 스페셜이면 넘어가도 되지만, 덮어쓰고 싶지 않다면 true 반환만
        if (!g.IsLineBlaster()) g.SetLineBlaster(_lastSwapAxis);

        createdCell = to;
        return true;
    }

    public bool TryPeekGem(Vector3Int cell, out Gem gem)
    {
        // 블로커 칸을 젬 없음으로 취급하고 싶다면:
        if (IsBlocked(cell))
        {
            gem = null;
            return false;
        }
        // gemMap 조회: 키 있고, null 아니면 true
        return gemMap.TryGetValue(cell, out gem) && gem != null;
    }
    // ===== Hint (보드가 Idle일 때만 호출) =====
    public bool TryFindHintDetailed(
        out Vector3Int from, out Vector3Int to,
        out Vector3Int anchorA, out Vector3Int anchorB)
    {
        from = default(Vector3Int);
        to = default(Vector3Int);
        anchorA = default(Vector3Int);
        anchorB = default(Vector3Int);

        // 유효 셀 수집(타일 O, 블로커 X, 젬 O)
        var cells = new List<Vector3Int>();
        foreach (var p in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(p)) continue;
            if (IsBlocked(p)) continue;
            if (!gemMap.TryGetValue(p, out var g) || g == null) continue;
            cells.Add(p);
        }

        HintCandidate? best = null;

        foreach (var a in cells)
        {
            foreach (var n in Neighbor6(a))
            {
                var b = n; // Neighbor6은 '좌표'를 반환
                if (!tilemap.HasTile(b) || IsBlocked(b)) continue;
                if (!gemMap.TryGetValue(b, out var gb) || gb == null) continue;

                // (a,b)/(b,a) 중복 방지
                if (!CanonicalPair(a, b)) continue;

                // ---- 가상 스왑 ----
                (gemMap[a], gemMap[b]) = (gemMap[b], gemMap[a]);

                // 중복 없이 반환되도록 구성해 둔 것
                var matched = tileMatchFinder.FindMatches(gemMap); 
                int count = matched.Count;

                // 스왑 복구(꼭!)
                (gemMap[a], gemMap[b]) = (gemMap[b], gemMap[a]);

                if (count < 3) continue;

                // 점수화: 4+면 스페셜 가능 가중치
                int score = count + (count >= 4 ? 2 : 0);

                if (!TryPickAnchorsForSwap(a, b, out var aa, out var bb))
                    continue;

                var cand = new HintCandidate { from = a, to = b, score = score, anchorA = aa, anchorB = bb };
                if (best == null || cand.score > best.Value.score)
                    best = cand;
            }
        }

        if (best != null)
        {
            from = best.Value.from;
            to = best.Value.to;
            anchorA = best.Value.anchorA;
            anchorB = best.Value.anchorB;
            return true;
        }
        return false;
    }

    // (a,b) / (b,a) 중 한 번만 평가하기 위한 정렬 기준
    static bool CanonicalPair(Vector3Int a, Vector3Int b)
    {
        if (a.y != b.y) return a.y < b.y;
        if (a.x != b.x) return a.x < b.x;
        return a.z < b.z;
    }

    // 힌트 내부용 구조체 (클래스 안에 둬야 함)
    struct HintCandidate
    {
        public Vector3Int from, to;
        public int score;
        public Vector3Int anchorA, anchorB;
    }


    // 스왑 (a->b) 시, to=b 기준으로 앵커 좌표 선택
    bool TryPickAnchorsForSwap(Vector3Int a, Vector3Int b, out Vector3Int anchorA, out Vector3Int anchorB)
    {
        anchorA = default(Vector3Int);
        anchorB = default(Vector3Int);

        // 가상 스왑
        (gemMap[a], gemMap[b]) = (gemMap[b], gemMap[a]);

        bool ok = false;
        var gTo = gemMap[b];
        if (gTo != null)
        {
            GemType movedType = gTo.GemType;

            // to=b 기준 3축에서 양옆 한 칸씩 같은 색 찾기
            for (int axis = 0; axis < 3 && !ok; axis++)
            {
                var (fwd, back) = HexDirections.GetAxisDeltas(b, axis);

                // 바로 옆 한 칸만 확인(앵커용)
                var left = b + back;
                var right = b + fwd;

                if (IsValidSame(left, movedType) && IsValidSame(right, movedType))
                {
                    anchorA = left;
                    anchorB = right;
                    ok = true;
                    break;
                }
            }

            // 실패하면: b의 이웃 중 동일색 2개 임의 선택(보조)
            if (!ok)
            {
                var same = new List<Vector3Int>(2);
                foreach (var n in Neighbor6(b))
                {
                    if (IsValidSame(n, movedType))
                    {
                        same.Add(n);
                        if (same.Count == 2) break;
                    }
                }
                if (same.Count == 2) { anchorA = same[0]; anchorB = same[1]; ok = true; }
            }
        }

        // 스왑 복구
        (gemMap[a], gemMap[b]) = (gemMap[b], gemMap[a]);
        return ok;
    }

    // 이 좌표가 타일 있고, 블로커 아니고, 같은 색 젬인가?
    bool IsValidSame(Vector3Int cell, GemType type)
    {
        if (!tilemap.HasTile(cell)) return false;
        if (IsBlocked(cell)) return false;
        return gemMap.TryGetValue(cell, out var g) && g != null && g.GemType == type;
    }
}

