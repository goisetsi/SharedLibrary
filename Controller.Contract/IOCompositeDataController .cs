﻿using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using System.Web.Http.Routing;
using System.Web.OData;

namespace Shared.Controller.Contract
{
    public interface IODataController<TEntity,TFirstKey, TSecondKey> where TEntity : class
    {
        HttpConfiguration Configuration { get; set; }
        HttpControllerContext ControllerContext { get; set; }
        HttpActionContext ActionContext { get; set; }
        ModelStateDictionary ModelState { get; }
        HttpRequestMessage Request { get; set; }
        HttpRequestContext RequestContext { get; set; }
        UrlHelper Url { get; set; }
        IPrincipal User { get; set; }
        IQueryable<TEntity> Get();
        SingleResult<TEntity> Get([FromODataUri] TFirstKey firstKeykey, [FromODataUri] TSecondKey secondKey);
        IHttpActionResult Put([FromODataUri] TFirstKey firstKey, [FromODataUri] TSecondKey secondKey, Delta<TEntity> patch);
        IHttpActionResult Post(TEntity entity);
        IHttpActionResult Patch([FromODataUri] TFirstKey firstKey, [FromODataUri] TSecondKey secondKey, Delta<TEntity> patch);
        IHttpActionResult Delete([FromODataUri] TFirstKey firstKey, [FromODataUri] TSecondKey secondKey);

        Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext,
            CancellationToken cancellationToken);
    }
}