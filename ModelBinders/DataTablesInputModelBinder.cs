using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using DataTables.Models.Configuration;
using DataTables.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;

namespace DataTables.Models
{
    public class DataTablesInputModelBinder : IModelBinder
    {
        private readonly DataTablesService dataTablesService;

        public DataTablesInputModelBinder(DataTablesService dataTablesService)
        {
            this.dataTablesService = dataTablesService;
        }
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var request = bindingContext.HttpContext.Request;
            IFormCollection value = request.HasFormContentType ? request.Form : FormCollection.Empty;

            string controllerName = bindingContext.ActionContext.ActionDescriptor.RouteValues["controller"];
            string actionName = bindingContext.ActionContext.ActionDescriptor.RouteValues["action"];

            DataTablesConfiguration configuration = dataTablesService.GetConfiguration(controllerName, actionName);

            var inputModel = DataTablesInputModel.FromFormCollection(value, configuration);
            bindingContext.Result = ModelBindingResult.Success(inputModel);
            return Task.CompletedTask;
        }
    }
}