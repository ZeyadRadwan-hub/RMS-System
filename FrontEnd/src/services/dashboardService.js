import apiClient from './api';

// Dashboard Service
export const dashboardService = {
    // Get dashboard statistics
    getStats: async (filters = {}) => {
        const response = await apiClient.post('/dashboard/stats', filters);
        return response.data;
    },

    // Get chart data
    getCharts: async (filters = {}) => {
        const response = await apiClient.post('/dashboard/charts', filters);
        return response.data;
    },
};

export default dashboardService;
