#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Newsletters.Scripts;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Newsletters.ScheduledTasks
{
    /// <summary>
    /// Class RefreshMediaLibraryTask.
    /// </summary>
    public class EmailNewsletter : IScheduledTask
    {
        // public ILibraryManager _libraryManager;
        // public ILocalizationManager _localization;

        // /// <summary>
        // /// Initializes a new instance of the <see cref="RefreshMediaLibraryTask" /> class.
        // /// </summary>
        // /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        // /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        // public void RefreshMediaLibraryTask(ILibraryManager libraryManager, ILocalizationManager localization)
        // {
        //     _libraryManager = libraryManager;
        //     _localization = localization;
        // }

        /// <inheritdoc />
        public string Name => "Newsletter Scheduler";

        /// <inheritdoc />
        public string Description => "Email Newsletters";

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
                IntervalTicks = TimeSpan.FromHours(12).Ticks
            };
        }

        /// <inheritdoc />
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(0);
            Console.WriteLine("I made it!!!");
            // return ((LibraryManager)_libraryManager).ValidateMediaLibraryInternal(progress, cancellationToken);
            // string to = "christopher.hebert94@gmail.com";
            // string from = "donotreply";
            // Smtp.SendSmtp(to, from);
            Smtp.SendEmail();
            return Task.CompletedTask;
        }
    }
}