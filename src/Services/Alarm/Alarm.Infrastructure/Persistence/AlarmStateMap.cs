using AccessHub.Alarm.Infrastructure.Sagas;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessHub.Alarm.Infrastructure.Persistence;

internal sealed class AlarmStateMap : SagaClassMap<AlarmState>
{
    protected override void Configure(EntityTypeBuilder<AlarmState> entity, ModelBuilder model)
    {
        entity.ToTable("alarms");
        entity.Property(x => x.CurrentState).HasMaxLength(32);
        entity.Property(x => x.Rule).HasMaxLength(32);
        entity.Property(x => x.Priority).HasMaxLength(16);
        entity.Property(x => x.SubjectKey).HasMaxLength(128);
        entity.Property(x => x.CardNumber).HasMaxLength(32);
        entity.Property(x => x.AcknowledgedBy).HasMaxLength(200);
        entity.Property(x => x.ClosedBy).HasMaxLength(200);
        entity.Property(x => x.CloseNote).HasMaxLength(2000);

        // At most one alarm that is not closed per subject.
        entity.HasIndex(x => x.SubjectKey).IsUnique().HasFilter("current_state <> 'Closed'").HasDatabaseName("ux_alarms_open_subject");
        entity.HasIndex(x => new { x.CurrentState, x.RaisedAt });
    }
}
