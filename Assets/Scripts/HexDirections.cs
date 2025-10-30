using UnityEngine;

public static class HexDirections
{

    public enum Layout { PointTop_OddR, FlatTop_OddQ }

    // ⬇️ 네 타일맵 레이아웃에 맞게 지정 (Grid/Tile Palette 설정과 일치시켜야 함)
    public static Layout layout = Layout.PointTop_OddR;

    /// 현재 셀의 짝/홀에 맞춘 6이웃 오프셋 반환
    public static Vector3Int[] GetNeighbor6(Vector3Int cell)
    {
        switch (layout)
        {
            case Layout.PointTop_OddR:
            default:
                {
                    bool oddRow = (cell.y & 1) == 1;
                    // E, NE, NW, W, SW, SE (순서는 임의)
                    return oddRow
                        ? new[]
                        {
                        new Vector3Int(+1,  0, 0),
                        new Vector3Int(+1, +1, 0),
                        new Vector3Int( 0, +1, 0),
                        new Vector3Int(-1,  0, 0),
                        new Vector3Int( 0, -1, 0),
                        new Vector3Int(+1, -1, 0),
                        }
                        : new[]
                        {
                        new Vector3Int(+1,  0, 0),
                        new Vector3Int( 0, +1, 0),
                        new Vector3Int(-1, +1, 0),
                        new Vector3Int(-1,  0, 0),
                        new Vector3Int(-1, -1, 0),
                        new Vector3Int( 0, -1, 0),
                        };
                }

            case Layout.FlatTop_OddQ:
                {
                    bool oddCol = (cell.x & 1) == 1;
                    // E, SE, SW, W, NW, NE
                    return oddCol
                        ? new[]
                        {
                        new Vector3Int(+1,  0, 0),
                        new Vector3Int(+1, -1, 0),
                        new Vector3Int( 0, -1, 0),
                        new Vector3Int(-1,  0, 0),
                        new Vector3Int( 0, +1, 0),
                        new Vector3Int(+1, +1, 0),
                        }
                        : new[]
                        {
                        new Vector3Int(+1,  0, 0),
                        new Vector3Int( 0, -1, 0),
                        new Vector3Int(-1, -1, 0),
                        new Vector3Int(-1,  0, 0),
                        new Vector3Int(-1, +1, 0),
                        new Vector3Int( 0, +1, 0),
                        };
                }
        }
    }

    /// 라인을 따라 다음 칸으로 진행할 때, 현재 셀 기준으로 '같은 방향' 벡터를 다시 선택
    public static Vector3Int NextStepDir(Vector3Int atCell, Vector3Int prevDir)
    {
        var dirs = GetNeighbor6(atCell);
        foreach (var d in dirs) if (d == prevDir) return d;
        return dirs[0]; // fallback
    }

    /// 현재 셀에서 prevDir의 반대 방향(= -prevDir) 벡터를 반환
    public static Vector3Int OppositeDir(Vector3Int atCell, Vector3Int prevDir)
    {
        var dirs = GetNeighbor6(atCell);
        var want = -prevDir;
        foreach (var d in dirs) if (d == want) return d;
        return want; // fallback
    }
}
