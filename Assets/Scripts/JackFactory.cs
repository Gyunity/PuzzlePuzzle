using UnityEngine;
using UnityEngine.Tilemaps;

public class JackFactory : MonoBehaviour
{
    [SerializeField]
    private Jack jackPrefab;
  
    public Jack CreatJack(Tilemap tilemap, Vector3Int cell, Transform root)
    {
        var pos = tilemap.CellToWorld(cell) + tilemap.tileAnchor;
        var jack = Instantiate(jackPrefab, pos, Quaternion.identity, root);
        return jack;
    }
}
