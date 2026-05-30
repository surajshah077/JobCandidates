const API_BASE = window.location.origin + "/api";
let currentUser = null;

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

    safeFormHandler('registerForm', 'btnRegister', async () => {
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

// ─── JOBS ────────────────────────────────────────────────────────────────────

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
    const newTitle = prompt('Title:', title); if (newTitle === null) return;
    const newDescription = prompt('Description:', description); if (newDescription === null) return;
    const newLocation = prompt('Location:', location); if (newLocation === null) return;
    const newSalaryRange = prompt('Salary Range:', salaryRange); if (newSalaryRange === null) return;
    const newRequiredSkills = prompt('Required Skills (comma separated):', requiredSkills); if (newRequiredSkills === null) return;

    apiRequest('PUT', `/jobs/${id}`, {
        title: newTitle.trim(),
        description: newDescription.trim(),
        location: newLocation.trim(),
        salaryRange: newSalaryRange.trim(),
        requiredSkills: newRequiredSkills.trim(),
        status: status || 'Open'
    })
        .then(() => { showMessage('success', 'Job updated.', 4000); loadJobs(); })
        .catch(err => showMessage('danger', 'Failed to update job: ' + err.message, 7000));
};

window.deleteJob = function (id) {
    if (!confirm(`Delete job #${id}?`)) return;
    apiRequest('DELETE', `/jobs/${id}`)
        .then(() => { showMessage('success', 'Job deleted.', 4000); loadJobs(); })
        .catch(err => showMessage('danger', 'Failed to delete job: ' + err.message, 7000));
};

function initJobsPage() {
    const refreshBtn = document.getElementById('btnRefreshJobs');
    if (refreshBtn) refreshBtn.addEventListener('click', loadJobs);

    safeFormHandler('jobCreateForm', 'btnCreateJob', async (e) => {
        const body = {
            title: document.getElementById('jobTitle').value.trim(),
            description: document.getElementById('jobDescription').value.trim(),
            location: document.getElementById('jobLocation').value.trim(),
            salaryRange: document.getElementById('jobSalaryRange').value.trim(),
            requiredSkills: document.getElementById('jobRequiredSkills').value.trim()
        };
        const result = await apiRequest('POST', '/jobs', body);
        showMessage('success', `Job created successfully. ID: ${result.id}`, 5000);
        e.target.reset();
        await loadJobs();
    }, 'Creating...');

    safeFormHandler('jobCloseForm', 'btnCloseJob', async (e) => {
        const id = document.getElementById('jobCloseId').value;
        await apiRequest('PUT', `/jobs/${id}/close`);
        showMessage('success', `Job #${id} closed successfully.`, 5000);
        e.target.reset();
        await loadJobs();
    }, 'Closing...');

    if (document.getElementById('jobsTable')) loadJobs();
}

// ─── CANDIDATES ──────────────────────────────────────────────────────────────

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
            if (c.email && c.email.trim() !== '') candidateEmailCache[c.id] = c.email;
            const resolvedEmail = (c.email && c.email.trim() !== '') ? c.email : (candidateEmailCache[c.id] || '');

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
    const newName = prompt('Name:', name); if (newName === null) return;
    const newEmail = prompt('Email:', email); if (newEmail === null) return;
    const newPhone = prompt('Phone:', phone); if (newPhone === null) return;
    const newEducation = prompt('Education:', education); if (newEducation === null) return;
    const newExp = prompt('Experience Years:', experienceYears); if (newExp === null) return;
    const newSkills = prompt('Skills (comma separated):', skills); if (newSkills === null) return;

    apiRequest('PUT', `/candidates/${id}`, {
        name: newName.trim(),
        email: newEmail.trim(),
        phone: newPhone.trim(),
        education: newEducation.trim(),
        experienceYears: parseInt(newExp) || 0,
        skills: newSkills.trim()
    })
        .then(() => {
            candidateEmailCache[id] = newEmail.trim();
            showMessage('success', 'Candidate updated.', 4000);
            loadCandidates();
        })
        .catch(err => showMessage('danger', 'Failed to update candidate: ' + err.message, 7000));
};

