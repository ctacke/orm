using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EntityGenerator.Dialogs
{
    public partial class GetPasswordDialog : Form
    {
        public string Password { get; private set; }

        public GetPasswordDialog()
        {
            InitializeComponent();

            password.KeyDown += new KeyEventHandler(password_KeyDown);
        }

        void  password_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.KeyCode)
            {
                case Keys.Enter:
                    AcceptPassword();
                    break;
                case Keys.Escape:
                    CancelPassword();
                    break;
            }
        }

        private void ok_Click(object sender, EventArgs e)
        {
            AcceptPassword();
        }

        private void AcceptPassword()
        {
            Password = password.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelPassword()
        {
            Password = string.Empty;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
