using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParserUnitTests
{
    [TestClass]
    public class ConditionTest
    {

        //Channel names
        //aridi-pct:tau-hour
        //arima-ch-06me:i-set
        //arima-ch-07me:i-set
        //x07ma-fe-bm1:x

        [TestMethod]
        public void Parse_Contains()
        {
            CheckCondition("channel contains x", (string channel) =>
            {
                return channel.Contains("x");
            });
        }

        [TestMethod]
        public void Parse_Starts()
        {
            CheckCondition("channel starts a", (string channel) =>
            {
                return channel.StartsWith("a");
            });
        }

        [TestMethod]
        public void Parse_Ends()
        {
            CheckCondition("channel ends x", (string channel) =>
            {
                return channel.EndsWith("x", StringComparison.InvariantCultureIgnoreCase);
            });
        }

        [TestMethod]
        public void Parse_Equal()
        {
            CheckCondition("channel = \"aridi-pct:tau-hour\"", (string channel) =>
            {
                return channel == "aridi-pct:tau-hour";
            });
        }

        [TestMethod]
        public void Parse_Not_Equal()
        {
            CheckCondition("channel != \"aridi-pct:tau-hour\"", (string channel) =>
            {
                return channel != "aridi-pct:tau-hour";
            });
        }

        [TestMethod]
        public void Parse_Bigger_Than()
        {
            CheckCondition("datacount > 6", (string datacount) =>
            {
                try { 
                    return int.Parse(datacount) > 6;
                } catch (FormatException)
                {
                    return string.Compare(datacount, "6", true) > 0;
                }
            }, "DataCount");
        }
        [TestMethod]
        public void Parse_Bigger_Equal_Than()
        {
            CheckCondition("datacount >= 6", (string datacount) =>
            {
                try
                {
                    return int.Parse(datacount) >= 6;
                } catch(FormatException)
                {
                    return string.Compare(datacount, "6", true) >= 0;
                }
            }, "DataCount");
        }
        [TestMethod]
        public void Parse_Smaller_Than()
        {
            CheckCondition("datacount < 6", (string datacount) =>
            {
                try
                {
                    return int.Parse(datacount) < 6;
                } catch (FormatException)
                {
                    return string.Compare(datacount, "6", true) < 0;
                }
            }, "DataCount");
        }
        [TestMethod]
        public void Parse_Smaller_Equal_Than()
        {
            CheckCondition("datacount <= 6", (string datacount) =>
            {
                try
                {
                    return int.Parse(datacount) <= 6;
                }
                catch (FormatException)
                {
                    return string.Compare(datacount, "6", true) <= 0;
                }
            }, "DataCount");
        }
        [TestMethod]
        public void Parse_Or()
        {
            CheckCondition("channel contains x || channel starts ar", (string channel) =>
            {
                return channel.Contains("x") || channel.Contains("ar");
            });
        }
        [TestMethod]
        public void Parse_And()
        {
            CheckCondition("channel contains x && channel contains i", (string channel) =>
            {
                return channel.Contains("x") && channel.Contains("i");
            });
        }

        [TestMethod]
        public void Parse_Or_And()
        {
            CheckCondition("channel contains x || channel starts ar && channel contains i", (string channel) =>
            {
                return channel.Contains("x") || channel.StartsWith("ar") && channel.Contains("i");
            });
        }

        [TestMethod]
        public void Parse_And_Or()
        {
            CheckCondition("channel contains x && channel starts ar || channel contains i", (string channel) =>
            {
                return channel.Contains("x") && channel.StartsWith("ar") || channel.Contains("i");
            });
        }

        [TestMethod]
        public void Parse_Multiple_Or_And()
        {
            CheckCondition("channel contains x || channel starts ar && channel contains i || channel starts ar && channel contains i && channel contains x", (string channel) =>
            {
                return channel.Contains("x") || channel.StartsWith("ar") && channel.Contains("i") || channel.StartsWith("ar") && channel.Contains("i") && channel.Contains("x");
            });
        }
        [TestMethod]
        public void Parse_Multiple_And_Or()
        {
            CheckCondition("channel starts ar && channel contains i && channel contains x && channel contains x || channel starts ar && channel contains i || channel starts ar && channel contains i && channel contains x", (string channel) =>
            {
                return channel.StartsWith("ar") && channel.Contains("i") && channel.Contains("x") && channel.Contains("x") || channel.StartsWith("ar") && channel.Contains("i") || channel.StartsWith("ar") && channel.Contains("i") && channel.Contains("x");
            });
        }

        [TestMethod]
        public void Parse_Parenthesis()
        {
            CheckCondition("(channel contains x || channel contains ar) && channel contains i", (string channel) =>
            {
                return (channel.Contains("x") || channel.Contains("ar")) && channel.Contains("i");
            });
        }

        [TestMethod]
        public void Parse_Multiple_Parenthesis()
        {
            CheckCondition("(channel contains x || channel contains ar) && (channel contains i || channel contains s)", (string channel) =>
            {
                return (channel.Contains("x") || channel.Contains("ar")) && (channel.Contains("i") || channel.Contains("s"));
            });
        }

        [TestMethod]
        public void Parse_Multiple_Containing_Parenthesis()
        {
            CheckCondition("(channel contains x || (channel contains ar && channel starts ari)) && (channel contains i || channel contains s)", (string channel) =>
            {
                return (channel.Contains("x") || (channel.Contains("ar") && channel.StartsWith("ari"))) && (channel.Contains("i") || channel.Contains("s"));
            });
        }

        [TestMethod]
        public void Parse_Multiple_Variables()
        {
            using (var ctx = new GWLogger.Backend.DataContext.Context(@"C:\temp\tt"))
            {
                var channelDetailId = ctx.MessageDetailTypes.First(row => row.Value == "ChannelName").Id;
                var dataCountDetailId = ctx.MessageDetailTypes.First(row => row.Value == "DataCount").Id;
                var node = GWLogger.Backend.DataContext.Query.QueryParser.Parse("datacount > 2 && channel contains x");
                ctx.GetLogs(ctx.Gateways.First(), new DateTime(2018, 09, 05), new DateTime(2018, 09, 05).AddDays(1).Date, "").Take(1000).ToList().ForEach(log =>
                {
                    var channelname = log.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == channelDetailId)?.Value.ToLower() ?? "";
                    var datacount = log.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == dataCountDetailId)?.Value.ToLower() ?? "";
                    var countCond = false;
                    try
                    {
                        countCond = int.Parse(datacount) > 6;
                    }
                    catch (FormatException)
                    {
                        countCond = string.Compare(datacount, "2", true) > 0;
                    }
                    if (countCond && channelname.IndexOf("x",StringComparison.InvariantCultureIgnoreCase) != -1)
                        Assert.IsTrue(node.CheckCondition(ctx, log));
                    else
                        Assert.IsFalse(node.CheckCondition(ctx, log));
                });
            }
        }

        private void CheckCondition(string query, Func<string, bool> expected, string detailType = "ChannelName")
        {
            using (var ctx = new GWLogger.Backend.DataContext.Context(@"C:\temp\tt"))
            {
                var detailId = ctx.MessageDetailTypes.First(row => row.Value == detailType).Id;
                var node = GWLogger.Backend.DataContext.Query.QueryParser.Parse(query);
                ctx.GetLogs(ctx.Gateways.First(), new DateTime(2018, 09, 05), new DateTime(2018, 09, 05).AddDays(1).Date, "").Take(1000).ToList().ForEach(log =>
                {
                    var source = log.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value.ToLower() ?? "";
                    if (expected(source))
                        Assert.IsTrue(node.CheckCondition(ctx, log));
                    else
                        Assert.IsFalse(node.CheckCondition(ctx, log));
                });
            }
        }
    }
}
