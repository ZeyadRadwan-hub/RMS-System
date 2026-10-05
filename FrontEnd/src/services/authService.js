import apiClient from './api';

// Authentication Service
export const authService = {
    // Login
    login: async (code, password) => {
        const response = await apiClient.post('/auth/login', { code, password });
        return response.data;
    },

    changePassword: async (currentPassword, newPassword) => {
        await apiClient.post('/auth/change-password', { currentPassword, newPassword });
    },

    // Logout
    logout: async () => {
        try {
            if (sessionStorage.getItem('rmsSessionToken')) {
                await apiClient.post('/auth/logout');
            }
        } finally {
            sessionStorage.removeItem('rmsSessionToken');
            localStorage.removeItem('user');
            window.location.href = '/login';
        }
    },

    // Get current user
    getCurrentUser: async () => {
        if (!sessionStorage.getItem('rmsSessionToken')) return null;
        const response = await apiClient.get('/auth/me');
        return response.data;
    },

    // Keep only the opaque credential in session storage, not a trusted role.
    saveUser: (user) => {
        if (user?.token) sessionStorage.setItem('rmsSessionToken', user.token);
        localStorage.removeItem('user');
    },

    // Check if user is authenticated
    isAuthenticated: () => {
        return !!sessionStorage.getItem('rmsSessionToken');
    },
};

export default authService;
