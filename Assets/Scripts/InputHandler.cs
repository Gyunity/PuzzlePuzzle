using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.Tilemaps;

public class InputHandler : MonoBehaviour
{
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private LayerMask gemLayer;
    [SerializeField]
    private TileBoardManager tileBoardManager;

    private Vector3Int slectGem;
    private Vector3Int swapGem;

    private Vector3Int resetVec = new Vector3Int(99999, 9999, 9999);

    private void Start()
    {
    }

    void Update()
    {
        if (!tileBoardManager.moveCheck || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            TryPickGemCell(Input.mousePosition, out slectGem);
            Debug.Log(slectGem);
        }
        else if (Input.GetMouseButton(0) && slectGem != resetVec)
        {
            if (TryPickGemCell(Input.mousePosition, out swapGem))
            {
                foreach (var n in tileBoardManager.Neighbor6(slectGem))
                {
                    if (n == swapGem)
                    {
                        tileBoardManager.TrySwap(slectGem, swapGem);
                    }
                }

            }
        }
    }



    private Vector3 ScreenToWorldOnTilePlane(Vector3 screenPos)
    {
        float planeZ = tilemap.transform.position.z;
        Vector3 sp = new(screenPos.x, screenPos.y, cam.nearClipPlane);
        return cam.ScreenToWorldPoint(sp);
    }
    private bool TryPickGemCell(Vector3 screenPos, out Vector3Int cell)
    {
        Gem gem;
        Vector3 world = ScreenToWorldOnTilePlane(screenPos);
        var col = Physics2D.OverlapPoint(world, gemLayer);
        if (col)
        {
            gem = col.GetComponentInParent<Gem>();
            if (gem != null)
            {
                cell = tilemap.WorldToCell(gem.transform.position);
                if (cell == slectGem)
                    return false;
                return true;
            }
        }
        gem = null;
        cell = resetVec;
        return false;
    }

}
