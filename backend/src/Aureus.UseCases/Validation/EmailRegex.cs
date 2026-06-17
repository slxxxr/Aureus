using System.Text.RegularExpressions;

namespace Aureus.UseCases.Validation;

internal static class EmailRegex
{
    internal static readonly Regex Pattern = new(
        @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
}
