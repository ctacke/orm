using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNETCF.IoC.UI;

namespace EntityGenerator.Views
{
    public interface IWizardView : ISmartPart
    {
        void OnNavigatingToForward();
        void OnNavigatingAwayForward();
        void OnNavigatingToBackward();
        void OnNavigatingAwayBackward();
    }
}
