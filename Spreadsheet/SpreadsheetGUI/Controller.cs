using System;
using SS;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SSGui;
using Formulas;
using System.Text.RegularExpressions;

namespace SpreadsheetGUI
{
    /// <summary>
    /// Controls the operation of an ISpreadsheedView.
    /// </summary>
    public class Controller
    {
        // The window being controlled
        private ISpreadsheetView window;

        // The model being used
        private Spreadsheet model;

        /// <summary>
        /// Begins controlling window.
        /// </summary>
        public Controller(ISpreadsheetView window)
        {
            this.window = window;
            this.model = new Spreadsheet();
            window.FileChosenEvent += HandleFileChosen;
            window.CloseEvent += HandleClose;
            window.NewEvent += HandleNew;
            window.UpdateCell += HandleCell;
            window.CellClicked += HandleCellChanged;
            window.SP.SelectionChanged += HandleCellChanged;
            window.HelpEvent += DisplayHelp;
            window.SaveEvent += SaveFile;
            window.CheckChanged += CheckIfChanged;

        }

        /// <summary>
        /// When we edit the contents of the cell.
        /// </summary>
        private void HandleCellChanged(SpreadsheetPanel ss)
        {
            int row, col;
            String value;
            string val2;
            ss.GetSelection(out col, out row);
            ss.GetValue(col, row, out value);
            window.CellName = this.LocationToCellName(row, col);
            ss.GetValue(col, row, out val2);
            window.Value = val2;
            string val;
            ss.GetValue(col, row, out val);
            if (val == null)
            {
                window.Value = "";
            }

            if (ss.GetValue(col, row, out value))
            {
                try
                {
                    object ssContents = model.GetCellContents(LocationToCellName(row, col));
                    string stringCont = ssContents.ToString(); 
                    if(ssContents is Formula)
                    {
                        stringCont = "=" + stringCont;
                    }
                    window.Content = stringCont;
                }
                catch
                {
                    window.Content = "";//If tbe cell is empty, the content box should also be empty.
                                        
                }
            }
        }
        /// <summary>
        /// The primary method that updates the cell if the value in the text box is updated.
        /// </summary>
        private void HandleCell()
        {
            int row, col;
            string value = window.Value;
            window.SP.GetSelection(out col, out row);
            window.SP.GetValue(col, row, out value);

                      
            //Convert row value to a cell name.
            string cellName = this.LocationToCellName(row, col);
            string content = window.Content; //Pull the current cell contents from the interface.

            HashSet<string> list = new HashSet<string>();
            try
            {
                
                //Modify the model cell contents.
                list = (HashSet<string>)(model.SetContentsOfCell(cellName, content)); //WATCH THIS. MIGHT CAUSE PROBLEMS.
            
            }
            catch (Exception e)
            {
                //CircularExecption
                if (e is CircularException)
                {
                    window.Value = "0";
                    window.Message = "Circular Exception detected";
               
                }
                //FormulaFormatError
                else if (e is FormulaFormatException)
                {
                    window.Message = "Sorry, but that formula is in the incorrect format!";
                    window.Value = "0";

                }
                //FormulaEvaluationException
                else if (e is FormulaEvaluationException)
                {
                    window.Message = "Formula Evaluation Exception Error detected with new formula";
                    window.Value = "0";
                }
            }

            finally
            {
                string contents;
                object o = model.GetCellContents(cellName);
                contents = o.ToString();
                
                if (o is Formula)
                {
                    contents = "=" + contents;

                }
                window.Content = contents;

                object val = model.GetCellValue(cellName);
                if (val is FormulaError)
                {
                    window.Value = "FORMULAERROR";
                }
             
                else
                {
                    window.Value = "";
                    window.Value = val.ToString();
                    window.Content = model.GetCellContents(cellName).ToString();
                }

                if(list.Count > 1)//If we need to update other cells in the table...
                {
                    AddAll(list);
                }
                    

            }
        }

