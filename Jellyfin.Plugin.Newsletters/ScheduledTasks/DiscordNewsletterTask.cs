#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Discord.CLIENT;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Newsletters.ScheduledTasks
{
    /// <summary>
    /// Class RefreshMediaLibraryTask.
    /// </summary>
    public class DiscordNewsletterTask : IScheduledTask
    {
        /// <inheritdoc />
        public string Name => "Discord Newsletter";

        /// <inheritdoc />
        public string Description => "Discord Newsletters";

        /// <inheritdoc />
        public string Category => "Newsletters";

        /// <inheritdoc />
        public string Key => "DiscordNewsletters";

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
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(0);

            DiscordClient client = new DiscordClient();
            await client.SendDiscordMessage().ConfigureAwait(false);
            progress.Report(100);
            return;
        }
    }
}