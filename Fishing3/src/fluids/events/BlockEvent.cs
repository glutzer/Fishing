using System;
using System.Collections.Generic;

namespace Fishing3;

/// <summary>
/// Generic event.
/// </summary>
public class BlockEvent<T1>
{
    private readonly List<(Action<T1>, float)> orderedList = new();

    public void Register(Action<T1> subscriber, float order = 0f)
    {
        for (int i = 0; i < orderedList.Count; i++)
        {
            if (orderedList[i].Item2 > order)
            {
                orderedList.Insert(i, (subscriber, order));
                return;
            }
        }

        orderedList.Add((subscriber, order));
    }

    public void Invoke(T1 param1)
    {
        foreach ((Action<T1> subscriber, float _) in orderedList)
        {
            subscriber(param1);
        }
    }

    public void Unregister(Action<T1> subscriber)
    {
        for (int i = 0; i < orderedList.Count; i++)
        {
            if (orderedList[i].Item1 == subscriber)
            {
                orderedList.RemoveAt(i);
                return;
            }
        }
    }
}