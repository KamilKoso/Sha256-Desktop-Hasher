using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Projekt_algorytm_hashujący_Sha_256__Kamil_Kosobudzki__Michał_Świerkot;

namespace Sha256_Hasher
{
    public partial class Form1 : Form
    {
        
        string hashResult;
        
 
        public Form1()
        {
            InitializeComponent();
            hashing.Visible = false;
          
            
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            hashTextbox.Text = "";
            DialogResult result = openFileDialog1.ShowDialog();
            if(result==DialogResult.OK)
            {
                //Wyłączenie przycisków
                button1.Enabled = false;
                hashStringTexBox.Enabled = false;
                hashing.Visible = true;

                fileNameLabel.Text = openFileDialog1.FileName;
                progressBar1.Maximum = 1000; // Aby zrobić progress bar płynny


                FileStream fs = await Task.Run(()=>File.OpenRead(openFileDialog1.FileName));
                


                hashResult =  await Task<string>.Run(() => Sha256.ByteArrayToString(Sha256.HashFile(fs,
                    (pr)=> 
                    {
                       if(this.InvokeRequired)
                    this.BeginInvoke(new Action(() => { progressBar1.Value = (int)pr; })); //Synchronizacja wątku  
                    })));
                hashTextbox.Text = hashResult ?? null;
                hashTextbox.SelectionStart = hashTextbox.Text.Length;


                //Włączenie przycików
                button1.Enabled = true;
                hashStringTexBox.Enabled = true;
                hashing.Visible = false;
            }
        }

        private async void hashStringTexBox_TextChanged(object sender, EventArgs e)
        {
            
            string toBeHashed = hashStringTexBox.Text;
            hashResult = await Task<string>.Run(() => Sha256.ByteArrayToString(Sha256.HashString(toBeHashed)));
            hashTextbox.Text = hashResult;
            hashTextbox.SelectionStart = hashTextbox.Text.Length;
        }
    }
    
}
