import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Eye, Edit2, XCircle, AlertCircle, X, Filter, CheckCircle } from 'lucide-react';
import transactionService from '../services/transactionService';
import requestService from '../services/requestService';
import { useAuth } from '../context/useAuth';
import Card from '../components/Card';
import Loading from '../components/Loading';
import RequestForm from '../components/RequestForm';
import { useNotification } from '../context/useNotification';
import QuickInsightsPopover from '../components/QuickInsightsPopover';
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

const HRRequests = () => {
    const { isBoard, isHR } = useAuth();
    const [requests, setRequests] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [showModal, setShowModal] = useState(false);
    const [selectedRequest, setSelectedRequest] = useState(null);
    const { showAlert, showConfirm, showPrompt } = useNotification();
    const [modalMode, setModalMode] = useState('view');

    // Filters state
    const [filters, setFilters] = useState({
        status: '',
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
            // Fetch all requests then filter to HR department only
            const data = await transactionService.getAllRequests();
            // Filter to show only requests from HR department employees
            const hrRequests = (data || []).filter(
                r => r.departmentName?.toLowerCase().includes('hr') ||
                    r.departmentName?.toLowerCase().includes('human')
            );
            setRequests(hrRequests);
        } catch (error) {
            console.error('Error fetching requests:', error);
            setError(error.response?.data?.message || error.message || 'Failed to load requests');
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
            type: '',
            startDate: '',
            endDate: ''
        });
    };



    const handleEdit = (request) => {
        setSelectedRequest(request);
        setModalMode('edit');
        setShowModal(true);
    };

    const handleCancel = async (request) => {
        const confirmed = await showConfirm('Are you sure you want to cancel this request?');
        if (confirmed) {
            try {
                await transactionService.cancel(request.id);
                fetchRequests();
            } catch (error) {
                showAlert('Error cancelling request: ' + error.message, 'error');
            }
        }
    };

    const handleApprove = async (request) => {
        const message = await showPrompt('Optional: Add a response message for approval', 'Approve Request');
        if (message !== null) {
            try {
                await transactionService.approve(request.id, message || '');
                fetchRequests();
            } catch (error) {
                showAlert('Error approving request: ' + (error.response?.data?.message || error.message), 'error');
            }
        }
    };

    const handleReject = async (request) => {
        const message = await showPrompt('Please provide a reason for rejection:', 'Reject Request');
        if (message !== null && message.trim()) {
            try {
                await transactionService.reject(request.id, message);
                fetchRequests();
            } catch (error) {
                showAlert('Error rejecting request: ' + (error.response?.data?.message || error.message), 'error');
            }
        }
    };

    const handleView = (request) => {
        setSelectedRequest(request);
        setModalMode('view');
        setShowModal(true);
    };

    if (loading) {
        return <Loading message="Loading HR requests..." />;
    }

    // Get unique statuses and types for filter dropdowns
    const uniqueStatuses = statusFilterOptions(requests);
    const uniqueTypes = [...new Set(requests.map(r => r.transactionTypeName))].filter(Boolean);

    return (
        <div className="my-requests-page">
            <div className="page-header">
                <h1>HR Requests {filteredRequests.length > 0 && `(${filteredRequests.length})`}</h1>
                <div className="action-buttons">
                    {/* Only HR can create requests, Board is view-only */}
                    {isHR && (
                        <button className="btn btn-primary" onClick={() => {
                            setSelectedRequest(null);
                            setModalMode('create');
                            setShowModal(true);
                        }}>
                            <Plus size={20} style={{ marginRight: '0.5rem' }} />
                            New Request
                        </button>
                    )}
                </div>
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
                    <Filter size={20} />
                    <h3>Filters</h3>
                    <button className="btn btn-sm btn-secondary" onClick={clearFilters} style={{ marginLeft: 'auto' }}>
                        Clear All
                    </button>
                </div>
                <div className="filter-grid">
                    <div className="form-group">
                        <label className="label">Status</label>
                        <select
                            className="select"
                            value={filters.status}
                            onChange={(e) => handleFilterChange('status', e.target.value)}
                        >
                            <option value="">All Statuses</option>
                            {uniqueStatuses.map(status => (
                                <option key={status.value} value={status.value}>{status.label}</option>
                            ))}
                        </select>
                    </div>

                    <div className="form-group">
                        <label className="label">Request Type</label>
                        <select
                            className="select"
                            value={filters.type}
                            onChange={(e) => handleFilterChange('type', e.target.value)}
                        >
                            <option value="">All Types</option>
                            {uniqueTypes.map(type => (
                                <option key={type} value={type}>{type}</option>
                            ))}
                        </select>
                    </div>

                    <div className="form-group">
                        <label className="label">Start Date From</label>
                        <input
                            type="date"
                            className="input"
                            value={filters.startDate}
                            onChange={(e) => handleFilterChange('startDate', e.target.value)}
                        />
                    </div>

                    <div className="form-group">
                        <label className="label">End Date To</label>
                        <input
                            type="date"
                            className="input"
                            value={filters.endDate}
                            onChange={(e) => handleFilterChange('endDate', e.target.value)}
                        />
                    </div>
                </div>
            </Card>

            <Card className="table-card">
                <div className="card-header">
                    <h3>HR Requests list ({filteredRequests.length})</h3>
                </div>
                {filteredRequests.length === 0 ? (
                    <div style={{ padding: '2rem', textAlign: 'center', color: '#6B7280' }}>
                        <p>No requests found matching your filters.</p>
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
                                                employeeName={request.employeeName || '—'}
                                                request={request}
                                                allRequests={requests}
                                            />
                                        </td>
                                        <td>{request.transactionTypeName}</td>
                                        <td>{new Date(request.startDate).toLocaleDateString()}</td>
                                        <td>{new Date(request.endDate).toLocaleDateString()}</td>
                                        <td>{request.calculatedDays}</td>
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
                                                {/* Board can approve/reject HR requests (only if not already in final status) */}
                                                {isBoard && request.statusID === 1 && (
                                                    <>
                                                        <button
                                                            className="btn-icon btn-approve"
                                                            onClick={() => handleApprove(request)}
                                                            title="Approve"
                                                            style={{ color: '#10B981' }}
                                                        >
                                                            <CheckCircle size={16} />
                                                        </button>
                                                        <button
                                                            className="btn-icon btn-reject"
                                                            onClick={() => handleReject(request)}
                                                            title="Reject"
                                                            style={{ color: '#EF4444' }}
                                                        >
                                                            <XCircle size={16} />
                                                        </button>
                                                    </>
                                                )}
                                                {/* Only HR can edit/cancel their own requests */}
                                                {isHR && request.canEdit && (
                                                    <button
                                                        className="btn-icon btn-edit"
                                                        onClick={() => handleEdit(request)}
                                                        title="Edit Request"
                                                    >
                                                        <Edit2 size={16} />
                                                    </button>
                                                )}
                                                {request.canCancel && (
                                                    <button
                                                        className="btn-icon btn-cancel"
                                                        onClick={() => handleCancel(request)}
                                                        title="Cancel Request"
                                                    >
                                                        <XCircle size={16} />
                                                    </button>
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

            {/* Request Modal */}
            {showModal && (
                <div className="modal-overlay" onClick={() => setShowModal(false)}>
                    <div className="modal-content" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h2>
                                {modalMode === 'create' && 'New Leave Request'}
                                {modalMode === 'edit' && 'Edit Leave Request'}
                                {modalMode === 'view' && 'Request Details'}
                            </h2>
                            <button className="modal-close" onClick={() => setShowModal(false)}>
                                <X size={20} />
                            </button>
                        </div>
                        <div className="modal-body">
                            {modalMode === 'view' ? (
                                selectedRequest && (
                                    <div className="request-details">
                                        <p><strong>Type:</strong> {selectedRequest.transactionTypeName}</p>
                                        <p><strong>Start Date:</strong> {new Date(selectedRequest.startDate).toLocaleDateString()}</p>
                                        <p><strong>End Date:</strong> {new Date(selectedRequest.endDate).toLocaleDateString()}</p>
                                        <p><strong>Days:</strong> {selectedRequest.calculatedDays}</p>
                                        <p><strong>Status:</strong> {statusLabel(selectedRequest.statusID, normalizeStatus(selectedRequest.statusName))}</p>
                                        {selectedRequest.leaveRationale && (
                                            <p><strong>Rationale:</strong> {selectedRequest.leaveRationale}</p>
                                        )}
                                        {selectedRequest.responseMessage && (
                                            <p><strong>Response:</strong> {selectedRequest.responseMessage}</p>
                                        )}
                                    </div>
                                )
                            ) : (
                                <RequestForm
                                    mode={modalMode}
                                    request={selectedRequest}
                                    onSubmit={async (formData) => {
                                        try {
                                            if (modalMode === 'create') {
                                                await requestService.create(formData);
                                            } else if (modalMode === 'edit') {
                                                await requestService.update(selectedRequest.id, formData);
                                            }
                                            setShowModal(false);
                                            fetchRequests();
                                        } catch (error) {
                                            showAlert('Error: ' + (error.response?.data?.message || error.message), 'error');
                                        }
                                    }}
                                    onCancel={() => setShowModal(false)}
                                />
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default HRRequests;
