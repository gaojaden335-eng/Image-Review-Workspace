import apiClient from './config'
import type { LoginDto, RegisterDto, AuthResponse } from '../types'

export const authApi = {
  // 登录
  login: async (data: LoginDto): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>('/auth/login', data)
    return response.data
  },

  // 注册
  register: async (data: RegisterDto): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>('/auth/register', data)
    return response.data
  },

  // 测试接口
  test: async (): Promise<{ message: string }> => {
    const response = await apiClient.get('/auth/test')
    return response.data
  },
}

