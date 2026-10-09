using MediatR;
using PerfumeStore.Application.Dashboard.Dtos;
using PerfumeStore.Application.Dashboard.Interfaces;

namespace PerfumeStore.Application.Dashboard.Queries.GetDashboard {
    public class GetDashboardQueryHandler(IDashboardService dashboardService) : IRequestHandler<GetDashboardQuery, DashboardDto> {
        public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken) {
            return await dashboardService.GetAsync(request.Year, request.Month);
        }
    }
}
