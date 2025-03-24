using System;
using System.Collections.Generic;

namespace Fishing3;


/// <summary>
/// Returns true if all events return true.
/// </summary>
public class CheckBlockEvent<T1>
{
    private readonly List<(Func<T1, bool>, float)> orderedList = new();

    public void Register(Func<T1, bool> subscriber, float order = 0f)
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

    /// <summary>
    /// Calls event with params. Returns false if any checks are false.
    /// </summary>
    public bool Invoke(T1 param1)
    {
        bool valid = true;

        foreach ((Func<T1, bool> subscriber, float _) in orderedList)
        {
            valid &= subscriber(param1);
            if (valid == false) return false;
        }

        return true;
    }

    public void Unregister(Func<T1, bool> subscriber)
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