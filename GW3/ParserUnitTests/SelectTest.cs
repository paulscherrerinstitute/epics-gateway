using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParserUnitTests
{
    [TestClass]
    public class SelectTest
    {
        [TestMethod]
        public void SimpleSelectStatement()
        {
            var node = GWLogger.Backend.DataContext.Query.QueryParser.Parse("select date,position");
            Assert.IsTrue(node is GWLogger.Backend.DataContext.Query.Statement.SelectNode);
        }

        [TestMethod]
        public void CheckSelectColumns()
        {
            var node = (GWLogger.Backend.DataContext.Query.Statement.SelectNode)GWLogger.Backend.DataContext.Query.QueryParser.Parse("select date,position");

            Assert.AreEqual(2, node.Columns.Count);
            Assert.AreEqual("date", node.Columns[0].Field.Name);
            Assert.AreEqual("position", node.Columns[1].Field.Name);
        }

        [TestMethod]
        public void ExecuteSelect()
        {

            var node = (GWLogger.Backend.DataContext.Query.Statement.SelectNode)GWLogger.Backend.DataContext.Query.QueryParser.Parse("select position");
            Assert.AreEqual(5432L, node.Values(null, new GWLogger.Backend.DataContext.LogEntry { Position = 5432 })[0]);
        }


        [TestMethod]
        public void CheckGroupColumns()
        {
            var node = (GWLogger.Backend.DataContext.Query.Statement.SelectNode)GWLogger.Backend.DataContext.Query.QueryParser.Parse("select date,position group by date");

            Assert.AreEqual(1, node.Group.Fields.Count);
            Assert.AreEqual("date", node.Group.Fields[0]);
        }
    }
}
