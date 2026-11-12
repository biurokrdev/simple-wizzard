using MediatR;

namespace D2ApiCreator.Application.Features.Examples.Queries;

/// <summary>
/// Handler for GetExampleQuery
/// </summary>
public class GetExampleQueryHandler : IRequestHandler<GetExampleQuery, ExampleResponse>
{
    public Task<ExampleResponse> Handle(GetExampleQuery request, CancellationToken cancellationToken)
    {
        var response = new ExampleResponse
        {
            Message = $"Hello {request.Name}! Welcome to D2ApiCreator built with DDD and Clean Architecture.",
            Timestamp = DateTime.UtcNow
        };

        return Task.FromResult(response);
    }
}

