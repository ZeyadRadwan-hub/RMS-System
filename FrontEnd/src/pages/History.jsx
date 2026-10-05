import React, { useState, useEffect, useMemo } from 'react';
import { Clock, Filter, CheckCircle, XCircle, Search, Download } from 'lucide-react';
import { buildRequestHistoryCsv } from '../utils/exportCsv';
import { statusLabel, matchesStatusFilter, statusBadgeClass } from '../utils/status';
import { useAuth } from '../context/useAuth';
import { useNotification } from '../context/useNotification';
import requestService from '../services/requestService';
import { overlapsDateRange } from '../utils/dateRange';
import Card from '../components/Card';
import Loading from '../components/Loading';
import CustomSelect from '../components/CustomSelect';
import CustomDatePicker from '../components/CustomDatePicker';
import './History.css';

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

const History = () => {
    const { isHR } = useAuth();
    const [requests, setRequests] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const { showAlert, showConfirm, showPrompt } = useNotification();

    // Filter states
    const [filters, setFilters] = useState({
        status: '',
        employeeName: '',
        departmentName: '',
        startDate: '',
        endDate: '',
    });

    useEffect(() => {
        fetchAllRequests();
    }, []);

    const fetchAllRequests = async () => {
        try {
            setLoading(true);
            setError(null);

            // Fetch all requests (HR/Board can see all)
            const data = await requestService.getAllRequests();
            setRequests(data);
        } catch (error) {
            console.error('Error fetching requests:', error);
            setError(error.response?.data?.message || error.message || 'Failed to load requests');
        } finally {
            setLoading(false);
        }
    };

    const filteredRequests = useMemo(() => {
        let filtered = [...requests];

        // Filter by status
        if (filters.status) {
            filtered = filtered.filter(req => matchesStatusFilter(req.statusID, filters.status));
        }

        // Filter by employee name
        if (filters.employeeName) {
            filtered = filtered.filter(req =>
                (req.employeeName?.toLowerCase().includes(filters.employeeName.toLowerCase()) ||
                 req.employeeCode?.toLowerCase().includes(filters.employeeName.toLowerCase()))
            );
        }

        // Filter by department
        if (filters.departmentName) {
            filtered = filtered.filter(req =>
                req.departmentName?.toLowerCase().includes(filters.departmentName.toLowerCase())
            );
        }

        // Filter by date range
        if (filters.startDate || filters.endDate) {
            filtered = filtered.filter(req => overlapsDateRange(req, filters.startDate, filters.endDate));
        }

        return filtered;
    }, [filters, requests]);

    const handleFilterChange = (key, value) => {
        setFilters(prev => ({ ...prev, [key]: value }));
    };

    const handleClearFilters = () => {
        setFilters({
            status: '',
            employeeName: '',
            departmentName: '',
            startDate: '',
            endDate: '',
        });
    };

    const handleExport = () => {
        if (filteredRequests.length === 0) {
            showAlert('No data to export', 'warning');
            return;
        }

        const csv = buildRequestHistoryCsv(filteredRequests);
        const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
        const link = document.createElement('a');
        link.href = url;
        link.download = `Leave_Requests_History_${new Date().toISOString().split('T')[0]}.csv`;
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
        showAlert('Report exported successfully', 'success');
    };

    const handleApprove = async (requestId) => {
        if (!isHR) return;

        const confirmed = await showConfirm('Are you sure you want to approve this request?');
        if (confirmed) {
            try {
                await requestService.approve(requestId);
                fetchAllRequests();
            } catch (error) {
                showAlert('Error approving request: ' + (error.response?.data?.message || error.message), 'error');
            }
        }
    };

    const handleReject = async (requestId) => {
        if (!isHR) return;

        const reason = await showPrompt('Please enter rejection reason:', 'Reject Request');
        if (reason) {
            try {
                await requestService.reject(requestId, reason);
                fetchAllRequests();
            } catch (error) {
                showAlert('Error rejecting request: ' + (error.response?.data?.message || error.message), 'error');
            }
        }
    };

    if (loading) {
        return <Loading message="Loading history..." />;
    }

    if (error) {
        return (
            <div className="history-page">
                <div className="alert alert-error">
                    <strong>Error:</strong> {error}
                    <button className="btn btn-primary" onClick={fetchAllRequests}>
                        Retry
                    </button>
                </div>
            </div>
        );
    }

    return (
        <div className="history-page">
            <div className="page-header">
                <div className="page-title-section">
                    <Clock size={32} className="page-icon" />
                    <div>
                        <h1 className="page-title">Request History</h1>
                        <p className="page-subtitle">View all leave requests in the system</p>
                    </div>
                </div>
            </div>

            {/* Filters */}
            <Card className="mb-3 history-filters-card">
                <div className="filters-header">
                    <div className="filters-title">
                        <Filter size={20} />
                        <span>Filters</span>
                    </div>
                    <div className="action-buttons">
                        <button className="btn btn-success btn-sm" onClick={handleExport}>
                            <Download size={16} />
                            Export CSV
                        </button>
                        <button className="btn btn-secondary btn-sm clear-filters-btn" onClick={handleClearFilters}>
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
                                { value: 'Pending', label: 'Pending' },
                                { value: 'Approved', label: 'Approved' },
                                { value: 'Rejected', label: 'Rejected' },
                                { value: 'Cancelled', label: 'Cancelled' }
                            ]}
                        />
                    </div>

                    <div className="form-group">
                        <label className="label">Employee Name</label>
                        <input
                            type="text"
                            className="input"
                            placeholder="Search by name or code..."
                            value={filters.employeeName}
                            onChange={(e) => handleFilterChange('employeeName', e.target.value)}
                        />
                    </div>

                    <div className="form-group">
                        <label className="label">Department</label>
                        <input
                            type="text"
                            className="input"
                            placeholder="Search by department..."
                            value={filters.departmentName}
                            onChange={(e) => handleFilterChange('departmentName', e.target.value)}
                        />
                    </div>

                    <div className="form-group">
                        <CustomDatePicker
                            label="Start Date (From)"
                            value={filters.startDate}
                            onChange={(val) => handleFilterChange('startDate', val)}
                        />
                    </div>

                    <div className="form-group">
                        <CustomDatePicker
                            label="End Date (To)"
                            value={filters.endDate}
                            onChange={(val) => handleFilterChange('endDate', val)}
                        />
                    </div>
                </div>
            </Card>

            {/* Requests List */}
            <Card>
                <div className="card-header">
                    <h3>All Requests ({filteredRequests.length})</h3>
                </div>

                <div className="table-container">
                    <table className="table">
                        <thead>
                            <tr>
                                <th>Employee</th>
                                <th>Department</th>
                                <th>Leave Type</th>
                                <th>Start Date</th>
                                <th>End Date</th>
                                <th>Days</th>
                                <th>Status</th>
                                {isHR && <th>Actions</th>}
                            </tr>
                        </thead>
                        <tbody>
                            {filteredRequests.length === 0 ? (
                                <tr>
                                    <td colSpan={isHR ? 8 : 7} className="text-center">
                                        No requests found
                                    </td>
                                </tr>
                            ) : (
                                filteredRequests.map((request) => (
                                    <tr key={request.id}>
                                        <td>{request.employeeName}</td>
                                        <td>{request.departmentName}</td>
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
                                        {isHR && (
                                            <td>
                                                {[1, 2].includes(request.statusID) ? (
                                                    <div className="action-buttons">
                                                        <button
                                                            className="btn btn-success btn-sm"
                                                            onClick={() => handleApprove(request.id)}
                                                            title="Approve"
                                                        >
                                                            <CheckCircle size={16} />
                                                        </button>
                                                        <button
                                                            className="btn btn-danger btn-sm"
                                                            onClick={() => handleReject(request.id)}
                                                            title="Reject"
                                                        >
                                                            <XCircle size={16} />
                                                        </button>
                                                    </div>
                                                ) : (
                                                    <span className="text-muted">-</span>
                                                )}
                                            </td>
                                        )}
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>
            </Card>
        </div>
    );
};

export default History;
