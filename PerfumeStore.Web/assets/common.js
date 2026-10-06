// Get element by id
function $(id) {
    return document.getElementById(id);
}

// Show a message in #pageMsg
function setMsg(elementId, text, isError = false) {
    const msgEl = $(elementId);
    if (!msgEl) return;

    msgEl.textContent = text || "";
    msgEl.style.color = isError ? "#ff8b8b" : "rgba(232,238,252,.65)";
}

// Prevent HTML injection
function escapeHtml(str) {
    return String(str ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

// Read query string: ?id=5
function getQueryParam(name) {
    const params = new URLSearchParams(window.location.search);
    return params.get(name);
}

// Small wrapper: GET JSON
async function apiGetJson(url) {
    const res = await fetch(url, {
        headers: { Accept: "application/json" }
    });
    if (!res.ok) {
        const errText = await res.text().catch(() => "");
        throw new Error(`HTTP ${res.status} - ${errText}`);
    }
    return await res.json();
}

// Small wrapper: POST/PUT JSON
async function apiSendJson(url, method, bodyObj) {
    const res = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify(bodyObj),
    });

    // If backend returns 400/500, read details to help debugging
    if (!res.ok) {
        const errText = await res.text().catch(() => "");
        throw new Error(`HTTP ${res.status} - ${errText}`);
    }

    // 204 No Content
    if (res.status === 204) return null;

    // If content-type is JSON, parse JSON safely
    const contentType = res.headers.get("content-type") || "";
    if (contentType.includes("application/json")) {
        return await res.json();
    }

    // Otherwise return text (could be empty or plain text)
    return await res.text();
}

function getValidationMessageFromHttpError(err) {
    const msg = err?.message || '';

    // Expecting: "HTTP 400 - <json>"
    const dashIndex = msg.indexOf(' - ');
    if (dashIndex === -1) return null;

    const maybeJson = msg.slice(dashIndex + 3).trim();
    if (!maybeJson.startsWith('{')) return null;

    try {
        const problem = JSON.parse(maybeJson);

        // ASP.NET Core validation: problem.errors = { field: [messages...] }
        const errors = problem?.errors;
        if (errors && typeof errors === 'object') {
            for (const key of Object.keys(errors)) {
                const arr = errors[key];
                if (Array.isArray(arr) && arr.length > 0) {
                    return arr[0]; // first message, e.g. "ID must be greater than 0."
                }
            }
        }

        // fallback
        if (problem?.detail) return problem.detail;
        if (problem?.title) return problem.title;

        return null;
    } catch {
        return null;
    }
}

function getFriendlyMessage(err) {
    const validationMsg = getValidationMessageFromHttpError(err);
    if (validationMsg) return validationMsg;

    if (err?.message?.includes('HTTP 401')) return 'You are not authorized.';
    if (err?.message?.includes('HTTP 403')) return 'You do not have permission.';
    if (err?.message?.includes('HTTP 404')) return 'Requested resource was not found.';
    if (err?.message?.includes('HTTP 500')) return 'Server error. Please try again later.';

    if (err?.message?.includes('Failed to fetch'))
        return 'Network error. Please check your connection.';

    return 'Something went wrong. Please try again.';
}

function formatDateOnly(dateStr) {
  if (!dateStr) return "";
  return dateStr.split('T')[0];
}
