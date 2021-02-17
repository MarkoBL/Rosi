using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Rosi.Core
{
    public static class Tr
    {
        static Translation _base;
        static Translation _translation;

        public static void Init(string language)
        {
            _base = new Translation("en");
            if (string.IsNullOrWhiteSpace(language) || language == "en")
                _translation = _base;
            else
                _translation = new Translation(language);

            _base.LoadEmbedded();
            if (_base != _translation)
                _translation.LoadEmbedded();
        }

        public static void LoadFiles(DirectoryInfo path)
        {
            _base?.LoadFiles(path);
            if (_base != _translation)
                _translation.LoadFiles(path);
        }

        public static void AddLines(IReadOnlyList<(string, string)> lines)
        {
            _translation.ProcessLines(lines);
        }

        public static bool Has(string key)
        {
            if (_translation.Has(key))
                return true;

            return _base.Has(key);
        }

        public static string Get(string key, params object[] parameterList)
        {
            var result = _translation.Get(key, out var succes, parameterList);
            if (succes)
                return result;

            return _base.Get(key, parameterList);
        }

        public static string Get(string key, out bool success, params object[] parameterList)
        {
            var result = _translation.Get(key, out success, parameterList);
            if (success)
                return result;

            return _base.Get(key, out success, parameterList);
        }

        public static bool TryGet(string key, out string text, params object[] parameterList)
        {
            var result = _translation.Get(key, out var success, parameterList);
            if (success)
            {
                text = result;
                return true;
            }

            result = _base.Get(key, out success, parameterList);
            if (success)
            {
                text = result;
                return true;
            }

            text = null;
            return false;
        }

        sealed class Translation
        {
            public bool Valid => _items.Count > 0;
            public readonly string Language;

            readonly Dictionary<string, string> _items = new Dictionary<string, string>();

            public Translation(string language)
            {
                Language = language;
            }

            void ProcessLines(IReadOnlyList<string> lines)
            {
                var replaces = new List<(string, string)>();
                foreach (var line in lines)
                {
                    var split = line.IndexOf(':');
                    if (split > 0)
                    {
                        var key = line.Substring(0, split);
                        var value = line.Substring(split + 2).Replace("\\n", "\n");
                        if (value.Length > 0)
                        {
                            if (value[0] == '.')
                                replaces.Add((key, value.Substring(1)));

                            _items[key] = value;
                        }
                    }
                }

                UpdateReplaces(replaces);
            }

            public void ProcessLines(IReadOnlyList<(string, string)> lines)
            {
                var replaces = new List<(string, string)>();
                foreach (var line in lines)
                {
                    var key = line.Item1;
                    var value = line.Item2;

                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                        continue;

                    if (value[0] == '.')
                        replaces.Add((key, value.Substring(1)));

                    _items[key] = value;
                }

                UpdateReplaces(replaces);
            }

            void UpdateReplaces(IReadOnlyList<(string, string)> replaces)
            {
                foreach (var replace in replaces)
                {
                    if (_items.TryGetValue(replace.Item2, out var newValue))
                        _items[replace.Item1] = newValue;
                }
            }

            public void LoadEmbedded()
            {
                var lines = FromResource($"{Language}.common.txt");
                ProcessLines(lines);

                lines = FromResource($"{Language}.txt");
                ProcessLines(lines);
            }

            public void LoadFiles(DirectoryInfo path)
            {
                var lines = FromFile(Path.Combine(path.FullName, $"{Language}.common.txt"));
                ProcessLines(lines);

                lines = FromFile(Path.Combine(path.FullName, $"{Language}.txt"));
                ProcessLines(lines);
            }

            public bool Has(string key)
            {
                return _items.ContainsKey(key);
            }

            public string Get(string key, params object[] parameterList)
            {
                if (_items.TryGetValue(key, out var value))
                {
                    if (parameterList != null)
                        return string.Format(value, parameterList);
                    return value;
                }
                return key;
            }

            public string Get(string key, out bool success, params object[] parameterList)
            {
                if (_items.TryGetValue(key, out var value))
                {
                    success = true;
                    if (parameterList != null && parameterList.Length > 0)
                        return string.Format(value, parameterList);
                    return value;
                }

                success = false;
                return key;
            }

            static List<string> FromFile(string filepath)
            {
                var result = new List<string>();

                if (File.Exists(filepath))
                {
                    var lines = File.ReadAllLines(filepath);
                    foreach (var line in lines)
                    {
                        var l = line.Trim();
                        if (l.Length == 0)
                            continue;
                        if (l[0] == '#')
                            continue;

                        result.Add(l);
                    }
                }

                return result;
            }

            static List<string> FromResource(string filename)
            {
                var result = new List<string>();

                var assembly = typeof(Translation).GetTypeInfo().Assembly;
                foreach (string resource in assembly.GetManifestResourceNames())
                {
                    if (resource.EndsWith(filename, StringComparison.Ordinal))
                    {
                        using var stream = assembly.GetManifestResourceStream(resource);
                        using var reader = new StreamReader(stream, Encoding.UTF8);

                        while (true)
                        {
                            var line = reader.ReadLine()?.Trim();
                            if (line == null)
                                break;
                            if (line.Length == 0)
                                continue;
                            if (line[0] == '#')
                                continue;

                            result.Add(line);
                        }
                    }
                }

                return result;
            }
        }
    }
}
