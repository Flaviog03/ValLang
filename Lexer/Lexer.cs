﻿using System.Collections.Generic;
using System;

public class Lexer
{
    public string text, fn;
    public Position pos;
    public char current_char;

    public Lexer(string fn, string text)
    {
        this.fn = fn;
        this.text = text;
        this.pos = new Position(-1, 0, -1, fn, text);
        this.current_char = default(char);
        this.advance();
    }

    public void advance()
    {
        this.pos.advance(this.current_char);
        this.current_char = this.pos.idx < this.text.Length ? this.text[this.pos.idx] : default(char);
    }

    public Tuple<List<Token>, Error> make_tokens()
    {
        List<Token> tokens = new List<Token>();

        while (this.current_char != default(char))
        {
            if (this.current_char == ' ' || this.current_char == '\t')
            {
                this.advance();
            }
            else if (this.current_char == '#')
            {
                this.skip_comment();
            }
            else if (this.current_char == '\n' || this.current_char == ';')
            {
                tokens.Add(new Token("NEWLINE", pos_start: this.pos));
                this.advance();
            }
            else if (this.current_char == '+')
            {
                Position pos_start = this.pos.copy();
                this.advance();
                string tok_type = "PLUS";

                if (this.current_char == '=')
                {
                    tok_type += "_EQ";
                    this.advance();
                }

                tokens.Add(new Token(tok_type, pos_start: this.pos, pos_end: this.pos));
            }
            else if (this.current_char == '-')
            {
                tokens.Add(this.make_minus_or_arrow());
            }
            else if (this.current_char == '*')
            {
                Position pos_start = this.pos.copy();
                this.advance();
                string tok_type = "MUL";

                if (this.current_char == '=')
                {
                    tok_type += "_EQ";
                    this.advance();
                }

                tokens.Add(new Token(tok_type, pos_start: this.pos, pos_end: this.pos));
            }
            else if (this.current_char == '/')
            {
                Position pos_start = this.pos.copy();
                this.advance();
                string tok_type = "DIV";

                if (this.current_char == '=')
                {
                    tok_type += "_EQ";
                    this.advance();
                }
                else if (this.current_char == '/')
                {
                    this.skip_comment();
                    continue;
                }
                else if (this.current_char == '*')
                {
                    this.skip_multi_line_comment();
                    continue;
                }

                tokens.Add(new Token(tok_type, pos_start: this.pos, pos_end: this.pos));
            }
            else if (this.current_char == '^')
            {
                Position pos_start = this.pos.copy();
                this.advance();
                string tok_type = "POW";

                if (this.current_char == '=')
                {
                    tok_type += "_EQ";
                    this.advance();
                }

                tokens.Add(new Token(tok_type, pos_start: this.pos, pos_end: this.pos));
            }
            else if (this.current_char == '(')
            {
                tokens.Add(new Token("LPAREN", pos_start: this.pos));
                this.advance();
            }
            else if (this.current_char == ':')
            {
                tokens.Add(new Token("COLON", pos_start: this.pos));
                this.advance();
            }
            else if (this.current_char == ')')
            {
                tokens.Add(new Token("RPAREN", pos_start: this.pos));
                this.advance();
            }
            else if (this.current_char == '[')
            {
                tokens.Add(new Token("LSQUARE", pos_start: this.pos));
                this.advance();
            }
            else if (this.current_char == ']')
            {
                tokens.Add(new Token("RSQUARE", pos_start: this.pos));
                this.advance();
            }
            else if ("0123456789".Contains(this.current_char.ToString()))
            {
                tokens.Add(this.make_number());
            }
            else if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(this.current_char.ToString()))
            {
                tokens.Add(this.make_identifier());
            }
            else if (this.current_char == '!')
            {
                tokens.Add(this.make_not_or_not_equals());
            }
            else if (this.current_char == '=')
            {
                tokens.Add(this.make_equals());
            }
            else if (this.current_char == '<')
            {
                tokens.Add(this.make_less_than());
            }
            else if (this.current_char == '>')
            {
                tokens.Add(this.make_greater_than());
            }
            else if (this.current_char == ',')
            {
                tokens.Add(new Token("COMMA", pos_start: this.pos));
                this.advance();
            }
            else if (this.current_char == '"')
            {
                tokens.Add(this.make_string('"'));
            }
            else if (this.current_char.ToString() == "'")
            {
                tokens.Add(this.make_string(char.Parse("'")));
            }
            else if (this.current_char == '&')
            {
                Tuple<Token, Error> result = this.make_and();

                if (result.Item2 != null)
                {
                    return new Tuple<List<Token>, Error>(null, result.Item2);
                }

                tokens.Add(result.Item1);
            }
            else if (this.current_char == '|')
            {
                Tuple<Token, Error> result = this.make_or();

                if (result.Item2 != null)
                {
                    return new Tuple<List<Token>, Error>(null, result.Item2);
                }

                tokens.Add(result.Item1);
            }
            else if (this.current_char == '{')
            {
                tokens.Add(new Token("LBRACE", pos_start: this.pos));
                tokens.Add(new Token("NEWLINE", pos_start: this.pos));

                this.advance();
            }
            else if (this.current_char == '}')
            {
                tokens.Add(new Token("RBRACE", pos_start: this.pos));
                tokens.Add(new Token("NEWLINE", pos_start: this.pos));

                this.advance();
            }
            else if (this.current_char == '~')
            {
                Position pos_start = this.pos.copy();
                this.advance();
                string tok_type = "LOGIC_NOT";

                if (this.current_char == '=')
                {
                    tok_type += "_EQ";
                    this.advance();
                }

                tokens.Add(new Token(tok_type, pos_start: this.pos, pos_end: this.pos));
            }
            else if (this.current_char == '%')
            {
                Position pos_start = this.pos.copy();
                this.advance();
                string tok_type = "MODULO";

                if (this.current_char == '=')
                {
                    tok_type += "_EQ";
                    this.advance();
                }

                tokens.Add(new Token(tok_type, pos_start: this.pos, pos_end: this.pos));
            }
            else if (this.current_char == '.')
            {
                tokens.Add(new Token("DOT", pos_start: this.pos));
                this.advance();
            }
            else
            {
                Position pos_start = this.pos.copy();

                char theChar = this.current_char;
                this.advance();

                return new Tuple<List<Token>, Error>(null, new IllegalCharError(pos_start, this.pos, "'" + theChar + "'"));
            }
        }

