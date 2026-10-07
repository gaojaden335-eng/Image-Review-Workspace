using ImageAnnotationApp.Models;

namespace ImageAnnotationApp.Services
{
    public class QueueService
    {
        private readonly HttpClientService _httpClient;

        public QueueService()
        {
            _httpClient = HttpClientService.Instance;
        }

        public async Task<List<Models.Queue>> GetAllAsync(int? projectId = null)
        {
            try
            {
                var endpoint = projectId.HasValue
                    ? $"queues?projectId={projectId.Value}"
                    : "queues";
                var queues = await _httpClient.GetAsync<List<Models.Queue>>(endpoint);
                return queues ?? new List<Models.Queue>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取队列列表失败: {ex.Message}", ex);
            }
        }

        public async Task<Models.Queue?> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetAsync<Models.Queue>($"queues/{id}");
            }
            catch (Exception ex)
            {
                throw new Exception($"获取队列详情失败: {ex.Message}", ex);
            }
        }

        public async Task<Models.Queue?> CreateAsync(CreateQueueDto dto)
        {
            try
            {
                return await _httpClient.PostAsync<Models.Queue>("queues", dto);
            }
            catch (Exception ex)
            {
                throw new Exception($"创建队列失败: {ex.Message}", ex);
            }
        }

        public async Task<Models.Queue?> UpdateAsync(int id, UpdateQueueDto dto)
        {
            try
            {
                return await _httpClient.PutAsync<Models.Queue>($"queues/{id}", dto);
            }
            catch (Exception ex)
            {
                throw new Exception($"更新队列失败: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                return await _httpClient.DeleteAsync($"queues/{id}");
            }
            catch (Exception ex)
            {
                throw new Exception($"删除队列失败: {ex.Message}", ex);
            }
        }
    }
}
