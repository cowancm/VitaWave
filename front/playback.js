// playback.js - Playback visualization with SignalR DevHub

// Canvas elements
const canvasXY = document.getElementById('playbackCanvasXY');
const canvasXZ = document.getElementById('playbackCanvasXZ');
const ctxXY = canvasXY.getContext('2d');
const ctxXZ = canvasXZ.getContext('2d');

// UI elements
const playbackFileSelect = document.getElementById('playbackFileSelect');
const refreshFilesButton = document.getElementById('refreshFilesButton');
const loadFileButton = document.getElementById('loadFileButton');
const timelineSlider = document.getElementById('timelineSlider');
const playButton = document.getElementById('playButton');
const stopButton = document.getElementById('stopButton');
const currentFrameEl = document.getElementById('currentFrame');
const totalFramesEl = document.getElementById('totalFrames');
const currentTimeEl = document.getElementById('currentTime');
const recordNameInput = document.getElementById('recordName');
const recordFramesInput = document.getElementById('recordFrames');
const startRecordButton = document.getElementById('startRecordButton');

// Chart configuration
const playbackConfig = {
    padding: 50,
    xMin: -5,
    xMax: 5,
    yMin: 0,
    yMax: 10,
    zMin: 0,
    zMax: 4,
    width: canvasXY.width,
    height: canvasXY.height,
    frameTimeMs: 55 // Each frame is 55ms
};

// Playback state
let devHubConnection = null;
let playbackData = null;
let currentFrameIndex = 0;
let isPlaying = false;
let playbackInterval = null;
let baseUrl = '';

// Get color based on doppler velocity (0-5 range, 0=white, 5=dark red)
function getDopplerColor(doppler) {
    // Clamp doppler to 0-5 range
    const value = Math.max(0, Math.min(5, Math.abs(doppler)));
    
    // Interpolate from white (255,255,255) to dark red (139,0,0)
    const ratio = value / 5;
    const r = Math.floor(255 - (255 - 139) * ratio);
    const g = Math.floor(255 * (1 - ratio));
    const b = Math.floor(255 * (1 - ratio));
    
    return `rgb(${r}, ${g}, ${b})`;
}

// Convert data coordinates to canvas coordinates
function dataToCanvas(x, y, config) {
    const { padding, xMin, xMax, yMin, yMax, width, height } = config;
    
    const plotWidth = width - 2 * padding;
    const plotHeight = height - 2 * padding;
    
    const canvasX = padding + ((x - xMin) / (xMax - xMin)) * plotWidth;
    const canvasY = height - padding - ((y - yMin) / (yMax - yMin)) * plotHeight;
    
    return { canvasX, canvasY };
}

