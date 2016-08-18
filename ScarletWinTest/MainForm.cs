using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.IO;

using Scarlet.IO;
using Scarlet.IO.ImageFormats;
using Scarlet.IO.CompressionFormats;
using Scarlet.IO.ContainerFormats;

namespace ScarletWinTest
{
    public partial class MainForm : Form
    {
        Version programVersion;

        List<FileFormatInfo> imageFormats;

        string currentFilename;
        FileFormat currentFile;

        public MainForm()
        {
            InitializeComponent();

            programVersion = new Version(Application.ProductVersion);

            imageFormats = GetFormatInfos(typeof(ImageFormat));

            SetFormTitle();
            SetOpenDialogFilters();
        }

        private void SetFormTitle()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} v{1}.{2}", Application.ProductName, programVersion.Major, programVersion.Minor);
            if (programVersion.Build != 0) builder.AppendFormat(".{0}", programVersion.Build);

            if (currentFile != null)
                builder.AppendFormat(" - [{0}]", Path.GetFileName(currentFilename));

            Text = builder.ToString();
        }

        private void SetOpenDialogFilters()
        {
            List<string> filterList = new List<string>();

            foreach (FileFormatInfo formatInfo in imageFormats.OrderBy(x => x.FormatDescription))
                filterList.Add(string.Format("{0} (*{1})|*{1}", formatInfo.FormatDescription, formatInfo.FileExtension));

            filterList.Insert(0, "All Files (*.*)|*.*");
            ofdOpenFile.Filter = string.Join("|", filterList);
        }

        public List<FileFormatInfo> GetFormatInfos(Type baseFormatType)
        {
            // TODO: very naive, only picks up formats in same assembly as base format type, file extension extraction from regex is iffy, etc...

            List<FileFormatInfo> infos = new List<FileFormatInfo>();

            foreach (Type type in Assembly.GetAssembly(baseFormatType).GetExportedTypes().Where(x => x != baseFormatType && x.BaseType == baseFormatType))
            {
                FileFormat instance = (FileFormat)Activator.CreateInstance(type);

                string description = (instance.GetFormatDescription() ?? string.Format("{0} Format", type.Name)), extension = ".*";

                var fnPatternAttrib = type.GetCustomAttributes(typeof(FilenamePatternAttribute), false).FirstOrDefault();
                if (fnPatternAttrib != null)
                {
                    string pattern = (fnPatternAttrib as FilenamePatternAttribute).Pattern;
                    extension = Path.GetExtension(pattern).Replace("$", "");
                }

                infos.Add(new FileFormatInfo(description, extension, type));
            }

            return infos;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofdOpenFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentFile = FileFormat.FromFile<FileFormat>(currentFilename = ofdOpenFile.FileName);

                if (currentFile != null && currentFile is ImageFormat)
                {
                    pbImage.Image = (currentFile as ImageFormat).GetBitmap();
                }

                SetFormTitle();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Application.ProductName);
        }
    }
}
