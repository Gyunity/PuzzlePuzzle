using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileMatchFinder : MonoBehaviour
{


    public List<Vector3Int> FindMatches(Dictionary<Vector3Int, Gem> gemMap)
    {
        var result = new HashSet<Vector3Int>();

        foreach (var kv in gemMap)
        {
            var pos = kv.Key;
            var gem = kv.Value;
            if (gem == null) continue;

            var dirs = HexDirections.GetNeighbor6(pos); // ✅ parity-aware

            foreach (var dir0 in dirs)
            {
                var line = new List<Vector3Int> { pos };

                // +쪽으로 전진
                var step = dir0;
                var p = pos;
                for (int i = 0; i < 4; i++) // 넉넉히 4칸까지 (보드 크기에 맞게 조절 가능)
                {
                    p += step;
                    if (!gemMap.TryGetValue(p, out var g) || g == null || g.GemType != gem.GemType)
                        break;

                    line.Add(p);
                    step = HexDirections.NextStepDir(p, step); // ✅ 다음 칸에서의 '같은 방향'
                }

                // -쪽(반대)으로 전진
                step = HexDirections.OppositeDir(pos, dir0);
                p = pos;
                for (int i = 0; i < 4; i++)
                {
                    p += step;
                    if (!gemMap.TryGetValue(p, out var g) || g == null || g.GemType != gem.GemType)
                        break;

                    line.Add(p);
                    step = HexDirections.NextStepDir(p, step);
                }

                if (line.Count >= 3)
                    foreach (var c in line) result.Add(c); // ✅ 중복 제거
            }
        }

        return result.ToList();
    }

    /// pos에 타입 t를 '가정 배치'했을 때 3개 이상 직선 매칭이 생기는지
    public bool WouldFormLineOf3At(Vector3Int pos, GemType t, IDictionary<Vector3Int, Gem> gemMap)
    {
        var dirs = HexDirections.GetNeighbor6(pos);

        foreach (var dir0 in dirs)
        {
            int count = 1; // 자신 포함

            // +쪽
            var step = dir0;
            var p = pos;
            for (int i = 0; i < 2; i++)
            {
                p += step;
                if (!gemMap.TryGetValue(p, out var g) || g == null || g.GemType != t) break;
                count++;
                step = HexDirections.NextStepDir(p, step);
            }

            // -쪽
            step = HexDirections.OppositeDir(pos, dir0);
            p = pos;
            for (int i = 0; i < 2; i++)
            {
                p += step;
                if (!gemMap.TryGetValue(p, out var g) || g == null || g.GemType != t) break;
                count++;
                step = HexDirections.NextStepDir(p, step);
            }

            if (count >= 3) return true;
        }

        return false;
    }
}
