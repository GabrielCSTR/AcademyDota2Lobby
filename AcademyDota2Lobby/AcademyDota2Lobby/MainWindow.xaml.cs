using AcademyDota2Lobby.Views;
using AcademyDota2Lobby.Views.Pages;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AcademyDota2Lobby
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static User CurrentUser { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // pegando dados do usuario do window login
            var windowLogin = this.DataContext;
            CurrentUser = (User)((WLogin)windowLogin).getUser();

            DoubleAnimation animation = new DoubleAnimation(0, 1,
                                    (Duration)TimeSpan.FromSeconds(1));
            this.BeginAnimation(UIElement.OpacityProperty, animation);

            SwitchScreen(new UCHome());
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(1);
        }

        private void BtnEvent_Click(object sender, RoutedEventArgs e)
        {
            int indexMenu = int.Parse(((RadioButton)e.Source).Uid); // index do menu

            RadioButton MenuButton = (RadioButton)e.Source; // salva o dados do button

            //Switch index menu selecionado
            switch (indexMenu)
            {
                case 0:
                    CheckButtonsPressed();              //Verifica os botao que estao Desativado e ativa novamente 
                    MenuButton.IsEnabled = false;       //Desativa botao selecionado
                    SwitchScreen(new UCHome());       //Inicia a screen do usercontrol selecionado
                    break;
                case 1:
                    CheckButtonsPressed();              //Verifica os botao que estao Desativado e ativa novamente 
                    MenuButton.IsEnabled = false;       //Desativa botao selecionado
                    SwitchScreen(new UCLobby());  //Inicia a screen do usercontrol selecionado
                    break;
                case 2:
                    CheckButtonsPressed();              //Verifica os botao que estao Desativado e ativa novamente 
                    MenuButton.IsEnabled = false;       //Desativa botao selecionado
                    SwitchScreen(new UCHome());    //Inicia a screen do usercontrol selecionado
                    break;
                case 3:
                    CheckButtonsPressed();              //Verifica os botao que estao Desativado e ativa novamente 
                    MenuButton.IsEnabled = false;       //Desativa botao selecionado
                    SwitchScreen(new UCCadastro());    //Inicia a screen do usercontrol selecionado
                    break;
                case 4:
                    CheckButtonsPressed();              //Verifica os botao que estao Desativado e ativa novamente 
                    MenuButton.IsEnabled = false;       //Desativa botao selecionado
                    SwitchScreen(new UCHome());    //Inicia a screen do usercontrol selecionado
                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// Desativa todos os botao que esta ativado
        /// </summary>
        /// <returns></returns>
        private void CheckButtonsPressed()
        {
            for (int i = 0; i < 5; i++)
            {
                var ButtonPressed = GetByUid(this, i.ToString());

                if (!ButtonPressed.IsEnabled)
                    ButtonPressed.IsEnabled = true;

            }
        }

        /// <summary>
        /// Retona Dados do Botao Por Uid informado
        /// </summary>
        /// <param name="rootElement"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static UIElement GetByUid(DependencyObject rootElement, string uid)
        {
            foreach (UIElement element in LogicalTreeHelper.GetChildren(rootElement).OfType<UIElement>())
            {
                if (element.Uid == uid)
                    return element;
                UIElement resultChildren = GetByUid(element, uid);
                if (resultChildren != null)
                    return resultChildren;
            }

            return null;
        }

        /// <summary>
        /// Switch Screen Selecionada
        /// </summary>
        /// <param name="sender">UserControl</param>
        internal void SwitchScreen(object sender)
        {
            var screen = ((UserControl)sender);

            if (screen != null)
            {
                StackPanelMain.Children.Clear();
                StackPanelMain.Children.Add(screen);
            }
        }
    }
}
