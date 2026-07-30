namespace OmniCore.Shared.Infrastructure.Inbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;

public sealed class ConfigureProcessInboxJob<TDbContext>(IOptions<InboxOptions> inboxOptions) 
    : IConfigureOptions<QuartzOptions>
    where TDbContext : DbContext
{
    private readonly InboxOptions _inboxOptions = inboxOptions.Value;

    public void Configure(QuartzOptions options)
    {
        var moduleName = typeof(TDbContext).Name.Replace("DbContext", "");
        var jobName = $"{moduleName}.ProcessInbox";

        options
            .AddJob<ProcessInboxJob<TDbContext>>(configure =>
                configure.WithIdentity(jobName))
            .AddTrigger(configure =>
                configure
                    .ForJob(jobName)
                    .WithSimpleSchedule(schedule =>
                        schedule
                            .WithIntervalInSeconds(_inboxOptions.IntervalInSeconds)
                            .RepeatForever()));
    }
}