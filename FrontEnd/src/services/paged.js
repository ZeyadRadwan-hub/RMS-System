const MAX_PAGE_SIZE = 200;
const MAX_PAGES = 10000;

export const fetchAllPages = async (fetchPage, pageSize = MAX_PAGE_SIZE) => {
    const boundedPageSize = Math.min(Math.max(Number(pageSize) || MAX_PAGE_SIZE, 1), MAX_PAGE_SIZE);
    const rows = [];
    for (let page = 1; page <= MAX_PAGES; page += 1) {
        const payload = await fetchPage(page, boundedPageSize);
        // Keep compatibility with older deployments that still return arrays.
        if (Array.isArray(payload)) return rows.concat(payload);
        rows.push(...(Array.isArray(payload?.items) ? payload.items : []));
        if (!payload?.hasNext) return rows;
    }
    throw new Error('The server returned too many pages');
};

export default fetchAllPages;
