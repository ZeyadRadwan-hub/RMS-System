import apiClient from './api';

// Transaction Service (Leave Requests)
export const transactionService = {
    // Create new leave request
    create: async (data) => {
        const response = await apiClient.post('/transactions', data);
        return response.data;
    },

    // Update leave request
    update: async (id, data) => {
        const response = await apiClient.put(`/transactions/${id}`, data);
        return response.data;
    },

    // Cancel leave request
    cancel: async (id) => {
        const response = await apiClient.post(`/transactions/${id}/cancel`);
        return response.data;
    },

    // Approve leave request
    approve: async (id, responseMessage) => {
        const response = await apiClient.post(`/transactions/${id}/approve`, {
            transactionId: id,
            responseMessage,
        });
        return response.data;
    },

    // Reject leave request
    reject: async (id, responseMessage) => {
        const response = await apiClient.post(`/transactions/${id}/reject`, {
            transactionId: id,
            responseMessage,
        });
        return response.data;
    },

    // Get transaction by ID
    getById: async (id) => {
        const response = await apiClient.get(`/transactions/${id}`);
        return response.data;
    },

    // Get my requests
    getMyRequests: async () => {
        const response = await apiClient.get('/transactions/my-requests');
        return response.data;
    },

    // Get team requests (Manager)
    getTeamRequests: async () => {
        const response = await apiClient.get('/transactions/my-team-requests');
        return response.data;
    },

    // Get all requests (HR/Board)
    getAllRequests: async () => {
        const response = await apiClient.get('/transactions/all');
        return response.data;
    },

    // Filter requests
    filter: async (filters) => {
        const response = await apiClient.post('/transactions/filter', filters);
        return response.data;
    },
};

export default transactionService;
