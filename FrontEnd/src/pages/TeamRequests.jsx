import React, { useState, useEffect, useMemo } from 'react';
import { Eye, CheckCircle, XCircle, AlertCircle, X, Filter } from 'lucide-react';
import transactionService from '../services/transactionService';
import Card from '../components/Card';
import Loading from '../components/Loading';
import CustomSelect from '../components/CustomSelect';
import CustomDatePicker from '../components/CustomDatePicker';
import QuickInsightsPopover from '../components/QuickInsightsPopover';
import { useNotification } from '../context/useNotification';
import './MyRequests.css';
import { overlapsDateRange } from '../utils/dateRange';
import { statusLabel, statusFilterOptions, matchesStatusFilter, statusBadgeClass } from '../utils/status';

// Normalize raw DB status strings to clean English labels
const normalizeStatus = (raw) => {
    if (!raw) return 'Unknown';
    const map = {
        'pending': 'Pending',
        'pending hr': 'Pending HR',
        'approved by hr': 'Approved',
        'rejected by manager': 'Rejected',
        'rejected by hr': 'Rejected',
        'cancelled by employee': 'Cancelled',
    };
    return map[raw.toLowerCase()] ?? raw;
};

const TeamRequests = () => {
    const [requests, setRequests] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [showModal, setShowModal] = useState(false);
    const [selectedRequest, setSelectedRequest] = useState(null);
    const { showAlert, showConfirm, showPrompt } = useNotification();

    // Filters state
    const [filters, setFilters] = useState({
        status: '',
        employee: '',
        type: '',
        startDate: '',
        endDate: ''
    });

    useEffect(() => {
        fetchRequests();
    }, []);

    const fetchRequests = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await transactionService.getTeamRequests();
            setRequests(data || []);
        } catch (error) {
            console.error('Error fetching team requests:', error);
            setError(error.response?.data?.message || error.message || 'Failed to load team requests');
        } finally {
            setLoading(false);
        }
    };

    const filteredRequests = useMemo(() => {
        let filtered = [...requests];

        // Status filter
        if (filters.status) {
            filtered = filtered.filter(req => matchesStatusFilter(req.statusID, filters.status));
        }

        // Employee filter
        if (filters.employee) {
            filtered = filtered.filter(req =>
                req.employeeName?.toLowerCase().includes(filters.employee.toLowerCase())
            );
        }

        // Type filter
        if (filters.type) {
            filtered = filtered.filter(req => req.transactionTypeName === filters.type);
        }

        // Date range filter
        if (filters.startDate || filters.endDate) {
            filtered = filtered.filter(req => overlapsDateRange(req, filters.startDate, filters.endDate));
        }

        // Sort: Pending requests first
        filtered.sort((a, b) => {
            const isPendingA = [1, 2].includes(a.statusID) ? 0 : 1;
            const isPendingB = [1, 2].includes(b.statusID) ? 0 : 1;
            if (isPendingA !== isPendingB) return isPendingA - isPendingB;
            return b.id - a.id; // Then by ID descending
        });

        return filtered;
    }, [requests, filters]);

    const handleFilterChange = (key, value) => {
        setFilters(prev => ({ ...prev, [key]: value }));
    };

    const clearFilters = () => {
        setFilters({
            status: '',
            employee: '',
            type: '',
            startDate: '',
            endDate: ''
        });
    };



    const handleApprove = async (request) => {
        const confirmed = await showConfirm('Are you sure you want to approve this request?');
        if (confirmed) {
            try {
                await transactionService.approve(request.id, 'Approved by manager');
                fetchRequests();
            } catch (error) {
                showAlert('Error approving request: ' + (error.response?.data?.message || error.message), 'error');
            }
        }
    };

    const handleReject = async (request) => {
        const reason = await showPrompt('Please provide a reason for rejection:');
        if (reason) {
            try {
                await transactionService.reject(request.id, reason);
                fetchRequests();
            } catch (error) {
                showAlert('Error rejecting request: ' + (error.response?.data?.message || error.message), 'error');
            }
        }
    };

    const handleView = (request) => {
        setSelectedRequest(request);
        setShowModal(true);
    };

    if (loading) {
        return <Loading message="Loading team requests..." />;
    }

    // Get unique values for filter dropdowns
    const uniqueStatuses = statusFilterOptions(requests);
    const uniqueTypes = [...new Set(requests.map(r => r.transactionTypeName))].filter(Boolean);

    return (
        <div className="my-requests-page">
            <div className="page-header">
                <h1>Team Requests {filteredRequests.length > 0 && `(${filteredRequests.length})`}</h1>
            </div>

            {error && (
                <div className="alert alert-error" style={{ marginBottom: '1rem' }}>
                    <AlertCircle size={20} style={{ marginRight: '0.5rem' }} />
                    <div>
                        <strong>Error:</strong> {error}
                        <button className="btn btn-sm" onClick={fetchRequests} style={{ marginLeft: '1rem' }}>
                            Retry
                        </button>
                    </div>
                </div>
            )}

            {/* Advanced Filters */}
            <Card className="filters-card mb-3">
                <div className="filters-header">
                    <div className="filters-title">
                        <Filter size={20} />
                        <span>Filters</span>
                    </div>
                    <button className="btn btn-secondary btn-sm" onClick={clearFilters} style={{ marginLeft: 'auto' }}>
                        Clear All
                    </button>
                </div>
                <div className="filter-grid">
                    <div className="form-group">
                        <label className="label">Employee Name</label>
                        <input
                            type="text"
                            className="input"
                            placeholder="Search by name..."
                            value={filters.employee}
                            onChange={(e) => handleFilterChange('employee', e.target.value)}
                        />
                    </div>

                    <div className="form-group">
                        <CustomSelect
                            label="Status"
                            value={filters.status}
                            onChange={(val) => handleFilterChange('status', val)}
                            options={[
                                { value: '', label: 'All Statuses' },
                                ...uniqueStatuses
                            ]}
                        />
                    </div>

                    <div className="form-group">
                        <CustomSelect
                            label="Request Type"
                            value={filters.type}
                            onChange={(val) => handleFilterChange('type', val)}
                            options={[
                                { value: '', label: 'All Types' },
                                ...uniqueTypes.map(t => ({ value: t, label: t }))
                            ]}
                        />
                    </div>

                    <div className="form-group">
                        <CustomDatePicker
                            label="Start Date From"
                            value={filters.startDate}
                            onChange={(val) => handleFilterChange('startDate', val)}
                        />
                    </div>

                    <div className="form-group">
                        <CustomDatePicker
                            label="End Date To"
                            value={filters.endDate}
                            onChange={(val) => handleFilterChange('endDate', val)}
                        />
                    </div>
                </div>
            </Card>

            <Card className="table-card">
                <div className="card-header">
                    <h3>All Requests ({filteredRequests.length})</h3>
                </div>
                {filteredRequests.length === 0 ? (
                    <div style={{ padding: '2rem', textAlign: 'center', color: '#6B7280' }}>
                        <p>No team requests found matching your filters.</p>
                    </div>
                ) : (
                    <div className="table-container">
                        <table className="table">
                            <thead>
                                <tr>
                                    <th>ID</th>
                                    <th>Employee</th>
                                    <th>Type</th>
                                    <th>Start Date</th>
                                    <th>End Date</th>
                                    <th>Days</th>
                                    <th>Status</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredRequests.map((request) => (
                                    <tr key={request.id}>
                                        <td>{request.id}</td>
                                        <td>
                                            <QuickInsightsPopover
                                                employeeId={request.employeeId}
                                                employeeName={request.employeeName}
                                                request={request}
                                                allRequests={requests}
                                            />
                                        </td>
                                        <td>{request.transactionTypeName}</td>
                                        <td>
                                            <span className="date-cell">
                                                {new Date(request.startDate).toLocaleDateString()}
                                            </span>
                                        </td>
                                        <td>
                                            <span className="date-cell">
                                                {new Date(request.endDate).toLocaleDateString()}
                                            </span>
                                        </td>
                                        <td>
                                            <span className="days-count">
                                                {request.calculatedDays}
                                            </span>
                                        </td>
                                        <td>
                                            <span className={statusBadgeClass(request.statusID)}>
                                                {statusLabel(request.statusID, normalizeStatus(request.statusName))}
                                            </span>
                                        </td>
                                        <td>
                                            <div className="action-buttons">
                                                <button
                                                    className="btn-icon btn-view"
                                                    onClick={() => handleView(request)}
                                                    title="View Details"
                                                >
                                                    <Eye size={16} />
                                                </button>
                                                {request.statusID === 1 && (
                                                    <>
                                                        <button
                                                            className="btn-icon"
                                                            onClick={() => handleApprove(request)}
                                                            title="Approve Request"
                                                        >
                                                            <CheckCircle size={16} color="#66BB6A" />
                                                        </button>
                                                        <button
                                                            className="btn-icon btn-cancel"
                                                            onClick={() => handleReject(request)}
                                                            title="Reject Request"
                                                        >
                                                            <XCircle size={16} />
                                                        </button>
                                                    </>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </Card>

            {/* Request Details Modal */}
            {showModal && selectedRequest && (
                <div className="modal-overlay" onClick={() => setShowModal(false)}>
                    <div className="modal-content" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h2>Request Details</h2>
                            <button className="modal-close" onClick={() => setShowModal(false)}>
                                <X size={20} />
                            </button>
                        </div>
                        <div className="modal-body">
                            <div className="request-details">
                                <p><strong>Employee:</strong> {selectedRequest.employeeName}</p>
                                <p><strong>Type:</strong> {selectedRequest.transactionTypeName}</p>
                                <p><strong>Start Date:</strong> {new Date(selectedRequest.startDate).toLocaleDateString()}</p>
                                <p><strong>End Date:</strong> {new Date(selectedRequest.endDate).toLocaleDateString()}</p>
                                <p><strong>Days:</strong> {selectedRequest.calculatedDays}</p>
                                <p><strong>Status:</strong> {statusLabel(selectedRequest.statusID, normalizeStatus(selectedRequest.statusName))}</p>
                                {selectedRequest.leaveRationale && (
                                    <p><strong>Rationale:</strong> {selectedRequest.leaveRationale}</p>
                                )}
                                {selectedRequest.substituteEmployeeName && (
                                    <p><strong>Substitute:</strong> {selectedRequest.substituteEmployeeName}</p>
                                )}
                                {selectedRequest.responseMessage && (
                                    <p><strong>Response:</strong> {selectedRequest.responseMessage}</p>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default TeamRequests;
