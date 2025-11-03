using System.Collections;
using UnityEngine;

public class VibrationManager : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject unityActivity;
    static AndroidJavaObject vibrator;
    static AndroidJavaClass vibrationEffectClass;
    static bool init;

    static void EnsureInit()
    {
        if (init) return;
        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            unityActivity   = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            vibrator        = unityActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
            init = true;
        }
        catch { init = false; }
    }

    public static void VibrateMillis(int ms)
    {
        EnsureInit();
        try
        {
            if (vibrator != null && vibrationEffectClass != null)
            {
                var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, 255 /* amplitude max */);
                vibrator.Call("vibrate", effect);
                return;
            }
        }
        catch { /* fallback */ }

        // 최소 폴백
        Handheld.Vibrate();
    }
#else
    public static void VibrateMillis(int ms)
    {
        // iOS/PC/에디터 폴백: 제어 불가 → 짧은 단일 진동
        Handheld.Vibrate();
    }
#endif
}
