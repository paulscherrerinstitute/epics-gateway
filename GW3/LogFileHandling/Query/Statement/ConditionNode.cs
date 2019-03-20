using System;
using System.Text.RegularExpressions;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    internal class ConditionNode : QueryNode
    {
        public QueryNode Left { get; }
        public string Condition { get; }
        public QueryNode Right { get; }

        private static Regex numberCheck = new Regex(@"^[\-]{0,1}[0-9]+[\.]{0,1}[0-9]*$");
        private static Regex dateCheck = new Regex(@"^([0-9]{4,4}\/)?([0-1][0-9]\/[0-3][0-9] )?([0-2][0-9]:[0-5][0-9]:[0-5][0-9])(\.[0-9]{1,3})?$");

        internal ConditionNode(QueryNode left, string condition, QueryNode right)
        {
            Left = left;
            Condition = condition;
            Right = right;
        }

        public override bool CheckCondition(Context context, LogEntry entry)
        {
            var valA = Left.Value(context, entry);
            var valB = Right.Value(context, entry);

            if (IsNumber(valA) && IsNumber(valB))
            {
                var nValA = double.Parse(valA);
                var nValB = double.Parse(valB);

                switch (Condition)
                {
                    case "!=":
                        return nValA != nValB;
                    case "=":
                        return nValA == nValB;
                    case ">":
                        return nValA > nValB;
                    case "<":
                        return nValA < nValB;
                    case ">=":
                        return nValA >= nValB;
                    case "<=":
                        return nValA <= nValB;
                    case "contains":
                        return valA.IndexOf(valB, StringComparison.InvariantCultureIgnoreCase) != -1;
                    case "starts":
                        return valA.IndexOf(valB, StringComparison.InvariantCultureIgnoreCase) == 0;
                    case "ends":
                        return valA.IndexOf(valB, StringComparison.InvariantCultureIgnoreCase) == valA.Length - valB.Length;
                }
            }
            if (IsDate(valA) && IsDate(valB))
            {
                var dValA = ToDate(valA);
                var dValB = ToDate(valB);

                switch (Condition)
                {
                    case "!=":
                        return dValA != dValB;
                    case "=":
                        return dValA == dValB;
                    case ">":
                        return dValA > dValB;
                    case "<":
                        return dValA < dValB;
                    case ">=":
                        return dValA >= dValB;
                    case "<=":
                        return dValA <= dValB;
                    case "contains":
                        return valA.IndexOf(valB, StringComparison.InvariantCultureIgnoreCase) != -1;
                    case "starts":
                        return valA.StartsWith(valB, StringComparison.InvariantCultureIgnoreCase);
                    case "ends":
                        return valA.EndsWith(valB, StringComparison.InvariantCultureIgnoreCase);
                }
            }
            else switch (Condition)
                {
                    case "!=":
                        return string.Compare(valA, valB, true) != 0;
                    case "=":
                        return string.Compare(valA, valB, true) == 0;
                    case ">":
                        return string.Compare(valA, valB, true) > 0;
                    case "<":
                        return string.Compare(valA, valB, true) < 0;
                    case ">=":
                        return string.Compare(valA, valB, true) >= 0;
                    case "<=":
                        return string.Compare(valA, valB, true) <= 0;
                    case "contains":
                        return valA.IndexOf(valB, StringComparison.InvariantCultureIgnoreCase) != -1;
                    case "starts":
                        return valA.StartsWith(valB, StringComparison.InvariantCultureIgnoreCase);
                    case "ends":
                        return valA.EndsWith(valB, StringComparison.InvariantCultureIgnoreCase);
                }
            throw new UnknownConditionException("Unknown condition '" + Condition + "'");
        }

        internal bool IsNumber(string value)
        {
            return numberCheck.IsMatch(value);
        }

        internal bool IsDate(string value)
        {
            return dateCheck.IsMatch(value);
        }

        internal DateTime ToDate(string value)
        {
            var m = dateCheck.Match(value);
            var now = DateTime.UtcNow;
            int year, month, day, hour, min, sec, mili;
            if (string.IsNullOrWhiteSpace(m.Groups[1].Value))
                year = now.Year;
            else
                year = int.Parse(m.Groups[1].Value.Replace("/", ""));

            if (string.IsNullOrWhiteSpace(m.Groups[2].Value))
            {
                month = now.Month;
                day = now.Day;
            }
            else
            {
                var d = m.Groups[2].Value.Split('/');
                month = int.Parse(d[0]);
                day = int.Parse(d[1]);
            }

            var h = m.Groups[3].Value.Trim().Split(':');
            hour = int.Parse(h[0]);
            min = int.Parse(h[1]);
            sec = int.Parse(h[2]);

            if (string.IsNullOrWhiteSpace(m.Groups[4].Value))
                mili = 0;
            else
                mili = int.Parse(m.Groups[4].Value.Replace(".", "").PadRight(3, '0').Substring(0, 3));

            return new DateTime(year, month, day, hour, min, sec, mili, DateTimeKind.Utc);
        }

        public override string Value(Context context, LogEntry entry) => throw new System.NotImplementedException();
    }
}
