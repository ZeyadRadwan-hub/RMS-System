import React, { useState, useEffect } from 'react';
import authService from '../services/authService';
import AuthContext from './auth-context';

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        let active = true;
        authService.getCurrentUser()
            .then(currentUser => { if (active) setUser(currentUser); })
            .catch(() => { if (active) setUser(null); })
            .finally(() => { if (active) setLoading(false); });
        return () => { active = false; };
    }, []);

    const login = async (code, password) => {
        try {
            const userData = await authService.login(code, password);
            authService.saveUser(userData);
            setUser(userData);
            return { success: true, data: userData };
        } catch (error) {
            return {
                success: false,
                error: error.response?.data?.message || 'Login failed'
            };
        }
    };

    const logout = () => {
        void authService.logout();
        setUser(null);
    };

    const value = {
        user,
        login,
        logout,
        loading,
        isAuthenticated: !!user,
        isEmployee: user?.role === 'Employee',
        isManager: user?.role === 'Manager',
        isHR: user?.role === 'HR',
        isBoard: user?.role === 'Board',
    };

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

