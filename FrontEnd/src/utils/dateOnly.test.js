/* global process */
import test from 'node:test';
import assert from 'node:assert/strict';
import { formatDateOnly, parseDateOnly } from './dateOnly.js';

test('date-only selection stays on the same Cairo calendar day', () => {
    process.env.TZ = 'Africa/Cairo';
    const selected = new Date(2026, 9, 4);
    assert.equal(formatDateOnly(selected), '2026-10-04');
    const parsed = parseDateOnly('2026-10-04');
    assert.equal(parsed.getFullYear(), 2026);
    assert.equal(parsed.getMonth(), 9);
    assert.equal(parsed.getDate(), 4);
});
