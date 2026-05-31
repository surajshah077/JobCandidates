const API_BASE = window.location.origin + "/api";
let currentUser = null;
let currentRankingJobId = null;
let currentRankingVersion = 'v1';

function showMessage(type, text, timeout = 5000) {
    const container = document.getElementById('messages');
    if (!container) return;
    const alert = document.createElement('div');
    alert.className = `alert alert-${type} alert-dismissible fade show shadow`;
    alert.role = 'alert';
    alert.innerHTML = `<div>${text}</div><button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>`;
    container.appendChild(alert);
    if (timeout > 0) setTimeout(() => { if (alert.parentNode) bootstrap.Alert.getOrCreateInstance(alert).close(); }, timeout);
}

function escapeHtml(value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');
}

function updateAuthStatus() {
    const status = document.getElementById('authStatus');
    if (!status) return;
    const details = document.getElementById('authDetails');
    const logoutBtn = document.getElementById('btnLogout');
    const title = status.querySelector('strong');
    const emailEl = document.getElementById('currentUserEmail');
    const roleEl = document.getElementById('currentUserRole');
    if (currentUser) {
        status.classList.remove('alert-info');
        status.classList.add('alert-success');
        if (title) title.innerText = 'Signed in';
        if (details) details.innerText = `${currentUser.name || 'User'} (${currentUser.role || 'User'})`;
        if (emailEl) emailEl.innerText = currentUser.email || '-';
        if (roleEl) roleEl.innerText = currentUser.role || '-';
        if (logoutBtn) logoutBtn.disabled = false;
    } else {
        status.classList.remove('alert-success');
        status.classList.add('alert-info');
        if (title) title.innerText = 'Not authenticated.';
        if (details) details.innerText = 'Please login first.';
        if (emailEl) emailEl.innerText = '-';
        if (roleEl) roleEl.innerText = '-';
        if (logoutBtn) logoutBtn.disabled = true;
    }
}

async function apiRequest(method, path, body = null) {
    const response = await fetch(API_BASE + path, {
        method,
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: body ? JSON.stringify(body) : null
    });
    let data = null;
    let rawText = '';
    try { rawText = await response.text(); data = rawText ? JSON.parse(rawText) : null; } catch { data = rawText || null; }
    if (response.status === 401) { currentUser = null; updateAuthStatus(); throw new Error(data?.message || 'You must login first.'); }
    if (response.status === 403) throw new Error(data?.message || 'You are not allowed to do this action.');
    if (!response.ok) {
        if (data?.message) throw new Error(data.message);
        if (data?.title) throw new Error(data.title);
        if (data?.errors) {
            const details = Object.entries(data.errors).map(([key, val]) => `${key}: ${Array.isArray(val) ? val.join(', ') : val}`).join(' | ');
            throw new Error(details);
        }
        throw new Error(typeof data === 'string' && data ? data : `Request failed with status ${response.status}`);
    }
    return data;
}

async function loadCurrentUser() {
    try { currentUser = await apiRequest('GET', '/auth/me'); } catch { currentUser = null; }
    updateAuthStatus();
}

function disableButton(button, disabled, loadingText = 'Please wait...') {
    if (!button) return;
    if (disabled) { button.dataset.originalText = button.innerHTML; button.disabled = true; button.innerHTML = `<span class="spinner-border spinner-border-sm me-1" role="status"></span>${loadingText}`; }
    else { button.disabled = false; button.innerHTML = button.dataset.originalText || button.innerHTML; }
}

function safeFormHandler(formId, buttonId, handler, loadingText = 'Please wait...') {
    const form = document.getElementById(formId);
    if (!form) return;
    const button = document.getElementById(buttonId);
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        e.stopPropagation();
        try { disableButton(button, true, loadingText); await handler(e); }
        catch (err) { showMessage('danger', err.message || 'Something went wrong.', 7000); }
        finally { disableButton(button, false); }
    });
    form.addEventListener('keydown', (e) => { if (e.key === 'Enter' && e.target.tagName.toLowerCase() !== 'textarea') e.preventDefault(); });
}

function initAuthPage() {
    const logoutBtn = document.getElementById('btnLogout');
    if (logoutBtn) logoutBtn.addEventListener('click', async (e) => { e.preventDefault(); try { await apiRequest('POST', '/auth/logout'); } catch {} currentUser = null; updateAuthStatus(); showMessage('info', 'Logged out successfully.', 3000); });
}

function initJobsPage() {}
function initCandidatesPage() {}
function initApplicationsPage() {}
function initInterviewsPage() {}
function initAnalyticsPage() {}

function getRankingMode() {
    return document.getElementById('rankingV2')?.checked ? 'v2' : 'v1';
}

function updateApiVersionBadge(mode) {
    const badge = document.getElementById('rankingVersionBadge');
    if (badge) {
        badge.textContent = mode.toUpperCase();
        badge.className = `badge ${mode === 'v2' ? 'bg-success' : 'bg-secondary'}`;
    }
}

