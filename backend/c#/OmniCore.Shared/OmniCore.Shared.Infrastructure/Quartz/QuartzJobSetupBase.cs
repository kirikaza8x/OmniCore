// namespace OmniCore.Shared.Infrastructure.Quartz;

// using Microsoft.Extensions.Options;
// using Quartz;

// public abstract class QuartzJobSetupBase<TJob>(int intervalInSeconds) : IConfigureOptions<QuartzOptions>
//     where TJob : IJob
// {
//     protected abstract string JobName { get; }

//     public virtual void Configure(QuartzOptions options)
//     {
//         var jobKey = new JobKey(JobName);

//         options
//             .AddJob<TJob>(configure => configure.WithIdentity(jobKey))
//             .AddTrigger(configure =>
//                 configure
//                     .ForJob(jobKey)
//                     .WithSimpleSchedule(schedule =>
//                         schedule
//                             .WithIntervalInSeconds(intervalInSeconds)
//                             .RepeatForever()));
//     }
// }