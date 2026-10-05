// Date-only values sort lexicographically in ISO format, without timezone shifts.
export const overlapsDateRange = (request, fromDate, toDate) => {
    const requestStart = request.startDate?.slice(0, 10);
    const requestEnd = request.endDate?.slice(0, 10);
    if (!requestStart || !requestEnd) return false;
    return (!fromDate || requestEnd >= fromDate) &&
        (!toDate || requestStart <= toDate);
};
