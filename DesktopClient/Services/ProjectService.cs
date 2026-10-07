using ImageAnnotationApp.Models;

namespace ImageAnnotationApp.Services
{
    public class ProjectService
    {
        private readonly HttpClientService _httpClient;

        public ProjectService()
        {
            _httpClient = HttpClientService.Instance;
        }

        public async Task<List<Project>> GetAllAsync()
        {
            try
            {
                var projects = await _httpClient.GetAsync<List<Project>>("projects");
                return projects ?? new List<Project>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取项目列表失败: {ex.Message}", ex);
            }
        }

        public async Task<Project?> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetAsync<Project>($"projects/{id}");
            }
            catch (Exception ex)
            {
                throw new Exception($"获取项目详情失败: {ex.Message}", ex);
            }
        }

        public async Task<Project?> CreateAsync(CreateProjectDto dto)
        {
            try
            {
                return await _httpClient.PostAsync<Project>("projects", dto);
            }
            catch (Exception ex)
            {
                throw new Exception($"创建项目失败: {ex.Message}", ex);
            }
        }

        public async Task<Project?> UpdateAsync(int id, UpdateProjectDto dto)
        {
            try
            {
                return await _httpClient.PutAsync<Project>($"projects/{id}", dto);
            }
            catch (Exception ex)
            {
                throw new Exception($"更新项目失败: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                return await _httpClient.DeleteAsync($"projects/{id}");
            }
            catch (Exception ex)
            {
                throw new Exception($"删除项目失败: {ex.Message}", ex);
            }
        }
    }
}
