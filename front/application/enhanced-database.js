// enhanced-database.js - Fixed version for your HTML
console.log('enhanced-database.js loaded');

class DatabaseManager {
    constructor() {
        this.apiBaseUrl = "http://localhost:5000/Database";
        this.initializeElements();
        this.attachEventListeners();
        this.startAutoRefresh();
    }
    
    initializeElements() {
        this.elements = {
            saveCsvBtn: document.getElementById("saveCsv"),
            clearDatabaseBtn: document.getElementById("clearDatabase"),
            output: document.getElementById("dataOutput"),
            statusMsg: document.getElementById("statusMessage"),
            lastUpdated: document.getElementById("dataLastUpdated")
        };
    }
    
    attachEventListeners() {
        // Only attach if element exists
        if (this.elements.saveCsvBtn) {
            this.elements.saveCsvBtn.addEventListener("click", () => this.saveCsv());
        }
        if (this.elements.clearDatabaseBtn) {
            this.elements.clearDatabaseBtn.addEventListener("click", () => this.clearDatabase());
        }
    }
    
    showMessage(msg, isError = false) {
        if (this.elements.statusMsg) {
            this.elements.statusMsg.innerHTML = `
                <div class="status-message ${isError ? 'error' : 'success'}">
                    <span>${isError ? '❌' : '✅'}</span>
                    <span>${msg}</span>
                </div>
            `;
        }
        console.log(`[${isError ? 'ERROR' : 'SUCCESS'}] ${msg}`);
        
        // Also show toast notification
        this.showToast(msg, isError);
    }
    
    showToast(message, isError = false) {
        // Remove any existing toasts to prevent overlap
        const existingToasts = document.querySelectorAll('.toast-notification');
        existingToasts.forEach(toast => {
            toast.style.opacity = '0';
            setTimeout(() => toast.remove(), 300);
        });
        
        // Create new toast
        const toast = document.createElement('div');
        toast.className = 'toast-notification';
        toast.style.cssText = `
            position: fixed;
            top: 100px;
            right: 20px;
            z-index: 9999;
            min-width: 300px;
            background: ${isError ? 'rgba(220, 38, 38, 0.95)' : 'rgba(16, 185, 129, 0.95)'};
            color: white;
            padding: 1rem 1.5rem;
            border-radius: 8px;
            box-shadow: 0 8px 16px rgba(0, 0, 0, 0.3);
            display: flex;
            align-items: center;
            gap: 0.75rem;
            font-size: 0.875rem;
            animation: slideIn 0.3s ease;
            backdrop-filter: blur(10px);
        `;
        toast.innerHTML = `
            <span style="font-size: 1.25rem;">${isError ? '❌' : '✅'}</span>
            <span>${message}</span>
        `;
        
        document.body.appendChild(toast);
        
        // Auto remove after 4 seconds
        setTimeout(() => {
            toast.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(400px)';
            setTimeout(() => toast.remove(), 300);
        }, 4000);
    }
    
    updateTimestamp() {
        if (this.elements.lastUpdated) {
            const now = new Date();
            const formatted = now.toLocaleString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
            this.elements.lastUpdated.textContent = formatted;
        }
    }
    
    async readEvents() {
        this.showMessage("Reading events...");
        console.log(`Attempting to fetch: ${this.apiBaseUrl}/read`);
        
        try {
            const res = await fetch(`${this.apiBaseUrl}/read`);
            console.log('Response status:', res.status);
            
            if (!res.ok) {
                throw new Error(`Read failed with status ${res.status}`);
            }
            
            const data = await res.json();
            console.log('Data received:', data);
            
            if (this.elements.output) {
                // Format as a nice table
                if (Array.isArray(data) && data.length > 0) {
                    const tableHtml = this.formatDataAsTable(data);
                    this.elements.output.innerHTML = tableHtml;
                } else {
                    this.elements.output.innerHTML = `
                        <div style="color: var(--text-muted); padding: 2rem; text-align: center;">
                            No events found in database
                        </div>
                    `;
                }
            }
            
            this.updateTimestamp();
            this.showMessage(`Loaded ${Array.isArray(data) ? data.length : 0} events`);
            
        } catch (err) {
            console.error('Fetch error:', err);
            this.showMessage(`Error: ${err.message}`, true);
        }
    }
    
