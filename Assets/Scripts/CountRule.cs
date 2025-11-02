using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class CountRule : MonoBehaviour, IDestructionListener
{
    [SerializeField]
    private MonoBehaviour countRef;

    private IMoveCount count;
    void Awake()
    {
        count = countRef as IMoveCount;
        if (count == null) Debug.LogError("[CountRule] countRef는 IMoveCount를 구현해야 합니다.");

    }

   

    public void OnGemsDestroyed(IReadOnlyCollection<Vector3Int> destroyed)
    {
        if (count == null || destroyed == null || destroyed.Count == 0)
            return;

        count.Decrement(1);
    }
}
