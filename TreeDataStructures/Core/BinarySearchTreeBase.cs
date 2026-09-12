using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder()
        .Select(e => e.Key)
        .ToList();
    public ICollection<TValue> Values => InOrder()
        .Select(e => e.Value)
        .ToList();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        if (Root is null)
        {
            TNode newNode = CreateNode(key, value);
            Root = newNode;
            Count++;
            OnNodeAdded(newNode);
            return;
        }

        TNode current = Root;

        while (true)
        {
            int cmp = Comparer.Compare(key, current.Key);

            if (cmp == 0)
            {
                current.Value = value;
                OnNodeAccessed(current);
                return;
            }

            if (cmp < 0)
            {
                if (current.Left is null)
                {
                    TNode newNode = CreateNode(key, value);
                    current.Left = newNode;
                    newNode.Parent = current;

                    Count++;
                    OnNodeAdded(newNode);
                    return;
                }

                current = current.Left;
            }
            else
            {
                if (current.Right is null)
                {
                    TNode newNode = CreateNode(key, value);
                    current.Right = newNode;
                    newNode.Parent = current;

                    Count++;
                    OnNodeAdded(newNode);
                    return;
                }

                current = current.Right;
            }
        }
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }
    
    
    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left is null)
        {
            TNode? parent = node.Parent;
            TNode? child = node.Right;

            Transplant(node, child);
            OnNodeRemoved(parent, child);
        }
        else if (node.Right is null)
        {
            TNode? parent = node.Parent;
            TNode? child = node.Left;

            Transplant(node, child);
            OnNodeRemoved(parent, child);
        }
        else
        {
            TNode successor = node.Right!;

            while (successor.Left is not null)
            {
                successor = successor.Left;
            }

            TNode? parent = successor.Parent;
            TNode? child = successor.Right;

            Transplant(successor, child);

            node.Key = successor.Key;
            node.Value = successor.Value;

            OnNodeRemoved(parent, child);
        }
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    protected virtual void OnNodeAccessed(TNode node) { }
    
    #endregion
    
    
    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;

        while (current is not null)
        {
            int cmp = Comparer.Compare(key, current.Key);

            if (cmp == 0)
            {
                OnNodeAccessed(current);
                return current;
            }

            current = cmp < 0 ? current.Left : current.Right;
        }

        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x.Right is null) { return; }

        TNode y = x.Right;

        x.Right = y.Left;
        x.Right?.Parent = x;

        Transplant(x, y);

        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        if (y.Left is null) { return; }

        TNode x = y.Left;

        y.Left = x.Right;
        x.Right?.Parent = y;

        Transplant(y, x);

        x.Right = y;
        y.Parent = x;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        if (x.Right is null) { return; }

        RotateRight(x.Right);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
       if (y.Left is null) { return; }

        RotateLeft(y.Left);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        TNode? parent = x.Right;

        if (parent is null) { return; }

        RotateLeft(x);
        RotateLeft(parent);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        TNode? parent = y.Left;

        if (parent is null) { return; }

        RotateRight(y);
        RotateRight(parent);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.InOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.PreOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.PostOrder);

    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.InOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.PreOrderReverse);

    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() =>
        new TreeIterator<TKey, TValue, TNode>(Root, TraversalStrategy.PostOrderReverse);
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
        InOrder()
            .Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value))
            .GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (arrayIndex < 0 || arrayIndex > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("Destination is too small", nameof(array));
        }

        foreach (var entry in InOrder())
        {
            array[arrayIndex] = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
            arrayIndex++;
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}