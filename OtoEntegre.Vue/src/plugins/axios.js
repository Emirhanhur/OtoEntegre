import axios from 'axios'

const api = axios.create({
  baseURL: 'http://localhost:5079/',
  withCredentials: true
  // .NET Core API endpoint’in
})

api.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

export default api
