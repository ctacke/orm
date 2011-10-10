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
    public partial class EditEntitiesView : WizardViewBase
    {
        public EditEntitiesView()
        {
            InitializeComponent();
            this.Caption = "Edit Entities";
        }
    }
}
