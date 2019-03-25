namespace GWLogger.Backend.DataContext.Query
{
    public enum Direction
    {
        Ascending,
        Descending
    }

    public class OrderColumn
    {
        public string Name { get; set; }
        public Direction Direction { get; set; } = Direction.Ascending;
    }
}
