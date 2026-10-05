const labels = {
    1: 'Pending',
    2: 'Pending HR',
    3: 'Approved',
    4: 'Rejected (Manager)',
    5: 'Rejected (HR)',
    6: 'Cancelled',
};

export const statusLabel = (statusID, fallback = 'Unknown') => labels[statusID] || fallback;
