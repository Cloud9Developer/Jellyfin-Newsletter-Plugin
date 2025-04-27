#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// using ICU4N.Logging;
using Jellyfin.Plugin.Newsletters.Clients.CLIENT;
using Jellyfin.Plugin.Newsletters.LOGGER;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Newsletters.ScheduledTasks
{
    /// <summary>
    /// Class RefreshMediaLibraryTask.
    /// </summary>
    public class NewsletterTask : IScheduledTask
    {
        private readonly IServerApplicationHost _applicationHost;

        public NewsletterTask(IServerApplicationHost applicationHost)
        {
            _applicationHost = applicationHost;
        }

        /// <inheritdoc />
        public string Name => "Newsletter";

        /// <inheritdoc />
        public string Description => "Send Newsletters to all the specified hooks in plugin";

        /// <inheritdoc />
        public string Category => "Newsletters";

        /// <inheritdoc />
        public string Key => "Newsletters";

        /// <summary>
        /// Creates the triggers that define when the task will run.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(168).Ticks
            };
        }

        /// <inheritdoc />
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(0);
            
            // Call the Notify/Send for each client
            Client client = new Client(_applicationHost);
            client.NotifyAll();

            progress.Report(100);
            return Task.CompletedTask;
        }
    }
}