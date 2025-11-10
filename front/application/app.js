// app.js - Main Application Controller
 console.log('enhanced-chart.js loaded');

class ResearchPlatformApp {
    constructor() {
        this.connection = null;
        this.settings = this.loadSettings();
        this.initializeElements();
        this.attachEventListeners();
        this.initializeTimestamp();
    }
    
    loadSettings() {
        const saved = localStorage.getItem('researchPlatformSettings');
        if (saved) {
            return JSON.parse(saved);
        }
        return {
            signalrUrl: 'https://localhost:7187/web',
            databaseUrl: 'http://localhost:5000/Database'
        };
    }
    
    saveSettings() {
        localStorage.setItem('researchPlatformSettings', JSON.stringify(this.settings));
    }
    
    initializeElements() {
        this.elements = {
            // Settings modal
            settingsBtn: document.getElementById('settingsBtn'),
            settingsModal: document.getElementById('settingsModal'),
            closeSettings: document.getElementById('closeSettings'),
            signalrUrlInput: document.getElementById('signalrUrl'),
            databaseUrlInput: document.getElementById('databaseUrl'),
            saveSettingsBtn: document.getElementById('saveSettings'),
            resetSettingsBtn: document.getElementById('resetSettings'),
            
            // Status
            statusDot: document.getElementById('statusDot'),
            statusText: document.getElementById('statusText'),
            lastUpdated: document.getElementById('lastUpdated'),
            
            // Tabs
            navTabs: document.querySelectorAll('.nav-tab'),
            visualizationTab: document.getElementById('visualizationTab'),
            dataTab: document.getElementById('dataTab')
        };
        
        // Load saved settings into inputs
        if (this.elements.signalrUrlInput) {
            this.elements.signalrUrlInput.value = this.settings.signalrUrl;
        }
        if (this.elements.databaseUrlInput) {
            this.elements.databaseUrlInput.value = this.settings.databaseUrl;
        }
    }
    
    attachEventListeners() {
        // Settings modal - event listeners are now in HTML inline script
        // But we still need the save and reset functionality
        if (this.elements.saveSettingsBtn) {
            this.elements.saveSettingsBtn.addEventListener('click', () => this.saveSettingsFromModal());
        }
        if (this.elements.resetSettingsBtn) {
            this.elements.resetSettingsBtn.addEventListener('click', () => this.resetSettings());
        }
        
        // Auto-connect on startup
        const waitForChart = () => {
        if (window.chartManager) {
            console.log('✅ Chart manager detected, auto-connecting to SignalR...');
            this.connectToSignalR();
        } else {
            console.log('⏳ Waiting for chart manager...');
            setTimeout(waitForChart, 100);
        }
    };
    setTimeout(waitForChart, 100);
    }
    
    openSettings() {
        if (this.elements.settingsModal) {
            this.elements.settingsModal.classList.add('active');
        }
    }
    
    closeSettings() {
        if (this.elements.settingsModal) {
            this.elements.settingsModal.classList.remove('active');
        }
    }
    
    saveSettingsFromModal() {
        const newSettings = {
            signalrUrl: this.elements.signalrUrlInput.value.trim(),
            databaseUrl: this.elements.databaseUrlInput.value.trim()
        };
        
        console.log('Saving settings:', newSettings);
        
        if (!newSettings.signalrUrl || !newSettings.databaseUrl) {
            alert('⚠️ Please provide valid URLs for both settings.');
            return;
        }
        
        this.settings = newSettings;
        this.saveSettings();
        
        console.log('Settings saved to localStorage');
        
        // Update database manager URL
        if (window.databaseManager) {
            databaseManager.updateApiUrl(this.settings.databaseUrl);
            console.log('Database manager URL updated');
        }
        
        // Reconnect if URLs changed
        if (this.connection) {
            console.log('Reconnecting to SignalR with new URL...');
            this.disconnectFromSignalR();
            setTimeout(() => this.connectToSignalR(), 500);
        } else {
            console.log('Connecting to SignalR with new URL...');
            this.connectToSignalR();
        }
        
        this.closeSettings();
        this.showNotification('✅ Settings saved and applied successfully!', false);
    }
    
    resetSettings() {
        const confirmed = confirm('⚠️ Reset all settings to default values?');
        if (!confirmed) return;
        
        this.settings = {
            signalrUrl: 'https://localhost:7187/web',
            databaseUrl: 'http://localhost:5000/Database'
        };
        
        if (this.elements.signalrUrlInput) {
            this.elements.signalrUrlInput.value = this.settings.signalrUrl;
        }
        if (this.elements.databaseUrlInput) {
            this.elements.databaseUrlInput.value = this.settings.databaseUrl;
        }
        
        this.saveSettings();
        console.log('Settings reset to default:', this.settings);
        this.showNotification('✅ Settings reset to default values', false);
        
        // Update database manager
        if (window.databaseManager) {
            databaseManager.updateApiUrl(this.settings.databaseUrl);
        }
        
        // Reconnect
        if (this.connection) {
            this.disconnectFromSignalR();
            setTimeout(() => this.connectToSignalR(), 500);
        }
    }
    
