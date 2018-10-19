using System;
using System.Collections.Generic;
using System.IO;
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
                    res[j + 1] = str;
                }

                return res;
            }
        }
        public List<HashSet<int>> data;
    }


    public class DfaManager {
        private static List<HashSet<int>> queue = new List<HashSet<int>>();//辅助判断集合是否放入集合
        private static List<DfaNode> dfanodes = new List<DfaNode>();//dfa节点集合
        private static List<string> mychars = new List<string>();//字符集合
        private static List<string> mycharstrings = new List<string>();//字符集的集合
        private static List<HashSet<string>> min_queue = new List<HashSet<string>>();//节点化后非/终态集合的集合
        public static List<List<string>> dfanodes_min = new List<List<string>>();//最小化后的节点储存结构
        //判断集合是否在队列
        private static bool FindInQueue(HashSet<int> set) {
            for (int i = 0; i < queue.Count; i++) {
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
        //返回在minnode的序号
        private static int FindInMinNode(string nodenum) {
            for(int i = 0; i < dfanodes_min.Count; i++) {
                string num = dfanodes_min[i][0];
                if (num[0] == '+') {
                    num = num.Substring(2);
                }
                if (num.Equals(nodenum))
                    return i;
            }
            return -1;
        }
        //判断节点是否在相同s0,s1,s2..集合
        private static bool InSameSet(string n1,string n2) {
            int pos = 0;
            for (; pos < min_queue.Count; pos++) {
                if (min_queue[pos].Contains(n1))
                    break;
            }
            return min_queue[pos].Contains(n2);
        }
        //判断DfaNode_MIN是否相等
        private static bool EqualDfaNode_MIN(List<string> a, List<string> b) {
            bool aIsEnd = a[0][0] == '+' ? true : false,
                bIsEnd = aIsEnd = b[0][0] == '+' ? true : false;
            if (aIsEnd == bIsEnd) {
                for (int i = 1; i < a.Count; i++) {
                    if (!a[i].Equals(b[i])&&!InSameSet(a[i],b[i])) {
                        return false;
                    }
                }
                return true;
            }
            //终态不对应，不相等
            return false;
        }
        //最小化节点变换
        private static void ReplaceDfa_MIN(string a, string b) {
            for (int i = 0; i < dfanodes_min.Count; i++) {
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
        public static void DrawDfa(ListView lv, NfaPair np) {
            GetAllDfaNode(np);

            int totalColumn = NfaManager.chars.Count + NfaManager.charstrings.Count, counts = 0;
            //建立表
            GridView gv = lv.View as GridView;
            gv.Columns.Clear();
            GridViewColumn col = new GridViewColumn();
            col.Header = "集合";
            col.DisplayMemberBinding = new Binding(string.Format("DATA[{0}]", counts++));
            gv.Columns.Add(col);
            foreach (var item in NfaManager.chars) {
                GridViewColumn c = new GridViewColumn();
                c.Header = ((char)item).ToString();
                mychars.Add(((char)item).ToString());
                c.DisplayMemberBinding = new Binding(string.Format("DATA[{0}]", counts++));
                gv.Columns.Add(c);
            }
            foreach (var item in NfaManager.charstrings) {
                GridViewColumn c = new GridViewColumn();
                c.Header = item;
                mycharstrings.Add(item);
                c.DisplayMemberBinding = new Binding(string.Format("DATA[{0}]", counts++));
                gv.Columns.Add(c);
            }
            lv.ItemsSource = dfanodes;
        }
        
        //Dfa到最小化Dfa
        private static void GetAllDfaNodeMin() {
            HashSet<string> notEndSet = new HashSet<string>();//非终态集合
            HashSet<string> isEndSet = new HashSet<string>();//终态集合

            //DFA节点化到dfanodes_min
            bool isFind;
            int cols = NfaManager.chars.Count + NfaManager.charstrings.Count;
            for (int i = 0; i < dfanodes.Count; i++) {
                List<string> temp = new List<string>();
                temp.Add(i.ToString());
                if (dfanodes[i].isEnd) {
                    temp[0] = "+ " + temp[0];
                    isEndSet.Add(i.ToString());
                }
                else {
                    notEndSet.Add(i.ToString());
                }
                for (int j = 0; j < cols; j++) {
                    //判断是否与第一列的集合相等，如果是则放入id
                    //否则，留空
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

            //s0,s1集合放入队列
            if(notEndSet.Count>0)
                min_queue.Add(notEndSet);
            if(isEndSet.Count>0)
                min_queue.Add(isEndSet);

            for (int i = 0; i < min_queue.Count; i++) {
                HashSet<string> hashset = min_queue[i];
                if (hashset.Count == 1) continue;
                string[] strs = hashset.ToArray<string>();
                int node1 = FindInMinNode(strs[0]);
                HashSet<string> newset = new HashSet<string>();
                for (int j = 1; j < strs.Length; j++) {
                    int node2 = FindInMinNode(strs[j]);
                    if (EqualDfaNode_MIN(dfanodes_min[node1], dfanodes_min[node2])) {
                        //两行相等，删除一行，替换该行节点
                        dfanodes_min.RemoveAt(node2);
                        ReplaceDfa_MIN(strs[j], strs[0]);
                    }
                    else {
                        //两行不等，放入新集合
                        hashset.Remove(strs[j]);
                        newset.Add(strs[j]);
                    }
                }
                if (newset.Count > 0) {
                    min_queue.Add(newset);
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
        
        //由字符集字符串生成真正的字符集
        private static HashSet<string> GetRealCharStringSet(string charsetstring) {
            HashSet<string> set = new HashSet<string>();
            int len = charsetstring.Length, startpos;
            bool isneg;
            if (charsetstring[0] == '^') {
                isneg = true;
                startpos = 1;
            }
            else {
                isneg = false;
                startpos = 0;
            }
            //加入集合
            for (int i = startpos; i < len; i++) {
                //预读下一字符，如果为连字符则
                if ((i + 1) < len && charsetstring[i + 1] == '-') {
                    for (int j = charsetstring[i]; j <= charsetstring[i + 2]; j++)
                        set.Add(((char)j).ToString());
                    i += 2;
                }
                else set.Add(charsetstring[i].ToString());
            }
            //取反标准ASCII的128个字符
            if (isneg) {
                HashSet<string> temp = new HashSet<string>();
                for (int i = 0; i <= 127; i++) {
                    if (!set.Contains(((char)i).ToString()))
                        temp.Add(((char)i).ToString());
                }
                set = temp;
            }
            return set;
        }
        //生成单独一个case
        private static string GetOneCase(List<string> input) {
            //生成到读入后
            string read = "c=input[pos++];\n";
            string state;
            bool canEnd = input[0][0] == '+' ? true : false;
            if (canEnd)
                state = input[0].Substring(2);
            else state = input[0];
            string code = "case " + state + ":\n";
            code += read;

            //生成字符判断
            int pos = 1;
            string judge = "";
            for (int i = 0; i < mychars.Count; i++) {
                if (input[pos + i].Length > 0) {
                    judge = "if(c=='" + mychars[i] + "')\n    {state=" + input[pos + i] + ";isMatch=true;}\n";
                    code += judge;
                }
            }

            //生成字符集判断
            pos += mychars.Count;
            for (int i = 0; i < mycharstrings.Count; i++) {
                if (input[pos + i].Length > 0) {
                    HashSet<string> set = GetRealCharStringSet(mycharstrings[i]);
                    judge = "if(";
                    foreach (var ch in set) {
                        judge += "c=='" + ch + "'||";
                    }
                    judge = judge.Substring(0, judge.Length - 2);
                    judge += ")\n    {state=" + input[pos + i] + ";isMatch=true;}\n";
                    code += judge;
                }
            }

            //生成结束判断
            if (canEnd)
                code += "if(c=='\\0')\n    {state=-1;isMatch=true;}\n";
            code += "if(!isMatch)\n    {state=-2;}\n";
            code += "break;\n\n";
            return code;
        }
        //生成代码
        private static string template = null;
        public static string GetCode() {
            string res;
            if (template == null) {
                StreamReader reader = new StreamReader("template.txt");
                template = reader.ReadToEnd();
                reader.Close();
            }

            //全部拼接
            string mycases = "";
            for (int i = 0; i < dfanodes_min.Count; i++) {
                string mycase = GetOneCase(dfanodes_min[i]);
                mycases += mycase;
            }
            res = template.Replace("{0}", mycases);

            return res;
        }

        public static void Clear() {
            NfaNode.totalNfaNode = 0;
            queue = new List<HashSet<int>>();
            dfanodes = new List<DfaNode>();
            dfanodes_min = new List<List<string>>();
            mychars = new List<string>();
            mycharstrings = new List<string>();
            min_queue = new List<HashSet<string>>();
        }
    }
}
