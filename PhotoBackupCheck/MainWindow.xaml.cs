using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace PhotoBackupCheck
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        FormSettings settings = new FormSettings();
        public string backupPath;
        public string sourcePath;

        private void SourceButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = "C:\\Users";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                sourcePath = dialog.FileName;
                SourcePathText.Text = dialog.FileName;
                settings.SourcePath = sourcePath;
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = "C:\\Users";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                backupPath = dialog.FileName;
                BackupPathText.Text = dialog.FileName;
                settings.BackupPath = backupPath;
            }
        }

        private void processButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(backupPath)) return;

            var allbackupFiles = Directory.GetFiles(backupPath, "*", SearchOption.AllDirectories);
            var allData = new Dictionary<string, List<FileInfo>>();
            for (var index = 0; index < allbackupFiles.Length; index++)
            {
                progress1.Value = 10 * (index / allbackupFiles.Length);
                var file = allbackupFiles[index];
                FileInfo info = new FileInfo(file);
                GetOrCreateDictionaryItem(allData, info.Name).Add(info);
            }


            var allSourceFile = Directory.GetFiles(sourcePath);

            for (var index = 0; index < allSourceFile.Length; index++)
            {
                var file = allSourceFile[index];
                progress1.Value = 10 + 90 * (index / allbackupFiles.Length);
                FileInfo info = new FileInfo(file);
                List<FileInfo> result;
                if (allData.TryGetValue(info.Name, out result))
                {
                    if (result.Count == 1)
                    {
                        info.MoveTo(sourcePath + "\\Processed\\" + info.Name);
                    }
                    else
                    {
                        if (result.Count == 2)
                        {
                            foreach (var fileInfo in result)
                            {
                                if (fileInfo.DirectoryName.Contains("Bests"))
                                {
                                    info.MoveTo(sourcePath + "\\Processed\\" + info.Name);
                                    break;
                                }
                            }
                        }
                        else
                        {
                           // should log something to notify duplicates ?
                        }
                    }
                }
                else
                {
                    // file isnt backed up 
                    if (info.Extension == ".nar" || info.Extension == ".dng")
                    {
                        if (allData.TryGetValue(info.Name.Replace(info.Extension, ".jpg"), out result))
                        {
                            if (result.Count == 1)
                            {
                                info.CopyTo(result[0].DirectoryName + "\\" + info.Name);
                                info.MoveTo(sourcePath + "\\Processed\\" + info.Name);
                            }
                            else
                            {
                                if (result.Count == 2)
                                {
                                    // we move the file to the normal backup folder, not the "Bests" one
                                    var r0HasBests = result[0].DirectoryName.Contains("Bests");
                                    var r1HasBests = result[1].DirectoryName.Contains("Bests");
                                    if (r0HasBests && !r1HasBests)
                                    {
                                        info.CopyTo(result[1].DirectoryName + "\\" + info.Name);
                                        info.MoveTo(sourcePath + "\\Processed\\" + info.Name);
                                    }
                                    else if (!r0HasBests && r1HasBests)
                                    {
                                        info.CopyTo(result[0].DirectoryName + "\\" + info.Name);
                                        info.MoveTo(sourcePath + "\\Processed\\" + info.Name);
                                    }
                                }
                                else
                                {
                                    // should log something to notify duplicates ?
                                }
                            }
                        }
                    }
                }
            }
        }

        List<FileInfo> GetOrCreateDictionaryItem(Dictionary<string, List<FileInfo>> dict, string key)
        {
            List<FileInfo> result;
            if (dict.TryGetValue(key, out result))
            {
                return result;
            }

            result = new List<FileInfo>();
            dict.Add(key, result);
            return result;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            settings.Save();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                sourcePath = (string)settings.SourcePath;
                SourcePathText.Text = sourcePath;
            }

            if (string.IsNullOrEmpty(backupPath))
            {
                backupPath = (string)settings.BackupPath;
                BackupPathText.Text = backupPath;
            }
            //sourcePath = (string)Properties.Settings.Default["sourcePath"];
            //SourcePathText.Text = sourcePath;

            //backupPath = (string)Properties.Settings.Default["backupPath"];
            //SourcePathText.Text = backupPath;


        }
    }



    sealed class FormSettings : ApplicationSettingsBase
    {
        [UserScopedSettingAttribute()]
        public String SourcePath
        {
            get { return (String)this["SourcePath"]; }
            set { this["SourcePath"] = value; }
        }

        [UserScopedSettingAttribute()]
        public String BackupPath
        {
            get { return (String)this["BackupPath"]; }
            set { this["BackupPath"] = value; }
        }
    }
}
