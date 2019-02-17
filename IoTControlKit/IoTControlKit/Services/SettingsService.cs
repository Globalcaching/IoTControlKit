using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public class SettingsService: BaseService, INotifyPropertyChanged
    {
        private static SettingsService _uniqueInstance = null;
        private static object _lockObject = new object();

        private Dictionary<string, Models.Settings.ApplicationSetting> _allSettings;
        public event PropertyChangedEventHandler PropertyChanged;


        private SettingsService()
        {
            Services.Database.SettingsDatabaseService.Instance.Execute((db) =>
            {
                _allSettings = db.Fetch<Models.Settings.ApplicationSetting>().ToList().ToDictionary(x => x.Name, x => x);
            });
        }

        public static SettingsService Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new SettingsService();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public LogLevel LogLevel
        {
            get { return (LogLevel)Enum.Parse(typeof(LogLevel), GetProperty(LogLevel.Information.ToString())); }
            set { SetProperty(value.ToString()); }
        }


        private string GetProperty(string defaultValue, [CallerMemberName] string name = "")
        {
            string result;
            lock (_allSettings)
            {
                Models.Settings.ApplicationSetting s;
                if (_allSettings.TryGetValue(name, out s))
                {
                    result = _allSettings[name].Value;
                }
                else
                {
                    result = defaultValue;
                }
            }
            return result;
        }

        private void SetProperty(string value, [CallerMemberName] string name = "")
        {
            string field = GetPropertyValue(name);
            if (!EqualityComparer<string>.Default.Equals(field, value))
            {
                lock (_allSettings)
                {
                    _allSettings[name].Value = value;
                    Services.Database.SettingsDatabaseService.Instance.Execute((db) =>
                    {
                        db.Save(_allSettings[name]);
                    });
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private string GetPropertyValue(string name)
        {
            string result;
            lock (_allSettings)
            {
                Models.Settings.ApplicationSetting s;
                if (_allSettings.TryGetValue(name, out s))
                {
                    result = _allSettings[name].Value;
                }
                else
                {
                    result = null;
                    s = new Models.Settings.ApplicationSetting();
                    s.Value = null;
                    s.Name = name;
                    Services.Database.SettingsDatabaseService.Instance.Execute((db) =>
                    {
                        db.Save(s);
                    });
                    _allSettings.Add(s.Name, s);
                }
            }
            return result;
        }

    }
}