        /// <summary>
        /// Handles a request to open a file.
        /// </summary>
        private void HandleFileChosen(String filename)
        {
            try
            {
                TextReader file = new StringReader(filename);
                LoadFile(file);
            }
            catch (Exception ex)
            {
                window.Message = "Unable to open file\n" + ex.Message;
            }
        }

        /// <summary>
        /// Handles a request to close the window
        /// </summary>
        private void HandleClose()
        {
            window.DoClose();
        }

        /// <summary>
        /// Handles a request to open a new window.
        /// </summary>
        private void HandleNew()
        {
            window.OpenNew();
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
            return colName + (row+1).ToString();
        }

        /// <summary>
        /// Gives back a column location
        /// </summary>
        /// <param name="cellName"></param>
        /// <returns></returns>
        private void CellNameToLocation(string cellName, out int row, out int column)
        {
            column = cellName[0] - 65;//CELLS START INDEXING AT ZERO.
            string stringrow = cellName.Substring(1);
            int.TryParse(stringrow, out row);
        }

        /// <summary>
        /// Whenever the contents of a cell are changed, the values are stored to be updated to the GUI.
        /// </summary>
        private void HandleCellChanged()
        {
            int col;
            int row;
            //Get the current cell
            window.SP.GetSelection(out col, out row);

            window.Content = "";//If tbe cell is empty, the content box should also be empty.

            string val;
            if (window.SP.GetValue(col, row, out val))
            {
                    window.Content = "";//If tbe cell is empty, the content box should also be empty.
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleOpen()
        {
            //LoadFile will need a file to create a spreadsheet out of.
          //  LoadFile();
        }

        /// <summary>
        /// Helper method from adding items from the spreadsheet to the GUI.
        /// </summary>
        /// <param name="list"></param>
        private void AddAll(IEnumerable<string> list)
        {
            list = new List<string>(list);

            foreach (string cell in list)
            {
                int r, c;
                CellNameToLocation(cell, out r, out c);
                window.SP.SetValue(c, r - 1, model.GetCellValue(cell).ToString()); //WATCH THIS. MIGHT CAUSE ISSUES.
            }

        }

        /// <summary>
        ///  A method that converts the contents of an xml file to the spreadsheet.
        /// </summary>
        /// <param name="filename"></param>
        private void LoadFile(TextReader reader)
        {
            
            model = new Spreadsheet(reader, new Regex(""));
            List<string> list = new List<string>(model.GetNamesOfAllNonemptyCells());

            //Convert each non-empty cell of the spreadsheet to the SpreadsheetGUI
            AddAll(list);
        }

        /// <summary>
        /// Saves file to a destination
        /// </summary>
        /// <param name="T"></param>
        private void SaveFile(TextWriter T)
        {
            if (model.Changed)
            {
                try
                {
                    model.Save(T);
                }
                catch
                {
                    window.Message = "Unable to save file.";
                }
            }
        }

        /// <summary>
        /// A help message to explain how to use the SpreadsheetGUI.
        /// </summary>
        private void DisplayHelp()
        {
            window.HelpMessage =
            "Welcome the the Ghetto Spreadsheet! \n " +
            "Sorry in advance. \n" +
            "\n\n" +
            "To Edit Cell Contents: \n" +
            "Click on the cell you would like to change." +
            "Edit its contents in the \"Contents\" box at the top of the page." +
            "\n\n" +
            "To close the program: File > Close \n" +
            "To open an existing spreadsheet: File > Open \n" +
            "To create a new Spreadsheet: File > New \n" +
            "To save a file: File > Save\n" +
            "To save a file to a different name: File > SaveAs \n" +
            "**We encourage you to save all files with the extension .ss**"

            ;

        }

        /// <summary>
        /// Lets us know if a file has changed since we last saved or created it.
        /// </summary>
        private void CheckIfChanged()
        {
            if (model.Changed)
            {
                window.Changed = true;
            }

            else
                window.Changed = false;
        }


    }
}
