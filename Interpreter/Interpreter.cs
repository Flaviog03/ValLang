﻿using System;
using System.Collections.Generic;

public class Interpreter
{
    public RuntimeResult visit(object node, Context context)
    {
        return (RuntimeResult)this.GetType().GetMethod("visit_" + node.GetType().Name).Invoke(this, new object[] { node, context });
    }

    public RuntimeResult visit_NumberNode(NumberNode node, Context context)
    {
        return new RuntimeResult().success(new NumberValue(node.tok.value).set_context(context).set_pos(node.pos_start, node.pos_end));
    }

    public RuntimeResult visit_BinOpNode(BinOpNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();
        object left = res.register(this.visit(node.left_node, context));

        if (res.should_return())
        {
            return res;
        }

        object right = res.register(this.visit(node.right_node, context));

        if (res.should_return())
        {
            return res;
        }

        Tuple<object, Error> result = null;

        if (node.op_tok.type == "PLUS")
        {
            result = (Tuple<object, Error>) left.GetType().GetMethod("added_to").Invoke(left, new object[] { right });
        }
        else if (node.op_tok.type == "MINUS")
        {
            result = (Tuple<object, Error>)left.GetType().GetMethod("subbed_by").Invoke(left, new object[] { right });
        }
        else if (node.op_tok.type == "MUL")
        {
            result = (Tuple<object, Error>)left.GetType().GetMethod("multed_by").Invoke(left, new object[] { right });
        }
        else if (node.op_tok.type == "MODULO")
        {
            result = (Tuple<object, Error>)left.GetType().GetMethod("moduled_by").Invoke(left, new object[] { right });
        }
        else if (node.op_tok.type == "EE")
        {
            result = (Tuple<object, Error>)left.GetType().GetMethod("get_comparison_eq").Invoke(left, new object[] { right });
        }
        else if (node.op_tok.type == "KEYWORD" && node.op_tok.value.ToString() == "and")
        {
            result = (Tuple<object, Error>)left.GetType().GetMethod("anded_by").Invoke(left, new object[] { right });
        }
        else if (node.op_tok.type == "KEYWORD" && node.op_tok.value.ToString() == "or")
        {
            result = (Tuple<object, Error>)left.GetType().GetMethod("ored_by").Invoke(left, new object[] { right });
        }
        else
        {
            if (left.GetType().GetMethod("get_comparison_" + node.op_tok.type.ToLower()) != null)
            {
                result = (Tuple<object, Error>)left.GetType().GetMethod("get_comparison_" + node.op_tok.type.ToLower()).Invoke(left, new object[] { right });
            }
            else
            {
                result = (Tuple<object, Error>)left.GetType().GetMethod(node.op_tok.type.ToLower() + "ed_by").Invoke(left, new object[] { right });
            }
        }

        if (result.Item2 != null)
        {
            return res.failure(result.Item2);
        }
        else
        {
            object realResult = result.Item1;
            realResult.GetType().GetMethod("set_pos").Invoke(realResult, new object[] { node.pos_start, node.pos_end });
            return res.success(realResult);
        }
    }

    public RuntimeResult visit_UnaryOpNode(UnaryOpNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();
        object number = res.register(this.visit(node.node, context));

        if (res.should_return())
        {
            return res;
        }

        Error error = null;

        if (node.op_tok.type == "MINUS")
        {
            number = (Tuple<object, Error>) number.GetType().GetMethod("multed_by").Invoke(number, new object[] { new NumberValue(-1) });
            error = (Error) number.GetType().GetProperty("Item2").GetValue(number);
        }
        else if (node.op_tok.type == "PLUS")
        {
            number = (Tuple<object, Error>)number.GetType().GetMethod("multed_by").Invoke(number, new object[] { new NumberValue(1) });
            error = (Error)number.GetType().GetProperty("Item2").GetValue(number);
        }
        else if (node.op_tok.type == "KEYWORD" && node.op_tok.value.ToString() == "not")
        {
            number = (Tuple<object, Error>)number.GetType().GetMethod("notted").Invoke(number, new object[] { });
            error = (Error)number.GetType().GetProperty("Item2").GetValue(number);
        }
        else if (node.op_tok.type == "LOGIC_NOT")
        {
            number = (Tuple<object, Error>)number.GetType().GetMethod("logic_notted").Invoke(number, new object[] { });
            error = (Error)number.GetType().GetProperty("Item2").GetValue(number);
        }

        if (error != null)
        {
            return res.failure(error);
        }
        else
        {
            object realNumber = number.GetType().GetProperty("Item1").GetValue(number);
            realNumber.GetType().GetMethod("set_pos").Invoke(realNumber, new object[] { node.pos_start, node.pos_end });
            return res.success(realNumber);
        }
    }

