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
    public partial class BoggleView : Form, IBoggleClient
    {
        private int localGameTimeLimit;

        public int gameTimeLimit
        {
            get
            {
                return localGameTimeLimit;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int playTimeLimit
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string userToken
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public event Action CancelJoinRequestEvent;
        public event Action CloseGameEvent;
        public event Action<string> CreateUserEvent;
        public event Action<string> GameStatusEvent;
        public event Action JoinGameEvent;
        public event Action<string> PlayWordEvent;
    }
}
