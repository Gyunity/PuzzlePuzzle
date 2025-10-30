using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class InputHandler : MonoBehaviour
{
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private Tilemap tilemap;
    [SerializeField]
    private LayerMask gemLayer;

    private GameObject slectGem = null;
    private GameObject swapGem = null;

    private bool moveCheck = true;


    void Update()
    {
        if (!moveCheck || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {

            TryPickGem(Input.mousePosition, out slectGem);
            
        }
        else if (Input.GetMouseButton(0) && slectGem && !swapGem)
        {
            TryPickGem(Input.mousePosition, out swapGem);
            GemSwap(slectGem, swapGem);
        }
    }

    private void TryPickGem(Vector3 screenPos, out GameObject hitGem)
    {
        Vector3 world = ScreenToWorldOnTilePlane(screenPos);
        Vector2 origin = world;             
        Vector2 dir = Vector2.zero;         


        RaycastHit2D hit = Physics2D.Raycast(origin, dir, 1000, gemLayer);

        if (hit)
        {
            if (hit.transform.gameObject == slectGem)
            {
                hitGem = null;
            }
            else
            {
                hitGem = hit.transform.gameObject;
            }
        }
        else
        {
            hitGem = null;
        }


    }

    private Vector3 ScreenToWorldOnTilePlane(Vector3 screenPos)
    {
        float planeZ = tilemap.transform.position.z;
        Vector3 sp = new(
            screenPos.x, screenPos.y,
            cam.orthographic ? cam.nearClipPlane : Mathf.Abs(planeZ - cam.transform.position.z)
        );
        return cam.ScreenToWorldPoint(sp);
    }

    private void GemSwap(GameObject aGem, GameObject bGem)
    {
        if (slectGem != null && swapGem != null)
        {
            StartCoroutine(SwapPosition(aGem.transform, bGem.transform));

            slectGem = null;
            swapGem = null;

        }

    }

    private IEnumerator SwapPosition(Transform aGem, Transform bGem, float duration = 0.25f)
    {
        moveCheck = false;
        Vector3 aStart = aGem.position;
        Vector3 bStart = bGem.position;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float du = Mathf.Clamp01(time/duration);

            aGem.position = Vector3.LerpUnclamped(aStart, bStart, du);
            bGem.position = Vector3.LerpUnclamped(bStart, aStart, du);
            yield return null;
        }
        aGem.position = bStart;
        bGem.position = aStart;

        yield return new WaitForSeconds(duration);

        time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float du = Mathf.Clamp01(time / duration);

            aGem.position = Vector3.LerpUnclamped(bStart, aStart, du);
            bGem.position = Vector3.LerpUnclamped(aStart, bStart, du);
            yield return null;
        }
        aGem.position = aStart;
        bGem.position = bStart;
        moveCheck = true;
    }
}