window.deleteCandidate = function (id) {
    if (!confirm(`Delete candidate #${id}?`)) return;
    apiRequest('DELETE', `/candidates/${id}`)
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

    safeFormHandler('candidateCreateForm', 'btnCreateCandidate', async (e) => {
        const body = {
            name: document.getElementById('candidateName').value.trim(),
            email: document.getElementById('candidateEmail').value.trim(),
            phone: document.getElementById('candidatePhone').value.trim(),
            education: document.getElementById('candidateEducation').value.trim(),
            experienceYears: parseInt(document.getElementById('candidateExperience').value || '0'),
            skills: document.getElementById('candidateSkills').value.trim()
        };
        const result = await apiRequest('POST', '/candidates', body);
        showMessage('success', `Candidate created. ID: ${result.id}`, 5000);
        e.target.reset();
        await loadCandidates();
    }, 'Creating...');

    if (document.getElementById('candidatesTable')) loadCandidates();
}

// ─── APPLICATIONS ─────────────────────────────────────────────────────────────

async function loadApplications() {
    const table = document.querySelector('#applicationsTable tbody');
    if (!table) return;
    try {
        const apps = await apiRequest('GET', '/applications');
        table.innerHTML = '';

        if (!apps.length) {
            table.innerHTML = `<tr><td colspan="5" class="text-center text-muted">No applications found.</td></tr>`;
            return;
        }

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
    } catch (err) {
        showMessage('danger', 'Failed to load applications: ' + err.message, 7000);
    }
}

window.updateApplicationStatus = function (id) {
    const statusOptions = ['Applied', 'Screening', 'TechnicalInterview', 'HRInterview', 'Offer', 'Rejected'];
    const chosen = prompt(`New status for Application #${id}?\nOptions: ${statusOptions.join(', ')}`);
    if (!chosen) return;
    if (!statusOptions.includes(chosen)) {
        showMessage('warning', 'Invalid status. Choose from: ' + statusOptions.join(', '), 5000);
        return;
    }
    const notes = prompt('Notes (optional):', '') || '';
    apiRequest('PUT', `/applications/${id}/status`, { status: chosen, notes })
        .then(() => { showMessage('success', `Application #${id} status updated to ${chosen}.`, 4000); loadApplications(); })
        .catch(err => showMessage('danger', 'Failed to update status: ' + err.message, 7000));
};

window.deleteApplication = function (id) {
    if (!confirm(`Delete application #${id}?`)) return;
    apiRequest('DELETE', `/applications/${id}`)
        .then(() => { showMessage('success', 'Application deleted.', 4000); loadApplications(); })
        .catch(err => showMessage('danger', 'Failed to delete: ' + err.message, 7000));
};

function initApplicationsPage() {
    const refreshBtn = document.getElementById('btnRefreshApplications');
    if (refreshBtn) refreshBtn.addEventListener('click', loadApplications);

    safeFormHandler('applicationCreateForm', 'btnCreateApplication', async (e) => {
        const body = {
            candidateId: parseInt(document.getElementById('applicationCandidateId').value),
            jobId: parseInt(document.getElementById('applicationJobId').value),
            notes: document.getElementById('applicationNotes').value.trim()
        };
        const result = await apiRequest('POST', '/applications', body);
        showMessage('success', `Application created. ID: ${result.id}`, 5000);
        e.target.reset();
        await loadApplications();
    }, 'Creating...');

    if (document.getElementById('applicationsTable')) loadApplications();
}

// ─── INTERVIEWS ───────────────────────────────────────────────────────────────

