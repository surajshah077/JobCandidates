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

// ─────────────────────────────────────────────
// AUTH PAGE
// ─────────────────────────────────────────────
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

/* JOBS */
async function loadJobs() {
    const table = document.querySelector('#jobsTable tbody');
    if (!table) return;

    try {
        const jobs = await apiRequest('GET', '/jobs');
        table.innerHTML = '';

        if (!jobs.length) {
            table.innerHTML = `<tr><td colspan="6" class="text-center text-muted">No jobs found.</td></tr>`;
            return;
        }

        jobs.forEach(j => {
            const title = (j.title ?? '').replace(/'/g, "\\'");
            const description = (j.description ?? '').replace(/'/g, "\\'");
            const location = (j.location ?? '').replace(/'/g, "\\'");
            const salaryRange = (j.salaryRange ?? '').replace(/'/g, "\\'");
            const requiredSkills = (j.requiredSkills ?? '').replace(/'/g, "\\'");

            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${j.id}</td>
                <td>${j.title ?? ''}</td>
                <td><span class="badge ${j.status === 'Closed' ? 'bg-secondary' : 'bg-success'}">${j.status ?? ''}</span></td>
                <td>${j.location ?? ''}</td>
                <td>${j.postedByName ? `${j.postedByName} (${j.postedByEmail ?? ''})` : (j.postedByEmail ?? '')}</td>
                <td>
                    <button class="btn btn-sm btn-outline-warning me-1"
                        onclick="editJob(${j.id}, '${title}', '${description}', '${location}', '${salaryRange}', '${requiredSkills}', '${j.status ?? ''}')">
                        Edit
                    </button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteJob(${j.id})">Delete</button>
                </td>`;
            table.appendChild(tr);
        });
    } catch (err) {
        showMessage('danger', 'Failed to load jobs: ' + err.message, 7000);
    }
}

window.editJob = function (id, title, description, location, salaryRange, requiredSkills, status) {
    const newTitle = prompt('Title:', title);
    if (newTitle === null) return;

    const newDescription = prompt('Description:', description);
    if (newDescription === null) return;

    const newLocation = prompt('Location:', location);
    if (newLocation === null) return;

    const newSalaryRange = prompt('Salary Range:', salaryRange);
    if (newSalaryRange === null) return;

    const newRequiredSkills = prompt('Required Skills (comma separated):', requiredSkills);
    if (newRequiredSkills === null) return;

    apiRequest('PUT', `/jobs/${id}`, {
        title: newTitle.trim(),
        description: newDescription.trim(),
        location: newLocation.trim(),
        salaryRange: newSalaryRange.trim(),
        requiredSkills: newRequiredSkills.trim(),
        status: status || 'Open'
    }, true)
        .then(() => {
            showMessage('success', 'Job updated.', 4000);
            loadJobs();
        })
        .catch(err => showMessage('danger', 'Failed to update job: ' + err.message, 7000));
};

window.deleteJob = function (id) {
    if (!confirm(`Delete job #${id}?`)) return;
    apiRequest('DELETE', `/jobs/${id}`, null, true)
        .then(() => { showMessage('success', 'Job deleted.', 4000); loadJobs(); })
        .catch(err => showMessage('danger', 'Failed to delete job: ' + err.message, 7000));
};

function initJobsPage() {
    const refreshBtn = document.getElementById('btnRefreshJobs');
    if (refreshBtn) refreshBtn.addEventListener('click', loadJobs);

    const createForm = document.getElementById('jobCreateForm');
    if (createForm) {
        createForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const body = { title: document.getElementById('jobTitle').value.trim(), description: document.getElementById('jobDescription').value.trim(), location: document.getElementById('jobLocation').value.trim(), salaryRange: document.getElementById('jobSalaryRange').value.trim(), requiredSkills: document.getElementById('jobRequiredSkills').value.trim() };
                const result = await apiRequest('POST', '/jobs', body, true);
                showMessage('success', `Job created successfully. Id: ${result.id}`, 5000);
                e.target.reset(); await loadJobs();
            } catch (err) { showMessage('danger', 'Failed to create job: ' + err.message, 7000); }
        });
    }

    // FIXED: Close Job form handler
    const closeForm = document.getElementById('jobCloseForm');
    if (closeForm) {
        closeForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const id = document.getElementById('jobCloseId').value;
                await apiRequest('PUT', `/jobs/${id}/close`, null, true);
                showMessage('success', `Job #${id} closed successfully.`, 5000);
                e.target.reset(); await loadJobs();
            } catch (err) { showMessage('danger', 'Failed to close job: ' + err.message, 7000); }
        });
    }

    if (document.getElementById('jobsTable')) loadJobs();
}

/* /* CANDIDATES */
const candidateEmailCache = {};

async function loadCandidates() {
    const table = document.querySelector('#candidatesTable tbody');
    if (!table) return;

    try {
        const candidates = await apiRequest('GET', '/candidates');
        table.innerHTML = '';

        if (!candidates.length) {
            table.innerHTML = `<tr><td colspan="5" class="text-center text-muted">No candidates found.</td></tr>`;
            return;
        }

        candidates.forEach(c => {
            if (c.email && c.email.trim() !== '') {
                candidateEmailCache[c.id] = c.email;
            }

            const resolvedEmail = (c.email && c.email.trim() !== '')
                ? c.email
                : (candidateEmailCache[c.id] || '');

            const name = (c.name || '').replace(/'/g, "\\'");
            const email = resolvedEmail.replace(/'/g, "\\'");
            const phone = (c.phone || '').replace(/'/g, "\\'");
            const education = (c.education || '').replace(/'/g, "\\'");
            const skills = (c.skills || '').replace(/'/g, "\\'");

            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${c.id}</td>
                <td>${c.name || ''}</td>
                <td>${resolvedEmail}</td>
                <td>${c.experienceYears ?? 0}</td>
                <td>
                    <button class="btn btn-sm btn-outline-warning me-1"
                        onclick="editCandidate(${c.id}, '${name}', '${email}', '${phone}', '${education}', ${c.experienceYears ?? 0}, '${skills}')">
                        Edit
                    </button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteCandidate(${c.id})">Delete</button>
                </td>`;
            table.appendChild(tr);
        });
    } catch (err) {
        showMessage('danger', 'Failed to load candidates: ' + err.message, 7000);
    }
}

window.editCandidate = function (id, name, email, phone, education, experienceYears, skills) {
    const newName = prompt('Name:', name);
    if (newName === null) return;

    const newEmail = prompt('Email:', email);
    if (newEmail === null) return;

    const newPhone = prompt('Phone:', phone);
    if (newPhone === null) return;

    const newEducation = prompt('Education:', education);
    if (newEducation === null) return;

    const newExp = prompt('Experience Years:', experienceYears);
    if (newExp === null) return;

    const newSkills = prompt('Skills:', skills);
    if (newSkills === null) return;

    apiRequest('PUT', `/candidates/${id}`, {
        name: newName.trim(),
        email: newEmail.trim(),
        phone: newPhone.trim(),
        education: newEducation.trim(),
        experienceYears: parseInt(newExp) || 0,
        skills: newSkills.trim()
    }, true)
        .then(() => {
            candidateEmailCache[id] = newEmail.trim();
            showMessage('success', 'Candidate updated.', 4000);
            loadCandidates();
        })
        .catch(err => showMessage('danger', 'Failed to update candidate: ' + err.message, 7000));
};

window.deleteCandidate = function (id) {
    if (!confirm(`Delete candidate #${id}?`)) return;

    apiRequest('DELETE', `/candidates/${id}`, null, true)
        .then(() => {
            delete candidateEmailCache[id];
            showMessage('success', 'Candidate deleted.', 4000);
            loadCandidates();
        })
        .catch(err => showMessage('danger', 'Failed to delete: ' + err.message, 7000));
};
function initCandidatesPage() {
    const refreshBtn = document.getElementById('btnRefreshCandidates');
    if (refreshBtn) refreshBtn.addEventListener('click', loadCandidates);
    const createForm = document.getElementById('candidateCreateForm');
    if (createForm) {
        createForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const body = { name: document.getElementById('candidateName').value.trim(), email: document.getElementById('candidateEmail').value.trim(), phone: document.getElementById('candidatePhone').value.trim(), education: document.getElementById('candidateEducation').value.trim(), experienceYears: parseInt(document.getElementById('candidateExperience').value || '0'), skills: document.getElementById('candidateSkills').value.trim() };
                const result = await apiRequest('POST', '/candidates', body, true);
                showMessage('success', `Candidate created. Id: ${result.id}`, 5000);
                e.target.reset(); await loadCandidates();
            } catch (err) { showMessage('danger', 'Failed to create candidate: ' + err.message, 7000); }
        });
    }
    if (document.getElementById('candidatesTable')) loadCandidates();
}

/* APPLICATIONS */
async function loadApplications() {
    const table = document.querySelector('#applicationsTable tbody');
    if (!table) return;
    try {
        const apps = await apiRequest('GET', '/applications');
        table.innerHTML = '';
        if (!apps.length) { table.innerHTML = `<tr><td colspan="5" class="text-center text-muted">No applications found.</td></tr>`; return; }
        const statusOptions = ['Applied', 'Screening', 'TechnicalInterview', 'HRInterview', 'Offer', 'Rejected'];
        apps.forEach(a => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
              <td>${a.id}</td>
    <td>${escapeHtml(a.candidate?.name ?? 'Candidate #' + a.candidateId)}</td>
    <td>${escapeHtml(a.job?.title ?? 'Job #' + a.jobId)}</td>
                <td><span class="badge bg-primary">${a.status ?? ''}</span></td>
                <td>
                    <button class="btn btn-sm btn-outline-warning me-1" onclick="updateApplicationStatus(${a.id})">Update Status</button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteApplication(${a.id})">Delete</button>
                </td>`;
            table.appendChild(tr);
        });
    } catch (err) { showMessage('danger', 'Failed to load applications: ' + err.message, 7000); }
}

window.updateApplicationStatus = function (id) {
    const statusOptions = ['Applied', 'Screening', 'TechnicalInterview', 'HRInterview', 'Offer', 'Rejected'];
    const chosen = prompt(`New status for Application #${id}?\nOptions: ${statusOptions.join(', ')}`);
    if (!chosen) return;
    if (!statusOptions.includes(chosen)) { showMessage('warning', 'Invalid status. Choose from: ' + statusOptions.join(', '), 5000); return; }
    const notes = prompt('Notes (optional):', '') || '';
    apiRequest('PUT', `/applications/${id}/status`, { status: chosen, notes }, true)
        .then(() => { showMessage('success', `Application #${id} status updated to ${chosen}.`, 4000); loadApplications(); })
        .catch(err => showMessage('danger', 'Failed to update status: ' + err.message, 7000));
};

window.deleteApplication = function (id) {
    if (!confirm(`Delete application #${id}?`)) return;
    apiRequest('DELETE', `/applications/${id}`, null, true)
        .then(() => { showMessage('success', 'Application deleted.', 4000); loadApplications(); })
        .catch(err => showMessage('danger', 'Failed to delete: ' + err.message, 7000));
};

function initApplicationsPage() {
    const refreshBtn = document.getElementById('btnRefreshApplications');
    if (refreshBtn) refreshBtn.addEventListener('click', loadApplications);
    const createForm = document.getElementById('applicationCreateForm');
    if (createForm) {
        createForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const body = { candidateId: parseInt(document.getElementById('applicationCandidateId').value), jobId: parseInt(document.getElementById('applicationJobId').value), notes: document.getElementById('applicationNotes').value.trim() };
                const result = await apiRequest('POST', '/applications', body, true);
                showMessage('success', `Application created. Id: ${result.id}`, 5000);
                e.target.reset(); await loadApplications();
            } catch (err) { showMessage('danger', 'Failed to create application: ' + err.message, 7000); }
        });
    }
    if (document.getElementById('applicationsTable')) loadApplications();
}

/* INTERVIEWS */
// Cache application -> candidate name mapping
const _appCandidateCache = {};

// Replace getCandidateNameForApplication entirely:
async function getCandidateNameForApplication(appId, candidateId) {
    const key = `app_${appId}`;
    if (_appCandidateCache[key]) return _appCandidateCache[key];
    try {
        // If candidateId already passed in (from the interview object), use it
        const id = candidateId ?? (await apiRequest('GET', `/applications/${appId}`)).candidateId;
        const candidate = await apiRequest('GET', `/candidates/${id}`);
        const label = `${candidate.name} (#${id})`;
        _appCandidateCache[key] = label;
        return label;
    } catch {
        _appCandidateCache[key] = `Candidate (App #${appId})`;
        return _appCandidateCache[key];
    }
}

async function loadInterviews() {
    const tbody = document.querySelector('#interviewsTable tbody');
    if (!tbody) return;
    tbody.innerHTML = `<tr><td colspan="4" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Loading interviews...</td></tr>`;
    try {
        const interviews = await apiRequest('GET', '/interviews');
        tbody.innerHTML = '';
        if (!interviews || interviews.length === 0) {
            tbody.innerHTML = `<tr><td colspan="4" class="text-center text-muted py-3">No interviews found.</td></tr>`;
            return;
        }

        // Enrich with candidate names in parallel
        const enriched = await Promise.all(interviews.map(async i => ({
            ...i,
            candidateName: await getCandidateNameForApplication(i.applicationId)
        })));

        enriched.forEach(i => {
            // scheduledDate is DateOnly from backend: "2026-05-26"
            const dateStr = i.scheduledDate
                ? new Date(i.scheduledDate + 'T00:00:00').toLocaleDateString()
                : '-';
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${escapeHtml(i.id)}</td>
                <td>${escapeHtml(i.candidateName)}</td>
                <td>${escapeHtml(dateStr)}</td>
                <td>${escapeHtml(i.mode ?? '')}</td>
            `;
            tbody.appendChild(tr);
        });
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="4" class="text-center text-danger py-3">${escapeHtml(err.message)}</td></tr>`;
    }
}

function initInterviewsPage() {
    const refreshBtn = document.getElementById('btnRefreshInterviews');
    if (refreshBtn) refreshBtn.addEventListener('click', loadInterviews);

    const form = document.getElementById('interviewCreateForm');
    if (form) {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const submitBtn = form.querySelector('button[type="submit"]');
            try {
                disableButton(submitBtn, true, 'Creating...');
                // Backend expects DateOnly (YYYY-MM-DD). The input is datetime-local → slice off the time part.
                const rawDate = document.getElementById('interviewDate').value; // "2026-05-31T13:11"
                const dateOnly = rawDate ? rawDate.substring(0, 10) : ''; // "2026-05-31"
                const body = {
                    applicationId: parseInt(document.getElementById('interviewApplicationId').value, 10),
                    scheduledDate: dateOnly,
                    mode: document.getElementById('interviewMode').value.trim(),
                    feedback: document.getElementById('interviewFeedback').value.trim()
                };
                await apiRequest('POST', '/interviews', body);
                showMessage('success', 'Interview scheduled successfully!');
                form.reset();
                await loadInterviews();
            } catch (err) {
                showMessage('danger', err.message || 'Failed to create interview.', 7000);
            } finally {
                disableButton(submitBtn, false);
            }
        });
    }

    loadInterviews();
}


/* ANALYTICS */
function setText(id, value) { const el = document.getElementById(id); if (el) el.innerText = value ?? '0'; }

function fillSimpleTable(tableId, rows, columns) {
    const table = document.querySelector(`#${tableId} tbody`);
    if (!table) return;
    table.innerHTML = '';
    if (!rows || !rows.length) { table.innerHTML = `<tr><td colspan="${columns.length}" class="text-center text-muted">No data found.</td></tr>`; return; }
    rows.forEach(row => { const tr = document.createElement('tr'); tr.innerHTML = columns.map(col => `<td>${row[col] ?? ''}</td>`).join(''); table.appendChild(tr); });
}

async function loadAnalytics() {
    if (!document.getElementById('metricTotalJobs')) return;
    try {
        const data = await apiRequest('GET', '/analytics', null, true);
        setText('metricTotalJobs', data.totalJobs); setText('metricTotalCandidates', data.totalCandidates);
        setText('metricTotalApplications', data.totalApplications); setText('metricTotalInterviews', data.totalInterviews);
        fillSimpleTable('applicationStatusTable', data.applicationsByStatus || [], ['status', 'count']);
        fillSimpleTable('jobStatusTable', data.jobsByStatus || [], ['status', 'count']);
        fillSimpleTable('topJobsTable', data.topJobsByApplications || [], ['jobId', 'title', 'applicationCount']);
    } catch (err) { showMessage('danger', 'Failed to load analytics: ' + err.message, 7000); }
}

function initAnalyticsPage() {
    const refreshBtn = document.getElementById('btnRefreshAnalytics');
    if (refreshBtn) refreshBtn.addEventListener('click', loadAnalytics);
    if (document.getElementById('metricTotalJobs')) loadAnalytics();
}


// ─────────────────────────────────────────────
// RANKING PAGE
// ─────────────────────────────────────────────
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

    // Populate V2 job info card if present
    const v2Card = document.getElementById('v2JobInfoCard');
    if (v2Card && result.jobId) {
        v2Card.classList.remove('d-none');
        const v2JobId = document.getElementById('v2JobId');
        const v2JobTitle = document.getElementById('v2JobTitle');
        const v2RequiredSkills = document.getElementById('v2RequiredSkills');
        if (v2JobId) v2JobId.textContent = result.jobId;
        if (v2JobTitle) v2JobTitle.textContent = result.jobTitle ?? '-';
        if (v2RequiredSkills) v2RequiredSkills.textContent = result.requiredSkills ?? '-';
    }

    rows.forEach(c => {
        const b = c.breakdown || {};
        const semanticPercent = typeof b.semanticScore === 'number' ? (b.semanticScore * 100).toFixed(1) : '0.0';
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
                <span class="badge bg-success">${escapeHtml(b.combinedScore ?? 0)}</span><br>
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
            expl.innerHTML = `<td colspan="7" class="bg-light"><small><strong>Explanation:</strong> ${escapeHtml(c.explanation)}</small></td>`;
            tableBody.appendChild(expl);
        }
    });
}

async function loadRanking(jobId, version) {
    // FIX: V1 route is /api/ranking/job/{jobId}, NOT /api/ranking/{jobId}
    const path = version === 'v2' ? `/ranking/v2/${jobId}` : `/ranking/job/${jobId}`;
    const tableBody = document.querySelector('#rankingTable tbody');
    if (tableBody) tableBody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Loading ranking...</td></tr>`;
    try {
        const result = await apiRequest('GET', path);
        if (version === 'v2') renderRankingV2(result);
        else renderRankingV1(Array.isArray(result) ? result : result?.rankedCandidates ?? []);
    } catch (err) {
        if (tableBody) tableBody.innerHTML = `<tr><td colspan="7" class="text-center text-danger">${escapeHtml(err.message)}</td></tr>`;
        showMessage('danger', err.message, 6000);
    }
}

function initRankingPage() {
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

// ─────────────────────────────────────────────
// ASSISTANT / AI TOOLS
// ─────────────────────────────────────────────
async function postJson(url, data) {
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(data)
    });
    let payload = null;
    try { payload = await res.json(); } catch { }
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
    if (resultBox) resultBox.textContent = 'Generating...';
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
    if (resultBox) resultBox.textContent = 'Generating...';
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
    if (resultBox) resultBox.textContent = 'Generating...';
    try {
        const data = await postJson('/api/assistant/email-template', payload);
        if (resultBox) resultBox.textContent = data.content || data.message || JSON.stringify(data, null, 2);
    } catch (e) {
        if (resultBox) resultBox.textContent = 'Error: ' + e.message;
    }
}

// ─────────────────────────────────────────────
// BOOTSTRAP
// ─────────────────────────────────────────────
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