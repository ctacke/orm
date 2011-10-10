namespace EntityGenerator.Views
{
    partial class GenerateCodeView
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.outputFolder = new System.Windows.Forms.TextBox();
            this.entityNamespace = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.language = new System.Windows.Forms.ComboBox();
            this.entityModifier = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.optionsGroup = new System.Windows.Forms.GroupBox();
            this.optionsGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(13, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(676, 63);
            this.label1.TabIndex = 2;
            this.label1.Text = "Viewing entities to be generated in Entity Generator is not currently supported. " +
                " In a future version, you will be able to see what the expected output from code" +
                " gen will be on this view";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(32, 55);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(165, 23);
            this.label2.TabIndex = 3;
            this.label2.Text = "Output Folder:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // outputFolder
            // 
            this.outputFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputFolder.Location = new System.Drawing.Point(203, 55);
            this.outputFolder.Name = "outputFolder";
            this.outputFolder.Size = new System.Drawing.Size(295, 22);
            this.outputFolder.TabIndex = 4;
            // 
            // entityNamespace
            // 
            this.entityNamespace.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.entityNamespace.Location = new System.Drawing.Point(203, 83);
            this.entityNamespace.Name = "entityNamespace";
            this.entityNamespace.Size = new System.Drawing.Size(295, 22);
            this.entityNamespace.TabIndex = 6;
            this.entityNamespace.Text = "OpenNETCF.ORM";
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(32, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(165, 23);
            this.label3.TabIndex = 5;
            this.label3.Text = "Entity Namespace:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(32, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(165, 23);
            this.label4.TabIndex = 7;
            this.label4.Text = "Output Language:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // language
            // 
            this.language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.language.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.language.FormattingEnabled = true;
            this.language.Location = new System.Drawing.Point(203, 25);
            this.language.Name = "language";
            this.language.Size = new System.Drawing.Size(295, 24);
            this.language.TabIndex = 8;
            // 
            // entityModifier
            // 
            this.entityModifier.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.entityModifier.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.entityModifier.FormattingEnabled = true;
            this.entityModifier.Location = new System.Drawing.Point(203, 111);
            this.entityModifier.Name = "entityModifier";
            this.entityModifier.Size = new System.Drawing.Size(295, 24);
            this.entityModifier.TabIndex = 10;
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(32, 111);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(165, 23);
            this.label5.TabIndex = 9;
            this.label5.Text = "Entity Modifier:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // optionsGroup
            // 
            this.optionsGroup.Controls.Add(this.language);
            this.optionsGroup.Controls.Add(this.entityModifier);
            this.optionsGroup.Controls.Add(this.label2);
            this.optionsGroup.Controls.Add(this.label5);
            this.optionsGroup.Controls.Add(this.outputFolder);
            this.optionsGroup.Controls.Add(this.label3);
            this.optionsGroup.Controls.Add(this.label4);
            this.optionsGroup.Controls.Add(this.entityNamespace);
            this.optionsGroup.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.optionsGroup.Location = new System.Drawing.Point(16, 130);
            this.optionsGroup.Name = "optionsGroup";
            this.optionsGroup.Size = new System.Drawing.Size(504, 145);
            this.optionsGroup.TabIndex = 11;
            this.optionsGroup.TabStop = false;
            this.optionsGroup.Text = "Code Options";
            // 
            // GenerateCodeView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.optionsGroup);
            this.Controls.Add(this.label1);
            this.Name = "GenerateCodeView";
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.optionsGroup, 0);
            this.optionsGroup.ResumeLayout(false);
            this.optionsGroup.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox outputFolder;
        private System.Windows.Forms.TextBox entityNamespace;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox language;
        private System.Windows.Forms.ComboBox entityModifier;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox optionsGroup;
    }
}
