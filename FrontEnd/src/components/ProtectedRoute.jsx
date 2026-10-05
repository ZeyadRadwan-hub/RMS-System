import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/useAuth';
import Loading from './Loading';

const ProtectedRoute = ({ children, requiredRole = null }) => {
    const { user, loading } = useAuth();

    if (loading) {
        return <Loading message="Checking authentication..." />;
    }

    if (!user) {
        return <Navigate to="/login" replace />;
    }

    // Check role-based access
    if (requiredRole) {
        const hasAccess = Array.isArray(requiredRole)
            ? requiredRole.includes(user.role)
            : user.role === requiredRole;

        if (!hasAccess) {
            return <Navigate to="/dashboard" replace />;
        }
    }

    return children;
};

export default ProtectedRoute;
