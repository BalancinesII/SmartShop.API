using System.Runtime.CompilerServices;
using FluentValidation;
using MediatR;

namespace SmartShop.Application.Common.Behaviours;

// ValidationBehaviour only runs for IRequest; stream requests (IStreamRequest)
// go through a separate MediatR pipeline, so they need their own behaviour.
// Validation runs when the stream is first read, before the handler starts.
public class StreamValidationBehaviour<TRequest, TResponse>
    : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public StreamValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async IAsyncEnumerable<TResponse> Handle(TRequest request,
                                                    StreamHandlerDelegate<TResponse> next,
                                                    [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        await foreach (var item in next().WithCancellation(cancellationToken))
            yield return item;
    }
}
