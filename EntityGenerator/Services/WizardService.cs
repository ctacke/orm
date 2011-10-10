using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using OpenNETCF.IoC.UI;
using EntityGenerator.Views;
using OpenNETCF.IoC;
using EntityGenerator.Presenters;
using System.Windows.Forms;
using EntityGenerator.Constants;
using EntityGenerator.Entities;

namespace EntityGenerator.Services
{
    class WizardService
    {
        private WizardStep CurrentStep { get; set; }
        private IWizardView CurrentView { get; set; }

        [ServiceDependency]
        WizardPresenter Presenter { get; set; }
        public Workspace Workspace { get; set; }

        public WizardService()
        {
            CurrentStep = WizardStep.None;
        }

        public void Start()
        {
            if (Workspace == null) return;

            Presenter.BackwardAllowed = false;
            Presenter.ForwardAllowed = false;
            CurrentStep = WizardStep.SelectStore;

            Workspace.Show<SelectStoreView>();
        }

        public bool Quit()
        {
            return (MessageBox.Show(
                    "Exit Wizard?", 
                    "Quit?", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Question, 
                    MessageBoxDefaultButton.Button2) 
                == DialogResult.Yes);
        }

        public void Restart()
        {
            SetStep(WizardStep.SelectStore);
            Workspace.Show<SelectStoreView>();
        }

        public void Back()
        {
            IWizardView nextView = null;

            if (CurrentView != null)
            {
                CurrentView.OnNavigatingAwayBackward();
            }

            switch (CurrentStep)
            {
                case WizardStep.SelectEntities:
                    SetStep(WizardStep.SelectStore);
                    nextView = RootWorkItem.SmartParts.GetFirstOrCreate<SelectStoreView>();
                    break;
                case WizardStep.EditEntities:
                    SetStep(WizardStep.SelectEntities);
                    nextView = RootWorkItem.SmartParts.GetFirstOrCreate<SelectEntitiesView>();
                    break;
                case WizardStep.GenerateCode:
                    SetStep(WizardStep.EditEntities);
                    nextView = RootWorkItem.SmartParts.GetFirstOrCreate<EditEntitiesView>();
                    break;
            }

            if (nextView != null)
            {
                nextView.OnNavigatingToBackward();
                Workspace.Show(nextView);
                CurrentView = nextView;
            }
        }

        public void Next()
        {
            IWizardView nextView = null;

            if (CurrentView != null)
            {
                CurrentView.OnNavigatingAwayForward();
            }

            switch (CurrentStep)
            {
                case WizardStep.SelectStore:
                    SetStep(WizardStep.SelectEntities);
                    nextView = RootWorkItem.SmartParts.GetFirstOrCreate<SelectEntitiesView>();
                    break;
                case WizardStep.SelectEntities:
                    SetStep(WizardStep.EditEntities);
                    nextView = RootWorkItem.SmartParts.GetFirstOrCreate<EditEntitiesView>();
                    break;
                case WizardStep.EditEntities:
                case WizardStep.GenerateCode:
                    SetStep(WizardStep.GenerateCode);
                    nextView = RootWorkItem.SmartParts.GetFirstOrCreate<GenerateCodeView>();
                    break;
            }

            if (nextView != null)
            {
                nextView.OnNavigatingToForward();
                Workspace.Show(nextView);
                CurrentView = nextView;
            }
        }

        private void SetStep(WizardStep newStep)
        {
            switch (newStep)
            {
                case WizardStep.SelectStore:
                    Presenter.RestartAllowed = false;
                    Presenter.BackwardAllowed = false;
                    Presenter.ForwardAllowed = true;
                    Presenter.LastStep = false;
                    break;
                case WizardStep.SelectEntities:
                    Presenter.RestartAllowed = true;
                    Presenter.BackwardAllowed = true;
                    Presenter.ForwardAllowed = true;
                    Presenter.LastStep = false;
                    break;
                case WizardStep.EditEntities:
                    Presenter.RestartAllowed = true;
                    Presenter.BackwardAllowed = true;
                    Presenter.ForwardAllowed = true;
                    Presenter.LastStep = false;
                    break;
                case WizardStep.GenerateCode:
                    Presenter.RestartAllowed = true;
                    Presenter.BackwardAllowed = true;
                    Presenter.ForwardAllowed = true;
                    Presenter.LastStep = true;
                    break;
            }

            CurrentStep = newStep;
        }
    }
}
