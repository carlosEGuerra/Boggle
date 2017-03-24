using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoggleClient
{
    public partial class BoggleView : Form
    {
        public event Action<string, string> RegisterPressed;

        public dynamic setUserIDBox { get; set;}

        public BoggleView()
        {
            InitializeComponent();
        }

        public void EnableControls(bool state)
        {
            RegisterButton.Enabled = state;

        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            if(RegisterPressed != null)
            {
                RegisterPressed(userNameBox.Text.Trim(), domainBox.Text.Trim());
            }
        }
    }
}
