const { test } = require('node:test');
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join } = require('node:path');
const vm = require('node:vm');

function load(fetch) {
    const context = vm.createContext({ fetch });
    vm.runInContext(readFileSync(join(__dirname, '../assets/common.js'), 'utf8'), context);
    return context;
}

test('GET errors retain business details for the user', async () => {
    const context = load(async () => ({ ok: false, status: 400,
        text: async () => JSON.stringify({ title: 'Business rule violation', detail: 'Not enough stock.' }) }));
    await assert.rejects(context.apiGetJson('/products'), error => {
        assert.equal(context.getFriendlyMessage(error), 'Not enough stock.');
        return true;
    });
});

test('field validation messages take priority over general details', () => {
    const context = load();
    const error = new Error('HTTP 400 - ' + JSON.stringify({
        title: 'Validation failed', detail: 'General detail', errors: { AmountPaid: ['Amount must be positive.'] }
    }));
    assert.equal(context.getFriendlyMessage(error), 'Amount must be positive.');
});

test('POST errors show safe server detail and keep status fallbacks', async () => {
    const context = load(async () => ({ ok: false, status: 500,
        text: async () => JSON.stringify({ title: 'Server error', detail: 'Please try again later.' }) }));
    await assert.rejects(context.apiSendJson('/invoices', 'POST', {}), error => {
        assert.equal(context.getFriendlyMessage(error), 'Please try again later.');
        return true;
    });
    assert.equal(context.getFriendlyMessage(new Error('HTTP 404')), 'Requested resource was not found.');
});
