using System;
using System.Collections.Generic;

namespace Fishing;

/// <summary>
/// Generic event.
/// </summary>
public class BlockEvent<T1>
{
    private List<(Action<T1>, float)>? orderedList;

    public void Register(Action<T1> subscriber, float order = 0f)
    {
        orderedList ??= new List<(Action<T1>, float)>(1);

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
        if (orderedList == null) return;

        foreach ((Action<T1> subscriber, float _) in orderedList)
        {
            subscriber(param1);
        }
    }

    public void Unregister(Action<T1> subscriber)
    {
        if (orderedList == null) return;

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