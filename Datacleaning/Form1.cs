using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Datacleaning
{
    public partial class Form1 : Form
    {

        int fileCount;
        int totalfileCount;
        List<string> result;
        string FolderPath = string.Empty;
        bool Complet = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            if (FBD.ShowDialog() == DialogResult.OK)
            {
                FolderPath = FBD.SelectedPath;
                new Thread(() => Search(FolderPath)).Start();
            }
        }

        public void Search(string path)
        {
            string[] files = Directory.GetFiles(path);
            totalfileCount = files.Count();
            pB.Invoke((MethodInvoker)(() => pB.Maximum = totalfileCount));
            fileCount = 0;
            result = new List<string>();
            Parallel.ForEach(files, file =>
            {
                new Thread(() => SearchInFile(file)).Start();
            });
            Complet = true;
        }

        public void SearchInFile(string path)
        {
            string line = string.Empty;
            StreamReader file = new StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                if (line.Contains("CARDNUMBER") &&
                    line.Contains("TERMINALID") &&
                    line.Contains("CARDNUMBER") &&
                    line.Contains("CARDSTATUS = I") &&
                    line.Contains("MOBILENUMBER"))
                {
                    if (line.Contains("TERMINALID = 50182001") ||
                        line.Contains("TERMINALID = 50182002") ||
                        line.Contains("TERMINALID = 50182003") ||
                        line.Contains("TERMINALID = 50182004") ||
                        line.Contains("TERMINALID = 50182005"))
                    {
                        string CarPlateNo = line.Split('{')[1].Split(',')[5].Split('=')[1].Trim(' ');
                        string CardNo = line.Split('{')[1].Split(',')[7].Split('=')[1].Trim(' ');
                        string MobileNo = line.Split('{')[1].Split(',')[8].Split('=')[1].Trim(' ');
                        line = " Update SmartCardAccounts set CarPlateNo = '" + CarPlateNo + "' , MobileNo = '" + MobileNo + "' where AccountId = (select s.AccountNo from SmartCards s where s.CardNo = '" + CardNo + "')";
                        result.Add(line);
                    }
                }
            }
            addOneThreadComplete();
        }

        void addOneThreadComplete()
        {
            fileCount++;
            txtResult.Invoke((MethodInvoker)(() => txtResult.Text = "File : " + totalfileCount + " / " + fileCount));
            pB.Invoke((MethodInvoker)(() => pB.Increment(1)));

            var lineCount = 0;
            if (Complet && (totalfileCount == fileCount))
            {
                pB.Invoke((MethodInvoker)(() => pB.Maximum = result.Count));
                pB.Invoke((MethodInvoker)(() => pB.Value = 0));
                
                FileStream file;
                using (file = File.Create(FolderPath + "\\Out.sql"))
                {
                    foreach (var line in result)
                    {
                        lineCount++;
                        txtResult.Invoke((MethodInvoker)(() => txtResult.Text = "Lines : " + result.Count + " / " + lineCount));
                        pB.Invoke((MethodInvoker)(() => pB.Increment(1)));
                        var titile = new UTF8Encoding(false).GetBytes(line + Environment.NewLine);
                        file.Write(titile, 0, titile.Length);
                    }
                }
                file.Close();
                txtResult.Invoke((MethodInvoker)(() => txtResult.Text = "Done"));
                Process.Start(FolderPath + "\\Out.sql");
            }
        }
    }
}
