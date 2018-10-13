using System;
using System.Collections.Generic;
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
            //显示生成代码
            string code = DfaManager.GetCode();
            tb_output.Text = code;
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

        private void bt_save_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            sfd.Filter = "c源文件|*.c";
            sfd.FileName = "code";
            if (true == sfd.ShowDialog()) {
                string localFilePath = sfd.FileName.ToString();
                string context = tb_output.Text;
                File.WriteAllText(localFilePath, context);
            }
        }
    }
}
