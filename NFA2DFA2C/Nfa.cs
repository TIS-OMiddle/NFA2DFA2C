using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NFA2DFA2C {
    public class NfaNode {
        public static int totalNfaNode = 0;

        public NfaNode() {
            nodenum = totalNfaNode++;
        }

        public int edge = -3;//-3:无边，-2有字符集合charset，-1为ε，其他则为该边字符
        public NfaNode next1;//连线节点1
        public NfaNode next2;//连线节点2
        public string charsetstring;//字符集对应的正则字符串
        public int nodenum;//节点编号
        public int deep;//节点深度
        public int deepnum;//depp深度上的第几个

        //获取ε闭包集合
        public HashSet<int> GetClosure() {
            HashSet<int> set = new HashSet<int>();
            set.Add(nodenum);
            if (edge != -1)
                return set;
            if (next1 != null) {
                HashSet<int> lset = next1.GetClosure();
                foreach (var i in lset) {
                    set.Add(i);
                }
            }
            if (next2 != null) {
                HashSet<int> rset = next2.GetClosure();
                foreach (var i in rset) {
                    set.Add(i);
                }
            }
            return set;
        }
        //获取经过指定字符的闭包
        public HashSet<int> GetClosureWithChar(int c) {
            HashSet<int> set = new HashSet<int>();
            if (edge == c)
                set.Add(next1.nodenum);
            else return set;
            if (next1 != null) {
                HashSet<int> lset = next1.GetClosure();
                foreach (var i in lset) {
                    set.Add(i);
                }
            }
            if (next2 != null) {
                HashSet<int> rset = next2.GetClosure();
                foreach (var i in rset) {
                    set.Add(i);
                }
            }
            return set;
        }
        //获取经过指定字符集的闭包
        public HashSet<int> GetClosureWithChar(string c) {
            HashSet<int> set = new HashSet<int>();
            if (charsetstring == c)
                set.Add(next1.nodenum);
            else return set;
            if (next1 != null && next1.edge == -1) {
                HashSet<int> lset = next1.GetClosure();
                foreach (var i in lset) {
                    set.Add(i);
                }
            }
            if (next2 != null && next2.edge == -1) {
                HashSet<int> rset = next2.GetClosure();
                foreach (var i in rset) {
                    set.Add(i);
                }
            }
            return set;
        }
    }

    public class NfaPair {
        public NfaNode start;
        public NfaNode end;
        public int count = 0;
    }

    public class NfaManager {
        //辅助变量
        public static NfaNode[] nfanodes;//快速查找对应序号的节点
        public static HashSet<int> chars = new HashSet<int>();
        public static HashSet<string> charstrings = new HashSet<string>();
        private static bool[] visited;//深度遍历
        private static int[] deeps;//对应深度节点数



        //由字符构建单个自动机
        private static NfaPair CreatNfaPair(char c) {
            NfaPair np = new NfaPair();

            NfaNode s = new NfaNode(), e = new NfaNode();
            s.edge = c;
            s.next1 = e;

            np.start = s;
            np.end = e;
            np.count += 2;
            return np;
        }
        //由字符串表示的字符集构建单个自动机
        private static NfaPair CreatNfaPair(string charsetstring) {
            NfaPair np = new NfaPair();
            NfaNode s = new NfaNode(), e = new NfaNode();
            s.edge = -2;
            s.charsetstring = charsetstring;
            s.next1 = e;
            np.start = s;
            np.end = e;
            np.count += 2;
            return np;
        }
        //连接&与运算自动机
        private static NfaPair CombineNfaPair(NfaPair before, NfaPair after) {
            before.end.edge = -1;
            before.end.next1 = after.start;
            before.end = after.end;
            before.count += after.count;
            return before;
        }
        //形成|或运算自动机
        private static NfaPair EitherNfaPair(NfaPair np1, NfaPair np2) {
            NfaPair np = new NfaPair();
            np.start = new NfaNode();
            np.end = new NfaNode();
            np.count += 2;
            if (np1.count < np2.count) {
                NfaPair t = np1;
                np1 = np2;
                np2 = t;
            }

            np.start.edge = -1;
            np.start.next1 = np1.start;
            np.start.next2 = np2.start;

            np1.end.edge = -1;
            np1.end.next1 = np.end;
            np2.end.edge = -1;
            np2.end.next1 = np.end;

            np.count += np1.count + np2.count;
            return np;
        }
        //形成*闭包自动机
        private static NfaPair DoStarCollapse(NfaPair nfaPair) {
            NfaPair np = new NfaPair();
            NfaNode before = new NfaNode(), after = new NfaNode();
            np.start = before;
            np.end = after;
            np.count += 2;
            //前面连接
            before.next1 = nfaPair.start;
            before.edge = -1;
            //后面连接
            nfaPair.end.next1 = after;
            nfaPair.end.edge = -1;
            //原来尾连头，现在头连尾
            nfaPair.end.next2 = nfaPair.start;
            before.next2 = after;

            np.count += nfaPair.count;
            return np;
        }
        //形成+闭包自动机
        private static NfaPair DoPosCollapse(NfaPair nfaPair) {
            NfaPair np = new NfaPair();
            NfaNode before = new NfaNode(), after = new NfaNode();
            np.start = before;
            np.end = after;
            np.count += 2;
            //前面连接
            before.next1 = nfaPair.start;
            before.edge = -1;
            //后面连接
            nfaPair.end.next1 = after;
            nfaPair.end.edge = -1;
            //原来尾连头
            nfaPair.end.next2 = nfaPair.start;
            np.count += nfaPair.count;
            return np;
        }
        //形成?闭包自动机
        private static NfaPair DoQuestCollapse(NfaPair nfaPair) {
            NfaPair np = new NfaPair();
            NfaNode before = new NfaNode(), after = new NfaNode();
            np.start = before;
            np.end = after;
            np.count += 2;
            //前面连接
            before.next1 = nfaPair.start;
            before.edge = -1;
            //后面连接
            nfaPair.end.next1 = after;
            nfaPair.end.edge = -1;
            //现在头连尾
            before.next2 = after;
            np.count += nfaPair.count;
            return np;
        }



        //后缀表达式
        private static Stack<NfaPair> npstack = new Stack<NfaPair>();//自动机栈
        private static Stack<char> opstack = new Stack<char>();//操作符栈,使用'\r'代表连接操作,'\n'代表起末标志
        private static HashSet<char> isop = new HashSet<char> {'|', '*', '+', '?', '\n', '(', ')','\r' };
        //全局运算组织
        public static NfaPair GetNfaPair(string pattern) {
            //补上连接运算符号
            int len = pattern.Length, pos = 0;
            char t, t_c;
            for (int i = 0; i < pattern.Length-1; i++) {
                if (pattern[i] == '[') {
                    while (pattern[i] != ']')
                        ++i;
                }
                t = pattern[i];
                if (i < pattern.Length - 1&&t!='|'&&t!='(') {
                    t_c = pattern[i + 1];
                    if (t_c!='*'&& t_c != '+' && t_c != '?' && t_c != '|' && t_c != ')') {
                        pattern = pattern.Insert(i + 1, "\r");
                        ++i;
                    }
                }
            }
            //开始操作
            opstack.Push('\n');
            pattern += "\n";
            char c;
            NfaPair npf;
            while (opstack.Count != 0) {
                c = pattern[pos++];
                //c不是操作符，则构建运算用nfa自动机
                if (c == '[') {
                    int i = pos;
                    while (pattern[i] != ']')
                        ++i;
                    npf = CreatNfaPair(pattern.Substring(pos, i - pos));
                    pos = i + 1;
                    npstack.Push(npf);
                }
                else if (!isop.Contains(c)) {
                    npf = CreatNfaPair(c);
                    npstack.Push(npf);
                }
                //c是操作符
                else {
                    if (Isp(opstack.Peek()) < Icp(c)) { opstack.Push(c); }//c优先度高，直接进栈
                    else if (Isp(opstack.Peek()) > Icp(c))//c优先度小，进栈前先清空可运算
                    {
                        while (Isp(opstack.Peek()) > Icp(c)) DoOperator(opstack.Pop());//清空可运算
                        if (c == ')' || c == '\n') opstack.Pop();//弹出'('或'\n'
                        else opstack.Push(c);//本次读取的操作符进栈
                    }
                    else opstack.Pop();//弹出'('或'\n'
                }
            }
            return npstack.Pop();
        }
        //栈内操作符优先度
        private static int Isp(char c) {
            switch (c) {
                case '\n': return 0;//最低，后面任何符号都可以进栈
                case '(': return 1;
                case '|': return 3;
                case '\r': return 5;
                case '*': return 7;
                case '+': return 7;
                case '?': return 7;
                case ')': return 8;
                default: return -1;
            }
        }
        //栈外操作符优先度
        private static int Icp(char c) {
            switch (c) {
                case '\n': return 0;//最低，清空栈以运算
                case ')': return 1;
                case '|': return 2;
                case '\r': return 4;
                case '*': return 6;
                case '+': return 6;
                case '?': return 6;
                case '(': return 8;
                default: return -1;
            }
        }
        //根据操作op操作nfastack
        private static void DoOperator(char op) {
            NfaPair left, right, result;
            switch (op) {
                case '|':
                    right = npstack.Pop();
                    left = npstack.Pop();
                    result = EitherNfaPair(left, right);
                    npstack.Push(result);
                    break;
                case '\r':
                    right = npstack.Pop();
                    left = npstack.Pop();
                    result = CombineNfaPair(left, right);
                    npstack.Push(result);
                    break;
                case '*':
                    right = npstack.Pop();
                    result= DoStarCollapse(right);
                    npstack.Push(result);
                    break;
                case '+':
                    right = npstack.Pop();
                    result = DoPosCollapse(right);
                    npstack.Push(result);
                    break;
                case '?':
                    right = npstack.Pop();
                    result = DoQuestCollapse(right);
                    npstack.Push(result);
                    break;
            }
        }



        //作图相关
        private static int radius = 15;//节点圆形半径
        private static int distanceX = 60;//节点横向基本距离
        private static int distanceY = 50;//节点纵向基本距离
        //遍历一次树，计算深度、同深度节点数量、可能字符、序号入数组，重置交由DrawNfaPair处理
        private static void DFS(NfaNode root) {
            _DFS(root, 0);
        }
        private static void _DFS(NfaNode node, int deep) {
            if (node != null && !visited[node.nodenum]) {
                node.deep = deep + 1;
                //可能字符、序号入数组
                if (node.edge == -2) {
                    charstrings.Add(node.charsetstring);
                }
                else if (node.edge >= 0) {
                    chars.Add(node.edge);
                }
                nfanodes[node.nodenum] = node;

                visited[node.nodenum] = true;
                node.deepnum = ++deeps[node.deep];

                _DFS(node.next1, node.deep);
                _DFS(node.next2, node.deep);
            }
        }
        //作图
        public static void DrawNfaPair(Canvas cv, NfaPair root) {
            nfanodes = new NfaNode[NfaNode.totalNfaNode];
            visited = new bool[NfaNode.totalNfaNode];
            deeps = new int[NfaNode.totalNfaNode + 1];
            for (int i = 0; i < visited.Count(); i++) {
                visited[i] = false;
                deeps[i] = 0;
            }
            DFS(root.start);
            for (int i = 0; i < visited.Count(); i++)
                visited[i] = false;
            _DrawNfaPair(cv, root.start);
        }
        private static void _DrawNfaPair(Canvas cv, NfaNode node) {
            if (node != null && !visited[node.nodenum]) {
                visited[node.nodenum] = true;
                //画当前node的圆
                var circle = GetCircle(node.deep * distanceX, node.deepnum * distanceY);
                cv.Children.Add(circle);
                cv.Children.Add(GetNumTextBlock(
                    node.deep * distanceX, node.deepnum * distanceY, node.nodenum.ToString()));

                //补上线、文字，同时递归
                string text = "";
                if (node.next1 != null) {
                    //线
                    var path = GetNext1Path(node.deep * distanceX, node.deepnum * distanceY,
                        node.next1.deep * distanceX, node.next1.deepnum * distanceY);
                    cv.Children.Add(path);

                    //文字
                    if (node.edge == -1) text = "ε";
                    else if (node.edge == -2) text = "[" + node.charsetstring + "]";
                    else text = ((char)node.edge).ToString();
                    var tb = GetTextBlock(node.deep * distanceX, node.deepnum * distanceY,
                        node.next1.deep * distanceX, node.next1.deepnum * distanceY, text);
                    cv.Children.Add(tb);
                    _DrawNfaPair(cv, node.next1);
                }
                if (node.next2 != null) {
                    //线
                    var path = GetNext2Path(node.deep * distanceX, node.deepnum * distanceY,
                        node.next2.deep * distanceX, node.next2.deepnum * distanceY);
                    cv.Children.Add(path);

                    //文字
                    var tb = GetTextBlock(node.deep * distanceX, node.deepnum * distanceY,
                        node.next2.deep * distanceX, node.next2.deepnum * distanceY, text);
                    cv.Children.Add(tb);
                    _DrawNfaPair(cv, node.next2);
                }

            }
        }
        //获取文字
        private static TextBlock GetTextBlock(int x1, int y1, int x2, int y2, string text) {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            if (text != "ε")
                tb.Foreground = Brushes.Red;
            else tb.Foreground = Brushes.Black;
            if (y1 == y2) {
                if (x1 + distanceX < x2)
                    tb.Margin = new Thickness(x1 + distanceX / 3, y1 - 2 * distanceY / 3, 0, 0);
                else if (x1 < x2) tb.Margin = new Thickness(x1 + distanceX / 3, y1 - distanceY / 3, 0, 0);
                else tb.Margin = new Thickness(x1 - distanceX / 3, y1 - 2 * distanceY / 3, 0, 0);
            }
            else if (y1 + 20 < y2)
                tb.Margin = new Thickness(x1 + radius, y1 + distanceY / 3, 0, 0);
            else
                tb.Margin = new Thickness(x1 + radius, y1 - distanceY / 3, 0, 0);
            return tb;
        }
        //获取序号文字
        private static TextBlock GetNumTextBlock(int x1, int y1, string text) {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.Foreground = Brushes.Black;
            tb.Margin = new Thickness(x1 - 5, y1 - 5, 0, 0);
            return tb;
        }
        //获取圆形
        private static Ellipse GetCircle(int x, int y) {
            Ellipse circle = new Ellipse();
            circle.StrokeThickness = 2;
            circle.Stroke = Brushes.Black;
            circle.Width = 2 * radius;
            circle.Height = 2 * radius;
            circle.Margin = new Thickness(x - radius, y - radius, 0, 0);
            return circle;
        }
        //获取路径2
        //坐标1、2均为圆心，自动解决弯曲情况
        private static Path GetNext2Path(int x1, int y1, int x2, int y2) {
            y1 -= radius;
            y2 -= radius;
            Path p = new Path();
            p.Stroke = Brushes.Black;
            String data;
            //由于浮点转整数的误差，y1并不精确小于y2
            if (y1 + 20 < y2) {
                x2 -= radius;
                y2 += radius;
                y1 += 2 * radius;
                int oft = (x2 - x1) / 3, bzx1 = x1 + oft, bzx2 = x1 + oft * 2,
                    bzy = y2,
                    arrowx1 = x2 - 5, arrowy1 = y2 - 5,
                    arrowx2 = x2 - 8, arrowy2 = y2 + 10;
                data = "M" + x1 + "," + y1 +//起始点
                    " C" + bzx1 + "," + bzy + " " +//3分1点上拉
                    bzx2 + "," + bzy + " " +//3分2点上拉
                    x2 + "," + y2 +//终点
                    " L " + arrowx1 + "," + arrowy1 +
                    " L " + x2 + "," + y2 +
                    " L " + arrowx2 + "," + arrowy2;
            }
            else {
                int oft = (x2 - x1) / 3, bzx1 = x1 + oft, bzx2 = x1 + oft * 2,
                    bzy = (y1 + y2 - 50) / 2,
                    arrowx1 = x2, arrowx2 = x2 - 12,
                    arrowy1 = y2 - 5, arrowy2 = y2;
                if (x2 < x1) {
                    arrowx2 = x2 + 12;
                }
                data = "M" + x1 + "," + y1 +//起始点
                    " C" + bzx1 + "," + bzy + " " +//3分1点上拉
                    bzx2 + "," + bzy + " " +//3分2点上拉
                    x2 + "," + y2 +//终点
                    " L " + arrowx1 + "," + arrowy1 +
                    " L " + x2 + "," + y2 +
                    " L " + arrowx2 + "," + arrowy2;
            }
            p.Data = Geometry.Parse(data);
            return p;
        }
        //获取路径1
        //坐标1、2均为圆心，自动解决弯曲情况
        private static Path GetNext1Path(int x1, int y1, int x2, int y2) {
            Path p = new Path();
            p.Stroke = Brushes.Black;
            String data;
            if (y1 > y2 + 20) {
                x1 += radius;
                y2 += radius;
                int oft = (x2 - x1) / 3, bzx1 = x1 + oft, bzx2 = x1 + oft * 2,
                    bzy = y1,
                    arrowx1 = x2 - 5, arrowy1 = y2,
                    arrowx2 = x2, arrowy2 = y2 + 10;
                data = "M" + x1 + "," + y1 +//起始点
                    " C" + bzx1 + "," + bzy + " " +//3分1点拉
                    bzx2 + "," + bzy + " " +//3分2点拉
                    x2 + "," + y2 +//终点
                    " L " + arrowx1 + "," + arrowy1 +
                    " L " + x2 + "," + y2 +
                    " L " + arrowx2 + "," + arrowy2;
            }
            else if (y1 + 20 < y2) {
                y1 += radius;
                x2 -= radius;
                int oft = (x2 - x1) / 3, bzx1 = x1 + oft, bzx2 = x1 + oft * 2,
                    bzy = y2,
                    arrowx1 = x2 - 5, arrowy1 = y2 - 5,
                    arrowx2 = x2 - 8, arrowy2 = y2 + 10;
                data = "M" + x1 + "," + y1 +//起始点
                    " C" + bzx1 + "," + bzy + " " +//3分1点上拉
                    bzx2 + "," + bzy + " " +//3分2点上拉
                    x2 + "," + y2 +//终点
                    " L " + arrowx1 + "," + arrowy1 +
                    " L " + x2 + "," + y2 +
                    " L " + arrowx2 + "," + arrowy2;
            }
            else {
                x1 += radius;
                x2 -= radius;
                data = "M" + x1 + "," + y1 +//起始点
                    " L " + x2 + "," + y2 +
                    " L" + (x2 - 5) + "," + (y2 - 5) +
                    " L " + x2 + "," + y2 +
                    " L" + (x2 - 8) + "," + (y2 + 10);
            }
            p.Data = Geometry.Parse(data);
            return p;
        }



        //重置
        public static void Clear() {
            NfaNode.totalNfaNode = 0;
            chars = new HashSet<int>();
            charstrings = new HashSet<string>();
        }
    }
}
