using SSGui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
            spreadsheetPanel1.SelectionChanged += displaySelection;
        }

        public event Action CloseEvent;
        public event Action<string> FileChosenEvent;
        public event Action NewEvent;
        public event Action UpdateCell;
        public event Action CellClicked;
        public event Action HelpEvent;
        private string _value;

        /// <summary>
        /// When we edit the contents of the cell.
        /// </summary>
        private void displaySelection(SpreadsheetPanel ss)
        {
            int row, col;
            String value;
            ss.GetSelection(out col, out row);
            ss.GetValue(col, row, out value);
            cellNameReadOnly.Text = this.LocationToCellName(row, col);
            string s;
            ss.GetValue(col, row, out s);
            if(s == null)
            {
                cellValReadOnly.Text = "";
            }
        }

        public string Message
        {
            set
            {
                MessageBox.Show(value.ToString(), "ERROR", MessageBoxButtons.OK ,MessageBoxIcon.Error);
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
                return cellValReadOnly.Text;
            }

            set
            {
                cellValReadOnly.Text = value;
            }
        }

        public string Value
        {
            get
            {
                
                return "work";
            }

            set
            {
                int row;
                int col;
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
            if(CloseEvent!= null)
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

        //When we double click on the spreadsheet panel, it should fire the UpdateCell event.
        private void spreadsheetPanel1_MouseClick(object sender, MouseEventArgs e)
        {
            //UpdateCell();
        }

        //If user presses "enter" in the editable contents box, fires the event to update all cell fields.
        private void ContentBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return || e.KeyChar == (char)Keys.Enter)
            {
                int col;
                int row;
                spreadsheetPanel1.GetSelection(out col, out row);
                Content = ContentBox.Text;
                spreadsheetPanel1.GetSelection(out col, out row);
                spreadsheetPanel1.SetValue(col, row, this.Value);
            }
        }

        private void SpreadsheetWindow_Load(object sender, EventArgs e)
        {
            
        }

        private void cellNameReadOnly_TextChanged(object sender, EventArgs e)
        {
            cellNameReadOnly.Text = "fucking work";
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
        private string LocationToCellName(int row, int col)
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

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }
    }
}
