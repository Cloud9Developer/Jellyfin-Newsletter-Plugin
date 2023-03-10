#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Scripts.Scraper;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Newsletters.ScheduledTasks
{
    /// <summary>
    /// Class RefreshMediaLibraryTask.
    /// </summary>
    public class ScrapeInfoTask : IScheduledTask
    {
        /// <inheritdoc />
        public string Name => "Filesystem Scraper";

        /// <inheritdoc />
        public string Description => "Gather info on recently added media and store it for Newsletters";

        /// <inheritdoc />
        public string Category => "Newsletters";

        /// <inheritdoc />
        public string Key => "EmailNewsletters";

        /// <summary>
        /// Creates the triggers that define when the task will run.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(4).Ticks
            };
        }

        /// <inheritdoc />
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // progress.Report(0);

            // Smtp mySmtp = new Smtp();
            // mySmtp.SendEmail();
            Scraper myScraper = new Scraper(progress);
            return myScraper.GetSeriesData(); // .ConfigureAwait(false);
            // progress.Report(100);
            // return Task.CompletedTask;
        }
    }
}