using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenNETCF.IoC;
using EntityGenerator.Views;
using EntityGenerator.Presenters;
using EntityGenerator.Services;

namespace EntityGenerator
{
    public partial class WizardForm : Form
    {
        WizardPresenter Presenter { get; set; }
        WizardService WizardService { get; set; }

        public WizardForm()
        {
            InitializeComponent();

            this.exit.Click += new EventHandler(exit_Click);
            this.next.Click += new EventHandler(next_Click);
            this.back.Click += new EventHandler(back_Click);
            this.restart.Click += new EventHandler(restart_Click);
            
            RootWorkItem.Workspaces.Add(this.wizardWorkspace);

            // set these manually becasue Property Injection will be too late (we need to use them in the ctor)
            Presenter = RootWorkItem.Services.Get<WizardPresenter>();
            WizardService = RootWorkItem.Services.Get<WizardService>();

            this.next.DataBindings.Add("Enabled", Presenter, "ForwardAllowed");
            this.back.DataBindings.Add("Enabled", Presenter, "BackwardAllowed");
            this.restart.DataBindings.Add("Enabled", Presenter, "RestartAllowed");

            WizardService.Workspace = wizardWorkspace;
            WizardService.Start();
        }

        void restart_Click(object sender, EventArgs e)
        {
            WizardService.Restart();
        }

        void back_Click(object sender, EventArgs e)
        {
            WizardService.Back();
            SetNextText();
        }

        void next_Click(object sender, EventArgs e)
        {
            WizardService.Next();
            SetNextText();
        }

        private void SetNextText()
        {
            if (Presenter.LastStep)
            {
                next.Text = "Finish";
            }
            else
            {
                next.Text = "Next";
            }
        }


        void exit_Click(object sender, EventArgs e)
        {
            if (WizardService.Quit())
            {
                this.Close();
            }
        }
    }
}