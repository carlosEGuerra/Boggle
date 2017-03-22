using SSGui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpreadsheetGUI
{
    public partial class SpreadsheetWindow : Form, ISpreadsheetView
    {


        //Constructs the window. 
        public SpreadsheetWindow()
        {
            InitializeComponent();
        }

        public event Action CloseEvent;
        public event Action<string> FileChosenEvent;
        public event Action NewEvent;
        public event Action UpdateCell;
        public event Action CellClicked;
        public event Action HelpEvent;
        public event Action CheckChanged;
        public event Action<TextWriter> SaveEvent;

        private string _value;
        private bool _changed;

        public bool Changed
        {
            get
            {
                return _changed;
            }
            set
            {
                _changed = value;
            }
        }



        public string Message
        {
            set
            {
                MessageBox.Show(value.ToString(), "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string HelpMessage
        {
            set
            {
                MessageBox.Show(value.ToString(), "Welcome!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public string Title
        {
            set
            {
                cellNameReadOnly.Text = value.ToString();
            }
        }

        public string Content
        {
            get
            {
                return ContentBox.Text;
            }

            set
            {
                ContentBox.Text = value;
            }
        }

        public string Value
        {
            get
            {

                return _value;
            }

            set
            {
                int row;
                int col;
                _value = value;
                cellValReadOnly.Text = value;
                spreadsheetPanel1.GetSelection(out col, out row);
                spreadsheetPanel1.SetValue(col, row, value);
            }
        }

        public SpreadsheetPanel SP
        {
            get
            {
                return spreadsheetPanel1;
            }

            set
            {

            }
        }

        public string CellName
        {
            get
            {
                return cellNameReadOnly.Text;
            }

            set
            {
                cellNameReadOnly.Text = value;
            }
        }

        /// <summary>
        /// Method that closes the current open window
        /// </summary>
        public void DoClose()
        {
            //closes the window
            if (CloseEvent != null)
            {
                Close();
            }
        }

        /// <summary>
        /// Opens a new blank spreadsheet
        /// </summary>
        public void OpenNew()
        {
            //runs a new and empty spreadsheet
            SpreadsheetApplicationContext.GetContext().RunNew();
        }


        private void spreadsheetPanel1_DoubleClick(object sender, EventArgs e)
        {

        }

        //When we click a spreadsheet panel, it should show the cell, value, and content
        private void spreadsheetPanel1_MouseClick(object sender, MouseEventArgs e)
        {
            CellClicked();
            ContentBox.Text = this.Content;
        }

        //If user presses "enter" in the editable contents box, fires the event to update all cell fields.
        private void ContentBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return || e.KeyChar == (char)Keys.Enter)
            {
                //Grab the contents the user typed.
                string OriginalContent = ContentBox.Text;
                if (OriginalContent[0] == '=')
                {
                    OriginalContent = OriginalContent.ToUpper();
                }

                Content = ContentBox.Text;
                ContentBox.Text = OriginalContent;

                UpdateCell();
                int col, row;
                spreadsheetPanel1.GetSelection(out col, out row);
                spreadsheetPanel1.SetValue(col, row, Value);
                cellValReadOnly.Text = Value;


            }
        }

        private void SpreadsheetWindow_Load(object sender, EventArgs e)
        {

        }

        private void spreadsheetPanel1_Load(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// When the file-close is chosen, it will close the current spreadsheet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(Changed)
            {
                DialogResult RESULT = MessageBox.Show("Would you like to save your file?", "Unsaved Changes", MessageBoxButtons.YesNo);
                if(RESULT == DialogResult.Yes)
                {
                    saveToolStripMenuItem_Click(sender, e);
                }
                
            }
            this.DoClose();
        }

        /// <summary>
        /// When the file-open new is chosen then a new blank spreadsheet is opened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.OpenNew();
        }
        /// <summary>
        /// Take in the row and column of the value to convert it to a cell name
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private string LocationTolCellName(int row, int col)
        {
            char colName = (char)(col + 65); //CELLS START INDEXING AT 0,0.
            return colName + (row + 1).ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellName"></param>
        /// <returns></returns>
        private int CellNameToLocation(string cellName)
        {
            int colLocation = (cellName[0] - 65);//CELLS START INDEXING AT ZERO.
            return colLocation;
        }

        /// <summary>
        /// When open is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog result = new OpenFileDialog();
            if (result.ShowDialog() == DialogResult.OK)
            {
                //Read in file.
                FileChosenEvent(result.FileName);

            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HelpEvent();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();

            if (save.ShowDialog() == DialogResult.OK)
            {
                /* if (!save.CheckFileExists)
                 {
                     SaveAsEvent();
                     return;
                 }
                 */
                string sfn = save.FileName;
                TextWriter writer = File.CreateText(sfn);
                SaveEvent(writer);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveToolStripMenuItem_Click(sender, e);
        }
    }
}
