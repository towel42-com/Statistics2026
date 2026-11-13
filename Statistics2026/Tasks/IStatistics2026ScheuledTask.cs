using MediaBrowser.Model.Tasks;
using System;

namespace Statistics2026.ScheduledTasks
{
    public interface IStatistics2026ScheduledTask : IScheduledTask
    {
        public List<string> ReasonsForExecute { get; set; }
    }
}