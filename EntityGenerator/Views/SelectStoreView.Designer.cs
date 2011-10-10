namespace EntityGenerator.Views
{
    partial class SelectStoreView
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
            this.selectText = new System.Windows.Forms.Label();
            this.sourceType = new System.Windows.Forms.ComboBox();
            this.sourceTypeText = new System.Windows.Forms.Label();
            this.dataSourceList = new System.Windows.Forms.ComboBox();
            this.browse = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // selectText
            // 
            this.selectText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectText.Location = new System.Drawing.Point(3, 97);
            this.selectText.Name = "selectText";
            this.selectText.Size = new System.Drawing.Size(234, 23);
            this.selectText.TabIndex = 1;
            this.selectText.Text = "Select the Data Source for the Entities";
            this.selectText.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // sourceType
            // 
            this.sourceType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sourceType.FormattingEnabled = true;
            this.sourceType.Location = new System.Drawing.Point(243, 54);
            this.sourceType.Name = "sourceType";
            this.sourceType.Size = new System.Drawing.Size(241, 21);
            this.sourceType.TabIndex = 2;
            // 
            // sourceTypeText
            // 
            this.sourceTypeText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sourceTypeText.Location = new System.Drawing.Point(3, 54);
            this.sourceTypeText.Name = "sourceTypeText";
            this.sourceTypeText.Size = new System.Drawing.Size(234, 21);
            this.sourceTypeText.TabIndex = 3;
            this.sourceTypeText.Text = "Data Source Type:";
            this.sourceTypeText.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dataSourceList
            // 
            this.dataSourceList.FormattingEnabled = true;
            this.dataSourceList.Location = new System.Drawing.Point(243, 99);
            this.dataSourceList.Name = "dataSourceList";
            this.dataSourceList.Size = new System.Drawing.Size(418, 21);
            this.dataSourceList.TabIndex = 4;
            // 
            // browse
            // 
            this.browse.Location = new System.Drawing.Point(667, 97);
            this.browse.Name = "browse";
            this.browse.Size = new System.Drawing.Size(64, 23);
            this.browse.TabIndex = 5;
            this.browse.Text = "...";
            this.browse.UseVisualStyleBackColor = true;
            this.browse.Click += new System.EventHandler(this.browse_Click);
            // 
            // SelectStoreView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.browse);
            this.Controls.Add(this.dataSourceList);
            this.Controls.Add(this.sourceTypeText);
            this.Controls.Add(this.sourceType);
            this.Controls.Add(this.selectText);
            this.Name = "SelectStoreView";
            this.Controls.SetChildIndex(this.selectText, 0);
            this.Controls.SetChildIndex(this.sourceType, 0);
            this.Controls.SetChildIndex(this.sourceTypeText, 0);
            this.Controls.SetChildIndex(this.dataSourceList, 0);
            this.Controls.SetChildIndex(this.browse, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label selectText;
        private System.Windows.Forms.ComboBox sourceType;
        private System.Windows.Forms.Label sourceTypeText;
        private System.Windows.Forms.ComboBox dataSourceList;
        private System.Windows.Forms.Button browse;
    }
}
