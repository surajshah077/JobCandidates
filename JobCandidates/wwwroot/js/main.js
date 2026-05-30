const API_BASE = window.location.origin + "/api";
let currentUser = null;
let currentRankingJobId = null;
let currentRankingVersion = 'v1';

// ─── UTILITIES ──────────────────────────────────────────────────────────────

function showMessage(type, text, timeout = 5000) {
    const container = document.getElementById('messages');
    if (!container) return;
    const alert = document.createElement('div');
    alert.className = `alert alert-${type} alert-dismissible fade show shadow`;
    alert.role = 'alert';
    alert.innerHTML = `<div>${text}</div><button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>`;
    container.appendChild(alert);
    if (timeout > 0) {
        setTimeout(() => {
            if (alert.parentNode) bootstrap.Alert.getOrCreateInstance(alert).close();
        }, timeout);
    }
}

function escapeHtml(value) {
    return String(value)
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
    try {
        rawText = await response.text();
        data = rawText ? JSON.parse(rawText) : null;
    } catch {
        data = rawText || null;
    }

    if (response.status === 401) {
        currentUser = null;
        updateAuthStatus();
        throw new Error(data?.message || 'You must login first.');
    }

    if (response.status === 403) {
        throw new Error(data?.message || 'You are not allowed to do this action.');
    }

    if (!response.ok) {
        if (data?.message) throw new Error(data.message);
        if (data?.title) throw new Error(data.title);
        if (data?.errors) {
            const details = Object.entries(data.errors)
                .map(([key, val]) => `${key}: ${Array.isArray(val) ? val.join(', ') : val}`)
                .join(' | ');
            throw new Error(details);
        }
        throw new Error(typeof data === 'string' && data ? data : `Request failed with status ${response.status}`);
    }

    return data;
}

async function loadCurrentUser() {
    try {
        const result = await apiRequest('GET', '/auth/me');
        currentUser = result;
    } catch {
        currentUser = null;
    }
    updateAuthStatus();
}

function disableButton(button, disabled, loadingText = 'Please wait...') {
    if (!button) return;
    if (disabled) {
        button.dataset.originalText = button.innerHTML;
        button.disabled = true;
        button.innerHTML = `<span class="spinner-border spinner-border-sm me-1" role="status"></span>${loadingText}`;
    } else {
        button.disabled = false;
        button.innerHTML = button.dataset.originalText || button.innerHTML;
    }
}

function safeFormHandler(formId, buttonId, handler, loadingText = 'Please wait...') {
    const form = document.getElementById(formId);
    if (!form) return;
    const button = document.getElementById(buttonId);
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        e.stopPropagation();
        try {
            disableButton(button, true, loadingText);
            await handler(e);
        } catch (err) {
            showMessage('danger', err.message || 'Something went wrong.', 7000);
        } finally {
            disableButton(button, false);
        }
    });
    form.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && e.target.tagName.toLowerCase() !== 'textarea') e.preventDefault();
    });
}

// ─── AUTH ────────────────────────────────────────────────────────────────────

