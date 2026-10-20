#if AMBERSCREEN_DEVCONSOLE
using System;
using System.Collections.Generic;
using System.Text;

namespace AmberScreen.Dev
{
    sealed class DevConsoleCommand
    {
        public string Name;
        public string Help;
        public Action<string[]> Run;
    }

    static class DevConsoleCommands
    {
        static readonly Dictionary<string, DevConsoleCommand> s_Commands =
            new Dictionary<string, DevConsoleCommand>(StringComparer.OrdinalIgnoreCase);
        static readonly List<string> s_History = new List<string>(32);
        static int s_HistoryIndex = -1;

        public static IReadOnlyDictionary<string, DevConsoleCommand> All => s_Commands;
        public static IReadOnlyList<string> History => s_History;

        public static void Reset()
        {
            s_Commands.Clear();
            s_History.Clear();
            s_HistoryIndex = -1;
            Register("help", "List registered commands.", args =>
            {
                var sb = new StringBuilder();
                sb.Append("Commands:");
                foreach (var kv in s_Commands)
                {
                    sb.Append('\n');
                    sb.Append(kv.Value.Name);
                    if (!string.IsNullOrEmpty(kv.Value.Help))
                    {
                        sb.Append(" — ");
                        sb.Append(kv.Value.Help);
                    }
                }

                DevConsole.Note(sb.ToString());
            });
        }

        public static void Register(string name, string help, Action<string[]> run)
        {
            if (string.IsNullOrEmpty(name) || run == null)
                return;
            s_Commands[name] = new DevConsoleCommand
            {
                Name = name,
                Help = help ?? string.Empty,
                Run = run
            };
        }

        public static void Unregister(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;
            s_Commands.Remove(name);
        }

        public static bool TryRun(string line, out string echo)
        {
            echo = line ?? string.Empty;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            PushHistory(line.Trim());
            if (!TryParse(line, out string name, out string[] args))
            {
                DevConsole.Note("Parse error.");
                return false;
            }

            if (!s_Commands.TryGetValue(name, out var cmd))
            {
                DevConsole.Note("Unknown command: " + name);
                return false;
            }

            try
            {
                cmd.Run(args);
            }
            catch (Exception e)
            {
                DevConsole.Note(e.Message);
                return false;
            }

            return true;
        }

        public static string HistoryPrev(string current)
        {
            if (s_History.Count == 0)
                return current;
            if (s_HistoryIndex < 0)
                s_HistoryIndex = s_History.Count;
            s_HistoryIndex = MathfMax(0, s_HistoryIndex - 1);
            return s_History[s_HistoryIndex];
        }

        public static string HistoryNext(string current)
        {
            if (s_History.Count == 0 || s_HistoryIndex < 0)
                return string.Empty;
            s_HistoryIndex++;
            if (s_HistoryIndex >= s_History.Count)
            {
                s_HistoryIndex = -1;
                return string.Empty;
            }

            return s_History[s_HistoryIndex];
        }

        static void PushHistory(string line)
        {
            if (s_History.Count > 0 &&
                string.Equals(s_History[s_History.Count - 1], line, StringComparison.Ordinal))
            {
                s_HistoryIndex = -1;
                return;
            }

            s_History.Add(line);
            while (s_History.Count > 32)
                s_History.RemoveAt(0);
            s_HistoryIndex = -1;
        }

        /// <summary>Split <paramref name="line"/> into name + args; quotes keep spaces.</summary>
        public static bool TryParse(string line, out string name, out string[] args)
        {
            name = null;
            args = Array.Empty<string>();
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var tokens = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Length = 0;
                    }

                    continue;
                }

                sb.Append(c);
            }

            if (inQuotes)
                return false;
            if (sb.Length > 0)
                tokens.Add(sb.ToString());
            if (tokens.Count == 0)
                return false;

            name = tokens[0];
            if (tokens.Count == 1)
            {
                args = Array.Empty<string>();
                return true;
            }

            args = new string[tokens.Count - 1];
            for (int i = 1; i < tokens.Count; i++)
                args[i - 1] = tokens[i];
            return true;
        }

        static int MathfMax(int a, int b) => a > b ? a : b;
    }
}
#endif
