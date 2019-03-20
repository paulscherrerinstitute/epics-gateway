namespace GWLogger.Backend.DataContext.Query
{
    public class QueryColumn
    {
        public string Field { get; set; }
        public string DisplayTitle { get; set; }

        public static implicit operator QueryColumn(string src)
        {
            return new QueryColumn { Field = src };
        }
    }
}
