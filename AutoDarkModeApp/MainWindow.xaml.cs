﻿using AutoDarkMode;
using AutoDarkModeApp.Communication;
using AutoDarkModeApp.Config;
using AutoDarkModeSvc.Handlers;
using NetMQ;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Windows.System.Power;

namespace AutoDarkModeApp
{
    public partial class MainWindow
    {
        private readonly RegeditHandler regEditHandler = new RegeditHandler();
        private readonly AutoDarkModeConfigBuilder configBuilder = AutoDarkModeConfigBuilder.Instance();
        private ICommandClient CommandClient { get; }



        private readonly bool is1903 = false;

        public MainWindow()
        {
            Console.WriteLine("--------- AppStart");

            // Read json config file
            configBuilder.Load();
            CommandClient = new ZeroMQClient(Command.DefaultPort);

            LanguageHelper();
            InitializeComponent();
            if (int.Parse(regEditHandler.GetOSversion()).CompareTo(1900) > 0) is1903 = true;
            AutoThemeSwitchInit();
            InitOffset();
            UiHandler();
            ThemeChange(this, null);
            SourceChord.FluentWPF.SystemTheme.ThemeChanged += ThemeChange;
            if (Properties.Settings.Default.FirstRun)
            {
                AddJumpList();
                Properties.Settings.Default.FirstRun = false;
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Updater updater = new Updater();
            updater.CheckNewVersion();
            LanguageHelper();
            DonationScreen();
        }

        private void DonationScreen()
        {
            Random rdmnumber = new Random();
            int generatedNumber = rdmnumber.Next(1, 100);
            if (generatedNumber == 50)
            {
                MsgBox msgBox = new MsgBox(Properties.Resources.donationDescription, Properties.Resources.donationTitle, "smiley", "yesno");
                msgBox.Owner = GetWindow(this);
                msgBox.ShowDialog();
                var result = msgBox.DialogResult;
                if (result == true)
                {
                    System.Diagnostics.Process.Start("https://www.paypal.me/arminosaj");
                }
            }
        }

        private void LanguageHelper()
        {
            if (String.IsNullOrWhiteSpace(Properties.Settings.Default.Language.ToString()))
            {
                Properties.Settings.Default.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToString();
            }
            CultureInfo.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Language, true);
        }

        private void ThemeChange(object sender, EventArgs e)
        {
            if (SourceChord.FluentWPF.SystemTheme.AppTheme.Equals(SourceChord.FluentWPF.ApplicationTheme.Dark))
            {
                EdgyIcon.Source = new BitmapImage(new Uri(@"Resources\Microsoft_Edge_Logo_White.png", UriKind.RelativeOrAbsolute));
            }
            else
            {
                EdgyIcon.Source = new BitmapImage(new Uri(@"Resources\Microsoft_Edge_Logo.png", UriKind.RelativeOrAbsolute));
            }
        }

        private void AutoThemeSwitchInit()
        {
            if (configBuilder.config.Enabled)
            {
                autoCheckBox.IsChecked = true;
                if (configBuilder.config.Location.Enabled)
                {
                    DarkStartHoursBox.Text = Convert.ToString(configBuilder.config.Sunset.Hour);
                    DarkStartMinutesBox.Text = Convert.ToString(configBuilder.config.Sunset.Minute);
                    LightStartHoursBox.Text = Convert.ToString(configBuilder.config.Sunrise.Hour);
                    LightStartMinutesBox.Text = Convert.ToString(configBuilder.config.Sunrise.Minute);
                }
            }
            else
            {
                AutoCheckBox_Unchecked(this, null);
            }
        }

