using System;
using Microsoft.Win32;

namespace HashLib7
{
    public class UserSettings
    {
        const string AppKey = "SOFTWARE\\HashFolders";
        const string RUFKey = "SearchFolder";
        const string ITCKey = "IndexThreadCount";
        const string ODUKey = "OneDriveUser";
        const string RTCKey = "ReportThreadCount";
        public static string RecentlyUsedFolder {
            get { return GetKey(RUFKey); }
            set { SetKey(RUFKey, value); }
        }

        public static int ReportThreadCount
        {
            get { return GetIntKey(RTCKey, 5); }
            set { SetKey(RTCKey, value.ToString()); }
        }

        public static int ThreadCount
        {
            get { return GetIntKey(ITCKey, 8); }
            set { SetKey(ITCKey, value.ToString()); }
        }

        public static string OneDriveUser
        {
            get { return GetKey(ODUKey); }
            set { SetKey(ODUKey, value); }
        }

        private static int GetIntKey(string key, int def = 1)
        {
            int res = 0;
            if (!int.TryParse(GetKey(key), out res))
                res = def;
            return res;
        }

        private static string GetKey(string key, string def = "")
        {
            string res = def;
            if (OperatingSystem.IsWindows())
            {
                using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(AppKey))
                {
                    if (regKey != null)
                    {
                        Object o = regKey.GetValue(key);
                        if (o == null) return def;
                        return o.ToString();
                    }
                }
            }
            return res;
        }

        private static void SetKey(string key, string value)
        {
            if (OperatingSystem.IsWindows())
            {
                using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(AppKey))
                {
                    regKey.SetValue(key, value);
                }
            }
        }
    }
}
