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
using EntityGenerator.Constants;
using EntityGenerator.Services;
using OpenNETCF.ORM;

namespace EntityGenerator.Views
{
    public partial class SelectEntitiesView : WizardViewBase
    {
        [ServiceDependency]
        private GeneratorService GeneratorService { get; set; }
        private WizardPresenter Presenter { get; set; }
        private IDataSource Source { get; set; }

        public SelectEntitiesView()
        {
            InitializeComponent();
        }

        [InjectionConstructor]
        public SelectEntitiesView([ServiceDependency]WizardPresenter presenter)
            : this()
        {
            Presenter = presenter;

            this.Caption = "Select Entities";
            entityTree.CheckBoxes = true;

            Source = Presenter.GetCurrentDataSource();
        }

        public override void OnNavigatingToForward()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                entityTree.BeginUpdate();
                entityTree.Nodes.Clear();

                var entities = Source.GetEntityDefinitions();

                foreach (var entity in entities)
                {
                    var entityNode = new TreeNode(entity.Entity.NameInStore);
                    entityNode.Tag = entity;
                    entityNode.Checked = true;

                    foreach (var field in entity.Fields)
                    {
                        var fieldNode = new TreeNode(field.FieldName);
                        fieldNode.Tag = field;
                        entityNode.Nodes.Add(fieldNode);
                        fieldNode.Checked = true;
                    }

                    entityTree.Nodes.Add(entityNode);
                }
            }
            catch (InvalidPasswordException)
            {
                Presenter.GoBack();
            }
            finally
            {
                entityTree.EndUpdate();
                Cursor.Current = Cursors.Default;
            }
        }

        public override void OnNavigatingAwayForward()
        {
            // TODO: walk tree and build structure
            var structure = new List<EntityGenerator.Entities.EntityInfo>();

            foreach (TreeNode entityNode in entityTree.Nodes)
            {
                if(!entityNode.Checked) continue;

                var info = entityNode.Tag as EntityGenerator.Entities.EntityInfo;

                foreach (TreeNode fieldNode in entityNode.Nodes)
                {
                    if (!fieldNode.Checked)
                    {
                        info.Fields.Remove(fieldNode.Tag as FieldAttribute);
                    }
                }

                structure.Add(info);
            }

            GeneratorService.SetSelectedStructure(structure);
        }

        private void selectEntities_Click(object sender, EventArgs e)
        {
            foreach (TreeNode entityNode in entityTree.Nodes)
            {
                entityNode.Checked = true;
            }
        }

        private void unselectEntities_Click(object sender, EventArgs e)
        {
            foreach (TreeNode entityNode in entityTree.Nodes)
            {
                entityNode.Checked = false;
            }
        }
    }
}
