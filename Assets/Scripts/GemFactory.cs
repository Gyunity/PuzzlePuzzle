using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GemFactory : MonoBehaviour
{
    [SerializeField]
    private Gem gemPrefab;


    public Gem CreateRandomGem(Tilemap tilemap, Vector3Int cell, Transform parent = null)
    {
        Vector3 world = tilemap.CellToWorld(cell) + tilemap.tileAnchor;
        if(parent == null)
            parent = tilemap.transform;

        GameObject go = Instantiate(gemPrefab.gameObject, world, Quaternion.identity, parent);
        GemType randomType = (GemType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(GemType)).Length);
        Gem gem = go.GetComponent<Gem>();

        Vector3 p = gem.transform.position;
        gem.transform.position = new Vector3(p.x, p.y, tilemap.transform.position.z);

        return gem;
    
    }

    public Gem CreateGemOfType(GemType type, Tilemap tilemap, Vector3Int cell, Transform parent = null)
    {
        Vector3 world = tilemap.CellToWorld(cell) + tilemap.tileAnchor;
        if(parent == null)
            parent = tilemap.transform;

        Gem gem = Instantiate(gemPrefab, world, Quaternion.identity, parent);
        gem.Init(type);

        Vector3 p = gem.transform.position;
        gem.transform.position = new Vector3(p.x, p.y, tilemap.transform.position.z);

        return gem;
    }

}
