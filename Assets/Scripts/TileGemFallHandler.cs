using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileGemFallHandler
{
  public void ApplyGravity(Tilemap tilemap, Dictionary<Vector3Int, Gem> gemMap)
    {
        List<Vector3Int> positions = new List<Vector3Int>(gemMap.Keys);
        positions.Sort((a, b) => b.y.CompareTo(a.y));

        foreach(var pos in positions)
        {
            //pos 타일에 뭐가 있으면 계속
            if (!tilemap.HasTile(pos) || gemMap[pos] != null)
                continue;

            //pos 타일 위
            Vector3Int above = pos + new Vector3Int(0, 1, 0);
            while (tilemap.HasTile(above))
            {
                if(gemMap.ContainsKey(above) && gemMap[above] != null)
                {
                    gemMap[pos] = gemMap[above];
                    gemMap[above] = null;
                    gemMap[pos].transform.position = tilemap.CellToWorld(pos) + tilemap.tileAnchor;
                    break;
                }
                above += new Vector3Int(0, 1, 0);
            }
        }


    }
}
