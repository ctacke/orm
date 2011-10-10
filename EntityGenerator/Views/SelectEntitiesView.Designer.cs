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
            this.SuspendLayout();
            // 
            // entityTree
            // 
            this.entityTree.Location = new System.Drawing.Point(3, 30);
            this.entityTree.Name = "entityTree";
            this.entityTree.Size = new System.Drawing.Size(728, 397);
            this.entityTree.TabIndex = 1;
            // 
            // SelectEntitiesView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.entityTree);
            this.Name = "SelectEntitiesView";
            this.Controls.SetChildIndex(this.entityTree, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView entityTree;
    }
}
