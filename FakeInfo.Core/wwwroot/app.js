const generatePersonBtn = document.getElementById("generatePersonBtn");
const generateBulkBtn = document.getElementById("generateBulkBtn");

const bulkCountInput = document.getElementById("bulkCount");

const singleResult = document.getElementById("singleResult");
const bulkResult = document.getElementById("bulkResult");

const singleEmpty = document.getElementById("singleEmpty");
const bulkEmpty = document.getElementById("bulkEmpty");

const errorMessage = document.getElementById("errorMessage");

// === GENERÉR 1 PERSON ===
generatePersonBtn.addEventListener("click", async () => {

    clear();

    try {

        const res = await fetch("/api/person/full");

        if (!res.ok) {
            throw new Error("Kunne ikke hente data");
        }

        const person = await res.json();

        renderSingle(person);

    } catch (err) {

        showError(err.message);
    }
});

// === GENERÉR BULK ===
generateBulkBtn.addEventListener("click", async () => {

    clear();

    const count = parseInt(bulkCountInput.value);

    if (count < 2 || count > 100) {

        showError("Antal skal være mellem 2 og 100");
        return;
    }

    try {

        const res = await fetch(`/api/person/bulk?count=${count}`);

        if (!res.ok) {
            throw new Error("Kunne ikke hente bulk data");
        }

        const persons = await res.json();

        renderBulk(persons);

    } catch (err) {

        showError(err.message);
    }
});

// === RENDER SINGLE ===
function renderSingle(person) {

    singleEmpty.classList.add("hidden");

    singleResult.classList.remove("hidden");

    singleResult.innerHTML = `
        <div class="card">
            <h3>Person Data</h3>
            ${createPersonHtml(person)}
        </div>
    `;
}

// === RENDER BULK ===
function renderBulk(persons) {

    bulkEmpty.classList.add("hidden");

    bulkResult.innerHTML = "";

    persons.forEach((person, index) => {

        const div = document.createElement("div");

        div.className = "card";

        div.innerHTML = `
            <h3>Person ${index + 1}</h3>
            ${createPersonHtml(person)}
        `;

        bulkResult.appendChild(div);
    });
}

// === TEMPLATE ===
function createPersonHtml(p) {

    const a = p.address || {};

    return `
        <div class="field">
            <span class="field-label">Navn</span>
            <span class="field-value">
                ${p.firstName} ${p.lastName}
            </span>
        </div>

        <div class="field">
            <span class="field-label">Køn</span>
            <span class="field-value">
                ${p.gender}
            </span>
        </div>

        <div class="field">
            <span class="field-label">CPR</span>
            <span class="field-value mono">
                ${p.cpr}
            </span>
        </div>

        <div class="field">
            <span class="field-label">Fødselsdato</span>
            <span class="field-value">
                ${formatDate(p.dateOfBirth)}
            </span>
        </div>

        <div class="field">
            <span class="field-label">Telefon</span>
            <span class="field-value">
                ${p.phone}
            </span>
        </div>

        <div class="field">
            <span class="field-label">Adresse</span>

            <span class="field-value">
                ${a.street || ""}
                ${a.number || ""}
                ${a.floor || ""}
                ${a.door || ""}
                <br>
                ${a.postalCode || ""}
                ${a.town || ""}
            </span>
        </div>
    `;
}

// === FORMAT DATE ===
function formatDate(date) {

    if (!date) return "";

    return new Date(date).toLocaleDateString("da-DK");
}

// === CLEAR ===
function clear() {

    errorMessage.textContent = "";

    singleResult.innerHTML = "";
    bulkResult.innerHTML = "";

    singleResult.classList.add("hidden");

    singleEmpty.classList.remove("hidden");
    bulkEmpty.classList.remove("hidden");
}

// === ERROR ===
function showError(message) {

    errorMessage.textContent = message;
}