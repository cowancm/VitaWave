// database.js
const apiBaseUrl = "http://localhost:5000/Database";

const readBtn = document.getElementById("readEvents");
const startBtn = document.getElementById("startLogging");
const stopBtn = document.getElementById("stopLogging");
const clearBtn = document.getElementById("clearDatabase");
const saveCsvBtn = document.getElementById("saveCsv");
const insertBtn = document.getElementById("insertEvent");
const output = document.getElementById("dataOutput");
const statusMsg = document.getElementById("statusMessage");

function showMessage(msg, isError = false) {
    statusMsg.textContent = msg;
    statusMsg.style.color = isError ? "#ef4444" : "#22c55e";
    console.log(`[${isError ? 'ERROR' : 'SUCCESS'}] ${msg}`);
}

// Read events from the database
readBtn.addEventListener("click", async () => {
    showMessage("Reading events...");
    console.log(`Attempting to fetch: ${apiBaseUrl}/read`);
    try {
        const res = await fetch(`${apiBaseUrl}/read`);
        console.log('Response status:', res.status);
        if (!res.ok) throw new Error(`Read failed with status ${res.status}`);
        const data = await res.json();
        console.log('Data received:', data);
        output.innerHTML = `<pre style="color: white; padding: 1rem;">${JSON.stringify(data, null, 2)}</pre>`;
        showMessage("Events loaded!");
    } catch (err) {
        console.error('Fetch error:', err);
        showMessage(`Error: ${err.message}. Check console (F12) for details.`, true);
    }
});

// Start logging
startBtn.addEventListener("click", async () => {
    showMessage("Starting logging...");
    console.log(`Attempting to POST: ${apiBaseUrl}/start`);
    try {
        const res = await fetch(`${apiBaseUrl}/start`, { method: "POST" });
        console.log('Response status:', res.status);
        const text = await res.text();
        console.log('Response text:', text);
        showMessage(text);
    } catch (err) {
        console.error('Fetch error:', err);
        showMessage(`Error: ${err.message}. Check console (F12) for details.`, true);
    }
});

// Stop logging
stopBtn.addEventListener("click", async () => {
    showMessage("Stopping logging...");
    console.log(`Attempting to POST: ${apiBaseUrl}/stop`);
    try {
        const res = await fetch(`${apiBaseUrl}/stop`, { method: "POST" });
        console.log('Response status:', res.status);
        const text = await res.text();
        console.log('Response text:', text);
        showMessage(text);
    } catch (err) {
        console.error('Fetch error:', err);
        showMessage(`Error: ${err.message}. Check console (F12) for details.`, true);
    }
});

// Clear the database
clearBtn.addEventListener("click", async () => {
    if (!confirm("Are you sure you want to clear all events?")) return;
    showMessage("Clearing database...");
    console.log(`Attempting to POST: ${apiBaseUrl}/clear`);
    try {
        const res = await fetch(`${apiBaseUrl}/clear`, { method: "POST" });
        console.log('Response status:', res.status);
        const text = await res.text();
        console.log('Response text:', text);
        showMessage(text);
        output.innerHTML = "";
    } catch (err) {
        console.error('Fetch error:', err);
        showMessage(`Error: ${err.message}. Check console (F12) for details.`, true);
    }
});

// Insert a test event
if (insertBtn) {
    insertBtn.addEventListener("click", async () => {
        showMessage("Inserting test event...");
        console.log(`Attempting to POST: ${apiBaseUrl}/insert`);

        // Sample event data
        const eventData = {
            "ev": {
                "ModuleID": "TestModule",
                "TID": 12345,
                "ResultId": "TestEvent"
            }
        };

        try {
            const res = await fetch(`${apiBaseUrl}/insert`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(eventData)
            });

            console.log('Response status:', res.status);
            const text = await res.text();
            console.log('Response text:', text);

            if (!res.ok) {
                throw new Error(`Insert failed with status ${res.status}: ${text}`);
            }

            showMessage(text);
        } catch (err) {
            console.error('Insert error:', err);
            showMessage(`Error: ${err.message}`, true);
        }
    });
}

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