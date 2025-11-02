using System;
using UnityEngine;

public class MoveCountService : MonoBehaviour, IMoveCount
{
    [SerializeField]
    private int moveCount = 10;
    public int Current {  get; private set; }

    public event Action<int> Changed;
    public event Action Done;
    private void Awake()
    {
        Current = Mathf.Max(0, moveCount);
        Changed?.Invoke(Current);
    }
    public void Decrement(int amount = 1)
    {
        if (Current <= 0)
            return;
        Current = Mathf.Max(0, Current - Mathf.Max(1, amount));
        Changed?.Invoke(Current);
        if (Current == 0)
            Done?.Invoke();
    }
}
