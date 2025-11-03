using System.Linq;
using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    void Awake()
    {
        // 혹시 Pause 상태였다면 복구
        Time.timeScale = 1f;

        // 트윈 등 진행 중 애니메이션 취소
        ScaleTweener.Instance?.CancelAll();
    }

    void Start()
    {
        // 비활성 포함 전체에서 IResettable 전부 실행
        var all = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                  .OfType<IResettable>();
        foreach (var r in all) r.ResetState();
    }
}
