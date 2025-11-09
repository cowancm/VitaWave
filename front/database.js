// database.js

const apiBaseUrl = "http://localhost:5000/Database";

const readBtn = document.getElementById("readEvents");
const startBtn = document.getElementById("startLogging");
const stopBtn = document.getElementById("stopLogging");
const clearBtn = document.getElementById("clearDatabase");
const saveCsvBtn = document.getElementById("saveCsv");

const output = document.getElementById("dataOutput");
const statusMsg = document.getElementById("statusMessage");

function showMessage(msg, isError = false) {
    statusMsg.textContent = msg;
    statusMsg.style.color = isError ? "red" : "green";
}

// Read events from the database
readBtn.addEventListener("click", async () => {
    showMessage("Reading events...");
    try {
        const res = await fetch(`${apiBaseUrl}/read`);
        if (!res.ok) throw new Error("Read failed");
        const data = await res.json();

        output.innerHTML = `
      <pre>${JSON.stringify(data, null, 2)}</pre>
    `;
        showMessage("Events loaded!");
    } catch (err) {
        showMessage(err.message, true);
    }
});

// Start logging
startBtn.addEventListener("click", async () => {
    showMessage("Starting logging...");
    try {
        const res = await fetch(`${apiBaseUrl}/start`, { method: "POST" });
        const text = await res.text();
        showMessage(text);
    } catch (err) {
        showMessage(err.message, true);
    }
});

// Stop logging
stopBtn.addEventListener("click", async () => {
    showMessage("Stopping logging...");
    try {
        const res = await fetch(`${apiBaseUrl}/stop`, { method: "POST" });
        const text = await res.text();
        showMessage(text);
    } catch (err) {
        showMessage(err.message, true);
    }
});


// insert Events into database
document.getElementById('insertEvents').addEventListener('click', async () => {
    const eventData = {
        ModuleID: "Module_Alpha",   // replace with real module ID
        TID: 1,
        ResultId: "Fall"            // or any ResultID you want
    };

    try {
        const response = await fetch('http://localhost:5000/Event/insert', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(eventData)
        });

        const text = await response.text();
        document.getElementById('statusMessage').innerText = text;
    } catch (err) {
        console.error(err);
        document.getElementById('statusMessage').innerText = "Failed to insert event.";
    }
});

// Clear the database
clearBtn.addEventListener("click", async () => {
    if (!confirm("Are you sure you want to clear all events?")) return;

    showMessage("Clearing database...");
    try {
        const res = await fetch(`${apiBaseUrl}/clear`, { method: "POST" });
        const text = await res.text();
        showMessage(text);
        output.innerHTML = "";
    } catch (err) {
        showMessage(err.message, true);
    }
});

// Save current events to CSV
saveCsvBtn.addEventListener("click", async () => {
    showMessage("Generating CSV...");
    try {
        // Redirect browser to GET endpoint for CSV download
        window.location.href = `${apiBaseUrl}/save-csv`;
        showMessage("CSV download started.");
    } catch (err) {
        showMessage(err.message, true);
    }
});