    public RuntimeResult visit_VarAccessNode(VarAccessNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        object var_name = node.var_name_tok.value;
        object value = context.symbol_table.get(var_name);

        if (value == null)
        {
            return res.failure(new RuntimeError(node.pos_start, node.pos_end, "'" + var_name.ToString() + "' is not defined", context));
        }

        return res.success(value);
    }

    public RuntimeResult visit_VarAssignNode(VarAssignNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        object var_name = node.var_name_tok.value;
        object value = res.register(this.visit(node.value_node, context));

        if (res.should_return())
        {
            return res;
        }

        if (value.GetType() == typeof(StructValue))
        {
            value = new StructValue(((StructValue)value).name, ((StructValue)value).statements).set_context(context).set_pos(node.pos_start, node.pos_end).declare(this);
        }

        if (context.symbol_table.can_be_rewrite(var_name))
        {
            context.symbol_table.set(var_name, value, node.rewritten);
        }
        else
        {
            return res.failure(new RuntimeError(node.pos_start, node.pos_end, "Can not access to constants variables", context));
        }

        value = value.GetType().GetMethod("copy").Invoke(value, new object[] { });
        value.GetType().GetMethod("set_pos").Invoke(value, new object[] { node.pos_start, node.pos_end });

        return res.success(value);
    }

    public RuntimeResult visit_VarReAssignNode(VarReAssignNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        object var_name = node.var_name_tok.value;
        object value = null;

        if (node.value_node != null)
        {
            value = res.register(this.visit(node.value_node, context));

            if (res.should_return())
            {
                return res;
            }
        }

        if (context.symbol_table.present(var_name))
        {
            if (!context.symbol_table.can_be_rewrite(var_name))
            {
                return res.failure(new RuntimeError(node.pos_start, node.pos_end, "Can not access to constants variables", context));
            }

            object actualValue = context.symbol_table.get(var_name);

            if (node.op_tok.type == "EQ")
            {
                context.symbol_table.set(var_name, value);
            }
            else if (node.op_tok.type == "PLUS_EQ")
            {
                Tuple<object, Error> result = (Tuple<object, Error>)actualValue.GetType().GetMethod("added_to").Invoke(actualValue, new object[] { value });
                value = result.Item1;
                context.symbol_table.set(var_name, value);
            }
            else if (node.op_tok.type == "MINUS_EQ")
            {
                Tuple<object, Error> result = (Tuple<object, Error>)actualValue.GetType().GetMethod("subbed_by").Invoke(actualValue, new object[] { value });
                value = result.Item1;
                context.symbol_table.set(var_name, value);
            }
            else if (node.op_tok.type == "MUL_EQ")
            {
                Tuple<object, Error> result = (Tuple<object, Error>)actualValue.GetType().GetMethod("multed_by").Invoke(actualValue, new object[] { value });
                value = result.Item1;
                context.symbol_table.set(var_name, value);
            }
            else if (node.op_tok.type == "LOGIC_NOT_EQ")
            {
                Tuple<object, Error> result = (Tuple<object, Error>)value.GetType().GetMethod("logic_notted").Invoke(value, new object[] { });
                value = result.Item1;
                context.symbol_table.set(var_name, value);
            }
            else if (node.op_tok.type == "MODULO_EQ")
            {
                Tuple<object, Error> result = (Tuple<object, Error>)actualValue.GetType().GetMethod("moduled_by").Invoke(actualValue, new object[] { value });
                value = result.Item1;
                context.symbol_table.set(var_name, value);
            }
            else
            {
                Tuple<object, Error> result = (Tuple<object, Error>)actualValue.GetType().GetMethod(node.op_tok.type.ToLower().Replace("_eq", "ed_by")).Invoke(actualValue, new object[] { value });
                value = result.Item1;
                context.symbol_table.set(var_name, value);
            }

            value = value.GetType().GetMethod("copy").Invoke(value, new object[] { });
            value.GetType().GetMethod("set_pos").Invoke(value, new object[] { node.pos_start, node.pos_end });
            return res.success(value);
        }

        return res.failure(new RuntimeError(node.pos_start, node.pos_end, "Variable is not created", context));
    }
    public RuntimeResult visit_DeleteNode(DeleteNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        object var_name = node.var_name_tok.value;

        if (context.symbol_table.present(var_name))
        {
            context.symbol_table.remove(var_name);

            return res.success(Values.NULL);
        }

        return res.failure(new RuntimeError(node.pos_start, node.pos_end, "Variable is not created", context));
    }

