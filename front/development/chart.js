// chart.js - Canvas-based scatter chart with SignalR

const canvas = document.getElementById('chartCanvas');
const ctx = canvas.getContext('2d');
const legendItemsEl = document.getElementById('legendItems');

// Connection UI elements
const signalrUrlInput = document.getElementById('signalrUrl');
const connectButton = document.getElementById('connectButton');
const disconnectButton = document.getElementById('disconnectButton');
const statusIndicator = document.getElementById('statusIndicator');
const statusText = document.getElementById('statusText');

// Test input elements
const testXInput = document.getElementById('testX');
const testYInput = document.getElementById('testY');
const testTidInput = document.getElementById('testTid');
const testStatusInput = document.getElementById('testStatus');
const addPointButton = document.getElementById('addPointButton');
const clearPointsButton = document.getElementById('clearPointsButton');

// Chart configuration
const chartConfig = {
    padding: 60,
    xMin: -5,
    xMax: 5,
    yMin: 0,
    yMax: 10,
    width: canvas.width,
    height: canvas.height
};

// Color map for TIDs
const tidColorMap = new Map();

// Store all points
let points = [];

// SignalR connection (initially null)
let connection = null;

// Generate consistent color for each TID
function getColorForTid(tid) {
    if (!tidColorMap.has(tid)) {
        const hue = Math.floor(Math.random() * 360);
        const saturation = 80 + Math.random() * 20;
        const lightness = 50 + Math.random() * 20;
        const color = `hsl(${hue}, ${saturation}%, ${lightness}%)`;
        tidColorMap.set(tid, color);
    }
    return tidColorMap.get(tid);
}

// Convert data coordinates to canvas coordinates
function dataToCanvas(x, y) {
    const { padding, xMin, xMax, yMin, yMax, width, height } = chartConfig;
    
    const plotWidth = width - 2 * padding;
    const plotHeight = height - 2 * padding;
    
    const canvasX = padding + ((x - xMin) / (xMax - xMin)) * plotWidth;
    const canvasY = height - padding - ((y - yMin) / (yMax - yMin)) * plotHeight;
    
    return { canvasX, canvasY };
}

// Draw grid and axes
function drawGrid() {
    const { padding, xMin, xMax, yMin, yMax, width, height } = chartConfig;
    
    ctx.strokeStyle = '#444';
    ctx.lineWidth = 1;
    ctx.fillStyle = '#ccc';
    ctx.font = '12px sans-serif';
    
    // Vertical grid lines (X axis)
    const xTicks = [xMin, 0, xMax];
    xTicks.forEach(tick => {
        const { canvasX } = dataToCanvas(tick, yMin);
        
        // Grid line
        ctx.beginPath();
        ctx.moveTo(canvasX, padding);
        ctx.lineTo(canvasX, height - padding);
        ctx.stroke();
        
        // Label
        ctx.fillText(tick.toString(), canvasX - 10, height - padding + 20);
    });
    
    // Horizontal grid lines (Y axis)
    const yTicks = [yMin, 5, yMax];
    yTicks.forEach(tick => {
        const { canvasY } = dataToCanvas(xMin, tick);
        
        // Grid line
        ctx.beginPath();
        ctx.moveTo(padding, canvasY);
        ctx.lineTo(width - padding, canvasY);
        ctx.stroke();
        
        // Label
        ctx.fillText(tick.toString(), padding - 30, canvasY + 5);
    });
    
    // Draw axes
    ctx.strokeStyle = '#666';
    ctx.lineWidth = 2;
    
    // X axis
    ctx.beginPath();
    ctx.moveTo(padding, height - padding);
    ctx.lineTo(width - padding, height - padding);
    ctx.stroke();
    
    // Y axis
    ctx.beginPath();
    ctx.moveTo(padding, padding);
    ctx.lineTo(padding, height - padding);
    ctx.stroke();
}

// Draw half-circle at origin
function drawHalfCircle() {
    const radius = 40;
    const { canvasX, canvasY } = dataToCanvas(0, 0);
    
    ctx.fillStyle = 'rgba(255, 0, 0, 0.2)';
    ctx.strokeStyle = 'red';
    ctx.lineWidth = 2;
    
    ctx.beginPath();
    ctx.arc(canvasX, canvasY, radius, 0, Math.PI, true);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
}

