using System.Collections.Generic;

namespace AVVI94.Breakpoints.Avalonia.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// An ordered list of breakpoints with keys and values
/// </summary>
public sealed class BreakpointList : IEnumerable, IEnumerable<KeyValuePair<string,double>>
{
    private readonly Dictionary<string, double> _keyValuePairs = [];
    private readonly SortedSet<(double Value, string Key)> _sortedValues = [];

    /// <summary>
    /// Provides a read-only view of the items in the list
    /// </summary>
    public IReadOnlyDictionary<string, double> Items => _keyValuePairs;

    /// <summary>
    /// Add a new key-value pair to the list
    /// </summary>
    /// <param name="key">Breakpoint name</param>
    /// <param name="value">Breakpoint value</param>
    /// <exception cref="ArgumentException"> Thrown if the breakpoint name or value is already present in the collection.</exception>
    public void Add(string key, double value)
    {
        if (_keyValuePairs.ContainsKey(key))
            throw new ArgumentException("The key already exists.");

        if (!_sortedValues.Add((value, key)))
            throw new ArgumentException("The value already exists.");

        _keyValuePairs[key] = value;
    }

    /// <summary>
    /// Add a new key-value pair to the list, inteded to be used with collection initializers
    /// </summary>
    /// <param name="item">Breakpoint</param>
    public void Add((string Key, double Value) item) => Add(item.Key, item.Value);

    /// <summary>
    /// Try to get the value of a breakpoint
    /// </summary>
    /// <param name="key">Breakpoint name</param>
    /// <param name="value">[OUT] Value of the breakpoint</param>
    /// <returns></returns>
    public bool TryGetValue(string key, out double value) => _keyValuePairs.TryGetValue(key, out value);

    /// <summary>
    /// Remove a breakpoint from the list
    /// </summary>
    /// <param name="key">Name of the breakpoint</param>
    /// <returns></returns>
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

    /// <summary>
    /// Get or set the value of a breakpoint
    /// </summary>
    /// <param name="key">Name of the breakpoint</param>
    /// <returns>Value of the breakpoint</returns>
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
    /// <summary>
    /// Get the key-value pair at the specified index
    /// </summary>
    /// <param name="index">Breakpoint index</param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
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
    /// <summary>
    /// Number of breakpoints in the list
    /// </summary>
    public int Count => _keyValuePairs.Count;

    /// <summary>
    /// Find the next breakpoint with a value greater than the specified value.
    /// </summary>
    /// <param name="value">Searched value</param>
    /// <returns>Breakpoint name and value or <see langword="null"/> if no next breakpoint was found.</returns>
    public KeyValuePair<string, double>? FindNext(double value)
    {
        var next = _sortedValues.FirstOrDefault(x => x.Value > value);
        return next == default ? null : new(next.Key, next.Value);
    }

    /// <summary>
    /// Find the previous breakpoint with a value less than the specified value.
    /// </summary>
    /// <param name="value">Searched value</param>
    /// <returns>
    /// Breakpoint name and value or <see langword="null"/> if no previous breakpoint was found.
    /// </returns>
    public KeyValuePair<string, double>? FindPrevious(double value)
    {
        var previous = _sortedValues.LastOrDefault(x => x.Value < value);
        return previous == default ? null : new(previous.Key, previous.Value);
    }

    /// <summary>
    /// Get an enumerator for the breakpoints. Implemented mainly for collection initializers.
    /// </summary>
    public IEnumerator GetEnumerator()
    {
        return ((IEnumerable)Items).GetEnumerator();
    }

    IEnumerator<KeyValuePair<string, double>> IEnumerable<KeyValuePair<string, double>>.GetEnumerator()
    {
        return Items.GetEnumerator();
    }
}
