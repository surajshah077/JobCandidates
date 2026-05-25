const API_BASE = window.location.origin + "/api";
let authToken = localStorage.getItem("authToken") || null;

function getToken() {
    return authToken;
}

function setToken(token) {
    authToken = token;
    localStorage.setItem("authToken", token);
}

function clearToken() {
    authToken = null;
    localStorage.removeItem("authToken");
}

function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(
            atob(base64)
                .split('')
                .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
                .join('')
        );
        return JSON.parse(jsonPayload);
    } catch {
        return null;
    }
}

function isTokenExpired(token) {
    const payload = parseJwt(token);
    if (!payload || !payload.exp) return true;
    return Date.now() >= payload.exp * 1000;
}

function showMessage(type, text, timeout = 5000) {
    const container = document.getElementById('messages');
    if (!container) return;

    const alert = document.createElement('div');
    alert.className = `alert alert-${type} alert-dismissible fade show shadow`;
    alert.role = 'alert';
    alert.innerHTML = `
        <div>${text}</div>
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    container.appendChild(alert);

    if (timeout > 0) {
        setTimeout(() => {
            if (alert.parentNode) {
                bootstrap.Alert.getOrCreateInstance(alert).close();
            }
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

    if (authToken && isTokenExpired(authToken)) {
        clearToken();
    }

    if (authToken) {
        const payload = parseJwt(authToken) || {};
        const email = payload.email || "-";
        const role = payload.role || "-";

        status.classList.remove('alert-info');
        status.classList.add('alert-success');
        if (title) title.innerText = 'Authenticated.';
        if (details) details.innerText = `Logged in as ${email} (${role}).`;
        if (emailEl) emailEl.innerText = email;
        if (roleEl) roleEl.innerText = role;
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

async function apiRequest(method, path, body = null, requireAuth = false) {
    const headers = { 'Content-Type': 'application/json' };

    if (authToken) {
        headers['Authorization'] = 'Bearer ' + authToken;
    } else if (requireAuth) {
        throw new Error('You must login first.');
    }

    console.log('API REQUEST', {
        method,
        url: API_BASE + path,
        hasToken: !!authToken,
        authHeader: headers['Authorization'] || 'missing'
    });

    const response = await fetch(API_BASE + path, {
        method,
        headers,
        body: body ? JSON.stringify(body) : null
    });

    let data = null;
    try {
        data = await response.json();
    } catch {
    }

    console.log('API RESPONSE', {
        status: response.status,
        data
    });

    if (response.status === 401) {
        clearToken();
        updateAuthStatus();
        throw new Error(data?.message || 'Unauthorized (401). Please login again.');
    }

    if (response.status === 403) {
        throw new Error(data?.message || 'Forbidden (403). Your role is not allowed.');
    }

    if (!response.ok) {
        throw new Error(data?.message || `Request failed with status ${response.status}`);
    }

    return data;
}

/* AUTH PAGE */
function initAuthPage() {
    const logoutBtn = document.getElementById('btnLogout');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', () => {
            clearToken();
            updateAuthStatus();
            showMessage('info', 'Logged out successfully.', 3000);
        });
    }

    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        registerForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const body = {
                    name: document.getElementById('registerName').value.trim(),
                    age: parseInt(document.getElementById('registerAge').value),
                    gender: document.getElementById('registerGender').value,
                    email: document.getElementById('registerEmail').value.trim(),
                    role: document.getElementById('registerRole').value
                };

                const result = await apiRequest('POST', '/auth/register', body);
                document.getElementById('registerVerifyEmail').value = body.email;
                showMessage('success', result.message || 'Registration OTP sent.', 5000);
            } catch (err) {
                showMessage('danger', 'Registration failed: ' + err.message, 7000);
            }
        });
    }

    const registerVerifyForm = document.getElementById('registerVerifyForm');
    if (registerVerifyForm) {
        registerVerifyForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const body = {
                    email: document.getElementById('registerVerifyEmail').value.trim(),
                    code: document.getElementById('registerOtpCode').value.trim()
                };

                const result = await apiRequest('POST', '/auth/verify-register-otp', body);
                setToken(result.token);
                updateAuthStatus();
                showMessage('success', 'Account verified and logged in.', 4000);
                e.target.reset();
            } catch (err) {
                showMessage('danger', 'Verification failed: ' + err.message, 7000);
            }
        });
    }

    const loginRequestForm = document.getElementById('loginRequestForm');
    if (loginRequestForm) {
        loginRequestForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const body = {
                    email: document.getElementById('loginEmail').value.trim()
                };

                const result = await apiRequest('POST', '/auth/request-login-otp', body);
                document.getElementById('loginVerifyEmail').value = body.email;
                showMessage('success', result.message || 'Login OTP sent.', 5000);
            } catch (err) {
                showMessage('danger', 'Login OTP request failed: ' + err.message, 7000);
            }
        });
    }

    const loginVerifyForm = document.getElementById('loginVerifyForm');
    if (loginVerifyForm) {
        loginVerifyForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const body = {
                    email: document.getElementById('loginVerifyEmail').value.trim(),
                    code: document.getElementById('loginOtpCode').value.trim()
                };

                const result = await apiRequest('POST', '/auth/verify-login-otp', body);
                setToken(result.token);
                updateAuthStatus();
                showMessage('success', 'Logged in successfully.', 4000);
                e.target.reset();
            } catch (err) {
                showMessage('danger', 'Login failed: ' + err.message, 7000);
            }
        });
    }
}

/* JOBS */
async function loadJobs() {
    const table = document.querySelector('#jobsTable tbody');
    if (!table) return;

    try {
        const jobs = await apiRequest('GET', '/jobs');
        table.innerHTML = '';

        if (!jobs.length) {
            table.innerHTML = `<tr><td colspan="5" class="text-center text-muted">No jobs found.</td></tr>`;
            return;
        }

        jobs.forEach(j => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${j.id}</td>
                <td>${j.title}</td>
                <td>${j.status ?? ''}</td>
                <td>${j.location ?? ''}</td>
                <td>${j.postedByEmail ?? ''}</td>
            `;
            table.appendChild(tr);
        });
    } catch (err) {
        showMessage('danger', 'Failed to load jobs: ' + err.message, 7000);
    }
}

