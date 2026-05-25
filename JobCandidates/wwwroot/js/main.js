const API_BASE = window.location.origin + "/api";
let currentUser = null;

function showMessage(type, text, timeout = 5000) {
    const container = document.getElementById('messages');
    if (!container) return;
    const alert = document.createElement('div');
    alert.className = `alert alert-${type} alert-dismissible fade show shadow`;
    alert.role = 'alert';
    alert.innerHTML = `<div>${text}</div><button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>`;
    container.appendChild(alert);
    if (timeout > 0) {
        setTimeout(() => { if (alert.parentNode) bootstrap.Alert.getOrCreateInstance(alert).close(); }, timeout);
    }
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
        status.classList.remove('alert-info'); status.classList.add('alert-success');
        if (title) title.innerText = 'Signed in';
        if (details) details.innerText = `${currentUser.name || 'User'} (${currentUser.role || 'User'})`;
        if (emailEl) emailEl.innerText = currentUser.email || '-';
        if (roleEl) roleEl.innerText = currentUser.role || '-';
        if (logoutBtn) logoutBtn.disabled = false;
    } else {
        status.classList.remove('alert-success'); status.classList.add('alert-info');
        if (title) title.innerText = 'Not authenticated.';
        if (details) details.innerText = 'Please login first.';
        if (emailEl) emailEl.innerText = '-';
        if (roleEl) roleEl.innerText = '-';
        if (logoutBtn) logoutBtn.disabled = true;
    }
}

async function apiRequest(method, path, body = null, requireAuth = false) {
    const headers = { 'Content-Type': 'application/json' };
    const response = await fetch(API_BASE + path, { method, headers, credentials: 'include', body: body ? JSON.stringify(body) : null });
    let data = null;
    try { data = await response.json(); } catch { }
    if (response.status === 401) { currentUser = null; updateAuthStatus(); if (requireAuth) throw new Error(data?.message || 'You must login first.'); }
    if (response.status === 403) throw new Error(data?.message || 'You are not allowed to do this action.');
    if (!response.ok) throw new Error(data?.message || `Request failed with status ${response.status}`);
    return data;
}

async function loadCurrentUser() {
    try { const result = await apiRequest('GET', '/auth/me'); currentUser = result; } catch { currentUser = null; }
    updateAuthStatus();
}

function disableButton(button, disabled, loadingText = 'Please wait...') {
    if (!button) return;
    if (disabled) { button.dataset.originalText = button.innerHTML; button.disabled = true; button.innerHTML = loadingText; }
    else { button.disabled = false; button.innerHTML = button.dataset.originalText || button.innerHTML; }
}

function safeFormHandler(formId, buttonId, handler, loadingText) {
    const form = document.getElementById(formId);
    if (!form) return;
    const button = document.getElementById(buttonId);
    form.addEventListener('submit', async (e) => {
        e.preventDefault(); e.stopPropagation();
        try { disableButton(button, true, loadingText); await handler(e); }
        catch (err) { showMessage('danger', err.message || 'Something went wrong.', 7000); }
        finally { disableButton(button, false); }
        return false;
    });
    form.addEventListener('keydown', (e) => { if (e.key === 'Enter' && e.target.tagName.toLowerCase() !== 'textarea') e.preventDefault(); });
}

/* AUTH */
function initAuthPage() {
    const logoutBtn = document.getElementById('btnLogout');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', async (e) => {
            e.preventDefault();
            try { await apiRequest('POST', '/auth/logout', null, true); } catch { }
            currentUser = null; updateAuthStatus(); showMessage('info', 'Logged out successfully.', 3000);
        });
    }
    safeFormHandler('registerForm', 'btnRegister', async () => {
        const body = { name: document.getElementById('registerName').value.trim(), age: parseInt(document.getElementById('registerAge').value), gender: document.getElementById('registerGender').value, email: document.getElementById('registerEmail').value.trim(), role: document.getElementById('registerRole').value };
        const result = await apiRequest('POST', '/auth/register', body);
        document.getElementById('registerVerifyEmail').value = body.email;
        showMessage('success', result.message || 'Registration OTP sent.', 5000);
    }, 'Sending OTP...');
    safeFormHandler('registerVerifyForm', 'btnVerifyRegister', async (e) => {
        const body = { email: document.getElementById('registerVerifyEmail').value.trim(), code: document.getElementById('registerOtpCode').value.trim() };
        const result = await apiRequest('POST', '/auth/verify-register-otp', body);
        await loadCurrentUser(); showMessage('success', result.message || 'Account verified and logged in.', 4000); e.target.reset();
    }, 'Verifying...');
    safeFormHandler('loginRequestForm', 'btnRequestLoginOtp', async () => {
        const body = { email: document.getElementById('loginEmail').value.trim() };
        const result = await apiRequest('POST', '/auth/request-login-otp', body);
        document.getElementById('loginVerifyEmail').value = body.email;
        showMessage('success', result.message || 'Login OTP sent.', 5000);
    }, 'Sending OTP...');
    safeFormHandler('loginVerifyForm', 'btnVerifyLogin', async (e) => {
        const body = { email: document.getElementById('loginVerifyEmail').value.trim(), code: document.getElementById('loginOtpCode').value.trim() };
        const result = await apiRequest('POST', '/auth/verify-login-otp', body);
        await loadCurrentUser(); showMessage('success', result.message || 'Logged in successfully.', 4000); e.target.reset();
        setTimeout(() => { window.location.href = 'jobs.html'; }, 800);
    }, 'Logging in...');
}

