using MediatR;

namespace D2ApiCreator.Application.Features.Examples.Queries;

/// <summary>
/// Query to get example message
/// </summary>
public record GetExampleQuery : IRequest<ExampleResponse>
{
    public string Name { get; init; } = string.Empty;
}

public record ExampleResponse
{
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

