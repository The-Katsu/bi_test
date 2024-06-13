using RussianBISqlOptimizer.Statements;

namespace RussianBISqlOptimizer;

public static class SqlOptimizer
{

    public static SelectQuery Optimize(SelectQuery baseQuery)
    {
        //ROOT
        //└── SELECT
        //    ├── COLUMN(Col1 AS Attribute1)
        //    ├── COLUMN(Col2 AS Attribute2)
        //    ├── FUNC(SUM(Col3) AS Attribute3)
        //    └── FROM
        //        └── SELECT
        //            ├── COLUMN(Column1 AS Col1)
        //            ├── COLUMN(Column2 AS Col2)
        //            ├── COLUMN(Column3 AS Col3)
        //            ├── FROM
        //                └── TABLE(`Table`)
        //            ├── WHERE
        //                └── COMPARER(`Column1` = 1000)
        //            └── GROUPBY
        //                ├── COLUMN(Column1)
        //                ├── COLUMN(Column2)
        //                └── COLUMN(Column3)
        //    └── GROUPBY
        //        ├── COLUMN(Col1)
        //        └── COLUMN(Col2)
        var ast = baseQuery.ToAst();

        var crt = ast;

        var query = new SelectQuery();

        while (crt.Childrens.Count > 0)
        {
            var c = crt.Childrens[0];
            switch(c.Type)
            {
                case SqlType.SELECT:
                    CheckSub(ref crt, ref query); 
                    // todo если не вложенная
                    break;
                case SqlType.TABLE:
                    query.From(new Table(c.Item.Trim(new char[] {'`', '(', ')'})));
                    break;
                case SqlType.WHERE:
                    var col = c.Childrens[0].Childrens.Single(x => x.Type == SqlType.COLUMN).Item.Trim('`');
                    var val = c.Childrens[0].Childrens.Single(x => x.Type == SqlType.VALUE).Item;
                    query.Where(new BinaryOperation(new Column(col), new Value(int.Parse(val)), c.Childrens[0].Item));
                    break;

            }
            crt.Childrens.Remove(c);
        }

        return query;
    }

    private static void CheckSub(ref Node crt, ref SelectQuery q)
    {
        var from = crt.Childrens.Single(x => x.Type == SqlType.SELECT).Childrens.Single(x => x.Type == SqlType.FROM);
        if (from.Childrens.Any(x => x.Type == SqlType.SELECT))
        {
            var s1 = crt.Childrens.Single(x => x.Type == SqlType.SELECT);
            var s2 = from.Childrens.Single(x => x.Type == SqlType.SELECT);

            var s1nodes = s1.Childrens.Where(x => x.Type == SqlType.COLUMN || x.Type == SqlType.FUNC).ToList();
            var s2nodes = s2.Childrens.Where(x => x.Type == SqlType.COLUMN || x.Type == SqlType.FUNC).ToList();

            var s1args = new List<string>();
            var s2args = new List<string>();

            var trim = new char[] {'`', ' '}; 

            foreach(var n in s1nodes)
            {
                if (n.Type == SqlType.COLUMN)
                {
                    s1args.Add(n.Item.Trim(trim));
                }
                if (n.Type == SqlType.FUNC)
                {
                    s1args.Add(n.Childrens.Single(x => x.Type == SqlType.COLUMN).Item.Trim(trim));
                }
            }

            foreach (var n in s2nodes)
            {
                if (n.Type == SqlType.COLUMN)
                {
                    s2args.Add(n.Childrens.Single(x => x.Type == SqlType.ALIAS).Item.Trim(trim));
                }
            }

            if (s1args.SequenceEqual(s2args))
            {
                for (int i = 0; i < s1nodes.Count; i++)
                {
                    if (s1nodes[i].Type == SqlType.COLUMN)
                    {
                        q.Select(
                            new Alias(
                                new Column(
                                    s2nodes[i].Item.Trim(trim)),
                                    s1nodes[i].Childrens[0].Item.Trim(trim)));
                    }
                    if (s1nodes[i].Type == SqlType.FUNC)
                    {
                        q.Select(
                            new Alias(
                                new FunctionCall(s1nodes[i].Item, 
                                    new Column(s2nodes[i].Item.Trim(trim))),
                                s1nodes[i].Childrens[1].Item.Trim(trim)));
                    }
                }
            }

            if (s1.Childrens.Any(x => x.Type == SqlType.GROUPBY) &&
                s2.Childrens.Any(x => x.Type == SqlType.GROUPBY))
            {
                var g1 = s1.Childrens.Single(x => x.Type == SqlType.GROUPBY);
                var g2 = s2.Childrens.Single(x => x.Type == SqlType.GROUPBY);
                var cs = new List<Column>(); 
                for (int i = 0; i < g1.Childrens.Count; i++)
                {
                    cs.Add(new Column(g2.Childrens[i].Item.Trim(new char[] {',', '`', ' '})));
                }
                q.GroupBy(cs);
                s2.Childrens = s2.Childrens.Where(x => x.Type != SqlType.GROUPBY).ToList();
            }

            crt = s2;
        }
    }


}

