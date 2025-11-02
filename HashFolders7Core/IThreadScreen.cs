using HashLib7;
using System;
using System.Windows.Forms;

namespace HashFolders
{
    public interface IThreadScreen
    {
        StateEnum Refresh(object sender, EventArgs e);
        void Abort();
        void Pause();
        void Resume();
        void CloseWindow();
    }

}