function setRankingHeader(version) {
    const head = document.getElementById('rankingTableHead');
    if (!head) return;
    head.innerHTML = version === 'v2'
        ? `<tr><th>Rank</th><th>ID</th><th>Candidate Name</th><th>Experience (yrs)</th><th>Score</th><th>Matched Skills</th><th>Semantic %</th></tr>`
        : `<tr><th>Rank</th><th>ID</th><th>Candidate Name</th><th>Experience (yrs)</th><th>Skill Match</th><th>Experience Score</th><th>Total Score</th></tr>`;
}

function renderRankingV1(rows) {
    const tableBody = document.querySelector('#rankingTable tbody');
    if (!tableBody) return;
    tableBody.innerHTML = '';
    if (!Array.isArray(rows) || rows.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="7" class="text-center text-muted">No ranked candidates found.</td></tr>`;
        return;
    }
    rows.forEach((c, index) => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${index + 1}</td>
            <td>${escapeHtml(c.candidateId ?? c.id ?? '')}</td>
            <td>${escapeHtml(c.candidateName ?? c.name ?? '')}</td>
            <td>${escapeHtml(c.experienceYears ?? 0)}</td>
            <td><span class="badge bg-info text-dark">${escapeHtml(c.skillMatchScore ?? 0)}</span></td>
            <td><span class="badge bg-secondary">${escapeHtml(c.experienceScore ?? ((c.experienceYears ?? 0) * 5))}</span></td>
            <td><strong class="text-primary">${escapeHtml(c.totalScore ?? ((c.skillMatchScore ?? 0) + ((c.experienceYears ?? 0) * 5)))}</strong></td>
        `;
        tableBody.appendChild(tr);
    });
}

