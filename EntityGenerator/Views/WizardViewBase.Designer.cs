namespace EntityGenerator.Views
{
    partial class WizardViewBase
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
            this.caption = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // caption
            // 
            this.caption.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.caption.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.caption.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.caption.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.caption.Location = new System.Drawing.Point(0, 0);
            this.caption.Name = "caption";
            this.caption.Size = new System.Drawing.Size(734, 27);
            this.caption.TabIndex = 0;
            this.caption.Text = "Title of View";
            this.caption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // WizardViewBase
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.caption);
            this.Name = "WizardViewBase";
            this.Size = new System.Drawing.Size(734, 430);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label caption;
    }
}
