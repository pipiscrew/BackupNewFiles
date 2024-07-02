using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GetNewFiles
{
    public partial class Form1 : Form
    {
        private StringBuilder entries;

        public Form1()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;

            //load user entered values
            string filename = Application.StartupPath + "\\settings.bin";

            if (File.Exists(filename))
            {
                string[] i = File.ReadAllLines(filename);

                textBox1.Text = i[0];
                dtp.Value = DateTime.Parse(i[1]);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //   BurnAware - https://www.burnaware.com/ | https://www.videohelp.com/software/BurnAware/old-versions
        //   Disc Span introduced on v8 - https://www.videohelp.com/software/BurnAware/version-history
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        private void button1_Click(object sender, EventArgs e)
        {
            //https://stackoverflow.com/a/6197967 - is culture-sensitive.
            // Console.WriteLine(DateTime.Now.ToShortDateString());// sysFormat);

            try
            {
                if (!Directory.Exists(textBox1.Text))
                    throw new Exception("Please enter a valid folder!");

                Cursor = System.Windows.Forms.Cursors.WaitCursor;

                entries = new StringBuilder();

                entries.Append("<compilation name=\"1006_" + DateTime.Now.ToString("MMddyyyyHHmmss") + "\">");

                ScanFolder4Burnaware(textBox1.Text, dtp.Value.Date);

                entries.Append("</compilation>");

                string filename=Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bafl";
                File.WriteAllText(filename, entries.ToString(), Encoding.UTF8);
                System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filename));
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message + "\n\nOperation aborted!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Cursor = System.Windows.Forms.Cursors.Default;
        }

        private void ScanFolder4Burnaware(string folderPath, DateTime targetDate)
        {
            string[] files = Directory.GetFiles(folderPath);
            string[] subDirectories = Directory.GetDirectories(folderPath);

            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime >= targetDate)
                {
                    FileInfo f = new FileInfo(file);

                    entries.Append(string.Format("<file name=\"{0}\" date=\"{1}\" parameter=\"\" priority=\"0\" hidden=\"0\" size=\"{2}\" source=\"{3}\"/>", Path.GetFileName(file), f.LastWriteTime, f.Length, file));
                }
            }

            foreach (string subDir in subDirectories)
            {
                int startIndex = entries.Length;
                string currentFolder = string.Format("<dir name=\"{0}\" date=\"{1}\" parameter=\"\" priority=\"0\" hidden=\"0\" size=\"0\">", Path.GetFileName(subDir), new DirectoryInfo(subDir).LastWriteTime);
                entries.Append(currentFolder);

                int currentIndex = entries.Length;

                //
                ScanFolder4Burnaware(subDir, targetDate);

                if (entries.Length == currentIndex)
                {
                    entries.Remove(startIndex, currentFolder.Length);
                }
                else
                    entries.Append("</dir>");
            }
        }



        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //   InfraRecorder - https://www.fosshub.com/InfraRecorder.html | http://infrarecorder.org/
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(textBox1.Text))
                    throw new Exception("Please enter a valid folder!");

                Cursor = System.Windows.Forms.Cursors.WaitCursor;

                ScanFolder4InfraRecorder(textBox1.Text, dtp.Value.Date);
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message + "\n\nOperation aborted!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Cursor = System.Windows.Forms.Cursors.Default;
        }

        private void ScanFolder4InfraRecorder(string folderPath, DateTime targetDate)
        {
            string template = "<File{0} flags=\"0\"><InternalName>{1}</InternalName> <FullPath>{2}</FullPath><FileTime>{3}</FileTime><FileSize>{4}</FileSize></File{0}>";
            string filename = DateTime.Now.ToString("yyyyMMddHHmmss") + "_{0}.irp";
            try
            {
                int discNo = 1;
                long userDiskSize = 0;
                long diskSizeCounter = 0;
                switch (comboBox1.SelectedIndex)
                {
                    case 0: //DVD
                        userDiskSize = 4700000000;
                        break;
                    case 1: //DVD-DL
                        userDiskSize = 8500000000;
                        break;
                    case 2: //BL
                        userDiskSize = 25000000000;
                        break;
                    case 3: //BL-DL
                        userDiskSize = 50000000000;
                        break;
                    case 4: //BL-XL TL
                        userDiskSize = 100000000000;
                        break;
                    case 5: //BL-XL QL
                        userDiskSize = 128000000000;
                        break;
                    default:
                        break;
                }

                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).Where(file => File.GetLastWriteTime(file) >= targetDate);

                int i = 0;
                FileInfo fileInfo;
                string infra;

                foreach (var file in files)
                {
                    if ((diskSizeCounter + file.Length) > userDiskSize)
                    {
                        //save disc
                        entries.Append("</Data></Project></InfraRecorder>");
                        File.WriteAllText(Application.StartupPath + "\\" + string.Format(filename, discNo.ToString()), entries.ToString(), new UnicodeEncoding(false, true));

                        diskSizeCounter = 0; i = 0; discNo++;
                    }

                    if (diskSizeCounter == 0)
                    {
                        //go new!
                        entries = new StringBuilder();
                        entries.Append("<?xml version=\"1.0\" encoding=\"utf-16\" standalone=\"yes\"?><InfraRecorder><Project version=\"3\" type=\"0\" dvd=\"1\"><Label>C</Label><FileSystem><Identifier>3</Identifier></FileSystem><ISO><Level>2</Level><Format>0</Format><DeepDirs>1</DeepDirs><Joliet enable=\"1\"><LongNames>1</LongNames></Joliet>			<OmitVerNum>0</OmitVerNum>		</ISO>		<Fields>			<Files>			</Files>		</Fields>		<Boot>		</Boot>		<Data>");
                    }

                    fileInfo = new FileInfo(file);
                    infra = file.Replace(textBox1.Text, "").Replace(@"\", "/");
                    entries.AppendLine(string.Format(template, i.ToString(), infra, file, fileInfo.LastWriteTime.ToFileTime(), fileInfo.Length));

                    diskSizeCounter += fileInfo.Length;

                    i++;
                }

                // the last one - save disc
                entries.Append("</Data></Project></InfraRecorder>");
                File.WriteAllText(Application.StartupPath + "\\" + string.Format(filename, discNo.ToString()), entries.ToString(), new UnicodeEncoding(false, true));

                System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", Application.StartupPath + "\\" + string.Format(filename, "1")));
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message + "\n\nOperation aborted!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //   CDBurnerXP - https://www.fosshub.com/CDBurnerXP.html | https://cdburnerxp.se/
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(textBox1.Text))
                    throw new Exception("Please enter a valid folder!");

                Cursor = System.Windows.Forms.Cursors.WaitCursor;

                entries = new StringBuilder();

                entries.Append("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?> <!DOCTYPE layout PUBLIC \"http://www.cdburnerxp.se/help/data.dtd\" \"\"> <?xml-stylesheet type='text/xsl' href='http://www.cdburnerxp.se/help/compilation.xsl'?> <!--data compilation created by CDBurnerXP 4.5.8.7128 (http://cdburnerxp.se)--> <layout type=\"Data\" version=\"4.5.8.7128\" date=\"2024-07-02 20:01:54\">   <options>     <BootableEnabled>0</BootableEnabled>     <FileSystemType>0</FileSystemType>     <UdfVersion>100</UdfVersion>   </options>   <VolumeInfo>     <ApplicationID>CDBurnerXP</ApplicationID>     <PublisherID>     </PublisherID>   </VolumeInfo>   <compilation name=\"Disc\">     <dir name=\"Disc\" path=\"\\\" realpath=\"\">");

                ScanFolder4CDBurnerXP(textBox1.Text, dtp.Value.Date);

                entries.Append("</dir></compilation></layout>");

                string filename = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".dxp";
                File.WriteAllText(filename, entries.ToString(), new UTF8Encoding(true));
                System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filename));
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message + "\n\nOperation aborted!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Cursor = System.Windows.Forms.Cursors.Default;
        }

        private void ScanFolder4CDBurnerXP(string folderPath, DateTime targetDate)
        {
            string[] files = Directory.GetFiles(folderPath);
            string[] subDirectories = Directory.GetDirectories(folderPath);

            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime >= targetDate)
                {
                    FileInfo f = new FileInfo(file);

                    entries.Append(string.Format("<file name=\"{0}\" path=\"{1}\" hidden=\"1\" date=\"{2}\" />", Path.GetFileName(file), file, f.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")));
                }
            }

            foreach (string subDir in subDirectories)
            {
                int startIndex = entries.Length;
                string currentFolder = string.Format("<dir name=\"{0}\" path=\"{1}\" realpath=\"{2}\" hidden=\"0\" date=\"{3}\">", Path.GetFileName(subDir), subDir.Replace(textBox1.Text, ""), subDir, new DirectoryInfo(subDir).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                entries.Append(currentFolder);
                int currentIndex = entries.Length;

                //
                ScanFolder4CDBurnerXP(subDir, targetDate);

                if (entries.Length == currentIndex)
                {
                    entries.Remove(startIndex, currentFolder.Length);
                }
                else
                    entries.Append("</dir>");
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //   rar a test.rar @filelist.txt | 7z a test.zip @filelist.txt | https://superuser.com/a/641698
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(textBox1.Text))
                    throw new Exception("Please enter a valid folder!");

                Cursor = System.Windows.Forms.Cursors.WaitCursor;

                var files = Directory.GetFiles(textBox1.Text, "*.*", SearchOption.AllDirectories).Where(file => File.GetLastWriteTime(file) >= dtp.Value.Date);

                string filename = Application.StartupPath + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
                File.WriteAllLines(filename, files, Encoding.UTF8);
                System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filename));

            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message + "\n\nOperation aborted!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Cursor = System.Windows.Forms.Cursors.Default;
        }


        #region TextBox DragDrop

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            string[] FileList = (string[])e.Data.GetData(DataFormats.FileDrop, false); if (Directory.Exists(FileList[0])) textBox1.Text = FileList[0];
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; else e.Effect = DragDropEffects.None;
        }

        #endregion

        //save user entered values
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            File.WriteAllText(Application.StartupPath + "\\settings.bin", textBox1.Text + "\r\n" + dtp.Value);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //   alternatively dont use any application | xcopy c:\gold h:\backup_gold /s/e/d | http://answers.google.com/answers/threadview/id/95707.html
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
