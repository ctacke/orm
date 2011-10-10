using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using EntityGenerator.Services;
using OpenNETCF.IoC;
using EntityGenerator.Entities;
using System.Windows.Forms;
using System.IO;

namespace EntityGenerator.Presenters
{
    public class WizardPresenter : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool m_forwardAllowed;
        private bool m_backwardAllowed;
        private bool m_restartAllowed;

        public bool LastStep { get; set; }
        
        internal BuildOptions GetBuildOptions()
        {
            return BuildOptions.Load(this.OptionsFile);
        }

        internal void SetBuildOptions(BuildOptions options)
        { 
            options.Save(this.OptionsFile);
        }

        private string OptionsFile
        {
            get { return Path.Combine(Application.StartupPath, "options.xml"); }
        }

        private WizardService WizardService
        {
            get { return RootWorkItem.Services.Get<WizardService>(); }
        }

        private GeneratorService GeneratorService
        {
            get { return RootWorkItem.Services.Get<GeneratorService>(); }
        }

        public bool ForwardAllowed
        {
            get { return m_forwardAllowed; }
            set
            {
                m_forwardAllowed = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("ForwardAllowed"));
            }
        }

        public bool BackwardAllowed
        {
            get { return m_backwardAllowed; }
            set
            {
                m_backwardAllowed = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("BackwardAllowed"));
            }
        }

        public bool RestartAllowed
        {
            get { return m_restartAllowed; }
            set
            {
                m_restartAllowed = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("RestartAllowed"));
            }
        }

        public IDataSource[] GetSupportedSources()
        {
            return GeneratorService.GetAvailableSources();
        }

        public object[] GetPreviousSources()
        {
            return GeneratorService.GetPreviousSources();
        }

        public void SetSource(IDataSource sourceType)
        {
            GeneratorService.SetCurrentSourceType(sourceType);
        }

        public object BrowseForSource()
        {
            return GeneratorService.BrowseForSource();
        }

        public IDataSource GetCurrentDataSource()
        {
            return GeneratorService.CurrentSourceType;
        }

        public void GoBack()
        {
            WizardService.Back();
        }
    }
}