/* JOBS */
async function loadJobs() {
    const table = document.querySelector('#jobsTable tbody');
    if (!table) return;
    try {
        const jobs = await apiRequest('GET', '/jobs');
        table.innerHTML = '';
        if (!jobs.length) { table.innerHTML = `<tr><td colspan="6" class="text-center text-muted">No jobs found.</td></tr>`; return; }
        jobs.forEach(j => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${j.id}</td>
                <td>${j.title}</td>
                <td><span class="badge ${j.status === 'Closed' ? 'bg-secondary' : 'bg-success'}">${j.status ?? ''}</span></td>
                <td>${j.location ?? ''}</td>
                <td>${j.postedByName ? `${j.postedByName} (${j.postedByEmail ?? ''})` : (j.postedByEmail ?? '')}</td>
                <td>
                    <button class="btn btn-sm btn-outline-warning me-1" onclick="editJob(${j.id}, '${(j.title || '').replace(/'/g, "\\'")}', '${(j.location || '').replace(/'/g, "\\'")}')">Edit</button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteJob(${j.id})">Delete</button>
                </td>`;
            table.appendChild(tr);
        });
    } catch (err) { showMessage('danger', 'Failed to load jobs: ' + err.message, 7000); }
}

window.editJob = function (id, title, location) {
    const newTitle = prompt('New title:', title);
    if (newTitle === null) return;
    const newLocation = prompt('New location:', location);
    if (newLocation === null) return;
    apiRequest('PUT', `/jobs/${id}`, { title: newTitle, location: newLocation }, true)
        .then(() => { showMessage('success', 'Job updated.', 4000); loadJobs(); })
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

/* CANDIDATES */
async function loadCandidates() {
    const table = document.querySelector('#candidatesTable tbody');
    if (!table) return;
    try {
        const candidates = await apiRequest('GET', '/candidates');
        table.innerHTML = '';
        if (!candidates.length) { table.innerHTML = `<tr><td colspan="5" class="text-center text-muted">No candidates found.</td></tr>`; return; }
        candidates.forEach(c => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${c.id}</td>
                <td>${c.name}</td>
                <td>${c.email}</td>
                <td>${c.experienceYears ?? 0}</td>
                <td>
                    <button class="btn btn-sm btn-outline-warning me-1" onclick="editCandidate(${c.id}, '${(c.name || '').replace(/'/g, "\\'")}', '${(c.phone || '').replace(/'/g, "\\'")}', '${(c.education || '').replace(/'/g, "\\'")}', ${c.experienceYears ?? 0}, '${(c.skills || '').replace(/'/g, "\\'")}')">Edit</button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteCandidate(${c.id})">Delete</button>
                </td>`;
            table.appendChild(tr);
        });
    } catch (err) { showMessage('danger', 'Failed to load candidates: ' + err.message, 7000); }
}

window.editCandidate = function (id, name, phone, education, experienceYears, skills) {
    const newName = prompt('Name:', name); if (newName === null) return;
    const newPhone = prompt('Phone:', phone); if (newPhone === null) return;
    const newEducation = prompt('Education:', education); if (newEducation === null) return;
    const newExp = prompt('Experience Years:', experienceYears); if (newExp === null) return;
    const newSkills = prompt('Skills:', skills); if (newSkills === null) return;
    apiRequest('PUT', `/candidates/${id}`, { name: newName, phone: newPhone, education: newEducation, experienceYears: parseInt(newExp) || 0, skills: newSkills }, true)
        .then(() => { showMessage('success', 'Candidate updated.', 4000); loadCandidates(); })
        .catch(err => showMessage('danger', 'Failed to update: ' + err.message, 7000));
};

