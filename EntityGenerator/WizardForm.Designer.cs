namespace EntityGenerator
{
    partial class WizardForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;
        private OpenNETCF.IoC.UI.DeckWorkspace wizardWorkspace;
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.wizardWorkspace = new OpenNETCF.IoC.UI.DeckWorkspace();
            this.next = new System.Windows.Forms.Button();
            this.back = new System.Windows.Forms.Button();
            this.exit = new System.Windows.Forms.Button();
            this.restart = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // wizardWorkspace
            // 
            this.wizardWorkspace.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.wizardWorkspace.Location = new System.Drawing.Point(3, 3);
            this.wizardWorkspace.Name = "wizardWorkspace";
            this.wizardWorkspace.Size = new System.Drawing.Size(737, 379);
            this.wizardWorkspace.TabIndex = 0;
            // 
            // next
            // 
            this.next.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.next.Location = new System.Drawing.Point(659, 388);
            this.next.Name = "next";
            this.next.Size = new System.Drawing.Size(81, 38);
            this.next.TabIndex = 1;
            this.next.Text = "Next";
            // 
            // back
            // 
            this.back.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.back.Location = new System.Drawing.Point(3, 388);
            this.back.Name = "back";
            this.back.Size = new System.Drawing.Size(81, 38);
            this.back.TabIndex = 2;
            this.back.Text = "Back";
            // 
            // exit
            // 
            this.exit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.exit.Location = new System.Drawing.Point(572, 388);
            this.exit.Name = "exit";
            this.exit.Size = new System.Drawing.Size(81, 38);
            this.exit.TabIndex = 3;
            this.exit.Text = "Exit";
            // 
            // restart
            // 
            this.restart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.restart.Location = new System.Drawing.Point(90, 388);
            this.restart.Name = "restart";
            this.restart.Size = new System.Drawing.Size(81, 38);
            this.restart.TabIndex = 4;
            this.restart.Text = "Restart";
            // 
            // WizardForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(743, 429);
            this.Controls.Add(this.back);
            this.Controls.Add(this.restart);
            this.Controls.Add(this.exit);
            this.Controls.Add(this.next);
            this.Controls.Add(this.wizardWorkspace);
            this.Menu = this.mainMenu1;
            this.MinimizeBox = false;
            this.Name = "WizardForm";
            this.Text = "OpenNETCF ORM Entity Generator";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button next;
        private System.Windows.Forms.Button back;
        private System.Windows.Forms.Button exit;
        private System.Windows.Forms.Button restart;
    }
}

