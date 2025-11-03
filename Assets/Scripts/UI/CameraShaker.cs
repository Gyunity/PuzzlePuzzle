using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [SerializeField] 
    Transform target;  
    [SerializeField] 
    float defaultAmplitude = 0.15f;

    Vector3 _origin;

    void Awake()
    {
        if (!target) target = Camera.main ? Camera.main.transform : transform;
        _origin = target.localPosition;
    }

    public void Shake(float duration, float amplitude = -1f)
    {
        if (amplitude <= 0f) amplitude = defaultAmplitude;
        StopAllCoroutines();
        StartCoroutine(Co_Shake(duration, amplitude));
    }

    private IEnumerator Co_Shake(float duration, float amp)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;                
            float k = 1f - Mathf.Clamp01(t / duration); 
            // 무작위 오프셋
            Vector3 off = new Vector3(
                (Random.value * 2f - 1f),
                (Random.value * 2f - 1f),
                0f) * amp * k;

            target.localPosition = _origin + off;
            yield return null;
        }
        target.localPosition = _origin;
    }
}