    public RuntimeResult visit_IfNode(IfNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        foreach (Tuple<object, object, bool> conditionExpr in node.cases)
        {
            object condition = conditionExpr.Item1;
            object expr = conditionExpr.Item2;
            bool should_return_null = conditionExpr.Item3;
            object condition_value = res.register(this.visit(condition, context));

            if (res.should_return())
            {
                return res;
            }

            if ((bool)condition_value.GetType().GetMethod("is_true").Invoke(condition_value, new object[] { }) == true)
            {
                object expr_value = res.register(this.visit(expr, context));

                if (res.should_return())
                {
                    return res;
                }

                return res.success(should_return_null ? Values.NULL : expr_value);
            }
        }

        if (node.else_case != null)
        {
            Tuple<object, bool> allElse = node.else_case;

            object expr = allElse.Item1;
            bool should_return_null = allElse.Item2;
            object else_value = res.register(this.visit(expr, context));

            if (res.should_return())
            {
                return res;
            }

            return res.success(should_return_null ? Values.NULL : else_value);
        }

        return res.success(Values.NULL);
    }

    public RuntimeResult visit_ForNode(ForNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();
        NumberValue start_value = (NumberValue) res.register(this.visit(node.start_value_node, context));

        if (res.should_return())
        {
            return res;
        }

        NumberValue end_value = (NumberValue)res.register(this.visit(node.end_value_node, context));

        if (res.should_return())
        {
            return res;
        }

        NumberValue step_value = new NumberValue(1);

        if (node.step_value_node != null)
        {
            step_value = (NumberValue) res.register(this.visit(node.step_value_node, context));
        }

        int i = (int) start_value.value;

        if ((int) step_value.value >= 0)
        {
            while (i < (int) end_value.value)
            {
                context.symbol_table.set(node.var_name_tok.value, new NumberValue(i));
                i += (int) step_value.value;
                object value = res.register(this.visit(node.body_node, context));

                if (res.should_return() && !res.loop_should_continue && !res.loop_should_break)
                {
                    return res;
                }

                if (res.loop_should_continue)
                {
                    continue;
                }

                if (res.loop_should_break)
                {
                    break;
                }
            }
        }    
        else
        {
            while (i > (int)end_value.value)
            {
                context.symbol_table.set(node.var_name_tok.value, new NumberValue(i));
                i += (int)step_value.value;
                object value = res.register(this.visit(node.body_node, context));

                if (res.should_return() && !res.loop_should_continue && !res.loop_should_break)
                {
                    return res;
                }

                if (res.loop_should_continue)
                {
                    continue;
                }

                if (res.loop_should_break)
                {
                    break;
                }
            }
        }

        return res.success(Values.NULL);
    }

    public RuntimeResult visit_WhileNode(WhileNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        while (true)
        {
            NumberValue condition = (NumberValue) res.register(this.visit(node.condition_node, context));

            if (res.should_return())
            {
                return res;
            }

            if (!condition.is_true())
            {
                break;
            }

            object value = res.register(this.visit(node.body_node, context));

            if (res.should_return() && !res.loop_should_continue && !res.loop_should_break)
            {
                return res;
            }
            if (res.loop_should_continue)
            {
                continue;
            }

            if (res.loop_should_break)
            {
                break;
            }
        }

        return res.success(Values.NULL);
    }

    public RuntimeResult visit_DoWhileNode(DoWhileNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();
        bool firstTime = true;

        while (true)
        {
            NumberValue condition = (NumberValue)res.register(this.visit(node.condition_node, context));

            if (res.should_return())
            {
                return res;
            }

            if (!firstTime)
            {
                if (!condition.is_true())
                {
                    break;
                }
            }
            else
            {
                firstTime = false;
            }

            object value = res.register(this.visit(node.body_node, context));

            if (res.should_return() && !res.loop_should_continue && !res.loop_should_break)
            {
                return res;
            }

            if (res.loop_should_continue)
            {
                continue;
            }

            if (res.loop_should_break)
            {
                break;
            }
        }

        return res.success(Values.NULL);
    }

