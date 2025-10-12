import axios from 'axios'

const api = axios.create({
// baseURL: 'https://api.kordteknoloji.com/',
   baseURL: 'http://localhost:5079/', 
  withCredentials: true

})

export default api
