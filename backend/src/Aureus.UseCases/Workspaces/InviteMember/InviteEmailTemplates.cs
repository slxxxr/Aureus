using System.Globalization;
using System.Net;

namespace Aureus.UseCases.Workspaces.InviteMember;

internal static class InviteEmailTemplates
{
    internal static (string Subject, string HtmlBody) Build(
        string language,
        string workspaceName,
        string inviterName,
        bool isRegisteredUser,
        string baseUrl,
        DateTimeOffset expiresAt)
    {
        var trimmedBaseUrl = baseUrl.TrimEnd('/');
        var actionUrl = isRegisteredUser ? $"{trimmedBaseUrl}/login" : $"{trimmedBaseUrl}/register";

        // workspaceName/inviterName are user-controlled — escape only the copy embedded in HTML.
        var safeWorkspaceName = WebUtility.HtmlEncode(workspaceName);
        var safeInviterName = WebUtility.HtmlEncode(inviterName);

        var copy = language == "en"
            ? EnglishCopy(safeInviterName, safeWorkspaceName, isRegisteredUser, expiresAt)
            : RussianCopy(safeInviterName, safeWorkspaceName, isRegisteredUser, expiresAt);

        var html = $"""
                    <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:32px 24px">
                      <h2 style="margin:0 0 8px">{copy.Heading}</h2>
                      <p style="color:#6b7280;margin:0 0 24px">{copy.Description}</p>
                      <a href="{actionUrl}" style="display:inline-block;padding:12px 24px;background:#111827;color:#fff;
                                text-decoration:none;border-radius:8px;font-weight:600">
                        {copy.ActionLabel}
                      </a>
                      <p style="color:#9ca3af;font-size:13px;margin:24px 0 0">
                        {copy.ExpiresText}
                      </p>
                    </div>
                    """;

        return ($"{inviterName} {copy.SubjectSuffix}", html);
    }

    private static Copy EnglishCopy(string safeInviterName, string safeWorkspaceName, bool isRegisteredUser,
        DateTimeOffset expiresAt)
    {
        var expiresLabel = expiresAt.ToString("MMMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));

        return new Copy(
            $"{safeInviterName} invited you to \"{safeWorkspaceName}\"",
            isRegisteredUser
                ? "Log in to Aureus to accept it."
                : "Create an account on Aureus to join this workspace.",
            isRegisteredUser ? "Log in to Aureus" : "Create your account",
            $"This invitation expires on {expiresLabel}.",
            "invited you to a workspace on Aureus");
    }

    private static Copy RussianCopy(string safeInviterName, string safeWorkspaceName, bool isRegisteredUser,
        DateTimeOffset expiresAt)
    {
        var expiresLabel = expiresAt.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));

        return new Copy(
            $"{safeInviterName} пригласил(а) вас в «{safeWorkspaceName}»",
            isRegisteredUser
                ? "Войдите в Aureus, чтобы принять его."
                : "Зарегистрируйтесь в Aureus, чтобы присоединиться к этому пространству.",
            isRegisteredUser ? "Войти в Aureus" : "Создать аккаунт",
            $"Приглашение действует до {expiresLabel}.",
            "пригласил(а) вас в рабочее пространство Aureus");
    }

    private readonly record struct Copy(
        string Heading,
        string Description,
        string ActionLabel,
        string ExpiresText,
        string SubjectSuffix);
}