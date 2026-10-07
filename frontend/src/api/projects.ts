import apiClient from './config'
import type { Project, CreateProjectDto } from '../types'

export const projectsApi = {
  // 获取所有项目
  getAll: async (): Promise<Project[]> => {
    const response = await apiClient.get<Project[]>('/projects')
    return response.data
  },

  // 获取单个项目
  getById: async (id: number): Promise<Project> => {
    const response = await apiClient.get<Project>(`/projects/${id}`)
    return response.data
  },

  // 创建项目
  create: async (data: CreateProjectDto): Promise<Project> => {
    const response = await apiClient.post<Project>('/projects', data)
    return response.data
  },

  // 更新项目
  update: async (id: number, data: CreateProjectDto): Promise<Project> => {
    const response = await apiClient.put<Project>(`/projects/${id}`, data)
    return response.data
  },

  // 删除项目
  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/projects/${id}`)
  },
}

