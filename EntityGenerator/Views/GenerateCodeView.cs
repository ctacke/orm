using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenNETCF.IoC.UI;
using OpenNETCF.IoC;
using EntityGenerator.Services;
using EntityGenerator.Presenters;
using EntityGenerator.Entities;
using System.Reflection;

namespace EntityGenerator.Views
{
    public partial class GenerateCodeView : WizardViewBase
    {
        private BuildOptions BuildOptions { get; set; }
        private WizardPresenter Presenter { get; set; }

        [ServiceDependency]
        private GeneratorService GeneratorService { get; set; }

        public GenerateCodeView()
        {
            InitializeComponent();
        }

        [InjectionConstructor]
        public GenerateCodeView([ServiceDependency]WizardPresenter presenter)
            : this()
        {
            Presenter = presenter;
            this.Caption = "GenerateCode";

            BuildOptions = presenter.GetBuildOptions();

            language.Items.Add("C#");
            language.SelectedIndex = 0;
            // TODO: load from options

            outputFolder.Text = BuildOptions.OutputFolder;

            entityNamespace.Text = BuildOptions.EntityNamespace;

            entityModifier.Items.Add("Public");
            entityModifier.Items.Add("Internal");

            switch (BuildOptions.EntityModifier)
            {
                case TypeAttributes.NotPublic:
                    entityModifier.SelectedIndex = 1;
                    break;
                default:
                    entityModifier.SelectedIndex = 0;
                    break;
            };
        }

        public override void OnNavigatingAwayForward()
        {
            // save current options
            // TODO: change when we change the UI
            BuildOptions.Language = OutputLanguage.CSharp;
            BuildOptions.OutputFolder = outputFolder.Text;
            BuildOptions.EntityNamespace = entityNamespace.Text;
            BuildOptions.EntityModifier = entityModifier.SelectedIndex == 1 ? TypeAttributes.NotPublic : TypeAttributes.Public;
            Presenter.SetBuildOptions(BuildOptions);

            GeneratorService.GenerateCode(BuildOptions);
        }
    }
}
