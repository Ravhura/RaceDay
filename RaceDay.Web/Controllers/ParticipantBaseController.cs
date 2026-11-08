using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Base class for the Participant pages. Only a logged-in Participant may use anything that inherits from it.</summary>
[RequireRole(Roles.Participant)]
public abstract class ParticipantBaseController : AppController
{
    protected readonly ApiClient Api;

    protected ParticipantBaseController(ApiClient api)
    {
        Api = api;
    }
}