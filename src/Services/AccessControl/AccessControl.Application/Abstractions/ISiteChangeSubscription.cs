using System.Threading.Channels;

namespace AccessHub.AccessControl.Application.Abstractions;

public interface ISiteChangeSubscription : IDisposable
{
    ChannelReader<SiteChangeRecord> Changes { get; }
}