// Draw all points
function drawPoints() {
    points.forEach(point => {
        const { canvasX, canvasY } = dataToCanvas(point.x, point.y);
        const color = getColorForTid(point.tid);
        
        ctx.fillStyle = color;
        ctx.strokeStyle = 'black';
        ctx.lineWidth = 1;
        
        ctx.beginPath();
        ctx.arc(canvasX, canvasY, 5, 0, 2 * Math.PI);
        ctx.fill();
        ctx.stroke();
    });
}

// Get latest point for each TID
function getLatestPoints() {
    const latestMap = new Map();
    points.forEach(point => {
        latestMap.set(point.tid, point);
    });
    return Array.from(latestMap.values());
}

// Update legend
function updateLegend() {
    const latest = getLatestPoints();
    
    legendItemsEl.innerHTML = latest.map(point => {
        const color = getColorForTid(point.tid);
        return `
            <div class="legend-item">
                <div class="legend-color" style="background-color: ${color}"></div>
                <span>TID ${point.tid} – Status: ${point.status}</span>
            </div>
        `;
    }).join('');
}

// Main render function
function render() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    drawGrid();
    drawHalfCircle();
    drawPoints();
    updateLegend();
}

// Update UI status
function updateConnectionStatus(connected) {
    if (connected) {
        statusIndicator.classList.add('connected');
        statusText.textContent = 'Connected';
        connectButton.classList.add('hidden');
        disconnectButton.classList.remove('hidden');
        signalrUrlInput.disabled = true;
    } else {
        statusIndicator.classList.remove('connected');
        statusText.textContent = 'Disconnected';
        connectButton.classList.remove('hidden');
        disconnectButton.classList.add('hidden');
        signalrUrlInput.disabled = false;
    }
}

// Connect to SignalR
async function connectToSignalR() {
    const url = signalrUrlInput.value.trim();
    
    if (!url) {
        alert('Please enter a SignalR Hub URL');
        return;
    }
    
    try {
        connectButton.disabled = true;
        connectButton.textContent = 'Connecting...';
        
        connection = new signalR.HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect()
            .build();
        
        connection.on('OnUnfilteredPoints', (data) => {
            console.log('New data received:', data);
            points = data;
            render();
            document.getElementById('lastUpdated').textContent = new Date().toLocaleString();
        });
        
        await connection.start();
        console.log('Connected to SignalR');
        updateConnectionStatus(true);
        
    } catch (err) {
        console.error('SignalR Connection Error:', err);
        alert(`Connection failed: ${err.message}`);
        updateConnectionStatus(false);
    } finally {
        connectButton.disabled = false;
        connectButton.textContent = 'Connect';
    }
}

// Disconnect from SignalR
async function disconnectFromSignalR() {
    if (connection) {
        try {
            await connection.stop();
            console.log('Disconnected from SignalR');
            connection = null;
        } catch (err) {
            console.error('Error disconnecting:', err);
        }
    }
    updateConnectionStatus(false);
}

// Add test point manually
function addTestPoint() {
    const x = parseFloat(testXInput.value);
    const y = parseFloat(testYInput.value);
    const tid = parseInt(testTidInput.value);
    const status = testStatusInput.value.trim() || 'test';
    
    if (isNaN(x) || isNaN(y) || isNaN(tid)) {
        alert('Please enter valid numbers for X, Y, and TID');
        return;
    }
    
    if (x < chartConfig.xMin || x > chartConfig.xMax) {
        alert(`X must be between ${chartConfig.xMin} and ${chartConfig.xMax}`);
        return;
    }
    
    if (y < chartConfig.yMin || y > chartConfig.yMax) {
        alert(`Y must be between ${chartConfig.yMin} and ${chartConfig.yMax}`);
        return;
    }
    
    const newPoint = { x, y, tid, status };
    points.push(newPoint);
    
    console.log('Added test point:', newPoint);
    render();
    document.getElementById('lastUpdated').textContent = new Date().toLocaleString();
}

// Clear all points
function clearAllPoints() {
    if (confirm('Are you sure you want to clear all points?')) {
        points = [];
        render();
        document.getElementById('lastUpdated').textContent = new Date().toLocaleString();
    }
}

// Event listeners
connectButton.addEventListener('click', connectToSignalR);
disconnectButton.addEventListener('click', disconnectFromSignalR);
addPointButton.addEventListener('click', addTestPoint);
clearPointsButton.addEventListener('click', clearAllPoints);

// Allow pressing Enter in URL input to connect
signalrUrlInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        connectToSignalR();
    }
});

// Allow pressing Enter in test inputs to add point
[testXInput, testYInput, testTidInput, testStatusInput].forEach(input => {
    input.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            addTestPoint();
        }
    });
});

// Initial render
render();