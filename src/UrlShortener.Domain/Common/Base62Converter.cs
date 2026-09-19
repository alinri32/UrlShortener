namespace UrlShortener.Domain.Common;

public static class Base62Converter
{
    // Base62 Character Set
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private static readonly char[] AlphabetChars = Alphabet.ToCharArray();

    // Encode
    public static string Encode(long value)
    {
        // Value Range Validation
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be a positive non-zero integer.");
        }

        // Stack Allocation for Max 11 Chars
        Span<char> buffer = stackalloc char[11];
        int position = buffer.Length;

        while (value > 0)
        {
            buffer[--position] = AlphabetChars[(int)(value % 62)];
            value /= 62;
        }

        // Memory Slice Copy
        return new string(buffer[position..]);
    }

    // Decode
    public static long Decode(ReadOnlySpan<char> code)
    {
        // Empty Check
        if (code.IsEmpty)
        {
            throw new ArgumentException("Code cannot be empty.", nameof(code));
        }

        long result = 0;

        foreach (char c in code)
        {
            int digitValue = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'A' and <= 'Z' => c - 'A' + 10,
                >= 'a' and <= 'z' => c - 'a' + 36,
                _ => throw new FormatException($"Invalid character '{c}' in short code.")
            };

            result = checked((result * 62) + digitValue);
        }

        return result;
    }
}