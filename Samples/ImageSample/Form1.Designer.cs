namespace ImageSample
{
    partial class Form1
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.name = new System.Windows.Forms.TextBox();
            this.load = new System.Windows.Forms.Button();
            this.picture = new System.Windows.Forms.PictureBox();
            this.store = new System.Windows.Forms.Button();
            this.list = new System.Windows.Forms.ListView();
            this.images = new System.Windows.Forms.ImageList(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.picture)).BeginInit();
            this.SuspendLayout();
            // 
            // name
            // 
            this.name.Location = new System.Drawing.Point(42, 157);
            this.name.Name = "name";
            this.name.Size = new System.Drawing.Size(293, 20);
            this.name.TabIndex = 0;
            // 
            // load
            // 
            this.load.Location = new System.Drawing.Point(179, 64);
            this.load.Name = "load";
            this.load.Size = new System.Drawing.Size(75, 48);
            this.load.TabIndex = 1;
            this.load.Text = "load";
            this.load.UseVisualStyleBackColor = true;
            this.load.Click += new System.EventHandler(this.load_Click);
            // 
            // picture
            // 
            this.picture.Location = new System.Drawing.Point(42, 28);
            this.picture.Name = "picture";
            this.picture.Size = new System.Drawing.Size(118, 112);
            this.picture.TabIndex = 2;
            this.picture.TabStop = false;
            // 
            // store
            // 
            this.store.Location = new System.Drawing.Point(260, 64);
            this.store.Name = "store";
            this.store.Size = new System.Drawing.Size(75, 48);
            this.store.TabIndex = 3;
            this.store.Text = "store";
            this.store.UseVisualStyleBackColor = true;
            this.store.Click += new System.EventHandler(this.store_Click);
            // 
            // list
            // 
            this.list.LargeImageList = this.images;
            this.list.Location = new System.Drawing.Point(42, 213);
            this.list.Name = "list";
            this.list.Size = new System.Drawing.Size(402, 184);
            this.list.TabIndex = 4;
            this.list.UseCompatibleStateImageBehavior = false;
            this.list.View = System.Windows.Forms.View.Tile;
            // 
            // images
            // 
            this.images.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.images.ImageSize = new System.Drawing.Size(16, 16);
            this.images.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(456, 409);
            this.Controls.Add(this.list);
            this.Controls.Add(this.store);
            this.Controls.Add(this.picture);
            this.Controls.Add(this.load);
            this.Controls.Add(this.name);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.picture)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox name;
        private System.Windows.Forms.Button load;
        private System.Windows.Forms.PictureBox picture;
        private System.Windows.Forms.Button store;
        private System.Windows.Forms.ListView list;
        private System.Windows.Forms.ImageList images;
    }
}

