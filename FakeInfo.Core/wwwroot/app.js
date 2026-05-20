// ======================== STATE ========================
let currentUser = null;
let personPage = 1;

// ======================== NAV ========================
function showPage(name, btn) {
    document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
    document.getElementById('page-' + name).classList.add('active');
    document.querySelectorAll('.nav button').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');

    if (name === 'admin') loadAdminPage();
    if (name === 'login') updateLoginUI();
}

// ======================== TOP 5 NAVNE ========================
async function loadTopNames() {
    try {
        const res = await fetch('/api/stats/top-names');
        const names = await res.json();
        const bar = document.getElementById('topNamesBar');
        if (names.length === 0) { bar.innerHTML = ''; return; }
        bar.innerHTML = '<span style="font-size:12px;color:var(--text-muted);font-family:DM Mono,monospace;align-self:center;">TOP NAVNE:</span>' +
            names.map(n => `<span class="name-badge">${n.name}<span class="count">\u00d7${n.count}</span></span>`).join('');
    } catch(e) {}
}
loadTopNames();

// ======================== GENERATOR ========================
const generatePersonBtn = document.getElementById('generatePersonBtn');
const generateBulkBtn = document.getElementById('generateBulkBtn');
const bulkCountInput = document.getElementById('bulkCount');
const singleResult = document.getElementById('singleResult');
const bulkResult = document.getElementById('bulkResult');
const singleEmpty = document.getElementById('singleEmpty');
const bulkEmpty = document.getElementById('bulkEmpty');
const errorMessage = document.getElementById('errorMessage');

generatePersonBtn.addEventListener('click', async () => {
    clearGenerator();
    try {
        const res = await fetch('/api/person/full');
        if (!res.ok) throw new Error('Kunne ikke hente data');
        const person = await res.json();
        renderSingle(person);
        loadTopNames();
    } catch(err) { errorMessage.textContent = err.message; }
});

generateBulkBtn.addEventListener('click', async () => {
    clearGenerator();
    const count = parseInt(bulkCountInput.value);
    if (isNaN(count) || count < 2 || count > 100) {
        errorMessage.textContent = 'Antal skal vaere mellem 2 og 100';
        return;
    }
    try {
        const res = await fetch(`/api/person/bulk?count=${count}`);
        if (!res.ok) throw new Error('Kunne ikke hente bulk data');
        const persons = await res.json();
        renderBulk(persons);
        loadTopNames();
    } catch(err) { errorMessage.textContent = err.message; }
});

function renderSingle(person) {
    singleEmpty.classList.add('hidden');
    singleResult.classList.remove('hidden');
    singleResult.innerHTML = `<div class="card"><h3>Person Data</h3>${personHtml(person)}</div>`;
}

function renderBulk(persons) {
    bulkEmpty.classList.add('hidden');
    bulkResult.innerHTML = persons.map((p,i) =>
        `<div class="card"><h3>Person ${i+1}</h3>${personHtml(p)}</div>`
    ).join('');
}

function personHtml(p) {
    const a = p.address || {};
    return `
        <div class="field"><span class="field-label">Navn</span><span class="field-value">${p.firstName} ${p.lastName}</span></div>
        <div class="field"><span class="field-label">Koen</span><span class="field-value">${p.gender}</span></div>
        <div class="field"><span class="field-label">CPR</span><span class="field-value mono">${p.cpr}</span></div>
        <div class="field"><span class="field-label">Foedselsdato</span><span class="field-value">${fmtDate(p.dateOfBirth)}</span></div>
        <div class="field"><span class="field-label">Telefon</span><span class="field-value">${p.phone}</span></div>
        <div class="field"><span class="field-label">Adresse</span><span class="field-value">${a.street||''} ${a.number||''}, ${a.floor||''} ${a.door||''}<br>${a.postalCode||''} ${a.town||''}</span></div>`;
}

function fmtDate(d) { return d ? new Date(d).toLocaleDateString('da-DK') : ''; }

function clearGenerator() {
    errorMessage.textContent = '';
    singleResult.innerHTML = '';
    singleResult.classList.add('hidden');
    singleEmpty.classList.remove('hidden');
    bulkResult.innerHTML = '';
    bulkEmpty.classList.remove('hidden');
}

