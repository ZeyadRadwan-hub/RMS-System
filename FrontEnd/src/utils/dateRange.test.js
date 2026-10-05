import test from 'node:test';
import assert from 'node:assert/strict';
import { overlapsDateRange } from './dateRange.js';

test('includes requests intersecting the filter window, including boundaries', () => {
    const request = { startDate: '2026-09-28T00:00:00', endDate: '2026-10-03T00:00:00' };
    assert.equal(overlapsDateRange(request, '2026-10-01', '2026-10-31'), true);
    assert.equal(overlapsDateRange(request, '2026-10-03', '2026-10-03'), true);
    assert.equal(overlapsDateRange(request, '2026-10-04', '2026-10-31'), false);
    assert.equal(overlapsDateRange(request, '', '2026-09-27'), false);
});

test('does not include records with missing dates', () => {
    assert.equal(overlapsDateRange({}, '', ''), false);
});
