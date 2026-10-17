using System;
using System.Text;

namespace JellyRiot.Core
{
    public static class DailySeed
    {
        public static ulong Derive(string dayKey, int rulesetVersion, string fixedSalt)
        {
            if (string.IsNullOrWhiteSpace(dayKey))
            {
                throw new ArgumentException("A UTC day key is required.", nameof(dayKey));
            }

            if (rulesetVersion < 1 || string.IsNullOrEmpty(fixedSalt))
            {
                throw new ArgumentException("Ruleset version and fixed salt are required.");
            }

            byte[] bytes = Encoding.UTF8.GetBytes(dayKey + "|" + rulesetVersion + "|" + fixedSalt);
            ulong hash = 14695981039346656037UL;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= 1099511628211UL;
            }

            return hash;
        }
    }
}