    formatDataAsTable(data) {
        if (!data || data.length === 0) return '<p>No data</p>';
        
        // Reverse the data array to show newest first
        const reversedData = [...data].reverse();
        
        // Get all unique keys from the data
        const keys = Object.keys(reversedData[0]);
        
        let html = `
            <table style="width: 100%; border-collapse: collapse; color: var(--text-primary);">
                <thead>
                    <tr style="background: var(--bg-card-hover); border-bottom: 2px solid var(--border);">
                        ${keys.map(key => `
                            <th style="padding: 0.75rem; text-align: left; font-weight: 600; text-transform: uppercase; font-size: 0.75rem; color: var(--text-secondary);">
                                ${key}
                            </th>
                        `).join('')}
                    </tr>
                </thead>
                <tbody>
                    ${reversedData.map((row, idx) => `
                        <tr style="border-bottom: 1px solid var(--border); ${idx % 2 === 0 ? 'background: var(--bg-dark);' : ''}">
                            ${keys.map(key => `
                                <td style="padding: 0.75rem; font-size: 0.875rem;">
                                    ${this.formatValue(row[key])}
                                </td>
                            `).join('')}
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        `;
        
        return html;
    }
    
    formatValue(value) {
        if (value === null || value === undefined) return '<span style="color: var(--text-muted);">—</span>';
        if (typeof value === 'object') return `<code style="font-size: 0.75rem;">${JSON.stringify(value)}</code>`;
        if (typeof value === 'number') return value.toFixed(4);
        return value;
    }
    
    async saveCsv() {
        this.showMessage("Generating CSV...");
        console.log(`Attempting to download CSV from: ${this.apiBaseUrl}/save-csv`);
        
        try {
            // Try to fetch the CSV with a timeout
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 5000);
            
            const response = await fetch(`${this.apiBaseUrl}/save-csv`, {
                signal: controller.signal
            }).catch(err => {
                clearTimeout(timeoutId);
                throw new Error('Cannot connect to database server. Please check your connection and settings.');
            });
            
            clearTimeout(timeoutId);
            
            if (!response.ok) {
                throw new Error(`Server returned error: ${response.status}`);
            }
            
            // Get the blob data
            const blob = await response.blob();
            
            // Create download link
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `events_${new Date().toISOString().split('T')[0]}.csv`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
            
            this.showMessage("✅ CSV downloaded successfully");
            
        } catch (err) {
            console.error('CSV download error:', err);
            if (err.name === 'AbortError') {
                this.showMessage('Connection timeout. Please check if the server is running.', true);
            } else {
                this.showMessage(`Error: ${err.message}`, true);
            }
        }
    }
    
    startAutoRefresh() {
        // Auto-refresh every 5 seconds when on the data tab
        this.refreshInterval = setInterval(() => {
            const dataTab = document.getElementById('dataTab');
            if (dataTab && !dataTab.classList.contains('hidden')) {
                console.log('Auto-refreshing data...');
                this.readEvents();
            }
        }, 5000);
        
        // Initial load
        setTimeout(() => {
            const dataTab = document.getElementById('dataTab');
            if (dataTab && !dataTab.classList.contains('hidden')) {
                this.readEvents();
            }
        }, 500);
    }
    
    async clearDatabase() {
        // Show confirmation dialog with strong warning
        const confirmed = confirm(
            "⚠️ WARNING: PERMANENT DATA DELETION ⚠️\n\n" +
            "This action will permanently delete ALL events from the database.\n\n" +
            "This operation CANNOT be undone!\n\n" +
            "Are you absolutely sure you want to proceed?"
        );
        
        if (!confirmed) {
            console.log('Database clear cancelled by user');
            return;
        }
        
        // Second confirmation
        const doubleConfirm = confirm(
            "⚠️ FINAL CONFIRMATION ⚠️\n\n" +
            "Click OK to permanently delete all database records.\n" +
            "Click Cancel to abort."
        );
        
        if (!doubleConfirm) {
            console.log('Database clear cancelled by user on second confirmation');
            return;
        }
        
        this.showMessage("Clearing database...");
        console.log(`Attempting to clear database at: ${this.apiBaseUrl}/clear`);
        
        try {
            const res = await fetch(`${this.apiBaseUrl}/clear`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            
            console.log('Clear response status:', res.status);
            
            if (!res.ok) {
                throw new Error(`Clear failed with status ${res.status}`);
            }
            
            const result = await res.text();
            console.log('Clear result:', result);
            
            this.showMessage("✅ Database cleared successfully");
            
            // Clear the output display
            if (this.elements.output) {
                this.elements.output.innerHTML = `
                    <div style="color: var(--text-muted); padding: 2rem; text-align: center;">
                        Database has been cleared. No events found.
                    </div>
                `;
            }
            
            this.updateTimestamp();
            
            // Refresh data after a short delay
            setTimeout(() => {
                this.readEvents();
            }, 1000);
            
        } catch (err) {
            console.error('Clear database error:', err);
            this.showMessage(`❌ Error clearing database: ${err.message}`, true);
        }
    }
    
    updateApiUrl(newUrl) {
        this.apiBaseUrl = newUrl;
        console.log('Database API URL updated to:', newUrl);
    }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.databaseManager = new DatabaseManager();
        console.log('✅ Database Manager initialized');
    });
} else {
    window.databaseManager = new DatabaseManager();
    console.log('✅ Database Manager initialized');
}