async function loadInterviews() {
    const table = document.querySelector('#interviewsTable tbody');
    if (!table) return;
    try {
        const interviews = await apiRequest('GET', '/interviews');
        table.innerHTML = '';

        if (!interviews.length) {
            table.innerHTML = `<tr><td colspan="5" class="text-center text-muted">No interviews found.</td></tr>`;
            return;
        }

        interviews.forEach(i => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${i.id}</td>
                <td>${i.applicationId}</td>
                <td>${i.scheduledDate ?? ''}</td>
                <td>${i.mode ?? ''}</td>
                <td>
                    <button class="btn btn-sm btn-outline-warning me-1"
                        onclick="editInterview(${i.id}, '${i.scheduledDate ?? ''}', '${(i.mode || '').replace(/'/g, "\\'")}', '${(i.feedback || '').replace(/'/g, "\\'")}')">
                        Edit
                    </button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteInterview(${i.id})">Delete</button>
                </td>`;
            table.appendChild(tr);
        });
    } catch (err) {
        showMessage('danger', 'Failed to load interviews: ' + err.message, 7000);
    }
}

window.editInterview = function (id, scheduledDate, mode, feedback) {
    const newDate = prompt('New date (YYYY-MM-DD):', scheduledDate); if (newDate === null) return;
    const newMode = prompt('Mode (Online/Offline):', mode); if (newMode === null) return;
    const newFeedback = prompt('Feedback:', feedback); if (newFeedback === null) return;

    apiRequest('PUT', `/interviews/${id}`, { scheduledDate: newDate, mode: newMode, feedback: newFeedback })
        .then(() => { showMessage('success', 'Interview updated.', 4000); loadInterviews(); })
        .catch(err => showMessage('danger', 'Failed to update: ' + err.message, 7000));
};

window.deleteInterview = function (id) {
    if (!confirm(`Delete interview #${id}?`)) return;
    apiRequest('DELETE', `/interviews/${id}`)
        .then(() => { showMessage('success', 'Interview deleted.', 4000); loadInterviews(); })
        .catch(err => showMessage('danger', 'Failed to delete: ' + err.message, 7000));
};

function initInterviewsPage() {
    const refreshBtn = document.getElementById('btnRefreshInterviews');
    if (refreshBtn) refreshBtn.addEventListener('click', loadInterviews);

    safeFormHandler('interviewCreateForm', 'btnCreateInterview', async (e) => {
        const body = {
            applicationId: parseInt(document.getElementById('interviewApplicationId').value),
            scheduledDate: document.getElementById('interviewDate').value.split('T')[0],
            mode: document.getElementById('interviewMode').value.trim(),
            feedback: document.getElementById('interviewFeedback').value.trim()
        };
        const result = await apiRequest('POST', '/interviews', body);
        showMessage('success', `Interview created. ID: ${result.id}`, 5000);
        e.target.reset();
        await loadInterviews();
    }, 'Creating...');

    if (document.getElementById('interviewsTable')) loadInterviews();
}

// ─── RANKING ──────────────────────────────────────────────────────────────────

let currentRankingJobId = null;
let currentRankingVersion = 'v1';

