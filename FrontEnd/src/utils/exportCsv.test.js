import test from 'node:test';
import assert from 'node:assert/strict';
import { buildRequestHistoryCsv } from './exportCsv.js';

test('history CSV uses normalized status and protects spreadsheet formula cells', () => {
    const csv = buildRequestHistoryCsv([{
        employeeName: '=HYPERLINK("bad")', departmentName: 'Quality',
        transactionTypeName: 'Annual Leave', startDate: '2026-10-04T00:00:00',
        endDate: '2026-10-05T00:00:00', calculatedDays: 2,
        statusID: 3, statusName: 'Renamed backend status'
    }]);
    assert.match(csv, /'=HYPERLINK/);
    assert.match(csv, /Approved/);
    assert.match(csv, /2026-10-04/);
});
