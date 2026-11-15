using MediaBrowser.Model.Tasks;
using Statistics2026.Utilities;
using System;

namespace Statistics2026.ScheduledTasks
{
    public interface IStatistics2026ScheduledTask : IScheduledTask, IHasEmbyInterface
    {
        public List<string> ReasonsForExecute { get; set; }
    }
}