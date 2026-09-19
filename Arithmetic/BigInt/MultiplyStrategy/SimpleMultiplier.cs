using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ReadOnlySpan<uint> aDigs = a.GetDigits();
        ReadOnlySpan<uint> bDigs = b.GetDigits();

        if (IsZero(aDigs) || IsZero(bDigs))
            return new BetterBigInteger([0u], false);

        uint[] result = new uint[aDigs.Length + bDigs.Length];

        for (int i = 0; i < aDigs.Length; i++)
        {
            uint aVal = aDigs[i];
            if (aVal == 0) continue;

            uint carryLo = 0;
            uint carryHi = 0;

            for (int j = 0; j < bDigs.Length; j++)
            {
                Multiply16x16To32x2(aVal, bDigs[j], out uint productLo, out uint productHi);

                uint sum1 = result[i + j] + productLo;
                uint c1 = (sum1 < result[i + j]) ? 1u : 0u;

                uint sum2 = sum1 + carryLo;
                uint c2 = (sum2 < sum1) ? 1u : 0u;

                result[i + j] = sum2;

                uint hiSum1 = productHi + carryHi;
                uint c3 = (hiSum1 < productHi) ? 1u : 0u;

                uint hiSum2 = hiSum1 + c1;
                uint c4 = (hiSum2 < hiSum1) ? 1u : 0u;

                uint hiSum3 = hiSum2 + c2;
                uint c5 = (hiSum3 < hiSum2) ? 1u : 0u;

                carryLo = hiSum3;
                carryHi = c3 + c4 + c5;
            }

            int k = i + bDigs.Length;
            while ((carryLo > 0 || carryHi > 0) && k < result.Length)
            {
                uint sum1 = result[k] + carryLo;
                uint c1 = (sum1 < result[k]) ? 1u : 0u;

                uint sum2 = sum1 + carryHi;
                uint c2 = (sum2 < sum1) ? 1u : 0u;

                result[k] = sum2;

                carryLo = c1 + c2;
                carryHi = 0;
                k++;
            }
        }

        return new BetterBigInteger(MagnitudeArithmetic.TrimMagnitude(result), false);
    }

    private static void Multiply16x16To32x2(uint a, uint b, out uint lo, out uint hi)
    {
        uint aLo = a & 0xFFFFu;
        uint aHi = a >> 16;
        uint bLo = b & 0xFFFFu;
        uint bHi = b >> 16;

        uint p0 = aLo * bLo;
        uint p1 = aHi * bLo;
        uint p2 = aLo * bHi;
        uint p3 = aHi * bHi;

        uint mid = p1 + p2;
        uint carryMid = (mid < p1) ? 1u : 0u;

        uint midLo = mid << 16;
        uint midHi = (mid >> 16) | (carryMid << 16);

        lo = p0 + midLo;
        uint carryLo = (lo < p0) ? 1u : 0u;

        hi = p3 + midHi;
        uint carryHi1 = (hi < p3) ? 1u : 0u;

        hi += carryLo;
        uint carryHi2 = (hi < carryLo) ? 1u : 0u;

        _ = carryHi1;
        _ = carryHi2;
    }

    private static bool IsZero(ReadOnlySpan<uint> digits)
        => digits.Length == 0 || (digits.Length == 1 && digits[0] == 0);
}