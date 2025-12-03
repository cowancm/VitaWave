// main.js - Enhanced Electron Configuration
const { app, BrowserWindow, Menu } = require('electron');
const path = require('path');

let mainWindow;

function createWindow() {
    mainWindow = new BrowserWindow({
        width: 1600,
        height: 1000,
        minWidth: 1200,
        minHeight: 800,
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false,
            webSecurity: false // Allow CORS for development
        },
        backgroundColor: '#0f172a',
        icon: path.join(__dirname, 'IMG_2231.png'),
        title: 'VitaWave',
        show: false // Don't show until ready
    });

    // Load the index.html
    mainWindow.loadFile('index.html');

    // Show window when ready
    mainWindow.once('ready-to-show', () => {
        mainWindow.show();
    });

    // Create application menu
    createMenu();

    // Open DevTools in development mode
    if (process.argv.includes('--debug')) {
        mainWindow.webContents.openDevTools();
    }

    // Handle window closed
    mainWindow.on('closed', () => {
        mainWindow = null;
    });
}

function createMenu() {
    const template = [
        {
            label: 'File',
            submenu: [
                {
                    label: 'Settings',
                    accelerator: 'CmdOrCtrl+,',
                    click: () => {
                        mainWindow.webContents.send('open-settings');
                    }
                },
                { type: 'separator' },
                {
                    label: 'Exit',
                    accelerator: 'CmdOrCtrl+Q',
                    click: () => {
                        app.quit();
                    }
                }
            ]
        },
        {
            label: 'View',
            submenu: [
                {
                    label: 'Visualization',
                    accelerator: 'CmdOrCtrl+1',
                    click: () => {
                        mainWindow.webContents.send('switch-tab', 'visualization');
                    }
                },
                {
                    label: 'Data Management',
                    accelerator: 'CmdOrCtrl+2',
                    click: () => {
                        mainWindow.webContents.send('switch-tab', 'data');
                    }
                },
                {
                    label: 'Analysis',
                    accelerator: 'CmdOrCtrl+3',
                    click: () => {
                        mainWindow.webContents.send('switch-tab', 'analysis');
                    }
                },
                { type: 'separator' },
                { role: 'reload' },
                { role: 'forceReload' },
                { role: 'toggleDevTools' },
                { type: 'separator' },
                { role: 'resetZoom' },
                { role: 'zoomIn' },
                { role: 'zoomOut' },
                { type: 'separator' },
                { role: 'togglefullscreen' }
            ]
        },
        {
            label: 'Window',
            submenu: [
                { role: 'minimize' },
                { role: 'zoom' },
                { type: 'separator' },
                { role: 'close' }
            ]
        },
        {
            label: 'Help',
            submenu: [
                {
                    label: 'Documentation',
                    click: async () => {
                        const { shell } = require('electron');
                        await shell.openExternal('https://github.com/yourproject/docs');
                    }
                },
                {
                    label: 'Report Issue',
                    click: async () => {
                        const { shell } = require('electron');
                        await shell.openExternal('https://github.com/yourproject/issues');
                    }
                },
                { type: 'separator' },
                {
                    label: 'About',
                    click: () => {
                        const { dialog } = require('electron');
                        dialog.showMessageBox(mainWindow, {
                            type: 'info',
                            title: 'About',
                            message: '',
                            detail: 'Version 2.0.0\n\nA professional research platform for tracking and analyzing elderly mobility patterns.\n\n© 2025 Research Team'
                        });
                    }
                }
            ]
        }
    ];

    const menu = Menu.buildFromTemplate(template);
    Menu.setApplicationMenu(menu);
}

// App lifecycle events
app.whenReady().then(() => {
    createWindow();

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) {
            createWindow();
        }
    });
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

// Handle certificate errors for development (self-signed certificates)
app.on('certificate-error', (event, webContents, url, error, certificate, callback) => {
    if (url.startsWith('https://localhost')) {
        // Allow localhost self-signed certificates in development
        event.preventDefault();
        callback(true);
    } else {
        callback(false);
    }
});