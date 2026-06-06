using Aureus.Domain.Exceptions;

namespace Aureus.Domain.Categories;

public sealed class CategoryException(CategoryErrorCode code, string message) : DomainException(message)
{
    public CategoryErrorCode Code { get; } = code;

    public override string ErrorCode => Code.ToString();

    public override ErrorType ErrorType => Code switch
    {
        CategoryErrorCode.NameTaken => ErrorType.Conflict,
        CategoryErrorCode.NotFound => ErrorType.NotFound,
        _ => ErrorType.Validation
    };
}
