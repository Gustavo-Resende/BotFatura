using Ardalis.Result;
using FluentValidation;
using MediatR;

namespace BotFatura.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            // Precisamos retornar Ardalis.Result de Erro (Invalid)
            // Assumimos que a resposta pedida no IRequestHandler é construída via genéricos
            var resultType = typeof(TResponse);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var errors = failures.Select(f => new ValidationError 
                { 
                    Identifier = f.PropertyName,
                    ErrorMessage = f.ErrorMessage
                }).ToList();

                var method = typeof(Result<>)
                    .MakeGenericType(resultType.GetGenericArguments()[0])
                    .GetMethod("Invalid", new[] { typeof(IEnumerable<ValidationError>) });

                return (TResponse)method!.Invoke(null, new object[] { errors })!;
            }
            else if (resultType == typeof(Result))
            {
                var errors = failures.Select(f => new ValidationError 
                { 
                    Identifier = f.PropertyName,
                    ErrorMessage = f.ErrorMessage
                }).ToList();

                return (TResponse)(object)Result.Invalid(errors);
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
