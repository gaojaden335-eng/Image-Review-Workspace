import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User, LoginDto, RegisterDto } from '../types'
import { authApi } from '../api/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)

  // JWT 由 HttpOnly Cookie 保存，前端只在当前会话保存非敏感用户信息。
  const storedUser = sessionStorage.getItem('user')
  if (storedUser) {
    try {
      user.value = JSON.parse(storedUser)
    } catch (e) {
      sessionStorage.removeItem('user')
    }
  }

  const isAuthenticated = computed(() => user.value !== null)
  const isAdmin = computed(() => user.value?.role === 'Admin')

  const login = async (credentials: LoginDto) => {
    const response = await authApi.login(credentials)
    user.value = {
      id: response.userId,
      username: response.username,
      role: response.role as User['role'],
    }
    
    sessionStorage.setItem('user', JSON.stringify(user.value))
  }

  const register = async (data: RegisterDto) => {
    const response = await authApi.register(data)
    user.value = {
      id: response.userId,
      username: response.username,
      role: response.role as User['role'],
    }
    
    sessionStorage.setItem('user', JSON.stringify(user.value))
  }

  const logout = async () => {
    try {
      await authApi.logout()
    } finally {
      user.value = null
      sessionStorage.removeItem('user')
    }
  }

  return {
    user,
    isAuthenticated,
    isAdmin,
    login,
    register,
    logout,
  }
})
