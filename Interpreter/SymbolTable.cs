﻿using System.Collections.Generic;
using System;

public class SymbolTable
{
    public Dictionary<object, Tuple<object, bool>> symbols;
    public SymbolTable parent;

    public SymbolTable(SymbolTable parent = null)
    {
        symbols = new Dictionary<object, Tuple<object, bool>>();
        this.parent = parent;
    }

    public void clear()
    {
        symbols.Clear();
        SymbolTable theParent = parent;

        while (theParent != null)
        {
            theParent.clear();
            theParent = theParent.parent;
        }
    }

    public bool can_be_rewrite(object name)
    {
        try
        {
            return this.symbols[name].Item2;
        }
        catch (System.Exception)
        {
            try
            {
                if (this.parent != null)
                {
                    return this.parent.can_be_rewrite(name);
                }
            }
            catch (System.Exception)
            {
            }
        }

        return true;
    }

    public object get(object name)
    {
        try
        {
            return this.symbols[name].Item1;
        }
        catch (System.Exception)
        {
            try
            {
                if (this.parent != null)
                {
                    return this.parent.get(name);
                }
            }
            catch (System.Exception)
            {
            }
        }

        return null;
    }

    public void set(object name, object value, bool rewritten = true)
    {
        this.symbols[name] = new Tuple<object, bool>(value, rewritten);
    }

    public void remove(object name)
    {
        this.symbols.Remove(name);
    }

    public bool present(object name)
    {
        bool present = false;

        try
        {
            present = this.symbols.ContainsKey(name);
        }
        catch (System.Exception ex)
        {
        }

        if (present)
        {
            return true;
        }

        present = false;

        try
        {
            present = this.parent.present(name);
        }
        catch (System.Exception ex)
        {
        }

        return present;
    }
}