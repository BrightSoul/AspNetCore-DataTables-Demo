using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DataTables.Models.Configuration
{
    [ModelBinder(typeof(DataTablesConfigurationModelBinder))]
    public class DataTablesConfiguration
    {
        public string TableName { get; set; }
        public string Endpoint { get; set; }
        public IEnumerable<DataTablesColumnDefinition> ColumnDefinitions { get; set; }
    }

    public class DataTablesColumnDefinition
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public bool Searchable { get; set; }
        public bool Sortable { get; set; }
        [JsonIgnore]
        public bool GloballySearchable { get; set; }
    }
}