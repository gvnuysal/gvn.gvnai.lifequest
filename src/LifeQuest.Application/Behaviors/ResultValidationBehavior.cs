using System.Collections.Concurrent;
using System.Reflection;
using FluentValidation;
using Gvn.GvnFramework.Core.Results;
using MediatR;

namespace LifeQuest.Application.Behaviors;

/// <summary>
/// Framework'teki <c>ValidationBehavior</c> hata durumunda <c>(TResponse)Result.Fail(...)</c> cast'i yapar;
/// TResponse <c>Result&lt;T&gt;</c> olduğunda bu <see cref="InvalidCastException"/> üretir. Bu davranış aynı
/// sözleşmeyi doğru tipte başarısız sonuç üreterek uygular ve async kuralları da destekler.
/// </summary>
public sealed class ResultValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validatorList = validators.ToList();
        if (validatorList.Count == 0)
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validatorList.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = results
            .SelectMany(r => r.Errors)
            .Select(e => Error.Validation(e.PropertyName, e.ErrorMessage))
            .ToList();

        return errors.Count == 0 ? await next() : FailureFactory.Create<TResponse>(errors);
    }
}

internal static class FailureFactory
{
    private static readonly ConcurrentDictionary<Type, Func<List<Error>, Result>> Factories = new();

    public static TResponse Create<TResponse>(List<Error> errors) where TResponse : Result
        => (TResponse)Factories.GetOrAdd(typeof(TResponse), Build)(errors);

    private static Func<List<Error>, Result> Build(Type type)
    {
        if (type == typeof(Result))
            return errors => Result.Fail(errors);

        var fail = type.GetMethod(
                       nameof(Result.Fail),
                       BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                       [typeof(IEnumerable<Error>)])
                   ?? throw new InvalidOperationException($"{type} için Fail(IEnumerable<Error>) bulunamadı.");

        return errors => (Result)fail.Invoke(null, [errors])!;
    }
}
