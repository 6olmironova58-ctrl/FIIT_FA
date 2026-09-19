using System.Numerics;
using System.Text;
using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit;
    
    private uint _smallValue;
    private uint[]? _data;
    
    public bool IsNegative => _signBit == 1;
    
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        ArgumentNullException.ThrowIfNull(digits);

        _signBit = isNegative ? 1 : 0;
        _data = digits.Length == 0 ? null : (uint[])digits.Clone();
        Normalize();
    }
    
    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false) 
    : this(digits.ToArray(), isNegative) {}
    
    public BetterBigInteger(string value, int radix)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix));
        if (value.Length == 0)
            throw new ArgumentException("Value cannot be empty.", nameof(value));

        int startIndex = 0;
        bool isNegative = false;

        if (value[0] == '-') { isNegative = true; startIndex = 1; }
        else if (value[0] == '+') { startIndex = 1; }

        if (startIndex >= value.Length)
            throw new ArgumentException("Value must contain at least one digit.", nameof(value));

        var result = new List<uint> { 0 };
        for (int i = startIndex; i < value.Length; i++)
        {
            MultiplyListByScalar(result, (uint)radix);
            AddScalarToList(result, CharToDigit(value[i], radix));
        }

        _signBit = isNegative ? 1 : 0;
        _data = result.ToArray();
        Normalize();
    }
    
    private void Normalize()
    {
        if (_data is null)
        {
            if (_smallValue == 0) _signBit = 0;
            return;
        }

        int len = _data.Length;
        while (len > 0 && _data[len - 1] == 0) len--;

        if (len == 0)
        {
            _data = null;
            _smallValue = 0;
            _signBit = 0;
        }
        else if (len == 1)
        {
            _smallValue = _data[0];
            _data = null;
            if (_smallValue == 0) _signBit = 0;
        }
        else if (len < _data.Length)
        {
            Array.Resize(ref _data, len);
        }
    }

    private static uint CharToDigit(char c, int radix)
    {
        uint digit = c switch
        {
            >= '0' and <= '9' => (uint)(c - '0'),
            >= 'a' and <= 'z' => (uint)(c - 'a' + 10),
            >= 'A' and <= 'Z' => (uint)(c - 'A' + 10),
            _ => throw new ArgumentException(
                $"Invalid character '{c}' for radix {radix}.")
        };

        if (digit >= (uint)radix)
            throw new ArgumentException(
                $"Digit '{c}' (value {digit}) is not valid for radix {radix}.");

        return digit;
    }

    private static void MultiplyListByScalar(List<uint> data, uint scalar)
    {
        if (scalar == 0)
        {
            data.Clear();
            data.Add(0);
            return;
        }
        if (scalar == 1)
            return;

        uint carry = 0;
        for (int i = 0; i < data.Count; i++)
        {
            uint val = data[i];
            uint lo = val & 0xFFFF;
            uint hi = val >> 16;

            uint prodLo = lo * scalar;
            uint prodHi = hi * scalar;

            uint sumLo = prodLo + carry;
            uint carryLo = sumLo < prodLo ? 1u : 0u;

            uint resLo = sumLo & 0xFFFF;
            uint resHi = (prodLo >> 16) + prodHi + carryLo;

            data[i] = (resHi << 16) | resLo;
            carry = resHi >> 16;
        }
        
        while (carry > 0)
        {
            data.Add(carry);
            carry = 0; 
        }
    }

    private static void AddScalarToList(List<uint> data, uint scalar)
    {
        if (scalar == 0)
            return;
            
        uint carry = scalar;
        for (int i = 0; i < data.Count && carry > 0; i++)
        {
            uint sum = data[i] + carry;
            carry = sum < data[i] ? 1u : 0u;
            data[i] = sum;
        }
        
        while (carry > 0)
        {
            data.Add(carry);
            carry = 0;
        }
    }
    
    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }
    
    public int CompareTo(IBigInteger? other) {
        if (other is null) return 1;

        if (IsNegative != other.IsNegative)
            return IsNegative ? -1 : 1;

        int cmp = MagnitudeArithmetic.CompareMagnitudes(GetDigits(), other.GetDigits());
        return IsNegative ? -cmp : cmp;
    }

    public bool Equals(IBigInteger? other) => CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode() {
        HashCode hash = new();
        hash.Add(_signBit);
        foreach (uint d in GetDigits()) hash.Add(d);
        return hash.ToHashCode();
    }
    
    
    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b) {
        if (a._signBit == b._signBit)
        {
            var result = MagnitudeArithmetic.AddMagnitudes(a.GetDigits(), b.GetDigits());
            return new BetterBigInteger(result, a.IsNegative);
        }
        
        int cmp = MagnitudeArithmetic.CompareMagnitudes(a.GetDigits(), b.GetDigits());
        if (cmp == 0) return Zero;
        
        if (cmp > 0)
        {
            var result = MagnitudeArithmetic.SubtractMagnitudes(a.GetDigits(), b.GetDigits());
            return new BetterBigInteger(result, a.IsNegative);
        }
        else
        {
            var result = MagnitudeArithmetic.SubtractMagnitudes(b.GetDigits(), a.GetDigits());
            return new BetterBigInteger(result, b.IsNegative);
        }
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b) => a + (-b);

    public static BetterBigInteger operator -(BetterBigInteger a) {
        if (a._smallValue == 0 && a._data is null) return a;
            return new BetterBigInteger(a.GetDigits().ToArray(), !a.IsNegative);
    }

    private int BitLength()
    {
        var digits = GetDigits();
        if (digits is [0]) return 0;

        return 32 * digits.Length - BitOperations.LeadingZeroCount(digits[^1]);
    }

    private static (BetterBigInteger quotient, BetterBigInteger remainder) DivRem(BetterBigInteger a, BetterBigInteger b) {
        if (b.GetDigits() is [0])
            throw new DivideByZeroException("Division by zero.");

        int cmp = MagnitudeArithmetic.CompareMagnitudes(a.GetDigits(), b.GetDigits());
        if (cmp < 0)
            return (new BetterBigInteger([0]), a);
        
        if (cmp == 0)
            return (new BetterBigInteger([1], a.IsNegative ^ b.IsNegative), new BetterBigInteger([0]));

        uint[] remDigits = a.GetDigits().ToArray();
        uint[] quoDigits = new uint[a.GetDigits().Length];
        uint[] bDigits = b.GetDigits().ToArray();

        int shift = a.BitLength() - b.BitLength();

        for (int i = shift; i >= 0; i--)
        {
            var shifted = new BetterBigInteger(bDigits, false) << i;
            
            if (MagnitudeArithmetic.CompareMagnitudes(remDigits, shifted.GetDigits()) < 0)
                continue;

            remDigits = MagnitudeArithmetic.SubtractMagnitudes(remDigits, shifted.GetDigits());
            
            quoDigits[i / 32] |= (1u << (i % 32));
        }

        bool remIsNeg = a.IsNegative && !IsZero(remDigits);
        bool quoIsNeg = (a.IsNegative ^ b.IsNegative) && !IsZero(quoDigits);

        return (
            new BetterBigInteger(MagnitudeArithmetic.TrimMagnitude(quoDigits), quoIsNeg),
            new BetterBigInteger(MagnitudeArithmetic.TrimMagnitude(remDigits), remIsNeg)
        );
    }

    private static bool IsZero(uint[] digits)
    {
        foreach (var d in digits)
            if (d != 0) return false;
        return true;
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b) 
        => DivRem(a, b).quotient;

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b) 
        => DivRem(a, b).remainder;

    private static readonly BetterBigInteger Zero = new([0u]);
    
    private static readonly IMultiplier _simple = new SimpleMultiplier();
    private static readonly IMultiplier _karatsuba = new KaratsubaMultiplier();
    private static readonly IMultiplier _fft = new FftMultiplier();

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        if (a._smallValue == 0 && a._data is null || b._smallValue == 0 && b._data is null)
            return new BetterBigInteger([0], false);

        ReadOnlySpan<uint> aDigits = a.GetDigits();
        ReadOnlySpan<uint> bDigits = b.GetDigits();
        
        int maxLen = Math.Max(aDigits.Length, bDigits.Length);

        IMultiplier strategy = maxLen switch
        {
            <= 64 => _simple,
            <= 10000 => _karatsuba,
            _ => _fft
        };

        BetterBigInteger aAbs = a.IsNegative ? -a : a;
        BetterBigInteger bAbs = b.IsNegative ? -b : b;

        BetterBigInteger product = strategy.Multiply(aAbs, bAbs);
        bool isNegative = a.IsNegative ^ b.IsNegative;

        return new BetterBigInteger(product.GetDigits().ToArray(), isNegative);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a) 
        => -a - new BetterBigInteger([1]);

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) 
        => BitwiseOp(a, b, (x, y) => x & y);

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) 
        => BitwiseOp(a, b, (x, y) => x | y);

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) 
        => BitwiseOp(a, b, (x, y) => x ^ y);

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (shift == 0) return a;
        if (shift < 0) return a >> (-shift);

        uint[] newMag = ShiftLeftMagnitude(a.GetDigits(), shift);
        return new BetterBigInteger(newMag, a.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift)
    {
        if (shift == 0) return a;
        if (shift < 0) return a << (-shift);

        int len = a.GetDigits().Length + 1;
        uint[] twos = ToTwosComplement(a, len);
        uint[] shifted = ShiftRightTwosComplement(twos, shift);
        return FromTwosComplement(shifted);
    }

    private static BetterBigInteger BitwiseOp(BetterBigInteger a, BetterBigInteger b, Func<uint, uint, uint> op)
    {
        int len = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        uint[] twosA = ToTwosComplement(a, len);
        uint[] twosB = ToTwosComplement(b, len);

        for (int i = 0; i < len; i++)
            twosA[i] = op(twosA[i], twosB[i]);

        return FromTwosComplement(twosA);
    }

    private static uint[] ToTwosComplement(BetterBigInteger value, int length)
    {
        uint[] res = new uint[length];
        ReadOnlySpan<uint> digits = value.GetDigits();
        int copyLen = Math.Min(digits.Length, length);
        digits[..copyLen].CopyTo(res);
        
        if (value.IsNegative)
        {
            for (int i = 0; i < length; i++)
                res[i] = ~res[i];
                
            uint carry = 1;
            for (int i = 0; i < length && carry > 0; i++)
            {
                uint sum = res[i] + carry;
                carry = sum < res[i] ? 1u : 0u;
                res[i] = sum;
            }
        }
        return res;
    }

    private static BetterBigInteger FromTwosComplement(uint[] twos)
    {
        bool isNeg = (twos[^1] & 0x80000000) != 0;
        uint[] mag = new uint[twos.Length];
        Array.Copy(twos, mag, twos.Length);
        
        if (isNeg)
        {
            for (int i = 0; i < mag.Length; i++)
                mag[i] = ~mag[i];
                
            uint carry = 1;
            for (int i = 0; i < mag.Length && carry > 0; i++)
            {
                uint sum = mag[i] + carry;
                carry = sum < mag[i] ? 1u : 0u;
                mag[i] = sum;
            }
        }
        return new BetterBigInteger(MagnitudeArithmetic.TrimMagnitude(mag), isNeg);
    }

    private static uint[] ShiftLeftMagnitude(ReadOnlySpan<uint> digits, int shift)
    {
        int wordShift = shift / 32;
        int bitShift = shift % 32;
        int newLen = digits.Length + wordShift + (bitShift > 0 ? 1 : 0);
        uint[] res = new uint[newLen];
        
        for (int i = 0; i < digits.Length; i++)
        {
            res[i + wordShift] |= digits[i] << bitShift;
            if (bitShift > 0 && i + wordShift + 1 < newLen)
            {
                res[i + wordShift + 1] |= digits[i] >> (32 - bitShift);
            }
        }
        return MagnitudeArithmetic.TrimMagnitude(res);
    }

    private static uint[] ShiftRightTwosComplement(uint[] twos, int shift) 
    {
        int wordShift = shift / 32;
        int bitShift = shift % 32;
        int len = twos.Length;
        uint[] res = new uint[len];
        
        uint signExt = (twos[^1] & 0x80000000) != 0 ? uint.MaxValue : 0;
        
        for (int i = 0; i < len; i++)
        {
            int src = i + wordShift;
            uint low = src < len ? twos[src] : signExt;
            uint high = (src + 1) < len ? twos[src + 1] : signExt;
            
            if (bitShift == 0)
            {
                res[i] = low;
            }
            else
            {
                res[i] = (low >> bitShift) | (high << (32 - bitShift));
            }
        }
        return res;
    }
    
    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;
    
    public override string ToString() => ToString(10);
    public string ToString(int radix) {
        if (radix < 2 || radix > 36)
            throw new ArgumentOutOfRangeException(nameof(radix), "Radix must be between 2 and 36.");

        if (_smallValue == 0 && _data is null)
            return "0";

        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        int maxLen = (int)(BitLength() / Math.Log2(radix)) + 2;
        char[] buffer = new char[maxLen];
        int pos = maxLen;

        uint[] currentMag = GetDigits().ToArray();

        while (currentMag.Length > 0 && !(currentMag.Length == 1 && currentMag[0] == 0))
        {
            var (nextMag, rem) = DivideMagnitudeByScalar(currentMag, (uint)radix);
            buffer[--pos] = digits[(int)rem];
            currentMag = nextMag;
        }

        if (IsNegative)
            buffer[--pos] = '-';

        return new string(buffer, pos, maxLen - pos);
    }

    private static (uint[] quotient, uint remainder) DivideMagnitudeByScalar(ReadOnlySpan<uint> dividend, uint divisor) {
        if (divisor == 0) throw new DivideByZeroException();
        int len = dividend.Length;
        while (len > 0 && dividend[len - 1] == 0) len--;
        if (len == 0) return ([0], 0);
        
        uint[] quotient = new uint[len];
        uint rem = 0;
        
        for (int i = len - 1; i >= 0; i--)
        {
            uint lo = dividend[i] & 0xFFFF;
            uint hi = dividend[i] >> 16;
            
            // rem < divisor, поэтому (rem << 16) не переполнит uint
            uint hiVal = (rem << 16) | hi;
            uint q1 = hiVal / divisor;
            uint r1 = hiVal % divisor;
            
            uint loVal = (r1 << 16) | lo;
            uint q0 = loVal / divisor;
            rem = loVal % divisor;
            
            quotient[i] = (q1 << 16) | q0;
        }
        return (MagnitudeArithmetic.TrimMagnitude(quotient), rem);
    } 
}

