using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EaseType { EaseOutQuad, EaseInOutQuad, EaseOutBack }

public static class Ease
{
    public static float Apply(EaseType t, float x)
    {
        x = Mathf.Clamp01(x);
        switch (t)
        {
            case EaseType.EaseOutQuad: return 1f - (1f - x) * (1f - x);
            case EaseType.EaseInOutQuad: return x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;
            case EaseType.EaseOutBack: { float c1 = 1.70158f, c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2); }
            default: return x;
        }
    }
}
public class ScaleTweener : MonoBehaviour
{
    public static ScaleTweener Instance { get; private set; }

    // 타겟별로 돌아가는 코루틴 추적
    private readonly Dictionary<Transform, Coroutine> _running = new();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 씬 바뀌면 전부 취소(파괴된 트랜스폼 만지지 않게)
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (_, __) => CancelAll();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        CancelAll();
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= (_, __) => CancelAll();
    }

    public void Cancel(Transform tr)
    {
        if (tr && _running.TryGetValue(tr, out var co))
        {
            StopCoroutine(co);
        }
        _running.Remove(tr);
    }

    public void CancelAll()
    {
        foreach (var kv in _running) if (kv.Value != null) StopCoroutine(kv.Value);
        _running.Clear();
    }

    public Coroutine PopIn(Transform tr, float duration, bool unscaled, Vector3? targetScale = null, EaseType ease = EaseType.EaseOutBack)
    {
        if (!tr) return null;
        tr.localScale = Vector3.zero;
        return StartTracked(tr, Co_Scale(tr, Vector3.zero, targetScale ?? Vector3.one, duration, unscaled, ease, null));
    }

    public Coroutine PopOut(Transform tr, float duration, bool unscaled, bool deactivateAtEnd = true, EaseType ease = EaseType.EaseInOutQuad)
    {
        if (!tr) return null;
        return StartTracked(tr, Co_Scale(tr, tr.localScale, Vector3.zero, duration, unscaled, ease, deactivateAtEnd ? tr.gameObject : null));
    }

    public Coroutine ScaleTo(Transform tr, Vector3 to, float duration, bool unscaled, EaseType ease = EaseType.EaseInOutQuad)
    {
        if (!tr) return null;
        return StartTracked(tr, Co_Scale(tr, tr.localScale, to, duration, unscaled, ease, null));
    }

    Coroutine StartTracked(Transform tr, IEnumerator co)
    {
        // 기존 트윈 있으면 취소
        Cancel(tr);
        var c = StartCoroutine(co);
        if (tr) _running[tr] = c;
        return c;
    }

    IEnumerator Co_Scale(Transform tr, Vector3 from, Vector3 to, float dur, bool unscaled, EaseType ease, GameObject deactivateAtEnd)
    {
        // 중요: 루프 매 프레임마다 파괴 여부 확인
        float t = 0f;
        while (t < dur)
        {
            if (!tr) yield break;                    // 파괴되었으면 즉시 종료
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Ease.Apply(ease, t / Mathf.Max(0.0001f, dur));
            tr.localScale = Vector3.LerpUnclamped(from, to, u);
            yield return null;
        }
        if (!tr) yield break;                        // 마무리에서도 체크
        tr.localScale = to;
        if (deactivateAtEnd) deactivateAtEnd.SetActive(false);

        // 완료 후 트래킹 해제
        _running.Remove(tr);
    }
}
