using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IoTControlKit.Services.Schedulers
{
    public class SchedulerService : BaseService, IDisposable
    {
        private AutoResetEvent _newActionEvent = null;
        private volatile bool _actionInProgress = false;
        private volatile bool _disposing = false;

        private HashSet<long> _changedPropertyValues = new HashSet<long>();
        private List<BaseScheduler> _allSchedulers;

        public SchedulerService()
        {
        }

        public void Dispose()
        {
            if (!_disposing)
            {
                ApplicationService.Instance.Database.DatabaseChanged -= Database_DatabaseChanged;

                _disposing = true;
                DateTime maxWaitTill = DateTime.Now.AddSeconds(5);
                while (_actionInProgress && DateTime.Now < maxWaitTill)
                {
                    Thread.Sleep(100);
                }
                try
                {
                    _newActionEvent?.Dispose();
                    _newActionEvent = null;
                }
                catch
                {
                }
            }
        }

        public void Start()
        {
            _allSchedulers = new List<BaseScheduler>()
            {
                new FlowScheduler()
            };        

            _newActionEvent = new AutoResetEvent(false);
            Thread thread = new Thread(() => { HandleActionQueueMethod(); });
            thread.IsBackground = true;
            thread.Start();

            ApplicationService.Instance.Database.DatabaseChanged += Database_DatabaseChanged;
        }

        private void Database_DatabaseChanged(DatabaseService.DatabaseChanges changes)
        {
            if (_disposing) return;

            var cf = new ChangesFilter<Framework.Models.DevicePropertyValue>(changes);
            if (cf.Updated.Any() || cf.Added.Any())
            {
                foreach (var p in cf.Added)
                {
                    if (!_changedPropertyValues.Contains(p.Id))
                    {
                        _changedPropertyValues.Add(p.Id);
                    }
                }
                foreach (var p in cf.Updated)
                {
                    if (!_changedPropertyValues.Contains(p.Id))
                    {
                        _changedPropertyValues.Add(p.Id);
                    }
                }
                _newActionEvent?.Set();
            }
        }

        private void HandleActionQueueMethod()
        {
            try
            {
                while (!_disposing)
                {
                    _actionInProgress = true;
                    try
                    {
                        if (!_disposing)
                        {
                            Dictionary<long, Framework.Models.DevicePropertyValue> propertyValues = null;
                            HashSet<long> changedProperties = null;
                            Dictionary<long, string> outputPropertyValues = new Dictionary<long, string>();
                            ApplicationService.Instance.Database.Execute((db) =>
                            {
                                propertyValues = db.Fetch<Framework.Models.DevicePropertyValue>().ToDictionary(x => x.DevicePropertyId, x => x);
                                changedProperties = _changedPropertyValues.ToHashSet();
                                _changedPropertyValues.Clear();
                            });
                            foreach (var scheduler in _allSchedulers)
                            {
                                scheduler.Schedule(propertyValues, changedProperties, outputPropertyValues);
                            }
                            if (outputPropertyValues.Any())
                            {
                                ApplicationService.Instance.OnSetDevicePropertyValue((from a in outputPropertyValues select new Framework.SetDeviceProperties() { DevicePropertyId = a.Key, Value = a.Value }).ToList());
                            }
                        }
                    }
                    catch
                    {
                    }
                    _actionInProgress = false;
                    if (!_disposing)
                    {
                        _newActionEvent?.WaitOne(1000);
                    }
                }
            }
            catch
            {
                //handler disposed
            }
        }
    }
}