window.deleteCandidate = function (id) {
    if (!confirm(`Delete candidate #${id}?`)) return;
    apiRequest('DELETE', `/candidates/${id}`, null, true)
        .then(() => { showMessage('success', 'Candidate deleted.', 4000); loadCandidates(); })
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
                <td>${a.candidateId}</td>
                <td>${a.jobId}</td>
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
async function loadInterviews() {
    const table = document.querySelector('#interviewsTable tbody');
    if (!table) return;
    try {
        const interviews = await apiRequest('GET', '/interviews');
        table.innerHTML = '';
        if (!interviews.length) { table.innerHTML = `<tr><td colspan="5" class="text-center text-muted">No interviews found.</td></tr>`; return; }
        interviews.forEach(i => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${i.id}</td>
                <td>${i.applicationId}</td>
                <td>${i.scheduledDate ?? ''}</td>
                <td>${i.mode ?? ''}</td>
                <td>
                    <button class="btn btn-sm btn-outline-warning me-1" onclick="editInterview(${i.id}, '${i.scheduledDate ?? ''}', '${(i.mode || '').replace(/'/g, "\\'")}', '${(i.feedback || '').replace(/'/g, "\\'")}')">Edit</button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteInterview(${i.id})">Delete</button>
                </td>`;
            table.appendChild(tr);
        });
    } catch (err) { showMessage('danger', 'Failed to load interviews: ' + err.message, 7000); }
}

window.editInterview = function (id, scheduledDate, mode, feedback) {
    const newDate = prompt('New date (YYYY-MM-DD):', scheduledDate); if (newDate === null) return;
    const newMode = prompt('Mode (Online/Offline):', mode); if (newMode === null) return;
    const newFeedback = prompt('Feedback:', feedback); if (newFeedback === null) return;
    apiRequest('PUT', `/interviews/${id}`, { scheduledDate: newDate, mode: newMode, feedback: newFeedback }, true)
        .then(() => { showMessage('success', 'Interview updated.', 4000); loadInterviews(); })
        .catch(err => showMessage('danger', 'Failed to update: ' + err.message, 7000));
};

window.deleteInterview = function (id) {
    if (!confirm(`Delete interview #${id}?`)) return;
    apiRequest('DELETE', `/interviews/${id}`, null, true)
        .then(() => { showMessage('success', 'Interview deleted.', 4000); loadInterviews(); })
        .catch(err => showMessage('danger', 'Failed to delete: ' + err.message, 7000));
};

function initInterviewsPage() {
    const refreshBtn = document.getElementById('btnRefreshInterviews');
    if (refreshBtn) refreshBtn.addEventListener('click', loadInterviews);
    const createForm = document.getElementById('interviewCreateForm');
    if (createForm) {
        createForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const body = {
                    applicationId: parseInt(document.getElementById('interviewApplicationId').value),
                    scheduledDate: document.getElementById('interviewDate').value.split('T')[0], // FIXED: date only
                    mode: document.getElementById('interviewMode').value.trim(),
                    feedback: document.getElementById('interviewFeedback').value.trim()
                };
                const result = await apiRequest('POST', '/interviews', body, true);
                showMessage('success', `Interview created. Id: ${result.id}`, 5000);
                e.target.reset(); await loadInterviews();
            } catch (err) { showMessage('danger', 'Failed to create interview: ' + err.message, 7000); }
        });
    }
    if (document.getElementById('interviewsTable')) loadInterviews();
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

document.addEventListener('DOMContentLoaded', async () => {
    try {
        const isAuthPage = window.location.pathname.endsWith('/index.html') || window.location.pathname === '/' || window.location.pathname.endsWith('/');
        if (isAuthPage) { try { await apiRequest('POST', '/auth/force-logout'); } catch { } }
        await loadCurrentUser();
        initAuthPage(); initJobsPage(); initCandidatesPage(); initApplicationsPage(); initInterviewsPage(); initAnalyticsPage();
    } catch (err) { console.error('Initialization error:', err); showMessage('danger', 'Frontend initialization failed: ' + err.message, 8000); }
});