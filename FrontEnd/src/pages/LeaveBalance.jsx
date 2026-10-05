import React, { useState, useEffect, useCallback } from 'react';
import { CalendarCheck, Calendar, Clock, ChevronDown, ChevronUp, X, Plus, Search, Users, User } from 'lucide-react';
import { useAuth } from '../context/useAuth';
import { useNotification } from '../context/useNotification';
import leaveBalanceService from '../services/leaveBalanceService';
import requestService from '../services/requestService';
import Card from '../components/Card';
import Loading from '../components/Loading';
import CustomSelect from '../components/CustomSelect';
import RequestForm from '../components/RequestForm';
import './LeaveBalance.css';

const LeaveBalance = () => {
    const { isHR, isBoard, isManager } = useAuth();
    const { showAlert } = useNotification();
    const [balance, setBalance] = useState(null);
    const [teamBalances, setTeamBalances] = useState([]);
    const [loading, setLoading] = useState(true);
    const [loadError, setLoadError] = useState(null);

    // UI state
    const [showTechnicalDetails, setShowTechnicalDetails] = useState(false);
    const [showRequestModal, setShowRequestModal] = useState(false);

    // Table filters
    const [searchTerm, setSearchTerm] = useState('');
    const [selectedDept, setSelectedDept] = useState('');
    const [selectedLevel, setSelectedLevel] = useState('');

    // Pagination
    const [currentPage, setCurrentPage] = useState(1);
    const [pageSize, setPageSize] = useState(10);

    useEffect(() => {
        setCurrentPage(1);
    }, [searchTerm, selectedDept, selectedLevel, pageSize]);

    const fetchBalanceData = useCallback(async () => {
        try {
            setLoading(true);
            setLoadError(null);
            const myBalance = await leaveBalanceService.getMyBalance(null);
            setBalance(myBalance);

            if (isManager || isHR || isBoard) {
                let teamData = [];
                if (isHR || isBoard) {
                    teamData = await leaveBalanceService.getAllBalances(null);
                } else if (isManager) {
                    teamData = await leaveBalanceService.getTeamBalances(null);
                }
                setTeamBalances(teamData || []);
            }
        } catch (error) {
            console.error('Error fetching balance data:', error);
            setLoadError(error.response?.data?.message || error.message || 'Failed to load leave balances');
            showAlert('Error loading leave balance data', 'error');
        } finally {
            setLoading(false);
        }
    }, [isManager, isHR, isBoard, showAlert]);

    useEffect(() => {
        fetchBalanceData();
    }, [fetchBalanceData]);

    if (loading) {
        return <Loading message="Loading leave balance..." />;
    }

    // ── Derived filter & pagination data ──────────────────────────────────────
    const uniqueDepartments = [...new Set(teamBalances.map(e => e.departmentName))].filter(Boolean);
    const uniqueLevels      = [...new Set(teamBalances.map(e => e.employeeLevel))].filter(Boolean);

    const filteredTeamBalances = teamBalances.filter(emp => {
        const name = emp.employeeName?.toLowerCase() || '';
        const code = emp.employeeCode?.toLowerCase() || '';
        const term = searchTerm.toLowerCase();
        return (
            (name.includes(term) || code.includes(term)) &&
            (selectedDept  === '' || emp.departmentName === selectedDept) &&
            (selectedLevel === '' || emp.employeeLevel  === selectedLevel)
        );
    });

    const totalItems  = filteredTeamBalances.length;
    const totalPages  = Math.ceil(totalItems / pageSize) || 1;
    const startIndex  = (currentPage - 1) * pageSize;
    const endIndex    = Math.min(startIndex + pageSize, totalItems);
    const paginatedRows = filteredTeamBalances.slice(startIndex, endIndex);

    // Available balance usage percentage (capped 0-100) for progress bar
    const usagePct = balance
        ? Math.min(100, Math.max(0,
            (balance.leaveUsed / (balance.totalAccruedLeave || 1)) * 100
          ))
        : 0;

    return (
        <div className="leave-balance-page">

            {loadError && (
                <div className="alert alert-error" role="alert">
                    <span>{loadError}</span>
                    <button className="btn btn-sm" onClick={fetchBalanceData}>Retry</button>
                </div>
            )}

            {/* ── Hero Banner (matches Dashboard) ─────────────────────────── */}
            <div className="lb-banner">
                <div className="lb-banner-content">
                    <div>
                        <h1>Leave Balance</h1>
                        <p>View your accrued leave, entitlement, and usage summary</p>
                    </div>
                    <div className="lb-banner-actions">
                        <button className="btn btn-primary" onClick={() => setShowRequestModal(true)}>
                            <Plus size={16} />
                            Request Leave
                        </button>
                    </div>
                </div>
            </div>

            {/* ── My Balance Section ───────────────────────────────────────── */}
            {balance && (
                <section className="lb-section">
                    <div className="lb-section-header">
                        <h2>My Leave Balance</h2>
                        <span className="lb-calc-date">
                            <Clock size={14} />
                            As of {new Date(balance.calculationDate || Date.now()).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}
                        </span>
                    </div>

                    {/* Stat Cards — exact Dashboard pattern */}
                    <div className="lb-stats-grid">

                        {/* Available Balance */}
                        <div className="stat-card lb-stat-available">
                            <div className="stat-content">
                                <div className="stat-label">Available Balance</div>
                                <div className="stat-value">
                                    {balance.leaveBalance?.toFixed(1) ?? 0}
                                    <span className="stat-unit">days</span>
                                </div>
                                {/* Usage progress bar */}
                                <div className="lb-progress-track">
                                    <div
                                        className="lb-progress-fill"
                                        style={{ width: `${usagePct}%` }}
                                    />
                                </div>
                                <div className="lb-progress-label">
                                    {balance.leaveUsed?.toFixed(1) ?? 0} used of {balance.totalAccruedLeave?.toFixed(1) ?? 0} accrued
                                </div>
                            </div>
                            <div className="stat-icon-container lb-icon-available">
                                <CalendarCheck size={22} />
                            </div>
                        </div>

                        {/* Annual Entitlement */}
                        <div className="stat-card lb-stat-entitlement">
                            <div className="stat-content">
                                <div className="stat-label">Annual Entitlement</div>
                                <div className="stat-value">
                                    {balance.annualLeaveEntitlement ?? 0}
                                    <span className="stat-unit">days / yr</span>
                                </div>
                            </div>
                            <div className="stat-icon-container lb-icon-entitlement">
                                <Calendar size={22} />
                            </div>
                        </div>

                    </div>

                    {/* Details Card */}
                    <Card hoverable={false} className="lb-details-card">
                        <div className="lb-details-header">
                            <h3>Employee Details</h3>
                            <button
                                className="btn btn-secondary btn-sm lb-toggle-btn"
                                onClick={() => setShowTechnicalDetails(v => !v)}
                            >
                                {showTechnicalDetails ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                                {showTechnicalDetails ? 'Hide Calculation Details' : 'Show Calculation Details'}
                            </button>
                        </div>

                        <div className="lb-details-grid">
                            <div className="lb-detail-item">
                                <span className="data-label">Employee Code</span>
                                <span className="data-value">{balance.employeeCode}</span>
                            </div>
                            <div className="lb-detail-item">
                                <span className="data-label">Full Name</span>
                                <span className="data-value">{balance.employeeName}</span>
                            </div>
                            <div className="lb-detail-item">
                                <span className="data-label">Level</span>
                                <span className="data-value">
                                    <span className="badge lb-badge-level">{balance.employeeLevel}</span>
                                </span>
                            </div>
                            <div className="lb-detail-item">
                                <span className="data-label">Date of Employment</span>
                                <span className="data-value text-date">
                                    {new Date(balance.dateOfEmployment).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}
                                </span>
                            </div>
                            <div className="lb-detail-item">
                                <span className="data-label">Total Accrued</span>
                                <span className="data-value text-numeric">{balance.totalAccruedLeave?.toFixed(1) ?? 0} days</span>
                            </div>
                            <div className="lb-detail-item">
                                <span className="data-label">Leave Used</span>
                                <span className="data-value text-numeric">{balance.leaveUsed?.toFixed(1) ?? 0} days</span>
                            </div>
                            <div className="lb-detail-item">
                                <span className="data-label">Carryover</span>
                                <span className="data-value text-numeric">{balance.carryoverFromPreviousYear?.toFixed(1) ?? 0} days</span>
                            </div>

                            {/* Technical / Calculation Details (collapsible) */}
                            {showTechnicalDetails && (
                                <>
                                    <div className="lb-detail-divider" />
                                    <div className="lb-detail-item lb-detail-highlight">
                                        <span className="data-label">Months of Service</span>
                                        <span className="data-value text-numeric">{balance.monthsOfService}</span>
                                    </div>
                                    <div className="lb-detail-item lb-detail-highlight">
                                        <span className="data-label">Monthly Accrual Rate</span>
                                        <span className="data-value text-numeric">{balance.monthlyAccrual?.toFixed(2)} days / mo</span>
                                    </div>
                                    <div className="lb-detail-item lb-detail-highlight">
                                        <span className="data-label">In Probation</span>
                                        <span className="data-value">
                                            <span className={`badge ${balance.isInProbation ? 'badge-pending' : 'badge-approved'}`}>
                                                {balance.isInProbation ? 'Yes' : 'No'}
                                            </span>
                                        </span>
                                    </div>
                                </>
                            )}
                        </div>
                    </Card>
                </section>
            )}

            {/* ── Team / All Employees Table ───────────────────────────────── */}
            {(isManager || isHR || isBoard) && teamBalances.length > 0 && (
                <section className="lb-section">
                    <div className="lb-section-header">
                        <h2>
                            {isHR || isBoard ? (
                                <><Users size={20} style={{ marginRight: '0.5rem', verticalAlign: 'middle' }} />All Employees Leave Balances</>
                            ) : (
                                <><User size={20} style={{ marginRight: '0.5rem', verticalAlign: 'middle' }} />Team Members Leave Balances</>
                            )}
                        </h2>
                        <span className="lb-record-count">{totalItems} record{totalItems !== 1 ? 's' : ''}</span>
                    </div>

                    <Card hoverable={false}>
                        {/* ── Filter Toolbar ──────────────────────────── */}
                        <div className="lb-filter-row">
                            <div className="form-group lb-filter-search">
                                <label className="label">Search</label>
                                <div style={{ position: 'relative' }}>
                                    <input
                                        type="text"
                                        className="input"
                                        placeholder="Name or code..."
                                        value={searchTerm}
                                        onChange={e => setSearchTerm(e.target.value)}
                                        style={{ paddingLeft: '2.4rem' }}
                                    />
                                    <Search size={15} className="lb-search-icon" />
                                </div>
                            </div>
                            <div className="form-group lb-filter-select">
                                <CustomSelect
                                    label="Department"
                                    value={selectedDept}
                                    onChange={val => setSelectedDept(val)}
                                    options={[
                                        { value: '', label: 'All Departments' },
                                        ...uniqueDepartments.map(d => ({ value: d, label: d }))
                                    ]}
                                />
                            </div>
                            <div className="form-group lb-filter-select">
                                <CustomSelect
                                    label="Level"
                                    value={selectedLevel}
                                    onChange={val => setSelectedLevel(val)}
                                    options={[
                                        { value: '', label: 'All Levels' },
                                        ...uniqueLevels.map(l => ({ value: l, label: l }))
                                    ]}
                                />
                            </div>
                        </div>

                        {/* ── Data Table (global .table class) ───────── */}
                        <div className="table-container" style={{ borderRadius: 0, border: 'none', boxShadow: 'none' }}>
                            <table className="table">
                                <thead>
                                    <tr>
                                        <th>Code</th>
                                        <th>Name</th>
                                        <th>Department</th>
                                        <th>Level</th>
                                        <th>Accrued</th>
                                        <th>Used</th>
                                        <th>Available</th>
                                        <th>Entitlement</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {paginatedRows.length === 0 ? (
                                        <tr>
                                            <td colSpan="8" style={{ textAlign: 'center', padding: '2.5rem', color: 'var(--color-text-secondary)' }}>
                                                No matching records found.
                                            </td>
                                        </tr>
                                    ) : (
                                        paginatedRows.map(emp => (
                                            <tr key={emp.employeeId}>
                                                <td className="text-date" style={{ fontWeight: 600 }}>{emp.employeeCode}</td>
                                                <td style={{ fontWeight: 500 }}>{emp.employeeName}</td>
                                                <td style={{ color: 'var(--color-text-secondary)' }}>{emp.departmentName || '—'}</td>
                                                <td>
                                                    <span className="badge lb-badge-level">{emp.employeeLevel}</span>
                                                </td>
                                                <td className="text-numeric">{emp.totalAccruedLeave?.toFixed(1)}</td>
                                                <td className="text-numeric">{emp.leaveUsed?.toFixed(1)}</td>
                                                <td>
                                                    <span className={`text-numeric lb-balance-value ${emp.leaveBalance < 0 ? 'lb-balance-negative' : emp.leaveBalance <= 5 ? 'lb-balance-low' : 'lb-balance-ok'}`}>
                                                        {emp.leaveBalance?.toFixed(1)}
                                                    </span>
                                                </td>
                                                <td className="text-numeric">{emp.annualLeaveEntitlement}</td>
                                            </tr>
                                        ))
                                    )}
                                </tbody>
                            </table>
                        </div>

                        {/* ── Pagination ──────────────────────────────── */}
                        {totalItems > 0 && (
                            <div className="lb-pagination">
                                <span className="lb-pagination-info">
                                    Showing {startIndex + 1}–{endIndex} of {totalItems} employees
                                </span>
                                <div className="lb-pagination-controls">
                                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                        <span style={{ fontSize: '0.8rem', color: 'var(--color-text-secondary)' }}>Per page:</span>
                                        <select
                                            className="select"
                                            value={pageSize}
                                            onChange={e => setPageSize(parseInt(e.target.value))}
                                            style={{ width: '70px', height: '34px', padding: '0 6px' }}
                                        >
                                            <option value={5}>5</option>
                                            <option value={10}>10</option>
                                            <option value={20}>20</option>
                                            <option value={50}>50</option>
                                        </select>
                                    </div>
                                    <div className="lb-page-nav">
                                        <button
                                            className="btn btn-secondary btn-sm"
                                            onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                                            disabled={currentPage === 1}
                                        >
                                            ← Previous
                                        </button>
                                        <span className="lb-page-badge">
                                            {currentPage} / {totalPages}
                                        </span>
                                        <button
                                            className="btn btn-secondary btn-sm"
                                            onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                                            disabled={currentPage === totalPages}
                                        >
                                            Next →
                                        </button>
                                    </div>
                                </div>
                            </div>
                        )}
                    </Card>
                </section>
            )}

            {/* ── Request Leave Modal ──────────────────────────────────────── */}
            {showRequestModal && (
                <div className="lb-modal-overlay" onClick={() => setShowRequestModal(false)}>
                    <div className="lb-modal" onClick={e => e.stopPropagation()}>
                        <div className="lb-modal-header">
                            <h2>Request New Leave</h2>
                            <button className="btn-icon" onClick={() => setShowRequestModal(false)} aria-label="Close">
                                <X size={20} />
                            </button>
                        </div>
                        <div className="lb-modal-body">
                            <RequestForm
                                mode="create"
                                onSubmit={async (formData) => {
                                    try {
                                        await requestService.create(formData);
                                        setShowRequestModal(false);
                                        showAlert('Leave request submitted successfully', 'success');
                                        fetchBalanceData();
                                    } catch (error) {
                                        showAlert('Error: ' + (error.response?.data?.message || error.message), 'error');
                                    }
                                }}
                                onCancel={() => setShowRequestModal(false)}
                            />
                        </div>
                    </div>
                </div>
            )}

        </div>
    );
};

export default LeaveBalance;
