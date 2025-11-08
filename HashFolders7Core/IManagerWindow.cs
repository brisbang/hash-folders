using System;
using System.ComponentModel;
using System.Windows;
using HashLib7;

namespace HashFolders
{
    public interface IManagerWindow
    {
        public void RefreshStats(ManagerStatus managerStatus);
        public AsyncManager AsyncManager { get; }

    }
}