        tokens.Add(new Token("EOF", pos_start: this.pos));
        return new Tuple<List<Token>, Error>(tokens, null);
    }

    public Tuple<Token, Error> make_and()
    {
        Position pos_start = this.pos.copy();
        this.advance();

        if (this.current_char == '&')
        {
            this.advance();
            return new Tuple<Token, Error>(new Token("KEYWORD", "and", pos_start, this.pos), null);
        }
        else if (this.current_char == '=')
        {
            this.advance();
            return new Tuple<Token, Error>(new Token("LOGIC_AND_EQ", pos_start: pos_start, pos_end: this.pos), null);
        }
        else
        {
            return new Tuple<Token, Error>(new Token("LOGIC_AND", pos_start: pos_start, pos_end: this.pos), null);
        }

        return new Tuple<Token, Error>(null, new ExpectedCharError(pos_start, this.pos, "Expected '&'"));
    }

    public Tuple<Token, Error> make_or()
    {
        Position pos_start = this.pos.copy();
        this.advance();

        if (this.current_char == '|')
        {
            this.advance();
            return new Tuple<Token, Error>(new Token("KEYWORD", "or", pos_start, this.pos), null);
        }
        else if (this.current_char == '=')
        {
            this.advance();
            return new Tuple<Token, Error>(new Token("LOGIC_OR_EQ", pos_start: pos_start, pos_end: this.pos), null);
        }
        else
        {
            return new Tuple<Token, Error>(new Token("LOGIC_OR", pos_start: pos_start, pos_end: this.pos), null);
        }

        return new Tuple<Token, Error>(null, new ExpectedCharError(pos_start, this.pos, "Expected '|'"));
    }

    public Token make_number()
    {
        string num_str = "";
        int dot_count = 0;
        Position pos_start = this.pos.copy();

        while (this.current_char != default(char) && ("0123456789.").Contains(this.current_char.ToString()))
        {
            if (this.current_char == '.')
            {
                if (dot_count == 1)
                {
                    break;
                }

                dot_count += 1;
                num_str += ".";
            }
            else
            {
                num_str += this.current_char;
            }

            this.advance();
        }

        if (dot_count == 0)
        {
            return new Token("INT", int.Parse(num_str), pos_start, this.pos);
        }
        else
        {
            return new Token("FLOAT", float.Parse(num_str.Replace(".", ",")), pos_start, this.pos);
        }
    }

    public Token make_identifier()
    {
        string id_str = "";
        Position pos_start = this.pos.copy();

        while (this.current_char != default(char) && ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_").Contains(this.current_char.ToString()))
        {
            id_str += this.current_char;
            this.advance();
        }

        string tok_type = "IDENTIFIER";

        foreach (string keyword in new string[] { "var", "and", "or", "not", "if", "then", "elif", "else", "for", "to", "step", "while", "fun", "end", "return", "continue", "break", "del", "do", "foreach", "in", "switch", "case", "default", "const", "struct" })
        {
            if (id_str == keyword)
            {
                tok_type = "KEYWORD";
                break;
            }
        }

        return new Token(tok_type, id_str, pos_start, this.pos);
    }

    public Token make_equals()
    {
        string tok_type = "EQ";
        Position pos_start = this.pos.copy();

        this.advance();

        if (this.current_char == '=')
        {
            this.advance();
            tok_type = "EE";
        }

        return new Token(tok_type, pos_start: pos_start, pos_end: this.pos);
    }

    public Token make_less_than()
    {
        string tok_type = "LT";
        Position pos_start = this.pos.copy();

        this.advance();

        if (this.current_char == '=')
        {
            this.advance();
            tok_type += "E";
        }
        else if (this.current_char == '<')
        {
            this.advance();
            tok_type = "LEFT_SHIFT";

            if (this.current_char == '=')
            {
                this.advance();
                tok_type += "_EQ";
            }
        }

        return new Token(tok_type, pos_start: pos_start, pos_end: this.pos);
    }

    public Token make_greater_than()
    {
        string tok_type = "GT";
        Position pos_start = this.pos.copy();

        this.advance();

        if (this.current_char == '=')
        {
            this.advance();
            tok_type += "E";
        }
        else if (this.current_char == '>')
        {
            this.advance();
            tok_type = "RIGHT_SHIFT";

            if (this.current_char == '=')
            {
                this.advance();
                tok_type += "_EQ";
            }
        }

        return new Token(tok_type, pos_start: pos_start, pos_end: this.pos);
    }

    public Token make_minus_or_arrow()
    {
        string tok_type = "MINUS";
        Position pos_start = this.pos.copy();

        this.advance();

        if (this.current_char == '>')
        {
            this.advance();
            tok_type = "ARROW";
        }
        else if (this.current_char == '=')
        {
            this.advance();
            tok_type += "_EQ";
        }

        return new Token(tok_type, pos_start: pos_start, pos_end: this.pos);
    }

    public Token make_string(char conclude_char)
    {
        string str = "";
        Position pos_start = this.pos.copy();
        bool escape_character = false;

        this.advance();

        Dictionary<char, char> escape_characters = new Dictionary<char, char>();
        escape_characters.Add('n', '\n');
        escape_characters.Add('t', '\t');

        while (this.current_char != default(char) && (this.current_char != conclude_char || escape_character))
        {
            if (escape_character)
            {
                try
                {
                    str += escape_characters[this.current_char];
                }
                catch (Exception)
                {
                    str += this.current_char;
                }
            }
            else
            {
                if (this.current_char == '\\')
                {
                    escape_character = true;
                }
                else
                {
                    str += this.current_char;
                }
            }

            this.advance();
            escape_character = false;
        }

        this.advance();
        return new Token("STRING", str, pos_start, this.pos);
    }

    public void skip_comment()
    {
        this.advance();

        while (this.current_char != '\n' && this.current_char != default(char) && this.current_char != ';')
        {
            this.advance();
        }

        this.advance();
    }

    public void skip_multi_line_comment()
    {
        this.advance();
        bool firstChar = false;

        while (true)
        {
            this.advance();

            if (this.current_char == '*' && !firstChar)
            {
                firstChar = true;
                continue;
            }
            else if (this.current_char == '*' && firstChar)
            {
                firstChar = false;
            }
            if (firstChar)
            {
                if (this.current_char == '/')
                {
                    break;
                }
            }
        }

        this.advance();
    }

    public Token make_not_or_not_equals()
    {
        Position pos_start = this.pos.copy();
        this.advance();

        if (this.current_char == '=')
        {
            this.advance();

            return new Token("NE", pos_start: pos_start, pos_end: this.pos);
        }

        return new Token("KEYWORD", "not", pos_start, this.pos);
    }
}