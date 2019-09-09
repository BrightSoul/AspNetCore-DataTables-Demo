using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using DataTables.Models.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DataTables.Models
{
    [ModelBinder(typeof(DataTablesInputModelBinder))]
    public class DataTablesInputModel
    {
        public static DataTablesInputModel FromQueryString(string queryString, DataTablesConfiguration configuration)
        {
            var nameValueCollection = string.IsNullOrWhiteSpace(queryString) ? new NameValueCollection() : HttpUtility.ParseQueryString(queryString);
            return FromNameValueCollection(nameValueCollection, configuration);
        }
        public static DataTablesInputModel FromNameValueCollection(NameValueCollection nameValueCollection, DataTablesConfiguration configuration)
        {
            Func<string, string> valueProvider = (string key) => nameValueCollection.Get(key);
            return FromValueProvider(valueProvider, configuration);
        }
        public static object FromFormCollection(IFormCollection value, DataTablesConfiguration configuration)
        {
            Func<string, string> valueProvider = (string key) => value[key];
            return FromValueProvider(valueProvider, configuration);
        }
        public static DataTablesInputModel FromValueProvider(Func<string, string> valueProvider, DataTablesConfiguration configuration)
        {
            int.TryParse(valueProvider("draw"), out int draw);
            int.TryParse(valueProvider("start"), out int start);
            int.TryParse(valueProvider("length"), out int length);
            string search = valueProvider("search[value]");

            var columnDefinitions = new List<DataTablesColumnModel>();
            int columnIndex = 0;
            while (true)
            {
                try
                {
                    var columnModel = DataTablesColumnModel.FromValueProvider(valueProvider, columnIndex++, configuration);
                    columnDefinitions.Add(columnModel);
                }
                catch (IndexOutOfRangeException)
                {
                    break;
                }
                catch
                {
                    continue;
                }
            }
            return new DataTablesInputModel(configuration.TableName, draw, start, length, search, columnDefinitions.AsReadOnly());
        }

        private DataTablesInputModel(string tableName, int draw, int start, int length, string search, IReadOnlyCollection<DataTablesColumnModel> columnDefinitions)
        {
            TableName = tableName;
            Draw = Math.Max(1, draw);
            Start = Math.Max(0, start);
            Length = Math.Min(100, Math.Max(10, length));
            Search = string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim();
            ColumnDefinitions = columnDefinitions;
        }
        public string TableName { get; }
        public int Draw { get; }
        public int Start { get; }
        public int Length { get; }
        public string Search { get; }
        public IReadOnlyCollection<DataTablesColumnModel> ColumnDefinitions { get; }
    }

    public class DataTablesColumnModel
    {
       
        public static DataTablesColumnModel FromValueProvider(Func<string, string> valueProvider, int columnIndex, DataTablesConfiguration configuration)
        {
            string prefix = $"colums[{columnIndex}]";
            string columnName = valueProvider($"columns[{columnIndex}][name]");

            if (columnName == null)
            {
                throw new IndexOutOfRangeException($"Cannot create column with index {columnIndex}");
            }

            DataTablesColumnDefinition columnDefinition = configuration.ColumnDefinitions.SingleOrDefault(col => col.Name == columnName);
            if (columnDefinition == null)
            {
                throw new InvalidOperationException($"Cannot create column '{columnName}' since it's not supported");
            }

            string search = string.Empty;
            if (columnDefinition.Searchable)
            {
                search = valueProvider($"columns[{columnIndex}][search][value]");
            }

            int orderIndex = 0;
            string columnReferenceIndex = null;
            var sortingDirection = DataTablesSortingDirection.None;
            int? sortingPriority = null;

            if (columnDefinition.Sortable)
            {
                do
                {
                    columnReferenceIndex = valueProvider($"order[{orderIndex}][column]");
                    if (columnReferenceIndex == columnIndex.ToString())
                    {
                        sortingDirection = "desc".Equals(valueProvider($"order[{orderIndex}][dir]"), StringComparison.InvariantCultureIgnoreCase) ?
                                DataTablesSortingDirection.Descending : DataTablesSortingDirection.Ascending;
                        break;
                    }
                    orderIndex++;
                } while (!string.IsNullOrWhiteSpace(columnReferenceIndex));

                if (sortingDirection != DataTablesSortingDirection.None)
                {
                    sortingPriority = orderIndex;
                }
            }

            return new DataTablesColumnModel(columnName, search, columnDefinition.GloballySearchable, sortingDirection, sortingPriority);
        }

        private DataTablesColumnModel(string name, string search, bool globallySearchable, DataTablesSortingDirection sortingDirection, int? sortingPriority = null)
        {
            Name = name;
            Search = search;
            SortingDirection = sortingDirection;
            GloballySearchable = globallySearchable;
            SortingPriority = sortingPriority;
        }
        public string Name { get; }
        public string Search { get; }
        public bool GloballySearchable { get; }
        public DataTablesSortingDirection SortingDirection { get; }
        public int? SortingPriority { get; }
    }
    public enum DataTablesSortingDirection
    {
        None,
        Ascending,
        Descending
    }
}