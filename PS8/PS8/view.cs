using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PS8
{
    public partial class view : Form
    {
        public view()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String textToShowP1 = "Welcome to Boggle!\n";
            String textToShowP2 = "To get started, insert the domain you would like to connect to as well as user name in their respected boxes.\n";
            String textToShowP3 = "When succesfully registered, enter the ammount of time you would like to play for.\n";
            String textToShowP4 = "Standard boggle rules apply when playing the game.";
            MessageBox.Show(textToShowP1 + textToShowP2 + textToShowP3 + textToShowP4);
        }
    }
}
