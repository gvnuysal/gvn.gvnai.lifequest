using Gvn.GvnFramework.Domain.Events;

namespace LifeQuest.Domain.Common;

/// <summary>
/// Tüm LifeQuest domain event'lerinin ortak tabanı. Gvn.GvnFramework 1.1.0 ile <c>IDomainEvent</c> doğrudan
/// MediatR <c>INotification</c>'dır; GvnDbContext event'leri commit sonrası <c>IMediator.Publish</c> ile yayınlar.
/// </summary>
public abstract record LifeQuestDomainEvent : DomainEvent;
