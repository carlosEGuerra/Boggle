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
    public partial class BoggleViewWindow : Form
    {
        /// <summary>
        /// Creates the view
        /// </summary>
        public BoggleViewWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// If state == true, enables all controls that are normally enabled; disables Cancel.
        /// If state == false, disables all controls; enables Cancel.
        /// </summary>
        public void EnableControls(bool state)
        {
            registerButton.Enabled = state;
            taskButton.Enabled = state && UserRegistered && taskBox.Text.Length > 0;
            allTaskButton.Enabled = state && UserRegistered;
            showCompletedTasksButton.Enabled = state && UserRegistered;

            foreach (Control control in taskPanel.Controls)
            {
                if (control is Button)
                {
                    control.Enabled = state && UserRegistered;
                }
            }
            cancelButton.Enabled = !state;
        }

        /*
                public event Action CancelJoinRequestEvent;
                public event Action CloseGameEvent;
                public event Action<string> CreateUserEvent;
                public event Action<string> GameStatusEvent;
                public event Action JoinGameEvent;
                public event Action<string> PlayWordEvent;

            */

    }
}
