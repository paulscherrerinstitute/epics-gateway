using GWLogger.Backend.DataContext.Query.Statement;

namespace GWLogger.Backend.DataContext.Query
{
    public class QueryColumn
    {
        public INamedNode Field { get; set; }
        public string DisplayTitle { get; set; }

        public static implicit operator QueryColumn(string src)
        {
            return new QueryColumn { Field = new VariableNode(src) };
        }
    }
}
