namespace ImageAnnotationApp.Services
{
    public class ExportService
    {
        private readonly HttpClientService _httpClient;

        public ExportService()
        {
            _httpClient = HttpClientService.Instance;
        }

        public async Task<byte[]> ExportSelectionsAsync(int? queueId, string format)
        {
            try
            {
                var endpoint = queueId.HasValue
                    ? $"export/selections?queueId={queueId.Value}&format={format}"
                    : $"export/selections?format={format}";
                return await _httpClient.DownloadAsync(endpoint);
            }
            catch (Exception ex)
            {
                throw new Exception($"导出选择记录失败: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> ExportProgressAsync(int? queueId, string format)
        {
            try
            {
                var endpoint = queueId.HasValue
                    ? $"export/progress?queueId={queueId.Value}&format={format}"
                    : $"export/progress?format={format}";
                return await _httpClient.DownloadAsync(endpoint);
            }
            catch (Exception ex)
            {
                throw new Exception($"导出进度数据失败: {ex.Message}", ex);
            }
        }

        public void SaveToFile(byte[] data, string fileName)
        {
            try
            {
                using var saveFileDialog = new SaveFileDialog
                {
                    FileName = fileName,
                    Filter = "CSV 文件 (*.csv)|*.csv|JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, data);
                    MessageBox.Show($"文件已保存到: {saveFileDialog.FileName}", "成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
