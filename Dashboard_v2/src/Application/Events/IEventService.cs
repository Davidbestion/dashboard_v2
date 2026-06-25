using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Events;

/// <summary>
/// Application service for managing academic events: listing, creating, updating, and country/event-type lookups.
/// </summary>
public interface IEventService
{
    Task<List<EventDto>> GetMyEventsAsync(CancellationToken ct = default);
    Task<List<EventDto>> GetAllEventsAsync(CancellationToken ct = default);
    Task<List<EventDto>> GetAreaEventsAsync(CancellationToken ct = default);
    Task<List<CountryDto>> GetCountriesAsync(CancellationToken ct = default);
    Task<(Result Result, CountryDto? Country)> CreateCountryAsync(CreateCountryRequest request, CancellationToken ct = default);
    Task<List<EventTypeDto>> GetEventTypesAsync(CancellationToken ct = default);
    Task<(Result Result, int? EventId)> CreateEventAsync(CreateEventRequest request, CancellationToken ct = default);
    Task<Result> UpdateEventAsync(int id, UpdateEventRequest request, CancellationToken ct = default);
    Task<Result> DeleteEventAsync(int id, CancellationToken ct = default);

}