function renderRankingV2(result) {
    const tableBody = document.querySelector('#rankingTable tbody');
    if (!tableBody) return;
    tableBody.innerHTML = '';

    const rows = Array.isArray(result?.rankedCandidates) ? result.rankedCandidates : [];
    if (!rows.length) {
        tableBody.innerHTML = `<tr><td colspan="7" class="text-center text-muted">No ranked candidates found.</td></tr>`;
        return;
    }

    rows.forEach(c => {
        const b = c.breakdown || {};

        const semanticPercent = typeof b.semanticScore === 'number'
            ? (b.semanticScore * 100).toFixed(1)
            : '0.0';

        const ruleScore = b.ruleBasedScore ?? 0;
        const skillScore = b.skillScore ?? null;
        const experienceScore = b.experienceScore ?? null;
        const locationScore = b.locationScore ?? null;
        const locationMatch = b.isLocationMatch === true;

        const ruleParts = [];
        if (skillScore !== null) ruleParts.push(`Skills: ${skillScore}`);
        if (experienceScore !== null) ruleParts.push(`Exp: ${experienceScore}`);
        if (locationScore !== null) ruleParts.push(`Loc: ${locationScore}`);
        const ruleBreakdown = ruleParts.join(', ');

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${escapeHtml(c.rank ?? '')}</td>
            <td>${escapeHtml(c.candidateId ?? '')}</td>
            <td>
                <strong>${escapeHtml(c.candidateName ?? '')}</strong>
                ${c.email ? `<br><small class="text-muted">${escapeHtml(c.email)}</small>` : ''}
                ${c.location ? `<br><small class="text-muted">Location: ${escapeHtml(c.location)}</small>` : ''}
            </td>
            <td>${escapeHtml(c.experienceYears ?? 0)}</td>
            <td>
                <span class="badge bg-success">${escapeHtml(b.combinedScore ?? 0)}</span>
                <br>
                <small class="text-muted">
                    Rule: ${escapeHtml(ruleScore)}${ruleBreakdown ? ` (${escapeHtml(ruleBreakdown)})` : ''}<br>
                    AI: ${escapeHtml(semanticPercent)}%
                </small>
            </td>
            <td>${escapeHtml(b.matchedSkillCount ?? 0)}/${escapeHtml(b.totalRequiredSkills ?? 0)}</td>
            <td>
                ${escapeHtml(semanticPercent)}%
                ${locationMatch ? `<br><small class="text-success">Location match</small>` : ''}
            </td>
        `;
        tableBody.appendChild(tr);

        if (c.explanation) {
            const expl = document.createElement('tr');
            expl.innerHTML = `
                <td colspan="7" class="bg-light">
                    <small><strong>Explanation:</strong> ${escapeHtml(c.explanation)}</small>
                </td>
            `;
            tableBody.appendChild(expl);
        }
    });
}

function initRankingPage() {

    const questionBtn = document.getElementById('btnGenerateQuestions');
    const explainBtn = document.getElementById('btnExplainRanking');
    const emailBtn = document.getElementById('btnGenerateEmail');

    if (questionBtn) questionBtn.addEventListener('click', generateQuestions);
    if (explainBtn) explainBtn.addEventListener('click', explainRanking);
    if (emailBtn) emailBtn.addEventListener('click', generateEmailTemplate);


    const form = document.getElementById('rankingForm');
    const refreshBtn = document.getElementById('btnRefreshRanking');
    const input = document.getElementById('rankingJobId');
    const btnLoad = document.getElementById('btnLoadRanking');
    const v1 = document.getElementById('rankingV1');
    const v2 = document.getElementById('rankingV2');

    if (v1 && v2) {
        const sync = () => {
            currentRankingVersion = getRankingMode();
            updateApiVersionBadge(currentRankingVersion);
            setRankingHeader(currentRankingVersion);
        };
        v1.addEventListener('change', sync);
        v2.addEventListener('change', sync);
        sync();
    }

    if (form) {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const jobId = parseInt(input.value, 10);
            if (!jobId || jobId <= 0) {
                showMessage('warning', 'Please enter a valid Job ID (must be > 0).', 4000);
                return;
            }
            const version = getRankingMode();
            currentRankingJobId = jobId;
            currentRankingVersion = version;
            disableButton(btnLoad, true, 'Loading...');
            try { await loadRanking(jobId, version); } finally { disableButton(btnLoad, false); }
        });
    }

    if (refreshBtn) {
        refreshBtn.addEventListener('click', async () => {
            if (!currentRankingJobId) {
                showMessage('info', 'Enter a Job ID first.', 4000);
                return;
            }
            await loadRanking(currentRankingJobId, currentRankingVersion);
        });
    }
}

async function postJson(url, data) {
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(data)
    });
    let payload = null;
    try { payload = await res.json(); } catch {}
    if (!res.ok) throw new Error(payload?.message || payload?.title || `HTTP ${res.status}`);
    return payload;
}

async function generateQuestions() {
    const payload = {
        jobTitle: document.getElementById('aqJobTitle')?.value || '',
        jobDescription: document.getElementById('aqJobDescription')?.value || '',
        requiredSkills: document.getElementById('aqRequiredSkills')?.value || '',
        candidateName: document.getElementById('aqCandidateName')?.value || '',
        candidateSkills: document.getElementById('aqCandidateSkills')?.value || '',
        experienceYears: parseInt(document.getElementById('aqExperienceYears')?.value || '0', 10)
    };
    const resultBox = document.getElementById('aqResult');
    try {
        const data = await postJson('/api/assistant/generate-questions', payload);
        if (resultBox) resultBox.textContent = data.content || data.message || JSON.stringify(data, null, 2);
    } catch (e) {
        if (resultBox) resultBox.textContent = 'Error: ' + e.message;
    }
}

async function explainRanking() {
    const payload = {
        jobTitle: document.getElementById('erJobTitle')?.value || '',
        candidateName: document.getElementById('erCandidateName')?.value || '',
        matchedSkillCount: parseInt(document.getElementById('erMatchedSkillCount')?.value || '0', 10),
        totalRequiredSkills: parseInt(document.getElementById('erTotalRequiredSkills')?.value || '0', 10),
        experienceYears: parseInt(document.getElementById('erExperienceYears')?.value || '0', 10),
        semanticScore: parseFloat(document.getElementById('erSemanticScore')?.value || '0'),
        combinedScore: parseFloat(document.getElementById('erCombinedScore')?.value || '0')
    };
    const resultBox = document.getElementById('erResult');
    try {
        const data = await postJson('/api/assistant/explain-ranking', payload);
        if (resultBox) resultBox.textContent = data.content || data.message || JSON.stringify(data, null, 2);
    } catch (e) {
        if (resultBox) resultBox.textContent = 'Error: ' + e.message;
    }
}

async function generateEmailTemplate() {
    const payload = {
        candidateName: document.getElementById('etCandidateName')?.value || '',
        jobTitle: document.getElementById('etJobTitle')?.value || '',
        emailType: document.getElementById('etEmailType')?.value || '',
        optionalNotes: document.getElementById('etOptionalNotes')?.value || ''
    };
    const resultBox = document.getElementById('etResult');
    try {
        const data = await postJson('/api/assistant/email-template', payload);
        if (resultBox) resultBox.textContent = data.content || data.message || JSON.stringify(data, null, 2);
    } catch (e) {
        if (resultBox) resultBox.textContent = 'Error: ' + e.message;
    }
}

function initMain() {
    initAuthPage();
    initJobsPage();
    initCandidatesPage();
    initApplicationsPage();
    initInterviewsPage();
    initRankingPage();
    initAnalyticsPage();
}

document.addEventListener('DOMContentLoaded', async () => {
    try {
        await loadCurrentUser();
        initMain();
    } catch (err) {
        console.error('Initialization error:', err);
        showMessage('danger', 'Frontend initialization failed: ' + err.message, 8000);
    }
});