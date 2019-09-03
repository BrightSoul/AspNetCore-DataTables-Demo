using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using DataTables.Models.Configuration;
using DataTables.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;

namespace DataTables.Models
{
    public class DataTablesConfigurationModelBinder : IModelBinder
    {
        private readonly DataTablesService dataTablesService;

        public DataTablesConfigurationModelBinder(DataTablesService dataTablesService)
        {
            this.dataTablesService = dataTablesService;
        }
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            string controllerName = bindingContext.ActionContext.ActionDescriptor.RouteValues["controller"];
            string actionName = bindingContext.ActionContext.ActionDescriptor.RouteValues["action"];

            DataTablesConfiguration configuration = dataTablesService.GetConfiguration(controllerName, actionName);
            bindingContext.Result = ModelBindingResult.Success(configuration);
            return Task.CompletedTask;
        }
    }
}