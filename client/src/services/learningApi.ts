import { apiGet } from './apiClient'
import type { LearningStats } from '../types/learning'

export const learningApi = {
  getStats: () => apiGet<LearningStats>('/api/learning/stats'),
}
