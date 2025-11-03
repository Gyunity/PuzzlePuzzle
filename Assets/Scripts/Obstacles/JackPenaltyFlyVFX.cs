using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JackPenaltyFlyVFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] 
    private Camera worldCam;           // 비워두면 Camera.main
    [SerializeField] 
    private Canvas canvasUI;           // 점수 UI가 있는 Canvas
    [SerializeField] 
    private RectTransform scoreAnchor; // 왼쪽 상단 Jack 점수 아이콘 RectTransform
    [SerializeField] 
    private Sprite jackSprite;         // 날아갈 토큰 스프라이트(잭 얼굴 등)


    [Header("Flight")]
    // 월드에서 약간 위로
    [SerializeField, Min(0f)]
    private float spawnOffsetY = 0.2f; 
    // 1개 토큰 비행 시간
    [SerializeField, Min(0f)]
    private float duration = 0.45f;   
    
    // 화면 좌표 기준 살짝 포물선
    [SerializeField, Min(0f)]
    private float arcHeight = 80f;    

    [Header("Pooling")]

    [SerializeField, Min(1)]
    private int poolSize = 8;
    [Header("Token Visual")]
    //토큰 전체 크기 배수
    [SerializeField, Min(0.1f)]
    private float tokenScale = 2f; 
    [SerializeField]
    private bool preserveAspect = true;
    [SerializeField]
    private float delayTime = 0.6f;
    private readonly List<Image> _pool = new();
    private int _poolHead = 0;

    void Awake()
    {
        if (!worldCam) worldCam = Camera.main;
        if (!canvasUI) canvasUI = GetComponentInParent<Canvas>();
        BuildPool();
    }
    public void PlayForJacksParallel(IReadOnlyList<Vector3> jackWorldPositions, Action onEachArrive)
    {
        if (jackWorldPositions == null || jackWorldPositions.Count == 0) return;
        // 여러 개를 '동시에' 쏜다: 각각 개별 코루틴 시작, 여기서 Wait 안 함
        foreach (var wp in jackWorldPositions)
            StartCoroutine(Co_FlyOne(wp, onEachArrive));
    }
    private IEnumerator Co_FlyOne(Vector3 jackWorldPos, Action onArrive)
    {
        yield return new WaitForSeconds(delayTime);
        var startWorld = jackWorldPos + Vector3.up * spawnOffsetY;
        var startScreen = worldCam.WorldToScreenPoint(startWorld);
        var endScreen = RectTransformUtility.WorldToScreenPoint(null, scoreAnchor.position);

        var token = Rent();
        var rt = token.rectTransform;
        rt.SetAsLastSibling();
        float startS = 0.9f * tokenScale;
        float endS = 1.0f * tokenScale;
        rt.localScale = Vector3.one * startS;

        RectTransform canvasRT = canvasUI.transform as RectTransform;
        Camera uiCam = (canvasUI.renderMode == RenderMode.ScreenSpaceOverlay) ? null : worldCam;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, startScreen, uiCam, out var fromUI);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, endScreen, uiCam, out var toUI);

        rt.anchoredPosition = fromUI;
        Vector2 ctrl = Vector2.Lerp(fromUI, toUI, 0.5f) + Vector2.up * arcHeight;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            float e = EaseOutQuad(u);

            rt.anchoredPosition = QuadBezier(fromUI, ctrl, toUI, e);
            float s = Mathf.Lerp(startS, endS, e);
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        rt.anchoredPosition = toUI;
        token.gameObject.SetActive(false);

        onArrive?.Invoke();   // 도착 시 콜백
    }
    void BuildPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("JackToken", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvasUI.transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = jackSprite;
            img.raycastTarget = false;
            img.preserveAspect = preserveAspect;
            go.SetActive(false);
            _pool.Add(img);
        }
    }

    private Image Rent()
    {
        var img = _pool[_poolHead];
        _poolHead = (_poolHead + 1) % _pool.Count;
        img.gameObject.SetActive(true);
        return img;
    }

   


    static float EaseOutQuad(float t) => t * (2f - t);
    static Vector2 QuadBezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float it = 1f - t;
        return it * it * a + 2f * it * t * b + t * t * c;
    }
}
