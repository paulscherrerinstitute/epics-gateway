using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web;

namespace GWLogger.Backend
{
    /// <summary>
    /// Implements BulkInsert via SqlBulkCopy and reflection
    /// While the package
    /// https://github.com/MikaelEliasson/EntityFramework.Utilities
    /// should do basically that plus more, sadly it doesn't support computed columns.
    /// </summary>
    public static class Bulk
    {
        static Dictionary<Type, List<PropertyInfo>> propCache = new Dictionary<Type, List<PropertyInfo>>();
        static SemaphoreSlim cacheLock = new SemaphoreSlim(1);

        public static void Insert<TType>(DbContext ctx, DbSet<TType> destTable, IEnumerable<TType> toAdd) where TType : class
        {
            if (ctx.Database.Connection.State == ConnectionState.Closed)
                ctx.Database.Connection.Open();
            var elemType = destTable.GetType().GenericTypeArguments.FirstOrDefault();

            var copy = new SqlBulkCopy((SqlConnection)ctx.Database.Connection);

            copy.DestinationTableName = ((TableAttribute)elemType.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault()).Name;

            DataTable data = new DataTable(copy.DestinationTableName);
            List<PropertyInfo> props;

            cacheLock.Wait();
            if (propCache.ContainsKey(elemType))
            {
                props = propCache[elemType];
                cacheLock.Release();
            }
            else
            {
                cacheLock.Release();
                props = new List<PropertyInfo>();

                foreach (var prop in elemType.GetProperties())
                {
                    // Skip computed columns;
                    if (((DatabaseGeneratedAttribute)prop.GetCustomAttributes(typeof(DatabaseGeneratedAttribute), false).FirstOrDefault())?.DatabaseGeneratedOption == DatabaseGeneratedOption.Computed)
                        continue;
                    // Foreign key? Skip
                    if (prop.GetCustomAttributes(typeof(ForeignKeyAttribute), false).Any())
                        continue;
                    // Some kind of array? Skip
                    if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                        continue;
                    props.Add(prop);
                }

                cacheLock.Wait();
                propCache.Add(elemType, props);
                cacheLock.Release();
            }

            props.ForEach(prop => data.Columns.Add(prop.Name, prop.PropertyType));
            toAdd.ToList().ForEach(i => data.Rows.Add(props.Select(col => col.GetValue(i)).ToArray()));
            copy.WriteToServer(data);
        }
    }
}