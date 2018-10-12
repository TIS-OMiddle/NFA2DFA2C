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

namespace NFA2DFA2C {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            Init();
        }

        private void Init() {
            //var c1 = NfaManager.GetNfaPair("a|b*c");
            //NfaManager.DrawNfaPair(canvas_nfa, c1);
        }

        private void bt_creat_Click(object sender, RoutedEventArgs e) {
            //清理工作
            canvas_nfa.Children.Clear();
            NfaManager.Clear();

            string pattern = tb_input.Text;
            var np = NfaManager.GetNfaPair(pattern);
            NfaManager.DrawNfaPair(canvas_nfa, np);
        }

        private void tb_input_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter)
                bt_creat_Click(null, null);
        }
    }
}