    public RuntimeResult visit_FuncDefNode(FuncDefNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        string func_name = node.var_name_tok == null ? "" : (string)node.var_name_tok.value;
        object body_node = node.body_node;

        List<string> arg_names = new List<string>();

        foreach (Token arg_name in node.arg_name_toks)
        {
            arg_names.Add((string)arg_name.value);
        }

        FunctionValue func_value = new FunctionValue(func_name, body_node, arg_names, node.should_auto_return);
        func_value.set_context(context).set_pos(node.pos_start, node.pos_end);

        if (node.var_name_tok != null && node.var_name_tok.value.ToString() != "")
        {
            context.symbol_table.set(func_name, func_value);
        }

        return res.success(func_value);
    }

    public RuntimeResult visit_CallNode(CallNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();
        List<object> args = new List<object>();
        object value_to_call = res.register(this.visit(node.node_to_call, context));

        if (res.should_return())
        {
            return res;
        }

        if (value_to_call.GetType() == typeof(FunctionValue))
        {
            value_to_call = ((FunctionValue)value_to_call).copy().set_pos(node.pos_start, node.pos_end).set_context(context);

            foreach (object arg_node in node.arg_nodes)
            {
                args.Add(res.register(this.visit(arg_node, context)));

                if (res.should_return())
                {
                    return res;
                }
            }

            object return_value = res.register(((FunctionValue)value_to_call).execute(args));

            if (res.should_return())
            {
                return res;
            }

            return_value = return_value.GetType().GetMethod("copy").Invoke(return_value, new object[] { });
            return_value = return_value.GetType().GetMethod("set_pos").Invoke(return_value, new object[] { node.pos_start, node.pos_end });
            return_value = return_value.GetType().GetMethod("set_context").Invoke(return_value, new object[] { context });
            return res.success(return_value);
        }
        else
        {
            value_to_call = ((BuiltInFunction)value_to_call).copy().set_pos(node.pos_start, node.pos_end).set_context(context);

            foreach (object arg_node in node.arg_nodes)
            {
                args.Add(res.register(this.visit(arg_node, context)));

                if (res.should_return())
                {
                    return res;
                }
            }

            object return_value = res.register(((BuiltInFunction)value_to_call).execute(args));

            if (res.should_return())
            {
                return res;
            }

            return_value = return_value.GetType().GetMethod("copy").Invoke(return_value, new object[] { });
            return_value = return_value.GetType().GetMethod("set_pos").Invoke(return_value, new object[] { node.pos_start, node.pos_end });
            return_value = return_value.GetType().GetMethod("set_context").Invoke(return_value, new object[] { context });

            return res.success(return_value);
        }

        return res.success(Values.NULL);
    }

    public RuntimeResult visit_StringNode(StringNode node, Context context)
    {
        return new RuntimeResult().success(new StringValue(node.tok.value.ToString()).set_context(context).set_pos(node.pos_start, node.pos_end));
    }

    public RuntimeResult visit_ListNode(ListNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();
        List<object> elements = new List<object>();

        foreach (object element_node in node.element_nodes)
        {
            elements.Add(res.register(this.visit(element_node, context)));

            if (res.should_return())
            {
                return res;
            }
        }

        return res.success(new ListValue(elements).set_context(context).set_pos(node.pos_start, node.pos_end));
    }

    public RuntimeResult visit_ReturnNode(ReturnNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();
        object value = Values.NULL;

        if (node.node_to_return != null)
        {
            value = res.register(this.visit(node.node_to_return, context));

            if (res.should_return())
            {
                return res;
            }
        }

        return res.success_return(value);
    }

    public RuntimeResult visit_ContinueNode(ContinueNode node, Context context)
    {
        return new RuntimeResult().success_continue();
    }

    public RuntimeResult visit_BreakNode(BreakNode node, Context context)
    {
        return new RuntimeResult().success_break();
    }