async function loadRanking(jobId, version = 'v1') {
    const table = document.querySelector('#rankingTable tbody');
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

    table.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-3">
        <span class="spinner-border spinner-border-sm me-2"></span>Loading ranking...
    </td></tr>`;

    try {
        let rankedCandidates = [];
        let jobInfo = null;

        if (version === 'v2') {
            const result = await apiRequest('GET', `/Ranking/v2/${jobId}`);
            jobInfo = result;

            if (v2Card) {
                const jobIdEl = document.getElementById('v2JobId');
                const titleEl = document.getElementById('v2JobTitle');
                const skillsEl = document.getElementById('v2RequiredSkills');
                if (jobIdEl) jobIdEl.textContent = result.jobId ?? jobId;
                if (titleEl) titleEl.textContent = result.jobTitle ?? '-';
                if (skillsEl) skillsEl.textContent = result.requiredSkills ?? '-';
                v2Card.classList.remove('d-none');
            }

            rankedCandidates = Array.isArray(result) ? result : (result.rankedCandidates ?? []);
        } else {
            rankedCandidates = await apiRequest('GET', `/ranking/job/${jobId}`);
        }

        table.innerHTML = '';

        if (!rankedCandidates || !rankedCandidates.length) {
            table.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-4">No ranked candidates found.</td></tr>`;
            return;
        }

        const medals = ['🥇', '🥈', '🥉'];

        rankedCandidates.forEach((c, index) => {
            const tr = document.createElement('tr');
            if (index === 0) tr.classList.add('table-warning');

            const rankDisplay = index < 3
                ? `<span style="font-size:1.2rem">${medals[index]}</span>`
                : `<span class="badge bg-secondary">#${index + 1}</span>`;

            const experienceScore = c.experienceYears * 5;
            const skillScore = c.skillMatchScore ?? 0;
            const totalScore = c.totalScore ?? (skillScore + experienceScore);

            tr.innerHTML = `
                <td>${rankDisplay}</td>
                <td>${c.candidateId}</td>
                <td><strong>${c.candidateName ?? ''}</strong></td>
                <td>${c.experienceYears ?? 0}</td>
                <td><span class="badge bg-info text-dark">${skillScore}</span></td>
                <td><span class="badge bg-secondary">${experienceScore}</span></td>
                <td><strong class="text-primary fs-6">${totalScore}</strong></td>
            `;
            table.appendChild(tr);
        });

        if (summary) {
            const top = rankedCandidates[0];
            summary.style.display = 'block';
            summary.innerHTML = `
                <strong>Job ID ${jobId}</strong> [${version.toUpperCase()}] — 
                ${rankedCandidates.length} candidates ranked. 
                Top: <strong>${top.candidateName}</strong> with score <strong>${top.totalScore ?? ((top.skillMatchScore ?? 0) + (top.experienceYears ?? 0) * 5)}</strong>.
            `;
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

    if (form) {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const jobId = parseInt(input.value);
            if (!jobId || jobId <= 0) {
                showMessage('warning', 'Please enter a valid Job ID (must be > 0).', 4000);
                return;
            }
            const version = document.querySelector('input[name="rankingVersion"]:checked')?.value ?? 'v1';
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
}

// ─── ANALYTICS ────────────────────────────────────────────────────────────────

function setText(id, value) {
    const el = document.getElementById(id);
    if (el) el.innerText = value ?? '0';
}

function fillSimpleTable(tableId, rows, columns) {
    const table = document.querySelector(`#${tableId} tbody`);
    if (!table) return;
    table.innerHTML = '';
    if (!rows || !rows.length) {
        table.innerHTML = `<tr><td colspan="${columns.length}" class="text-center text-muted">No data found.</td></tr>`;
        return;
    }
    rows.forEach(row => {
        const tr = document.createElement('tr');
        tr.innerHTML = columns.map(col => `<td>${row[col] ?? ''}</td>`).join('');
        table.appendChild(tr);
    });
}

async function loadAnalytics() {
    if (!document.getElementById('metricTotalJobs')) return;
    try {
        const data = await apiRequest('GET', '/analytics');
        setText('metricTotalJobs', data.totalJobs);
        setText('metricTotalCandidates', data.totalCandidates);
        setText('metricTotalApplications', data.totalApplications);
        setText('metricTotalInterviews', data.totalInterviews);
        fillSimpleTable('applicationStatusTable', data.applicationsByStatus || [], ['status', 'count']);
        fillSimpleTable('jobStatusTable', data.jobsByStatus || [], ['status', 'count']);
        fillSimpleTable('topJobsTable', data.topJobsByApplications || [], ['jobId', 'title', 'applicationCount']);
    } catch (err) {
        showMessage('danger', 'Failed to load analytics: ' + err.message, 7000);
    }
}

function initAnalyticsPage() {
    const refreshBtn = document.getElementById('btnRefreshAnalytics');
    if (refreshBtn) refreshBtn.addEventListener('click', loadAnalytics);
    if (document.getElementById('metricTotalJobs')) loadAnalytics();
}

// ─── INIT ─────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', async () => {
    try {
        const path = window.location.pathname;
        const isAuthPage = path.endsWith('/index.html') || path === '/' || path.endsWith('/');
        if (isAuthPage) {
            try { await apiRequest('POST', '/auth/force-logout'); } catch { }
        }

        await loadCurrentUser();

        initAuthPage();
        initJobsPage();
        initCandidatesPage();
        initApplicationsPage();
        initInterviewsPage();
        initRankingPage();
        initAnalyticsPage();
    } catch (err) {
        console.error('Initialization error:', err);
        showMessage('danger', 'Frontend initialization failed: ' + err.message, 8000);
    }
});