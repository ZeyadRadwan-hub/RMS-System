import test from 'node:test';
import assert from 'node:assert/strict';
import { fetchAllPages } from './paged.js';

test('fetchAllPages follows bounded API pages until hasNext is false', async () => {
    const calls = [];
    const rows = await fetchAllPages(async (page, pageSize) => {
        calls.push([page, pageSize]);
        return page === 1
            ? { items: [{ id: 1 }, { id: 2 }], page: 1, pageSize, totalCount: 3, hasNext: true }
            : { items: [{ id: 3 }], page: 2, pageSize, totalCount: 3, hasNext: false };
    }, 2);

    assert.deepEqual(rows.map(row => row.id), [1, 2, 3]);
    assert.deepEqual(calls, [[1, 2], [2, 2]]);
});