    public RuntimeResult visit_ForEachNode(ForEachNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        Token element_var_name = node.element_var_name;
        Token list_var_name = node.list_var_name;
        object body_node = node.body_node;

        if (!context.symbol_table.present(list_var_name.value.ToString()))
        {
            return res.failure(new RuntimeError(node.pos_start, node.pos_end, "List not present", context));
        }

        ListValue theList = (ListValue)context.symbol_table.get(list_var_name.value);

        foreach (object element in theList.elements)
        {
            if (res.loop_should_continue)
            {
                continue;
            }

            if (res.loop_should_break)
            {
                break;
            }

            context.symbol_table.set(element_var_name.value, element);
            res.register(this.visit(body_node, context));

            if (res.should_return())
            {
                return res;
            }

            if (res.loop_should_continue)
            {
                continue;
            }

            if (res.loop_should_break)
            {
                break;
            }

        }

        return res.success(Values.NULL);
    }

    public RuntimeResult visit_SwitchNode(SwitchNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        Token var_name_tok = node.var_name_tok;
        List<Tuple<object, object>> cases = node.cases;
        object default_case = node.default_case;

        bool broken = false;

        foreach (Tuple<object, object> element in cases)
        {
            if (res.loop_should_break)
            {
                broken = true;
                break;
            }

            object expr = res.register(this.visit(element.Item1, context));

            if (res.should_return())
            {
                return res;
            }

            object body_node = element.Item2;
            object variable = context.symbol_table.get(var_name_tok.value);

            if ((string) (variable.GetType().GetMethod("as_string").Invoke(variable, new object[] { })) == (string) (expr.GetType().GetMethod("as_string").Invoke(expr, new object[] { })))
            {
                res.register(this.visit(body_node, context));

                if (res.should_return())
                {
                    return res;
                }
            }

            if (res.loop_should_break)
            {
                broken = true;
                break;
            }
        }

        if (!broken && default_case != null)
        {
            res.register(this.visit(default_case, context));

            if (res.should_return())
            {
                return res;
            }
        }

        return res.success(Values.NULL);
    }

    public RuntimeResult visit_StructDefNode(StructDefNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        Token var_name_tok = node.var_name_tok;
        object statements = node.statements_node;

        StructValue value = new StructValue((string)var_name_tok.value, statements);

        value.set_context(context).set_pos(node.pos_start, node.pos_end);
        context.symbol_table.set(var_name_tok.value, value);

        return res.success(value);
    }

    public RuntimeResult visit_StructAccessNode(StructAccessNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        Token var_name_tok = node.var_name_tok;
        Token access_var_name_tok = node.access_var_name_tok;

        if (!context.symbol_table.present(var_name_tok.value))
        {
            return res.failure(new RuntimeError(node.pos_start, node.pos_end, "Could not find this struct", context));
        }

        StructValue theStruct = (StructValue) context.symbol_table.get(var_name_tok.value);

        if (!theStruct.context.symbol_table.present(access_var_name_tok.value))
        {
            return res.failure(new RuntimeError(node.pos_start, node.pos_end, "Could not find this variable", context));
        }

        object value = theStruct.context.symbol_table.get(access_var_name_tok.value);

        return res.success(value);
    }