    switchTab(tabName) {
        // This method can be removed since tab switching is handled in HTML
        // But keeping it for keyboard shortcut support
        const tabs = document.querySelectorAll('.nav-tab');
        tabs.forEach(tab => {
            if (tab.dataset.tab === tabName) {
                tab.click();
            }
        });
    }
    
    updateConnectionStatus(connected) {
        if (connected) {
            this.elements.statusDot.classList.add('connected');
            this.elements.statusText.textContent = 'Connected';
        } else {
            this.elements.statusDot.classList.remove('connected');
            this.elements.statusText.textContent = 'Disconnected';
        }
    }
    
    async connectToSignalR() {
        const url = this.settings.signalrUrl;
        
        if (!url) {
            this.showNotification('Please configure SignalR URL in settings', true);
            return;
        }
        
        try {
            this.elements.statusText.textContent = 'Connecting...';
            console.log('=== SignalR Connection Attempt ===');
            console.log('URL:', url);
            console.log('Full settings:', this.settings);
            
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(url)
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        console.log(`Retry attempt ${retryContext.previousRetryCount + 1}`);
                        return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();
            
            // Handle incoming data
            this.connection.on('OnUnfilteredPoints', (data) => {
            console.log('=== Data Received from SignalR ===');
            console.log('Data type:', typeof data);
            console.log('Is array:', Array.isArray(data));
            console.log('Data length:', data?.length);
            
            if (data && data.length > 0) {
                console.log('First item structure:', data[0]);
                console.log('First item keys:', Object.keys(data[0]));
                console.log('Sample data:', data.slice(0, Math.min(3, data.length)));
            }
            
            if (window.chartManager) {
                console.log('✅ Updating chart with', data?.length || 0, 'points');
                window.chartManager.setPoints(data);  // ✅ Uses global reference
                this.updateTimestamp();
            } else {
                console.error('❌ chartManager not found!');
            }
        });
            
            // Handle reconnection
            this.connection.onreconnecting((error) => {
                console.warn('SignalR reconnecting...', error);
                this.elements.statusText.textContent = 'Reconnecting...';
            });
            
            this.connection.onreconnected((connectionId) => {
                console.log('SignalR reconnected:', connectionId);
                this.updateConnectionStatus(true);
            });
            
            this.connection.onclose((error) => {
                console.error('SignalR connection closed:', error);
                this.updateConnectionStatus(false);
            });
            
            await this.connection.start();
            console.log('✅ SignalR connected successfully');
            console.log('Connection ID:', this.connection.connectionId);
            this.updateConnectionStatus(true);
            this.showNotification('Connected to data stream', false);
            
        } catch (err) {
            console.error('=== SignalR Connection Error ===');
            console.error('Error type:', err.constructor.name);
            console.error('Error message:', err.message);
            console.error('Full error:', err);
            console.error('Stack:', err.stack);
            this.updateConnectionStatus(false);
            this.showNotification(`Connection failed: ${err.message}`, true);
        }
    }
    
    async disconnectFromSignalR() {
        if (this.connection) {
            try {
                await this.connection.stop();
                console.log('SignalR disconnected');
                this.connection = null;
            } catch (err) {
                console.error('Error disconnecting:', err);
            }
        }
        this.updateConnectionStatus(false);
    }
    
    updateTimestamp() {
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
    
    initializeTimestamp() {
        this.updateTimestamp();
        // Update timestamp every second
        setInterval(() => {
            if (!this.elements.visualizationTab.classList.contains('hidden')) {
                // Only update if visualization tab is active
                const lastUpdateText = this.elements.lastUpdated.textContent;
                if (lastUpdateText !== '--') {
                    // Keep the last update time, don't continuously update
                }
            }
        }, 1000);
    }
    
    showNotification(message, isError) {
        // Console log for debugging
        console.log(`[${isError ? 'ERROR' : 'INFO'}] ${message}`);
        
        // Visual toast notification
        const notification = document.createElement('div');
        notification.className = `status-message ${isError ? 'error' : 'success'}`;
        notification.style.position = 'fixed';
        notification.style.top = '100px';
        notification.style.right = '20px';
        notification.style.zIndex = '9999';
        notification.style.minWidth = '300px';
        notification.style.animation = 'slideIn 0.3s ease';
        notification.innerHTML = `
            <span>${isError ? '❌' : '✅'}</span>
            <span>${message}</span>
        `;
        
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.style.opacity = '0';
            notification.style.transition = 'opacity 0.3s ease';
            setTimeout(() => notification.remove(), 300);
        }, 4000);
    }
}

// Add slide-in animation
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from {
            transform: translateX(400px);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
`;
document.head.appendChild(style);

// Initialize application when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.app = new ResearchPlatformApp();
    });
} else {
    window.app = new ResearchPlatformApp();
}