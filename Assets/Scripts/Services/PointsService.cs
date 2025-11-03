using System;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PointsService : MonoBehaviour, IPoints, IResettable
{
    [SerializeField]
    int startPoints = 0;
    public int Current { get; private set; }

    public event Action<int> Changed;

    private void Awake()
    {
        Current = startPoints;
        Changed?.Invoke(Current);
    }
    public void Add(int amount)
    {
        if (amount == 0)
            return;
        Current += amount;
        Changed?.Invoke(Current);
    }

    public void Reset(int value = 0)
    {
        Current = value;
        Changed?.Invoke(Current);
    }

    public void ResetState()
    {
        Current = startPoints;
        Changed?.Invoke(Current);
    }
}