function initJobsPage() {
    const refreshBtn = document.getElementById('btnRefreshJobs');
    if (refreshBtn) refreshBtn.addEventListener('click', loadJobs);

    const createForm = document.getElementById('jobCreateForm');
    if (createForm) {
        createForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const body = {
                    title: document.getElementById('jobTitle').value.trim(),
                    description: document.getElementById('jobDescription').value.trim(),
                    location: document.getElementById('jobLocation').value.trim(),
                    salaryRange: document.getElementById('jobSalaryRange').value.trim(),
                    requiredSkills: document.getElementById('jobRequiredSkills').value.trim()
                };

                const result = await apiRequest('POST', '/jobs', body, true);
                showMessage('success', `Job created successfully. Id: ${result.id}`, 5000);
                e.target.reset();
                await loadJobs();
            } catch (err) {
                showMessage('danger', 'Failed to create job: ' + err.message, 7000);
            }
        });
    }

    const closeForm = document.getElementById('jobCloseForm');
    if (closeForm) {
        closeForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const id = document.getElementById('jobCloseId').value;
                const result = await apiRequest('PUT', `/jobs/${id}/close`, null, true);
                showMessage('success', `Job ${result.id} closed.`, 4000);
                e.target.reset();
                await loadJobs();
            } catch (err) {
                showMessage('danger', 'Failed to close job: ' + err.message, 7000);
            }
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

        if (!candidates.length) {
            table.innerHTML = `<tr><td colspan="4" class="text-center text-muted">No candidates found.</td></tr>`;
            return;
        }

        candidates.forEach(c => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${c.id}</td>
                <td>${c.name}</td>
                <td>${c.email}</td>
                <td>${c.experienceYears ?? 0}</td>
            `;
            table.appendChild(tr);
        });
    } catch (err) {
        showMessage('danger', 'Failed to load candidates: ' + err.message, 7000);
    }
}

function initCandidatesPage() {
    const refreshBtn = document.getElementById('btnRefreshCandidates');
    if (refreshBtn) refreshBtn.addEventListener('click', loadCandidates);

    const createForm = document.getElementById('candidateCreateForm');
    if (createForm) {
        createForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const body = {
                    name: document.getElementById('candidateName').value.trim(),
                    email: document.getElementById('candidateEmail').value.trim(),
                    phone: document.getElementById('candidatePhone').value.trim(),
                    education: document.getElementById('candidateEducation').value.trim(),
                    experienceYears: parseInt(document.getElementById('candidateExperience').value || '0'),
                    skills: document.getElementById('candidateSkills').value.trim()
                };

                const result = await apiRequest('POST', '/candidates', body, true);
                showMessage('success', `Candidate created successfully. Id: ${result.id}`, 5000);
                e.target.reset();
                await loadCandidates();
            } catch (err) {
                showMessage('danger', 'Failed to create candidate: ' + err.message, 7000);
            }
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

        if (!apps.length) {
            table.innerHTML = `<tr><td colspan="4" class="text-center text-muted">No applications found.</td></tr>`;
            return;
        }

        apps.forEach(a => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${a.id}</td>
                <td>${a.candidateId}</td>
                <td>${a.jobId}</td>
                <td>${a.status ?? ''}</td>
            `;
            table.appendChild(tr);
        });
    } catch (err) {
        showMessage('danger', 'Failed to load applications: ' + err.message, 7000);
    }
}

