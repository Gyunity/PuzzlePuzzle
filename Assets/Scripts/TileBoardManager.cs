using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileBoardManager : MonoBehaviour
{

    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private GemFactory gemFactory;
    [SerializeField]
    private TileMatchFinder tileMatchFinder;
    [SerializeField]
    private Transform gemsRoot;

    private Dictionary<Vector3Int, Gem> gemMap = new Dictionary<Vector3Int, Gem>();

   
    private GemType[] allTypes;

    private void Start()
    {
        allTypes = (GemType[])Enum.GetValues(typeof(GemType));
        InitializeBoard();
    }

    private Vector3 WorldCenterOf(Vector3Int cell)
    {
        Vector3 w = tilemap.CellToWorld(cell) + tilemap.tileAnchor;
        return new Vector3(w.x, w.y, tilemap.transform.position.z);
    }

    private void SnapGemToCell(Gem gem, Vector3Int cell)
    {
        if(gem == null)
            return;
        gem.transform.position = WorldCenterOf(cell);
        if (gemsRoot)
            gem.transform.SetParent(gemsRoot, worldPositionStays: true);
    }


    //타일맵에 있는 자리들을 바탕으로 Gem을 생성하고 Type를 랜덤으로 부여한다.
    private void InitializeBoard()
    {
        List<Vector3Int> positions = new List<Vector3Int>();
        var bounds = tilemap.cellBounds;
        foreach(var p in bounds.allPositionsWithin)
            if (tilemap.HasTile(p))
                positions.Add(p);

        positions.Sort((a, b) => a.y != b.y ?  a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
        

        foreach (var cell in positions)
        {
            
            List<GemType> candidates = new List<GemType>(allTypes);
            candidates.RemoveAll(t => tileMatchFinder.WouldFormLineOf3At(cell, t, gemMap));

            GemType chosen = candidates.Count > 0 ?
                candidates[UnityEngine.Random.Range(0, candidates.Count)] : allTypes[UnityEngine.Random.Range(0, allTypes.Length)];

            gemMap[cell] = gemFactory.CreateGemOfType(chosen, tilemap, cell, gemsRoot);

        }
    }

    public void TrySwap(Vector3Int a, Vector3Int b)
    {
        if (!gemMap.ContainsKey(a) || !gemMap.ContainsKey(b) || a==b)
            return;


        (gemMap[a], gemMap[b]) = (gemMap[b], gemMap[a]);

        SnapGemToCell(gemMap[a], a);
        SnapGemToCell(gemMap[b], b);

        List<Vector3Int> matches = tileMatchFinder.FindMatches(gemMap);

        if(matches.Count == 0)
        {
            (gemMap[a], gemMap[b]) = (gemMap[b], gemMap[a]);
            SnapGemToCell(gemMap[a], a);
            SnapGemToCell(gemMap[b], b);
            return;
        }
        ResolveCascades(matches);
    }
    private void ResolveCascades(List<Vector3Int> initialMatches = null)
    {
        var matches = initialMatches ?? tileMatchFinder.FindMatches(gemMap);

        while (matches.Count > 0)
        {
            foreach (var p in matches)
            {
                if (gemMap[p] != null)
                {
                    Destroy(gemMap[p].gameObject);
                    gemMap[p] = null;
                }
            }

            new TileGemFallHandler().ApplyGravity(tilemap, gemMap);

            FillEmptyChack();

            matches = tileMatchFinder.FindMatches(gemMap);
        }
    }
    private void FillEmptyChack()
    {
        var empties = gemMap.Where(kv => kv.Value == null).Select(kv => kv.Key).ToList();
        foreach (var cell in empties)
        {
            var candidates = new List<GemType>(allTypes);
            candidates.RemoveAll(t => tileMatchFinder.WouldFormLineOf3At(cell, t, gemMap));

            GemType chosen = candidates.Count > 0
                ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
                : allTypes[UnityEngine.Random.Range(0, allTypes.Length)];

            gemMap[cell] = gemFactory.CreateGemOfType(chosen, tilemap, cell, gemsRoot);
        }
    }
    public bool TryGetGemAtCell(Vector3Int cell, out Gem gem)
    {
        return gemMap.TryGetValue(cell, out gem) && gem != null;
    }
}
