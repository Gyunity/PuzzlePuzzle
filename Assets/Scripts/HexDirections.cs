using UnityEngine;

public static class HexDirections
{
    // Flat-Top + Odd-R (행 오프셋)
    public static bool IsOddRow(Vector3Int c) => (c.y & 1) == 1;

    // 현재 셀에서 3개 축의 "앞/뒤" 델타를 넘겨준다.
    // 축0: E-W, 축1: NE-SW, 축2: NW-SE
    public static (Vector3Int fwd, Vector3Int back) GetAxisDeltas(Vector3Int cell, int axis)
    {
        bool odd = IsOddRow(cell);

        switch (axis)
        {
            case 0: // E <-> W (항상 동일)
                return (new Vector3Int(+1, 0, 0), new Vector3Int(-1, 0, 0));

            case 1: // NE <-> SW : 짝/홀 행에서 번갈아 달라짐
                // 전진(NE) 방향
                var ne = odd ? new Vector3Int(+1, +1, 0) : new Vector3Int(0, +1, 0);
                // 반대(SW) 방향
                var sw = odd ? new Vector3Int(0, -1, 0) : new Vector3Int(-1, -1, 0);
                return (ne, sw);

            case 2: // NW <-> SE : 짝/홀 행에서 번갈아 달라짐
                // 전진(NW) 방향
                var nw = odd ? new Vector3Int(0, +1, 0) : new Vector3Int(-1, +1, 0);
                // 반대(SE) 방향
                var se = odd ? new Vector3Int(+1, -1, 0) : new Vector3Int(0, -1, 0);
                return (nw, se);
        }

        return (Vector3Int.zero, Vector3Int.zero);
    }
    
}
