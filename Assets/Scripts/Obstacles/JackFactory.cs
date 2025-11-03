using UnityEngine;
using UnityEngine.Tilemaps;

public class JackFactory : MonoBehaviour
{
    public System.Action<Vector3Int, JackVisuals> Created;
   
    public Jack jackPrefab;

    public JackVisuals CreatJack(Tilemap tilemap, Vector3Int cell, Transform parent)
    {
        var go = Instantiate(jackPrefab, parent);
        go.transform.position = tilemap.CellToWorld(cell) + tilemap.tileAnchor;

        var jv = go.GetComponent<JackVisuals>();
        if (jv) jv.SetCell(cell);

        // 만들어졌음을 브로드캐스트
        Created?.Invoke(cell, jv);
        return jv;
    }
}
