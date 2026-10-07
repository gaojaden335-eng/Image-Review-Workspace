import apiClient from './config'
import type { Selection, CreateSelectionDto, UserProgress } from '../types'

export const selectionsApi = {
  // 创建选择记录
  create: async (data: CreateSelectionDto): Promise<Selection> => {
    const response = await apiClient.post<Selection>('/selections', data)
    return response.data
  },

  // 获取单个选择记录
  getById: async (id: number): Promise<Selection> => {
    const response = await apiClient.get<Selection>(`/selections/${id}`)
    return response.data
  },

  // 获取队列的选择记录
  getQueueSelections: async (queueId: number, userId?: number): Promise<Selection[]> => {
    const params = userId ? { userId } : {}
    const response = await apiClient.get<Selection[]>(`/selections/queue/${queueId}`, { params })
    return response.data
  },

  // 获取用户在队列中的进度
  getProgress: async (queueId: number): Promise<UserProgress> => {
    const response = await apiClient.get<UserProgress>(`/selections/progress/${queueId}`)
    return response.data
  },

  // 获取所有进度（管理员）
  getAllProgress: async (queueId?: number): Promise<UserProgress[]> => {
    const params = queueId ? { queueId } : {}
    const response = await apiClient.get<UserProgress[]>('/selections/progress/all', { params })
    return response.data
  },
}