    public RuntimeResult visit_StructReAssignNode(StructReAssignNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        if (!context.symbol_table.present(node.var_name_tok.value))
        {
            return res.failure(new RuntimeError(node.pos_start, node.pos_end, "Could not find this struct", context));
        }

        StructValue theStruct = (StructValue)context.symbol_table.get(node.var_name_tok.value);

        object var_name = node.access_var_name_tok.value;
        object value = null;

        if (node.node != null)
        {
            value = res.register(this.visit(node.node, context));

            if (res.should_return())
            {
                return res;
            }
        }

        if (theStruct.context.symbol_table.present(var_name))
        {
            if (!theStruct.context.symbol_table.can_be_rewrite(var_name))
            {
                return res.failure(new RuntimeError(node.pos_start, node.pos_end, "Can not access to constants variables", context));
            }

            object actualValue = theStruct.context.symbol_table.get(var_name);

            if (node.op_tok.type == "EQ")
            {
                theStruct.context.symbol_table.set(var_name, value);
            }
            else if (node.op_tok.type == "PLUS_EQ")
            {
                Tuple<object, Error> result = (Tuple<object, Error>)actualValue.GetType().GetMethod("added_to").Invoke(actualValue, new object[] { value });
                value = result.Item1;
                theStruct.context.symbol_table.set(var_name, value);
            }
            else if (node.op_tok.type == "MINUS_EQ")
            {
                Tuple<object, Error> result = (Tuple<object, Error>)actualValue.GetType().GetMethod("subbed_by").Invoke(actualValue, new object[] { value });
                value = result.Item1;
                context.symbol_table.set(var_name, value);
            }
            else if (node.op_tok.type == "MUL_EQ")
            {
                Tuple<object, Error> result = (Tuple<object, Error>)actualValue.GetType().GetMethod("multed_by").Invoke(actualValue, new object[] { value });
                value = result.Item1;
                theStruct.context.symbol_table.set(var_name, value);
            }
            else if (node.op_tok.type == "LOGIC_NOT_EQ")
            {
                Tuple<object, Error> result = (Tuple<object, Error>)value.GetType().GetMethod("logic_notted").Invoke(value, new object[] { });
                value = result.Item1;
                theStruct.context.symbol_table.set(var_name, value);
            }
            else if (node.op_tok.type == "MODULO_EQ")
            {
                Tuple<object, Error> result = (Tuple<object, Error>)actualValue.GetType().GetMethod("moduled_by").Invoke(actualValue, new object[] { value });
                value = result.Item1;
                theStruct.context.symbol_table.set(var_name, value);
            }
            else
            {
                Tuple<object, Error> result = (Tuple<object, Error>)actualValue.GetType().GetMethod(node.op_tok.type.ToLower().Replace("_eq", "ed_by")).Invoke(actualValue, new object[] { value });
                value = result.Item1;
                theStruct.context.symbol_table.set(var_name, value);
            }

            value = value.GetType().GetMethod("copy").Invoke(value, new object[] { });
            value.GetType().GetMethod("set_pos").Invoke(value, new object[] { node.pos_start, node.pos_end });

            return res.success(value);
        }

        return res.failure(new RuntimeError(node.pos_start, node.pos_end, "Variable is not created", context));
    }

    public RuntimeResult visit_StructCallNode(StructCallNode node, Context context)
    {
        RuntimeResult res = new RuntimeResult();

        Token struct_var_name_tok = node.struct_var_name_tok;
        Token access_var_name_tok = node.access_var_name_tok;

        List<object> arg_nodes = node.arg_nodes;
        List<object> args = new List<object>();

        if (!context.symbol_table.present(struct_var_name_tok.value))
        {
            return res.failure(new RuntimeError(node.pos_start, node.pos_end, "Could not find this struct", context));
        }

        StructValue theStruct = (StructValue)context.symbol_table.get(struct_var_name_tok.value);

        if (!theStruct.context.symbol_table.present(access_var_name_tok.value))
        {
            return res.failure(new RuntimeError(node.pos_start, node.pos_end, "Could not find this variable", context));
        }

        object value_to_call = theStruct.context.symbol_table.get(access_var_name_tok.value);

        if (value_to_call.GetType() == typeof(FunctionValue))
        {
            value_to_call = ((FunctionValue)value_to_call).copy().set_pos(node.pos_start, node.pos_end).set_context(theStruct.context);

            foreach (object arg_node in node.arg_nodes)
            {
                args.Add(res.register(this.visit(arg_node, context)));

                if (res.should_return())
                {
                    return res;
                }
            }

            object return_value = res.register(((FunctionValue)value_to_call).execute(args));

            if (res.should_return())
            {
                return res;
            }

            return_value = return_value.GetType().GetMethod("copy").Invoke(return_value, new object[] { });
            return_value = return_value.GetType().GetMethod("set_pos").Invoke(return_value, new object[] { node.pos_start, node.pos_end });
            return_value = return_value.GetType().GetMethod("set_context").Invoke(return_value, new object[] { theStruct.context });

            return res.success(return_value);
        }
        else
        {
            value_to_call = ((BuiltInFunction)value_to_call).copy().set_pos(node.pos_start, node.pos_end).set_context(theStruct.context);

            foreach (object arg_node in node.arg_nodes)
            {
                args.Add(res.register(this.visit(arg_node, context)));

                if (res.should_return())
                {
                    return res;
                }
            }

            object return_value = res.register(((BuiltInFunction)value_to_call).execute(args));

            if (res.should_return())
            {
                return res;
            }

            return_value = return_value.GetType().GetMethod("copy").Invoke(return_value, new object[] { });
            return_value = return_value.GetType().GetMethod("set_pos").Invoke(return_value, new object[] { node.pos_start, node.pos_end });
            return_value = return_value.GetType().GetMethod("set_context").Invoke(return_value, new object[] { theStruct.context });

            return res.success(return_value);
        }

        return res.success(Values.NULL);
    }
}