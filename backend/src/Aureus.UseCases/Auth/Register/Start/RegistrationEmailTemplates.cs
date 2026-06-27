namespace Aureus.UseCases.Auth.Register.Start;

internal static class RegistrationEmailTemplates
{
    internal static (string Subject, string HtmlBody) Build(string language, string code)
    {
        var copy = language == "en" ? EnglishCopy() : RussianCopy();

        var html = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:32px 24px">
              <h2 style="margin:0 0 8px">{copy.Heading}</h2>
              <p style="color:#6b7280;margin:0 0 24px">{copy.Description}</p>
              <div style="font-size:36px;font-weight:700;text-align:center;
                          padding:20px;background:#f9fafb;border-radius:8px;margin-bottom:24px">
                {code}
              </div>
              <p style="color:#9ca3af;font-size:13px;margin:0">{copy.Footer}</p>
            </div>
            """;

        return (copy.Subject, html);
    }

    private readonly record struct Copy(string Subject, string Heading, string Description, string Footer);

    private static Copy EnglishCopy() => new(
        Subject: "Your Aureus verification code",
        Heading: "Confirm your email",
        Description: "Enter this code in Aureus to complete registration.",
        Footer: "Code expires in 1 hour. If you did not request this, you can ignore this email.");

    private static Copy RussianCopy() => new(
        Subject: "Ваш код подтверждения Aureus",
        Heading: "Подтвердите email",
        Description: "Введите этот код в Aureus для завершения регистрации.",
        Footer: "Код действует 1 час. Если вы не запрашивали его — просто проигнорируйте это письмо.");
}
