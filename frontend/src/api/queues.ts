import apiClient from './config'
import type { Queue, CreateQueueDto } from '../types'

export const queuesApi = {
  // 获取所有队列
  getAll: async (projectId?: number): Promise<Queue[]> => {
    const params = projectId ? { projectId } : {}
    const response = await apiClient.get<Queue[]>('/queues', { params })
    return response.data
  },

  // 获取单个队列
  getById: async (id: number): Promise<Queue> => {
    const response = await apiClient.get<Queue>(`/queues/${id}`)
    return response.data
  },

  // 创建队列
  create: async (data: CreateQueueDto): Promise<Queue> => {
    const response = await apiClient.post<Queue>('/queues', data)
    return response.data
  },

  // 更新队列
  update: async (id: number, data: Partial<CreateQueueDto>): Promise<Queue> => {
    const response = await apiClient.put<Queue>(`/queues/${id}`, data)
    return response.data
  },

  // 删除队列
  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/queues/${id}`)
  },
}

