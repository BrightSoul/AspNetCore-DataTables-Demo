using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataTables.Models;
using DataTables.Models.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DataTables.Services
{
    public class DataTablesService
    {
        private readonly IConfiguration configuration;
        private readonly IUrlHelper urlHelper;
        public DataTablesService(IConfiguration configuration, IUrlHelper urlHelper)
        {
            this.configuration = configuration;
            this.urlHelper = urlHelper;
        }

        public DataTablesConfiguration GetConfiguration(string controllerName, string actionName)
        {
            (controllerName, actionName) = SanitizeNames(controllerName, actionName);

            string configurationName = $"DataTables:{controllerName}.{actionName}";
            var configurationObject = new DataTablesConfiguration();
            configurationObject.Endpoint = urlHelper.Action($"{actionName}Data", controllerName);
            var section = configuration.GetSection(configurationName);
            if (!section.Exists())
            {
                throw new InvalidOperationException($"Sezione di configurazione '{configurationName}' non trovata. Verifica di averla creata nel file appsettings.json (o in altra fonte di configurazione).");
            }
            
            section.Bind(configurationObject);
            return configurationObject;
        }

        private (string, string) SanitizeNames(string controllerName, string actionName)
        {
            if (string.IsNullOrWhiteSpace(controllerName))
            {
                throw new ArgumentNullException(controllerName);
            }
            if (string.IsNullOrWhiteSpace(actionName))
            {
                throw new ArgumentNullException(actionName);
            }

            if (controllerName.EndsWith("Controller"))
            {
                controllerName = controllerName.Substring(0, controllerName.Length - 10);
            }
            if (actionName.EndsWith("Data"))
            {
                actionName = actionName.Substring(0, actionName.Length - 4);
            }
            if (actionName.EndsWith("DataAsync"))
            {
                actionName = actionName.Substring(0, actionName.Length - 4);
            }
            return (controllerName, actionName);
        }

        public async Task<DataTablesViewModel> GetResultsAsync(DataTablesInputModel inputModel)
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();
                int draw = inputModel.Draw;
                int recordsTotal = await GetRecordsTotalAsync(connection, inputModel);
                int recordsFiltered = await GetRecordsFilteredAsync(connection, inputModel);
                IReadOnlyCollection<object[]> data = await GetDataAsync(connection, inputModel);

                return new DataTablesViewModel(draw, recordsTotal, recordsFiltered, data);
            }
        }

        protected virtual DbConnection CreateConnection()
        {
            var connectionString = configuration.GetConnectionString("Northwind");
            return new SQLiteConnection(connectionString);
        }

        private async Task<int> GetRecordsTotalAsync(DbConnection connection, DataTablesInputModel inputModel)
        {
            var sb = new StringBuilder();
            using (var command = connection.CreateCommand())
            {
                AddCountSelectClauseToCommand(command, sb, inputModel);
                command.CommandText = sb.ToString();
                object result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        private async Task<int> GetRecordsFilteredAsync(DbConnection connection, DataTablesInputModel inputModel)
        {
            var sb = new StringBuilder();
            using (var command = connection.CreateCommand())
            {
                AddCountSelectClauseToCommand(command, sb, inputModel);
                AddWhereClauseToCommand(command, sb, inputModel);
                command.CommandText = sb.ToString();
                object result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        private async Task<IReadOnlyCollection<object[]>> GetDataAsync(DbConnection connection, DataTablesInputModel inputModel)
        {
            var dataRows = new List<object[]>();
            var sb = new StringBuilder();
            using (var command = connection.CreateCommand())
            {
                AddSelectClauseToCommand(command, sb, inputModel);
                AddWhereClauseToCommand(command, sb, inputModel);
                AddOrderByClauseToCommand(command, sb, inputModel);
                AddLimitClauseToCommand(command, sb, inputModel);
                command.CommandText = sb.ToString();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var dataRow = new object[reader.FieldCount];
                        dataRows.Add(dataRow);
                        reader.GetValues(dataRow);
                    }
                }
            }
            return dataRows.AsReadOnly();
        }

        private void AddSelectClauseToCommand(DbCommand command, StringBuilder sb, DataTablesInputModel inputModel)
        {
            var fieldNames = string.Join(", ", inputModel.ColumnDefinitions.Select(col => col.Name));
            sb.Append($"SELECT {fieldNames} FROM {inputModel.TableName}");
        }

        private void AddCountSelectClauseToCommand(DbCommand command, StringBuilder sb, DataTablesInputModel inputModel)
        {
            sb.Append($"SELECT COUNT(*) FROM {inputModel.TableName}");
        }

        private void AddWhereClauseToCommand(DbCommand command, StringBuilder sb, DataTablesInputModel inputModel)
        {
            var columnsWithSearchValue = inputModel.ColumnDefinitions.Where(col => !string.IsNullOrWhiteSpace(col.Search)).ToArray();
            var globallySearchableColumns = inputModel.ColumnDefinitions.Where(col => col.GloballySearchable).ToArray();

            bool hasColumnsWithSearchValue = columnsWithSearchValue.Length > 0;
            bool hasGlobalSearch = !string.IsNullOrWhiteSpace(inputModel.Search) && globallySearchableColumns.Length > 0;

            if (!hasColumnsWithSearchValue && !hasGlobalSearch) {
                return;
            }
            sb.Append(" WHERE 1=1");

            int whereParameterIndex = 0;
            if (hasColumnsWithSearchValue)
            {
                foreach (var columnWithSearchValue in columnsWithSearchValue)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@w{whereParameterIndex++}";
                    parameter.Value = $"%{columnWithSearchValue.Search}%";
                    sb.Append($" AND {columnWithSearchValue.Name} LIKE {parameter.ParameterName}");
                    command.Parameters.Add(parameter);
                }
            }

            if (hasGlobalSearch)
            {
                sb.Append(" AND (0=1");
                foreach (var globallySearchableColumn in globallySearchableColumns)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@w{whereParameterIndex++}";
                    parameter.Value = $"%{inputModel.Search}%";
                    sb.Append($" OR {globallySearchableColumn.Name} LIKE {parameter.ParameterName}");
                    command.Parameters.Add(parameter);
                }
                sb.Append(")");
            }
        }

        private void AddOrderByClauseToCommand(DbCommand command, StringBuilder sb, DataTablesInputModel inputModel)
        {
            var sortableColumns = inputModel.ColumnDefinitions
                                        .Where(col => col.SortingPriority.HasValue)
                                        .OrderBy(col => col.SortingPriority.Value)
                                        .Select(col => $"{col.Name} {(col.SortingDirection == DataTablesSortingDirection.Descending ? "DESC" : "ASC")}")
                                        .ToArray();
            bool hasSortableColumns = sortableColumns.Any();

            if (!hasSortableColumns)
            {
                return;
            }

            string joinedColumns = string.Join(", ", sortableColumns);
            sb.Append($" ORDER BY {joinedColumns}");
        }

        private void AddLimitClauseToCommand(DbCommand command, StringBuilder sb, DataTablesInputModel inputModel)
        {
            var limitParameter = command.CreateParameter();
            limitParameter.ParameterName = "@limit";
            limitParameter.Value = inputModel.Length;
            command.Parameters.Add(limitParameter);

            var offsetParameter = command.CreateParameter();
            offsetParameter.ParameterName = "@offset";
            offsetParameter.Value = inputModel.Start;
            command.Parameters.Add(offsetParameter);

            sb.Append($" LIMIT {limitParameter.ParameterName} OFFSET {offsetParameter.ParameterName}");
        }
    }
}