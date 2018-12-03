using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParserUnitTests
{
    [TestClass]
    public class ASTTest
    {
        [TestMethod]
        [ExpectedException(typeof(GWLogger.Backend.DataContext.Query.InvalidTokenException))]
        public void InvalidToken()
        {
            var node = GWLogger.Backend.DataContext.Query.QueryParser.Parse("channel == -");
        }

        [TestMethod]
        [ExpectedException(typeof(GWLogger.Backend.DataContext.Query.InvalidTokenException))]
        public void InvalidToken_WithoutSpace()
        {
            var node = GWLogger.Backend.DataContext.Query.QueryParser.Parse("channel === 2");
        }

        [TestMethod]
        [ExpectedException(typeof(GWLogger.Backend.DataContext.Query.MissingTokenException))]
        public void MissingToken()
        {
            var node = GWLogger.Backend.DataContext.Query.QueryParser.Parse("channel contains");
        }

        [TestMethod]
        [ExpectedException(typeof(GWLogger.Backend.DataContext.Query.SpareTokenException))]
        public void SpareToken()
        {
            var node = GWLogger.Backend.DataContext.Query.QueryParser.Parse("channel contains 2 s");
        }
    }
}
