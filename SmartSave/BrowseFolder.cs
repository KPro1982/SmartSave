using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SmartSave
{
   public partial class Form1 : Form
        {
            public Form1()
            {
                InitializeComponent();
            }
        
            private void Form1_Load(object sender, EventArgs e)
            {
                //
                // This event handler was created by double-clicking the window in the designer.
                // It runs on the program's startup routine.
                //
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    //
                    // The user selected a folder and pressed the OK button.
                    // We print the number of files found.
                    //
                    string[] files = Directory.GetFiles(folderBrowserDialog1.SelectedPath);
                    MessageBox.Show("Files found: " + files.Length.ToString(), "Message");
                }
            }
        }
    
}