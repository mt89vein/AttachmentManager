using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.Forms.MessageBox;

namespace AttachmentManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string DefaultAttachmentsFolder = "D:/Attachments";
        private const string DefaultArchveFolder = "D:/ZipResult";
        private static readonly Regex ValidDataRegex = new Regex($@"[^0-9,{Environment.NewLine}\t\r]+");
        private static readonly Regex ValidList = new Regex($@"[^0-9,{Environment.NewLine}\s]");

        public MainWindow()
        {
            InitializeComponent();
            AttachmentPath.Text = DefaultAttachmentsFolder;
            ArchivePath.Text = DefaultArchveFolder;
        }

        /// <summary>
        /// Запрет на ввод по Regex
        /// </summary>
        private void AttachmentIdsTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = ValidDataRegex.IsMatch(e.Text);
        }

        private void MakeArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var attachmentIds = ParseAttachmentIds(AttachmentIdsTextBox.Text);
                var filePaths = attachmentIds.Select(BuildRelativePath);

                if (!filePaths.Any())
                {
                    MessageBox.Show(@"Нет файлов для архивирования");
                    return;
                }
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var filePath in filePaths)
                        {
                            var absolutePath = Path.Combine(AttachmentPath.Text, filePath);
                            if (!File.Exists(absolutePath))
                            {
                                MessageBox.Show($@"Не найден файл {absolutePath}");
                                return;
                            }
                            archive.CreateEntryFromFile(absolutePath, filePath);
                        }
                    }

                    using (var fileStream = new FileStream($"{ArchivePath.Text}/{DateTime.Now.ToFileTime()}.zip",
                        FileMode.Create))
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        memoryStream.CopyTo(fileStream);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show($@"Во время обработки возникла ошибка: {exception.Message}");
            }
        }

        /// <summary>
        /// Парсит введеный текст и формирует список идентификаторов в строковом формате
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Список идентификаторов в строковом формате</returns>
        private static IEnumerable<string> ParseAttachmentIds(string text)
        {
            return ValidList
                .Replace(text, string.Empty)
                .Replace(Environment.NewLine, ",")
                .Replace(" ", ",")
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Формирует относительный путь к файлу на основе идентификатора
        /// </summary>
        /// <param name="arg">Идентификатор в строковом формате</param>
        /// <returns>Относительный путь к файлу</returns>
        private string BuildRelativePath(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg) || !int.TryParse(arg, out var id))
            {
                throw new ArgumentNullException(nameof(arg), @"Идентификатор не может быть преобразован в тип Integer");
            }

            var hex = id.ToString("X8");

            var path =
                Path.Combine(
                    hex.Substring(0, 2),
                    hex.Substring(2, 2),
                    hex.Substring(4, 2),
                    hex + ".stream"
                );
            return path;
        }

        private void AttachmentDirectoryChooseBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = !string.IsNullOrWhiteSpace(AttachmentPath.Text)
                    ? AttachmentPath.Text
                    : DefaultAttachmentsFolder;

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    AttachmentPath.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void ArchiveResultDirectoryChooseBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = !string.IsNullOrWhiteSpace(AttachmentPath.Text)
                    ? AttachmentPath.Text
                    : DefaultArchveFolder;

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ArchivePath.Text = folderDialog.SelectedPath;
                }
            }
        }
    }
}