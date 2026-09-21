namespace UrlShortener.Domain.Common;

public static class Base62Converter
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly char[] AlphabetChars = Alphabet.ToCharArray();

    // Public methods
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

        return new string(buffer[position..]);
    }
    public static long Decode(ReadOnlySpan<char> code)
    {
        // Input Validation
        if (code.IsEmpty || code.IsWhiteSpace())
        {
            throw new ArgumentException("Code cannot be empty or whitespace.", nameof(code));
        }

        long result = 0;

        foreach (char c in code)
        {
            int digitValue = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'z' => c - 'a' + 10,
                >= 'A' and <= 'Z' => c - 'A' + 36,
                _ => throw new FormatException($"Invalid character '{c}' in short code.")
            };

            result = checked((result * 62) + digitValue);
        }

        return result;
    }
}