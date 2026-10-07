using ImageAnnotationApp.Models;

namespace ImageAnnotationApp.Services
{
    public class SelectionService
    {
        private readonly HttpClientService _httpClient;

        public SelectionService()
        {
            _httpClient = HttpClientService.Instance;
        }

        public async Task<Selection?> CreateAsync(CreateSelectionDto dto)
        {
            try
            {
                return await _httpClient.PostAsync<Selection>("selections", dto);
            }
            catch (Exception ex)
            {
                throw new Exception($"创建选择记录失败: {ex.Message}", ex);
            }
        }

        public async Task<Selection?> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetAsync<Selection>($"selections/{id}");
            }
            catch (Exception ex)
            {
                throw new Exception($"获取选择记录失败: {ex.Message}", ex);
            }
        }

        public async Task<List<Selection>> GetQueueSelectionsAsync(int queueId, int? userId = null)
        {
            try
            {
                var endpoint = userId.HasValue
                    ? $"selections/queue/{queueId}?userId={userId.Value}"
                    : $"selections/queue/{queueId}";
                var selections = await _httpClient.GetAsync<List<Selection>>(endpoint);
                return selections ?? new List<Selection>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取队列选择记录失败: {ex.Message}", ex);
            }
        }

        public async Task<UserProgress?> GetProgressAsync(int queueId)
        {
            try
            {
                return await _httpClient.GetAsync<UserProgress>($"selections/progress/{queueId}");
            }
            catch (Exception ex)
            {
                throw new Exception($"获取进度失败: {ex.Message}", ex);
            }
        }

        public async Task<List<UserProgress>> GetAllProgressAsync(int? queueId = null)
        {
            try
            {
                var endpoint = queueId.HasValue
                    ? $"selections/progress/all?queueId={queueId.Value}"
                    : "selections/progress/all";
                var progress = await _httpClient.GetAsync<List<UserProgress>>(endpoint);
                return progress ?? new List<UserProgress>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取所有进度失败: {ex.Message}", ex);
            }
        }
    }
}
