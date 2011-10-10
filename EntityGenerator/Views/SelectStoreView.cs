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
using EntityGenerator.Presenters;
using EntityGenerator.Entities;

namespace EntityGenerator.Views
{
    public partial class SelectStoreView : WizardViewBase
    {
        WizardPresenter Presenter { get; set; }

        public SelectStoreView()
        {
            InitializeComponent();
        }

        [InjectionConstructor]
        public SelectStoreView([ServiceDependency]WizardPresenter presenter)
            : this()
        {
            Presenter = presenter;

            this.Caption = "Select Data Store";

            sourceType.SelectedIndexChanged += new EventHandler(sourceType_SelectedIndexChanged);
            LoadSourceTypes();

            dataSourceList.TextChanged += new EventHandler(dataSourceList_TextChanged);
        }

        void dataSourceList_TextChanged(object sender, EventArgs e)
        {
            Presenter.ForwardAllowed = (dataSourceList.Text.Length > 0);
        }

        void sourceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sourceType.SelectedIndex < 0) return;

            // get a list of previously selected sources for this source type
            var selectedSource = sourceType.SelectedItem as IDataSource;
            if (selectedSource == null) return;

            Presenter.SetSource(selectedSource);
            var previousSources = Presenter.GetPreviousSources();

            dataSourceList.Items.Clear();
            
            if (previousSources != null)
            {
                foreach (var source in previousSources)
                {
                    dataSourceList.Items.Add(source);
                }
            }
        }

        private void LoadSourceTypes()
        {
            var sources = Presenter.GetSupportedSources();

            this.sourceType.Items.Clear();

            foreach (var source in sources)
            {
                sourceType.Items.Add(source);
            }

            sourceType.SelectedIndex = 0;
        }

        private void browse_Click(object sender, EventArgs e)
        {
            var store = Presenter.BrowseForSource();

            if (store == null) return;
            dataSourceList.Text = store.ToString();
        }
    }
}
