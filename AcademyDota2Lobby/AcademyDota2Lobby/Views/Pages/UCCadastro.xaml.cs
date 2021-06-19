using AmRoMessageDialog;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace AcademyDota2Lobby.Views
{
    /// <summary>
    /// Interaction logic for UCCadastro.xaml
    /// </summary>
    public partial class UCCadastro : UserControl
    {
        public UCCadastro()
        {
            InitializeComponent();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            //DBConnection cx = new DBConnection(Connection.StringConnection);
            //var teste = cx;

            var _messageBox = new AmRoMessageBox
            {
                Background = "#333333",
                TextColor = "#ffffff",
                IconColor = "#FFF44336",
                RippleEffectColor = "#000000",
                ClickEffectColor = "#1F2023",
                ShowMessageWithEffect = true,
                EffectArea = this,
                CaptionFontSize = 16,
                MessageFontSize = 14

            };

            _messageBox.Show("Informe todos os campos corretamente!", "ALERT", AmRoMessageBoxButton.Ok, AmRoMessageBoxIcon.Error);
        }
    }
}
