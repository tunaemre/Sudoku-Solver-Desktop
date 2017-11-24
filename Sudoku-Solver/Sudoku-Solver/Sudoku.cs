using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Sudoku_Solver
{
    //By Tuna Emre

    public partial class Sudoku : Form
    {
        class CellAddr
        {
            public int row { get; set; }
            public int column { get; set; }
        }

        class ButtonUpdateEventArgs : EventArgs
        {
            public Button button { get; set; }
            public string text { get; set; }
            public bool? enable { get; set; }
        }

        class MatrixChangedEventArgs : EventArgs
        {
            public TextBox textBox { get; set; }
            public string value { get; set; }
        }

        class ProgressChangedEventArgs : EventArgs
        {
            public ProgressBar progressBar { get; set; }
            public int progress{ get; set; }
        }

        static TextBox[,] formMatrix = new TextBox[9,9];

        static int[,] originalMatrix, currentMatrix;

        static int originalBlankCellCount, iterateCount;

        static IEnumerable<int> values;

        static Random rand = new Random();

        static int currentProgress
        {
            get
            {
                int blankCount = 0;
                for (int row = 0; row < 9; row++)
                {
                    for (int column = 0; column < 9; column++)
                    {
                        if (currentMatrix[row, column] == -1)
                            blankCount++;
                    }
                }

                return originalBlankCellCount - blankCount;
            }
        }

        public Sudoku()
        {
            InitializeComponent();

            for (int row = 0; row < 9; row++)
            {
                for (int column = 0; column < 9; column++)
                {
                    TextBox cell = new TextBox();
                    cell.Location = new System.Drawing.Point(13 + column * 20 + column * 6, 13 + row * 20 + row * 6);
                    cell.Name = "cell-" + row + "-" + column;
                    cell.Size = new System.Drawing.Size(20, 20);
                    cell.TabIndex = 0;
                    cell.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
                    cell.MaxLength = 1;
                    cell.TextChanged += Cell_TextChanged;

                    this.Controls.Add(cell);

                    formMatrix[row,column] = cell;
                }
            }

            values = Enumerable.Range(1, 9);

            //fillTestMatrix();
        }

        private void Cell_TextChanged(object sender, EventArgs e)
        {
            TextBox cell = (TextBox)sender;
            if (cell.TextLength == 1)
                SelectNextControl(cell, true, false, true, true);
        }

        //void fillTestMatrix()
        //{
        //    formMatrix[0, 1].Text = "1";
        //    formMatrix[0, 5].Text = "2";
        //    formMatrix[0, 6].Text = "3";

        //    formMatrix[1, 1].Text = "4";
        //    formMatrix[1, 3].Text = "5";
        //    formMatrix[1, 4].Text = "1";
        //    formMatrix[1, 7].Text = "6";
        //    formMatrix[1, 8].Text = "7";

        //    formMatrix[2, 5].Text = "8";
        //    formMatrix[2, 7].Text = "2";

        //    formMatrix[3, 0].Text = "9";
        //    formMatrix[3, 2].Text = "7";
        //    formMatrix[3, 6].Text = "1";

        //    formMatrix[4, 1].Text = "6";
        //    formMatrix[4, 4].Text = "5";
        //    formMatrix[4, 7].Text = "7";

        //    formMatrix[5, 2].Text = "3";
        //    formMatrix[5, 6].Text = "2";
        //    formMatrix[5, 8].Text = "6";

        //    formMatrix[6, 1].Text = "3";
        //    formMatrix[6, 3].Text = "9";

        //    formMatrix[7, 0].Text = "5";
        //    formMatrix[7, 1].Text = "2";
        //    formMatrix[7, 4].Text = "4";
        //    formMatrix[7, 5].Text = "1";
        //    formMatrix[7, 7].Text = "9";

        //    formMatrix[8, 2].Text = "6";
        //    formMatrix[8, 3].Text = "3";
        //    formMatrix[8, 7].Text = "8";
        //}

        private void buttonSolve_Click(object sender, EventArgs e)
        {
            originalMatrix = new int[9, 9];

            originalBlankCellCount = 0;

            for (int row = 0; row < 9; row++)
            {
                for (int column = 0; column < 9; column++)
                {
                    TextBox cell = formMatrix[row, column];

                    int value = -1;
                    if (string.IsNullOrWhiteSpace(cell.Text))
                    {
                        originalMatrix[row, column] = value;
                        originalBlankCellCount++;
                        continue;
                    }

                    if (Int32.TryParse(cell.Text, out value) && value > 0 && value < 10)
                        originalMatrix[row, column] = value;
                    else
                    {
                        onError(cell.Text);
                        return;
                    }
                }
            }
            
            currentMatrix = (int[,]) originalMatrix.Clone();

            progressBar.Maximum = originalBlankCellCount;

            iterateCount = 0;

            Thread mainThread = new Thread(new ThreadStart(start))
            {
                IsBackground = true
            };

            mainThread.Start();
        }

        public void start()
        {
            formButtonUpdater(this, new ButtonUpdateEventArgs()
            {
                button = buttonSolve,
                enable = false,
                text = "Solving..."
            });

            if (iterate())
            {
                formButtonUpdater(this, new ButtonUpdateEventArgs()
                {
                    button = buttonSolve,
                    enable = false,
                    text = "Solved"
                });
            }
            else
            {
                onError("Cannot solve!");
            }
            
        }

        bool iterate()
        {
            iterateCount++;

            CellAddr cellAddr = getNextCell();
            if (cellAddr == null)
                return true;

            List<int> availables = getAvailables(cellAddr);

            foreach (int available in availables)
            {
                currentMatrix[cellAddr.row, cellAddr.column] = available;
                Debug.Write("\n" + cellAddr.row + "," + cellAddr.column + ":" + available);

                formMatrixUpdater(this, new MatrixChangedEventArgs()
                {
                    textBox = formMatrix[cellAddr.row, cellAddr.column],
                    value = available.ToString()
                });

                progressBarUpdater(this, new ProgressChangedEventArgs()
                {
                    progressBar = progressBar,
                    progress = currentProgress
                });

                if (iterate())
                    return true;

                currentMatrix[cellAddr.row, cellAddr.column] = -1;

                formMatrixUpdater(this, new MatrixChangedEventArgs()
                {
                    textBox = formMatrix[cellAddr.row, cellAddr.column],
                    value = string.Empty
                });

                progressBarUpdater(this, new ProgressChangedEventArgs()
                {
                    progressBar = progressBar,
                    progress = currentProgress
                });
            }

            return false;
        }

        static CellAddr getNextCell()
        {
            for (int row = 0; row < 9; row++)
            {
                for (int column = 0; column < 9; column++)
                {
                    if (currentMatrix[row, column] == -1)
                        return new CellAddr()
                        {
                            row = row,
                            column = column
                        };
                }
            }

            return null;
        }

        static List<int> getAvailables(CellAddr cellAddr)
        {
            List<int> available = values.ToList();

            for (int i = 0; i < 9; i++)
            {
                if (currentMatrix[i, cellAddr.column] != -1)
                    available.Remove(currentMatrix[i, cellAddr.column]);

                if (currentMatrix[cellAddr.row, i] != -1)
                    available.Remove(currentMatrix[cellAddr.row, i]);
            }

            int rowStart = (cellAddr.row / 3) * 3;
            int columnStart = (cellAddr.column / 3) * 3;

            for (int i = rowStart; i < (rowStart + 3) ; i++)
            {
                for (int j = columnStart; j < (columnStart + 3); j++)
                {
                    if (currentMatrix[i, j] != -1)
                        available.Remove(currentMatrix[i, j]);
                }
            }

            return available;
        }

        private void onError(string value)
        {
            MessageBox.Show("Invalid cell value: " + value, "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        void formButtonUpdater(object sender, ButtonUpdateEventArgs e)
        {
            MethodInvoker methodInvokerDelegate = delegate ()
            {
                if (e.text != null)
                    e.button.Text = e.text;
                if (e.enable.HasValue)
                    e.button.Enabled = e.enable.Value;
            };

            if (e.button.InvokeRequired)
                e.button.Invoke(methodInvokerDelegate);
            else
                methodInvokerDelegate();
        }

        void formMatrixUpdater(object sender, MatrixChangedEventArgs e)
        {
            MethodInvoker methodInvokerDelegate = delegate ()
            {
                e.textBox.Text = e.value;
            };

            if (e.textBox.InvokeRequired)
                e.textBox.Invoke(methodInvokerDelegate);
            else
                methodInvokerDelegate();
        }

        void progressBarUpdater(object sender, ProgressChangedEventArgs e)
        {
            MethodInvoker methodInvokerDelegate = delegate ()
            {
                e.progressBar.Value = e.progress;
            };

            if (e.progressBar.InvokeRequired)
                e.progressBar.Invoke(methodInvokerDelegate);
            else
                methodInvokerDelegate();
        }
    }
}