function initApplicationsPage() {
    const refreshBtn = document.getElementById('btnRefreshApplications');
    if (refreshBtn) refreshBtn.addEventListener('click', loadApplications);

    const createForm = document.getElementById('applicationCreateForm');
    if (createForm) {
        createForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            try {
                const body = {
                    candidateId: parseInt(document.getElementById('applicationCandidateId').value),
                    jobId: parseInt(document.getElementById('applicationJobId').value),
                    notes: document.getElementById('applicationNotes').value.trim()
                };

                const result = await apiRequest('POST', '/applications', body, true);
                showMessage('success', `Application created successfully. Id: ${result.id}`, 5000);
                e.target.reset();
                await loadApplications();
            } catch (err) {
                showMessage('danger', 'Failed to create application: ' + err.message, 7000);
            }
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

        if (!interviews.length) {
            table.innerHTML = `<tr><td colspan="4" class="text-center text-muted">No interviews found.</td></tr>`;
            return;
        }

        interviews.forEach(i => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${i.id}</td>
                <td>${i.applicationId}</td>
                <td>${i.interviewDate ?? ''}</td>
                <td>${i.mode ?? ''}</td>
            `;
            table.appendChild(tr);
        });
    } catch (err) {
        showMessage('danger', 'Failed to load interviews: ' + err.message, 7000);
    }
}

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
                    interviewDate: document.getElementById('interviewDate').value,
                    mode: document.getElementById('interviewMode').value.trim(),
                    feedback: document.getElementById('interviewFeedback').value.trim()
                };

                const result = await apiRequest('POST', '/interviews', body, true);
                showMessage('success', `Interview created successfully. Id: ${result.id}`, 5000);
                e.target.reset();
                await loadInterviews();
            } catch (err) {
                showMessage('danger', 'Failed to create interview: ' + err.message, 7000);
            }
        });
    }

    if (document.getElementById('interviewsTable')) loadInterviews();
}

document.addEventListener('DOMContentLoaded', () => {
    updateAuthStatus();
    initAuthPage();
    initJobsPage();
    initCandidatesPage();
    initApplicationsPage();
    initInterviewsPage();
});