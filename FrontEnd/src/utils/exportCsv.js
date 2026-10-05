import { statusLabel } from './status.js';

const safeCell = (value) => {
    let text = String(value ?? '');
    if (/^\s*[=+\-@]/.test(text)) text = `'${text}`;
    return `"${text.replaceAll('"', '""')}"`;
};

export const buildRequestHistoryCsv = (rows) => {
    const headers = ['Employee Name', 'Department', 'Leave Type', 'Start Date',
        'End Date', 'Days', 'Status'];
    const lines = rows.map((row) => [
        row.employeeName, row.departmentName, row.transactionTypeName,
        row.startDate?.slice(0, 10), row.endDate?.slice(0, 10),
        row.calculatedDays, statusLabel(row.statusID, row.statusName)
    ].map(safeCell).join(','));
    return '\uFEFF' + [headers.map(safeCell).join(','), ...lines].join('\r\n');
};
