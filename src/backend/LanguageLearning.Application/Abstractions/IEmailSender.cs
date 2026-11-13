using LanguageLearning.Application.Common.Email;

namespace LanguageLearning.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