// ======================== CPR VALIDERING ========================
document.getElementById('validateCprBtn').addEventListener('click', async () => {
    const cpr = document.getElementById('cprInput').value.trim();
    const err = document.getElementById('cprError');
    const res_div = document.getElementById('cprResult');
    err.textContent = '';
    res_div.innerHTML = '';

    if (!cpr) { err.textContent = 'Indtast et CPR-nummer'; return; }

    try {
        const res = await fetch('/api/cpr/validate', {
            method: 'POST', headers: {'Content-Type':'application/json'},
            body: JSON.stringify({cpr})
        });
        const data = await res.json();

        const statusColor = data.isValid ? 'var(--success)' : 'var(--danger)';
        const statusText = data.isValid ? 'GYLDIGT' : 'UGYLDIGT';

        res_div.innerHTML = `<div class="card">
            <h3>CPR Validering <span style="color:${statusColor}">${statusText}</span></h3>
            <div class="field"><span class="field-label">CPR</span><span class="field-value mono">${data.cpr}</span></div>
            <div class="field"><span class="field-label">Laengde OK</span><span class="field-value">${data.hasValidLength ? '\u2713' : '\u2717'}</span></div>
            <div class="field"><span class="field-label">Kun cifre</span><span class="field-value">${data.isAllDigits ? '\u2713' : '\u2717'}</span></div>
            <div class="field"><span class="field-label">Gyldig dato</span><span class="field-value">${data.hasValidDate ? '\u2713' : '\u2717'}</span></div>
            <div class="field"><span class="field-label">Koen</span><span class="field-value">${data.detectedGender || '-'}</span></div>
            <div class="field"><span class="field-label">Foedselsdato</span><span class="field-value">${data.parsedDateOfBirth ? fmtDate(data.parsedDateOfBirth) : '-'}</span></div>
            <div class="field"><span class="field-label">Alder</span><span class="field-value">${data.calculatedAge ?? '-'}</span></div>
            ${data.errors.length ? '<div style="margin-top:12px;color:var(--danger);font-size:13px;">Fejl: ' + data.errors.join(', ') + '</div>' : ''}
        </div>`;
    } catch(e) { err.textContent = 'Fejl ved validering'; }
});

// ======================== ADRESSE SOEGNING ========================
document.getElementById('searchAddressBtn').addEventListener('click', async () => {
    const postal = document.getElementById('searchPostal').value.trim();
    const town = document.getElementById('searchTown').value.trim();
    const err = document.getElementById('addressError');
    const res_div = document.getElementById('addressResult');
    err.textContent = '';
    res_div.innerHTML = '';

    if (!postal && !town) { err.textContent = 'Angiv postnummer eller bynavn'; return; }

    try {
        const params = new URLSearchParams();
        if (postal) params.set('postalCode', postal);
        if (town) params.set('town', town);

        const res = await fetch('/api/address/search?' + params);
        if (!res.ok) { const d = await res.json(); err.textContent = d.error; return; }
        const data = await res.json();

        if (data.length === 0) { res_div.innerHTML = '<div class="empty-state">Ingen resultater</div>'; return; }

        res_div.innerHTML = `<div class="table-wrap"><table>
            <tr><th>Postnummer</th><th>By</th></tr>
            ${data.map(a => `<tr><td>${a.postalCode}</td><td>${a.town}</td></tr>`).join('')}
        </table></div>`;
    } catch(e) { err.textContent = 'Fejl ved soegning'; }
});

// ======================== LOGIN / REGISTER ========================
document.getElementById('loginBtn').addEventListener('click', async () => {
    const user = document.getElementById('loginUser').value.trim();
    const pass = document.getElementById('loginPass').value;
    const err = document.getElementById('loginError');
    err.textContent = '';

    try {
        const res = await fetch('/api/auth/login', {
            method:'POST', headers:{'Content-Type':'application/json'},
            body: JSON.stringify({username:user, password:pass})
        });
        const data = await res.json();
        if (!res.ok) { err.textContent = data.error; return; }
        currentUser = { username: data.username, role: data.role };
        updateLoginUI();
    } catch(e) { err.textContent = 'Login fejlede'; }
});

document.getElementById('registerBtn').addEventListener('click', async () => {
    const user = document.getElementById('regUser').value.trim();
    const pass = document.getElementById('regPass').value;
    const err = document.getElementById('regError');
    const suc = document.getElementById('regSuccess');
    err.textContent = ''; suc.textContent = '';

    try {
        const res = await fetch('/api/auth/register', {
            method:'POST', headers:{'Content-Type':'application/json'},
            body: JSON.stringify({username:user, password:pass})
        });
        const data = await res.json();
        if (!res.ok) { err.textContent = data.error; return; }
        suc.textContent = `Bruger '${data.username}' oprettet!`;
    } catch(e) { err.textContent = 'Registrering fejlede'; }
});

document.getElementById('logoutBtn').addEventListener('click', () => {
    currentUser = null;
    updateLoginUI();
});

function updateLoginUI() {
    const loggedIn = currentUser !== null;
    document.getElementById('loginSection').classList.toggle('hidden', loggedIn);
    document.getElementById('loggedInSection').classList.toggle('hidden', !loggedIn);
    if (loggedIn) {
        document.getElementById('loggedInUser').textContent = currentUser.username;
        document.getElementById('loggedInRole').textContent = currentUser.role;
    }
}

// ======================== ADMIN ========================
function loadAdminPage() {
    const isAdmin = currentUser && currentUser.role === 'admin';
    document.getElementById('adminLocked').classList.toggle('hidden', isAdmin);
    document.getElementById('adminContent').classList.toggle('hidden', !isAdmin);
    if (isAdmin) { loadDbPersons(1); loadDbUsers(); }
}

