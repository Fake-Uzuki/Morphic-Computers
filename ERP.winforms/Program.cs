using System;
using System.Windows.Forms;
using ERP.winforms.UI.Dialogs;

namespace ERP.winforms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.ApplicationExit += (s, e) =>
            {
                Services.DataService.Instance.SaveAllToDisk();
            };

            // UC-01: Micro Company User Authentication Form
            using var loginForm = new LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new Form1(loginForm.AuthenticatedUser, loginForm.AuthenticatedRole, loginForm.SelectedCompany));
            }
        }
    }
}
