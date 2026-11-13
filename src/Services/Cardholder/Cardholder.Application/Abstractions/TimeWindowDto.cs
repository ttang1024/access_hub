namespace AccessHub.Cardholder.Application.Abstractions;

public sealed record TimeWindowDto(DayOfWeek Day, int StartMinute, int EndMinute);
