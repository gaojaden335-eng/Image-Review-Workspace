import apiClient from './config'

export const exportApi = {
  // 导出选择数据
  exportSelections: async (queueId: number, format: 'csv' | 'json' = 'csv'): Promise<Blob | any> => {
    if (format === 'csv') {
      const response = await apiClient.get(`/export/selections`, {
        params: { queueId, format },
        responseType: 'blob',
      })
      return response.data
    } else {
      const response = await apiClient.get(`/export/selections`, {
        params: { queueId, format },
      })
      return response.data
    }
  },

  // 导出进度数据
  exportProgress: async (queueId?: number, format: 'csv' | 'json' = 'csv'): Promise<Blob | any> => {
    const params: any = { format }
    if (queueId) {
      params.queueId = queueId
    }

    if (format === 'csv') {
      const response = await apiClient.get(`/export/progress`, {
        params,
        responseType: 'blob',
      })
      return response.data
    } else {
      const response = await apiClient.get(`/export/progress`, {
        params,
      })
      return response.data
    }
  },
}

