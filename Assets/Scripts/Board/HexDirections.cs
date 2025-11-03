using UnityEngine;

public static class HexDirections
{
    public static bool IsOddRow(Vector3Int c) => (c.y & 1) == 1;

    // Odd-R 기준 6이웃
    public static Vector3Int[] GetNeighbor6(Vector3Int cell)
    {
        bool odd = IsOddRow(cell);
        return odd
            ? new[] // odd row
            {
                new Vector3Int(+1,  0, 0), // E
                new Vector3Int(-1,  0, 0), // W
                new Vector3Int(+1, +1, 0), // NE
                new Vector3Int( 0, +1, 0), // NW
                new Vector3Int(+1, -1, 0), // SE
                new Vector3Int( 0, -1, 0), // SW
            }
            : new[] // even row
            {
                new Vector3Int(+1,  0, 0), // E
                new Vector3Int(-1,  0, 0), // W
                new Vector3Int( 0, +1, 0), // NE
                new Vector3Int(-1, +1, 0), // NW
                new Vector3Int( 0, -1, 0), // SE
                new Vector3Int(-1, -1, 0), // SW
            };
    }

    // 3개 축(E/W, NE/SW, NW/SE) – 라인 스캔/스페셜용
    // axis: 0(E/W), 1(NE/SW), 2(NW/SE)
    public static (Vector3Int fwd, Vector3Int back) GetAxisDeltas(Vector3Int cell, int axis)
    {
        bool odd = IsOddRow(cell);
        switch (axis)
        {
            case 0: // E <-> W (행과 무관)
                return (new Vector3Int(+1, 0, 0), new Vector3Int(-1, 0, 0));

            case 1: // NE <-> SW
                return odd
                    ? (new Vector3Int(+1, +1, 0), new Vector3Int(0, -1, 0))
                    : (new Vector3Int(0, +1, 0), new Vector3Int(-1, -1, 0));

            case 2: // NW <-> SE
                return odd
                    ? (new Vector3Int(0, +1, 0), new Vector3Int(+1, -1, 0))
                    : (new Vector3Int(-1, +1, 0), new Vector3Int(0, -1, 0));
        }
        return (Vector3Int.zero, Vector3Int.zero);
    }

    // 델타가 어느 축(0/1/2)에 속하는지 판별(원점 독립)
    public static int AxisFromDelta(Vector3Int d)
    {
        // 축0 후보
        if (d == new Vector3Int(+1, 0, 0) || d == new Vector3Int(-1, 0, 0)) return 0;

        // 축1/축2는 행 패리티에 따라 모양이 달라지므로,
        // 두 축의 모든 가능 델타를 한 번에 체크
        if (d == new Vector3Int(0, +1, 0) || d == new Vector3Int(+1, +1, 0) ||
            d == new Vector3Int(0, -1, 0) || d == new Vector3Int(-1, -1, 0))
            return 1; // NE/SW 축

        if (d == new Vector3Int(-1, +1, 0) || d == new Vector3Int(0, +1, 0) ||
            d == new Vector3Int(+1, -1, 0) || d == new Vector3Int(0, -1, 0))
            return 2; // NW/SE 축

        return 0; // fallback
    }
}