        private void UiHandler()
        {
            int appTheme = configBuilder.config.AppsTheme;
            Console.WriteLine("appTheme Value: " + appTheme);
            if (appTheme == 0) AppComboBox.SelectedIndex = 0;
            if (appTheme == 1) AppComboBox.SelectedIndex = 1;
            if (appTheme == 2) AppComboBox.SelectedIndex = 2;

            int systemTheme = configBuilder.config.SystemTheme;
            Console.WriteLine("SystemTheme Value: " + systemTheme);
            if (systemTheme == 0) SystemComboBox.SelectedIndex = 0;
            if (systemTheme == 1) SystemComboBox.SelectedIndex = 1;
            if (systemTheme == 2) SystemComboBox.SelectedIndex = 2;

            int edgeTheme = configBuilder.config.EdgeTheme;
            Console.WriteLine("EdgeTheme Value: " + edgeTheme);
            if (edgeTheme == 0) EdgeComboBox.SelectedIndex = 0;
            if (edgeTheme == 1) EdgeComboBox.SelectedIndex = 1;
            if (edgeTheme == 2) EdgeComboBox.SelectedIndex = 2;
            if (edgeTheme == 3) EdgeComboBox.SelectedIndex = 3;

            if (!is1903)
            {
                SystemComboBox.IsEnabled = false;
                SystemComboBox.ToolTip = Properties.Resources.cmb1903;
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.ToolTip = Properties.Resources.cmb1903;
            }
            else
            {
                AccentColorCheckBox.ToolTip = Properties.Resources.cbAccentColor;
            }

            if (configBuilder.config.AccentColorTaskbar)
            {
                AccentColorCheckBox.IsChecked = true;
            }

            ShowDeskBGStatus();
        }

        private void PopulateOffsetFields(int offsetDark, int offsetLight)
        {
            if (offsetLight < 0)
            {
                OffsetLightModeButton.Content = "-";
                OffsetLightBox.Text = Convert.ToString(-offsetLight);
            }
            else
            {
                OffsetLightBox.Text = Convert.ToString(offsetLight);
            }
            if (offsetDark < 0)
            {
                OffsetDarkModeButton.Content = "-";
                OffsetDarkBox.Text = Convert.ToString(-offsetDark);
            }
            else
            {
                OffsetDarkBox.Text = Convert.ToString(offsetDark);
            }
        }

        private void InitOffset()
        {
            PopulateOffsetFields(configBuilder.config.Location.SunsetOffsetMin, configBuilder.config.Location.SunriseOffsetMin);
        }

