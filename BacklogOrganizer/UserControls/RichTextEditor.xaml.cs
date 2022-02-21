using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using BacklogOrganizer.Utilities;

using DataFormats = System.Windows.Forms.DataFormats;


namespace BacklogOrganizer.UserControls
{
    public partial class RichTextEditor
    {
        private readonly List<FontFamily> myFontFamilies = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();

        public RichTextEditor()
        {
            InitializeComponent();
            FontFamilyComboBox.ItemsSource = myFontFamilies;
            FontSizeComboBox.ItemsSource = new List<double> { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
        }

        private void RichTextEditor_OnLoaded(object sender, RoutedEventArgs e)
        {
            FontFamilyComboBox.SelectedIndex = myFontFamilies.FindIndex(x => x.ToString().Equals("Segoe UI"));
            FontSizeComboBox.SelectedIndex = 4;
        }

        public void DisableControls()
        {
            FontToolBar.IsEnabled = false;
            RickTextBox.IsReadOnly = true;
        }

        public void SetTextFromHtml(string html)
        {
            var rtfText = MarkupConverter.HtmlToRtfConverter.ConvertHtmlToRtf(html);

            using (var rtfMemoryStream = new MemoryStream())
            {
                using (var rtfStreamWriter = new StreamWriter(rtfMemoryStream))
                {
                    rtfStreamWriter.Write(rtfText);
                    rtfStreamWriter.Flush();
                    rtfMemoryStream.Seek(0, SeekOrigin.Begin);

                    var document = RickTextBox.Document;
                    var range = new TextRange(document.ContentStart, document.ContentEnd);
                    range.Load(rtfMemoryStream, DataFormats.Rtf);

                    foreach (var block in document.Blocks)
                    {
                        block.FontFamily = new FontFamily("Segoe UI");
                        block.FontSize = 12;
                    }
                }
            }
        }

        public string ReadValue()
        {
            var document = RickTextBox.Document;
            var range = new TextRange(document.ContentStart, document.ContentEnd);

            string rtfText;
            using (var rtfMemoryStream = new MemoryStream())
            {
                range.Save(rtfMemoryStream, DataFormats.Rtf);
                rtfMemoryStream.Seek(0, SeekOrigin.Begin);
                using (var rtfStreamReader = new StreamReader(rtfMemoryStream))
                {
                    rtfText = rtfStreamReader.ReadToEnd();
                }
            }

            return MarkupConverter.RtfToHtmlConverter.ConvertRtfToHtml(rtfText);
        }

        private void RtbEditor_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                var fontFamily = RickTextBox.Selection.GetPropertyValue(TextElement.FontFamilyProperty);
                FontFamilyComboBox.SelectedItem = fontFamily;

                var fontWeight = RickTextBox.Selection.GetPropertyValue(TextElement.FontWeightProperty);
                BoldButton.IsChecked = fontWeight != DependencyProperty.UnsetValue && fontWeight.Equals(FontWeights.Bold);

                var fontSize = RickTextBox.Selection.GetPropertyValue(TextElement.FontSizeProperty);
                FontSizeComboBox.Text = fontSize.ToString();

                var italic = RickTextBox.Selection.GetPropertyValue(TextElement.FontStyleProperty);
                ItalicButton.IsChecked = italic != DependencyProperty.UnsetValue && italic.Equals(FontStyles.Italic);

                var underline = RickTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
                UnderlineButton.IsChecked = underline != DependencyProperty.UnsetValue && underline.Equals(TextDecorations.Underline);
            }
            catch (Exception ex)
            {
                Logger.Error(null, ex);
            }
        }

        private void CmbFontFamily_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (FontFamilyComboBox.SelectedItem != null)
                    RickTextBox.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, FontFamilyComboBox.SelectedItem);
            }
            catch (Exception ex)
            {
                Logger.Error(null, ex);
            }
        }

        private void CmbFontSize_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                RickTextBox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, FontSizeComboBox.Text);
            }
            catch (Exception ex)
            {
                Logger.Error(null, ex);
            }
        }
    }
}
