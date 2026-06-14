using FluentValidation;

namespace Aureus.UseCases.Analytics.GetInsights;

internal sealed class GetInsightsQueryValidator : AbstractValidator<GetInsightsQuery>
{
    private const int QuestionMaxLength = 500;

    public GetInsightsQueryValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty()
            .MaximumLength(QuestionMaxLength);
    }
}
