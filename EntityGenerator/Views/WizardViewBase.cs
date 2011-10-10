using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenNETCF.IoC.UI;

namespace EntityGenerator.Views
{
    public partial class WizardViewBase : SmartPart, IWizardView
    {
        public WizardViewBase()
        {
            InitializeComponent();
        }
        
        public string Caption
        {
            get { return caption.Text; }
            set { caption.Text = value; }
        }

        public virtual void OnNavigatingToForward() { }
        public virtual void OnNavigatingAwayForward() { }
        public virtual void OnNavigatingToBackward() { }
        public virtual void OnNavigatingAwayBackward() { }
    }
}
