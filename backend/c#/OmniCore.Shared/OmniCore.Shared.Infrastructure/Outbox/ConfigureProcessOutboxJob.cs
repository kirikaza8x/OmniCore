// namespace OmniCore.Shared.Infrastructure.Outbox;

// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Options;
// using Quartz;

// public sealed class ConfigureProcessOutboxJob<TDbContext>(IOptions<OutboxOptions> outboxOptions) 
//     : IConfigureOptions<QuartzOptions>
//     where TDbContext : DbContext
// {
//     private readonly OutboxOptions _options = outboxOptions.Value;

//     public void Configure(QuartzOptions options)
//     {
//         string moduleName = typeof(TDbContext).Name.Replace("DbContext", string.Empty);
//         string jobName = $"{moduleName}.ProcessOutbox";

//         options
//             .AddJob<ProcessOutboxJob<TDbContext>>(configure => configure.WithIdentity(jobName))
//             .AddTrigger(configure =>
//                 configure
//                     .ForJob(jobName)
//                     .WithSimpleSchedule(schedule =>
//                         schedule
//                             .WithIntervalInSeconds(_options.IntervalInSeconds)
//                             .RepeatForever()));
//     }
// }