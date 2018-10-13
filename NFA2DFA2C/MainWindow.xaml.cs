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
    class MyTag {
        public string Tag { get; set; }
        public string Reg { get; set; }
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            Init();
        }

        private static System.Collections.ObjectModel.ObservableCollection<MyTag> mytags = new System.Collections.ObjectModel.ObservableCollection<MyTag>();
        private void Init() {
            mytags.Add(new MyTag() { Tag = "letter", Reg = "[A-Za-z]" });
            mytags.Add(new MyTag() { Tag = "digital", Reg = "[0-9]" });
            lv_tag.ItemsSource = mytags;
        }

        private void bt_creat_Click(object sender, RoutedEventArgs e) {
            //清理工作
            canvas_nfa.Children.Clear();
            NfaManager.Clear();
            DfaManager.Clear();

            //替换标识,获取Nfa
            string pattern = tb_input.Text;
            for (int i = 0; i < mytags.Count; i++)
                pattern = pattern.Replace(mytags[i].Tag, mytags[i].Reg);
            var np = NfaManager.GetNfaPair(pattern);
            //作图Nfa
            NfaManager.DrawNfaPair(canvas_nfa, np);
            //作表Dfa
            DfaManager.DrawDfa(lv_dfa, np);
            //作表DfaMin
            DfaManager.DrawDfaMin(lv_dfa_min);
        }

        private void tb_input_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter)
                bt_creat_Click(null, null);
        }

        private void bt_add_Click(object sender, RoutedEventArgs e) {
            if (tb_tag.Text.Length > 0) {
                string[] strs = tb_tag.Text.Split('=');
                mytags.Add(new MyTag() { Tag = strs[0], Reg = strs[1] });
            }
        }

        private void bt_rm_Click(object sender, RoutedEventArgs e) {
            if (lv_tag.SelectedIndex > -1) {
                mytags.RemoveAt(lv_tag.SelectedIndex);
            }
        }
    }
}
