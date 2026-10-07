import api from './config'
import type { UserDto, ApproveUserDto } from '../types'

// 获取所有游客用户（待审核）
export const getGuestUsers = async (): Promise<UserDto[]> => {
  const response = await api.get('/users/guests')
  return response.data
}

// 获取所有用户
export const getAllUsers = async (): Promise<UserDto[]> => {
  const response = await api.get('/users')
  return response.data
}

// 批准游客用户（升级为普通用户）
export const approveUser = async (data: ApproveUserDto): Promise<UserDto> => {
  const response = await api.post('/users/approve', data)
  return response.data
}

// 删除用户（拒绝游客申请）
export const deleteUser = async (userId: number): Promise<void> => {
  await api.delete(`/users/${userId}`)
}
