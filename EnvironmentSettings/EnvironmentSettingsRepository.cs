using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PLang.Errors;
using PLang.Interfaces;
using PLang.Models;
using PLang.Utils;
using System.Collections;
using System.Text;

namespace EnvironmentSettings
{
	public class EnvironmentSettingsRepository : ISettingsRepository
	{
		private readonly ILogger logger;
		private Dictionary<string, Setting> environmentSettings;

		public bool IsDefaultSystemDbPath
		{
			get { return true; }
		}

		public EnvironmentSettingsRepository(IPLangFileSystem fileSystem, ILogger logger)
		{
			this.logger = logger;

			LoadVars();

			if (!fileSystem.File.Exists(".env")) return;

			var lines = fileSystem.File.ReadAllLines(".env");
			foreach (var line in lines)
			{
				var settingValue = line.Substring(line.IndexOf('=') + 1);
				if (string.IsNullOrWhiteSpace(settingValue)) continue;
				try
				{
					Set(JsonConvert.DeserializeObject<Setting>(settingValue));
				}
				catch (Exception ex)
				{
					logger.LogError(ex, $"Error deserializing setting in line:{line}");
				}
			}
			
		}

		private void LoadVars()
		{
			environmentSettings = new Dictionary<string, Setting>();
			var variables = Environment.GetEnvironmentVariables();
			foreach (DictionaryEntry variable in variables)
			{
				var key = GetKey("", variable.Key.ToString());
				var setting = GetSettingFromValue(key, variable.Value.ToString());
				environmentSettings.AddOrReplace(key, setting);
			}
		}

		private Setting? GetSettingFromValue(string key, string value)
		{
			if (value.TrimStart().StartsWith("{"))
			{
				try
				{
					var setting = JsonConvert.DeserializeObject<Setting>(value.ToString());
					return setting;
				}
				catch
				{
					logger.LogError($"Error deserializing variable: {key}");
					return null;
				}
			} else
			{
				value = JsonConvert.SerializeObject(value);
				
				var setting = new Setting("1", key, "string", key, value);
				return setting;
			}
		}

		private string GetKey(string? fullName, string? key)
		{
            if (key.Contains("+"))
            {
				return key.Substring(key.LastIndexOf('+') + 1);
            }
			return key;
		}

		public Setting? Get(string? fullName, string? type, string? key)
		{
			string environmentKey = GetKey(fullName, key);
			if (environmentSettings.ContainsKey(environmentKey))
			{
				var setting = environmentSettings[environmentKey];
				if (setting != null)
				{
					return setting;
				}
			}
			
			var json = Environment.GetEnvironmentVariable(environmentKey);
			if (json == null)
			{				
				logger.LogWarning($"Environment variable '{environmentKey}' not found. fullName:{fullName} | type:{type} | key:{key}");
				return null;
			}
			return GetSettingFromValue(environmentKey, json);
		}

		public IEnumerable<Setting> GetSettings()
		{
			LoadVars();

			return environmentSettings.Select(p => p.Value);
		}

		public void Remove(Setting setting)
		{
			var key = GetKey(setting.ClassOwnerFullName, setting.Key);
			environmentSettings.Remove(key);

			logger.LogWarning($"Cannot really remove Environment variable: {setting.Key}");
		}

		public void Set(Setting? setting)
		{
			if (setting == null) return;

			var key = GetKey(setting.ClassOwnerFullName, setting.Key);
			var json = JsonConvert.SerializeObject(setting);

			Environment.SetEnvironmentVariable(key, json);
			environmentSettings.AddOrReplace(key, setting);
			logger.LogWarning($@"Set environment variable, get lost on next restart
key: {key}
value: {json}");
		}

		public void SetSharedDataSource(string? appId = null)
		{
			//no shared datasource. Maybe this could be user and system variables?
		}

		public string SerializeSettings()
		{
			StringBuilder sb = new StringBuilder();
			var settings = GetSettings();
			foreach (var setting in settings)
			{
				var key = GetKey(setting.ClassOwnerFullName, setting.Key);
				var json = JsonConvert.SerializeObject(setting);
				sb.Append(key + "=" + json + Environment.NewLine);
			}
			return sb.ToString();
		}

		public IError? SetSystemDbPath(string path)
		{
			return null;
		}

		public void ResetSystemDbPath()
		{
			
		}
	}
}
