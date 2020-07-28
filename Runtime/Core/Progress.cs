using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rosi.Core
{
    public sealed class Progress : IAsyncDisposable
    {
        public readonly ProgressLevels ProgessLevel;
        public readonly string Id;
        public readonly DateTime StartTime;
        public readonly long Counter;

        public ProgressStatus Status { get; private set; } = ProgressStatus.Pending;

        public readonly object Tag;
        public object DoneTag { get; internal set; }

        public Progress Parent { get; private set; }
        public IReadOnlyList<Progress> Children => _children;
        readonly List<Progress> _children = new List<Progress>();

        static readonly object _lock = new object();
        static long _counter;

        Progress(ProgressLevels progressLevel, object tag, string id)
        {
            ProgessLevel = progressLevel;
            Id = id;
            StartTime = DateTime.UtcNow;
            Tag = tag;

            lock(_lock)
            {
                _counter++;
                Counter = _counter;
            }
        }

        public static async Task<Progress> New(ProgressLevels progressLevel, string id, object tag = null)
        {
            var progress = new Progress(progressLevel, tag, id);
            await PubSub.Current.PublishAsync(progress);
            return progress;
        }

        public async Task<Progress> NewChild(ProgressLevels progressLevel, string id, object tag = null)
        {
            var progress = new Progress(progressLevel, tag, id)
            {
                Parent = this
            };

            lock (this)
                _children.Add(progress);

            await PubSub.Current.PublishAsync(progress);
            return progress;
        }

        public Task Success(object doneTag = null)
        {
            return Done(true, doneTag);
        }

        public Task Failure(object doneTag = null)
        {
            return Done(false, doneTag);
        }

        public async Task Done(bool success, object doneTag = null)
        {
            if (Status != ProgressStatus.Pending)
                return;

            Status = success ? ProgressStatus.Success : ProgressStatus.Failure;
            DoneTag = doneTag;
            await PubSub.Current.PublishAsync(this);
        }

        public async ValueTask DisposeAsync()
        {
            await Done(false);
        }
    }
}
