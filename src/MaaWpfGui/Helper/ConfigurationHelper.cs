// <copyright file="ConfigurationHelper.cs" company="MaaAssistantArknights">
// MaaWpfGui - A part of the MaaCoreArknights project
// Copyright (C) 2021 MistEO and Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MaaWpfGui.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MaaWpfGui.Helper
{
    public class ConfigurationHelper
    {
        private static readonly string _configurationFile = Path.Combine(Environment.CurrentDirectory, "config/gui.json");
        private static readonly string _configurationBakFile = Path.Combine(Environment.CurrentDirectory, "config/gui.json.bak");
        private static Dictionary<string, Dictionary<string, string>> _kvsMap;
        private static string _current = ConfigurationKeys.DefaultConfiguration;
        private static Dictionary<string, string> _kvs;

        private static readonly ILogger _logger = Log.ForContext<ConfigurationHelper>();

        private static readonly object _lock = new object();

        public delegate void ConfigurationUpdateEventHandler(string key, string oldValue, string newValue);

        public static event ConfigurationUpdateEventHandler ConfigurationUpdateEvent;

        private static bool Released { get; set; }

        /// <summary>
        /// Get a configuration value
        /// </summary>
        /// <param name="key">The config key</param>
        /// <param name="defaultValue">The default value to return if the key is not existed</param>
        /// <returns>The config value</returns>
        public static string GetValue(string key, string defaultValue)
        {
            var hasValue = _kvs.TryGetValue(key, out var value);

            _logger.Debug("Read configuration key {Key} with default value {DefaultValue}, configuration hit: {HasValue}, configuration value {Value}", key, defaultValue, hasValue, value);

            return hasValue
                ? value
                : defaultValue;
        }

        /// <summary>
        /// Set a configuration value
        /// </summary>
        /// <param name="key">The config key</param>
        /// <param name="value">The config value</param>
        /// <returns>The return value of <see cref="Save"/></returns>
        public static bool SetValue(string key, string value)
        {
            var old = string.Empty;
            if (_kvs.ContainsKey(key))
            {
                old = _kvs[key];
                _kvs[key] = value;
            }
            else
            {
                _kvs.Add(key, value);
            }

            var result = Save();
            if (result)
            {
                ConfigurationUpdateEvent?.Invoke(key, old, value);
                _logger.Debug("Configuration {Key} has been set to {Value}", key, value);
            }
            else
            {
                _logger.Warning("Failed to save configuration {Key} to {Value}", key, value);
            }

            return result;
        }

        /// <summary>
        /// Deletes a configuration
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The return value of <see cref="Save"/>.</returns>
        public static bool DeleteValue(string key)
        {
            var old = string.Empty;
            if (_kvs.TryGetValue(key, out var kv))
            {
                old = kv;
            }

            _kvs.Remove(key);
            var result = Save();
            if (result)
            {
                ConfigurationUpdateEvent?.Invoke(key, old, string.Empty);
                _logger.Debug("Configuration {Key} has been deleted", key);
            }
            else
            {
                _logger.Warning("Failed to save configuration file when deleted {Key}", key);
            }

            return result;
        }

        /// <summary>
        /// Load configuration file
        /// </summary>
        /// <returns>True if success, false if failed</returns>
        public static bool Load()
        {
            if (Directory.Exists("config") is false)
            {
                Directory.CreateDirectory("config");
            }

            // Load configuration file
            var parsed = ParseJsonFile(_configurationFile);
            if (parsed is null)
            {
                if (File.Exists(_configurationBakFile))
                {
                    File.Copy(_configurationBakFile, _configurationFile, true);
                    parsed = ParseJsonFile(_configurationFile);
                }
            }

            if (parsed is null)
            {
                _logger.Information("Failed to load configuration file, creating a new one");

                _kvsMap = new Dictionary<string, Dictionary<string, string>>();
                _current = ConfigurationKeys.DefaultConfiguration;
                _kvsMap[_current] = new Dictionary<string, string>();
                _kvs = _kvsMap[_current];

                return false;
            }

            if (parsed.ContainsKey(ConfigurationKeys.ConfigurationMap))
            {
                // new version
                _kvsMap = parsed[ConfigurationKeys.ConfigurationMap].ToObject<Dictionary<string, Dictionary<string, string>>>();
                _current = parsed[ConfigurationKeys.CurrentConfiguration].ToString();
                _kvs = _kvsMap[_current];
            }
            else
            {
                // old version
                _logger.Information("Configuration file is in old version, migrating to new version");

                _kvsMap = new Dictionary<string, Dictionary<string, string>>();
                _current = ConfigurationKeys.DefaultConfiguration;
                _kvsMap[_current] = parsed.ToObject<Dictionary<string, string>>();
                _kvs = _kvsMap[_current];
            }

            return true;
        }

        /// <summary>
        /// Save configuration file
        /// </summary>
        /// <returns>The result of saving process</returns>
        private static bool Save(string file = null)
        {
            if (Released)
            {
                _logger.Warning("Attempting to save configuration file after release, this is not allowed");
                return false;
            }

            var jsonStr = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { ConfigurationKeys.ConfigurationMap, _kvsMap },
                { ConfigurationKeys.CurrentConfiguration, _current },
            }, Formatting.Indented);

            try
            {
                lock (_lock)
                {
                    File.WriteAllText(file ?? _configurationFile, jsonStr);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to save configuration file.");
                return false;
            }

            return true;
        }

        public static string GetCheckedStorage(string storageKey, string name, string defaultValue)
        {
            return GetValue(storageKey + name + ".IsChecked", defaultValue);
        }

        public static bool SetCheckedStorage(string storageKey, string name, string value)
        {
            return SetValue(storageKey + name + ".IsChecked", value);
        }

        public static string GetFacilityOrder(string facility, string defaultValue)
        {
            return GetValue("Infrast.Order." + facility, defaultValue);
        }

        public static bool SetFacilityOrder(string facility, string value)
        {
            return SetValue("Infrast.Order." + facility, value);
        }

        public static string GetTimer(int i, string defaultValue)
        {
            return GetValue($"Timer.Timer{i + 1}", defaultValue);
        }

        public static bool SetTimer(int i, string value)
        {
            return SetValue($"Timer.Timer{i + 1}", value);
        }

        public static string GetTimerHour(int i, string defaultValue)
        {
            return GetValue($"Timer.Timer{i + 1}Hour", defaultValue);
        }

        public static bool SetTimerHour(int i, string value)
        {
            return SetValue($"Timer.Timer{i + 1}Hour", value);
        }

        public static string GetTimerMin(int i, string defaultValue)
        {
            return GetValue($"Timer.Timer{i + 1}Min", defaultValue);
        }

        public static bool SetTimerMin(int i, string value)
        {
            return SetValue($"Timer.Timer{i + 1}Min", value);
        }

        public static string GetTaskOrder(string task, string defaultValue)
        {
            return GetValue("TaskQueue.Order." + task, defaultValue);
        }

        public static bool SetTaskOrder(string task, string value)
        {
            return SetValue("TaskQueue.Order." + task, value);
        }

        public static void Release()
        {
            Save();
            Save(_configurationBakFile);
            Released = true;
        }

        private static JObject ParseJsonFile(string filePath)
        {
            if (File.Exists(filePath) is false)
            {
                return null;
            }

            var str = File.ReadAllText(filePath);

            try
            {
                var obj = (JObject)JsonConvert.DeserializeObject(str);
                if (obj is null)
                {
                    throw new Exception("Failed to parse json file");
                }

                return obj;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to deserialize json file: {FilePath}", filePath);
            }

            return null;
        }

        public static bool SwitchConfiguration(string configName)
        {
            if (_kvsMap.ContainsKey(configName) is false)
            {
                _logger.Warning("Configuration {ConfigName} does not exist", configName);
                return false;
            }

            _current = configName;
            _kvs = _kvsMap[_current];
            return true;
        }

        public static bool AddConfiguration(string configName, string copyFrom = null)
        {
            if (string.IsNullOrEmpty(configName))
            {
                return false;
            }

            if (_kvsMap.ContainsKey(configName))
            {
                _logger.Warning("Configuration {ConfigName} already exists", configName);
                return false;
            }

            if (copyFrom is null)
            {
                _kvsMap[configName] = new Dictionary<string, string>();
            }
            else
            {
                if (_kvsMap.ContainsKey(copyFrom) is false)
                {
                    _logger.Warning("Configuration {ConfigName} does not exist", copyFrom);
                    return false;
                }

                _kvsMap[configName] = new Dictionary<string, string>(_kvsMap[copyFrom]);
            }

            return true;
        }

        public static bool DeleteConfiguration(string configName)
        {
            if (_kvsMap.ContainsKey(configName) is false)
            {
                _logger.Warning("Configuration {ConfigName} does not exist", configName);
                return false;
            }

            if (_current == configName)
            {
                _logger.Warning("Configuration {ConfigName} is current configuration, cannot delete", configName);
                return false;
            }

            _kvsMap.Remove(configName);
            return true;
        }

        public static List<string> GetConfigurationList()
        {
            return _kvsMap.Keys.ToList();
        }

        public static string GetCurrentConfiguration()
        {
            return _current;
        }
    }
}
