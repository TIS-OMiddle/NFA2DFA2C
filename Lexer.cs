using System;

public class Lexer {
    private string expression;
    private int start;
    private int pos;
    public void setExpression(string str) {
        expression = str;
        start = 0;
        pos = 0;
    }

    public Lexer() {

    }
}
