namespace CronTaskRunner.Sample.Email;

/// <param name="To">Получатели письма (минимум один).</param>
public sealed record EmailMessage(IReadOnlyList<string> To, string Subject, string Body, bool IsHtml = false);