// Draw grid and axes for a view
function drawGrid(ctx, xLabel, yLabel, config) {
    const { padding, xMin, xMax, yMin, yMax, width, height } = config;
    
    ctx.strokeStyle = '#444';
    ctx.lineWidth = 1;
    ctx.fillStyle = '#ccc';
    ctx.font = '12px sans-serif';
    
    // Vertical grid lines (X axis)
    const xTicks = [xMin, 0, xMax];
    xTicks.forEach(tick => {
        const { canvasX } = dataToCanvas(tick, yMin, config);
        
        // Grid line
        ctx.beginPath();
        ctx.moveTo(canvasX, padding);
        ctx.lineTo(canvasX, height - padding);
        ctx.stroke();
        
        // Label
        ctx.fillText(tick.toString(), canvasX - 10, height - padding + 20);
    });
    
    // Horizontal grid lines (Y axis)
    const yTicks = [yMin, Math.floor((yMin + yMax) / 2), yMax];
    yTicks.forEach(tick => {
        const { canvasY } = dataToCanvas(xMin, tick, config);
        
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
    
    // Axis labels
    ctx.fillStyle = '#fff';
    ctx.font = '14px sans-serif';
    ctx.fillText(xLabel, width / 2 - 10, height - 10);
    ctx.save();
    ctx.translate(15, height / 2);
    ctx.rotate(-Math.PI / 2);
    ctx.fillText(yLabel, 0, 0);
    ctx.restore();
}

// Draw points for XY view
function drawPointsXY(points) {
    const config = {
        ...playbackConfig,
        yMin: playbackConfig.yMin,
        yMax: playbackConfig.yMax
    };
    
    points.forEach(point => {
        const { canvasX, canvasY } = dataToCanvas(point.x, point.y, config);
        const color = getDopplerColor(point.doppler);
        
        ctxXY.fillStyle = color;
        ctxXY.strokeStyle = 'black';
        ctxXY.lineWidth = 1;
        
        ctxXY.beginPath();
        ctxXY.arc(canvasX, canvasY, 4, 0, 2 * Math.PI);
        ctxXY.fill();
        ctxXY.stroke();
    });
}

// Draw points for XZ view
function drawPointsXZ(points) {
    const config = {
        ...playbackConfig,
        yMin: playbackConfig.zMin,
        yMax: playbackConfig.zMax
    };
    
    points.forEach(point => {
        const { canvasX, canvasY } = dataToCanvas(point.x, point.z, config);
        const color = getDopplerColor(point.doppler);
        
        ctxXZ.fillStyle = color;
        ctxXZ.strokeStyle = 'black';
        ctxXZ.lineWidth = 1;
        
        ctxXZ.beginPath();
        ctxXZ.arc(canvasX, canvasY, 4, 0, 2 * Math.PI);
        ctxXZ.fill();
        ctxXZ.stroke();
    });
}

// Render current frame
function renderFrame(frameIndex) {
    if (!playbackData || !playbackData.frames || frameIndex >= playbackData.frames.length) {
        return;
    }
    
    const frame = playbackData.frames[frameIndex];
    const points = frame.points || [];
    
    // Clear canvases
    ctxXY.clearRect(0, 0, canvasXY.width, canvasXY.height);
    ctxXZ.clearRect(0, 0, canvasXZ.width, canvasXZ.height);
    
    // Draw XY view
    const configXY = {
        ...playbackConfig,
        yMin: playbackConfig.yMin,
        yMax: playbackConfig.yMax
    };
    drawGrid(ctxXY, 'X', 'Y', configXY);
    drawPointsXY(points);
    
    // Draw XZ view
    const configXZ = {
        ...playbackConfig,
        yMin: playbackConfig.zMin,
        yMax: playbackConfig.zMax
    };
    drawGrid(ctxXZ, 'X', 'Z', configXZ);
    drawPointsXZ(points);
    
    // Update UI
    currentFrameIndex = frameIndex;
    currentFrameEl.textContent = frameIndex + 1;
    const timeInSeconds = (frameIndex * playbackConfig.frameTimeMs / 1000).toFixed(2);
    currentTimeEl.textContent = timeInSeconds;
    timelineSlider.value = frameIndex;
}

// Connect to DevHub
async function connectToDevHub() {
    // Check if already connected
    if (devHubConnection && devHubConnection.state === signalR.HubConnectionState.Connected) {
        console.log('Already connected to DevHub');
        return true;
    }
    
    // Extract base URL from the main SignalR URL
    const mainUrl = document.getElementById('signalrUrl').value.trim();
    
    if (!mainUrl) {
        showNotification('Please enter a SignalR URL first', 'error');
        return false;
    }
    
    try {
        // Extract base URL (remove /web or any path after host)
        const url = new URL(mainUrl);
        baseUrl = `${url.protocol}//${url.host}/dev`;
        
        console.log('Connecting to DevHub:', baseUrl);
        
        devHubConnection = new signalR.HubConnectionBuilder()
            .withUrl(baseUrl)
            .withAutomaticReconnect()
            .build();
        
        // Handle receiving playback file names
        devHubConnection.on('ReceivePlaybackFileNames', (files) => {
            console.log('Received playback files:', files);
            updateFileList(files);
        });
        
        // Handle receiving playback file data
        devHubConnection.on('ReceivePlaybackFile', (file) => {
            console.log('Received playback file data:', file);
            loadPlaybackData(file);
        });
        
        await devHubConnection.start();
        console.log('Connected to DevHub');
        showNotification('Connected to DevHub', 'success');
        
        return true;
        
    } catch (err) {
        console.error('DevHub Connection Error:', err);
        showNotification(`DevHub connection failed: ${err.message}`, 'error');
        return false;
    }
}

// Update file list dropdown
function updateFileList(files) {
    playbackFileSelect.innerHTML = '<option value="">-- No file selected --</option>';
    
    files.forEach(filePath => {
        // Extract just the filename from the full path
        const fileName = filePath.split('\\').pop().split('/').pop();
        const option = document.createElement('option');
        option.value = fileName;
        option.textContent = fileName;
        playbackFileSelect.appendChild(option);
    });
}

// Request files from server
async function requestFiles() {
    // Ensure we're connected
    if (!devHubConnection || devHubConnection.state !== signalR.HubConnectionState.Connected) {
        console.log('Not connected to DevHub, attempting to connect...');
        const connected = await connectToDevHub();
        if (!connected) {
            showNotification('Failed to connect to DevHub', 'error');
            return;
        }
    }
    
    try {
        console.log('Requesting files from server...');
        await devHubConnection.invoke('RequestFiles');
        console.log('File request sent successfully');
    } catch (err) {
        console.error('Error requesting files:', err);
        showNotification(`Failed to request files: ${err.message}`, 'error');
    }
}

// Load selected file
async function loadSelectedFile() {
    const fileName = playbackFileSelect.value;
    
    if (!fileName) {
        showNotification('Please select a file', 'error');
        return;
    }
    
    // Ensure we're connected
    if (!devHubConnection || devHubConnection.state !== signalR.HubConnectionState.Connected) {
        console.log('Not connected to DevHub, attempting to connect...');
        const connected = await connectToDevHub();
        if (!connected) {
            showNotification('Failed to connect to DevHub', 'error');
            return;
        }
    }
    
    try {
        console.log('Requesting playback file:', fileName);
        await devHubConnection.invoke('SendPlaybackFile', fileName);
        showNotification(`Loading file: ${fileName}`, 'info');
    } catch (err) {
        console.error('Error loading file:', err);
        showNotification(`Failed to load file: ${err.message}`, 'error');
    }
}

// Load playback data
function loadPlaybackData(data) {
    playbackData = data;
    currentFrameIndex = 0;
    
    if (!playbackData.frames || playbackData.frames.length === 0) {
        showNotification('No frames found in playback file', 'error');
        return;
    }
    
    // Update UI
    totalFramesEl.textContent = playbackData.frames.length;
    timelineSlider.max = playbackData.frames.length - 1;
    timelineSlider.value = 0;
    timelineSlider.disabled = false;
    playButton.disabled = false;
    stopButton.disabled = false;
    
    // Render first frame
    renderFrame(0);
    
    showNotification(`Loaded ${playbackData.frames.length} frames`, 'success');
    console.log(`Loaded ${playbackData.frames.length} frames`);
}

// Play/Pause playback
function togglePlayback() {
    if (isPlaying) {
        pausePlayback();
    } else {
        startPlayback();
    }
}

// Start playback
function startPlayback() {
    if (!playbackData || playbackData.frames.length === 0) {
        return;
    }
    
    isPlaying = true;
    playButton.textContent = 'Pause';
    playButton.classList.add('playing');
    
    playbackInterval = setInterval(() => {
        if (currentFrameIndex >= playbackData.frames.length - 1) {
            // End of playback
            pausePlayback();
            return;
        }
        
        currentFrameIndex++;
        renderFrame(currentFrameIndex);
    }, playbackConfig.frameTimeMs);
}

// Pause playback
function pausePlayback() {
    isPlaying = false;
    playButton.textContent = 'Play';
    playButton.classList.remove('playing');
    
    if (playbackInterval) {
        clearInterval(playbackInterval);
        playbackInterval = null;
    }
}

// Stop playback
function stopPlayback() {
    pausePlayback();
    currentFrameIndex = 0;
    renderFrame(0);
}

// Start recording
async function startRecording() {
    const fileName = recordNameInput.value.trim();
    const numFrames = parseInt(recordFramesInput.value);
    
    if (!fileName) {
        showNotification('Please enter a file name', 'error');
        return;
    }
    
    if (isNaN(numFrames) || numFrames < 1) {
        showNotification('Please enter a valid number of frames', 'error');
        return;
    }
    
    // Ensure we're connected
    if (!devHubConnection || devHubConnection.state !== signalR.HubConnectionState.Connected) {
        console.log('Not connected to DevHub, attempting to connect...');
        const connected = await connectToDevHub();
        if (!connected) {
            showNotification('Failed to connect to DevHub', 'error');
            return;
        }
    }
    
    try {
        console.log(`Starting recording: ${fileName}, ${numFrames} frames`);
        await devHubConnection.invoke('Record', numFrames, fileName);
        showNotification(`Recording started: ${fileName} (${numFrames} frames)`, 'success');
    } catch (err) {
        console.error('Error starting recording:', err);
        showNotification(`Failed to start recording: ${err.message}`, 'error');
    }
}

// Event listeners
refreshFilesButton.addEventListener('click', requestFiles);
loadFileButton.addEventListener('click', loadSelectedFile);
playButton.addEventListener('click', togglePlayback);
stopButton.addEventListener('click', stopPlayback);
startRecordButton.addEventListener('click', startRecording);

timelineSlider.addEventListener('input', (e) => {
    if (!isPlaying) {
        const frameIndex = parseInt(e.target.value);
        renderFrame(frameIndex);
    }
});

// Initialize empty charts
const emptyConfig = {
    ...playbackConfig,
    yMin: playbackConfig.yMin,
    yMax: playbackConfig.yMax
};
drawGrid(ctxXY, 'X', 'Y', emptyConfig);

const emptyConfigXZ = {
    ...playbackConfig,
    yMin: playbackConfig.zMin,
    yMax: playbackConfig.zMax
};
drawGrid(ctxXZ, 'X', 'Z', emptyConfigXZ);

// Auto-connect to DevHub when playback tab is opened
document.querySelector('[data-tab="playback"]').addEventListener('click', async () => {
    // Small delay to ensure tab is visible
    setTimeout(async () => {
        if (!devHubConnection || devHubConnection.state !== signalR.HubConnectionState.Connected) {
            console.log('Playback tab opened, connecting to DevHub...');
            const connected = await connectToDevHub();
            if (connected) {
                // Request files after successful connection
                await requestFiles();
            }
        }
    }, 100);
});