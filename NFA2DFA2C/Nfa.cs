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
        public HashSet<byte> charset;//字符集
        public string charsetstring;//字符集对应的正则字符串
        public int nodenum;//节点编号
        public int deep;//节点深度
        public int deepnum;//depp深度上的第几个
    }

    public class NfaPair {
        public NfaNode start;
        public NfaNode end;
    }

    public class NfaManager {
        //辅助变量
        public static NfaNode[] nfanodes;
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
            return np;
        }
        //由字符串表示的字符集构建单个自动机
        private static NfaPair CreatNfaPair(string charsetstring) {
            NfaPair np = new NfaPair();
            bool isneg;
            int startpos;
            if (charsetstring[0] == '^') {
                isneg = true;
                startpos = 1;
            }
            else {
                isneg = false;
                startpos = 0;
            }

            NfaNode s = new NfaNode(), e = new NfaNode();
            s.edge = -2;
            s.charsetstring = charsetstring;
            HashSet<byte> set = new HashSet<byte>();
            int len = charsetstring.Length;
            //加入集合
            for (int i = startpos; i < len; i++) {
                //预读下一字符，如果为连字符则
                if ((i + 1) < len && charsetstring[i + 1] == '-') {
                    for (int j = charsetstring[i]; j <= charsetstring[i + 2]; j++)
                        set.Add((byte)j);
                    i += 2;
                }
                else set.Add((byte)charsetstring[i]);
            }
            //取反标准ASCII的128个字符
            if (isneg) {
                HashSet<byte> temp = new HashSet<byte>();
                for (byte i = 0; i <= 127; i++) {
                    if (!set.Contains(i))
                        temp.Add(i);
                }
                set = temp;
            }
            s.charset = set;
            s.next1 = e;

            np.start = s;
            np.end = e;
            return np;
        }
        //连接两个自动机
        private static NfaPair CombineNfaPair(NfaPair before, NfaPair after) {
            before.end.edge = -1;
            before.end.next1 = after.start;
            before.end = after.end;
            return before;
        }
        //形成|或运算自动机
        private static NfaPair EitherNfaPair(NfaPair np1, NfaPair np2) {
            NfaPair np = new NfaPair();
            np.start = new NfaNode();
            np.end = new NfaNode();

            np.start.edge = -1;
            np.start.next1 = np1.start;
            np.start.next2 = np2.start;

            np1.end.edge = -1;
            np1.end.next1 = np.end;
            np2.end.edge = -1;
            np2.end.next1 = np.end;

            return np;
        }
        //形成*闭包自动机
        private static NfaPair DoStarCollapse(NfaPair nfaPair) {
            NfaPair np = new NfaPair();
            NfaNode before = new NfaNode(), after = new NfaNode();
            np.start = before;
            np.end = after;
            //前面连接
            before.next1 = nfaPair.start;
            before.edge = -1;
            //后面连接
            nfaPair.end.next1 = after;
            nfaPair.end.edge = -1;
            //原来尾连头，现在头连尾
            nfaPair.end.next2 = nfaPair.start;
            before.next2 = after;

            return np;
        }
        //形成+闭包自动机
        private static NfaPair DoPosCollapse(NfaPair nfaPair) {
            NfaPair np = new NfaPair();
            NfaNode before = new NfaNode(), after = new NfaNode();
            np.start = before;
            np.end = after;
            //前面连接
            before.next1 = nfaPair.start;
            before.edge = -1;
            //后面连接
            nfaPair.end.next1 = after;
            nfaPair.end.edge = -1;
            //原来尾连头
            nfaPair.end.next2 = nfaPair.start;

            return np;
        }
        //形成?闭包自动机
        private static NfaPair DoQuestCollapse(NfaPair nfaPair) {
            NfaPair np = new NfaPair();
            NfaNode before = new NfaNode(), after = new NfaNode();
            np.start = before;
            np.end = after;
            //前面连接
            before.next1 = nfaPair.start;
            before.edge = -1;
            //后面连接
            nfaPair.end.next1 = after;
            nfaPair.end.edge = -1;
            //现在头连尾
            before.next2 = after;

            return np;
        }
        //简单情况，形成只有+?*的闭包、字符、[]字符集的自动机
        private static NfaPair EasyCondition(string pattern) {
            int len = pattern.Length;
            if (len == 0) return null;

            //开始处理第一个
            NfaPair root;
            List<NfaPair> list = new List<NfaPair>();
            int pos;
            if (pattern[0] == '[') {
                pos = -1;
            }
            else {
                root = CreatNfaPair(pattern[0]);
                pos = 0;
                list.Add(root);
            }
            for (int i = pos + 1; i < len; i++) {
                switch (pattern[i]) {
                    case '*': list[pos] = DoStarCollapse(list[pos]); break;
                    case '+': list[pos] = DoPosCollapse(list[pos]); break;
                    case '?': list[pos] = DoQuestCollapse(list[pos]); break;
                    case '[':
                        int j = i;
                        while (pattern[j] != ']') {
                            ++j;
                        }
                        list.Add(CreatNfaPair(pattern.Substring(i + 1, j - i - 1)));
                        i = j;
                        ++pos;
                        break;
                    default:
                        list.Add(CreatNfaPair(pattern[i]));
                        ++pos;
                        break;
                }
            }
            root = list[0];
            for (int i = 1; i <= pos; i++) {
                root = CombineNfaPair(root, list[i]);
            }
            return root;
        }
        //普通情况，在简单的基础上增加|的处理
        public static NfaPair NormalCondition(string pattern) {
            if (pattern.Length == 0) return null;
            string[] patterns = pattern.Split('|');
            int len = patterns.Length;
            int[] pattnodes = new int[len];
            NfaPair[] nps = new NfaPair[len];
            for (int i = 0; i < len; i++)
                nps[i] = EasyCondition(patterns[i]);
            //由长到短
            for (int i = 0; i < len; i++) {
                pattnodes[i] = 0;
                for (int j = 0; j < patterns[i].Length; j++) {
                    if (patterns[i][j] == '[') {
                        while (patterns[i][j] != ']') {
                            j++;
                        }
                        pattnodes[i] += 2;
                    }
                    else if (patterns[i][j] == '*' || patterns[i][j] == '+' || patterns[i][j] == '?')
                        pattnodes[i] += 4;
                    else pattnodes[i] += 2;
                }
            }
            //从大到小排序调整
            for (int i = 0; i < len; i++) {
                for (int j = i + 1; j < len; j++) {
                    if (pattnodes[i] < pattnodes[j]) {
                        int t = pattnodes[i];
                        pattnodes[i] = pattnodes[j];
                        pattnodes[j] = t;

                        NfaPair np = nps[i];
                        nps[i] = nps[j];
                        nps[j] = np;
                    }
                }
            }

            for (int i = 1; i < len; i++) {
                nps[0] = EitherNfaPair(nps[0], nps[i]);
            }
            return nps[0];
        }
        //困难情况，在普通的基础上增加()的处理
        public static NfaPair HardCondition(string pattern) {
            int len = pattern.Length;
            if (len == 0) return null;//空字符串抛出null
            NfaPair root;

            //获取最内括号对
            int left = len - 1, right;
            while (left >= 0 && pattern[left] != '(')
                --left;
            //没有'('
            if (left < 0) return NormalCondition(pattern);
            //搜索')'，形成最初root
            right = left;
            while (pattern[right] != ')')
                ++right;
            root = NormalCondition(pattern.Substring(left + 1, right - left - 1));

            //左右两侧搜索
            int p = left - 1, q = right + 1;
            NfaPair temp;
            while (left >= 0) {
                //左边
                while (p >= 0 && pattern[p] != '(')
                    --p;
                temp = NormalCondition(pattern.Substring(p + 1, left - p - 1));
                if (temp != null)
                    root = CombineNfaPair(temp, root);
                left = p--;

                //右边
                while (q < len && pattern[q] != ')')
                    q++;
                temp = NormalCondition(pattern.Substring(right + 1, q - right - 1));
                if (temp != null)
                    root = CombineNfaPair(root, temp);
                //预读一个判断闭包
                if (q + 1 < len) {
                    switch (pattern[q+1]) {
                        case '*':
                            root = DoStarCollapse(root);
                            ++q;
                            break;
                        case '+':
                            root = DoPosCollapse(root);
                            ++q;
                            break;
                        case '?':
                            root = DoQuestCollapse(root);
                            ++q;
                            break;
                    }
                }
                right = q++;
            }
            return root;
        }



        //作图相关
        private static int radius = 15;//节点圆形半径
        private static int distanceX = 60;//节点横向基本距离
        private static int distanceY = 50;//节点纵向基本距离
        //遍历一次树，计算深度、同深度节点数量，重置交由DrawNfaPair处理
        private static void DFS(NfaNode root) {
            _DFS(root, 0);
        }
        private static void _DFS(NfaNode node, int deep) {
            if (node != null && !visited[node.nodenum]) {
                node.deep = deep + 1;
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



        //初始化
        public static void Clear() {
            NfaNode.totalNfaNode = 0;
        }
    }
}
