import React from 'react';
import { useAuth } from '../context/useAuth';
import './Header.css';

const Header = ({ title }) => {
    const { user, logout } = useAuth();

    return (
        <header className="header">
            <div className="header-content">
                <div className="header-left">
                    <h1 className="header-title">{title}</h1>
                </div>
                <div className="header-right">
                    <div className="header-user">
                        <span className="header-user-name">{user?.name}</span>
                        <span className="header-user-code">#{user?.code}</span>
                    </div>
                    <button className="btn btn-secondary btn-sm" onClick={logout}>
                        Logout
                    </button>
                </div>
            </div>
        </header>
    );
};

export default Header;
