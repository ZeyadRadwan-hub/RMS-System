import test from 'node:test';
import assert from 'node:assert/strict';
import { statusFilterOptions, matchesStatusFilter, statusBadgeClass } from './status.js';

test('status options use stable IDs even when raw labels change or are missing', () => {
    const options = statusFilterOptions([
        { statusID: 2, statusName: 'بانتظار الموارد البشرية' },
        { statusID: 3, statusName: null },
        { statusID: 2, statusName: 'Changed label' },
    ]);
    assert.deepEqual(options, [{ value: 2, label: 'Pending HR' }, { value: 3, label: 'Approved' }]);
});

test('history groups use IDs and distinguish pending from approved and rejected', () => {
    for (const id of [1, 2]) assert.equal(matchesStatusFilter(id, 'pending'), true);
    for (const id of [4, 5]) assert.equal(matchesStatusFilter(id, 'rejected'), true);
    assert.equal(matchesStatusFilter(3, 'approved'), true);
    assert.equal(matchesStatusFilter(6, 'cancelled'), true);
    assert.equal(matchesStatusFilter(3, 'pending'), false);
    assert.equal(matchesStatusFilter(2, '2'), true);
    assert.equal(matchesStatusFilter(1, '2'), false);
    assert.equal(matchesStatusFilter(6, ''), true);
    assert.equal(matchesStatusFilter(99, 'pending'), false);
});

test('badge classes depend on ID, including unknown IDs', () => {
    assert.equal(statusBadgeClass(3), 'badge badge-approved');
    assert.equal(statusBadgeClass(4), 'badge badge-rejected');
    assert.equal(statusBadgeClass(5), 'badge badge-rejected');
    assert.equal(statusBadgeClass(2), 'badge badge-pending-hr');
    assert.equal(statusBadgeClass(99), 'badge');
});
