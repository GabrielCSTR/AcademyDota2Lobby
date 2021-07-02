using D2LAPI;
using D2LAPI.Modal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AcademyDota2Lobby.Views.Pages
{
    /// <summary>
    /// Interaction logic for WLogin.xaml
    /// </summary>
    public partial class WLogin : Window
    {
        private static User _currentUser;
        internal static User CurrentUser { get => _currentUser; set => _currentUser = value; }

        public WLogin()
        {
            InitializeComponent();
        }

        #region EVENT BUTTONS CLOSE, MINIMIZE, MAXIMIZE
        private void btnMin_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        #endregion

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation(0, 1,
                        (Duration)TimeSpan.FromSeconds(1));
            this.BeginAnimation(UIElement.OpacityProperty, animation);

            saveEmail.IsChecked = Properties.Settings.Default.isEmailChecked;
            // load os dados do user para os campos
            tbxUsername.Text = Properties.Settings.Default.email;
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = tbxUsername.Text;         // username
            string password = pbxPassword.Password;     // password

            if (username.Length <= 0)
            {
                lblStatus.Text = "Por favor, informe o seu E-mail!";
            }
            else if (password.Length <= 0)
            {
                lblStatus.Text = "Por favor, informe uma senha valida!";
            }
            else
            {
                // loading
                panelLogin.Visibility = Visibility.Collapsed;
                btnLogin.Visibility = Visibility.Collapsed;
                loading.Visibility = Visibility.Visible;

                Authentication(username, password); // authentication login
            }
        }

        private void Authentication(string username, string password)
        {
            // API
            ApiOperations api = new ApiOperations();
            // VALIDATION API
            User Authentication = api.AuthenticateUser(username, password);

            // CHECK RETURN VALIDATION
            if (Authentication.Response != null && Authentication.Response.StatusCode != 200)
            {
                lblStatus.Text = Authentication.Response.Error;
                // loading
                panelLogin.Visibility = Visibility.Visible;
                btnLogin.Visibility = Visibility.Visible;
                loading.Visibility = Visibility.Collapsed;

                return;
            }
            else if (Authentication != null && Authentication.access_token != null)
            {
                //Main main = new Main(); // Run Construtor of Main to initialize Main Ludo Object
                //Main.d2l.Users.Add(Authentication);

                // Start authentication 
                authenticationManegerAsync(Authentication);

                //MainWindow main = new MainWindow();
            }
        }

        private async void authenticationManegerAsync(User user)
        {
            _currentUser = user; // salve user logged

            lblStatus.Text = "Login successful"; // msg

            await Task.Delay(2000); // delay

            MainWindow main = new MainWindow();
            main.DataContext = this;

            this.Hide();

            main.Show();
        }

        private void saveEmail_Click(object sender, RoutedEventArgs e)
        {
            if(saveEmail.IsChecked == false)
            {
                // salva os dados de acesso
                Properties.Settings.Default.email = tbxUsername.Text;
                Properties.Settings.Default.isEmailChecked = true;
                Properties.Settings.Default.Save();
            }else
            {
                // salva os dados de acesso
                Properties.Settings.Default.email = null;
                Properties.Settings.Default.isEmailChecked = false;
                Properties.Settings.Default.Save();
            }

        }

        public Object getUser()
        {
            return CurrentUser;
        }
    }
}
