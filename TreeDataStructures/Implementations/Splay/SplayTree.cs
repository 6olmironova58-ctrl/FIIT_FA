using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
    }

    protected override void OnNodeAccessed(BstNode<TKey, TValue> node)
    {
        Splay(node);
    }

    protected override void RemoveNode(BstNode<TKey, TValue> node)
    {
        Splay(node);
        var (left, right) = Split(node);
        Root = Merge(left, right);
    }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return base.TryGetValue(key, out value);
    }

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent is not null)
        {
            var parent = node.Parent;
            var grandParent = parent.Parent;

            if (grandParent is null) {
                if (node.IsLeftChild)
                    RotateRight(parent);
                else
                    RotateLeft(parent);
            }
            else if (node.IsLeftChild && parent.IsLeftChild)
                RotateDoubleRight(grandParent);
            else if (!node.IsLeftChild && !parent.IsLeftChild)
                RotateDoubleLeft(grandParent);
            else if (node.IsLeftChild)
                RotateBigLeft(grandParent);
            else
                RotateBigRight(grandParent);
        }
    }

    private (BstNode<TKey, TValue>? Left, BstNode<TKey, TValue>? Right) Split(BstNode<TKey, TValue> node)
    {
        var left = node.Left;
        var right = node.Right;

        if (left is not null)
            left.Parent = null;

        if (right is not null)
            right.Parent = null;

        return (left, right);
    }

    private BstNode<TKey, TValue>? Merge(BstNode<TKey, TValue>? left, BstNode<TKey, TValue>? right)
    {
        if (left is null)
            return right;

        if (right is null)
            return left;

        Root = left;

        var current = left;
        while (current.Right is not null)
            current = current.Right;

        Splay(current);

        current.Right = right;
        right.Parent = current;

        return current;
    }
}
