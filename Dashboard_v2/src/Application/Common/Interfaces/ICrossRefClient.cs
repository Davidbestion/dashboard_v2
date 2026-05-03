using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Publications;

namespace Dashboard_v2.Application.Common.Interfaces;

public interface ICrossRefClient
{
    Task<PublicationCrossRefDto?> GetWorkByDoiAsync(string doi, CancellationToken ct = default);
    Task<List<PublicationCrossRefDto>> SearchWorksByTitleAsync(string title, int rows = 5, CancellationToken ct = default);
}
