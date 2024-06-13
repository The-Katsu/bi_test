using RussianBISqlOptimizer.Statements;

namespace RussianBISqlOptimizer
{
    public static class AstMaker
    {
        public static Node ToAst(this SelectQuery baseQuery)
        {
            var sql = baseQuery.ToSqlString().Split('\n');

            var keys = new HashSet<string>()
            {
                "SELECT",
                "FROM",
                "GROUP BY",
                "WHERE"
            };

            var head = new Node() { Type = SqlType.ROOT };
            var lvl = 1;
            var crt = head;

            foreach (var str in sql)
            {
                // todo
                if (lvl > 1)
                {
                    if (!BracersLvlUp(str))
                    {
                        lvl--;
                        crt = head;
                        for (int i = 0; i < lvl; i++)
                        {
                            crt = crt.Childrens.Single(x => x.Type == SqlType.SELECT);
                        }
                    }
                }

                var parts = str.Split(' ');
                var node = new Node();

                if (parts.Contains("SELECT"))
                {
                    Select(ref node, str);
                    crt.Childrens.Add(node);
                    continue;
                }
                if (parts.Contains("FROM"))
                {
                    From(ref crt, ref lvl, str);
                    continue;
                }
                if (parts.Contains("WHERE"))
                {
                    Where(ref crt, parts);
                    continue;
                }
                if (parts.Contains("GROUP"))
                {
                    GroupBy(ref crt, ref lvl, head, parts, str);
                    continue;
                }

            }

            return head;
        }

        private static void GroupBy(ref Node crt, ref int lvl, Node head, string[] parts, string str)
        {
            var  node = new Node() { Item = "GROUP BY", Type = SqlType.GROUPBY };
            parts = str.Substring(str.IndexOf("BY") + 2).Split(' ').Where(x => x != string.Empty).ToArray();
            foreach (var p in parts)
            {
                node.Childrens.Add(new Node() { Item = p, Type = SqlType.COLUMN });
            }
            crt.Childrens.Add(node);
            lvl--;
            crt = head.Childrens.Single(x => x.Type == SqlType.SELECT);
        }

        private static void Where(ref Node crt, string[] parts)
        {
            var tmp = parts.Where(x => x != "WHERE").ToArray();
            var node = new Node() { Type = SqlType.WHERE };
            node.Childrens.Add(
                new Node()
                {
                    Item = tmp[1],
                    Type = SqlType.COMPARER,
                    Childrens = new List<Node>()
                    {
                            new Node() { Item = tmp[0], Type = SqlType.COLUMN },
                            new Node() { Item = tmp[2], Type = SqlType.VALUE }
                    }
                }
                );
            crt.Childrens.Add(node);
        }
        private static void From(ref Node crt, ref int lvl, string sql)
        {
            var node = new Node() { Item = "From", Type = SqlType.FROM };
            var parts = sql.Split(' ');
            if (sql.Contains("SELECT"))
            {
                var slc = new Node();
                lvl++;
                crt = crt.Childrens.Single(x => x.Type == SqlType.SELECT);
                Select(ref slc, sql);
                node.Childrens.Add(slc);
                crt.Childrens.Add(node);
                crt = slc;
            }
            else
            {
                crt.Childrens.Add(new Node() { Type = SqlType.TABLE, Item = parts[^1] });
            }
        }

        private static void Select(ref Node node, string sql)
        {
            node = new Node() { Item = "Select", Type = SqlType.SELECT };
            var i = sql.IndexOf("SELECT") + "SELECT".Length;
            var str = sql[i..];
            var arr = str.Split(',');
            foreach (var col in arr)
            {
                if (col.Contains("AS"))
                {
                    var cols = col.Split("AS");
                    if (cols[0].Contains('('))
                    {
                        var fun = cols[0].Split(new char[] { '(', ')' });
                        node.Childrens.Add(new Node()
                        {
                            Item = fun[0],
                            Type = SqlType.FUNC,
                            Childrens = new List<Node>()
                            {
                                new() { Type = SqlType.COLUMN, Item = fun[1] },
                                new() { Type = SqlType.ALIAS, Item = cols[1] }
                            }
                        }
                        );
                    }
                    else
                    {
                        node.Childrens.Add(new Node()
                        {
                            Item = cols[0],
                            Type = SqlType.COLUMN,
                            Childrens = new List<Node>()
                            {
                                new() { Type = SqlType.ALIAS, Item = cols[1] }
                            }
                        }
                        );
                    }
                }
                else
                {
                    //todo
                }
            }
        }

        private static bool BracersLvlUp(string sql)
        {
            // todo
            return true;
        }
    }

    
}
