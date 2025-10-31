using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static UnityEditor.Searcher.SearcherWindow;
public class TileBoardManager : MonoBehaviour
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

    private void Start()
    {
        allTypes = (GemType[])Enum.GetValues(typeof(GemType));
        InitializeBoard();
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

            List<GemType> candidates = new List<GemType>(allTypes);
            candidates.RemoveAll(t => tileMatchFinder.WouldFormLineOf3At(cell, t, gemMap));

            GemType chosen = candidates.Count > 0 ?
                candidates[UnityEngine.Random.Range(0, candidates.Count)] : allTypes[UnityEngine.Random.Range(0, allTypes.Length)];

            gemMap[cell] = gemFactory.CreateGemOfType(chosen, tilemap, cell, gemsRoot);

        }
    }

    private Vector3 WorldCenterOf(Vector3Int cell)
    {
        Vector3 w = tilemap.CellToWorld(cell) + tilemap.tileAnchor;
        return new Vector3(w.x, w.y, tilemap.transform.position.z);
    }

    //aGem과 bGem을 교체한다
    public void TrySwap(Vector3Int a, Vector3Int b)
    {
        if (!gemMap.ContainsKey(a) || !gemMap.ContainsKey(b) || a == b)
            return;

        StartCoroutine(SwapPosition(a, b));

    }
    //두개의 지점을 교체
    private IEnumerator SwapPosition(Vector3Int cellA, Vector3Int cellB, float duration = 0.25f)
    {
        moveCheck = false;
        if (!gemMap[cellA] || !gemMap[cellB])
            yield break;
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


    private IEnumerator Co_ResolveCascades(List<Vector3Int> initialMatches = null)
    {
        if (_isResolving) yield break;
        _isResolving = true;

        var matches = initialMatches ?? tileMatchFinder.FindMatches(gemMap);

        while (matches.Count > 0)
        {
            // 1) 파괴
            foreach (var p in matches)
            {
                var g = gemMap[p];
                if (g != null)
                {
                    Destroy(g.gameObject);
                    GemCrush gemC = Instantiate(gemCrush, g.transform.position, Quaternion.identity).GetComponent<GemCrush>();
                    gemC.Init(g.GemType);
                    Destroy(gemC, 0.5f);
                    gemMap[p] = null;
                }
            }

            // 2) 0.25s 딜레이 후 낙하+보충 애니메이션
            yield return StartCoroutine(Co_ApplyGravityAndRefillAnimated(0.25f, 0.08f, true));

            // 3) 다음 매치
            matches = tileMatchFinder.FindMatches(gemMap);
        }

        _isResolving = false;
    }
    private IEnumerator Co_ApplyGravityAndRefillAnimated(
    float delayBeforeFall, float perCellTime, bool alignEnd = true
)
    {
        if (delayBeforeFall > 0f) yield return new WaitForSeconds(delayBeforeFall);

        // 1) 유효 셀 모으고 y(열)별, 아래→위(x 오름차순)
        var allCells = new List<Vector3Int>();
        foreach (var p in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.HasTile(p)) allCells.Add(p);
        var columns = allCells
        .GroupBy(c => c.y)
        .ToDictionary(g => g.Key, g => g.OrderBy(c => c.x).ToList());

        // 2) 열 압축: 이동 계획(fallMoves) 만들고, gemMap은 '목적지'로 선갱신
        var fallMoves = new List<(Gem gem, Vector3Int from, Vector3Int to)>();
        foreach (var kv in columns)
        {
            var col = kv.Value; // 아래→위
            int write = 0;
            for (int read = 0; read < col.Count; read++)
            {
                var fromCell = col[read];
                if (!gemMap.TryGetValue(fromCell, out var g) || g == null) continue;

                var toCell = col[write];
                if (fromCell != toCell)
                {
                    fallMoves.Add((g, fromCell, toCell));
                    gemMap[toCell] = g;
                    gemMap[fromCell] = null;
                }
                write++;
            }
            for (int i = write; i < col.Count; i++) gemMap[col[i]] = null;
        }

        // 3) 스폰 계획(spawnMoves)도 같은 타이밍에 계산
        var spawnMoves = new List<(Gem gem, Vector3Int from, Vector3Int to)>();
        foreach (var kv in columns)
        {
            var col = kv.Value;
            for (int i = col.Count - 1; i >= 0; i--)
            {
                var cell = col[i];
                if (gemMap[cell] != null) continue;

                // 즉시 3매치 방지
                var candidates = new List<GemType>(allTypes);
                candidates.RemoveAll(t => tileMatchFinder.WouldFormLineOf3At(cell, t, gemMap));
                var chosen = (candidates.Count > 0)
                    ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
                    : allTypes[UnityEngine.Random.Range(0, allTypes.Length)];

                // 셀 바로 위에서 생성
                var up = HexDirections.GetAxisDeltas(cell, 0).fwd; // (+1,0,0) 쪽
                var spawnCell = cell + up;

                var gem = gemFactory.CreateGemOfType(chosen, tilemap, spawnCell, gemsRoot);
                gem.transform.position = WorldCenterOf(spawnCell);

                // 로직상 목적지는 cell
                gemMap[cell] = gem;

                spawnMoves.Add((gem, spawnCell, cell));
            }
        }

        // 4) 두 리스트를 모두 '동시에' 애니메이션
        //    - alignEnd=true: 모두 같은 시간 T에 끝남 (delay를 주입)
        //    - alignEnd=false: 모두 동시에 시작, 거리 비례 시간 (속도 일정)
        int DistanceInCells(Vector3Int a, Vector3Int b) => Mathf.Abs(a.x - b.x); // 우리 중력축이 x이므로

        int maxDist = 1;
        foreach (var m in fallMoves) maxDist = Mathf.Max(maxDist, DistanceInCells(m.from, m.to));
        foreach (var m in spawnMoves) maxDist = Mathf.Max(maxDist, DistanceInCells(m.from, m.to));

        float T = maxDist * perCellTime;

        var batch = new List<(Transform tr, Vector3 start, Vector3 end, float delay, float duration)>();

        // 기존 낙하
        foreach (var m in fallMoves)
        {
            int d = DistanceInCells(m.from, m.to);
            float dur = Mathf.Max(0.0001f, d * perCellTime);
            float delay = alignEnd ? (T - dur) : 0f;

            batch.Add((
                m.gem.transform,
                m.gem.transform.position,
                WorldCenterOf(m.to),
                delay,
                dur
            ));
        }

        // 스폰 낙하
        foreach (var m in spawnMoves)
        {
            int d = DistanceInCells(m.from, m.to);
            float dur = Mathf.Max(0.0001f, d * perCellTime);
            float delay = alignEnd ? (T - dur) : 0f;

            batch.Add((
                m.gem.transform,
                WorldCenterOf(m.from),
                WorldCenterOf(m.to),
                delay,
                dur
            ));
        }

        // 동시에 실행 (fall+spawn 모두)
        // 끝을 맞추고 싶으면 EaseInQuad, 시작을 맞추고 싶으면 EaseOutQuad가 보기 좋음
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
