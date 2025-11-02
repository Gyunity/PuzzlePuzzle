using System;
using UnityEngine;

//점수 서비스
public class JackScoreService : MonoBehaviour, IScore
{
    [SerializeField]
    private int startScore = 15;
    public int Current {  get; private set; }

    public event Action<int> Changed;
    public event Action Cleared;

    private void Awake()
    {
        Current = Mathf.Max(0, startScore);
        Changed?.Invoke(Current);
    }
    public void Decrement(int amount = 1)
    {
        if (Current <= 0) 
            return;
        Current = Mathf.Max(0, Current - Mathf.Max(1, amount));
        Changed?.Invoke(Current);
        if (Current == 0) 
            Cleared?.Invoke();
    }

    
}
