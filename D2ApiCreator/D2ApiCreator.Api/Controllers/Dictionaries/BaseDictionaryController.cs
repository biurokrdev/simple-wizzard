using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers.Dictionaries;

/// <summary>
/// Klasa bazowa dla kontrollerów słownikowych dostarczających dane zewnętrzne
/// Te kontrolery są wyłącznie do dostarczania danych słownikowych z aplikacji zewnętrznych,
/// nie zarządzają encjami utworzonymi w naszej aplikacji.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/dictionaries/[controller]")]
[Asp.Versioning.ApiVersion("1.0")]
[Produces("application/json")]
public abstract class BaseDictionaryController : ControllerBase
{
}
