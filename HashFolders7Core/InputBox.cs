using System;
using System.Windows.Forms;

public static class InputBox
{
    public static string Show(string prompt, string title = "Input", string defaultValue = "")
    {
        Form form = new();
        Label label = new();
        TextBox textBox = new();
        Button buttonOk = new();
        Button buttonCancel = new();

        form.Text = title;
        label.Text = prompt;
        textBox.Text = defaultValue;

        label.SetBounds(9, 9, 372, 60);
        textBox.SetBounds(12, 9 + 60 + 5, 100, 20);
        buttonOk.SetBounds(217, 9 + 60 + 5, 75, 23);
        buttonCancel.SetBounds(217 + 75 + 5, 9 + 60 + 5, 75, 23);

        label.AutoSize = true;
//        textBox.Anchor |= AnchorStyles.Right;
        buttonOk.Text = "OK";
        buttonCancel.Text = "Cancel";
        buttonOk.DialogResult = DialogResult.OK;
        buttonCancel.DialogResult = DialogResult.Cancel;

        form.ClientSize = new System.Drawing.Size(396, textBox.Top + textBox.Height + 5);
        form.Controls.AddRange([label, textBox, buttonOk, buttonCancel]);
        form.ClientSize = new System.Drawing.Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.MinimizeBox = false;
        form.MaximizeBox = false;
        form.AcceptButton = buttonOk;
        form.CancelButton = buttonCancel;

        return form.ShowDialog() == DialogResult.OK ? textBox.Text : null;
    }
}
