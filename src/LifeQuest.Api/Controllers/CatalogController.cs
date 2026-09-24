using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Application.Catalog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeQuest.Api.Controllers;

[AllowAnonymous]
[Route("api/v1/catalog")]
public sealed class CatalogController(ISender sender) : ApiControllerBase
{
    [HttpGet("interests")]
    public async Task<IActionResult> Interests(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetInterestCatalogQuery(), cancellationToken));
}
