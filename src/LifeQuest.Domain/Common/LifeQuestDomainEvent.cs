using Gvn.GvnFramework.Domain.Events;
using MediatR;

namespace LifeQuest.Domain.Common;

/// <summary>
/// GvnDbContext domain event'leri <c>IMediator.Publish(object)</c> ile yayınlar; MediatR bunun için
/// event'in <see cref="INotification"/> olmasını şart koşar. Framework'teki <c>IDomainEvent</c> bunu
/// içermediğinden tüm LifeQuest event'leri bu tabandan türetilir.
/// </summary>
public abstract record LifeQuestDomainEvent : DomainEvent, INotification;
