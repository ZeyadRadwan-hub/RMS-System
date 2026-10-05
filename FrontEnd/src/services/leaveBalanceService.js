import apiClient from './api';

// Leave Balance Service
export const leaveBalanceService = {
    // Get leave balance for specific employee
    getBalance: async (employeeId, asOfDate = null) => {
        const params = asOfDate ? { asOfDate } : {};
        const response = await apiClient.get(`/leavebalance/${employeeId}`, { params });
        return response.data;
    },

    // Get my leave balance
    getMyBalance: async (asOfDate = null) => {
        const params = asOfDate ? { asOfDate } : {};
        const response = await apiClient.get('/leavebalance/my-balance', { params });
        return response.data;
    },

    // Get team balances (Manager)
    getTeamBalances: async (asOfDate = null) => {
        const params = asOfDate ? { asOfDate } : {};
        const response = await apiClient.get('/leavebalance/team-balances', { params });
        return response.data;
    },

    // Get department balances (HR/Board)
    getDepartmentBalances: async (departmentId, asOfDate = null) => {
        const params = asOfDate ? { asOfDate } : {};
        const response = await apiClient.get(`/leavebalance/department/${departmentId}`, { params });
        return response.data;
    },

    // Get all balances (HR/Board)
    getAllBalances: async (asOfDate = null) => {
        const params = asOfDate ? { asOfDate } : {};
        const response = await apiClient.get('/leavebalance/all', { params });
        return response.data;
    },
};

export default leaveBalanceService;
