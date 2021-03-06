﻿public class ForEachNode
{
    public Token element_var_name, list_var_name;
    public object body_node;
    public bool should_return_null;
    public Position pos_start, pos_end;

    public ForEachNode(Token element_var_name, Token list_var_name, object body_node, bool should_return_null)
    {
        this.element_var_name = element_var_name;
        this.list_var_name = list_var_name;
        this.body_node = body_node;
        this.should_return_null = should_return_null;
        this.pos_start = element_var_name.pos_start;
        this.pos_end = (Position) body_node.GetType().GetField("pos_end").GetValue(body_node);
    }
}