using OpenNETCF.ORM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ImageSample
{
    public partial class Form1 : Form
    {
        private IDataStore m_store;
        private DataClass m_current;

        public Form1()
        {
            InitializeComponent();

            picture.SizeMode = PictureBoxSizeMode.StretchImage;

            list.View = View.LargeIcon;
            list.ItemActivate += list_ItemActivate;

            m_store = new SqlCeDataStore("teststore.sdf");

            m_store.CreateOrUpdateStore();
            m_store.AddType<DataClass>();

            RefreshList();
        }

        void list_ItemActivate(object sender, EventArgs e)
        {
            if(list.SelectedItems.Count != 1) return;

            picture.Image = ((DataClass)list.SelectedItems[0].Tag).Picture;
        }

        private void load_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Pictures|*.png;*.jpg;*.bmp";
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var img = Image.FromFile(ofd.FileName);

                    picture.Image = img;
                    name.Text = Path.GetFileNameWithoutExtension(ofd.FileName);

                    m_current = new DataClass()
                    {
                        Name = name.Text,
                        Picture = img
                    };
                }
            }
        }

        private void store_Click(object sender, EventArgs e)
        {
            if (m_current == null) return;

            m_store.Insert(m_current);

            m_current = null;

            RefreshList();
            
        }

        private void RefreshList()
        {
            images.Images.Clear();
            list.Items.Clear();
            
            var index = 0;

            list.BeginUpdate();

            foreach (var i in m_store.Select<DataClass>())
            {
                var lvi = new ListViewItem(i.Name, index++);
                lvi.Tag = i;
                images.Images.Add(i.Picture);
                list.Items.Add(lvi);
            }

            list.EndUpdate();
        }
    }
}
