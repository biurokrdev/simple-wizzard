namespace D2ApiCreator.Domain.Common.Exceptions;

/// <summary>
/// Exception thrown when a domain rule is violated
/// </summary>
public class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string message) : base(message)
    {
    }
}

