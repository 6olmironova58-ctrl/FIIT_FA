using System;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private readonly int _threshold;
    private readonly SimpleMultiplier _simple = new();

    public KaratsubaMultiplier(int threshold = 32)
    {
        if (threshold < 1)
            throw new ArgumentException("Threshold must be at least 1", nameof(threshold));
        
        _threshold = threshold;
    }

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        bool isNeg = a.IsNegative ^ b.IsNegative;
        
        uint[] aDigits = a.GetDigits().ToArray();
        uint[] bDigits = b.GetDigits().ToArray();
        
        uint[] result = MultiplyMagnitude(aDigits, bDigits);
        return new BetterBigInteger(result, isNeg);
    }

    private uint[] MultiplyMagnitude(uint[] a, uint[] b)
    {
        if (a.Length <= _threshold || b.Length <= _threshold)
        {
            var aB = new BetterBigInteger(a, false);
            var bB = new BetterBigInteger(b, false);
            return _simple.Multiply(aB, bB).GetDigits().ToArray();
        }

        int m = Math.Max(a.Length, b.Length) / 2;

        uint[] aLo = Slice(a, 0, m);
        uint[] aHi = Slice(a, m, a.Length - m);
        uint[] bLo = Slice(b, 0, m);
        uint[] bHi = Slice(b, m, b.Length - m);

        uint[] z0 = MultiplyMagnitude(aLo, bLo);
        uint[] z2 = MultiplyMagnitude(aHi, bHi);

        // z1 = (aLo + aHi) * (bLo + bHi) - z0 - z2
        uint[] aSum = MagnitudeArithmetic.AddMagnitudes(aLo, aHi);
        uint[] bSum = MagnitudeArithmetic.AddMagnitudes(bLo, bHi);
        
        uint[] z1Raw = MultiplyMagnitude(aSum, bSum);
        uint[] z1 = MagnitudeArithmetic.SubtractMagnitudes(
                        MagnitudeArithmetic.SubtractMagnitudes(z1Raw, z0), 
                        z2);

        uint[] z1Shifted = ShiftLeft(z1, m);
        uint[] z2Shifted = ShiftLeft(z2, 2 * m);

        // result = z0 + z1 * B^m + z2 * B^2m
        return MagnitudeArithmetic.AddMagnitudes(
                   MagnitudeArithmetic.AddMagnitudes(z0, z1Shifted), 
                   z2Shifted);
    }

    private static uint[] Slice(uint[] a, int offset, int length)
    {
        if (offset >= a.Length) return [0];
        length = Math.Min(length, a.Length - offset);
        if (length <= 0) return [0];

        uint[] result = new uint[length];
        Array.Copy(a, offset, result, 0, length);
        return MagnitudeArithmetic.TrimMagnitude(result);
    }

    private static uint[] ShiftLeft(uint[] a, int limbCount)
    {
        if (a.Length == 1 && a[0] == 0) return [0];
        if (limbCount == 0) return a;

        uint[] result = new uint[a.Length + limbCount];
        Array.Copy(a, 0, result, limbCount, a.Length);
        return result;
    }
}