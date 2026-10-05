import apiClient from './api';

const payloadForRequest = (data) => {
    if (!data.medicalDocuments?.length) return data;
    const form = new FormData();
    Object.entries(data).forEach(([key, value]) => {
        if (key === 'medicalDocuments') {
            value.forEach((file) => form.append('MedicalDocuments', file));
        } else if (value !== null && value !== undefined && value !== '') {
            form.append(key, value);
        }
    });
    return form;
};

// Request Service
export const requestService = {
    // Get all requests (HR/Board)
    getAllRequests: async () => {
        const response = await apiClient.get('/transactions/all');
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

    // Create new request
    create: async (requestData) => {
        const response = await apiClient.post('/transactions', payloadForRequest(requestData));
        return response.data;
    },

    // Update request
    update: async (id, requestData) => {
        const response = await apiClient.put(`/transactions/${id}`, payloadForRequest(requestData));
        return response.data;
    },

    // Cancel request
    cancel: async (id) => {
        const response = await apiClient.post(`/transactions/${id}/cancel`);
        return response.data;
    },

    // Approve request (Manager/HR)
    approve: async (id, responseMessage = '') => {
        const response = await apiClient.post(`/transactions/${id}/approve`, { responseMessage });
        return response.data;
    },

    // Reject request (Manager/HR)
    reject: async (id, reason) => {
        const response = await apiClient.post(`/transactions/${id}/reject`, { responseMessage: reason });
        return response.data;
    },

    getMedicalDocuments: async (id) => {
        const response = await apiClient.get(`/transactions/${id}/medical-documents`);
        return response.data;
    },

    downloadMedicalDocument: async (requestId, medicalDocument) => {
        const response = await apiClient.get(
            `/transactions/${requestId}/medical-documents/${medicalDocument.id}`, { responseType: 'blob' }
        );
        const url = URL.createObjectURL(response.data);
        const link = document.createElement('a');
        link.href = url;
        link.download = medicalDocument.originalName;
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    },

};

export default requestService;
