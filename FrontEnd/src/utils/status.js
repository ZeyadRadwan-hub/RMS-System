const labels = {
    1: 'Pending',
    2: 'Pending HR',
    3: 'Approved',
    4: 'Rejected (Manager)',
    5: 'Rejected (HR)',
    6: 'Cancelled',
};

export const statusLabel = (statusID, fallback = 'Unknown') => labels[statusID] || fallback;

export const statusFilterOptions = (requests) => {
    const options = new Map();
    for (const request of requests) {
        const id = Number(request.statusID);
        if (Number.isInteger(id) && id > 0 && !options.has(id)) {
            options.set(id, { value: id, label: statusLabel(id, request.statusName || 'Unknown') });
        }
    }
    return [...options.values()].sort((a, b) => a.value - b.value);
};

export const matchesStatusFilter = (statusID, filter) => {
    if (filter === '' || filter == null) return true;
    const groups = { pending: [1, 2], approved: [3], rejected: [4, 5], cancelled: [6] };
    const group = groups[String(filter).toLowerCase()];
    if (group) return group.includes(Number(statusID));
    const id = Number(filter);
    return Number.isInteger(id) && id > 0 && Number(statusID) === id;
};

export const statusBadgeClass = (statusID) => {
    const classes = { 1: 'pending', 2: 'pending-hr', 3: 'approved', 4: 'rejected', 5: 'rejected', 6: 'cancelled' };
    return classes[statusID] ? `badge badge-${classes[statusID]}` : 'badge';
};
