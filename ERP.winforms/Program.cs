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

            // Re-authentication session loop supporting Logout
            bool relogin = true;
            while (relogin)
            {
                relogin = false;
                using var loginForm = new LoginForm();
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    using var mainForm = new Form1(loginForm.AuthenticatedUser, loginForm.AuthenticatedRole, loginForm.SelectedCompany);
                    Application.Run(mainForm);
                    if (mainForm.IsLoggedOut)
                    {
                        relogin = true;
                    }
                }
            }
        }
    }
}
