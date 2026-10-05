import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { BarChart3, FileText, Users, ClipboardList, UserCircle, TrendingUp, Settings, History, Building2, Menu, X, LogOut, LayoutDashboard, Calendar, Users2, ShieldCheck, UserCog } from 'lucide-react';
import { useAuth } from '../context/useAuth';
import { useNotification } from '../context/useNotification';
import './Sidebar.css';
import logo from '../../ElSewedy_Logo.png';

const Sidebar = () => {
    const { user, isHR, isBoard, isManager } = useAuth();
    const location = useLocation();
    const [collapsed, setCollapsed] = useState(false);
    const [mobileOpen, setMobileOpen] = useState(false);

    const isActive = (path) => location.pathname === path;

    // Navigation items based on role
    const getNavigationItems = () => {
        const items = [];

        // Common items for all roles
        items.push({
            path: '/dashboard',
            icon: BarChart3,
            label: 'Dashboard',
        });

        // My Requests - NOT for Board members
        if (!isBoard) {
            items.push({
                path: '/my-requests',
                icon: FileText,
                label: 'My Requests',
            });
        }

        // Manager-specific items
        if (isManager) {
            items.push({
                path: '/team-requests',
                icon: Users,
                label: 'Team Requests',
            });
        }

        // HR-specific items
        if (isHR) {
            items.push({
                path: '/all-requests',
                icon: ClipboardList,
                label: 'All Requests',
            });
            items.push({
                path: '/employees',
                icon: UserCircle,
                label: 'Employees',
            });
        }

        // Board-specific items
        if (isBoard) {
            items.push({
                path: '/history',
                icon: History,
                label: 'History',
            });
            items.push({
                path: '/hr-requests',
                icon: Building2,
                label: 'HR Requests',
            });
        }

        // Leave Balance (all roles except Board)
        if (!isBoard) {
            items.push({
                path: '/leave-balance',
                icon: TrendingUp,
                label: 'Leave Balance',
            });
        }

        // Profile (all roles)
        items.push({
            path: '/profile',
            icon: UserCog,
            label: 'Profile Settings',
        });

        return items;
    };

    const { showConfirm } = useNotification();
    const { logout } = useAuth();

    const handleLogout = async () => {
        const confirmed = await showConfirm('Are you sure you want to sign out?', 'Sign Out');
        if (confirmed) {
            logout();
        }
    };

    const navigationItems = getNavigationItems();

    return (
        <>
        <button type="button" className={`mobile-menu-button ${mobileOpen ? 'is-open' : ''}`} aria-label="Open navigation"
            aria-expanded={mobileOpen} aria-controls="rms-sidebar" onClick={() => setMobileOpen(true)}>
            <Menu size={24} />
        </button>
        {mobileOpen && <button type="button" className="mobile-sidebar-backdrop"
            aria-label="Close navigation" onClick={() => setMobileOpen(false)} />}
        <div id="rms-sidebar" className={`sidebar ${collapsed ? 'collapsed' : ''} ${mobileOpen ? 'open' : ''}`}>
            <div className="sidebar-header">
                <div className="sidebar-logo">
                    <img src={logo} alt="ElSewedy Logo" className={`sidebar-logo-img ${collapsed ? 'collapsed' : ''}`} />
                    {!collapsed && <span className="sidebar-logo-text">RMS System</span>}
                </div>
                <button
                    className="sidebar-toggle"
                    onClick={() => window.matchMedia('(max-width: 768px)').matches
                        ? setMobileOpen(false) : setCollapsed(!collapsed)}
                    aria-label={mobileOpen ? 'Close navigation' : 'Toggle sidebar'}
                >
                    {collapsed ? <Menu size={20} /> : <X size={20} />}
                </button>
            </div>

            <div className="sidebar-nav-container">
                <nav className="sidebar-nav">
                    {navigationItems.map((item) => {
                        const IconComponent = item.icon;
                        const active = isActive(item.path);
                        return (
                            <Link
                                key={item.path}
                                to={item.path}
                                onClick={() => setMobileOpen(false)}
                                className={`nav-item ${active ? 'active' : ''}`}
                                title={collapsed ? item.label : ''}
                            >
                                <span className="nav-icon">
                                    <IconComponent size={20} />
                                </span>
                                {!collapsed && <span className="nav-label">{item.label}</span>}
                                {active && !collapsed && <div className="active-indicator" />}
                            </Link>
                        );
                    })}
                </nav>
            </div>

            <div className="sidebar-footer">
                <div className="sidebar-user">
                    <div className="user-avatar">
                        {user?.name?.charAt(0) || 'U'}
                    </div>
                    {!collapsed && (
                        <div className="user-info">
                            <div className="user-name">{user?.name}</div>
                            <div className="user-role">{user?.role}</div>
                        </div>
                    )}
                </div>

                <button
                    className="logout-button"
                    onClick={handleLogout}
                    title={collapsed ? "Logout" : ""}
                >
                    <span className="nav-icon">
                        <LogOut size={20} />
                    </span>
                    {!collapsed && <span className="nav-label">Logout</span>}
                </button>
            </div>
        </div>
        </>
    );
};

export default Sidebar;
