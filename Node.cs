namespace RussianBISqlOptimizer
{
    public class Node
    {
        public string Item { get; set; } = null!;

        public SqlType Type { get; set; }

        public List<Node> Childrens { get; set; } = new List<Node>();
    }

    public enum SqlType
    {
        SELECT,
        GROUPBY,
        WHERE,
        FROM,

        COLUMN,
        ALIAS,
        FUNC,

        COMPARER,
        VALUE,
        TABLE,

        ROOT
    }
}