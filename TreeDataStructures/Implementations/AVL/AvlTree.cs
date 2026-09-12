using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        FixTreeStructure(newNode.Parent, stopAfterRebalance: true);
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        FixTreeStructure(parent);
    }

    #region AVL Helpers

    private static int GetHeight(AvlNode<TKey, TValue>? node)
        => node?.Height ?? 0;

    private int GetBalance(AvlNode<TKey, TValue> node) {
        if (node is null) return 0;
        return GetHeight(node.Left) - GetHeight(node.Right);
    }

    private void UpdateHeight(AvlNode<TKey, TValue>? node)
        => node?.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));

    private void BalanceNode(AvlNode<TKey, TValue> node)
    {
        UpdateHeight(node);
        int balance = GetBalance(node);

        if (balance > 1)
        {
            if (GetBalance(node.Left) >= 0) { RotateRight(node); }
            else { RotateBigRight(node); }
        }
        else if (balance < -1)
        {
            if (GetBalance(node.Right) <= 0) { RotateLeft(node); }
            else { RotateBigLeft(node); }
        }

            UpdateHeight(node);
        if (node.Parent is not null)
        {
            UpdateHeight(node.Parent.Left);
            UpdateHeight(node.Parent.Right);
            UpdateHeight(node.Parent);
        }
    }

    private void FixTreeStructure(AvlNode<TKey, TValue>? node, bool stopAfterRebalance = false)
    {
        while (node is not null)
        {
            int oldHeight = node.Height;

            UpdateHeight(node);

            if (GetBalance(node) is > 1 or < -1)
            {
                BalanceNode(node);
                if (stopAfterRebalance) break;
            }
            else if (node.Height == oldHeight) break;

            node = node.Parent;
        }
    }

    #endregion
}