        private void OffsetModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Content.ToString() == "+")
                {
                    button.Content = "-";
                }
                else
                {
                    button.Content = "+";
                }
                OffsetButton.IsEnabled = true;
            }
        }

        private void OffsetButton_Click(object sender, RoutedEventArgs e)
        {
            int offsetDark;
            int offsetLight;

            //get values from TextBox
            try
            {
                offsetDark = int.Parse(OffsetDarkBox.Text);
                offsetLight = int.Parse(OffsetLightBox.Text);
            }
            catch
            {
                userFeedback.Text = Properties.Resources.errorNumberInput;
                return;
            }

            PopulateOffsetFields(offsetDark, offsetLight);

            if (OffsetLightModeButton.Content.ToString() == "+")
            {
                configBuilder.config.Location.SunriseOffsetMin = offsetLight;
            }
            else
            {
                configBuilder.config.Location.SunriseOffsetMin = -offsetLight;
            }

            if (OffsetDarkModeButton.Content.ToString() == "+")
            {
                configBuilder.config.Location.SunsetOffsetMin = offsetDark;
            }
            else
            {
                configBuilder.config.Location.SunsetOffsetMin = -offsetDark;
            }

            OffsetButton.IsEnabled = false;
            GetLocation();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            int darkStart;
            int darkStartMinutes;
            int lightStart;
            int lightStartMinutes;

            //get values from TextBox
            try
            {
                darkStart = int.Parse(DarkStartHoursBox.Text);
                darkStartMinutes = int.Parse(DarkStartMinutesBox.Text);
                lightStart = int.Parse(LightStartHoursBox.Text);
                lightStartMinutes = int.Parse(LightStartMinutesBox.Text);
            }
            catch
            {
                userFeedback.Text = Properties.Resources.errorNumberInput;
                return;
            }

            //check values from TextBox
            if (darkStart >= 24)
            {
                darkStart = 23;
                darkStartMinutes = 59;
            }
            if (lightStart >= darkStart)
            {
                lightStart = darkStart - 3;
            }
            if (lightStart < 0)
            {
                lightStart = 6;
                darkStart = 17;
            }

            if (lightStartMinutes > 59)
            {
                lightStartMinutes = 59;
            }
            if (darkStartMinutes > 59)
            {
                darkStartMinutes = 59;
            }
            DarkStartHoursBox.Text = Convert.ToString(darkStart);
            LightStartHoursBox.Text = Convert.ToString(lightStart);
            if (lightStartMinutes < 10)
            {
                LightStartMinutesBox.Text = "0" + Convert.ToString(lightStartMinutes);
            }
            else
            {
                LightStartMinutesBox.Text = Convert.ToString(lightStartMinutes);
            }
            if (darkStartMinutes < 10)
            {
                DarkStartMinutesBox.Text = "0" + Convert.ToString(darkStartMinutes);
            }
            else
            {
                DarkStartMinutesBox.Text = Convert.ToString(darkStartMinutes);
            }

            configBuilder.config.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, lightStart, lightStartMinutes, 0);
            configBuilder.config.Sunset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, darkStart, darkStartMinutes, 0);

            //todo: implement switching logic via commandclient

            applyButton.IsEnabled = false;
            if (PowerManager.EnergySaverStatus == EnergySaverStatus.On)
            {
                userFeedback.Text = Properties.Resources.msgChangesSaved + "\n\n" + Properties.Resources.msgBatterySaver;
                applyButton.IsEnabled = true;
            }
            else
            {
                userFeedback.Text = Properties.Resources.msgChangesSaved; // changes were saved!
            }

            SaveConfigInteractive();
        }

        //textbox event handler
        private void TextBox_BlockChars_TextInput(object sender, TextCompositionEventArgs e)
        {
            applyButton.IsEnabled = true;
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextBox_BlockChars_TextInput_Offset(object sender, TextCompositionEventArgs e)
        {
            OffsetButton.IsEnabled = true;
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextBox_BlockCopyPaste_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
        private void TexttBox_SelectAll_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var textBox = ((System.Windows.Controls.TextBox)sender);
            textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                textBox.SelectAll();
            }));
        }
        private void TextBox_TabNext_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (((TextBox)sender).MaxLength == ((TextBox)sender).Text.Length)
            {
                var ue = e.OriginalSource as FrameworkElement;
                e.Handled = true;
                ue.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        //open aboutWindow
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow
            {
                Owner = GetWindow(this)
            };
            aboutWindow.ShowDialog();
            // todo: this is fine, can stay like this
            if (aboutWindow.BckgrUpdateCB.IsChecked == true && Properties.Settings.Default.BackgroundUpdate == false)
            {
                TaskSchdHandler.CreateAppUpdaterTask();
                Properties.Settings.Default.BackgroundUpdate = true;
            }
            else if (aboutWindow.BckgrUpdateCB.IsChecked == false && Properties.Settings.Default.BackgroundUpdate == true)
            {
                TaskSchdHandler.RemoveAppUpdaterTask();
                Properties.Settings.Default.BackgroundUpdate = false;
            }
        }

        // set starttime based on user location
        private void LocationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // todo: info for utku, I've already partly implemented this
            // to use the command pipe infrastructure to test if the backend works.
            // this also serves as an example how to use the new command infrastructure
            // for UI operations use the Async variant to prevent UI blocking
            configBuilder.config.Location.Enabled = true;
            GetLocation();
        }
        public async void GetLocation()
        {
            SetOffsetVisibility(Visibility.Visible);
            locationBlock.Visibility = Visibility.Visible;
            locationBlock.Text = Properties.Resources.msgSearchLoc; // Searching your location...
            LocationHandler locationHandler = new LocationHandler();
            //invoking the location command will always enable location services by default

            var accessStatus = await CommandClient.SendMesssageAndGetReplyAsync(Command.Location);
            if (accessStatus != Command.NoLocAccess)
            {
                //locate user + get sunrise & sunset times
                locationBlock.Text = Properties.Resources.lblCity + ": " + await locationHandler.GetCityName();
                int[] sundate = locationHandler.CalculateSunTime(false);

                //apply settings & change UI
                LightStartHoursBox.Text = sundate[0].ToString();
                LightStartMinutesBox.Text = sundate[1].ToString();
                DarkStartHoursBox.Text = sundate[2].ToString();

                DarkStartMinutesBox.Text = sundate[3].ToString();
                LightStartHoursBox.IsEnabled = false;
                LightStartMinutesBox.IsEnabled = false;
                DarkStartHoursBox.IsEnabled = false;
                DarkStartMinutesBox.IsEnabled = false;
                applyButton.IsEnabled = false;
                ApplyButton_Click(this, null);
            }
            else
            {
                NoLocationAccess();
            }
            return;
        }
        private async void NoLocationAccess()
        {
            configBuilder.config.Location.Enabled = false;
            locationCheckBox.IsChecked = false;
            locationBlock.Text = Properties.Resources.msgLocPerm; // The App needs permission to location
            locationBlock.Visibility = Visibility.Visible;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }
        private async void LocationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            configBuilder.config.Location.Enabled = false;
            try
            {
                configBuilder.Save();
                await CommandClient.SendMessageAsync(Command.UpdateConfig);
            }
            catch (Exception)
            {
                //todo: do something with the error
            }

            LightStartHoursBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            DarkStartHoursBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            applyButton.IsEnabled = true;
            locationBlock.Visibility = Visibility.Collapsed;
            SetOffsetVisibility(Visibility.Collapsed);

            userFeedback.Text = Properties.Resources.msgClickApply; // Click on apply to save changes
        }

        //automatic theme switch checkbox
        private void AutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            configBuilder.config.Enabled = true;

            if (is1903) SystemComboBox.IsEnabled = true;
            if (is1903 && !SystemComboBox.SelectedIndex.Equals(1)) AccentColorCheckBox.IsEnabled = true;
            AppComboBox.IsEnabled = true;
            EdgeComboBox.IsEnabled = true;
            locationCheckBox.IsEnabled = true;
            applyButton.IsEnabled = true;
            DarkStartHoursBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            LightStartHoursBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            BGWinButton.IsEnabled = true;
            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes

            //todo: addAutostart call to backend
        }
        private void AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            configBuilder.config.Enabled = false;

            configBuilder.config.Wallpaper.Enabled = false;
            configBuilder.config.Wallpaper.DarkThemeWallpapers.Clear();
            configBuilder.config.Wallpaper.LightThemeWallpapers.Clear();

            AccentColorCheckBox.IsEnabled = false;
            SystemComboBox.IsEnabled = false;
            AppComboBox.IsEnabled = false;
            EdgeComboBox.IsEnabled = false;
            locationCheckBox.IsEnabled = false;
            locationCheckBox.IsChecked = false;
            applyButton.IsEnabled = false;
            DarkStartHoursBox.IsEnabled = false;
            DarkStartMinutesBox.IsEnabled = false;
            LightStartHoursBox.IsEnabled = false;
            LightStartMinutesBox.IsEnabled = false;
            BGWinButton.IsEnabled = false;
            userFeedback.Text = Properties.Resources.welcomeText; //Activate the checkbox to enable automatic theme switching
            ShowDeskBGStatus();

            //todo: removeAutostart call to backend
        }

        //ComboBox
        private void AppComboBox_DropDownClosed(object sender, EventArgs e)
        {
            //todo: switch over to pipe based

            configBuilder.config.AppsTheme = AppComboBox.SelectedIndex;

            if (AppComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.AppThemeChange = 0;
                try
                {

                    //todo: switch over to pipe based
                    regEditHandler.SwitchThemeBasedOnTime();
                }
                catch
                {

                }
            }
            if (AppComboBox.SelectedIndex.Equals(1))
            {

                //todo: switch over to pipe based
                Properties.Settings.Default.AppThemeChange = 1;
                regEditHandler.AppTheme(1);
            }
            if (AppComboBox.SelectedIndex.Equals(2))
            {

                //todo: switch over to pipe based
                Properties.Settings.Default.AppThemeChange = 2;
                regEditHandler.AppTheme(0);
            }
        }
        private void SystemComboBox_DropDownClosed(object sender, EventArgs e)
        {
            configBuilder.config.SystemTheme = SystemComboBox.SelectedIndex;
            //todo: switch over to pipe based


            if (SystemComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.SystemThemeChange = 0;
                try
                {
                    regEditHandler.SwitchThemeBasedOnTime();
                }
                catch
                {

                }
                AccentColorCheckBox.IsEnabled = true;
            }
            if (SystemComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.SystemThemeChange = 1;
                if (configBuilder.config.AccentColorTaskbar)
                {
                    regEditHandler.ColorPrevalence(0);
                    Thread.Sleep(200);
                }
                regEditHandler.SystemTheme(1);
                AccentColorCheckBox.IsEnabled = false;
                AccentColorCheckBox.IsChecked = false;
            }
            if (SystemComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.SystemThemeChange = 2;
                regEditHandler.SystemTheme(0);
                if (configBuilder.config.AccentColorTaskbar)
                {
                    Thread.Sleep(200);
                    regEditHandler.ColorPrevalence(1);
                }
                AccentColorCheckBox.IsEnabled = true;
            }
        }
        private void EdgeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            //todo: switch over to pipe based

            configBuilder.config.EdgeTheme = EdgeComboBox.SelectedIndex;

            if (EdgeComboBox.SelectedIndex.Equals(0))
            {
                Properties.Settings.Default.EdgeThemeChange = 0;
                try
                {
                    regEditHandler.SwitchThemeBasedOnTime();
                }
                catch
                {

                }
            }
            if (EdgeComboBox.SelectedIndex.Equals(1))
            {
                Properties.Settings.Default.EdgeThemeChange = 1;
                regEditHandler.EdgeTheme(0);
            }
            if (EdgeComboBox.SelectedIndex.Equals(2))
            {
                Properties.Settings.Default.EdgeThemeChange = 2;
                regEditHandler.EdgeTheme(1);
            }
            if (EdgeComboBox.SelectedIndex.Equals(3))
            {
                Properties.Settings.Default.EdgeThemeChange = 3;
            }
        }

        private void AddJumpList()
        {
            JumpTask darkJumpTask = new JumpTask
            {
                Title = Properties.Resources.lblDarkTheme,//Dark theme
                Arguments = "/dark",
                CustomCategory = Properties.Resources.lblSwitchTheme//Switch current theme
            };
            JumpTask lightJumpTask = new JumpTask
            {
                Title = Properties.Resources.lblLightTheme,//Light theme
                Arguments = "/light",
                CustomCategory = Properties.Resources.lblSwitchTheme//Switch current theme
            };

            JumpList jumpList = new JumpList();
            jumpList.JumpItems.Add(darkJumpTask);
            jumpList.JumpItems.Add(lightJumpTask);
            jumpList.ShowFrequentCategory = false;
            jumpList.ShowRecentCategory = false;
            JumpList.SetJumpList(Application.Current, jumpList);
        }

        private void AccentColorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //todo: switch over to pipe based

            configBuilder.config.AccentColorTaskbar = true;
            try
            {
                if (SystemComboBox.SelectedIndex.Equals(0)) regEditHandler.SwitchThemeBasedOnTime();
                if (SystemComboBox.SelectedIndex.Equals(2)) regEditHandler.ColorPrevalence(1);
            }
            catch
            {

            }
        }

        private void AccentColorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            //todo: switch over to pipe based

            configBuilder.config.AccentColorTaskbar = false;
            regEditHandler.ColorPrevalence(0);
        }

        //open desktop background window
        private void BGWinButton_Click(object sender, RoutedEventArgs e)
        {
            DesktopBGui BGui = new DesktopBGui
            {
                Owner = GetWindow(this)
            };
            BGui.ShowDialog();
            if (BGui.saved == true)
            {
                ApplyButton_Click(this, null);
            }
            ShowDeskBGStatus();
        }
        private void ShowDeskBGStatus()
        {
            if (!configBuilder.config.Wallpaper.Enabled)
            {
                DeskBGStatus.Text = Properties.Resources.enabled;
            }
            else
            {
                DeskBGStatus.Text = Properties.Resources.disabled;
            }
        }
        private void SetOffsetVisibility(Visibility value)
        {
            OffsetLbl.Visibility = value;
            OffsetDarkLbl.Visibility = value;
            OffsetDarkModeButton.Visibility = value;
            OffsetLightLbl.Visibility = value;
            OffsetLightModeButton.Visibility = value;
            OffsetLightBox.Visibility = value;
            OffsetDarkBox.Visibility = value;
            OffsetDarkDot.Visibility = value;
            OffsetLightDot.Visibility = value;
            OffsetButton.Visibility = value;
        }

        //application close behaviour
        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
            // workaround to counter async running clients while context is being closed!
            CommandClient.SendMessage("");
            NetMQConfig.Cleanup();
            Process.GetCurrentProcess().Kill();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveConfigInteractive();

            if (configBuilder.config.ClassicMode) CommandClient.SendMessage(Command.Shutdown);
            base.OnClosing(e);
        }

        private void SaveConfigInteractive()
        {
            try
            {
                configBuilder.Save();
            }
            catch (Exception ex)
            {
                userFeedback.Text = Properties.Resources.msgErrorOcc;
                string error = Properties.Resources.errorThemeApply + "\n\n Error ocurred in: MainWindow.OnClosing.autoDarkModeConfigBuilder.Save()" + "\n\n" + ex.Message;
                MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
                {
                    Owner = GetWindow(this)
                };
                msg.ShowDialog();
                var result = msg.DialogResult;
                if (result == true)
                {
                    Process.Start("https://github.com/Armin2208/Windows-Auto-Night-Mode/issues");
                }
                return;
            }
        }
    }
}