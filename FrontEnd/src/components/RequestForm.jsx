import React, { useState, useEffect, useRef } from 'react';
import { Upload, X, FileText, AlertCircle } from 'lucide-react';
import { useNotification } from '../context/useNotification';
import employeeService from '../services/employeeService';
import requestService from '../services/requestService';
import CustomSelect from './CustomSelect';
import CustomDatePicker from './CustomDatePicker';

const SICK_LEAVE_ID = 1;

// Format file size to human-readable
const formatFileSize = (bytes) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
};

const RequestForm = ({ mode, request, onSubmit, onCancel }) => {
    const { showAlert } = useNotification();
    const [formData, setFormData] = useState({
        transactionTypesID: request?.transactionTypesID || 2, // Default to Annual Leave
        startDate: request?.startDate?.split('T')[0] || '',
        endDate: request?.endDate?.split('T')[0] || '',
        substituteEmployeeId: request?.substituteEmployeeId || '',
        leaveRationale: request?.leaveRationale || '',
    });

    const [employees, setEmployees] = useState([]);
    const [loadingEmployees, setLoadingEmployees] = useState(false);
    const [submitting, setSubmitting] = useState(false);

    // Medical documents state
    const [medicalFiles, setMedicalFiles] = useState([]);
    const [existingMedicalDocuments, setExistingMedicalDocuments] = useState([]);
    const [loadingMedicalDocuments, setLoadingMedicalDocuments] = useState(mode === 'edit' && request?.transactionTypesID === SICK_LEAVE_ID);
    const [isDragging, setIsDragging] = useState(false);
    const fileInputRef = useRef(null);

    const isSickLeave = parseInt(formData.transactionTypesID) === SICK_LEAVE_ID;

    useEffect(() => {
        fetchEmployees();
    }, []);

    useEffect(() => {
        if (mode !== 'edit' || !request?.id || request.transactionTypesID !== SICK_LEAVE_ID) return;
        let active = true;
        requestService.getMedicalDocuments(request.id)
            .then((items) => { if (active) setExistingMedicalDocuments(items); })
            .catch(() => { if (active) showAlert('Could not load existing medical documents', 'error'); })
            .finally(() => { if (active) setLoadingMedicalDocuments(false); });
        return () => { active = false; };
    }, [mode, request?.id, request?.transactionTypesID, showAlert]);

    // Clear files when leave type changes away from Sick Leave
    useEffect(() => {
        if (!isSickLeave && medicalFiles.length > 0) {
            setMedicalFiles([]);
            if (fileInputRef.current) {
                fileInputRef.current.value = '';
            }
        }
    }, [isSickLeave, medicalFiles.length]);

    const fetchEmployees = async () => {
        try {
            setLoadingEmployees(true);
            const data = await employeeService.getSubstitutes();
            setEmployees(data || []);
        } catch (error) {
            console.error('Error fetching employees:', error);
        } finally {
            setLoadingEmployees(false);
        }
    };

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData(prev => ({
            ...prev,
            [name]: name === 'transactionTypesID' || name === 'substituteEmployeeId'
                ? (value ? parseInt(value) : '')
                : value
        }));
    };

    const handleCustomChange = (name, value) => {
        setFormData(prev => ({
            ...prev,
            [name]: name === 'transactionTypesID' || name === 'substituteEmployeeId'
                ? (value ? parseInt(value) : '')
                : value
        }));
    };

    // ── Medical File Handlers ──────────────────────────────────────
    const addFiles = (newFiles) => {
        const fileArray = Array.from(newFiles);
        if (medicalFiles.length + fileArray.length > 5 || fileArray.some(f => f.size > 10 * 1024 * 1024 || f.size === 0)) {
            showAlert('Upload up to 5 medical documents, each between 1 byte and 10 MB', 'warning');
            return;
        }
        // Prevent duplicates by name+size
        const existing = new Set(medicalFiles.map(f => `${f.name}-${f.size}`));
        const unique = fileArray.filter(f => !existing.has(`${f.name}-${f.size}`));
        setMedicalFiles(prev => [...prev, ...unique]);
    };

    const handleFileSelect = (e) => {
        if (e.target.files?.length) {
            addFiles(e.target.files);
        }
        // Reset input so the same file can be re-selected if removed
        e.target.value = '';
    };

    const handleRemoveFile = (index) => {
        setMedicalFiles(prev => prev.filter((_, i) => i !== index));
    };

    const handleDragEnter = (e) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragging(true);
    };

    const handleDragLeave = (e) => {
        e.preventDefault();
        e.stopPropagation();
        // Only set false if we actually left the drop zone
        if (!e.currentTarget.contains(e.relatedTarget)) {
            setIsDragging(false);
        }
    };

    const handleDragOver = (e) => {
        e.preventDefault();
        e.stopPropagation();
    };

    const handleDrop = (e) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragging(false);
        if (e.dataTransfer.files?.length) {
            addFiles(e.dataTransfer.files);
        }
    };

    // ── Form Submission ────────────────────────────────────────────
    const handleSubmit = async (e) => {
        e.preventDefault();

        // Validation
        if (!formData.startDate || !formData.endDate) {
            showAlert('Please select both start and end dates', 'warning');
            return;
        }

        if (new Date(formData.endDate) < new Date(formData.startDate)) {
            showAlert('End date must be after start date', 'warning');
            return;
        }

        // Sick leave: require medical documents
        if (isSickLeave && medicalFiles.length === 0 && existingMedicalDocuments.length === 0) {
            showAlert('Medical documents are required for Sick Leave requests. Please upload at least one file.', 'warning');
            return;
        }

        setSubmitting(true);
        try {
            // Ensure substituteEmployeeId is a number (0 if not selected)
            const submitData = {
                ...formData,
                substituteEmployeeId: formData.substituteEmployeeId || null,
                ...(isSickLeave && { medicalDocuments: medicalFiles }),
            };
            await onSubmit(submitData);
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} className="request-form">
            <div className="form-group">
                <label className="label">Leave Type *</label>
                <CustomSelect
                    value={formData.transactionTypesID}
                    onChange={(val) => handleCustomChange('transactionTypesID', val)}
                    options={[
                        { value: 2, label: 'Annual Leave' },
                        { value: 1, label: 'Sick Leave' },
                        { value: 3, label: 'Half Day' },
                        { value: 5, label: 'Unpaid Leave' }
                    ]}
                />
            </div>

            {/* ── Medical Documents Upload (Sick Leave only) ── */}
            {isSickLeave && (
                <div className="form-group medical-upload-section">
                    <label className="label">
                        Medical Documents *
                        <span className="label-hint">Upload certificates, prescriptions, or medical reports</span>
                    </label>

                    {/* Drag & Drop Zone */}
                    <div
                        className={`upload-dropzone ${isDragging ? 'upload-dropzone-active' : ''}`}
                        onDragEnter={handleDragEnter}
                        onDragLeave={handleDragLeave}
                        onDragOver={handleDragOver}
                        onDrop={handleDrop}
                        onClick={() => fileInputRef.current?.click()}
                    >
                        <input
                            ref={fileInputRef}
                            type="file"
                            multiple
                            accept=".pdf,.jpg,.jpeg,.png,.doc,.docx"
                            onChange={handleFileSelect}
                            className="upload-input-hidden"
                        />
                        <div className="upload-icon-wrapper">
                            <Upload size={22} />
                        </div>
                        <p className="upload-text">
                            <span className="upload-text-bold">Click to upload</span> or drag and drop
                        </p>
                        <p className="upload-hint">PDF, JPG, PNG, DOC up to 10MB each</p>
                    </div>

                    {/* File List */}
                    {existingMedicalDocuments.length > 0 && (
                        <div className="upload-file-list">
                            {existingMedicalDocuments.map((item) => (
                                <button key={item.id} type="button" className="upload-file-item"
                                    onClick={() => requestService.downloadMedicalDocument(request.id, item)}>
                                    <FileText size={16} /> {item.originalName} ({formatFileSize(item.sizeBytes)})
                                </button>
                            ))}
                        </div>
                    )}
                    {medicalFiles.length > 0 && (
                        <div className="upload-file-list">
                            {medicalFiles.map((file, index) => (
                                <div key={`${file.name}-${index}`} className="upload-file-item">
                                    <div className="upload-file-icon">
                                        <FileText size={16} />
                                    </div>
                                    <div className="upload-file-info">
                                        <span className="upload-file-name">{file.name}</span>
                                        <span className="upload-file-size">{formatFileSize(file.size)}</span>
                                    </div>
                                    <button
                                        type="button"
                                        className="upload-file-remove"
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            handleRemoveFile(index);
                                        }}
                                        title="Remove file"
                                    >
                                        <X size={14} />
                                    </button>
                                </div>
                            ))}
                        </div>
                    )}

                    {/* Validation hint if no files */}
                    {medicalFiles.length === 0 && existingMedicalDocuments.length === 0 && (
                        <div className="upload-required-hint">
                            <AlertCircle size={13} />
                            <span>At least one medical document is required for Sick Leave</span>
                        </div>
                    )}
                </div>
            )}

            <div className="form-row">
                <div className="form-group">
                    <CustomDatePicker
                        label="Start Date *"
                        value={formData.startDate}
                        onChange={(val) => handleCustomChange('startDate', val)}
                    />
                </div>

                <div className="form-group">
                    <CustomDatePicker
                        label="End Date *"
                        value={formData.endDate}
                        onChange={(val) => handleCustomChange('endDate', val)}
                    />
                </div>
            </div>

            <div className="form-group">
                <label className="label">Substitute Employee (Optional)</label>
                <CustomSelect
                    value={formData.substituteEmployeeId}
                    onChange={(val) => handleCustomChange('substituteEmployeeId', val)}
                    placeholder={loadingEmployees ? "Loading..." : "-- Select Substitute --"}
                    options={[
                        { value: '', label: '-- None --' },
                        ...employees.map(emp => ({
                            value: emp.id,
                            label: emp.name
                        }))
                    ]}
                />
                <small style={{ color: '#6B7280', fontSize: '0.75rem', marginTop: '4px', display: 'block' }}>
                    {loadingEmployees ? 'Loading employees...' : 'Select a colleague (non-manager) to cover your duties'}
                </small>
            </div>

            <div className="form-group">
                <label htmlFor="leaveRationale" className="label">Reason for Leave *</label>
                <textarea
                    id="leaveRationale"
                    name="leaveRationale"
                    value={formData.leaveRationale}
                    onChange={handleChange}
                    className="input"
                    rows={4}
                    placeholder="Please provide a reason for your leave request..."
                    required
                />
            </div>

            <div className="form-actions">
                <button
                    type="button"
                    onClick={onCancel}
                    className="btn btn-secondary"
                    disabled={submitting}
                >
                    Cancel
                </button>
                <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={submitting || loadingMedicalDocuments || (isSickLeave && medicalFiles.length === 0 && existingMedicalDocuments.length === 0)}
                >
                    {submitting ? 'Submitting...' : (mode === 'create' ? 'Create Request' : 'Update Request')}
                </button>
            </div>
        </form>
    );
};

export default RequestForm;
