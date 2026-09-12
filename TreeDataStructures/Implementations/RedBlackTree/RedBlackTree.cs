using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        if (IsBlack(newNode))
            return;

        if (newNode.Parent is null)
        {
            newNode.Color = RbColor.Black;
            return;
        }

        RbFixInsert(newNode);
    }
    
    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
    }

    protected override void RemoveNode(RbNode<TKey, TValue> node)
    {
        RbNode<TKey, TValue> toDelete = node;

        if (node.Left is not null && node.Right is not null)
        {
            toDelete = node.Right;
            while (toDelete.Left is not null)
            {
                toDelete = toDelete.Left;
            }

            node.Key = toDelete.Key;
            node.Value = toDelete.Value;
        }

        RbNode<TKey, TValue>? child = toDelete.Left ?? toDelete.Right;
        RbNode<TKey, TValue>? parent = toDelete.Parent;
        bool deletedWasBlack = IsBlack(toDelete);

        Transplant(toDelete, child);

        if (!deletedWasBlack)
            return;

        if (IsRed(child))
        {
            SetColor(child, RbColor.Black);
            return;
        }

        RbFixRemove(child, parent);
    }
    
    private static bool IsRed(RbNode<TKey, TValue>? node)
        => node is not null && node.Color == RbColor.Red;

    private static bool IsBlack(RbNode<TKey, TValue>? node)
        => node is null || node.Color == RbColor.Black;

    private static void SetColor(RbNode<TKey, TValue>? node, RbColor color)
    {
        if (node is not null)
        {
            node.Color = color;
        }
    }

    private void RbFixInsert(RbNode<TKey, TValue> node)
    {
        if (IsBlack(node.Parent))
            return;

        var parent = node.Parent!;
        var grandparent = parent.Parent;

        if (grandparent is null)
        {
            parent.Color = RbColor.Black;
            return;
        }

        var uncle = parent.IsLeftChild ? grandparent.Right : grandparent.Left;

        if (IsRed(uncle))
        {
            SetColor(parent, RbColor.Black);
            SetColor(uncle, RbColor.Black);
            SetColor(grandparent, RbColor.Red);
            RbFixInsert(grandparent);
            return;
        }

        if (parent.IsLeftChild)
        {
            if (node.IsRightChild)
            {
                RotateLeft(parent);
                parent = node;
            }

            RotateRight(grandparent);
            SetColor(parent, RbColor.Black);
            SetColor(grandparent, RbColor.Red);
        }
        else
        {
            if (node.IsLeftChild)
            {
                RotateRight(parent);
                parent = node;
            }

            RotateLeft(grandparent);
            SetColor(parent, RbColor.Black);
            SetColor(grandparent, RbColor.Red);
        }
    }

    private void RbFixRemove(RbNode<TKey, TValue>? node, RbNode<TKey, TValue>? parent)
    {
        while (node != Root && IsBlack(node))
        {
            if (parent is null)
                break;

            bool isLeft = node == parent.Left || (node is null && parent.Left is null);
            var sibling = isLeft ? parent.Right : parent.Left;

            if (IsRed(sibling))
            {
                SetColor(sibling, RbColor.Black);
                SetColor(parent, RbColor.Red);

                if (isLeft) RotateLeft(parent);
                else RotateRight(parent);

                sibling = isLeft ? parent.Right : parent.Left;
            }

            if (sibling is null)
            {
                node = parent;
                parent = node.Parent;
                continue;
            }

            var nearNephew = isLeft ? sibling.Left : sibling.Right;
            var farNephew = isLeft ? sibling.Right : sibling.Left;

            if (IsBlack(nearNephew) && IsBlack(farNephew))
            {
                SetColor(sibling, RbColor.Red);
                node = parent;
                parent = node.Parent;
            }
            else
            {
                if (IsBlack(farNephew))
                {
                    SetColor(nearNephew, RbColor.Black);
                    SetColor(sibling, RbColor.Red);

                    if (isLeft) RotateRight(sibling);
                    else RotateLeft(sibling);

                    sibling = isLeft ? parent.Right : parent.Left;
                    farNephew = isLeft ? sibling!.Right : sibling!.Left;
                }

                SetColor(sibling, parent.Color);
                SetColor(parent, RbColor.Black);
                SetColor(farNephew, RbColor.Black);

                if (isLeft) RotateLeft(parent);
                else RotateRight(parent);

                node = Root;
            }
        }

        SetColor(node, RbColor.Black);
    }
}