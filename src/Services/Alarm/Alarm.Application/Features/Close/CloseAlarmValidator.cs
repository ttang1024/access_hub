using AccessHub.Alarm.Domain;
using FluentValidation;

namespace AccessHub.Alarm.Application.Features.Close;

public sealed class CloseAlarmValidator : AbstractValidator<CloseAlarmCommand>
{
    public CloseAlarmValidator() => RuleFor(x => x.Note).NotEmpty().MaximumLength(AlarmErrors.MaxNoteLength);
}
