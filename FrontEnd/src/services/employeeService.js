import apiClient from './api';

// Employee Service (HR only)
export const employeeService = {
    // Get all employees
    getAll: async () => {
        const response = await apiClient.get('/employees');
        return response.data;
    },

    getSubstitutes: async () => {
        const response = await apiClient.get('/employees/substitutes');
        return response.data;
    },

    // Get employee by ID
    getById: async (id) => {
        const response = await apiClient.get(`/employees/${id}`);
        return response.data;
    },

    // Create new employee
    create: async (data) => {
        const response = await apiClient.post('/employees', data);
        return response.data;
    },

    // Update employee
    update: async (id, data) => {
        const response = await apiClient.put(`/employees/${id}`, data);
        return response.data;
    },

    // Delete employee (soft delete)
    delete: async (id) => {
        const response = await apiClient.delete(`/employees/${id}`);
        return response.data;
    },
};

export default employeeService;
