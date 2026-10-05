import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/useAuth';

const BlockBoardRoute = ({ children }) => {
    const { isBoard } = useAuth();

    if (isBoard) {
        return <Navigate to="/dashboard" replace />;
    }

    return children;
};

export default BlockBoardRoute;