function initAuthPage() {
    const logoutBtn = document.getElementById('btnLogout');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', async (e) => {
            e.preventDefault();
            try { await apiRequest('POST', '/auth/logout'); } catch { }
            currentUser = null;
            updateAuthStatus();
            showMessage('info', 'Logged out successfully.', 3000);
        });
    }

    safeFormHandler('registerForm', 'btnRegister', async (e) => {
        const body = {
            name: document.getElementById('registerName').value.trim(),
            age: parseInt(document.getElementById('registerAge').value),
            gender: document.getElementById('registerGender').value,
            email: document.getElementById('registerEmail').value.trim(),
            role: document.getElementById('registerRole').value
        };
        const result = await apiRequest('POST', '/auth/register', body);
        document.getElementById('registerVerifyEmail').value = body.email;
        showMessage('success', result.message || 'Registration OTP sent. Check your email.', 6000);
    }, 'Sending OTP...');

    safeFormHandler('registerVerifyForm', 'btnVerifyRegister', async (e) => {
        const body = {
            email: document.getElementById('registerVerifyEmail').value.trim(),
            code: document.getElementById('registerOtpCode').value.trim()
        };
        const result = await apiRequest('POST', '/auth/verify-register-otp', body);
        await loadCurrentUser();
        showMessage('success', result.message || 'Account verified and logged in.', 4000);
        e.target.reset();
    }, 'Verifying...');

    safeFormHandler('loginRequestForm', 'btnRequestLoginOtp', async () => {
        const body = { email: document.getElementById('loginEmail').value.trim() };
        const result = await apiRequest('POST', '/auth/request-login-otp', body);
        document.getElementById('loginVerifyEmail').value = body.email;
        showMessage('success', result.message || 'Login OTP sent. Check your email.', 6000);
    }, 'Sending OTP...');

    safeFormHandler('loginVerifyForm', 'btnVerifyLogin', async (e) => {
        const body = {
            email: document.getElementById('loginVerifyEmail').value.trim(),
            code: document.getElementById('loginOtpCode').value.trim()
        };
        const result = await apiRequest('POST', '/auth/verify-login-otp', body);
        await loadCurrentUser();
        showMessage('success', result.message || 'Logged in successfully.', 4000);
        e.target.reset();
        setTimeout(() => { window.location.href = 'jobs.html'; }, 900);
    }, 'Logging in...');
}

// ─── JOBS / CANDIDATES / APPLICATIONS / INTERVIEWS ─────────────────────────
// keep your existing functions here exactly as they are
// if you already have them working, do NOT replace them unless needed

// ─── RANKING ──────────────────────────────────────────────────────────────────

function getRankingMode() {
    const v2 = document.getElementById('rankingV2');
    if (v2 && v2.checked) return 'v2';
    return 'v1';
}

function updateApiVersionBadge(mode) {
    const badge = document.getElementById('apiVersionBadge');
    if (badge) badge.textContent = mode.toUpperCase();
}

function renderRankingV1(candidates) {
    const tableBody = document.querySelector('#candidatesTable tbody');
    if (!tableBody) return;
    tableBody.innerHTML = '';

    if (!Array.isArray(candidates) || candidates.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="7" class="text-muted">No candidates found.</td></tr>`;
        return;
    }

    candidates.forEach((c, index) => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td><span class="badge bg-secondary">#${index + 1}</span></td>
            <td>${escapeHtml(String(c.candidateId ?? c.id ?? ''))}</td>
            <td>${escapeHtml(c.candidateName ?? c.name ?? '')}</td>
            <td>${escapeHtml(String(c.experienceYears ?? 0))}</td>
            <td><span class="badge bg-info text-dark">${escapeHtml(String(c.skillMatchScore ?? 0))}</span></td>
            <td><span class="badge bg-secondary">${escapeHtml(String(c.experienceScore ?? ((c.experienceYears ?? 0) * 5)))}</span></td>
            <td><strong class="text-primary">${escapeHtml(String(c.totalScore ?? ((c.skillMatchScore ?? 0) + ((c.experienceYears ?? 0) * 5))))}</strong></td>
        `;
        tableBody.appendChild(row);
    });
}

