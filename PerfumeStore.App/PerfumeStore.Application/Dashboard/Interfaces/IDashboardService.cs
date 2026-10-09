using PerfumeStore.Application.Dashboard.Dtos;

namespace PerfumeStore.Application.Dashboard.Interfaces {
    public interface IDashboardService {
        Task<DashboardDto> GetAsync(int? year = null, int? month = null);
    }
}
