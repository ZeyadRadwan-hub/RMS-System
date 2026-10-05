import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { BarChart, Bar, PieChart, Pie, Cell, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { Palmtree, Umbrella, HeartPulse, FileText, AlertCircle, Users, User, TrendingUp, Clock } from 'lucide-react';
import { useAuth } from '../context/useAuth';
import requestService from '../services/requestService';
import Card from '../components/Card';
import Loading from '../components/Loading';
import CustomSelect from '../components/CustomSelect';
import CustomDatePicker from '../components/CustomDatePicker';
import './Dashboard.css';
import { overlapsDateRange } from '../utils/dateRange';

// ── Status ID → clean English display label ────────────────────────────────
// Maps raw DB status strings to normalized labels so the UI is always English
// regardless of what text the backend returns.
const STATUS_LABEL_MAP = {
    'pending':              'Pending',
    'pending hr':           'Pending HR',
    'pendinghR':            'Pending HR',
    'approved by hr':       'Approved',
    'approved':             'Approved',
    'rejected by manager':  'Rejected (Manager)',
    'rejected by hr':       'Rejected (HR)',
    'rejected':             'Rejected',
    'cancelled by employee':'Cancelled',
    'cancelled':            'Cancelled',
};

const normalizeStatus = (raw) => {
    if (!raw) return 'Unknown';
    return STATUS_LABEL_MAP[raw.toLowerCase()] ?? raw;
};

const STATUS_OPTIONS = [
    { value: '', label: 'All Statuses' },
    { value: '1', label: 'Pending' },
    { value: '2', label: 'Pending HR' },
    { value: '3', label: 'Approved' },
    { value: '4', label: 'Rejected (Manager)' },
    { value: '5', label: 'Rejected (HR)' },
    { value: '6', label: 'Cancelled' },
];

// ── Professional chart palette — blue-first, red reserved for true alerts ──
const CHART_COLORS = ['#3B82F6', '#10B981', '#F59E0B', '#8B5CF6', '#14B8A6', '#C4161C'];

const Dashboard = () => {
    const { user, isManager, isHR, isBoard } = useAuth();

    // Tab state: 'my' or 'team'
    const [activeTab, setActiveTab] = useState('my');

    // Data
    const [myRequests, setMyRequests] = useState([]);
    const [teamRequests, setTeamRequests] = useState([]);
    const [loadingMy, setLoadingMy] = useState(true);
    const [loadingTeam, setLoadingTeam] = useState(false);
    const [error, setError] = useState(null);

    // Filters — default to Approved so stats show meaningful leave days taken
    const [filters, setFilters] = useState({
        fromDate: '',
        toDate: '',
        statusID: '3', // Default: Approved
    });

    const fetchMyRequests = useCallback(async () => {
        try {
            setLoadingMy(true);
            setError(null);
            // Board sees all company requests; others see their own
            const data = isBoard
                ? await requestService.getAllRequests()
                : await requestService.getMyRequests();
            setMyRequests(data);
        } catch (err) {
            console.error('Error fetching requests:', err);
            setError(err.response?.data?.message || err.message || 'Failed to load requests');
        } finally {
            setLoadingMy(false);
        }
    }, [isBoard]);

    const fetchTeamRequests = useCallback(async () => {
        try {
            setLoadingTeam(true);
            const data = isHR
                ? await requestService.getAllRequests()
                : await requestService.getTeamRequests();
            setTeamRequests(data);
        } catch (err) {
            console.error('Error fetching team requests:', err);
            setError(err.response?.data?.message || err.message || 'Failed to load team requests');
        } finally {
            setLoadingTeam(false);
        }
    }, [isHR]);

    useEffect(() => {
        fetchMyRequests();
    }, [fetchMyRequests]);

    useEffect(() => {
        if (activeTab === 'team' && (isManager || isHR) && teamRequests.length === 0) {
            fetchTeamRequests();
        }
    }, [activeTab, isManager, isHR, teamRequests.length, fetchTeamRequests]);

    // Active dataset based on tab
    const currentRequests = activeTab === 'my' ? myRequests : teamRequests;

    // Apply filters reactively — summary cards and charts both derive from this
    const filteredRequests = useMemo(() => {
        let result = [...currentRequests];

        if (filters.statusID) {
            result = result.filter(r => r.statusID === parseInt(filters.statusID));
        }
        if (filters.fromDate || filters.toDate) {
            result = result.filter(r => overlapsDateRange(r, filters.fromDate, filters.toDate));
        }

        return result;
    }, [currentRequests, filters]);

    // ── Stats — sum of leave DAYS taken, scoped to filteredRequests ─────────
    const stats = useMemo(() => {
        const sum = (keyword) =>
            filteredRequests
                .filter(r => r.transactionTypeName?.toLowerCase().includes(keyword))
                .reduce((acc, r) => acc + Math.abs(r.calculatedDays || 0), 0);

        return {
            annual:  sum('annual'),
            casual:  sum('casual'),
            sick:    sum('sick'),
            total:   filteredRequests.length,
        };
    }, [filteredRequests]);

    // ── Custom Tooltip ───────────────────────────────────────────────────────
    const CustomTooltip = ({ active, payload, label }) => {
        if (active && payload?.length) {
            return (
                <div style={{
                    backgroundColor: '#fff',
                    padding: '10px 14px',
                    border: '1px solid #e2e8f0',
                    borderRadius: '8px',
                    boxShadow: '0 4px 12px rgba(0,0,0,0.08)',
                }}>
                    <p style={{ margin: 0, fontWeight: 700, color: '#1e293b', fontSize: '13px' }}>{label}</p>
                    <p style={{ margin: '4px 0 0', color: payload[0].fill, fontSize: '13px', fontWeight: 600 }}>
                        {payload[0].value} {payload[0].value === 1 ? 'Request' : 'Requests'}
                    </p>
                </div>
            );
        }
        return null;
    };

    // ── Chart data ───────────────────────────────────────────────────────────
    const chartData = useMemo(() => {
        // Bar chart: leave days by type
        const barData = [
            { label: 'Annual',  value: stats.annual  },
            { label: 'Casual',  value: stats.casual  },
            { label: 'Sick',    value: stats.sick    },
        ]
            .filter(d => d.value > 0)
            .map((d, i) => ({ ...d, fill: CHART_COLORS[i % CHART_COLORS.length] }));

        // Pie chart: status distribution with normalized labels
        const statusGroups = {};
        filteredRequests.forEach(r => {
            const label = normalizeStatus(r.statusName);
            statusGroups[label] = (statusGroups[label] || 0) + 1;
        });

        const pieData = Object.entries(statusGroups).map(([label, value]) => ({
            label,
            value,
            percentage: filteredRequests.length > 0 ? (value / filteredRequests.length) * 100 : 0,
        }));

        return { barData, pieData };
    }, [filteredRequests, stats]);

    const handleFilterChange = (key, value) => {
        setFilters(prev => ({ ...prev, [key]: value }));
    };

    const handleClearFilters = () => {
        setFilters({ fromDate: '', toDate: '', statusID: '' });
    };

    const isLoading = activeTab === 'my' ? loadingMy : loadingTeam;

    if (isLoading) {
        return <Loading message={activeTab === 'my' ? 'Loading your dashboard...' : 'Loading team dashboard...'} />;
    }

    if (error) {
        return (
            <div className="dashboard-page">
                <div className="alert alert-error">
                    <AlertCircle className="alert-icon" size={20} />
                    <div>
                        <strong>Error loading dashboard:</strong>
                        <p>{error}</p>
                        <button className="btn btn-primary" onClick={activeTab === 'my' ? fetchMyRequests : fetchTeamRequests}>
                            Retry
                        </button>
                    </div>
                </div>
            </div>
        );
    }

    const showTeamTab = isManager || isHR;

    // Role-specific banner text
    const bannerTitle = isBoard
        ? 'Company Dashboard'
        : isHR
            ? 'HR Dashboard'
            : isManager
                ? 'Team Dashboard'
                : 'My Dashboard';

    const bannerSubtitle = isBoard
        ? 'Company-wide leave activity overview'
        : isHR
            ? 'Manage and monitor all employee leave requests'
            : isManager
                ? `Manage your team's leave requests and approvals`
                : `Welcome back, ${user?.name}`;

    return (
        <div className="dashboard-page">

            {/* ── Hero Banner ──────────────────────────────────────────────── */}
            <div className="dashboard-banner">
                <div className="dashboard-banner-content">
                    <h1>{bannerTitle}</h1>
                    <p>{bannerSubtitle}</p>
                </div>
            </div>

            {/* ── Role Tabs (Employee / Manager / HR only — not Board) ──── */}
            {showTeamTab && (
                <div className="dashboard-tabs">
                    <button
                        className={`dashboard-tab ${activeTab === 'my' ? 'active' : ''}`}
                        onClick={() => setActiveTab('my')}
                    >
                        <User size={16} />
                        <span>My Dashboard</span>
                    </button>
                    <button
                        className={`dashboard-tab ${activeTab === 'team' ? 'active' : ''}`}
                        onClick={() => setActiveTab('team')}
                    >
                        <Users size={16} />
                        <span>{isHR ? 'All Employees' : 'My Team'}</span>
                    </button>
                </div>
            )}

            {/* ── No-Team Message ──────────────────────────────────────────── */}
            {activeTab === 'team' && !isManager && !isHR && (
                <Card className="not-manager-card">
                    <div className="not-manager-content">
                        <Users size={48} className="not-manager-icon" />
                        <h2>No Team Assigned</h2>
                        <p>You are not currently a manager. When employees are assigned to you, their requests will appear here.</p>
                    </div>
                </Card>
            )}

            {/* ── Dashboard Content ────────────────────────────────────────── */}
            {(activeTab === 'my' || (activeTab === 'team' && (isManager || isHR))) && (
                <>
                    {/* ── Filters Card ─────────────────────────────────────── */}
                    <Card className="filters-card mb-3" hoverable={false}>
                        <div className="dashboard-filter-row">
                            <div className="filter-input-group">
                                <CustomDatePicker
                                    label="From Date"
                                    value={filters.fromDate}
                                    onChange={(val) => handleFilterChange('fromDate', val)}
                                />
                            </div>
                            <div className="filter-input-group">
                                <CustomDatePicker
                                    label="To Date"
                                    value={filters.toDate}
                                    onChange={(val) => handleFilterChange('toDate', val)}
                                />
                            </div>
                            <div className="filter-input-group">
                                <CustomSelect
                                    label="Status"
                                    value={filters.statusID}
                                    onChange={(val) => handleFilterChange('statusID', val)}
                                    options={STATUS_OPTIONS}
                                />
                            </div>
                            <div className="filter-action-group">
                                <button className="clear-filters-btn" onClick={handleClearFilters}>
                                    Clear All
                                </button>
                            </div>
                        </div>
                    </Card>

                    {/* ── Stats Cards (reactive to filters) ────────────────── */}
                    <div className="stats-grid">
                        <Card hoverable={false} className="stat-card stat-annual">
                            <div className="stat-content">
                                <div className="stat-label">Annual Leave</div>
                                <div className={`stat-value ${stats.annual === 0 ? 'zero-state' : ''}`}>
                                    {stats.annual.toFixed(1)} <span className="stat-unit">days</span>
                                </div>
                            </div>
                            <div className="stat-icon-container">
                                <Palmtree size={20} />
                            </div>
                        </Card>

                        <Card hoverable={false} className="stat-card stat-casual">
                            <div className="stat-content">
                                <div className="stat-label">Casual Leave</div>
                                <div className={`stat-value ${stats.casual === 0 ? 'zero-state' : ''}`}>
                                    {stats.casual.toFixed(1)} <span className="stat-unit">days</span>
                                </div>
                            </div>
                            <div className="stat-icon-container">
                                <Umbrella size={20} />
                            </div>
                        </Card>

                        <Card hoverable={false} className="stat-card stat-sick">
                            <div className="stat-content">
                                <div className="stat-label">Sick Leave</div>
                                <div className={`stat-value ${stats.sick === 0 ? 'zero-state' : ''}`}>
                                    {stats.sick.toFixed(1)} <span className="stat-unit">days</span>
                                </div>
                            </div>
                            <div className="stat-icon-container">
                                <HeartPulse size={20} />
                            </div>
                        </Card>

                        <Card hoverable={false} className="stat-card stat-total">
                            <div className="stat-content">
                                <div className="stat-label">Total Requests</div>
                                <div className={`stat-value ${stats.total === 0 ? 'zero-state' : ''}`}>{stats.total}</div>
                            </div>
                            <div className="stat-icon-container">
                                <FileText size={20} />
                            </div>
                        </Card>
                    </div>

                    {/* ── Charts ───────────────────────────────────────────── */}
                    <div className="charts-grid">
                        <Card title="Leave Days by Type" className="chart-card">
                            {chartData.barData.length > 0 ? (
                                <ResponsiveContainer width="100%" height={320}>
                                    <BarChart data={chartData.barData} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
                                        <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                                        <XAxis
                                            dataKey="label"
                                            axisLine={false}
                                            tickLine={false}
                                            tick={{ fill: '#64748b', fontSize: 12 }}
                                            dy={10}
                                        />
                                        <YAxis
                                            axisLine={false}
                                            tickLine={false}
                                            tick={{ fill: '#64748b', fontSize: 12 }}
                                            allowDecimals={false}
                                        />
                                        <Tooltip content={<CustomTooltip />} cursor={{ fill: '#f8fafc' }} />
                                        {/* barSize is responsive via ResponsiveContainer; maxBarSize prevents overflow */}
                                        <Bar dataKey="value" radius={[6, 6, 0, 0]} maxBarSize={60}>
                                            {chartData.barData.map((entry, index) => (
                                                <Cell key={`bar-${index}`} fill={entry.fill} />
                                            ))}
                                        </Bar>
                                    </BarChart>
                                </ResponsiveContainer>
                            ) : (
                                <div className="no-data-message">
                                    <TrendingUp size={40} style={{ color: '#cbd5e1', marginBottom: '0.75rem' }} />
                                    <p>No data for the selected filters</p>
                                </div>
                            )}
                        </Card>

                        <Card title="Status Distribution" className="chart-card">
                            {chartData.pieData.length > 0 ? (
                                <ResponsiveContainer width="100%" height={320}>
                                    <PieChart>
                                        <Pie
                                            data={chartData.pieData}
                                            cx="50%"
                                            cy="45%"
                                            innerRadius={65}
                                            outerRadius={105}
                                            paddingAngle={4}
                                            dataKey="value"
                                            nameKey="label"
                                        >
                                            {chartData.pieData.map((entry, index) => (
                                                <Cell
                                                    key={`cell-${index}`}
                                                    fill={CHART_COLORS[index % CHART_COLORS.length]}
                                                    stroke="none"
                                                />
                                            ))}
                                        </Pie>
                                        <Tooltip
                                            formatter={(value, name) => [`${value} requests`, name]}
                                            contentStyle={{
                                                background: '#fff',
                                                border: '1px solid #e2e8f0',
                                                borderRadius: '8px',
                                                fontSize: '13px',
                                            }}
                                        />
                                        <Legend
                                            verticalAlign="bottom"
                                            height={40}
                                            iconType="circle"
                                            iconSize={10}
                                            formatter={(value) => (
                                                <span style={{ color: '#475569', fontSize: '12px' }}>{value}</span>
                                            )}
                                        />
                                    </PieChart>
                                </ResponsiveContainer>
                            ) : (
                                <div className="no-data-message">
                                    <Clock size={40} style={{ color: '#cbd5e1', marginBottom: '0.75rem' }} />
                                    <p>No data for the selected filters</p>
                                </div>
                            )}
                        </Card>
                    </div>
                </>
            )}
        </div>
    );
};

export default Dashboard;
