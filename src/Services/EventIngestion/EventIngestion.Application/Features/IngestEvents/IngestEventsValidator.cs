using AccessHub.EventIngestion.Domain.Events;
using FluentValidation;

namespace AccessHub.EventIngestion.Application.Features.IngestEvents;

/// <summary>Any invalid event rejects the whole batch; nothing is stored.</summary>
public sealed class IngestEventsValidator : AbstractValidator<IngestEventsCommand>
{
    public const int MaxBatchSize = 500;

    public IngestEventsValidator(TimeProvider clock)
    {
        RuleFor(x => x.Events).NotNull().Must(e => e is { Count: > 0 and <= MaxBatchSize }).WithMessage($"A batch holds 1 to {MaxBatchSize} events.");
        RuleForEach(x => x.Events).ChildRules(e =>
        {
            e.RuleFor(x => x.EventId).NotEmpty();
            e.RuleFor(x => x.DoorId).NotEmpty();
            e.RuleFor(x => x.Type).Must(t => Enum.TryParse<DoorEventKind>(t, ignoreCase: false, out var k) && Enum.IsDefined(k)).WithMessage("Unknown event type '{PropertyValue}'.");
            e.RuleFor(x => x.OccurredAt).NotEmpty().LessThanOrEqualTo(_ => clock.GetUtcNow() + DoorEvent.MaxClockSkew).WithMessage("OccurredAt is in the future.");
            e.RuleFor(x => x.CardNumber).NotEmpty().MaximumLength(32).When(x => x.Type is nameof(DoorEventKind.AccessGranted) or nameof(DoorEventKind.AccessDenied));
            e.RuleFor(x => x.DenyReason).Must(r => r is not null && DoorEventErrors.DenyReasons.Contains(r)).When(x => x.Type == nameof(DoorEventKind.AccessDenied)).WithMessage("A valid deny reason is required.");
            e.RuleFor(x => x.AccessListSequence).GreaterThanOrEqualTo(0).When(x => x.AccessListSequence is not null);
        });
    }
}