document.getElementById('loadStatsBtn').addEventListener('click', async () => {
    const div = document.getElementById('statsResult');
    div.innerHTML = '<div class="empty-state">Indlaeser...</div>';
    try {
        const res = await fetch('/api/stats');
        const s = await res.json();
        if (s.totalGenerated === 0) { div.innerHTML = '<div class="empty-state">Ingen data endnu</div>'; return; }
        div.innerHTML = `
            <div class="stats-grid">
                <div class="stat-card"><div class="stat-value">${s.totalGenerated}</div><div class="stat-label">Total genereret</div></div>
                <div class="stat-card"><div class="stat-value">${s.malePercentage}%</div><div class="stat-label">Maend</div></div>
                <div class="stat-card"><div class="stat-value">${s.femalePercentage}%</div><div class="stat-label">Kvinder</div></div>
                <div class="stat-card"><div class="stat-value">${s.averageAge}</div><div class="stat-label">Gns. alder</div></div>
                <div class="stat-card"><div class="stat-value">${s.youngestAge}</div><div class="stat-label">Yngste</div></div>
                <div class="stat-card"><div class="stat-value">${s.oldestAge}</div><div class="stat-label">Aeldste</div></div>
                <div class="stat-card"><div class="stat-value">${s.generatedToday}</div><div class="stat-label">I dag</div></div>
                <div class="stat-card"><div class="stat-value">${s.generatedThisWeek}</div><div class="stat-label">Denne uge</div></div>
            </div>
            <div class="section-header"><h2>Top 5 postnumre</h2><div class="section-line"></div></div>
            <div class="top-names">${s.topPostalCodes.map(p => `<span class="name-badge">${p.postalCode} ${p.town}<span class="count">\u00d7${p.count}</span></span>`).join('')}</div>
            <div class="section-header"><h2>Top 5 navne</h2><div class="section-line"></div></div>
            <div class="top-names">${s.topFirstNames.map(n => `<span class="name-badge">${n.name}<span class="count">\u00d7${n.count}</span></span>`).join('')}</div>`;
    } catch(e) { div.innerHTML = '<div class="empty-state">Fejl ved hentning</div>'; }
});

async function loadDbPersons(page) {
    personPage = page;
    const div = document.getElementById('dbPersons');
    try {
        const res = await fetch(`/api/person/all?page=${page}&pageSize=10`);
        const data = await res.json();
        if (data.total === 0) { div.innerHTML = '<div class="empty-state">Ingen personer i databasen</div>'; return; }
        div.innerHTML = `<div class="table-wrap"><table>
            <tr><th>ID</th><th>Navn</th><th>CPR</th><th>Koen</th><th>By</th><th>Oprettet</th><th></th></tr>
            ${data.data.map(p => `<tr>
                <td>${p.id}</td><td>${p.firstName} ${p.lastName}</td>
                <td style="font-family:DM Mono,monospace;color:var(--accent);">${p.cpr}</td>
                <td>${p.gender}</td><td>${p.postalCode} ${p.town}</td>
                <td>${fmtDate(p.createdAt)}</td>
                <td><button class="btn-danger" style="padding:4px 10px;font-size:12px;" onclick="deletePerson(${p.id})">Slet</button></td>
            </tr>`).join('')}
        </table></div>
        <div class="pagination">
            <button class="btn-secondary" style="padding:6px 12px;font-size:12px;" ${page<=1?'disabled':''} onclick="loadDbPersons(${page-1})">Forrige</button>
            <span>Side ${data.page} af ${data.totalPages}</span>
            <button class="btn-secondary" style="padding:6px 12px;font-size:12px;" ${page>=data.totalPages?'disabled':''} onclick="loadDbPersons(${page+1})">Naeste</button>
        </div>`;
    } catch(e) { div.innerHTML = '<div class="empty-state">Fejl</div>'; }
}

async function deletePerson(id) {
    if (!confirm('Slet denne person?')) return;
    try {
        await fetch(`/api/person/${id}`, {method:'DELETE'});
        loadDbPersons(personPage);
        loadTopNames();
    } catch(e) { alert('Fejl ved sletning'); }
}

async function loadDbUsers() {
    const div = document.getElementById('dbUsers');
    try {
        const res = await fetch('/api/auth/users');
        const users = await res.json();
        div.innerHTML = `<div class="table-wrap"><table>
            <tr><th>ID</th><th>Brugernavn</th><th>Rolle</th><th>Oprettet</th><th></th></tr>
            ${users.map(u => `<tr>
                <td>${u.id}</td><td>${u.username}</td>
                <td style="color:${u.role==='admin'?'var(--warn)':'var(--text-muted)'};">${u.role}</td>
                <td>${fmtDate(u.createdAt)}</td>
                <td>${u.role!=='admin'?`<button class="btn-danger" style="padding:4px 10px;font-size:12px;" onclick="deleteUser(${u.id})">Slet</button>`:''}</td>
            </tr>`).join('')}
        </table></div>`;
    } catch(e) { div.innerHTML = '<div class="empty-state">Fejl</div>'; }
}

async function deleteUser(id) {
    if (!confirm('Slet denne bruger?')) return;
    try {
        await fetch(`/api/auth/users/${id}`, {method:'DELETE'});
        loadDbUsers();
    } catch(e) { alert('Fejl ved sletning'); }
}