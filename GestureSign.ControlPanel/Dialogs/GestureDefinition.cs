﻿using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// GestureDefinition.xaml 的交互逻辑
    /// </summary>
    public partial class GestureDefinition : MetroWindow
    {
        public GestureDefinition()
        {
            InitializeComponent();
            MessageProcessor.GotNewGesture += MessageProcessor_GotNewGesture;
        }

        public GestureDefinition(IGesture gesture)
            : this()
        {
            _currentPointPatterns = gesture.PointPatterns;
            GestureManager.Instance.GestureName = _oldGestureName = gesture.Name;
        }

        private string _oldGestureName;
        private string _similarGestureName;
        private PointPattern[] _currentPointPatterns;
        private Color _color;

        public static event EventHandler GesturesChanged;

        public string GestureName { get; set; }

        public string SimilarGestureName
        {
            get { return _similarGestureName; }
            set
            {
                _similarGestureName = value;
                if (string.IsNullOrEmpty(_similarGestureName))
                {
                    cmdDone.Content = LocalizationProvider.Instance.GetTextValue("Common.Save");
                    ExistingGestureImage.Visibility = ExistingTextBlock.Visibility = Visibility.Collapsed;
                }
                else
                {
                    cmdDone.Content = LocalizationProvider.Instance.GetTextValue("Common.Overwrite");
                    var gesture = GestureManager.Instance.GetNewestGestureSample(_similarGestureName);
                    if (gesture != null)
                        ExistingGestureImage.Source = GestureImage.CreateImage(gesture.PointPatterns, new Size(65, 65), _color);

                    ExistingGestureImage.Visibility = ExistingTextBlock.Visibility = Visibility.Visible;
                    txtGestureName.Text = _similarGestureName;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _color = (Color)Application.Current.Resources["HighlightColor"];

            if (_currentPointPatterns != null)
                imgGestureThumbnail.Source = GestureImage.CreateImage(_currentPointPatterns, new Size(65, 65), _color);

            if (_oldGestureName == null)
            {
                NamedPipe.SendMessageAsync("StartTeaching", "GestureSignDaemon");
                Title = LocalizationProvider.Instance.GetTextValue("GestureDefinition.Title");
            }
            else
            {
                Title = LocalizationProvider.Instance.GetTextValue("GestureDefinition.Rename");
                txtGestureName.Text = _oldGestureName; //this.txtGestureName.Text

                DrawGestureTextBlock.Visibility = ResetButton.Visibility = StackUpGestureButton.Visibility = Visibility.Collapsed;

                txtGestureName.Focus();
                txtGestureName.SelectAll();
            }
        }


        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            //Non-renaming mode
            if (_oldGestureName == null)
                await NamedPipe.SendMessageAsync("StopTraining", "GestureSignDaemon");
        }


        private void cmdDone_Click(object sender, RoutedEventArgs e)
        {
            if (SaveGesture())
            {
                DialogResult = true;
                Close();
            }
            else txtGestureName.Focus();
        }

        private void cmdCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtGestureName_TextChanged(object sender, TextChangedEventArgs e)
        {
            string newGestureName = txtGestureName.Text.Trim();
            if (_oldGestureName == newGestureName || SimilarGestureName == newGestureName) return;

            txtGestureName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private async void MessageProcessor_GotNewGesture(object sender, Gesture e)
        {
            SimilarGestureName = e.Name;
            _currentPointPatterns = e.PointPatterns;
            imgGestureThumbnail.Source = GestureImage.CreateImage(_currentPointPatterns, new Size(65, 65), _color);

            await SetTrainingState(false);
            StackUpGestureButton.IsEnabled = _currentPointPatterns.Length < 3;
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            await SetTrainingState(true);

            SimilarGestureName = null;
            _currentPointPatterns = null;
            imgGestureThumbnail.Source = null;

            txtGestureName.Clear();
        }

        private async void StackUpGestureButton_Click(object sender, RoutedEventArgs e)
        {
            await NamedPipe.SendMessageAsync("StackUpGesture", "GestureSignDaemon");
            await SetTrainingState(true);
        }

        #region Private Methods

        private async Task<bool> SetTrainingState(bool state)
        {
            if (state)
            {
                DrawGestureTextBlock.Visibility = Visibility.Visible;
                StackUpGestureButton.IsEnabled = ResetButton.IsEnabled = false;
                return await NamedPipe.SendMessageAsync("StartTeaching", "GestureSignDaemon");
            }
            else
            {
                DrawGestureTextBlock.Visibility = Visibility.Collapsed;
                ResetButton.IsEnabled = true;
                return await NamedPipe.SendMessageAsync("StopTraining", "GestureSignDaemon");
            }
        }

        private bool SaveGesture()
        {
            string newGestureName = txtGestureName.Text.Trim();

            if (string.IsNullOrEmpty(newGestureName))
            {
                txtGestureName.Clear();
                return false;
            }

            if (_currentPointPatterns == null || _currentPointPatterns.Length == 0)
                return false;

            //Rename mode
            if (_oldGestureName != null)
            {
                if (_oldGestureName.Equals(newGestureName)) return true;
                if (GestureManager.Instance.GestureExists(newGestureName))
                {
                    txtGestureName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                    return false;
                }
                GestureManager.Instance.RenameGesture(_oldGestureName, newGestureName);
            }
            else
            {
                if (String.IsNullOrEmpty(SimilarGestureName))
                {
                    if (GestureManager.Instance.GestureExists(newGestureName))
                    {
                        txtGestureName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                        return false;
                    }
                    // Add new gesture to gesture manager
                    GestureManager.Instance.AddGesture(new Gesture(newGestureName, _currentPointPatterns));
                }
                else
                {
                    if (Array.Exists(GestureManager.Instance.Gestures, g => g.Name == newGestureName && g.Name != SimilarGestureName))
                    {
                        txtGestureName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                        return false;
                    }

                    if (SimilarGestureName != newGestureName)
                        GestureManager.Instance.RenameGesture(SimilarGestureName, newGestureName);

                    GestureManager.Instance.DeleteGesture(newGestureName);
                    // Add new gesture to gesture manager
                    GestureManager.Instance.AddGesture(new Gesture(newGestureName, _currentPointPatterns));
                }
            }
            GestureManager.Instance.GestureName = newGestureName;
            GestureManager.Instance.SaveGestures();

            GesturesChanged?.Invoke(this, new EventArgs());
            return true;
        }

        #endregion
    }
}
