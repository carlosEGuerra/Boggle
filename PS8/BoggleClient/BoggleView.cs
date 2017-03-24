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
        private bool _userRegistered;

        public event Action<string, string> RegisterPressed;
        public event Action<string> JoinGamePressed;
        public event Action<string> PlayWord;

        public string setPlayer1Score
        {
            get { return Player1ScoreBox.Text.ToString(); }
            set { Player1ScoreBox.Text = value; }
        }

        public string setUserID
        {
            get { return userID.Text.ToString(); }
            set { userID.Text = value; }
        }
        public string setGameID
        {
            get { return gameID.Text.ToString(); }
            set { gameID.Text = value; }
        }

        //ALL OF THE GETTERS AND SETTERS FOR THE PANELS ON THE GAME BOARD
        public string setButton1
        {
            get { return butt1.ToString(); }
            set { butt1.Text = value; }
        }

        public string setButton2
        {
            get { return butt2.ToString(); }
            set { butt2.Text = value; }
        }

        public string setButton3
        {
            get { return butt3.ToString(); }
            set { butt3.Text = value; }
        }
        public string setButton4
        {
            get { return butt4.ToString(); }
            set { butt4.Text = value; }
        }
        public string setButton5
        {
            get { return butt5.ToString(); }
            set { butt5.Text = value; }

        }
        public string setButton6
        {
            get { return butt6.ToString(); }
            set { butt6.Text = value; }
        }
        public string setButton7
        {
            get { return butt7.ToString(); }
            set { butt7.Text = value; }
        }
        public string setButton8
        {
            get { return butt8.ToString(); }
            set { butt8.Text = value; }
        }
        public string setButton9
        {
            get { return butt9.ToString(); }
            set { butt9.Text = value; }
        }
        public string setButton10
        {
            get { return butt10.ToString(); }
            set { butt10.Text = value; }
        }
        public string setButton11
        {
            get { return butt11.ToString(); }
            set { butt11.Text = value; }
        }
        public string setButton12
        {
            get { return butt12.ToString(); }
            set { butt12.Text = value; }
        }
        public string setButton13
        {
            get { return butt13.ToString(); }
            set { butt13.Text = value; }
        }
        public string setButton14
        {
            get { return butt14.ToString(); }
            set { butt14.Text = value; }
        }
        public string setButton15
        {
            get { return butt15.ToString(); }
            set { butt15.Text = value; }
        }

        public string setButton16
        {
            get { return butt16.ToString(); }
            set { butt16.Text = value; }
        }

        public string setP1
        {
            get { return P1Box.ToString(); }
            set { P1Box.Text = value; }
        }

        public string setP2
        {
            get { return P2Box.ToString(); }
            set { P2Box.Text = value; }
        }

        public BoggleView()
        {
            InitializeComponent();
        }

        public event Action CancelPressed;

        public event Action CancelJoinRequest;

        public bool userRegistered
        {
            get { return _userRegistered; }
            set
            {
                _userRegistered = value;
            }
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

        private void joinGameButton_Click(object sender, EventArgs e)
        {

            if (!String.IsNullOrEmpty(timeDesiredBox.Text))
            {
                JoinGamePressed(timeDesiredBox.Text);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            CancelPressed();
        }

        /// <summary>
        /// If the user plays a word.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return || e.KeyChar == (char)Keys.Enter)
            {
                PlayWord(UserInput.Text);
                string newText = UserInput.Text;
                Player1Responces.Text += newText + "                              ";
                UserInput.Text = "";
            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Hello and welcome to Boggle!\n" +
                            "To start off please enter a domain you want to connect to and a username!\n" +
                            "To join a game please enter the ammount of time you would like to play for!\n" +
                            "Once in a game, use the input area under the grid to type the word and enter to send it off!\n" +
                            "Once the game has completed, the columns on both sides of the board will tell you each others points as well as the words that have been played by each player!\n" +
                            "Have fun!");
        }

        private void CancelJoinButton_Click(object sender, EventArgs e)
        {
            CancelJoinRequest();
        }
    }
}
