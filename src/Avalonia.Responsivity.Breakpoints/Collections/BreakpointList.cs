using System.Collections.Generic;

namespace Avalonia.Responsivity.Breakpoints.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public sealed class BreakpointList : IEnumerable
{
    private readonly Dictionary<string, double> _keyValuePairs = [];
    private readonly SortedSet<(double Value, string Key)> _sortedValues = [];

    public IReadOnlyDictionary<string, double> Items => _keyValuePairs;

    public void Add(string key, double value)
    {
        if (_keyValuePairs.ContainsKey(key))
            throw new ArgumentException("The key already exists.");

        if (!_sortedValues.Add((value, key)))
            throw new ArgumentException("The value already exists.");

        _keyValuePairs[key] = value;
    }

    public void Add((string Key, double Value) item) => Add(item.Key, item.Value);

    public bool TryGetValue(string key, out double value) => _keyValuePairs.TryGetValue(key, out value);

    public bool Remove(string key)
    {
        if (_keyValuePairs.TryGetValue(key, out double value))
        {
            _keyValuePairs.Remove(key);
            _sortedValues.Remove((value, key));
            return true;
        }
        return false;
    }

    public double this[string key]
    {
        get => _keyValuePairs[key];
        set
        {
            if (_keyValuePairs.ContainsKey(key))
            {
                var oldValue = _keyValuePairs[key];
                _sortedValues.Remove((oldValue, key));
                _keyValuePairs[key] = value;
                _sortedValues.Add((value, key));
            }
            else
            {
                Add(key, value);
            }
        }
    }

    public (string Key, double Value) this[int index]
    {
        get
        {
            if (index < 0 || index >= _sortedValues.Count)
                throw new IndexOutOfRangeException();

            var item = _sortedValues.ElementAt(index);
            return (item.Key, item.Value);
        }
    }

    public int Count => _keyValuePairs.Count;

    public KeyValuePair<string, double>? FindNext(double value)
    {
        var next = _sortedValues.FirstOrDefault(x => x.Value > value);
        return next == default ? null : new(next.Key, next.Value);
    }

    public KeyValuePair<string, double>? FindPrevious(double value)
    {
        var previous = _sortedValues.LastOrDefault(x => x.Value < value);
        return previous == default ? null : new(previous.Key, previous.Value);
    }

    public IEnumerator GetEnumerator()
    {
        return ((IEnumerable)Items).GetEnumerator();
    }
}