function renderRankingV2(data) {
    const tableBody = document.querySelector('#candidatesTable tbody');
    if (!tableBody) return;
    tableBody.innerHTML = '';

    const candidates = Array.isArray(data?.rankedCandidates) ? data.rankedCandidates : [];
    if (candidates.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="7" class="text-muted">No candidates found.</td></tr>`;
        return;
    }

    candidates.forEach(c => {
        const b = c.breakdown || {};
        const matched = b.matchedSkillCount ?? 0;
        const total = b.totalRequiredSkills ?? 0;
        const semantic = typeof b.semanticScore === 'number' ? (b.semanticScore * 100).toFixed(1) : '0.0';
        const combined = b.combinedScore ?? 0;

        const row = document.createElement('tr');
        row.innerHTML = `
            <td><span class="badge bg-primary">#${escapeHtml(String(c.rank ?? ''))}</span></td>
            <td>${escapeHtml(String(c.candidateId ?? ''))}</td>
            <td>
                <strong>${escapeHtml(c.candidateName ?? '')}</strong><br>
                <small class="text-muted">${escapeHtml(c.email ?? '')}</small>
            </td>
            <td>${escapeHtml(String(c.experienceYears ?? 0))}</td>
            <td><span class="badge bg-success">${escapeHtml(String(combined))}/100</span></td>
            <td>${escapeHtml(`${matched}/${total}`)}</td>
            <td>${escapeHtml(`${semantic}%`)}</td>
        `;
        tableBody.appendChild(row);

        if (c.explanation) {
            const expl = document.createElement('tr');
            expl.innerHTML = `<td colspan="7" style="background:#f8f9fa;"><small><strong>AI Explanation:</strong> ${escapeHtml(c.explanation)}</small></td>`;
            tableBody.appendChild(expl);
        }
    });
}

async function loadRanking(jobId, version = 'v1') {
    const table = document.querySelector('#candidatesTable tbody');
    const summary = document.getElementById('rankingSummary');
    const v2Card = document.getElementById('v2JobInfoCard');
    const versionBadge = document.getElementById('rankingVersionBadge');
    if (!table) return;

    if (versionBadge) {
        versionBadge.textContent = version.toUpperCase();
        versionBadge.className = `badge ${version === 'v2' ? 'bg-success' : 'bg-secondary'}`;
    }

    if (v2Card) v2Card.classList.add('d-none');
    if (summary) summary.style.display = 'none';

    table.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Loading ranking...</td></tr>`;

    try {
        if (version === 'v2') {
            const result = await apiRequest('GET', `/ranking/v2/${jobId}`);
            renderRankingV2(result);

            if (v2Card) {
                const jobIdEl = document.getElementById('v2JobId');
                const titleEl = document.getElementById('v2JobTitle');
                const skillsEl = document.getElementById('v2RequiredSkills');
                if (jobIdEl) jobIdEl.textContent = result.jobId ?? jobId;
                if (titleEl) titleEl.textContent = result.jobTitle ?? '-';
                if (skillsEl) skillsEl.textContent = result.requiredSkills ?? '-';
                v2Card.classList.remove('d-none');
            }

            if (summary && result?.rankedCandidates?.length) {
                const top = result.rankedCandidates[0];
                summary.style.display = 'block';
                summary.innerHTML = `<strong>Job ID ${jobId}</strong> [V2] — ${result.rankedCandidates.length} candidates ranked. Top: <strong>${top.candidateName}</strong>.`;
            }
        } else {
            const result = await apiRequest('GET', `/ranking/job/${jobId}`);
            renderRankingV1(result);

            if (summary && result?.length) {
                const top = result[0];
                summary.style.display = 'block';
                summary.innerHTML = `<strong>Job ID ${jobId}</strong> [V1] — ${result.length} candidates ranked. Top: <strong>${top.candidateName}</strong>.`;
            }
        }
    } catch (err) {
        table.innerHTML = `<tr><td colspan="7" class="text-center text-danger py-3">Failed to load ranking: ${err.message}</td></tr>`;
        showMessage('danger', 'Failed to load ranking: ' + err.message, 7000);
    }
}

function initRankingPage() {
    const form = document.getElementById('rankingForm');
    const refreshBtn = document.getElementById('btnRefreshRanking');
    const input = document.getElementById('rankingJobId');
    const btnLoad = document.getElementById('btnLoadRanking');
    const v1 = document.getElementById('rankingV1');
    const v2 = document.getElementById('rankingV2');

    if (form) {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const jobId = parseInt(input.value);
            if (!jobId || jobId <= 0) {
                showMessage('warning', 'Please enter a valid Job ID (must be > 0).', 4000);
                return;
            }
            const version = getRankingMode();
            currentRankingJobId = jobId;
            currentRankingVersion = version;
            disableButton(btnLoad, true, 'Loading...');
            try {
                await loadRanking(jobId, version);
            } finally {
                disableButton(btnLoad, false);
            }
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

    if (v1 && v2) {
        v1.addEventListener('change', () => { currentRankingVersion = 'v1'; updateApiVersionBadge('v1'); });
        v2.addEventListener('change', () => { currentRankingVersion = 'v2'; updateApiVersionBadge('v2'); });
        updateApiVersionBadge(getRankingMode());
    }
}

function loadCurrentRanking() {
    const jobInput = document.getElementById('jobId');
    const jobId = jobInput ? parseInt(jobInput.value || '0', 10) : 0;
    if (!jobId || jobId <= 0) return;
    currentRankingJobId = jobId;
    const mode = getRankingMode();
    currentRankingVersion = mode;
    updateApiVersionBadge(mode);
    loadRanking(jobId, mode);
}

// ─── ASSISTANT ───────────────────────────────────────────────────────────────

async function postJson(url, data) {
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return await res.json();
}

async function generateQuestions() {
    const payload = {
        jobTitle: document.getElementById('aqJobTitle').value,
        jobDescription: document.getElementById('aqJobDescription').value,
        requiredSkills: document.getElementById('aqRequiredSkills').value,
        candidateName: document.getElementById('aqCandidateName').value,
        candidateSkills: document.getElementById('aqCandidateSkills').value,
        experienceYears: parseInt(document.getElementById('aqExperienceYears').value || '0', 10)
    };

    try {
        const data = await postJson('/api/assistant/generate-questions', payload);
        document.getElementById('aqResult').textContent = data.content || '';
    } catch (e) {
        document.getElementById('aqResult').textContent = 'Error: ' + e.message;
    }
}

async function explainRanking() {
    const payload = {
        jobTitle: document.getElementById('erJobTitle').value,
        candidateName: document.getElementById('erCandidateName').value,
        matchedSkillCount: parseInt(document.getElementById('erMatchedSkillCount').value || '0', 10),
        totalRequiredSkills: parseInt(document.getElementById('erTotalRequiredSkills').value || '0', 10),
        experienceYears: parseInt(document.getElementById('erExperienceYears').value || '0', 10),
        semanticScore: parseFloat(document.getElementById('erSemanticScore').value || '0'),
        combinedScore: parseFloat(document.getElementById('erCombinedScore').value || '0')
    };

    try {
        const data = await postJson('/api/assistant/explain-ranking', payload);
        document.getElementById('erResult').textContent = data.content || '';
    } catch (e) {
        document.getElementById('erResult').textContent = 'Error: ' + e.message;
    }
}

async function generateEmailTemplate() {
    const payload = {
        candidateName: document.getElementById('etCandidateName').value,
        jobTitle: document.getElementById('etJobTitle').value,
        emailType: document.getElementById('etEmailType').value,
        optionalNotes: document.getElementById('etOptionalNotes').value
    };

    try {
        const data = await postJson('/api/assistant/email-template', payload);
        document.getElementById('etResult').textContent = data.content || '';
    } catch (e) {
        document.getElementById('etResult').textContent = 'Error: ' + e.message;
    }
}

// ─── INIT ─────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', async () => {
    try {
        await loadCurrentUser();
        initAuthPage();
        initJobsPage();
        initCandidatesPage();
        initApplicationsPage();
        initInterviewsPage();
        initRankingPage();
        if (document.getElementById('metricTotalJobs')) loadAnalytics();
    } catch (err) {
        console.error('Initialization error:', err);
        showMessage('danger', 'Frontend initialization failed: ' + err.message, 8000);
    }
});