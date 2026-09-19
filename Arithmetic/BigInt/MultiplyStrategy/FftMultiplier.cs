using System;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    private const int RecursionThreshold = 64;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null || b is null)
            throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));

        var aDigits = a.GetDigits();
        var bDigits = b.GetDigits();

        if (IsZeroSpan(aDigits) || IsZeroSpan(bDigits))
            return new BetterBigInteger([0]);

        var totalBits = (aDigits.Length + bDigits.Length) * 32;
        var rnd2N = NextPowerOfTwo(totalBits);

        var result = SchonhageStrassen(a, b, rnd2N);
        var isNeg = a.IsNegative != b.IsNegative;
        return new BetterBigInteger(result.GetDigits().ToArray(), isNeg);
    }

    private BetterBigInteger SchonhageStrassen(BetterBigInteger a, BetterBigInteger b, int rnd2N)
    {
        if (rnd2N <= RecursionThreshold)
            return new KaratsubaMultiplier().Multiply(a, b);

        // m число блоков, n битовая длина блока
        var halfLog = Log2(rnd2N) / 2;
        var m = 1 << halfLog;
        var n = rnd2N / m;

        // K больше 2n + log2(m) = 2n + halfLog
        var K = NextPowerOfTwo(2 * n + halfLog + 1);

        var aBlocks = SplitIntoBlocks(a, m, n);
        var bBlocks = SplitIntoBlocks(b, m, n);

        Ntt(aBlocks, K, invert: false);
        Ntt(bBlocks, K, invert: false);

        var cBlocks = new BetterBigInteger[aBlocks.Length];
        for (int i = 0; i < cBlocks.Length; i++)
        {
            var product = SchonhageStrassen(aBlocks[i], bBlocks[i], K);
            cBlocks[i] = ReduceMod(product, K);
        }

        Ntt(cBlocks, K, invert: true);
        for (var i = 0; i < cBlocks.Length; i++)
            cBlocks[i] = ReduceMod(cBlocks[i], K);

        return CombineBlocks(cBlocks, n);
    }

    private static void BitReversalPermutation(BetterBigInteger[] a)
    {
        var n = a.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            while ((j & bit) != 0)
            {
                j ^= bit;
                bit >>= 1;
            }
            j ^= bit;
            if (i < j) (a[i], a[j]) = (a[j], a[i]);
        }
    }

    private void ButterflyPass(BetterBigInteger[] a, int K, bool invert)
    {
        var n = a.Length;
        for (var len = 2; len <= n; len <<= 1)
        {
            var halfLen = len >> 1;
            for (var block = 0; block < n; block += len)
                ButterflyBlock(a, block, halfLen, K, invert);
        }
    }

    private void ButterflyBlock(BetterBigInteger[] a, int block, int halfLen, int K, bool invert)
    {
        for (var k = 0; k < halfLen; k++)
        {
            // Шаг корня w = 2^step
            var n = halfLen * 2;
            var step = (k * (2 * K / n));
            if (invert)
                step = (2 * K - step) % (2 * K);

            var u = a[block + k];
            var v = ModShift(a[block + k + halfLen], step, K);
            a[block + k] = ModAdd(u, v, K);
            a[block + k + halfLen] = ModSub(u, v, K);
        }
    }

    private static void DivideByN(BetterBigInteger[] a)
    {
        var logN = Log2(a.Length);
        for (var i = 0; i < a.Length; i++)
            a[i] >>= logN;
    }

    internal void Ntt(BetterBigInteger[] a, int K, bool invert)
    {
        BitReversalPermutation(a);
        ButterflyPass(a, K, invert);
        if (invert)
            DivideByN(a);
    }

    internal BetterBigInteger ModShift(BetterBigInteger val, int k, int K)
    {
        k %= 2 * K;
        if (k == 0) return val;

        if (k < K)
        {
            var mask = (One() << K) - One();
            var low = (val << k) & mask;
            var high = val >> (K - k);
            return ModSub(low, high, K);
        }
        else
        {
            return ModNeg(ModShift(val, k - K, K), K);
        }
    }

    private static BetterBigInteger ModAdd(BetterBigInteger a, BetterBigInteger b, int K)
    {
        var res = a + b;
        var mod = (One() << K) + One(); // 2^K + 1
        return res >= mod ? res - mod : res;
    }

    private static BetterBigInteger ModSub(BetterBigInteger a, BetterBigInteger b, int K)
    {
        if (a >= b) return a - b;
        var mod = (One() << K) + One();
        return (a + mod) - b;
    }

    private static BetterBigInteger ModNeg(BetterBigInteger val, int K)
    {
        if (IsZeroSpan(val.GetDigits())) return val;
        var mod = (One() << K) + One();
        return mod - val;
    }

    internal static BetterBigInteger ReduceMod(BetterBigInteger val, int K)
    {
        var mod = (One() << K) + One();
        var mask = (One() << K) - One();
        var low = val & mask;
        var high = val >> K;
        var res = ModSub(low, high, K);

        if (res < Zero())
            res += mod;
        if (res >= mod)
            res -= mod;

        return res;
    }

    private static BetterBigInteger[] SplitIntoBlocks(BetterBigInteger val, int m, int n)
    {
        var blocks = new BetterBigInteger[2 * m];
        var mask = (One() << n) - One();
        for (var i = 0; i < m; i++)
            blocks[i] = (val >> (i * n)) & mask;
        for (var i = m; i < 2 * m; i++)
            blocks[i] = Zero();
        return blocks;
    }

    private static BetterBigInteger CombineBlocks(BetterBigInteger[] blocks, int n)
    {
        var res = Zero();
        for (int i = 0; i < blocks.Length; i++)
            res += blocks[i] << (i * n);
        return res;
    }

    private static BetterBigInteger Zero() => new([0]);
    private static BetterBigInteger One() => new([1]);

    private static bool IsZeroSpan(ReadOnlySpan<uint> digits)
    {
        foreach (var d in digits)
            if (d != 0)
                return false;
        return true;
    }

    private static int NextPowerOfTwo(int val)
    {
        int res = 1;
        while (res < val) res <<= 1;
        return res;
    }

    private static int Log2(int val)
    {
        int res = 0;
        while ((1 << res) < val) res++;
        return res;
    }
}