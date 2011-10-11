namespace EntityGenerator.Views
{
    partial class SelectEntitiesView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.entityTree = new System.Windows.Forms.TreeView();
            this.selectEntities = new System.Windows.Forms.Button();
            this.unselectEntities = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // entityTree
            // 
            this.entityTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.entityTree.Location = new System.Drawing.Point(3, 62);
            this.entityTree.Name = "entityTree";
            this.entityTree.Size = new System.Drawing.Size(728, 368);
            this.entityTree.TabIndex = 1;
            // 
            // selectEntities
            // 
            this.selectEntities.Location = new System.Drawing.Point(3, 31);
            this.selectEntities.Name = "selectEntities";
            this.selectEntities.Size = new System.Drawing.Size(129, 25);
            this.selectEntities.TabIndex = 2;
            this.selectEntities.Text = "Select All Entities";
            this.selectEntities.UseVisualStyleBackColor = true;
            this.selectEntities.Click += new System.EventHandler(this.selectEntities_Click);
            // 
            // unselectEntities
            // 
            this.unselectEntities.Location = new System.Drawing.Point(138, 31);
            this.unselectEntities.Name = "unselectEntities";
            this.unselectEntities.Size = new System.Drawing.Size(129, 25);
            this.unselectEntities.TabIndex = 3;
            this.unselectEntities.Text = "Un-Select All Entities";
            this.unselectEntities.UseVisualStyleBackColor = true;
            this.unselectEntities.Click += new System.EventHandler(this.unselectEntities_Click);
            // 
            // SelectEntitiesView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.unselectEntities);
            this.Controls.Add(this.selectEntities);
            this.Controls.Add(this.entityTree);
            this.Name = "SelectEntitiesView";
            this.Controls.SetChildIndex(this.entityTree, 0);
            this.Controls.SetChildIndex(this.selectEntities, 0);
            this.Controls.SetChildIndex(this.unselectEntities, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView entityTree;
        private System.Windows.Forms.Button selectEntities;
        private System.Windows.Forms.Button unselectEntities;
    }
}
