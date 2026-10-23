namespace OopsGrocery.Services;

public sealed record ValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static ValidationResult Success() => new(true);
    public static ValidationResult Failure(string message) => new(false, message);
}

public static class LoginValidator
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 10;
    public static readonly char[] AllowedSpecialCharacters = ['.', '_', '-'];

    public static ValidationResult ValidateUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return ValidationResult.Failure("Please enter your username.");
        }

        return ValidationResult.Success();
    }

    public static ValidationResult ValidatePassword(string? password, string? username = null)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return ValidationResult.Failure("Please enter your password.");
        }

        // Rule 1: Password length strictly between 8 and 10 characters (inclusive)
        if (password.Length < MinPasswordLength || password.Length > MaxPasswordLength)
        {
            return ValidationResult.Failure(
                $"Password length must be strictly between {MinPasswordLength} and {MaxPasswordLength} characters long.");
        }

        // Rule 2: Password must not be identical to the username
        if (!string.IsNullOrWhiteSpace(username) &&
            string.Equals(password, username.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Failure("Password must not be identical to the username.");
        }

        // Rule 3: Only allowed special characters (., _, or -)
        if (password.Any(c => !char.IsLetterOrDigit(c) && !AllowedSpecialCharacters.Contains(c)))
        {
            return ValidationResult.Failure(
                "Password can only contain letters, numbers, and allowed special characters (., _, or -).");
        }

        // Rule 4: Complexity - at least one uppercase letter
        if (!password.Any(char.IsUpper))
        {
            return ValidationResult.Failure("Password must contain at least one uppercase letter.");
        }

        // Rule 5: Complexity - at least one lowercase letter
        if (!password.Any(char.IsLower))
        {
            return ValidationResult.Failure("Password must contain at least one lowercase letter.");
        }

        // Rule 6: Complexity - at least one number
        if (!password.Any(char.IsDigit))
        {
            return ValidationResult.Failure("Password must contain at least one number.");
        }

        // Rule 7: Complexity - at least one allowed special character (., _, or -)
        if (!password.Any(c => AllowedSpecialCharacters.Contains(c)))
        {
            return ValidationResult.Failure("Password must contain at least one allowed special character (., _, or -).");
        }

        // Rule 8: Sequential or repeating patterns are strictly prohibited
        if (HasSequentialOrRepeatingPattern(password))
        {
            return ValidationResult.Failure(
                "Password must not contain sequential or repeating patterns (such as 123, abc, or 111).");
        }

        return ValidationResult.Success();
    }

    public static bool HasSequentialOrRepeatingPattern(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 3)
        {
            return false;
        }

        // 1. Repeating identical characters (case-insensitive, e.g. "111111", "aaa", "Aaa", "...")
        for (int i = 0; i <= password.Length - 3; i++)
        {
            char c1 = char.ToLowerInvariant(password[i]);
            char c2 = char.ToLowerInvariant(password[i + 1]);
            char c3 = char.ToLowerInvariant(password[i + 2]);

            if (c1 == c2 && c2 == c3)
            {
                return true;
            }
        }

        // 2. Sequential numbers or letters (ascending or descending, e.g. "12345678910", "abcdefgh", "cba", "321")
        for (int i = 0; i <= password.Length - 3; i++)
        {
            char c1 = password[i];
            char c2 = password[i + 1];
            char c3 = password[i + 2];

            // Numerical sequences
            if (char.IsDigit(c1) && char.IsDigit(c2) && char.IsDigit(c3))
            {
                if ((c2 - c1 == 1 && c3 - c2 == 1) || (c1 - c2 == 1 && c2 - c3 == 1))
                {
                    return true;
                }
            }

            // Alphabetical sequences (case-insensitive)
            if (char.IsLetter(c1) && char.IsLetter(c2) && char.IsLetter(c3))
            {
                char l1 = char.ToLowerInvariant(c1);
                char l2 = char.ToLowerInvariant(c2);
                char l3 = char.ToLowerInvariant(c3);

                if ((l2 - l1 == 1 && l3 - l2 == 1) || (l1 - l2 == 1 && l2 - l3 == 1))
                {
                    return true;
                }
            }
        }

        // 3. Repeating patterns/cycles (e.g. "abab", "1212", "abcabc")
        for (int patternLen = 2; patternLen <= 3; patternLen++)
        {
            for (int i = 0; i <= password.Length - (patternLen * 2); i++)
            {
                string chunk1 = password.Substring(i, patternLen);
                string chunk2 = password.Substring(i + patternLen, patternLen);
                if (string.Equals(chunk1, chunk2, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
