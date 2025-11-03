using System;
using UnityEngine;

public class MoveCountService : MonoBehaviour, IMoveCount, IResettable
{
    [SerializeField]
    private int moveCount = 10;
    public int Current { get; private set; }

    public event Action<int> Changed;
    public event Action Done;
    public bool IsDepleted => Current <= 0;
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

    public void ResetState()
    {
        Current = moveCount;
        Changed?.Invoke(Current);
    }
    public void Add(int amount)
    {
        if (amount <= 0) return;
        var wasDepleted = IsDepleted;

        Current += amount;
        Changed?.Invoke(Current);
    }
}