internal static class MagnitudeArithmetic
{
    public static uint[] AddMagnitudes(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        var result = new uint[Math.Max(a.Length, b.Length) + 1];
        uint carry = 0;

        for (var i = 0; i < result.Length - 1; i++)
        {
            var aVal = i < a.Length ? a[i] : 0u;
            var bVal = i < b.Length ? b[i] : 0u;

            var s0 = aVal + bVal;
            var c0 = s0 < aVal ? 1u : 0u;

            var s1 = s0 + carry;
            var c1 = s1 < s0 ? 1u : 0u;

            result[i] = s1;
            carry = c0 + c1;
        }

        result[^1] = carry;
        return TrimMagnitude(result);
    }

    public static uint[] SubtractMagnitudes(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        var result = new uint[a.Length];
        uint borrow = 0;

        for (var i = 0; i < a.Length; i++)
        {
            var aVal = a[i];
            var bVal = i < b.Length ? b[i] : 0u;

            var s0 = aVal - bVal;
            var c0 = s0 > aVal ? 1u : 0u;

            var s1 = s0 - borrow;
            var c1 = s1 > s0 ? 1u : 0u;

            result[i] = s1;
            borrow = c0 + c1;
        }

        return TrimMagnitude(result);
    }

    public static int CompareMagnitudes(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int lenA = a.Length;
        while (lenA > 1 && a[lenA - 1] == 0) lenA--;

        int lenB = b.Length;
        while (lenB > 1 && b[lenB - 1] == 0) lenB--;

        if (lenA != lenB)
            return lenA > lenB ? 1 : -1;

        for (var i = lenA - 1; i >= 0; i--)
        {
            if (a[i] != b[i])
                return a[i] > b[i] ? 1 : -1;
        }

        return 0;
    }

    internal static uint[] TrimMagnitude(uint[] array)
    {
        int length = array.Length;
        while (length > 1 && array[length - 1] == 0) length--;

        if (length == array.Length) return array;

        var result = new uint[length];
        Array.Copy(array, 0, result, 0, length);
        return result;
    }
}