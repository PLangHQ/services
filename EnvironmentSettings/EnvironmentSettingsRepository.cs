using Jil;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PLang.Interfaces;
using PLang.Models;
using System.Collections;
using System.Text;

namespace EnvironmentSettings
{
	public class EnvironmentSettingsRepository : ISettingsRepository
	{
		private readonly ILogger logger;

		public EnvironmentSettingsRepository(IPLangFileSystem fileSystem, ILogger logger)
		{
			this.logger = logger;

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

		private string GetKey(string? fullName, string? key)
		{
			return $"{fullName.Replace("+", "-")}_{key.Replace("+", "-")}";
		}

		public Setting? Get(string? fullName, string? type, string? key)
		{
			var json = Environment.GetEnvironmentVariable(GetKey(fullName, key));
			if (json == null) return null;

			return JsonConvert.DeserializeObject<Setting>(json);
		}

		public IEnumerable<Setting> GetSettings()
		{
			List<Setting> settings = new List<Setting>();
			var dict = Environment.GetEnvironmentVariables();
			foreach (DictionaryEntry item in dict)
			{
				try
				{
					string value = item.Value.ToString();
					if (value.StartsWith("{"))
					{
						var setting = JsonConvert.DeserializeObject<Setting>(item.Value.ToString());
						if (setting != null) { settings.Add(setting); }
					}
				}
				catch { }
			}

			return settings;
		}

		public void Remove(Setting setting)
		{
			logger.LogWarning($"Cannot remove Environment variable: {setting.Key}");
		}

		public void Set(Setting? setting)
		{
			if (setting == null) return;

			var key = GetKey(setting.ClassOwnerFullName, setting.Key);
			var json = JsonConvert.SerializeObject(setting);

			Environment.SetEnvironmentVariable(key, json);
		}

		public void SetSharedDataSource(string? appId = null)
		{

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
	}
}
