using ClosedXML.Excel;
using System.Text.RegularExpressions;


namespace ExcelMergerCleanerWinForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFolderPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string folderPath = txtFolderPath.Text;

            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                MessageBox.Show("Please select a valid folder path.");
                return;
            }

            try
            {
                string outputPath = Path.Combine(folderPath, "Merged_Cleaned_Output.xlsx");

                var masterWorkbook = new XLWorkbook();
                var masterSheet = masterWorkbook.Worksheets.Add("MergedData");

                // Headers
                masterSheet.Cell(1, 1).Value = "First Name";
                masterSheet.Cell(1, 2).Value = "Last Name";
                masterSheet.Cell(1, 3).Value = "Number";
                masterSheet.Cell(1, 4).Value = "Address";
                masterSheet.Cell(1, 5).Value = "Email Address";

                int masterRow = 2;

                foreach (var file in Directory.GetFiles(folderPath, "*.xlsx"))
                {
                    using var workbook = new XLWorkbook(file);

                    foreach (var sheet in workbook.Worksheets)
                    {
                        var firstRowUsed = sheet.FirstRowUsed();
                        if (firstRowUsed == null) continue;

                        int lastRow = sheet.LastRowUsed().RowNumber();

                        for (int r = firstRowUsed.RowNumber() + 1; r <= lastRow; r++) // Skip header
                        {
                            var row = sheet.Row(r);

                            string firstName = "", lastName = "", number = "", address = "", email = "";
                            List<string> extras = new List<string>();

                            foreach (var cell in row.CellsUsed())
                            {
                                string value = cell.GetString().Trim();
                                if (string.IsNullOrWhiteSpace(value)) continue;

                                if (Regex.IsMatch(value, @"^\+?\d{7,15}$"))
                                {
                                    number = value;
                                }
                                else if (Regex.IsMatch(value, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b"))
                                {
                                    email = value;
                                }
                                else if (value.Split(' ').Length == 2 && string.IsNullOrEmpty(firstName))
                                {
                                    var parts = value.Split(' ');
                                    firstName = parts[0];
                                    lastName = parts[1];
                                }
                                else if (string.IsNullOrEmpty(firstName)) firstName = value;
                                else if (string.IsNullOrEmpty(lastName)) lastName = value;
                                else if (string.IsNullOrEmpty(address)) address = value;
                                else extras.Add(value);
                            }

                            masterSheet.Cell(masterRow, 1).Value = firstName;
                            masterSheet.Cell(masterRow, 2).Value = lastName;
                            masterSheet.Cell(masterRow, 3).Value = number;
                            masterSheet.Cell(masterRow, 4).Value = address;
                            masterSheet.Cell(masterRow, 5).Value = email;

                            for (int i = 0; i < extras.Count; i++)
                            {
                                masterSheet.Cell(masterRow, 6 + i).Value = extras[i];
                            }

                            masterRow++;
                        }
                    }
                }

                masterWorkbook.SaveAs(outputPath);
                lblStatus.Text = $"✅ Done! Merged file saved at:\n{outputPath}";
                MessageBox.Show("✅ Merging and cleaning complete!", "Success");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "❌ Error: " + ex.Message;
                MessageBox.Show("Error: " + ex.Message, "Error");
            }
        }

    }
}
