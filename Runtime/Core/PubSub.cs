using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Rosi.Core
{
	public class PubSub
	{
		public static readonly PubSub Current = new PubSub();

		abstract class CallerInfo
		{
			readonly WeakReference<object> _target;

			public object Target 
			{
				get
				{
					_target.TryGetTarget(out var target);
					return target;
				}
			}
			
			protected CallerInfo(object target)
			{
				_target = new WeakReference<object>(target);
			}

			public abstract Task CallAsync (object data);
		}

		class CallerInfo<T> : CallerInfo
		{
			readonly Func<T, Task> _task;

			public CallerInfo(Func<T, Task> task, object target) : base(target) 
			{
				_task = task;
			}

			public override Task CallAsync (object data)
			{
                return _task.Invoke((T)data);
			}
		}

		readonly Dictionary<Type, List<CallerInfo>> _lookUp = new Dictionary<Type, List<CallerInfo>>();
		readonly object _lock = new object();

		public void Subscribe<T>(object target, Func<T, Task> function)
		{
			lock (_lock)
			{
				var type = typeof(T);
				if (!_lookUp.ContainsKey(type))
					_lookUp[type] = new List<CallerInfo>();

                _lookUp[type].Add(new CallerInfo<T>(function, target));
			}
		}

        public void Unsubscribe(object target, Type type)
        {
            lock (_lock)
            {
                if (_lookUp.TryGetValue(type, out var list))
                {
                    list.RemoveAll((a) =>
                    {
                        var t = a.Target;
                        if (t == null)
                            return true;

                        return (t == target);
                    });
                }
            }
        }

        public void Unsubscribe<T>(object target)
		{
            Unsubscribe(target, typeof(T));
		}

        void CleanList(Type type)
        {
			lock (_lock)
			{
				if (_lookUp.TryGetValue(type, out var list))
					list.RemoveAll((a) => a.Target == null);
			}
        }

        public async Task PublishAsync<T>(T data)
		{
            var cleanList = false;
			var type = typeof(T);
			List<CallerInfo> list;

			lock (_lock)
				_lookUp.TryGetValue(type, out list);
					
			if(list != null)
			{
				CallerInfo[] items;
				lock (_lock)
					items = list.ToArray(); // meh

                for (int i = 0; i < items.Length; i++)
                {
                    try
                    {
                        var item = items[i];
                        if (item.Target != null)
                        {
                            await item.CallAsync(data);
                        }
                        else
                        {
                            cleanList = true;
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.HandleException(ex);
                    }
                }
			}

            if (cleanList)
                CleanList(type);
		}

        internal void Subscribe<T>(object progressEvent)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
			lock(_lock)
                _lookUp.Clear();
        }
	}
}

