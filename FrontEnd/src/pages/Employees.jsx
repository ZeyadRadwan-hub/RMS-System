import React, { useState, useEffect, useMemo } from 'react';
import { Users, Plus, Edit2, Trash2, Search, Filter } from 'lucide-react';
import { useAuth } from '../context/useAuth';
import { useNotification } from '../context/useNotification';
import employeeService from '../services/employeeService';
import leaveBalanceService from '../services/leaveBalanceService';
import Card from '../components/Card';
import EmployeeDetailModal from '../components/EmployeeDetailModal';
import EmployeeFormModal from '../components/EmployeeFormModal';
import Loading from '../components/Loading';
import CustomSelect from '../components/CustomSelect';
import CustomDatePicker from '../components/CustomDatePicker';
import './Employees.css';

const Employees = () => {
    const { isHR, isBoard } = useAuth();
    const { showAlert, showConfirm } = useNotification();
    const [employees, setEmployees] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [showModal, setShowModal] = useState(false);
    const [selectedEmployee, setSelectedEmployee] = useState(null);
    const [modalMode, setModalMode] = useState('view'); // 'view', 'create', 'edit'

    // Employee Detail Modal (for HR/Board)
    const [showDetailModal, setShowDetailModal] = useState(false);
    const [detailEmployee, setDetailEmployee] = useState(null);

    const [filters, setFilters] = useState({
        name: '',
        departmentID: '',
        levelID: '',
        status: 'active' // 'active', 'inactive', 'all'
    });

    useEffect(() => {
        fetchEmployees();
    }, []);

    const fetchEmployees = async () => {
        try {
            setLoading(true);
            setError(null);
            const data = await employeeService.getAll();
            const nonBoardEmployees = (data || []).filter(emp => emp.departmentID !== 11);
            setEmployees(nonBoardEmployees);
        } catch (error) {
            console.error('Error fetching employees:', error);
            setError(error.response?.data?.message || error.message || 'Failed to load employees');
        } finally {
            setLoading(false);
        }
    };

    const filteredEmployees = useMemo(() => {
        let filtered = [...employees];

        // Name filter
        if (filters.name) {
            filtered = filtered.filter(emp =>
                emp.name?.toLowerCase().includes(filters.name.toLowerCase()) ||
                emp.code?.toLowerCase().includes(filters.name.toLowerCase())
            );
        }

        // Department filter
        if (filters.departmentID) {
            filtered = filtered.filter(emp => emp.departmentID === parseInt(filters.departmentID));
        }

        // Level filter
        if (filters.levelID) {
            filtered = filtered.filter(emp => emp.employeeLevelId === parseInt(filters.levelID));
        }

        // Status filter
        if (filters.status !== 'all') {
            const isActive = filters.status === 'active';
            filtered = filtered.filter(emp => emp.isActive === isActive);
        }

        return filtered;
    }, [employees, filters]);

    const handleFilterChange = (key, value) => {
        setFilters(prev => ({ ...prev, [key]: value }));
    };



    const handleEmployeeClick = async (employee) => {
        if (isHR || isBoard) {
            try {
                // Fetch leave balance for this employee
                const balanceData = await leaveBalanceService.getBalance(employee.id);

                // Merge employee data with balance data
                const employeeWithBalance = {
                    ...employee,
                    leaveBalance: balanceData.leaveBalance,
                    annualLeaveEntitlement: balanceData.annualLeaveEntitlement,
                    leaveUsed: balanceData.leaveUsed,
                    totalAccruedLeave: balanceData.totalAccruedLeave
                };

                setDetailEmployee(employeeWithBalance);
                setShowDetailModal(true);
            } catch (error) {
                console.error('Error fetching employee balance:', error);
                showAlert(error.response?.data?.message || 'Unable to load employee balance', 'error');
            }
        }
    };

    const handleFormSubmit = async (formData) => {
        try {
            if (modalMode === 'create') {
                await employeeService.create(formData);
            } else if (modalMode === 'edit') {
                await employeeService.update(selectedEmployee.id, formData);
            }

            setShowModal(false);
            setSelectedEmployee(null);
            fetchEmployees();
        } catch (error) {
            showAlert(`Error ${modalMode === 'create' ? 'creating' : 'updating'} employee: ` +
                (error.response?.data?.message || error.message), 'error');
        }
    };

    const handleDelete = async (employee) => {
        const confirmed = await showConfirm(`Are you sure you want to deactivate ${employee.name}?`);
        if (confirmed) {
            try {
                await employeeService.delete(employee.id);
                fetchEmployees();
            } catch (error) {
                showAlert('Error deactivating employee: ' + (error.response?.data?.message || error.message), 'error');
            }
        }
    };

    if (loading) {
        return <Loading message="Loading employees..." />;
    }

    return (
        <div className="employees-page">
            <div className="page-header">
                <div>
                    <h1><Users size={32} style={{ display: 'inline', marginRight: '0.5rem' }} />Employees Management</h1>
                    <p className="page-subtitle">Manage all employees (excluding Board members)</p>
                </div>
                <div className="action-buttons">
                    <button
                        className="btn btn-primary"
                        onClick={() => {
                            setSelectedEmployee(null);
                            setModalMode('create');
                            setShowModal(true);
                        }}
                    >
                        <Plus size={20} style={{ marginRight: '0.5rem' }} />
                        Add Employee
                    </button>
                </div>
            </div>

            {error && (
                <div className="alert alert-error" style={{ marginBottom: '1rem' }}>
                    <span>⚠️</span>
                    <div>
                        <strong>Error:</strong> {error}
                        <button className="btn btn-sm" onClick={fetchEmployees} style={{ marginLeft: '1rem' }}>
                            Retry
                        </button>
                    </div>
                </div>
            )}

            {/* Filters */}
            <Card className="filters-card mb-3">
                <div className="filters-header">
                    <Filter size={20} />
                    <h3>Filters</h3>
                </div>
                <div className="filter-grid">
                    <div className="form-group">
                        <label className="label">
                            <Search size={14} style={{ marginRight: '0.25rem', verticalAlign: 'middle' }} />
                            Search Name/Code
                        </label>
                        <input
                            type="text"
                            className="input"
                            placeholder="Enter name or code..."
                            value={filters.name}
                            onChange={(e) => handleFilterChange('name', e.target.value)}
                        />
                    </div>

                    <div className="form-group">
                        <CustomSelect
                            label="Department"
                            value={filters.departmentID}
                            onChange={(val) => handleFilterChange('departmentID', val)}
                            options={[
                                { value: '', label: 'All Departments' },
                                { value: 7, label: 'Quality' },
                                { value: 8, label: 'Production' },
                                { value: 9, label: 'IT' },
                                { value: 10, label: 'HR' }
                            ]}
                        />
                    </div>

                    <div className="form-group">
                        <CustomSelect
                            label="Employee Level"
                            value={filters.levelID}
                            onChange={(val) => handleFilterChange('levelID', val)}
                            options={[
                                { value: '', label: 'All Levels' },
                                { value: 1, label: 'A (15 days)' },
                                { value: 2, label: 'B (24 days)' }
                            ]}
                        />
                    </div>

                    <div className="form-group">
                        <CustomSelect
                            label="Status"
                            value={filters.status}
                            onChange={(val) => handleFilterChange('status', val)}
                            options={[
                                { value: 'all', label: 'All Statuses' },
                                { value: 'active', label: 'Active Only' },
                                { value: 'inactive', label: 'Inactive Only' }
                            ]}
                        />
                    </div>
                </div>
            </Card>

            {/* Employees Table */}
            <Card className="table-card">
                <div className="card-header">
                    <h3>Employees List ({filteredEmployees.length})</h3>
                </div>
                {filteredEmployees.length === 0 ? (
                    <div style={{ padding: '2rem', textAlign: 'center', color: '#6B7280' }}>
                        <Users size={48} style={{ opacity: 0.3, marginBottom: '1rem' }} />
                        <p>No employees found matching your filters.</p>
                    </div>
                ) : (
                    <div className="table-container">
                        <table className="table">
                            <thead>
                                <tr>
                                    <th>Code</th>
                                    <th>Name</th>
                                    <th>Department</th>
                                    <th>Level</th>
                                    <th>Manager</th>
                                    <th>Status</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredEmployees.map((employee) => (
                                    <tr
                                        key={employee.id}
                                        className={(isHR || isBoard) ? 'clickable-row' : ''}
                                        onClick={() => handleEmployeeClick(employee)}
                                    >
                                        <td>{employee.code}</td>
                                        <td>{employee.name}</td>
                                        <td>{employee.departmentName || 'N/A'}</td>
                                        <td>{employee.employeeLevel || 'N/A'}</td>
                                        <td>{employee.isManager ? 'Yes' : 'No'}</td>
                                        <td>
                                            <span className={`badge badge-${employee.isActive ? 'approved' : 'cancelled'}`}>
                                                {employee.isActive ? 'Active' : 'Inactive'}
                                            </span>
                                        </td>
                                        <td onClick={(e) => e.stopPropagation()}>
                                            <div className="action-buttons">
                                                <button
                                                    className="btn-icon btn-edit"
                                                    onClick={() => {
                                                        setSelectedEmployee(employee);
                                                        setModalMode('edit');
                                                        setShowModal(true);
                                                    }}
                                                    title="Edit Employee"
                                                >
                                                    <Edit2 size={16} />
                                                </button>
                                                <button
                                                    className="btn-icon btn-cancel"
                                                    onClick={() => handleDelete(employee)}
                                                    title="Deactivate Employee"
                                                >
                                                    <Trash2 size={16} />
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </Card>

            {/* Employee Form Modal (Add/Edit) */}
            <EmployeeFormModal
                isOpen={showModal}
                onClose={() => {
                    setShowModal(false);
                    setSelectedEmployee(null);
                }}
                onSubmit={handleFormSubmit}
                employee={selectedEmployee}
                mode={modalMode}
            />

            {/* Employee Detail Modal (HR/Board only) */}
            <EmployeeDetailModal
                employee={detailEmployee}
                isOpen={showDetailModal}
                onClose={() => {
                    setShowDetailModal(false);
                    setDetailEmployee(null);
                }}
            />
        </div>
    );
};

export default Employees;
