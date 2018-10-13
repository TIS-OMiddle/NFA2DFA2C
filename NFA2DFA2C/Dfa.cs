using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace NFA2DFA2C {
    public class DfaNode {
        public static int state_num = 0;

        public DfaNode() {
            statenum = state_num++;
        }

        public int statenum;//节点编号
        public bool isEnd = false;//可终止节点
        //第一列
        public HashSet<int> nfastates = new HashSet<int>();

        //该dfanode对应的那一行的后几列保存
        //public string[] DATA { get { return data; } }
        public string[] DATA {
            get {
                string[] res = new string[data.Count + 1];

                string str = "{";
                foreach (int i in nfastates) {
                    str += i + ",";
                }
                str = str.Substring(0, str.Length - 1);
                str += "}";
                if (isEnd) str = "+ " + str;
                res[0] = str;

                for (int j = 0; j < data.Count; j++) {
                    str = "{";
                    foreach (int i in data[j]) {
                        str += i + ",";
                    }
                    str = str.Substring(0, str.Length - 1);
                    str += "}";
                    if (str.Length == 1)
                        str = isEnd ? "AC" : "ERROR";
                    res[j+1] = str;
                }

                return res;
            }
        }
        public List<HashSet<int>> data;
    }


    public class DfaManager {
        private static List<HashSet<int>> queue = new List<HashSet<int>>();
        private static List<DfaNode> dfanodes = new List<DfaNode>();
        //最小化后的节点关系
        public static List<List<string>> dfanodes_min = new List<List<string>>();
        //判断集合是否在队列
        private static bool FindInQueue(HashSet<int> set) {
            for(int i = 0; i < queue.Count; i++) {
                if (EqualSet(queue[i], set))
                    return true;
            }
            return false;
        }
        //判断集合是否相等
        private static bool EqualSet(HashSet<int> a, HashSet<int> b) {
            if (a.Count != b.Count)
                return false;
            foreach (var item in a) {
                if (!b.Contains(item)) {
                    return false;
                }
            }
            return true;
        }
        //判断DfaNode是否相等
        private static bool EqualDfaNode(DfaNode a,DfaNode b) {
            if (a.isEnd == b.isEnd) {
                for (int i = 0; i < a.data.Count; i++) {
                    if (!EqualSet(a.data[i], b.data[i])) {
                        return false;
                    }
                }
                return true;
            }
            //终态不对应，不相等
            return false;
        }
        //判断DfaNode_MIN是否相等
        private static bool EqualDfaNode_MIN(List<string> a, List<string> b) {

            bool aIsEnd = a[0][0] == '+' ? true : false,
                bIsEnd = aIsEnd = b[0][0] == '+' ? true : false;
            if (aIsEnd == bIsEnd) {
                for(int i = 1; i < a.Count; i++) {
                    if (a[i] != b[i]) {
                        return false;
                    }
                }
                return true;
            }
            //终态不对应，不相等
            return false;
        }
        //最小化节点变换
        private static void ReplaceDfa_MIN(string a,string b) {
            for(int i = 0; i < dfanodes_min.Count; i++) {
                for (int j = 0; j < dfanodes_min[0].Count; j++)
                    dfanodes_min[i][j] = dfanodes_min[i][j].Replace(a, b);
            }
        }
        //Nfa到Dfa
        private static void GetAllDfaNode(NfaPair np) {
            int pos = 0, totalColumn = NfaManager.chars.Count + NfaManager.charstrings.Count;
            HashSet<int> s0 = np.start.GetClosure();
            queue.Add(s0);

            //第一列那列放入queue，每行放入dfanodes
            for (; pos < queue.Count; pos++) {
                HashSet<int> theset = queue[pos];

                //设置dfanode
                DfaNode dfanode = new DfaNode();
                dfanode.data = new List<HashSet<int>>();
                dfanode.nfastates = theset;
                if (theset.Contains(np.end.nodenum))
                    dfanode.isEnd = true;

                //所有字符的对应闭包入队列
                foreach (var pattchar in NfaManager.chars) {
                    HashSet<int> temp = new HashSet<int>();
                    foreach (var nodenum in theset) {
                        HashSet<int> t = NfaManager.nfanodes[nodenum].GetClosureWithChar(pattchar);
                        foreach (var t_item in t) {
                            temp.Add(t_item);
                        }
                    }
                    if (temp.Count > 0 && !FindInQueue(temp))
                        queue.Add(temp);
                    dfanode.data.Add(temp);
                }
                //所有字符集的对应闭包入队列
                foreach (var pattchar in NfaManager.charstrings) {
                    HashSet<int> temp = new HashSet<int>();
                    foreach (var nodenum in theset) {
                        HashSet<int> t = NfaManager.nfanodes[nodenum].GetClosureWithChar(pattchar);
                        foreach (var t_item in t) {
                            temp.Add(t_item);
                        }
                    }
                    if (temp.Count > 0 && !FindInQueue(temp))
                        queue.Add(temp);
                    dfanode.data.Add(temp);
                }
                dfanodes.Add(dfanode);
            }
        }
        //显示Dfa
        public static void DrawDfa(ListView lv,NfaPair np) {
            GetAllDfaNode(np);

            int totalColumn = NfaManager.chars.Count + NfaManager.charstrings.Count, counts = 0;
            //建立表
            GridView gv = lv.View as GridView;
            gv.Columns.Clear();
            GridViewColumn col = new GridViewColumn();
            col.Header = "集合";
            col.DisplayMemberBinding = new Binding(string.Format("DATA[{0}]",counts++));
            gv.Columns.Add(col);
            foreach (var item in NfaManager.chars) {
                GridViewColumn c = new GridViewColumn();
                c.Header = ((char)item).ToString();
                c.DisplayMemberBinding = new Binding(string.Format("DATA[{0}]", counts++));
                gv.Columns.Add(c);
            }
            foreach (var item in NfaManager.charstrings) {
                GridViewColumn c = new GridViewColumn();
                c.Header = item;
                c.DisplayMemberBinding = new Binding(string.Format("DATA[{0}]", counts++));
                gv.Columns.Add(c);
            }
            lv.ItemsSource = dfanodes;
        }
        //Dfa到最小化Dfa
        private static void GetAllDfaNodeMin() {
            //DFA节点化
            bool isFind;
            int cols = NfaManager.chars.Count + NfaManager.charstrings.Count;
            for (int i = 0; i < dfanodes.Count; i++) {
                List<string> temp = new List<string>();
                temp.Add(i.ToString());
                if (dfanodes[i].isEnd)
                    temp[0] = "+ " + temp[0];
                for (int j = 0; j < cols; j++) {
                    //判断是否与第一列的集合相等，如果是则放入id
                    //否则，AC放入-1，ERROR放入-2
                    isFind = false;
                    for (int k = 0; k < dfanodes.Count; k++) {
                        if (EqualSet(dfanodes[i].data[j], dfanodes[k].nfastates)) {
                            temp.Add(k.ToString());
                            isFind = true;
                            break;
                        }
                    }
                    if (!isFind)
                        temp.Add("");
                }
                dfanodes_min.Add(temp);
            }

            //DFA节点化后最小化
            for (int i = 0; i < dfanodes_min.Count; i++) {
                for (int j = dfanodes_min.Count - 1; j > i; j--) {
                    if (EqualDfaNode_MIN(dfanodes_min[i], dfanodes_min[j])) {
                        dfanodes_min.RemoveAt(j);
                        ReplaceDfa_MIN(j.ToString(), i.ToString());
                    }
                }
            }
        }
        //显示最小化Dfa
        public static void DrawDfaMin(ListView lv) {
            GetAllDfaNodeMin();

            int totalColumn = NfaManager.chars.Count + NfaManager.charstrings.Count, counts = 0;
            //建立表
            GridView gv = lv.View as GridView;
            gv.Columns.Clear();
            GridViewColumn col = new GridViewColumn();
            col.Header = "节点";
            col.DisplayMemberBinding = new Binding(string.Format("[{0}]", counts++));
            gv.Columns.Add(col);
            foreach (var item in NfaManager.chars) {
                GridViewColumn c = new GridViewColumn();
                c.Header = ((char)item).ToString();
                c.DisplayMemberBinding = new Binding(string.Format("[{0}]", counts++));
                gv.Columns.Add(c);
            }
            foreach (var item in NfaManager.charstrings) {
                GridViewColumn c = new GridViewColumn();
                c.Header = item;
                c.DisplayMemberBinding = new Binding(string.Format("[{0}]", counts++));
                gv.Columns.Add(c);
            }
            lv.ItemsSource = dfanodes_min;
        }

        public static void Clear() {
            NfaNode.totalNfaNode = 0;
            queue = new List<HashSet<int>>();
            dfanodes = new List<DfaNode>();
            dfanodes_min = new List<List<string>>();
        }
    }
}
