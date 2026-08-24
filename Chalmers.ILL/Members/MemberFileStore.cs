using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace Chalmers.ILL.Members
{
    // Replaces the Umbraco member database (cmsMember tables in Umbraco.sdf) with a small,
    // manually maintained JSON file. There are only a handful of accounts, so a file is enough
    // and removes the last live database dependency on Umbraco.
    public static class MemberFileStore
    {
        public static List<MemberAccount> Load() => Load(ResolvePath());

        public static List<MemberAccount> Load(string path)
        {
            if (!File.Exists(path))
                return new List<MemberAccount>();

            try
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<List<MemberAccount>>(json) ?? new List<MemberAccount>();
            }
            catch (Exception)
            {
                return new List<MemberAccount>();
            }
        }

        public static void Save(List<MemberAccount> accounts) => Save(accounts, ResolvePath());

        public static void Save(List<MemberAccount> accounts, string path)
        {
            var json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        private static string ResolvePath()
        {
            var appRoot = HttpRuntime.AppDomainAppPath;
            if (string.IsNullOrEmpty(appRoot))
                appRoot = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appRoot, "Config", "members.json");
        }
    }
}
