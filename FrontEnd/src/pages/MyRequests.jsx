import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Eye, Edit2, XCircle, AlertCircle, X, Filter } from 'lucide-react';
import transactionService from '../services/transactionService';
import requestService from '../services/requestService';
import Card from '../components/Card';
import Loading from '../components/Loading';
import RequestForm from '../components/RequestForm';
import CustomSelect from '../components/CustomSelect';
import CustomDatePicker from '../components/CustomDatePicker';
import { useNotification } from '../context/useNotification';
import { overlapsDateRange } from '../utils/dateRange';
import './MyRequests.css';

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

const MyRequests = () => {
    const [requests, setRequests] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [showModal, setShowModal] = useState(false);
    const [selectedRequest, setSelectedRequest] = useState(null);
    const [modalMode, setModalMode] = useState('view');
    const { showAlert, showConfirm } = useNotification();

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
            const data = await transactionService.getMyRequests();
            setRequests(data || []);
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
            filtered = filtered.filter(req => req.statusName === filters.status);
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
                showAlert('Error cancelling request: ' +
                    (error.response?.data?.message || error.message), 'error');
            }
        }
    };

    const handleView = (request) => {
        setSelectedRequest(request);
        setModalMode('view');
        setShowModal(true);
    };

    if (loading) {
        return <Loading message="Loading your requests..." />;
    }

    // Get unique statuses and types for filter dropdowns
    const uniqueStatuses = [...new Set(requests.map(r => r.statusName))].filter(Boolean);
    const uniqueTypes = [...new Set(requests.map(r => r.transactionTypeName))].filter(Boolean);

    return (
        <div className="my-requests-page">
            <div className="page-header">
                <h1>My Requests {filteredRequests.length > 0 && `(${filteredRequests.length})`}</h1>
                <button className="btn btn-primary" onClick={() => {
                    setSelectedRequest(null);
                    setModalMode('create');
                    setShowModal(true);
                }}>
                    <Plus size={20} style={{ marginRight: '0.5rem' }} />
                    New Request
                </button>
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
                    <div className="action-buttons">
                        <button className="btn btn-sm btn-secondary" onClick={clearFilters}>
                            Clear All
                        </button>
                    </div>
                </div>
                <div className="filter-grid">
                    <div className="form-group">
                        <CustomSelect
                            label="Status"
                            value={filters.status}
                            onChange={(val) => handleFilterChange('status', val)}
                            options={[
                                { value: '', label: 'All Statuses' },
                                ...uniqueStatuses.map(s => ({ value: s, label: normalizeStatus(s) }))
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
                        <p>No requests found matching your filters.</p>
                    </div>
                ) : (
                    <div className="table-container">
                        <table className="table">
                            <thead>
                                <tr>
                                    <th>ID</th>
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
                                            <span className={`badge badge-${request.statusName?.toLowerCase().replace(/\s+/g, '-')}`}>
                                                {normalizeStatus(request.statusName)}
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
                                                {request.canEdit && (
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
                                        <p><strong>Status:</strong> {normalizeStatus(selectedRequest.statusName)}</p>
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

export default MyRequests;
