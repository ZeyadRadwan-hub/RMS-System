import React from 'react';
import { X, Calendar, Briefcase, User, Award } from 'lucide-react';
import './EmployeeDetailModal.css';

const EmployeeDetailModal = ({ employee, isOpen, onClose }) => {
    if (!isOpen || !employee) return null;

    const getInitials = (name) => {
        return name
            ?.split(' ')
            .map(n => n[0])
            .join('')
            .toUpperCase()
            .slice(0, 2) || '??';
    };

    const formatDate = (dateString) => {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    };

    return (
        <div className="employee-modal-overlay" onClick={onClose}>
            <div className="employee-modal" onClick={(e) => e.stopPropagation()}>
                {/* Header with gradient */}
                <div className="employee-modal-header">
                    <button className="employee-modal-close" onClick={onClose}>
                        <X size={24} />
                    </button>

                    <div className="employee-header-content">
                        <div className="employee-avatar-large">
                            {getInitials(employee.name || employee.employeeName)}
                        </div>
                        <div className="employee-header-info">
                            <h2>{employee.name || employee.employeeName}</h2>
                            <p className="employee-position">{employee.position || employee.jobTitle || 'Employee'}</p>
                            <p className="employee-email">
                                {employee.email || 'N/A'}
                            </p>
                        </div>
                    </div>
                </div>

                {/* Body */}
                <div className="employee-modal-body">
                    {/* Personal Information */}
                    <div className="info-section">
                        <h3 className="section-title">
                            <User size={18} />
                            Personal Information
                        </h3>
                        <div className="info-grid">
                            <div className="info-item">
                                <span className="info-label">Employee Code</span>
                                <span className="info-value">{employee.employeeCode || employee.code || 'N/A'}</span>
                            </div>
                            <div className="info-item">
                                <span className="info-label">Department</span>
                                <span className="info-value">{employee.departmentName || employee.department || 'N/A'}</span>
                            </div>
                            <div className="info-item">
                                <span className="info-label">Position</span>
                                <span className="info-value">{employee.position || employee.jobTitle || 'N/A'}</span>
                            </div>
                            <div className="info-item">
                                <span className="info-label">Manager</span>
                                <span className="info-value">{employee.managerName || 'N/A'}</span>
                            </div>
                            <div className="info-item">
                                <span className="info-label">Date of Employment</span>
                                <span className="info-value">
                                    <Calendar size={14} />
                                    {formatDate(employee.dateOfEmployment || employee.hireDate)}
                                </span>
                            </div>
                            <div className="info-item">
                                <span className="info-label">Employee Level</span>
                                <span className="info-value">
                                    <Award size={14} />
                                    {employee.employeeLevel || employee.level || 'N/A'}
                                </span>
                            </div>
                        </div>
                    </div>

                    {/* Leave Information */}
                    <div className="info-section">
                        <h3 className="section-title">
                            <Briefcase size={18} />
                            Leave Information
                        </h3>
                        <div className="leave-stats-grid">
                            <div className="leave-stat-card">
                                <div className="leave-stat-value">{employee.annualLeaveEntitlement || 0}</div>
                                <div className="leave-stat-label">Annual Entitlement</div>
                            </div>
                            <div className="leave-stat-card">
                                <div className="leave-stat-value">{employee.leaveBalance?.toFixed(1) || '0.0'}</div>
                                <div className="leave-stat-label">Leave Balance</div>
                            </div>
                            <div className="leave-stat-card">
                                <div className="leave-stat-value">{employee.leaveUsed?.toFixed(1) || '0.0'}</div>
                                <div className="leave-stat-label">Leave Used</div>
                            </div>
                            <div className="leave-stat-card">
                                <div className="leave-stat-value">{employee.totalAccruedLeave?.toFixed(1) || '0.0'}</div>
                                <div className="leave-stat-label">Total Accrued</div>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Footer */}
                <div className="employee-modal-footer">
                    <button className="btn btn-secondary" onClick={onClose}>
                        Close
                    </button>
                </div>
            </div>
        </div>
    );
};

export default EmployeeDetailModal;
